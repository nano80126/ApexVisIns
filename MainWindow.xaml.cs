using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;
using MaterialDesignThemes.Wpf;
using MCAJawIns.Tab;
using System.Text.Json;
using System.Text.Json.Serialization;

using MCAJawInfo = MCAJawIns.Mongo.Info;
using System.IO;
using MongoDB.Bson;
using MCAJawIns.Mongo;

namespace MCAJawIns
{
    #region Enumerator
    public enum JawTypes
    {
        /// <summary>
        /// 小Jaw
        /// </summary>
        [Description("小 Jaw")]
        S = 1,
        /// <summary>
        /// 中 Jaw
        /// </summary>
        [Description("中 Jaw")]
        M = 2,
        /// <summary>
        /// 大 Jaw
        /// </summary>
        [Description("大 Jaw")]
        L = 3
    }

    /// <summary>
    /// 啟用模式
    /// </summary>
    public enum InitModes
    {
        /// <summary>
        /// 自動
        /// </summary>
        [Description("自動模式")]
        AUTO = 1,
        /// <summary>
        /// 編輯
        /// </summary>
        [Description("編輯模式")]
        EDIT = 2
    }

    /// <summary>
    /// 硬體初始化用旗標
    /// </summary>
    [Flags]
    public enum InitFlags
    {
        /// <summary>
        /// 初始化完成
        /// </summary>
        [Description("初始化完成")]
        OK = 0,
        /// <summary>
        /// 初始化資料庫失敗
        /// </summary>
        [Description("初始化資料庫失敗")]
        INIT_DATABASE_FAILED = 1,
        /// <summary>
        /// 讀取尺寸規格資料失敗
        /// </summary>
        [Description("載入尺寸規格資料失敗")]
        LOAD_SPEC_DATA_FAILED = 2,
        /// <summary>
        /// 載入稼動時間(availability)與檢驗數量(performance)失敗
        /// </summary>
        [Description("載入稼動時間(availability)與檢驗數量(performance)資料失敗")]
        LOAD_AP_INFO_FAILED = 4,
        /// <summary>
        /// 外圍設備連線失敗
        /// </summary>
        [Description("初始化逾時")]
        INIT_TIMEOUT_FAILED = 8,
        /// <summary>
        /// 相機Trigger Mode 設置失敗
        /// </summary>
        [Description("相機Trigger Mode設置失敗")]
        SET_CAMERA_TRIGGER_MODE_FAILED = 16
    }
    #endregion

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
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

        #region Serial Port Enumerator (SerialPort 列舉器)
        /// <summary>
        /// Com Port 列舉器，
        /// </summary>
        public SerialEnumer SerialEnumer { get; set; }
        #endregion

        #region Light Controller
        //public static LightSerial LightCtrl { get; set; }
        /// <summary>
        /// 光源控制器
        /// </summary>
        public static LightSerial[] LightCtrls { get; set; }
        #endregion

        #region I/O Controller
        public static WISE4050 ModbusTCPIO { get; set; }
        #endregion

        #region EtherCAT Motion
        [Obsolete]
        public static ServoMotion ServoMotion { get; set; }
        #endregion

        #region Database
        /// <summary>
        /// MongoDB 存取
        /// </summary>
        public static MongoAccess MongoAccess { get; set; }
        #endregion

        #region Resources
        /// <summary>
        /// 訊息通知器
        /// </summary>
        public static MsgInformer MsgInformer { get; set; }
        #endregion

        #region Varibles
#if JawS
        private JawTypes _jawTypes = JawTypes.S;
#elif JawM
        private JawTypes _jawTypes = JawTypes.M;
#elif JawL
        private JawTypes _jawTypes = JawTypes.L;
#endif
        /// <summary>
        /// 選擇 Jaw Type
        /// </summary>
        public JawTypes JawType
        {
            get => _jawTypes;
            set
            {
                if (value != _jawTypes)
                {
                    _jawTypes = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 初始化模式
        /// </summary>
        public InitModes InitMode { get; set; } = InitModes.AUTO;
        #endregion

        #region Tabs
        /// <summary>
        /// MCA Jaw 主頁面 (設為 public，因為 DatabaseTab 需要取用)
        /// </summary>
        public MCAJaw MCAJaw { get; set; }
        private CameraTab CameraTab { get; set; }
        //private MotionTab MotionTab { get; set; }
        private DatabaseTab DatabaseTab { get; set; }
        private EngineerTab EngineerTab { get; set; }
        /// <summary>
        /// 系統資訊頁面 (設為 public)
        /// </summary>
        public SystemInfoTab SystemInfoTab { get; set; }
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            // InitializePanels();

            // Debug.WriteLine($"{DateTime.Now:mm:ss.fff}");
            LoadTabItems();
            // Debug.WriteLine($"{DateTime.Now:mm:ss.fff}");
        }

        /// <summary>
        /// 主視窗載入，
        /// 資源尋找等初始化在此
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine($"{(bool)(BaslerCam?.IsConnected) == false}");
            //Debug.WriteLine($"------------------------");

            #region Find Resource
            MsgInformer = FindResource(nameof(MCAJawIns.MsgInformer)) as MsgInformer;
            MsgInformer.EnableCollectionBinding();
            MsgInformer.ProgressValueChanged += MsgInformer_ProgressValueChanged;   // 綁定 ProgressBar Value Changed 事件
            MsgInformer.EnableProgressBar();
            #endregion

            #region Cameras
            CameraEnumer = TryFindResource(nameof(CameraEnumer)) as CameraEnumer;
            CameraEnumer?.WorkerStart();

#if DEBUG || debug
            BaslerCam = FindResource(nameof(BaslerCam)) as BaslerCam;
#endif
            BaslerCams = FindResource(nameof(BaslerCams)) as BaslerCam[];
            #endregion

            #region Serial Port
            SerialEnumer = FindResource(nameof(SerialEnumer)) as SerialEnumer;
            SerialEnumer.WorkerStart();
            #endregion

            #region Light Controller
            LightCtrls = FindResource(nameof(LightCtrls)) as LightSerial[];
            #endregion

            #region EtherCAT Motion
            // MCA Jaw 用不到
            // ServoMotion = FindResource(nameof(ServoMotion)) as ServoMotion;
            // ServoMotion.EnableCollectionBinding();  // 啟用 Collection Binding，避免跨執行緒錯誤
            #endregion

            #region IO Controller
            // IO Module (WISE-4050/LAN)
            ModbusTCPIO = FindResource(nameof(ModbusTCPIO)) as WISE4050;
            #endregion

            #region MongoDB Access
            MongoAccess = FindResource(nameof(MongoAccess)) as MongoAccess;
            #endregion

            // 載入後, focus 視窗
            _ = Focus();

            // 若不為 DebugMode，設為全螢幕
            WindowState = !DebugMode ? WindowState.Maximized : WindowState.Normal;

            // SpinWait.SpinUntil(() => false, 1000);
            // CreateIOWindow();
#if false
            ObservableCollection<int> observablaCollection = new ObservableCollection<int>();
            for (int i = 0; i < 10; i++)
            {
                observablaCollection.Add(i);
            }
            Debug.WriteLine($"{string.Join(",", observablaCollection)}");
            observablaCollection.Move(0, observablaCollection.Count() - 1);
            Debug.WriteLine($"{string.Join(",", observablaCollection)}");

            int idx = observablaCollection.LastIndex();
            Debug.WriteLine($"{idx}"); 
#endif

            // AppTabControl.Items[]

            #region 開啟 Mode Dialog
            ModeWindow modeWindow = new() { Owner = this };
            if (modeWindow.ShowDialog() == true) { Debug.WriteLine($"Init Mode: {InitMode}, MainWindow.xaml Line: 280"); }
            #endregion

            #region 初始化
            // 設定啟動時間
            SystemInfoTab.SystemInfo.SetStartTime();
            // 設定是否為自動模式
            SystemInfoTab.SystemInfo.SetMode(InitMode == InitModes.AUTO);
            // 啟動 TcpListener
            SystemInfoTab.SystemInfo.SetTcpListener();
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
            CameraEnumer?.WorkerEnd();   // 停止 Camera Enumerator
            CameraEnumer?.Dispose();
            SerialEnumer?.WorkerEnd();   // 停止 Serial Enumerator 
            SerialEnumer?.Dispose();
            // LightEnumer.WorkerEnd(); // deprecated class

            MsgInformer?.DisableCollectionBinding();
            MsgInformer?.DisposeProgressTask();
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
                // Debug.WriteLine($"{tabItem.Header} {(tabItem.Header as PackIcon).Kind}");
                if (tabItem.Content != null) { continue; }

                switch (i)
                {
                    case 0:
                        MCAJaw = new MCAJaw()
                        {
                            Name = "MCAJawTab",
                            Focusable = true,
                            FocusVisualStyle = null,
                        };
                        tabItem.Content = MCAJaw;
                        break;
                    case 1:
                        CameraTab = new CameraTab()
                        {
                            Name = nameof(CameraTab),
                            Focusable = true,
                            FocusVisualStyle = null
                        };
                        tabItem.Content = CameraTab;
                        break;
                    case 2:
                        // 空位保留
                        break;
                    case 3:
                        DatabaseTab = new DatabaseTab()
                        {
                            Name = nameof(DatabaseTab),
                            Focusable = true,
                            FocusVisualStyle = null
                        };
                        tabItem.Content = DatabaseTab;
                        break;
                    case 4:
                        if (DebugMode)  // 先判斷是否為 Debug Mode
                        {
                            EngineerTab = new EngineerTab()
                            {
                                Name = nameof(EngineerTab),
                                Focusable = true,
                                FocusVisualStyle = null
                            };
                            tabItem.Content = EngineerTab;
                        }
                        break;
                    case 5:
                        SystemInfoTab = new SystemInfoTab()
                        {
                            Name = nameof(SystemInfoTab),
                            Focusable = true,
                            FocusVisualStyle = null
                        };
                        tabItem.Content = SystemInfoTab;
                        break;
                    default:
                        break;
                }
            }
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
                // MainProgress.Value = e.OldValue;
                // MainProgress.SetPercent(e.NewValue, e.Duration);
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
                // 預設管理者密碼
                if (LoginPassword.Password == Password)
                {
                    //LoginFlag = true;
                    AuthLevel = 9;
                    //IOWindow?.PropertyChange(nameof(LoginFlag));
                    //LoginPasswordHint.Text = string.Empty;
                    //LoginPasswordHint.Visibility = Visibility.Hidden;
                    LoginDialog.IsOpen = false;
                }
                else if (PasswordDict.TryGetValue(LoginPassword.Password, out int level))
                {
                    //LoginFlag = true;
                    AuthLevel = level;
                    //
                    //LoginPasswordHint.Text = string.Empty;
                    //LoginPasswordHint.Visibility = Visibility.Hidden;
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
            // empty password and hide hint
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
                //LoginFlag = true;
                AuthLevel = 9;
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

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            //LoginFlag = false;
            AuthLevel = 0;
            OnNavIndex = 0;
        }
        #endregion

        /// <summary>
        /// 程式完整關閉 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AppFullClose_Click(object sender, RoutedEventArgs e)
        {
            // 關閉所有相機
            if (BaslerCams != null)
            {
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
            }

            if (BaslerCam != null)
            {
                if (BaslerCam.IsOpen)
                {
                    if (BaslerCam.IsGrabbing)
                    {
                        Basler_StopStreamGrabber(BaslerCam);
                    }
                    BaslerCam.Close();
                }
            }

            // 重製 & 關閉所有光源
            if (LightCtrls != null)
            {
                foreach (LightSerial ctrl in LightCtrls)
                {
                    if (ctrl.IsComOpen)
                    {
                        _ = ctrl.TryResetAllChannel(out _);
                        ctrl.ComClose();
                    }
                }
            }


            // 與資料庫斷線
            if (MongoAccess != null && MongoAccess.Connected)
            {
                // 若為自動模式，紀錄自動模式運行時間和檢驗數量
                if (InitMode == InitModes.AUTO)
                {
                    MCAJawInfo info = new MCAJawInfo()
                    {
                        Type = MCAJawInfo.InfoTypes.System,
                        Data = SystemInfoTab.SystemInfo.ToBsonDocument(),
                        UpdateTime = DateTime.Now,
                        InsertTime = DateTime.Now,
                    };
                    MongoAccess.InsertOne(nameof(JawCollection.Info), info);
                }

                MongoAccess.Disconnect();
            }

            _ = SpinWait.SpinUntil(() => BaslerCams == null || BaslerCams.All(cam => !cam.IsConnected), 3000);
            _ = SpinWait.SpinUntil(() => LightCtrls == null || LightCtrls.All(ctrl => !ctrl.IsComOpen), 3000);

            Close();
        }
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
            DoubleAnimation animation = new(fromValue, toValue, timeSpan, FillBehavior.HoldEnd);
            progressBar.BeginAnimation(RangeBase.ValueProperty, animation);
        }
    }
}


/// <summary>
/// 自訂義 property 用
/// </summary>
namespace MCAJawIns.CustomProperty
{
    public class StatusHelper : DependencyObject
    {
        /// <summary>
        /// Hardware is connected
        /// </summary>
        public static readonly DependencyProperty ConnectedProperty = DependencyProperty.RegisterAttached("Connected", typeof(bool), typeof(StatusHelper), new PropertyMetadata(false));

        /// <summary>
        /// Camera is Grabbing
        /// </summary>
        public static readonly DependencyProperty GrabbingProperty = DependencyProperty.RegisterAttached("IsGrabbing", typeof(bool), typeof(StatusHelper), new PropertyMetadata(false));

        /// <summary>
        /// Motor is alarm
        /// </summary>
        public static readonly DependencyProperty MotorAlarmProperty = DependencyProperty.RegisterAttached("MotorAlarm", typeof(bool), typeof(StatusHelper), new PropertyMetadata(false));

        /// <summary>
        /// Bit Property
        /// </summary>
        public static readonly DependencyProperty BitProperty = DependencyProperty.RegisterAttached("Bit", typeof(bool), typeof(StatusHelper), new PropertyMetadata(false));

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

        public static void SetMotorAlarm(DependencyObject target, bool value)
        {
            target.SetValue(MotorAlarmProperty, value);
        }

        public static bool GetMotorAlarm(DependencyObject target)
        {
            return (bool)target.GetValue(MotorAlarmProperty);
        }

        public static void SetBit(DependencyObject target, bool value)
        {
            target.SetValue(BitProperty, value);
        }

        public static bool GetBit(DependencyObject target)
        {
            return (bool)target.GetValue(BitProperty);
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

