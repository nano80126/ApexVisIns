using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
using Basler.Pylon;
using Basler;
using System.Threading;
using System.Windows.Threading;
using System.Runtime.InteropServices;

namespace LockPlate.content
{
    /// <summary>
    /// Programming.xaml 的互動邏輯
    /// </summary>m
    public partial class EngineerTab : StackPanel, INotifyPropertyChanged
    {
        #region Resources
        public Crosshair Crosshair { set; get; }
        public AssistRect AssistRect { set; get; }
        public Indicator Indicator { set; get; }
        #endregion

        #region Varibles
        private static bool MoveImage;
        private static double TempX;
        private static double TempY;
        private ImageSource _imgSrc;
        #endregion

        #region Properties
        /// <summary>
        /// 主視窗物件
        /// </summary>
        public MainWindow MainWindow { get; set; }
        /// <summary>
        /// Informer 物件
        /// </summary>
        //public MsgInformer MsgInformer { get; set; }
        #endregion

        #region Flags
        /// <summary>
        /// 已載入旗標
        /// </summary>
        private bool loaded;
        #endregion

        public EngineerTab()
        {
            InitializeComponent();

            MainWindow = (MainWindow)Application.Current.MainWindow;
        }

        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            #region Find Resource
            if (Crosshair == null) { Crosshair = TryFindResource(nameof(Crosshair)) as Crosshair; }
            if (AssistRect == null) { AssistRect = TryFindResource(nameof(AssistRect)) as AssistRect; }
            if (Indicator == null) { Indicator = TryFindResource(nameof(Indicator)) as Indicator; }
            #endregion

            InitializePanelObjects();

            #region Reset ZoomRetio
            ZoomRatio = 100;
            #endregion

            if (!loaded)
            {
                MainWindow.MsgInformer?.AddInfo(MsgInformer.Message.MsgCode.APP, "測試頁面已載入");
                loaded = true;
            }

            //OpenCvSharp.Mat m = new OpenCvSharp.Mat(@"C:\Users\nano80126\Pictures\EKs6vfSUYAAyeRc.jpg");
            //MainWindow.BaslerCam.Width = m.Width;
            //MainWindow.BaslerCam.Height = m.Height;
            //MainWindow.BaslerCam.PropertyChange();

            //Indicator.Image = m;
            //Indicator.ImageSource = m.ToImageSource();
        }

        private void StackPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine("Engineer Tab Unload");
        }

        /// <summary>
        /// 綁定 Panel 物件
        /// </summary>
        private void InitializePanelObjects()
        {
            ConfigPanel.EngineerTab = this;
            LightPanel.EngineerTab = this;
            DigitalIOPanel.EngineerTab = this;
        }

        /// <summary>
        /// Preview Mouse Scroll Event 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImageScroller_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer viewer = sender as ScrollViewer;
            //System.Windows.Point pt = e.GetPosition(ImageCanvas);

            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (e.Delta > 0)
                {
                    ZoomRatio += 5;
                }
                else
                {
                    ZoomRatio -= 5;
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
                //canvas.Cursor = Cursors.Arrow;
                //MoveImage = true;
                Point pt2ImageGrid = e.GetPosition(ImageGrid);   // Point to ImageGrid
                Point transformPoint = ImageGrid.TransformToVisual(ImageViewbox).Transform(pt2ImageGrid);    // Add ImageViewbox offset

                TempX = transformPoint.X;
                TempY = transformPoint.Y;

                _ = canvas.CaptureMouse();
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

        private void ImageCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            Canvas canvas = sender as Canvas;
            Point pt = e.GetPosition(canvas);

            double _x = pt.X < 0 ? 0 : pt.X > canvas.Width ? canvas.Width : pt.X;
            double _y = pt.Y < 0 ? 0 : pt.Y > canvas.Height ? canvas.Height : pt.Y;

            if (canvas.IsMouseCaptured)
            {
                if (Keyboard.IsKeyDown(Key.Space))
                {
                    /// Point from ImageGrid LeftTop Pos
                    Point pt2 = e.GetPosition(ImageGrid);

                    ImageScroller.ScrollToHorizontalOffset(TempX - pt2.X);
                    ImageScroller.ScrollToVerticalOffset(TempY - pt2.Y);
                }
                else if (AssistRect.Enable && AssistRect.MouseDown)
                {
                    if (e.LeftButton == MouseButtonState.Pressed)
                    {
                        if (_x < AssistRect.TempX)
                        {
                            AssistRect.X = _x;
                        }

                        if (_y < AssistRect.TempY)
                        {
                            AssistRect.Y = _y;
                        }

                        AssistRect.Width = Math.Abs(_x - AssistRect.TempX);
                        AssistRect.Height = Math.Abs(_y - AssistRect.TempY);
                    }
                    else if (e.MiddleButton == MouseButtonState.Pressed)
                    {
                        double pX = AssistRect.TempX + _x - AssistRect.OftX;
                        double pY = AssistRect.TempY + _y - AssistRect.OftY;

                        AssistRect.X = pX < 0 ? 0 : pX + AssistRect.Width > canvas.Width ? canvas.Width - AssistRect.Width : pX;
                        AssistRect.Y = pY < 0 ? 0 : pY + AssistRect.Height > canvas.Height ? canvas.Height - AssistRect.Height : pY;
                    }
                }
            }

            // 變更 座標
            //AssistRect.PosX = (int)_x;
            //AssistRect.PosY = (int)_y;

            // 變更 座標
            //Indicator.X = (int)_x;
            //Indicator.Y = (int)_y;

            Indicator.SetPoint((int)_x, (int)_y);

            //// 變更 RGB
            //if (Indicator.Image != null) 
            //{
            //    Indicator.SetPoint((int)_x, (int)_y);
            //}
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

        #region Properties
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public double ZoomRatio
        {
            get => ImageViewbox == null ? 0 : ImageViewbox.Width / ImageCanvas.Width * 100;
            set
            {
                int v = (int)Math.Floor(value);

                if (20 > v)
                {
                    ImageViewbox.Width = 0.2 * ImageCanvas.Width;
                }
                else if (v > 200)
                {
                    ImageViewbox.Width = 2 * ImageCanvas.Width;
                }
                else
                {
                    double ratio = value / 100;
                    ImageViewbox.Width = ratio * ImageCanvas.Width;
                }
                OnPropertyChanged(nameof(ZoomRatio));
            }
        }

        public ImageSource ImageSource
        {
            get => _imgSrc;
            set
            {
                _imgSrc = value;
                OnPropertyChanged(nameof(ImageSource));
            }
        }
        #endregion


        #region 測試用區塊
        /// <summary>
        /// 前進
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            bool status = MainWindow.IOController.ReadDIBitValue(0, 5);

            if (status)
            {
                MainWindow.IOController.WriteDOBit(0, 0, true);
                SpinWait.SpinUntil(() => false, 200);
                MainWindow.IOController.WriteDOBit(0, 0, false);
            }
        }


        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            bool status = MainWindow.IOController.ReadDIBitValue(0, 0);

            Debug.WriteLine($"{status}");
            if (status)
            {
                MainWindow.IOController.WriteDOBit(0, 1, true);
                SpinWait.SpinUntil(() => false, 200);
                MainWindow.IOController.WriteDOBit(0, 1, false);
            }
        }

        private void Button3_Click(object sender, RoutedEventArgs e)
        {
            bool status = MainWindow.IOController.ReadDIBitValue(0, 1);

            Debug.WriteLine($"{status}");
            if (status)
            {
                MainWindow.IOController.WriteDOBit(0, 2, true);
                SpinWait.SpinUntil(() => false, 200);
                MainWindow.IOController.WriteDOBit(0, 2, false);
            }
        }

        private void Button4_Click(object sender, RoutedEventArgs e)
        {
            bool status = MainWindow.IOController.ReadDIBitValue(0, 4);

            Debug.WriteLine($"{status}");
            if (status)
            {
                MainWindow.IOController.WriteDOBit(0, 5, true);
                SpinWait.SpinUntil(() => false, 200);
                MainWindow.IOController.WriteDOBit(0, 5, false);
            }
        }

        private void Button5_Click(object sender, RoutedEventArgs e)
        {
            bool status = MainWindow.IOController.ReadDIBitValue(0, 3);

            Debug.WriteLine($"{status}");
            if (status)
            {
                MainWindow.IOController.WriteDOBit(0, 4, true);
                SpinWait.SpinUntil(() => false, 200);
                MainWindow.IOController.WriteDOBit(0, 4, false);
            }
        }

        private void Button6_Click(object sender, RoutedEventArgs e)
        {
            bool status = MainWindow.IOController.ReadDIBitValue(0, 2);

            Debug.WriteLine($"{status}");
            if (status)
            {
                MainWindow.IOController.WriteDOBit(0, 3, true);
                SpinWait.SpinUntil(() => false, 200);
                MainWindow.IOController.WriteDOBit(0, 3, false);
            }
        }

        private void Button7_Click(object sender, RoutedEventArgs e)
        {
            DateTime t1 = DateTime.Now;

            (sender as Button).IsEnabled = false;

            bool status = MainWindow.IOController.ReadDIBitValue(0, 5);

            if (status)
            {
                MainWindow.IOController.WriteDOBit(0, 0, true);
                SpinWait.SpinUntil(() => false, 200);
                MainWindow.IOController.WriteDOBit(0, 0, false);
            }
            else
            {
                (sender as Button).IsEnabled = true;
                return;
            }

            status = SpinWait.SpinUntil(() => MainWindow.IOController.ReadDIBitValue(0, 0), 1000);
            SpinWait.SpinUntil(() => false, 200);

            Debug.WriteLine($"status: {status}");
            if (status)
            {
                MainWindow.IOController.WriteDOBit(0, 1, true);
                SpinWait.SpinUntil(() => false, 200);
                MainWindow.IOController.WriteDOBit(0, 1, false);
            }
            else
            {
                (sender as Button).IsEnabled = true;
                return;
            }

            status = SpinWait.SpinUntil(() => MainWindow.IOController.ReadDIBitValue(0, 1), 1000);
            SpinWait.SpinUntil(() => false, 200);

            Debug.WriteLine($"{status}");
            if (status)
            {
                MainWindow.IOController.WriteDOBit(0, 2, true);
                SpinWait.SpinUntil(() => false, 200);
                MainWindow.IOController.WriteDOBit(0, 2, false);
            }
            else
            {
                (sender as Button).IsEnabled = true;
                return;
            }

            status = SpinWait.SpinUntil(() => MainWindow.IOController.ReadDIBitValue(0, 2), 1000);
            SpinWait.SpinUntil(() => false, 200);

            Debug.WriteLine($"{status}");
            if (status)
            {
                MainWindow.IOController.WriteDOBit(0, 3, true);
                SpinWait.SpinUntil(() => false, 200);
                MainWindow.IOController.WriteDOBit(0, 3, false);
            }
            else
            {
                (sender as Button).IsEnabled = true;
                return;
            }

            status = SpinWait.SpinUntil(() => MainWindow.IOController.ReadDIBitValue(0, 3), 1000);
            SpinWait.SpinUntil(() => false, 200);

            Debug.WriteLine($"{status}");
            if (status)
            {
                MainWindow.IOController.WriteDOBit(0, 4, true);
                SpinWait.SpinUntil(() => false, 200);
                MainWindow.IOController.WriteDOBit(0, 4, false);
            }
            else
            {
                (sender as Button).IsEnabled = true;
                return;
            }

            status = SpinWait.SpinUntil(() => MainWindow.IOController.ReadDIBitValue(0, 4), 1000);
            SpinWait.SpinUntil(() => false, 200);

            Debug.WriteLine($"{status}");
            if (status)
            {
                MainWindow.IOController.WriteDOBit(0, 5, true);
                SpinWait.SpinUntil(() => false, 200);
                MainWindow.IOController.WriteDOBit(0, 5, false);
            }
            else
            {
                (sender as Button).IsEnabled = true;
                return;
            }

            status = SpinWait.SpinUntil(() => MainWindow.IOController.ReadDIBitValue(0, 5), 1000);

            (sender as Button).IsEnabled = true;

            Debug.WriteLine($"{(DateTime.Now - t1).TotalMilliseconds}");
        }
        #endregion
    }
}
