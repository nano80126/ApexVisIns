using Basler.Pylon;
using Microsoft.Win32;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;


namespace ApexVisIns
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        #region Long life worker
        public CameraEnumer CameraEnumer;
        public Thermometer Thermometer;
        #endregion

        #region Cameras
        public static BaslerCam BaslerCam { get; set; }
        public static BaslerCam[] BaslerCams { get; set; }

        //public UvcCam UvcCam;
        #endregion

        #region Devices
        public static ObservableCollection<DeviceConfig> DeviceConfigs { get; set; }
        //public static DeviceConfig[] DeviceConfigs;
        #endregion

        #region Resources
        public Crosshair Crosshair;         // 待刪
        public AssistRect AssistRect;       // 待刪
        public AssistPoint[] AssistPoints;  // 待刪
        public Indicator Indicator;         // 待刪
        public static MsgInformer MsgInformer { get; set; }
        #endregion

        #region BFR
        public BFR.Trail BFRTrail;
        //public BFR.BFR BFR;
        #endregion

        #region Varibles
        public static bool IsProcessing { get; set; }

        private static bool MoveImage;
        private static double TempX;
        private static double TempY;

        private readonly PixelDataConverter pxConverter = new();
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            InitializePanels();
        }

        /// <summary>
        /// 初始化 Panel's MainWindow
        /// </summary>
        private void InitializePanels()
        {
#if false
            // Left Top
            ConfigPanel.MainWindow = this;
            OffsetPanel.MainWindow = this;
            // Left Bottom
            BFRTestingPanel.MainWindow = this;
            // Right Top
            ThermometerPanel.MainWindow = this; 
#endif
            // Tabs
            //ListViewTab.MainWindow = this;

            #region Tabs
            EngineerTab.MainWindow = this;
            #endregion
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine($"{DateTime.Now:HH:mm:ss.fff}");

            #region LongLifeWorker
            CameraEnumer = TryFindResource(nameof(CameraEnumer)) as CameraEnumer;
            CameraEnumer?.WorkerStart();
            //Thermometer = FindResource(nameof(Thermometer)) as Thermometer;

            //Thermometer = TryFindResource(nameof(Thermometer)) as Thermometer;
            //Thermometer?.OpenSerialPort();
            #endregion

            #region Cameras
            BaslerCam = FindResource(nameof(BaslerCam)) as BaslerCam;
            BaslerCams = FindResource(nameof(BaslerCams)) as BaslerCam[];
            #endregion

            #region Device
            //DeviceConfigs = (FindResource(nameof(DeviceConfigs)) as DeviceConfig[]).ToList();
            DeviceConfigs = FindResource(nameof(DeviceConfigs)) as ObservableCollection<DeviceConfig>;
            #endregion

            #region Find Resource
            Crosshair = FindResource(nameof(Crosshair)) as Crosshair;
            AssistRect = FindResource(nameof(AssistRect)) as AssistRect;
            Indicator = FindResource(nameof(Indicator)) as Indicator;
            AssistPoints = FindResource(nameof(AssistPoints)) as AssistPoint[];

            MsgInformer = FindResource(nameof(ApexVisIns.MsgInformer)) as MsgInformer;
            BFRTrail = FindResource(nameof(BFRTrail)) as BFR.Trail;
            #endregion

            //Debug.WriteLine($"{DeviceConfigs.Count}");
            //OnTabIndex = 0;
            //Debug.WriteLine($"{DateTime.Now:HH:mm:ss.fff}");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CameraEnumer?.WorkerEnd();
        }

        #region App Content 這邊可以全部刪掉 (待確認)
        private void ImageScroller_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer viewer = sender as ScrollViewer;
            //System.Windows.Point pt = e.GetPosition(ImageCanvas);

            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (e.Delta > 0)
                {
                    //ZoomRatio += 5;
                }
                else
                {
                    //ZoomRatio -= 5;
                }
            }
            else if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                if (e.Delta > 0)
                {
                    viewer.LineLeft();
                }
                else
                {
                    viewer.LineRight();
                }
            }
            else
            {
                if (e.Delta > 0)
                {
                    viewer.LineUp();
                }
                else
                {
                    viewer.LineDown();
                }
            }
            e.Handled = true;
        }

        private void ImageCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Canvas canvas = sender as Canvas;

            if (Keyboard.IsKeyDown(Key.Space))
            {
                ////canvas.Cursor = Cursors.Arrow;
                ////MoveImage = true;
                //System.Windows.Point pt2ImageGrid = e.GetPosition(ImageGrid);   // Point to ImageGrid
                //System.Windows.Point transformPoint = ImageGrid.TransformToVisual(ImageViewbox).Transform(pt2ImageGrid);    // Add ImageViewbox offset

                //TempX = transformPoint.X;
                //TempY = transformPoint.Y;

                //_ = canvas.CaptureMouse();
            }
            else if (AssistRect.Enable)
            {
                System.Windows.Point pt = e.GetPosition(canvas);

                //CaptureMouse();
                AssistRect.MouseDown = true;
                switch (e.ChangedButton)
                {
                    case MouseButton.Left:
                        //_ = canvas.CaptureMouse();
                        canvas.Cursor = Cursors.Cross;
                        // // // // // // // // // // //
                        AssistRect.TempX = AssistRect.X = pt.X;
                        AssistRect.TempY = AssistRect.Y = pt.Y;
                        AssistRect.Width = AssistRect.Height = 0;
                        break;
                    case MouseButton.Middle:
                        //_ = canvas.CaptureMouse();
                        canvas.Cursor = Cursors.SizeAll;
                        // // // // // // // // // // //
                        //RECT.TempX = RECT.X;
                        AssistRect.TempX = AssistRect.X;
                        //RECT.TempY = RECT.Y;
                        AssistRect.TempY = AssistRect.Y;
                        //RECT.OftX = pt.X;
                        AssistRect.OftX = pt.X;
                        //RECT.OftY = pt.Y;
                        AssistRect.OftY = pt.Y;
                        break;
                    case MouseButton.Right:
                        // 重置 RECT
                        //RECT.X = RECT.Y = RECT.Width = RECT.Height = 0;
                        AssistRect.X = AssistRect.Y = AssistRect.Width = AssistRect.Height = 0;
                        break;
                    case MouseButton.XButton1:
                        break;
                    case MouseButton.XButton2:
                        break;
                    default:
                        break;
                }
                _ = canvas.CaptureMouse();
            }
            e.Handled = true;
        }

        private void ImageCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            //Canvas canvas = sender as Canvas;
            //System.Windows.Point pt = e.GetPosition(canvas);

            //double _x = pt.X < 0 ? 0 : pt.X > canvas.Width ? canvas.Width : pt.X;
            //double _y = pt.Y < 0 ? 0 : pt.Y > canvas.Height ? canvas.Height : pt.Y;

            //if (canvas.IsMouseCaptured)
            //{
            //    if (Keyboard.IsKeyDown(Key.Space))
            //    {
            //        /// Point from ImageGrid LeftTop Pos
            //        System.Windows.Point pt2 = e.GetPosition(ImageGrid);

            //        ImageScroller.ScrollToHorizontalOffset(TempX - pt2.X);
            //        ImageScroller.ScrollToVerticalOffset(TempY - pt2.Y);
            //    }
            //    else if (AssistRect.Enable && AssistRect.MouseDown)
            //    {
            //        if (e.LeftButton == MouseButtonState.Pressed)
            //        {
            //            if (_x < AssistRect.TempX)
            //            {
            //                AssistRect.X = _x;
            //            }

            //            if (_y < AssistRect.TempY)
            //            {
            //                AssistRect.Y = _y;
            //            }

            //            // RECT.Width = Math.Abs(_x - RECT.TempX);
            //            // RECT.Height = Math.Abs(_y - RECT.TempY);
            //            AssistRect.Width = Math.Abs(_x - AssistRect.TempX);
            //            AssistRect.Height = Math.Abs(_y - AssistRect.TempY);
            //        }
            //        else if (e.MiddleButton == MouseButtonState.Pressed)
            //        {
            //            // RECT.X = RECT.TempX + _x - RECT.OftX;
            //            // RECT.Y = RECT.TempY + _y - RECT.OftY;
            //            double pX = AssistRect.TempX + _x - AssistRect.OftX;
            //            double pY = AssistRect.TempY + _y - AssistRect.OftY;

            //            AssistRect.X = pX < 0 ? 0 : pX + AssistRect.Width > canvas.Width ? canvas.Width - AssistRect.Width : pX;
            //            AssistRect.Y = pY < 0 ? 0 : pY + AssistRect.Height > canvas.Height ? canvas.Height - AssistRect.Height : pY;
            //        }
            //    }
            //}

            //// 變更 座標
            ////AssistRect.PosX = (int)_x;
            ////AssistRect.PosY = (int)_y;

            //// 變更 座標
            ////Indicator.X = (int)_x;
            ////Indicator.Y = (int)_y;

            //Indicator.SetPoint((int)_x, (int)_y);

            ////// 變更 RGB
            ////if (Indicator.Image != null) 
            ////{
            ////    Indicator.SetPoint((int)_x, (int)_y);
            ////}
            //e.Handled = true;
        }

        private void ImageCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Canvas canvas = sender as Canvas;
            canvas.Cursor = Cursors.Arrow;

            if (canvas.IsMouseCaptured)
            {
                if (AssistRect.Enable)
                {
                    //ReleaseMouseCapture();
                    AssistRect.MouseDown = false;
                    AssistRect.ResetTemp();
                }
                //else if (MoveImage)
                //{
                //    MoveImage = false;
                //    TempX = TempY = 0;
                //}
                TempX = TempY = 0;
                canvas.ReleaseMouseCapture();
            }
            e.Handled = true;
        }

        private void ImageCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            Canvas canvas = sender as Canvas;
            canvas.Cursor = Cursors.Arrow;
            if (AssistRect.Enable)
            {
                //ReleaseMouseCapture();
                AssistRect.MouseDown = false;
                AssistRect.ResetTemp();
            }
            else if (MoveImage)
            {
                MoveImage = false;
                TempX = TempY = 0;
            }
        }
        #endregion

        #region Footer Message
        private void ErrPopupBox_Opened(object sender, RoutedEventArgs e)
        {
            MsgInformer.ResetErrorCount();
        }

        private void MessageClearBtn_Click(object sender, RoutedEventArgs e)
        {
            MsgInformer.ClearError();
        }

        private void InfoPopupbox_Opened(object sender, RoutedEventArgs e)
        {
            MsgInformer.ResetInfoCount();
        }

        private void InfoClearBtn_Click(object sender, RoutedEventArgs e)
        {
            MsgInformer.ClearInfo();
        }

        private void PopupBox_Closed(object sender, RoutedEventArgs e)
        {
            Keyboard.ClearFocus();
            _ = TitleGrid.Focus();
        }
        #endregion

        int X = -40;

        /// <summary>
        /// 測試用按鈕
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //ThermometerPanel.Thermometer.Temperature++;
            ////Thermometer.Temperature++;
            //Debug.WriteLine(BFRTestingPanel.BFR.Temp);

            //FooterBarMessage.Add(new FooterBarMessage.Message()
            //{
            //    Code = FooterBarMessage.Message.ErrorCode.A,
            //    Description = $"{DateTime.Now:HH:mm:ss} + {Math.PI:F64}",
            //    MsgType = FooterBarMessage.Message.MessageType.Info
            //});

            //BFRTestingPanel.BFR.AddRecord(123, 456, 10);

            //nitinolBFR.ptQueue1.Enqueue(new OpenCvSharp.Point(10, 10));
            //nitinolBFR.ptQueue1.Enqueue(new OpenCvSharp.Point(20, 12));
            //nitinolBFR.ptQueue1.Enqueue(new OpenCvSharp.Point(30, 16));

            //Debug.WriteLine($"{nitinolBFR.ptQueue1.Average(pt => pt.X)} {nitinolBFR.ptQueue1.Average(pt => pt.Y)}");

            //nitinolBFR.ptQueue1.Clear();
            //nitinolBFR.ptQueue2.Clear();

            BFRTrail.AddRecord(new Random().Next(0, 100), new Random().Next(20, 80), X++);
        }
    }

    public static class ImageSourceConverter
    {
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        private static extern void DeleteObject(IntPtr o);

        #region 效能低下
        /// <summary>
        /// Low performace
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns> (盡量壓到 10ms 左右)
        public static BitmapSource ToBitmapSource(this Bitmap src)
        {
            BitmapSource bs;
            IntPtr ptr = src.GetHbitmap();
            //long imageSize = src.Width * src.Height * 3;
            //GC.AddMemoryPressure(imageSize);
            try
            {
                bs = Imaging.CreateBitmapSourceFromHBitmap(ptr, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(ptr);
                //GC.RemoveMemoryPressure(imageSize);
                src.Dispose();
            }
            return bs;
        }
        /// <summary>
        /// Medium performance
        /// </summary>
        /// <param name="src">Source bitmap</param>
        /// <returns></returns>
        public static ImageSource ToBitmapSource2(this Bitmap src)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                src.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }
        #endregion

        #region Bitmap & Mat convert to ImageSource (High performace)
        /// <summary>
        /// Convert Bitmap to ImageSource
        /// </summary>
        /// <param name="src">source image</param>
        /// <returns>Image source</returns>
        public static ImageSource ToImageSource(this Bitmap src)
        {
            BitmapData bitmapData = src.LockBits(new System.Drawing.Rectangle(0, 0, src.Width, src.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            BitmapSource bitmapSource = BitmapSource.Create(src.Width, src.Height, 96, 96, PixelFormats.Bgr24, null, bitmapData.Scan0, src.Width * src.Height * 3, src.Width * 3);
            src.UnlockBits(bitmapData);
            return bitmapSource;
        }

        /// <summary>
        /// Convert Mat to ImageSource
        /// </summary>
        /// <param name="src">source image</param>
        /// <returns>Image source</returns>
        public static unsafe ImageSource ToImageSource(this Mat src)
        {
            switch (src.Type().Channels)
            {
                case 1:
                    return BitmapSource.Create(src.Width, src.Height, 96, 96, PixelFormats.Gray8, null, (IntPtr)src.DataPointer, src.Width * src.Height, src.Width);
                case 3:
                    return BitmapSource.Create(src.Width, src.Height, 96, 96, PixelFormats.Bgr24, null, (IntPtr)src.DataPointer, src.Width * src.Height * 3, src.Width * 3);
                default:
                    throw new Exception("Unsupported type");
            }
        }
        #endregion
    }
}
