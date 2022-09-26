using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MCAJawIns.Tab
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
        // public ObservableCollection<AssistPoint> AssistPoints { get; set; }
        public AssistPoints AssistPoints { get; set; }
        #endregion

        #region Varibles
        // private static bool MoveImage;
        private static double TempX;
        private static double TempY;
        private ImageSource _imgSrc;
        #endregion

        #region Properties
        /// <summary>
        /// 主視窗物件
        /// </summary>
        public MainWindow MainWindow { get; } = (MainWindow)Application.Current.MainWindow;
        /// <summary>
        /// Image Zoom Ratio
        /// </summary>
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
                else if (v > 300)
                {
                    ImageViewbox.Width = 3 * ImageCanvas.Width;
                }
                else
                {
                    double ratio = value / 100;
                    ImageViewbox.Width = ratio * ImageCanvas.Width;
                }
                OnPropertyChanged(nameof(ZoomRatio));
            }
        }
        /// <summary>
        /// Image Source
        /// </summary>
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

        #region Flags
        /// <summary>
        /// 已載入旗標
        /// </summary>
        private bool loaded;
        #endregion

        public EngineerTab()
        {
            InitializeComponent();

            // Set value when init
            // MainWindow = (MainWindow)Application.Current.MainWindow;
        }

        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            #region Find Resource
            if (Crosshair == null) { Crosshair = TryFindResource(nameof(Crosshair)) as Crosshair; }
            if (AssistRect == null) { AssistRect = TryFindResource(nameof(AssistRect)) as AssistRect; }
            if (Indicator == null) { Indicator = TryFindResource(nameof(Indicator)) as Indicator; }
            if (AssistPoints == null) { AssistPoints = TryFindResource(nameof(AssistPoints)) as AssistPoints; }
            #endregion

            InitializePanelObjects();

            #region Reset ZoomRetio
            ZoomRatio = 100;
            #endregion

            if (!loaded)
            {
                MainWindow.MsgInformer?.AddInfo(MsgInformer.Message.MsgCode.APP, "開發者頁面已載入");
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
            FunctionPanel.EngineerTab = this;
            // DigitalIOPanel.EngineerTab = this;
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
            if (canvas.IsMouseCaptured) { return; }

            // 按下 SPACE
            if (Keyboard.IsKeyDown(Key.Space))
            {
                Point pt2ImageGrid = e.GetPosition(ImageGrid);   // Point to ImageGrid
                Point transformPoint = ImageGrid.TransformToVisual(ImageViewbox).Transform(pt2ImageGrid);    // Add ImageViewbox offset

                TempX = transformPoint.X;
                TempY = transformPoint.Y;

                _ = canvas.CaptureMouse();
            }
            else if (AssistRect.Enable)
            {
                Point pt = e.GetPosition(canvas);

                //AssistRect.MouseDown = true;
                switch (e.ChangedButton)
                {
                    case MouseButton.Left:
                        //canvas.Cursor = Cursors.Cross;
                        AssistRect.IsLeftMouseDown = true; // for changing cursor
                        // // // // // // // // // // //
                        AssistRect.TempX = AssistRect.X = pt.X;
                        AssistRect.TempY = AssistRect.Y = pt.Y;
                        AssistRect.Width = AssistRect.Height = 0;
                        break;
                    case MouseButton.Middle:
                        //canvas.Cursor = Cursors.SizeAll;
                        AssistRect.IsMiddleMouseDown = true;    // for changing cursor
                        // // // // // // // // // // //
                        AssistRect.TempX = AssistRect.X;
                        AssistRect.TempY = AssistRect.Y;
                        AssistRect.OftX = pt.X;
                        AssistRect.OftY = pt.Y;
                        break;
                    case MouseButton.Right:
                        // 重置 RECT
                        AssistRect.X = AssistRect.Y = AssistRect.Width = AssistRect.Height = 0;
                        break;
                    case MouseButton.XButton1:
                    case MouseButton.XButton2:
                    default:
                        break;
                }
                _ = canvas.CaptureMouse();
            }
            else if (AssistPoints.Enable)
            {
                switch (e.ChangedButton)
                {
                    case MouseButton.Left:
                        AssistPoints.IsMouseDown = true;
                        break;
                    case MouseButton.Middle:
                    case MouseButton.Right:
                    case MouseButton.XButton1:
                    case MouseButton.XButton2:
                    default:
                        break;
                }
            }
            e.Handled = true;
        }

        private void ImageCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Canvas canvas = sender as Canvas;
            //canvas.Cursor = Cursors.Arrow;

            if (canvas.IsMouseCaptured)
            {
                if (AssistRect.Enable && AssistRect.IsMouseDown)
                {
                    // ReleaseMouseCapture();
                    // AssistRect.MouseDown = false;
                    AssistRect.ResetTemp();
                    AssistRect.ResetMouse();
                }

                TempX = TempY = 0;
                canvas.ReleaseMouseCapture();
            }
            else
            {
                if (AssistPoints.Enable && AssistPoints.IsMouseDown)
                {
                    // 取點 & 顏色
                    Point pt = e.GetPosition(canvas);
                    Indicator.GetRGB((int)pt.X, (int)pt.Y, out byte R, out byte G, out byte B);

                    AssistPoints.Source.Add(new AssistPoint(pt.X, pt.Y, R, G, B));

                    #region 顏色生成 (待刪除)
                    //if (R < G && R < B)
                    //{
                    //    //SolidColorBrush brush = new SolidColorBrush(Color.FromRgb((byte)255, (byte)(255 - G), (byte)(255 - B)));
                    //    AssistPoints.Source.Add(new AssistPoint(pt.X, pt.Y, 255, (byte)(255 - G), (byte)(255 - B)));
                    //}
                    //else if (G < R && G < B)
                    //{
                    //    //SolidColorBrush brush = new SolidColorBrush(Color.FromRgb((byte)(255 - R), 255, (byte)(255 - B)));
                    //    AssistPoints.Source.Add(new AssistPoint(pt.X, pt.Y, (byte)(255 - R), 255, (byte)(255 - B)));
                    //}
                    //else if (B < R && B < G)
                    //{
                    //    //SolidColorBrush brush = new SolidColorBrush(Color.FromRgb((byte)(255 - R), (byte)(255 - G), 255));
                    //    AssistPoints.Source.Add(new AssistPoint(pt.X, pt.Y, (byte)(255 - R), (byte)(255 - G), 255));
                    //}
                    //else
                    //{
                    //    //SolidColorBrush brush = new SolidColorBrush(Color.FromRgb((byte)(255 - R), (byte)(255 - G), (byte)(255 - B)));
                    //    AssistPoints.Source.Add(new AssistPoint(pt.X, pt.Y, (byte)(255 - R), (byte)(255 - G), (byte)(255 - B)));
                    //}
                    #endregion

                    AssistPoints.ResetMouse();
                }
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
                else if (AssistRect.Enable && AssistRect.IsMouseDown)
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

            Indicator.SetPoint((int)_x, (int)_y);

            e.Handled = true;
        }

        private void ImageCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            //Canvas canvas = sender as Canvas;
            //canvas.Cursor = Cursors.Arrow;
            if (AssistRect.Enable)
            {
                //ReleaseMouseCapture();
                //AssistRect.MouseDown = false;
                AssistRect.ResetTemp();
                AssistRect.ResetMouse();
            }
            else if (AssistPoints.Enable)
            {
                AssistPoints.ResetMouse();
            }
#if false
            else if (MoveImage)
            {
                MoveImage = false;
                TempX = TempY = 0;
            } 
#endif
        }

        #region Property Changed Event
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
