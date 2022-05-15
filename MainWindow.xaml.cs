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
using System.ComponentModel;
using System.Windows.Media.Animation;
using ApexVisIns.Product;
using ApexVisIns.content;

namespace ApexVisIns
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        #region Cameras
        /// <summary>
        /// Camera 列舉器
        /// 綁在 MainWindow
        /// </summary>
        public CameraEnumer CameraEnumer { get; set; }

        /// <summary>
        ///相機物件 (工程師頁面使用)
        /// </summary>
        public static BaslerCam BaslerCam { get; set; }

        /// <summary>
        /// 相機陣列 (上線正式使用)
        /// </summary>
        public static BaslerCam[] BaslerCams { get; set; }
        #endregion

        #region Serial Port Enumerator
        /// <summary>
        /// Com Port 列舉器，
        /// </summary>
        public SerialEnumer SerialEnumer { get; set; }
        #endregion


        #region Light Controller
        /// <summary>
        /// Com Port 列舉器，
        /// 綁在 MainWindow
        /// </summary>
        [Obsolete("這邊要移除")]
        public LightEnumer LightEnumer { get; set; }

        //public static LightSerial LightCtrl { get; set; }
        /// <summary>
        /// 光源控制器
        /// </summary>
        public static LightSerial[] LightCtrls { get; set; }
        #endregion

        #region I/O Controller
        public static IOController IOController { get; set; }

        public static ModbusTCPIO ModbusTCPIO { get; set; }

        public IOWindow IOWindow { get; set; }

        [Obsolete("待確認")]
        public Thread IOThread { get; set; }
        #endregion

        #region Devices
        /// <summary>
        /// 相機裝置列表
        /// </summary>
        // public static ObservableCollection<DeviceConfig> DeviceConfigs { get; set; }
        #endregion

        #region EtherCAT Motion
        [Obsolete("Not used in MCA_Jaw")]
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

        public enum InitModes
        {
            AUTO = 0,
            WARM = 1,
            EDIT = 2
        }

        /// <summary>
        /// 初始化模式
        /// </summary>
        public InitModes InitMode { get; set; } = InitModes.AUTO;
        #endregion

        #region Tabs
        private MCAJaw MCAJaw { get; set; }
        private CameraTab CameraTab { get; set; }
        private MotionTab MotionTab { get; set; }
        private DatabaseTab DatabaseTab { get; set; }
        private EngineerTab EngineerTab { get; set; }
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            //InitializePanels();

            LoadTabItems();
        }

        /// <summary>
        /// 初始化 Panel's MainWindow
        /// </summary>
        [Obsolete]
        private void InitializePanels()
        {
            #region Set Tabs's and Panels's MainWindows ref
            // MainTab
            //MainTab.MainWindow = this;
            // MCA Jaw Tab
#if true
            //JawTab.MainWindow = this;
            // Device Tab
            //DeviceTab.MainWindow = this;
            // Motion Tab
            //MotionTab.MainWindow = this;
            // Engineer Tab
            //EngineerTab.MainWindow = this;
            //EngineerTab.ConfigPanel.MainWindow = this;
            //EngineerTab.LightPanel.MainWindow = this;
            //EngineerTab.DigitalIOPanel.MainWindow = this;
#endif
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
            MsgInformer = FindResource(nameof(ApexVisIns.MsgInformer)) as MsgInformer;
            MsgInformer.EnableCollectionBinding();
            MsgInformer.ProgressValueChanged += MsgInformer_ProgressValueChanged;   // 綁定 ProgressBar Value Changed 事件
            MsgInformer.EnableProgressBar();
            #endregion

            #region Cameras
            CameraEnumer = TryFindResource(nameof(CameraEnumer)) as CameraEnumer;
            CameraEnumer?.WorkerStart();

#if DEBUG
            BaslerCam = FindResource(nameof(BaslerCam)) as BaslerCam;
#endif
            BaslerCams = FindResource(nameof(BaslerCams)) as BaslerCam[];
            #endregion

            #region Serial Port
            SerialEnumer = FindResource(nameof(SerialEnumer)) as SerialEnumer;
            SerialEnumer.WorkerStart();
            #endregion

            #region Light Controller
            //LightEnumer = TryFindResource(nameof(LightEnumer)) as LightEnumer;
            //LightEnumer?.WorkerStart();

#if DEBUG
            //LightController = FindResource(nameof(LightController)) as LightController;
#endif
            // LightCtrls_old = FindResource(nameof(LightCtrls_old)) as LightController[]; // depricated
            LightCtrls = FindResource(nameof(LightCtrls)) as LightSerial[];
            #endregion

            #region EtherCAT Motion
            // MotionEnumer = FindResource(nameof(MotionEnumer)) as MotionEnumer;
            ServoMotion = FindResource(nameof(ServoMotion)) as ServoMotion;
            ServoMotion.EnableCollectionBinding();  // 啟用 Collection Binding，避免跨執行緒錯誤
            // ServoMotion.ListAvailableDevices(true);
            #endregion

            #region IO Controller
            // PCI Card
            IOController = FindResource(nameof(IOController)) as IOController;
            IOController.EnableCollectionBinding(); // 啟用 Collection Binding，避免跨執行緒錯誤
            // IO Module (WISE-4050/LAN)
            ModbusTCPIO = FindResource(nameof(ModbusTCPIO)) as ModbusTCPIO;
            #endregion

            #region ApexDefect 上線檢驗用
            // Main Tab 使用
            ApexDefect = FindResource(nameof(ApexDefect)) as ApexDefect;
            #endregion

            // 載入後, focus 視窗
            _ = Focus();

            // 若不為 DebugMode，設為全螢幕
            WindowState = !DebugMode ? WindowState.Maximized : WindowState.Normal;

            // SpinWait.SpinUntil(() => false, 1000);

            // CreateIOWindow();

            // AppTabControl.Items[]


#if false
            bool once = false;
            foreach (TabItem item in AppTabControl.Items)
            {
                Debug.WriteLine(item.Header);
                Debug.WriteLine(item.Content);
                Debug.WriteLine(item.Content == null);

                if (item.Content == null && !once)
                {
                    item.Content = new content.DeviceTab()
                    {
                        Name = "DeviceTab",
                        Focusable = true,
                        FocusVisualStyle = null
                    };

                    once = true;
                }
            } 
#endif

#if false
            //Dictionary<int, OpenCvSharp.Rect> rrr = new Dictionary<int, OpenCvSharp.Rect>() {
            //    { 120, new OpenCvSharp.Rect(10,10,10,10)},
            //    { 240, new OpenCvSharp.Rect(20,10,10,10)},
            //    { 360, new OpenCvSharp.Rect(30,10,10,10)},
            //};

            //foreach (int item in rrr.Keys)
            //{
            //    Debug.WriteLine($"{item}, {rrr[item]}");
            //}  
#endif
            #region 開啟 Mode Dialog
            ModeWindow modeWindow = new() { Owner = this };
            if (modeWindow.ShowDialog() == true) { Debug.WriteLine($"Init Mode: {InitMode}"); }
            #endregion
        }

        /// <summary>
        /// 主視窗關閉，
        /// 停止 LongLifeWorker
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            CameraEnumer.WorkerEnd();   // 停止 Camera Enumerator
            CameraEnumer.Dispose();
            SerialEnumer.WorkerEnd();   // 停止 Serial Enumerator 
            SerialEnumer.Dispose();
            // LightEnumer.WorkerEnd(); // deprecated class

            ServoMotion.Dispose();      // 處置 ServoMotion
            IOController.Dispose();     // 處置 IOController

            MsgInformer.DisableCollectionBinding();
            MsgInformer.DisposeProgressTask();

            if (IOWindow != null)
            {
                IOWindow.Close();
            }
        }

        /// <summary>
        /// 主視窗關閉
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender, EventArgs e)
        {



        }


        /// <summary>
        /// 載入 TabItems
        /// </summary>
        private void LoadTabItems()
        {
            for (int i = 0; i < AppTabControl.Items.Count; i++)
            {
                TabItem tabItem = (TabItem)AppTabControl.Items[i];
                //Debug.WriteLine($"{tabItem.Header} {(tabItem.Header as PackIcon).Kind}");
                if (tabItem.Content != null) { continue; }

                switch (i)
                {
                    case 0:
                        MCAJaw = new MCAJaw()
                        {
                            Name = "MCAJawTab",
                            Focusable = true,
                            FocusVisualStyle = null
                        };
                        tabItem.Content = MCAJaw;
                        break;
                    case 1:
                        CameraTab = new CameraTab()
                        {
                            Name = "DeviceTab",
                            Focusable = true,
                            FocusVisualStyle = null
                        };
                        tabItem.Content = CameraTab;
                        break;
                    case 2:
                        MotionTab = new MotionTab()
                        {
                            Name = "MotionTab",
                            Focusable = true,
                            FocusVisualStyle = null
                        };
                        tabItem.Content = MotionTab;
                        break;
                    case 3:
                        DatabaseTab = new DatabaseTab()
                        {
                            Name = "DatabaseTab",
                            Focusable = true,
                            FocusVisualStyle = null
                        };
                        tabItem.Content = DatabaseTab;
                        break;
                    case 4:
                        EngineerTab = new EngineerTab()
                        {
                            Name = "EngineerTab",
                            Focusable = true,
                            FocusVisualStyle = null
                        };
                        tabItem.Content = EngineerTab;
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// 開啟 IO Window
        /// </summary>
        public void CreateIOWindow()
        {
            //IOThread = new(() =>
            //{

            if (IOWindow == null)
            {
                IOWindow = new IOWindow(this);
            }
            IOWindow?.Show();
            //    IOWindow.Closed += (sender2, e2) => IOWindow.Dispatcher.InvokeShutdown();
            //    System.Windows.Threading.Dispatcher.Run();
            //});
            //IOThread.SetApartmentState(ApartmentState.STA);
            //IOThread.Start();
        }

        /// <summary>
        /// 開啟 IO Window
        /// </summary>
        public void OpenIOWindow()
        {
            //IOThread.SetApartmentState(ApartmentState.STA);
            //IOThread.Start();
        }

        /// <summary>
        /// 顯示 IO 視窗
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowIOWindow_Click(object sender, RoutedEventArgs e)
        {
            IOWindow.Show();
            IOWindow.Activate();
        }

        /// <summary>
        /// 程式完整關閉 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AppFullClose_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"-----------------------------------------------------");

            // 關閉所有相機
            foreach (BaslerCam cam in BaslerCams)
            {
                if (cam.IsOpen)
                {
                    // 若 Grabber 開啟中，關閉 Grabber
                    if (cam.IsGrabbing)
                    {
                        Basler_StopStreamGrabber(cam);
                    }
                    cam.Close();
                }
            }

            // Servo Off & 關閉 Motion 控制 
            if (ServoMotion.DeviceOpened)
            {
                ServoMotion.SetAllServoOff();
                ServoMotion.DisableAllTimer();
                ServoMotion.CloseDevice();
            }

            // 重製 & 關閉所有光源
            // foreach (LightController ctrl in LightCtrls_old)
            foreach (LightSerial ctrl in LightCtrls)
            {
                if (ctrl.IsComOpen)
                {
                    _ = ctrl.TryResetAllChannel(out _);
                    ctrl.ComClose();
                }
            }

            _ = SpinWait.SpinUntil(() => BaslerCams.All(cam => !cam.IsConnected), 3000);
            _ = SpinWait.SpinUntil(() => !ServoMotion.DeviceOpened, 3000);
            // SpinWait.SpinUntil(() => LightCtrls_old.All(ctrl => !ctrl.IsComOpen), 3000);
            _ = SpinWait.SpinUntil(() => LightCtrls.All(ctrl => !ctrl.IsComOpen), 3000);

            Close();
        }

        #region Footer Progress & Message
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

        /// <summary>
        /// 進度表更新 (動畫)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MsgInformer_ProgressValueChanged(object sender, MsgInformer.ProgressValueChangedEventArgs e)
        {
            Debug.WriteLine($"{e.OldValue} {e.NewValue} {e.Duration}");
            Dispatcher.Invoke(() =>
            {
                //MainProgress.Value = e.OldValue;
                //MainProgress.SetPercent(e.NewValue, e.Duration);
                MainProgress.SetPercent(e.OldValue, e.NewValue, e.Duration);
            });
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
                if (LoginPassword.Password == Password)
                {
                    LoginFlag = true;
                    IOWindow?.PropertyChange(nameof(LoginFlag));
                    LoginPasswordHint.Text = string.Empty;
                    LoginPasswordHint.Visibility = Visibility.Hidden;
                    LoginDialog.IsOpen = false;
                }
                else
                {
                    LoginPasswordHint.Text = "密碼錯誤";
                    LoginPasswordHint.Visibility = Visibility.Visible;
                    e.Handled = true;
                }
            }
        }

        private void DialogHost_DialogOpened(object sender, DialogOpenedEventArgs eventArgs)
        {
            // LoginPassword.Focus();
        }

        private void DialogHost_DialogClosing(object sender, DialogClosingEventArgs eventArgs)
        {
            LoginPassword.Password = string.Empty;
            LoginPasswordHint.Text = string.Empty;
            LoginPasswordHint.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// 開啟 Login Panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserLogin_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DebugMode)
            {
                LoginFlag = true;
                if (IOWindow != null) { IOWindow.PropertyChange(nameof(LoginFlag)); }
                e.Handled = true;
            }
            else
            {
                // 若已登入，不做任何反應
                if (LoginFlag)
                {
                    e.Handled = true;
                }
            }
        }

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
        #endregion

        #region 公用物件操作 (Apex 使用)
        /// <summary>
        /// 開始窗戶、耳朵相機連續拍攝
        /// </summary>
        public void StartWindowEarCameraContinous()
        {
            // 窗戶
            if (!BaslerCams[0].IsContinuousGrabbing && !BaslerCams[0].IsGrabberOpened)
            {
                BaslerCams[0].Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.Off);
                BaslerCams[0].Camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByStreamGrabber);

                BaslerCams[0].IsContinuousGrabbing = true;
            }

            // 耳朵
            if (!BaslerCams[1].IsContinuousGrabbing && !BaslerCams[1].IsGrabberOpened)
            {
                BaslerCams[1].Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.Off);
                BaslerCams[1].Camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByStreamGrabber);

                BaslerCams[1].IsContinuousGrabbing = true;
            }
        }
        /// <summary>
        /// 停止窗戶、耳朵相機連續拍攝
        /// </summary>
        public void StopWindowEarCameraContinous()
        {
            if (BaslerCams[0].Camera.StreamGrabber.IsGrabbing && BaslerCams[0].IsContinuousGrabbing)
            {
                BaslerCams[0].Camera.StreamGrabber.Stop();
                BaslerCams[0].Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.On);

                BaslerCams[0].IsContinuousGrabbing = false;
            }

            if (BaslerCams[1].Camera.StreamGrabber.IsGrabbing && BaslerCams[1].IsContinuousGrabbing)
            {
                BaslerCams[1].Camera.StreamGrabber.Stop();
                BaslerCams[1].Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.On);

                BaslerCams[1].IsContinuousGrabbing = false;
            }
        }

        /// <summary>
        /// 開始管件表面相機連續拍攝
        /// </summary>
        public void StartSurfaceCameraContinous()
        {
            // 表面 1 
            if (!BaslerCams[2].IsContinuousGrabbing && !BaslerCams[2].IsGrabberOpened)
            {
                BaslerCams[2].Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.Off);
                BaslerCams[2].Camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByStreamGrabber);

                BaslerCams[2].IsContinuousGrabbing = true;
            }

            // 表面 2
            if (!BaslerCams[3].IsContinuousGrabbing && !BaslerCams[3].IsGrabberOpened)
            {
                BaslerCams[3].Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.Off);
                BaslerCams[3].Camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByStreamGrabber);

                BaslerCams[3].IsContinuousGrabbing = true;
            }
        }

        /// <summary>
        /// 停止管件表面相機連續拍攝
        /// </summary>
        public void StopSurfaceCameraContinous()
        {
            if (BaslerCams[2].Camera.StreamGrabber.IsGrabbing && BaslerCams[2].IsContinuousGrabbing)
            {
                BaslerCams[2].Camera.StreamGrabber.Stop();
                BaslerCams[2].Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.On);

                BaslerCams[2].IsContinuousGrabbing = false;
            }

            if (BaslerCams[3].Camera.StreamGrabber.IsGrabbing && BaslerCams[3].IsContinuousGrabbing)
            {
                BaslerCams[3].Camera.StreamGrabber.Stop();
                BaslerCams[3].Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.On);

                BaslerCams[3].IsContinuousGrabbing = false;
            }
        }

        // /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// 

#if true
        /// <summary>
        /// 啟動窗戶、耳朵相機 Grabber
        /// </summary>
        public void StartWindowEarGrabber()
        {
            if (!BaslerCams[0].IsGrabberOpened && !BaslerCams[0].IsGrabbing)
            {
                // 啟動 StreamGrabber 連續拍攝
                BaslerCams[0].Camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByUser);

                BaslerCams[0].Camera.WaitForFrameTriggerReady(500, TimeoutHandling.ThrowException);
                BaslerCams[0].IsGrabberOpened = true;
                BaslerCams[0].IsContinuousGrabbing = false;

                // 
                //BaslerCams[0].Camera.StreamGrabber.ImageGrabbed -= MainTab.StreamGrabber_ImageGrabbed;
            }

            if (!BaslerCams[1].IsGrabberOpened && !BaslerCams[1].IsGrabbing)
            {
                // 啟動 StreamGrabber 連續拍攝
                BaslerCams[1].Camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByUser);

                BaslerCams[1].Camera.WaitForFrameTriggerReady(500, TimeoutHandling.ThrowException);
                BaslerCams[1].IsGrabberOpened = true;
                BaslerCams[1].IsContinuousGrabbing = false;

                // 
                //BaslerCams[1].Camera.StreamGrabber.ImageGrabbed -= MainTab.StreamGrabber_ImageGrabbed;
            }
        }

        /// <summary>
        /// 停止窗戶、耳朵相機 Grabber
        /// </summary>
        public void StopWindowEarGrabber()
        {
            if (BaslerCams[0].IsGrabbing)
            {
                // 啟動 StreamGrabber 連續拍攝
                BaslerCams[0].Camera.StreamGrabber.Stop();
                BaslerCams[0].IsGrabberOpened = false;

                // 
                //BaslerCams[0].Camera.StreamGrabber.ImageGrabbed += MainTab.StreamGrabber_ImageGrabbed;
            }

            if (BaslerCams[1].IsGrabbing)
            {
                // 啟動 StreamGrabber 連續拍攝
                BaslerCams[1].Camera.StreamGrabber.Stop();
                BaslerCams[1].IsGrabberOpened = false;

                // 
                //BaslerCams[1].Camera.StreamGrabber.ImageGrabbed += MainTab.StreamGrabber_ImageGrabbed;
            }
        }
#endif

        // /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// 
        #endregion
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

        #region 效能較好 Bitmap & Mat convert to ImageSource (High performace)
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

    public static class ProgressBarExtension
    {
        public static void SetPercent(this ProgressBar progressBar, double toValue, TimeSpan timeSpan)
        {
            DoubleAnimation animation = new DoubleAnimation(toValue, timeSpan, FillBehavior.HoldEnd);
            progressBar.BeginAnimation(RangeBase.ValueProperty, animation);
        }


        public static void SetPercent(this ProgressBar progressBar, double fromValue, double toValue, TimeSpan timeSpan)
        {
            DoubleAnimation animation = new DoubleAnimation(fromValue, toValue, timeSpan, FillBehavior.HoldEnd);
            progressBar.BeginAnimation(RangeBase.ValueProperty, animation);
        }
    }
}


/// <summary>
/// 自訂義 property 用
/// </summary>
namespace ApexVisIns.CustomProperty
{
    public class StatusHelper : DependencyObject
    {
        public static readonly DependencyProperty ConnectedProperty = DependencyProperty.RegisterAttached("Connected", typeof(bool), typeof(StatusHelper), new PropertyMetadata(false));


        public static readonly DependencyProperty GrabbingProperty = DependencyProperty.RegisterAttached("IsGrabbing", typeof(bool), typeof(StatusHelper), new PropertyMetadata(false));


        public static readonly DependencyProperty AlarmProperty = DependencyProperty.RegisterAttached("Alarm", typeof(bool), typeof(StatusHelper), new PropertyMetadata(false));


        public static void SetConnected(DependencyObject target, bool value)
        {
            target.SetValue(ConnectedProperty, value);
        }

        public static bool GetConnected(DependencyObject target)
        {
            return (bool)target.GetValue(ConnectedProperty);
        }

        public static void SetIsGrabbing(DependencyObject target, bool value)
        {
            target.SetValue(GrabbingProperty, value);
        }

        public static bool GetIsGrabbing(DependencyObject target)
        {
            return (bool)target.GetValue(GrabbingProperty);
        }

        public static void SetAlarm(DependencyObject target, bool value)
        {
            target.SetValue(AlarmProperty, value);
        }

        public static bool GetAlarm(DependencyObject target)
        {
            return (bool)target.GetValue(AlarmProperty);
        }
    }

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

