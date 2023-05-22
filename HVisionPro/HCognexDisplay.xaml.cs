using Cognex.VisionPro;
using Cognex.VisionPro.ImageFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace HVisionPro
{
    /// <summary>
    /// HCognexDisplay.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class HCognexDisplay : UserControl
    {
        public string HeaderName
        {
            get { return (string)GetValue(HeaderNameProperty); }
            set
            {

                if (value != null)
                {
                    SetValue(HeaderNameProperty, value);
                }
                else
                {
                    SetValue(HeaderNameProperty, value);
                }

                engine.Header = value;
            }
        }

        public static readonly DependencyProperty HeaderNameProperty = DependencyProperty.Register(
            "HeaderName",
            typeof(string),
            typeof(HCognexDisplay),
            new PropertyMetadata(OnHeaderNameChanged));

        static void OnHeaderNameChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (HeaderNameChanged != null)
            {
                HeaderNameChanged(obj);
            }
        }

        public delegate void HeaderNameChangeHandler(object sender);
        public static HeaderNameChangeHandler HeaderNameChanged = delegate { };

        private HCognexDisplayEngine engine;
        public HCognexDisplayEngine Engine { get{ return engine; } }

        public ICogImage Image
        {
            get { return (ICogImage)GetValue(ImageProperty); }
            set
            {

                if (value != null)
                {
                    SetValue(ImageProperty, value);
                }
                else
                {
                    SetValue(ImageProperty, value);
                }
                 
            }
        }

        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(
            "Image",
            typeof(ICogImage),
            typeof(HCognexDisplay),
            new PropertyMetadata(OnImageChanged));

        static void OnImageChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (ImageChanged != null)
            {
                ImageChanged(obj);
            }
        }

        public delegate void ImageChangeHandler(object sender);
        public static ImageChangeHandler ImageChanged = delegate { };

        public bool HasImage { get { return engine.CogDisplay.Image != null; } }

        public HCognexDisplay()
        {
            InitializeComponent();

            engine = new HCognexDisplayEngine(this);
            //this.DataContext = engine;

            HeaderNameChanged += new HeaderNameChangeHandler((object sender) =>
            {
                if (this == ((HCognexDisplay)sender))
                {
                    Text_Name.Text = HeaderName;
                }
            });

            ImageChanged += new ImageChangeHandler((object sender) =>
            {
                if (this == ((HCognexDisplay)sender))
                {
                    engine.CogDisplay.Image = Image;
                    engine.CogDisplay.Fit();
                }
            });

            engine.CogDisplay.DoubleClick += CogDisplay_DoubleClick;
        }

        public delegate void OnDisplayDoubleClickEventHandler();
        public event OnDisplayDoubleClickEventHandler OnDisplayDoubleClickEvent = delegate { };

        private void CogDisplay_DoubleClick(object sender, EventArgs e)
        {
            OnDisplayDoubleClickEvent();
        }

        public void LoadImage(string path)
        {
            CogImageFileTool tool = new CogImageFileTool();
            tool.Operator.Open(path, CogImageFileModeConstants.Read);
            tool.Run();
            ICogImage image = tool.OutputImage;

            Image = image;
        }

        public void LoadImage(BitmapSource bitmapSource)
        {
            if (bitmapSource != null)
            {
                System.Drawing.Bitmap bitmap;
                ICogImage cogImage;

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    BitmapEncoder bitmapEncoder = new BmpBitmapEncoder();
                    bitmapEncoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                    bitmapEncoder.Save(memoryStream);

                    bitmap = new System.Drawing.Bitmap(memoryStream);
                }

                if (bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
                {
                    cogImage = new CogImage8Grey(bitmap);
                }
                else
                {
                    cogImage = new CogImage24PlanarColor(bitmap);
                }

                Image = cogImage;
            }
            else
            {
                Image = null;
            }
     
        }

        public void LoadImage(HCognexDisplay display)
        {
            this.Image = display.Image;
        }

        public void DrawOk()
        {
            if(Image != null)
            {
                CogGraphicLabel label = new CogGraphicLabel();
                label.Text = "OK";
                label.BackgroundColor = CogColorConstants.Black;
                label.Color = CogColorConstants.White;
                label.Alignment = CogGraphicLabelAlignmentConstants.TopRight;
                label.Font = new System.Drawing.Font("돋움", 30);
                label.X = Image.Width;
                engine.CogDisplay.InteractiveGraphics.Add(label, "", false);
            }
          
        }

        public void DrawNG()
        {
            if (Image != null)
            {
                CogGraphicLabel label = new CogGraphicLabel();
                label.Text = "NG";
                label.BackgroundColor = CogColorConstants.Red;
                label.Color = CogColorConstants.White;
                label.Alignment = CogGraphicLabelAlignmentConstants.TopRight;
                label.Font = new System.Drawing.Font("돋움", 30);
                label.X = Image.Width;
                engine.CogDisplay.InteractiveGraphics.Add(label, "", false);
            }
        }

        public void Clear()
        {
            engine.CogDisplay.StaticGraphics.Clear();
            engine.CogDisplay.InteractiveGraphics.Clear();
        }

        public void ClearImage()
        {
            Image = null;
            Clear();
        }

        public void SaveImage(string path)
        {
            /*
            CogImageFileTool tool = new CogImageFileTool();
            tool.InputImage = Image;
            tool.Operator.Open(path, CogImageFileModeConstants.Write);
            tool.Run();
            */

            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    using (System.Drawing.Bitmap bmp = Image.ToBitmap())
                    {
                        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Bmp);
                        bmp.Dispose();
                    }
                });
            }
            catch
            {
                Console.WriteLine("Failed To Save Jpeg Image");
            }
        }

        public void SaveJpegImage(string path)
        {
            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    using (System.Drawing.Bitmap bmp = Image.ToBitmap())
                    {
                        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Jpeg);
                        bmp.Dispose();
                    }
                });
            }
            catch
            {
                Console.WriteLine("Failed To Save Jpeg Image");
            }
        }

        public void SaveInspectionImage(string path)
        {
            try
            {
                
                using (System.Drawing.Image image = engine.CogDisplay.CreateContentBitmap(Cognex.VisionPro.Display.CogDisplayContentBitmapConstants.Custom, null, 1300))
                {
                    image.Save(path, System.Drawing.Imaging.ImageFormat.Jpeg);
                    image.Dispose();
                }
            }
            catch
            {
                Console.WriteLine("Failed To Save Jpeg Image");
            }
        }
    }
}
