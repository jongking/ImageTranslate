using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using Newtonsoft.Json;
using Tesseract;

namespace ImageTranslate
{
    public partial class Form1 : Form
    {
        private Image _nowImage;
        private Image _zoomImage;
        //private double _zoomRate;
        private Image _ocrImage;
        
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] file = (string[])e.Data.GetData(DataFormats.FileDrop);
                for (int i = 0; i <= file.Length - 1; i++)
                {
                    if (System.IO.File.Exists(file[i]))
                    {
                        _nowImage = Image.FromFile(file[i]);
                        //ShowOcrResult(_nowImage);
                        _zoomImage = GetZoomImage(_nowImage);
                        pictureBox1.Refresh();
                        break;
                    }
                }
            }
        }

        private void Form1_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Move;
            }
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (_nowImage != null && _zoomImage != null)
            {
                var point = new Point(0, 0);
                e.Graphics.DrawImage(_zoomImage, point);
            }
            if (MouseIsDown)
            {
                Pen pen1 = new Pen(Color.Black);
                pen1.DashStyle = DashStyle.Dash;
                e.Graphics.DrawRectangle(pen1, MouseRect);
            }
        }

        private Image GetZoomImage(Image srcImage)
        {
            var srcb = new Bitmap(srcImage);
            var r = GetThumbnail(srcb, 800, 600);
            return r;
        }

        public static Bitmap GetThumbnail(Bitmap b, int destWidth, int destHeight)
        {
            System.Drawing.Image imgSource = b;
            System.Drawing.Imaging.ImageFormat thisFormat = imgSource.RawFormat;
            int sW = 0, sH = 0;
            // 按比例缩放           
            int sWidth = imgSource.Width;
            int sHeight = imgSource.Height;
            if (sHeight > destHeight || sWidth > destWidth)
            {
                if ((sWidth * destHeight) > (sHeight * destWidth))
                {
                    sW = destWidth;
                    sH = (destWidth * sHeight) / sWidth;
                }
                else
                {
                    sH = destHeight;
                    sW = (sWidth * destHeight) / sHeight;
                }
            }
            else
            {
                sW = sWidth;
                sH = sHeight;
            }
            Bitmap outBmp = new Bitmap(sW, sH);
            Graphics g = Graphics.FromImage(outBmp);
            g.Clear(Color.Transparent);
            // 设置画布的描绘质量         
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(imgSource, new Rectangle(0, 0, sW, sH), 0, 0, imgSource.Width, imgSource.Height, GraphicsUnit.Pixel);
            g.Dispose();
            return outBmp;
        }

        private void ShowOcrResult(Image srcImage)
        {
            using (var engine = new TesseractEngine(System.Environment.CurrentDirectory + "/tessdata", "jpn", EngineMode.Default))
            {
                engine.SetVariable("chop_enable ", "F");
                engine.SetVariable("enable_new_segsearch", 0);
                engine.SetVariable("use_new_state_cost ", "F");
                engine.SetVariable("segment_segcost_rating", "F");
                engine.SetVariable("language_model_ngram_on", 0);
                engine.SetVariable("textord_force_make_prop_words", "F");
                engine.SetVariable("edges_max_children_per_outline", 50);

                // have to load Pix via a bitmap since Pix doesn't support loading a stream.
                using (var image = new System.Drawing.Bitmap(srcImage))
                {
                    using (var pix = PixConverter.ToPix(image))
                    {
                        using (var page = engine.Process(pix))
                        {
                            textBox2.Text = page.GetText();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 显示翻译结果
        /// </summary>
        private void ShowTranslateResult()
        {
            if (textBox2.Text == "")
            {
                textBox1.Text = "";
                return;
            }
            //byte[] buffer = Encoding.UTF8.GetBytes(textBox2.Text);
            //string query = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            var query = textBox2.Text.Replace("\n", string.Empty).Replace("\r", string.Empty);
            var from = "jp";
            var to = "zh";
            var appid = "20170317000042489";
            var appkey = "HKYz5OA4aOloL60QyM2w";
            var salt = "12322";
            var sign = MD5Encrypt(appid + query + salt + appkey);
            var url =
                string.Format(
                    "http://api.fanyi.baidu.com/api/trans/vip/translate?q={0}&from={1}&to={2}&appid={3}&salt={4}&sign={5}",
                    HttpUtility.UrlEncode(query, Encoding.UTF8), from, to, appid, salt, sign);
            var httpclient = new HttpClient();

            var rs = httpclient.GetStringAsync(url).Result;

            TranslateResponse response = JsonConvert.DeserializeObject<TranslateResponse>(rs);

            if (response.trans_result != null)
            {
                var tl = HttpUtility.HtmlDecode(response.trans_result[0].dst);

                textBox1.Text = tl;
            }
        }

        public static string MD5Encrypt(string strText)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] result = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(strText));
            string ret = "";
            for (int i = 0; i < result.Length; i++)
            {
                ret += result[i].ToString("x").PadLeft(2, '0');
            }
            return ret;
        }

        bool MouseIsDown = false;
        Rectangle MouseRect = Rectangle.Empty;

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            MouseIsDown = true;
            DrawStart(e.Location);
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (MouseIsDown)
            {
                ResizeToRectangle(e.Location);
                pictureBox1.Refresh();
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            MouseIsDown = false;
            //ocr区域内的日文
            if (_nowImage != null && _zoomImage != null)
            {
                if (MouseRect.Width > 0 && MouseRect.Height > 0)
                {
                    _ocrImage = GetOcrImage(_nowImage, _zoomImage, MouseRect);
                    ShowOcrResult(_ocrImage);
                    ShowTranslateResult();
                }
            }
            //MouseRect = Rectangle.Empty;
        }

        private Image GetOcrImage(Image nowImage, Image zoomImage, Rectangle mouseRect)
        {
            double zoomRate = Convert.ToDouble(nowImage.Width) / zoomImage.Width;

            var destWidth = Convert.ToInt32(mouseRect.Width*zoomRate);
            var destHeight = Convert.ToInt32(mouseRect.Height*zoomRate);
            var x = Convert.ToInt32(mouseRect.X*zoomRate);
            var y = Convert.ToInt32(mouseRect.Y*zoomRate);

            Bitmap outBmp = new Bitmap(destWidth, destHeight);
            Graphics g = Graphics.FromImage(outBmp);
            g.Clear(Color.Transparent);
            // 设置画布的描绘质量         
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(nowImage, new Rectangle(0, 0, destWidth, destHeight), x, y, destWidth, destHeight, GraphicsUnit.Pixel);
            g.Dispose();
            return outBmp;
        }

        private void ResizeToRectangle(Point p)
        {
            MouseRect.Width = p.X - MouseRect.Left;
            MouseRect.Height = p.Y - MouseRect.Top;
        }

        private void DrawStart(Point StartPoint)
        {
            MouseRect = new Rectangle(StartPoint.X, StartPoint.Y, 0, 0);
        }

        public class TranslateResponse
        {
            public string from = "";
            public string to = "";
            public List<TranslateChild> trans_result;
        }

        public class TranslateChild
        {
            public string src = "";
            public string dst = "";
        }

        /// <summary>
        /// 替换图中的文字
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            if (_nowImage != null && _zoomImage != null)
            {
                if (MouseRect.Width > 0 && MouseRect.Height > 0)
                {
                    double zoomRate = Convert.ToDouble(_nowImage.Width) / _zoomImage.Width;

                    var destWidth = Convert.ToInt32(MouseRect.Width * zoomRate);
                    var destHeight = Convert.ToInt32(MouseRect.Height * zoomRate);
                    var x = Convert.ToInt32(MouseRect.X * zoomRate);
                    var y = Convert.ToInt32(MouseRect.Y * zoomRate);

                    //替换的文字图片
                    Bitmap switchBmp = new Bitmap(destWidth, destHeight);
                    string switchStr = textBox1.Text;
                    Graphics g = Graphics.FromImage(switchBmp);
                    Font font = new Font(FontFamily.GenericSansSerif, Convert.ToInt32(numericUpDown1.Value));
                    SolidBrush sbrush = new SolidBrush(Color.Black);
                    g.Clear(Color.White);
                    g.DrawString(switchStr, font, sbrush, new RectangleF(0, 0, destWidth, destHeight), new StringFormat(StringFormatFlags.DirectionVertical));
                    g.Dispose();

                    //覆盖原图
                    Graphics g2 = Graphics.FromImage(_nowImage);
                    g2.DrawImage(switchBmp, new Rectangle(x, y, destWidth, destHeight), 0, 0, destWidth, destHeight, GraphicsUnit.Pixel);
                    g2.Dispose();

                    //生成缩略图
                    _zoomImage = GetZoomImage(_nowImage);
                    pictureBox1.Refresh();
                }
            }
        }
    }
}
