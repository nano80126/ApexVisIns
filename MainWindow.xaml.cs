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
using System.IO.Ports;
using MaterialDesignThemes.Wpf;
using MaterialDesignThemes;
using System.Windows.Controls.Primitives;


namespace ApexVisIns
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        #region Long life worker
        /// <summary>
        /// Camera 列舉器
        /// 綁在 MainWindow
        /// </summary>
        public CameraEnumer CameraEnumer;
        // public Thermometer Thermometer;
        #endregion

        #region Cameras
        /// <summary>
        ///相機物件 (工程師頁面使用)
        /// </summary>
        public static BaslerCam BaslerCam { get; set; }
        /// <summary>
        /// 相機陣列 (上限正式使用)
        /// </summary>
        public static BaslerCam[] BaslerCams { get; set; }
        /// <summary>
        /// 已儲存的相機組態
        /// </summary>
        public static ObservableCollection<DeviceConfig> DeviceConfigs { get; set; }
        #endregion

        #region Light Controller
        /// <summary>
        /// Com Port 列舉器，
        /// 綁在 MainWindow
        /// </summary>
        public LightEnumer LightEnumer { get; set; }

        /// <summary>
        /// 光源控制器
        /// </summary>
        public static LightController LightController { get; set; }

        /// <summary>
        /// 光源控制器陣列
        /// </summary>
        public static LightController[] LightCtrls { get; set; }
        #endregion

        #region I/O Controller
        public static IOController IOController { get; set; }
        #endregion

        #region Devices
        /// <summary>
        /// 相機裝置列表
        /// </summary>
        // public static ObservableCollection<DeviceConfig> DeviceConfigs { get; set; }
        #endregion

        #region EtherCAT Motion
        /// <summary>
        /// Motion Device 列舉器，綁在 MainWindow
        /// </summary>
        public MotionEnumer MotionEnumer { get; set; }

        public static ServoMotion ServoMotion { get; set; }
        #endregion

        #region Major
        public static ApexDefect ApexDefect { get; set; }
        #endregion

        #region Resources
        // public Crosshair Crosshair;         // 待刪
        // public AssistRect AssistRect;       // 待刪
        // public AssistPoint[] AssistPoints;  // 待刪
        // public Indicator Indicator;         // 待刪
        /// <summary>
        /// 訊息通知器
        /// </summary>
        public static MsgInformer MsgInformer { get; set; }
        #endregion

        #region Varibles
        /// <summary>
        /// 影像處理中
        /// </summary>
        public static bool IsProcessing { get; set; }

        private readonly PixelDataConverter pxConverter = new()
        {
            OutputPixelFormat = PixelType.Mono8
        };
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            InitializePanels();

            //GetSystemPath();
        }

        /// <summary>
        /// 初始化 Panel's MainWindow
        /// </summary>
        private void InitializePanels()
        {
            #region Set Tabs's and Panels's MainWindows ref
            // MainTab
            MainTab.MainWindow = this;
            // Config Tab
            DeviceTab.MainWindow = this;
            // Engineer Tab
            EngineerTab.MainWindow = this;
            EngineerTab.ConfigPanel.MainWindow = this;
            EngineerTab.LightPanel.MainWindow = this;
            EngineerTab.DigitalIOPanel.MainWindow = this;
            #endregion
        }

        /// <summary>
        /// 主視窗載入，
        /// 資源尋找等初始化在此
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            #region Find Resource
            // Crosshair = FindResource(nameof(Crosshair)) as Crosshair;
            // AssistRect = FindResource(nameof(AssistRect)) as AssistRect;
            // Indicator = FindResource(nameof(Indicator)) as Indicator;
            // AssistPoints = FindResource(nameof(AssistPoints)) as AssistPoint[];

            MsgInformer = FindResource(nameof(ApexVisIns.MsgInformer)) as MsgInformer;
            MsgInformer.EnableProgressBar();
            MsgInformer.EnableCollectionBinding();
            #endregion

            #region Cameras
            CameraEnumer = TryFindResource(nameof(CameraEnumer)) as CameraEnumer;
            CameraEnumer?.WorkerStart();

#if DEBUG
            BaslerCam = FindResource(nameof(BaslerCam)) as BaslerCam;
#endif
            BaslerCams = FindResource(nameof(BaslerCams)) as BaslerCam[];
            #endregion

            #region Light Controller
            LightEnumer = TryFindResource(nameof(LightEnumer)) as LightEnumer;
            LightEnumer?.WorkerStart();

#if DEBUG
            LightController = FindResource(nameof(LightController)) as LightController;
#endif
            LightCtrls = FindResource(nameof(LightCtrls)) as LightController[];
            #endregion

            #region IO Controller
            IOController = FindResource(nameof(IOController)) as IOController;
            #endregion

            #region Device Configs
            DeviceConfigs = FindResource(nameof(DeviceConfigs)) as ObservableCollection<DeviceConfig>;
            #endregion

            #region EtherCAT Motion
            // Resource 一樣尋找
            MotionEnumer = FindResource(nameof(MotionEnumer)) as MotionEnumer;
            ServoMotion = FindResource(nameof(ServoMotion)) as ServoMotion;

            if (MotionEnumer.CheckDllVersion())
            {
                MotionEnumer?.WorkerStart();
                ServoMotion.EnableCollectionBinding();
            }
            else
            {
                MotionEnumer.Interrupt();
                MsgInformer.AddWarning(MsgInformer.Message.MsgCode.MOTION, "MOTION 控制驅動未安裝或版本不符");
            }
            #endregion

            #region ApexDefect
            ApexDefect = FindResource(nameof(ApexDefect)) as ApexDefect;
            #endregion

            // 載入後, focus 視窗

            _ = Focus();
        }

        /// <summary>
        /// 主視窗關閉，
        /// 停止 LongLifeWorker
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MsgInformer.DisableCollectionBinding();
            MsgInformer.DisposeProgressTask();

            CameraEnumer.WorkerEnd();
            LightEnumer.WorkerEnd();

            IOController.Dispose();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            #region 保留
            //MsgInformer.CollectionDebinding();
            //MsgInformer.DisposeProgressTask();
            //CameraEnumer?.WorkerEnd();
            //LightEnumer?.WorkerEnd(); 
            #endregion
        }

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

        /// <summary>
        /// 讓 TextBox 失去 Focus用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Panels_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
            _ = TitleGrid.Focus();
        }

        #region User Login
        private void LoginBtn_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (password.Password == Password)
                {
                    LoginFlag = true;
                    passwordHint.Text = string.Empty;
                    passwordHint.Visibility = Visibility.Hidden;
                    LoginDialog.IsOpen = false;
                }
                else
                {
                    passwordHint.Text = "密碼錯誤";
                    passwordHint.Visibility = Visibility.Visible;
                    e.Handled = true;
                }
            }
        }

        private void DialogHost_DialogClosing(object sender, DialogClosingEventArgs eventArgs)
        {
            password.Password = string.Empty;
            passwordHint.Text = string.Empty;
            passwordHint.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// 開啟 Login Panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserLogin_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // 若已登入，不做任何反應
            if (LoginFlag)
            {
                e.Handled = true;
            }
        }
        #endregion

        private void LoginDialog_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                LoginBtn.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                {
                    RoutedEvent = Mouse.PreviewMouseDownEvent,
                    Source = this
                });
            }
            else if (e.Key == Key.Escape)
            {
                (sender as DialogHost).IsOpen = false;
            }
        }
    }

    /// <summary>
    /// ImageSource 轉換器
    /// </summary>
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


/// <summary>
/// 自訂義 property 用
/// </summary>
namespace ApexVisIns.CustomProperty
{
    public class StatusHelper : DependencyObject
    {
        public static readonly DependencyProperty ConnectedProperty = DependencyProperty.RegisterAttached(
          "Connected", typeof(bool), typeof(StatusHelper), new PropertyMetadata(false));

        public static void SetConnected(DependencyObject target, bool value)
        {
            target.SetValue(ConnectedProperty, value);
        }

        public static bool GetConnected(DependencyObject target)
        {
            return (bool)target.GetValue(ConnectedProperty);
        }
    }

    // 確認為什麼不能兩個
    public class ProcedureBlock : DependencyObject
    {
        public static readonly DependencyProperty BlockNameProperty = DependencyProperty.RegisterAttached(
          "BlockName", typeof(string), typeof(ProcedureBlock), new PropertyMetadata(string.Empty));

        public static void SetBlockName(DependencyObject target, string value)
        {
            target.SetValue(BlockNameProperty, value);
        }

        public static string GetBlockName(DependencyObject target)
        {
            return (string)target.GetValue(BlockNameProperty);
        }
    }
}

