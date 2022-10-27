#define UNITTEST1

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Basler.Pylon;
using MCAJawIns.Algorithm;
using MCAJawIns.Mongo;
using MCAJawIns.Product;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using SystemInfo;
using MCAJawConfig = MCAJawIns.Mongo.Config;
using MCAJawInfo = MCAJawIns.Mongo.Info;

namespace MCAJawIns.Tab
{
    /// <summary>
    /// MCAJaw.xaml 的互動邏輯
    /// </summary>
    public partial class MCAJaw : StackPanel, INotifyPropertyChanged, IDisposable
    {
        #region Enumerator
        /// <summary>
        /// 當前狀態列舉
        /// </summary>
        public enum INS_STATUS
        {
            [Description("初始化")]
            INIT = 0,
            [Description("準備檢驗")]
            READY = 1,
            [Description("檢驗中")]
            INSPECTING = 2,
            [Description("錯誤")]
            ERROR = 3,
            [Description("閒置")]
            IDLE = 4,
            [Description("編輯模式")]
            DEVELOPMENT = 8,
            [Description("未知")]
            UNKNOWN = 9,
        }
        #endregion

        #region Resources (defined in .xaml)
        /// <summary>
        /// Jaw 檢驗結果 (綁 Lot)
        /// </summary>
        public JawInspection JawInspection { get; set; }
        /// <summary>
        /// Jaw 規格設定 (包含檢驗結果) (這邊物件再細分 => results collection group 拆出來)
        /// </summary>
        public JawResultGroup JawResultGroup { get; set; }
        /// <summary>
        /// Jaw 尺寸規格設定列表
        /// </summary>
        public JawSizeSpecList JawSizeSpecList { get; set; }

        //public obser
        /// JawResultsGroup // 量測結果顯示
        /// JawSpecSetting  // 尺寸啟用與否設定
        #endregion

        #region Fields
        /// <summary>
        /// 初始化用 TokenSource
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        /// <summary>
        /// Disposed 旗標
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Main & Spec Setting
        /// </summary>
        private int _jawTab;

        /// <summary>
        /// NG 音效
        /// </summary>
        [Obsolete]
        private readonly SoundPlayer SoundNG = new SoundPlayer(@".\sound\NG.wav");          // 3 短音
        // private readonly SoundPlayer SoundAlarm = new SoundPlayer(@".\sound\Alarm.wav");    // 4 極短音

        private MediaPlayer PlayerNG;
        private MediaPlayer PlayerAlarm;

        private INS_STATUS _status = INS_STATUS.UNKNOWN;
        #endregion

        #region Properties
        public MainWindow MainWindow { get; } = (MainWindow)Application.Current.MainWindow;
        public MCAJawS MCAJawS { get; set; }
        public MCAJawM MCAJawM { get; set; }
        public MCAJawL MCAJawL { get; set; }

        public MCAJawAlgorithm MCAJawPart { get; set; }
        /// <summary>
        /// 啟用之Tab (檢驗用 or 尺寸設定用)
        /// </summary>
        public int JawTab
        {
            get => _jawTab;
            set
            {
                if (value != _jawTab)
                {
                    _jawTab = value;
                    OnPropertyChanged();
                }
            }
        }
        /// <summary>
        /// 當前狀態
        /// </summary>
        public INS_STATUS Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Private Path for Initializing
        /// <summary>
        /// 規格路徑
        /// </summary>
        private string SpecDirectory { get; } = @$"specification";
        /// <summary>
        /// 規格檔案
        /// </summary>
        private string SpecPath { get; } = $@"MCAJaw.json";
        /// <summary>
        /// 規格群組檔案
        /// </summary>
        private string SpecGroupPath { get; } = $@"Group.json";
        /// <summary>
        /// 相機組態目錄, Camera Configs Directory
        /// </summary>
        private string CamerasDirectory { get; } = @"cameras";
        /// <summary>
        /// 相機組態檔名稱, Camera Configs File Name
        /// </summary>
        private string CamerasPath { get; } = @"camera.json";
        #endregion

        #region Local Object (方便呼叫)
        /// <summary>
        /// WISE-4050/LAN IO 控制器
        /// </summary>
        private WISE4050 ModbusTCPIO;
        /// <summary>
        /// 24V 光源控制器
        /// </summary>
        private LightSerial LightCOM2;
        /// <summary>
        /// Basler Camera 相機 1
        /// </summary>
        private BaslerCam BaslerCam1;
        /// <summary>
        /// Basler Camera 相機 2
        /// </summary>
        private BaslerCam BaslerCam2;
        /// <summary>
        /// Basler Camera 相機 3
        /// </summary>
        private BaslerCam BaslerCam3;
        /// <summary>
        /// 資料庫存取
        /// </summary>
        private MongoAccess MongoAccess;
        #endregion

        #region Flags
        /// <summary>
        /// Tab loaded 旗標
        /// </summary>
        private bool loaded;
        /// <summary>
        /// 硬體正在初始化旗標
        /// </summary>
        private bool initializing;
        /// <summary>
        /// 硬體初始化完畢
        /// </summary>
        private bool initialized;

        // // // Public below
        private bool CameraInitialized { get; set; }
        private bool LightCtrlInitilized { get; set; }
        private bool IOCtrlInitialized { get; set; }
        private bool DatabaseInitialized { get; set; }
        #endregion

        public MCAJaw()
        {
            InitializeComponent();

            // Set value when init
            // MainWindow = (MainWindow)Application.Current.MainWindow;
            // 初始化路徑 
            InitSpecSettingDirectory();
        }

        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            #region 綁定 Resource
            JawInspection = FindResource(nameof(JawInspection)) as JawInspection;
            JawResultGroup = FindResource("SpecGroup") as JawResultGroup;
            JawSizeSpecList = FindResource(nameof(JawSizeSpecList)) as JawSizeSpecList;

            if (!JawResultGroup.SyncBinding) { JawResultGroup.EnableCollectionBinding(); }
            #endregion

            #region 初始化
            // 先確認已初始化旗標
            if (!initialized)
            {
                switch (MainWindow.InitMode)
                {
                    case InitModes.AUTO:
                        // 自動模式初始化
                        if (!initializing) { InitPeripherals(); }

                        // 設定為自動模式 (轉至 MainWindow.xaml.cs)
                        // MainWindow.SystemInfoTab.SystemInfo.SetMode(true);

                        // 設定閒置計時器 (移動到初始化完成後)
                        // SetIdleTimer(60);
                        break;
                    case InitModes.EDIT:
                        // 編輯模式初始化 (只連線 MongoDB)
                        if (!initializing) { InitDevelopment(); }

                        // 設定為編輯模式 (轉至 MainWindow.xaml.cs)
                        // MainWindow.SystemInfoTab.SystemInfo.SetMode(false);
                        break;
                    default:
                        // 保留
                        break;
                }

                switch (MainWindow.JawType)
                {
                    case JawTypes.S:
                        //MCAJawS = new();
                        MCAJawPart = new MCAJawS();
                        break;
                    case JawTypes.M:
                        //MCAJawM = new();
                        MCAJawPart = new MCAJawM();
                        break;
                    case JawTypes.L:
                        //MCAJawL = new();
                        MCAJawPart = new MCAJawL();
                        break;
                    default:
                        // 保留
                        break;
                }

                Debug.WriteLine($"Type:  {MCAJawPart.GetType()} line: 285");
            }

#if false
            // Debug.WriteLine($"Flags");
            // Debug.WriteLine($"{InitFlags.INIT_PERIPHERALS_FAILED | InitFlags.SET_CAMERA_TRIGGER_MODE_FAILED}");
            // Debug.WriteLine($"{InitFlags.INIT_PERIPHERALS_FAILED}");
            // Debug.WriteLine($"{((InitFlags.INIT_PERIPHERALS_FAILED | InitFlags.LOAD_SPEC_DATA_FAILED) & InitFlags.LOAD_SPEC_DATA_FAILED) == InitFlags.LOAD_SPEC_DATA_FAILED}");
#endif
            #endregion

            #region All Tab basic info
            if (!loaded)
            {
                MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.APP, "主頁面已載入");
                // 載入規格設定 (called after mongo initialized)
                // LoadSpecList();
                loaded = true;
            }
            #endregion
        }

        #region 初始化
        /// <summary>
        /// 外圍設備初始化初始化
        /// </summary>
        private async void InitPeripherals()
        {
            try
            {
                // 若正在初始化，直接 return
                if (initializing) { return; }

                CancellationToken token = _cancellationTokenSource.Token;

                initializing = true;

                Status = INS_STATUS.INIT;

                await InitMongoDB(token)
                    .ContinueWith(t =>
                    {
                        if (token.IsCancellationRequested)
                        {
                            Status = INS_STATUS.UNKNOWN;
                            token.ThrowIfCancellationRequested();
                        }

                        if (!MongoAccess.Connected) { return InitFlags.INIT_DATABASE_FAILED; }

                        // 初始化其他外設且等待完成
                        Task.WhenAll(
                            InitCamera(token),
                            InitLightCtrl(token),
                            InitIOCtrl(token)).Wait();
                        // Debug.WriteLine($"start 2 {DateTime.Now:ss.fff}");

                        return InitFlags.OK;
                    }, token)
                    .ContinueWith(t =>
                    {
                        // 終止初始化，狀態變更為未知
                        if (token.IsCancellationRequested)
                        {
                            Status = INS_STATUS.UNKNOWN;
                            token.ThrowIfCancellationRequested();
                        }

                        // 讀取 Size Spec 設定
                        bool load = LoadSpecList();

                        // 讀取 Size Spec Group 設定
                        LoadSpecGroupList();

                        return (!load ? InitFlags.LOAD_SPEC_DATA_FAILED : InitFlags.OK) | t.Result;
                    })
                    .ContinueWith(t =>
                    {
                        // 終止初始化，狀態變更為未知
                        if (token.IsCancellationRequested)
                        {
                            Status = INS_STATUS.UNKNOWN;
                            token.ThrowIfCancellationRequested();
                        }

                        if ((t.Result & InitFlags.INIT_DATABASE_FAILED) != InitFlags.INIT_DATABASE_FAILED)
                        {
                            // 載入自動模式時間與檢驗數量
                            FilterDefinition<MCAJawInfo> filter = Builders<MCAJawInfo>.Filter.Eq(nameof(MCAJawInfo.Type), nameof(MCAJawInfo.InfoTypes.System));
                            SortDefinition<MCAJawInfo> sort = Builders<MCAJawInfo>.Sort.Descending(nameof(MCAJawInfo.UpdateTime));

                            MongoAccess.FindOneSort(nameof(JawCollection.Info), filter, sort, out MCAJawInfo info);

                            if (info != null)
                            {
                                string timeString = (string)info.Data[nameof(Env.TotalAutoTime)];
                                string[] split = timeString.Split(':');

                                TimeSpan timeSpan = new TimeSpan(int.Parse(split[0], CultureInfo.CurrentCulture),
                                    int.Parse(split[1], CultureInfo.CurrentCulture),
                                    int.Parse(split[2], CultureInfo.CurrentCulture));

                                MainWindow.SystemInfoTab.Env.SetTotalAutoTime((int)timeSpan.TotalSeconds);
                                MainWindow.SystemInfoTab.Env.SetTotalParts((int)info.Data[nameof(Env.TotalParts)]);
                            }
                            else
                            {
                                return InitFlags.LOAD_AP_INFO_FAILED | t.Result;
                            }
                        }

                        return t.Result;
                    }, token)
                    .ContinueWith(t =>
                    {
                        // 等待進度條 等待進度條 等待進度條

                        // 終止初始化，狀態變更為未知
                        if (token.IsCancellationRequested)
                        {
                            Status = INS_STATUS.UNKNOWN;
                            token.ThrowIfCancellationRequested();
                        }

                        // 等待進度條滿，超過 5 秒則 Timeout
                        if (!SpinWait.SpinUntil(() => MainWindow.MsgInformer.ProgressValue >= 100, 5 * 1000))
                        {
                            // 外設初始化逾時
                            return InitFlags.INIT_TIMEOUT_FAILED | t.Result;
                        }

                        // 初始化 Media Player
                        InitMediaPlayer();

                        return t.Result;
                    }, token)
                    .ContinueWith(t =>
                    {
                        // 終止初始化，狀態變更為閒置
                        if (token.IsCancellationRequested)
                        {
                            Status = INS_STATUS.UNKNOWN;
                            token.ThrowIfCancellationRequested();
                        }

                        // 若外設啟動成功，啟動相機 StreamGrabber & Trigger Mode
                        if ((t.Result & InitFlags.INIT_TIMEOUT_FAILED) != InitFlags.INIT_TIMEOUT_FAILED)
                        {
                            // 相機開啟 Grabber
                            for (int i = 0; i < MainWindow.BaslerCams.Length; i++)
                            {
                                BaslerCam cam = MainWindow.BaslerCams[i];

                                #region 載入 UserSet => 大、中、小
                                if (!cam.IsGrabbing)
                                {
                                    // 這邊要防呆
                                    // cam.Camera.Parameters[PLGigECamera.UserSetSelector].SetValue("UserSet1");
                                    // cam.Camera.Parameters[PLGigECamera.UserSetLoad].Execute();

                                    // 確認為 UserSet1 (已經設為預設)
                                    // string userSet = cam.Camera.Parameters[PLGigECamera.UserSetSelector].GetValue();
                                    // Debug.WriteLine($"{cam.ModelName} {userSet}");

                                    MainWindow.Basler_StartStreamGrabber(cam);
                                }
                                #endregion
                            }

                            if (!MainWindow.BaslerCams.All(cam => cam.IsTriggerMode))
                            {
                                // 開啟 Trigger Mode 失敗
                                return InitFlags.SET_CAMERA_TRIGGER_MODE_FAILED | t.Result;
                            }

                            #region 確認相機鏡頭蓋取下
                            byte[] threholds = new byte[MainWindow.BaslerCams.Length];
                            for (int i = 0; i < MainWindow.BaslerCams.Length; i++)
                            {
                                switch (i)
                                {
                                    case 0:
                                        MainWindow.LightCtrls[1].SetAllChannelValue(96, 0);
                                        break;
                                    case 1:
                                        MainWindow.LightCtrls[1].SetAllChannelValue(0, 128);
                                        break;
                                    case 2:
                                        MainWindow.LightCtrls[1].SetAllChannelValue(320, 256);
                                        break;
                                }
                                _ = SpinWait.SpinUntil(() => false, 30);

                                BaslerCam cam = MainWindow.BaslerCams[i];
                                OpenCvSharp.Mat mat = MainWindow.Basler_RetrieveResult(cam);
                                // OpenCvSharp.Rect roi = new OpenCvSharp.Rect(mat.Width / 3, mat.Height / 3, mat.Width / 3, mat.Height / 3);
                                // Methods.GetRoiOtsu(mat, roi, 0, 255, out OpenCvSharp.Mat otsu, out threholds[i]);
                                threholds[i] = mat.At<byte>(mat.Height / 2, mat.Width / 2);

                                // Dispatcher.Invoke(() => {
                                // OpenCvSharp.Cv2.ImShow($"otsu{i}", otsu.Clone());
                                // });

                                mat.Dispose();
                            }
                            MainWindow.LightCtrls[1].SetAllChannelValue(0, 0);
                            Debug.WriteLine($"Threshold: {string.Join(",", threholds)}");

                            if (threholds.Any(threhold => threhold < 5))
                            {
                                Dispatcher.InvokeAsync(() =>
                                {
                                    MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, "相機鏡頭蓋未取下或有遮蔽物");

                                    PlayerAlarm.Position = TimeSpan.Zero;
                                    PlayerAlarm.Play();
                                });
                            }
                            #endregion
                        }

                        return t.Result;
                    }, token)
                    .ContinueWith(t =>
                    {
                        switch (t.Result)
                        {
                            case InitFlags.OK:
                                _ = MainWindow.Dispatcher.Invoke(() => Status = INS_STATUS.READY);
                                SetIdleTimer(60);
                                //InitMediaPlayer();
                                break;
                            case InitFlags.LOAD_SPEC_DATA_FAILED:
                                // 若僅有此錯誤，不影響運作
                                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.APP, $"初始化過程發生錯誤: Error Code {t.Result}, 使用預設之尺寸規格設定");
                                _ = MainWindow.Dispatcher.Invoke(() => Status = INS_STATUS.READY);
                                SetIdleTimer(60);
                                //InitMediaPlayer();
                                break;
                            case InitFlags.LOAD_AP_INFO_FAILED:
                                // 若僅有此錯誤，不影響運作
                                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.APP, $"初始化過程發生錯誤: Error Code {t.Result}, 使用初始設定");
                                _ = MainWindow.Dispatcher.Invoke(() => Status = INS_STATUS.READY);
                                SetIdleTimer(60);
                                //InitMediaPlayer();
                                break;
                            case InitFlags.LOAD_SPEC_DATA_FAILED | InitFlags.LOAD_AP_INFO_FAILED:
                                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.APP, $"初始化過程發生錯誤: Error Code {t.Result}, 使用預設設定與初始值");
                                _ = MainWindow.Dispatcher.Invoke(() => Status = INS_STATUS.READY);
                                SetIdleTimer(60);
                                //InitMediaPlayer();
                                break;
                            default:
                                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.APP, $"初始化過程失敗: Error Code {t.Result}");
                                _ = MainWindow.Dispatcher.Invoke(() => Status = INS_STATUS.ERROR);
                                // 若初始化失敗，不設定 IdleTimer();
                                break;
                        }

                        initializing = false;
                        initialized = true;
                    }, token);
            }
            catch (OperationCanceledException cancell)
            {
                // 新增 Message
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.APP, $"初始化過程被終止: {cancell.Message}");
            }
            catch (Exception ex)
            {
                // 新增 Message
                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.APP, $"初始化過程失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 開發模式初始化
        /// </summary>
        private async void InitDevelopment()
        {
            try
            {
                if (initializing) { return; }

                CancellationToken token = _cancellationTokenSource.Token;
                initializing = true;

                Status = INS_STATUS.INIT;

                await InitMongoDB(token)
                    .ContinueWith(t =>
                    {
                        if (token.IsCancellationRequested)
                        {
                            Status = INS_STATUS.UNKNOWN;
                            token.ThrowIfCancellationRequested();
                        }

                        // 讀取 Size Spec 設定
                        bool load = LoadSpecList();

                        // 讀取 Size Group Spec 設定
                        LoadSpecGroupList();

                        return !load ? InitFlags.LOAD_SPEC_DATA_FAILED : InitFlags.OK;
                    }, token).ContinueWith(t =>
                    {
                        switch (t.Result)
                        {
                            case InitFlags.LOAD_SPEC_DATA_FAILED:
                                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.APP, $"初始化過程發生錯誤: Error Code {t.Result}, 使用預設之尺寸規格設定");
                                break;
                            case InitFlags.OK:
                                break;
                        }

                        MainWindow.Dispatcher.Invoke(() => { Status = INS_STATUS.DEVELOPMENT; });
                        initializing = false;
                        initialized = true;
                    });
            }
            catch (Exception)
            {

                throw;
            }
        }

        /// <summary>
        /// 硬體關閉
        /// </summary>
        private void DisablePeripherals()
        {
            LightCOM2?.ComClose();
            LightCOM2?.Dispose();

            ModbusTCPIO?.Disconnect();
            ModbusTCPIO?.Dispose();
        }

        /// <summary>
        /// 初始化 Database
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private Task InitMongoDB(CancellationToken ct)
        {
            return Task.Run(() =>
            {
                if (DatabaseInitialized) { return; }

                if (ct.IsCancellationRequested) { ct.ThrowIfCancellationRequested(); }

                MongoAccess = MainWindow.MongoAccess;

                try
                {
                    // 選擇連線資料庫
                    switch (MainWindow.JawType)
                    {
                        case JawTypes.S:
                            MongoAccess.Connect("MCAJawS", "intaiUser", "mcajaw", 1500);
                            break;
                        case JawTypes.M:
                            MongoAccess.Connect("MCAJawM", "intaiUser", "mcajaw", 1500);
                            break;
                        case JawTypes.L:
                            MongoAccess.Connect("MCAJawL", "intaiUser", "mcajaw", 1500);
                            break;
                        default:
                            break;
                    }
                    // MongoAccess.Connect("MCAJawS", "intaiUser", "mcajaw", 1500);

                    if (MongoAccess.Connected)
                    {
                        // 建立權限集合
                        MongoAccess.CreateCollection(nameof(JawCollection.Auth));
                        // 建立組態集合 (目前只有資料庫保存時間設定)
                        MongoAccess.CreateCollection(nameof(JawCollection.Configs));
                        // 建立資訊集合
                        if (MongoAccess.CreateCollection(nameof(JawCollection.Info)))
                        {
                            CreateIndexModel<Info> indexModel = new CreateIndexModel<Info>(Builders<Info>.IndexKeys.Ascending(x => x.InsertTime));
                            MongoAccess.CreateIndexOne(nameof(JawCollection.Info), indexModel);
                        }
                        // 建立批次結果集合
                        if (MongoAccess.CreateCollection(nameof(JawCollection.Lots)))
                        {
                            CreateIndexModel<JawInspection> indexModel = new CreateIndexModel<JawInspection>(Builders<JawInspection>.IndexKeys.Ascending(x => x.DateTime));
                            MongoAccess.CreateIndexOne(nameof(JawCollection.Lots), indexModel);
                        }
                        // 建立量測結果集合
                        if (MongoAccess.CreateCollection(nameof(JawCollection.Measurements)))
                        {
                            CreateIndexModel<JawMeasurements> indexModel = new CreateIndexModel<JawMeasurements>(Builders<JawMeasurements>.IndexKeys.Ascending(x => x.DateTime));
                            MongoAccess.CreateIndexOne(nameof(JawCollection.Measurements), indexModel);
                        }

#if false               // Show indexes
                        MongoAccess.GetIndexes<JawInspection>(nameof(JawCollection.Auth), out List<BsonDocument> list);
                        Debug.WriteLine($"{string.Join(", ", list)}");
                        MongoAccess.GetIndexes<JawInspection>(nameof(JawCollection.Configs), out list);
                        Debug.WriteLine($"{string.Join(", ", list)}");
                        MongoAccess.GetIndexes<JawInspection>(nameof(JawCollection.Info), out list);
                        Debug.WriteLine($"{string.Join(", ", list)}");
                        MongoAccess.GetIndexes<JawInspection>(nameof(JawCollection.Lots), out list);
                        Debug.WriteLine($"{string.Join(", ", list)}");
                        MongoAccess.GetIndexes<JawInspection>(nameof(JawCollection.Measurements), out list);
                        Debug.WriteLine($"{string.Join(", ", list)}"); 
#endif

                        // 取得 Mongo 版本
                        string version = MongoAccess.GetVersion();
                        // 設定 Mongo 版本
                        MainWindow.SystemInfoTab.Env.SetMongoVersion(version);

#if InsertAuth          // 新增使用者、組態

                        DateTime dt = DateTime.Now;
                        AuthLevel[] authLevels = new AuthLevel[] {
                            new AuthLevel() { Role= nameof(AuthRoles.User), Password = "intai", Level = 1, InsertTime = dt, UpdateTime = dt },
                            new AuthLevel() { Role= nameof(AuthRoles.Quaiity), Password = "qc", Level = 2, InsertTime = dt, UpdateTime = dt  },
                            new AuthLevel() { Role= nameof(AuthRoles.Engineer), Password = "eng", Level = 5 , InsertTime = dt, UpdateTime = dt },
                        };
                        MongoAccess.InsertMany(nameof(JawCollection.Auth), authLevels);
#endif
                        // 載入使用者權限
                        MongoAccess.FindAll(nameof(JawCollection.Auth), Builders<AuthLevel>.Filter.Empty, out List<AuthLevel> levels);
                        foreach (AuthLevel item in levels)
                        {
                            MainWindow.PasswordDict.Add(item.Password, item.Level);
                        }

#if DeleteOldData       // 移除過期資料
                        MongoAccess.FindOne("Configs", Builders<MCAJawConfig>.Filter.Empty, out MCAJawConfig config);
                        if (config != null)
                        {
                            DateTime dt = DateTime.Now.AddMonths(config.DataReserveMonths * -1);
                            DeleteResult result;
                            result = MongoAccess.DeleteMany("Lots", Builders<JawInspection>.Filter.Lt("DateTime", dt));
                            if (result.DeletedCount > 0)
                            {
                                MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.DATABASE, $"批次舊資料已刪除, 刪除數量: {result.DeletedCount}");
                            }
                            result = MongoAccess.DeleteMany("Measurements", Builders<JawMeasurements>.Filter.Lt("DateTime", dt));
                            if (result.DeletedCount > 0)
                            {
                                MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.DATABASE, $"量測紀錄舊資料已刪除, 刪除數量: {result.DeletedCount}");
                            }
                        } 
#endif

                        // MainWindow.MsgInformer.TargetProgressValue += 17;
                        MainWindow.MsgInformer.AdvanceProgressValue(17);

                        DatabaseInitialized = true;
                        MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.DATABASE, "資料庫初始化完成");
                    }
                    else
                    {
                        throw new DatabaseException("資料庫連線失敗");
                    }
                }
                catch (Exception ex)
                {
                    // 讀取 Size Spec
                    LoadSpecList(false);

                    // 不切的話，message 太長
                    MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.DATABASE, $"資料庫初始化失敗: {ex.Message.Split(new string[] { "\n", ". " }, StringSplitOptions.RemoveEmptyEntries)[0]}");
                }
            }, ct);
        }

        /// <summary>
        /// 初始化相機
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private Task InitCamera(CancellationToken ct)
        {
            return Task.Run(() =>
            {
                if (CameraInitialized) { return; }

                if (ct.IsCancellationRequested) { ct.ThrowIfCancellationRequested(); }

                try
                {
                    string path = $@"{Directory.GetCurrentDirectory()}\{CamerasDirectory}\{CamerasPath}";
                    CameraConfigBaseExtension[] configs = Array.Empty<CameraConfigBaseExtension>();

                    // 載入相機組態
                    if (MongoAccess?.Connected == true)
                    {
                        FilterDefinition<MCAJawConfig> filter = Builders<MCAJawConfig>.Filter.Eq(nameof(MCAJawConfig.Type), nameof(MCAJawConfig.ConfigType.CAMERA));
                        MongoAccess.FindOne(nameof(JawCollection.Configs), filter, out MCAJawConfig cfg);

                        if (cfg != null)
                        {
                            // 反序列化
                            configs = cfg.DataArray.Select(x => BsonSerializer.Deserialize<CameraConfigBaseExtension>(x.ToBsonDocument())).ToArray();
                        }
                        else
                        {
                            if (File.Exists(path))
                            {
                                using StreamReader reader = new StreamReader(path);
                                string jsonStr = reader.ReadToEnd();

                                if (jsonStr != string.Empty)
                                {
                                    // 反序列化
                                    configs = JsonSerializer.Deserialize<CameraConfigBaseExtension[]>(jsonStr);
                                }
                                else
                                {
                                    throw new CameraException("相機設定檔為空");
                                }
                            }
                        }
                    }
                    else if (File.Exists(path))
                    {
                        using StreamReader reader = new StreamReader(path);
                        string jsonStr = reader.ReadToEnd();

                        if (jsonStr != string.Empty)
                        {
                            // 反序列化
                            configs = JsonSerializer.Deserialize<CameraConfigBaseExtension[]>(jsonStr);
                        }
                        else
                        {
                            throw new CameraException("相機設定檔為空");
                        }
                    }
                    else
                    {
                        throw new CameraException("相機設定檔不存在");
                    }

#if false
                    // string path = @"./devices/device.json";
                    // string path = $@"{Directory.GetCurrentDirectory()}\{CamerasDirectory}\{CamerasPath}";
                    // if (File.Exists(path))
                    // {
                    //     using StreamReader reader = new StreamReader(path);
                    //     string jsonStr = reader.ReadToEnd();
                    //     if (jsonStr != string.Empty)
                    //     {
                    //      // json 反序列化
                    //      CameraConfigBase[] devices = JsonSerializer.Deserialize<CameraConfigBase[]>(jsonStr);  
#endif

                    // 等待 Camera Enumerator 初始化
                    if (!SpinWait.SpinUntil(() => MainWindow.CameraEnumer.InitFlag == LongLifeWorker.InitFlags.Finished, 3000)) { throw new TimeoutException(); }

                    // 已連線之 Camera
                    List<CameraConfigBase> cams = MainWindow.CameraEnumer.CamsSource.ToList();

                    // 排序 Devices
                    Array.Sort(configs, (a, b) => a.TargetFeature - b.TargetFeature);

                    double Cam1PxSize = 0, Cam2PxSize = 0, Cam3PxSize = 0;
                    double Cam1Mg = 1, Cam2Mg = 1, Cam3Mg = 1;

                    Parallel.ForEach(configs, (dev, index) =>
                    {
                        // 確認 Device 為在線上之 Camera 
                        if (cams.Exists(cam => cam.SerialNumber == dev.SerialNumber))
                        {
                            Debug.WriteLine($"{dev.IP} {dev.TargetFeature} {dev.PixelSize} {dev.LensConfig.Magnification}");
#if true
                            switch (dev.TargetFeature)
                            {
                                case TargetFeature.MCA_Front:
                                    Cam1PxSize = dev.PixelSize;
                                    Cam1Mg = dev.LensConfig.Magnification;
                                    if (!MainWindow.BaslerCams[0].IsConnected)
                                    {
                                        BaslerCam1 = MainWindow.BaslerCams[0];
                                        if (MainWindow.Basler_Connect(BaslerCam1, dev.SerialNumber, dev.TargetFeature, ct))
                                        {
                                            _ = SpinWait.SpinUntil(() => false, 50);
                                            MainWindow.MsgInformer.AdvanceProgressValue(17);
                                        }
                                    }
                                    break;
                                case TargetFeature.MCA_Bottom:
                                    Cam2PxSize = dev.PixelSize;
                                    Cam2Mg = dev.LensConfig.Magnification;
                                    if (!MainWindow.BaslerCams[1].IsConnected)
                                    {
                                        BaslerCam2 = MainWindow.BaslerCams[1];
                                        if (MainWindow.Basler_Connect(BaslerCam2, dev.SerialNumber, dev.TargetFeature, ct))
                                        {
                                            _ = SpinWait.SpinUntil(() => false, 100);
                                            MainWindow.MsgInformer.AdvanceProgressValue(17);
                                        }
                                    }
                                    break;
                                case TargetFeature.MCA_SIDE:
                                    Cam3PxSize = dev.PixelSize;
                                    Cam3Mg = dev.LensConfig.Magnification;
                                    if (!MainWindow.BaslerCams[2].IsConnected)
                                    {
                                        BaslerCam3 = MainWindow.BaslerCams[2];
                                        if (MainWindow.Basler_Connect(BaslerCam3, dev.SerialNumber, dev.TargetFeature, ct))
                                        {
                                            _ = SpinWait.SpinUntil(() => false, 150);
                                            MainWindow.MsgInformer.AdvanceProgressValue(17);
                                        }
                                    }
                                    break;
                                case TargetFeature.Null:
                                    MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.CAMERA, "相機目標特徵未設置");
                                    break;
                                default:
                                    MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.CAMERA, "相機目標特徵設置有誤");
                                    break;
                            }
#endif
                        }
                    });

#if false
                    switch (MainWindow.JawType)
                    {
                        case JawTypes.S:
                            //MCAJawS.SetVisionParam(1, Cam1PxSize, Cam1Mg);
                            //MCAJawS.SetVisionParam(2, Cam2PxSize, Cam2Mg);
                            //MCAJawS.SetVisionParam(3, Cam3PxSize, Cam3Mg);

                            MCAJaw_.SetVisionParam(1, Cam1PxSize, Cam1Mg);
                            MCAJaw_.SetVisionParam(2, Cam2PxSize, Cam2Mg);
                            MCAJaw_.SetVisionParam(3, Cam3PxSize, Cam3Mg);
                            break;
                        case JawTypes.M:
                            //MCAJawM.SetVisionParam(1, Cam1PxSize, Cam1Mg);
                            //MCAJawM.SetVisionParam(2, Cam2PxSize, Cam2Mg);
                            //MCAJawM.SetVisionParam(3, Cam3PxSize, Cam3Mg);

                            MCAJaw_.SetVisionParam(1, Cam1PxSize, Cam1Mg);
                            MCAJaw_.SetVisionParam(2, Cam2PxSize, Cam2Mg);
                            MCAJaw_.SetVisionParam(3, Cam3PxSize, Cam3Mg);
                            break;
                        case JawTypes.L:
                            //MCAJawL.SetVisionParam(1, Cam1PxSize, Cam1Mg);
                            //MCAJawL.SetVisionParam(2, Cam2PxSize, Cam2Mg);
                            //MCAJawL.SetVisionParam(3, Cam3PxSize, Cam3Mg);

                            MCAJaw_.SetVisionParam(1, Cam1PxSize, Cam1Mg);
                            MCAJaw_.SetVisionParam(2, Cam2PxSize, Cam2Mg);
                            MCAJaw_.SetVisionParam(3, Cam3PxSize, Cam3Mg);
                            break;
                        default:
                            break;
                    } 
#endif
                    MCAJawPart.SetVisionParam(1, Cam1PxSize, Cam1Mg);
                    MCAJawPart.SetVisionParam(2, Cam2PxSize, Cam2Mg);
                    MCAJawPart.SetVisionParam(3, Cam3PxSize, Cam3Mg);

                    if (MainWindow.BaslerCams.All(cam => cam.IsConnected))
                    {
                        // 設置初始化完成旗標
                        CameraInitialized = true;
                        MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.CAMERA, "相機初始化完成");
                    }
                    else
                    {
                        throw new CameraException("相機未完全初始化");
                    }
                }
                catch (Exception ex)
                {
                    MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.CAMERA, $"相機初始化失敗: {ex.Message}");
                }
            }, ct);
        }

        /// <summary>
        /// 初始化光源
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private Task InitLightCtrl(CancellationToken ct)
        {
            return Task.Run(() =>
            {
                if (LightCtrlInitilized) { return; }

                if (ct.IsCancellationRequested) { ct.ThrowIfCancellationRequested(); }

                try
                {
                    //if (!SpinWait.SpinUntil(() => MainWindow.LightEnumer.InitFlag == LongLifeWorker.InitFlags.Finished, 3000))
                    //{
                    //    throw new TimeoutException("COM Port 列舉逾時");
                    //}

                    string result = string.Empty;
                    foreach (LightSerial ctrl in MainWindow.LightCtrls)
                    {
                        switch (ctrl.ComPort)
                        {
                            case "COM2":
                                LightCOM2 = ctrl;
                                LightCOM2.ComOpen(115200, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);

                                if (LightCOM2.Test(out result))
                                {
                                    // 重置所有通道
                                    LightCOM2.ResetAllChannel();
                                }
                                else
                                {
                                    // 關閉 COM 
                                    LightCOM2.ComClose();
                                    // 拋出異常
                                    //throw new Exception($"24V {result}");
                                    throw new LightCtrlException($"24V 光源控制通訊逾時");
                                }
                                break;
                            default:
                                break;
                        }
                    }

                    if (LightCOM2.IsComOpen)
                    {
                        //MainWindow.MsgInformer.TargetProgressValue += 17;
                        MainWindow.MsgInformer.AdvanceProgressValue(17);


                        LightCtrlInitilized = true;
                        MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.LIGHT, "光源控制初始化完成");
                    }
                    else
                    {
                        throw new LightCtrlException("光源控制器連線失敗");
                    }
                }
                catch (Exception ex)
                {
                    MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.LIGHT, $"光源初始化失敗: {ex.Message}");
                }
            }, ct);
        }

        /// <summary>
        /// 初始化 IO
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private Task InitIOCtrl(CancellationToken ct)
        {
            return Task.Run(() =>
            {
                if (IOCtrlInitialized) { return; }

                if (ct.IsCancellationRequested) { ct.ThrowIfCancellationRequested(); }

                ModbusTCPIO = MainWindow.ModbusTCPIO;

                try
                {
                    ModbusTCPIO.Connect();
                    ModbusTCPIO.IOChanged += ModbusTCPIO_IOChanged;


                    if (ModbusTCPIO.Conneected)
                    {
                        MainWindow.MsgInformer.AdvanceProgressValue(17);

                        IOCtrlInitialized = true;
                        MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.IO, "IO 控制初始化完成");
                    }
                    else
                    {
                        throw new WISE4050Exception("IO 控制器連線失敗");
                    }
                }
                catch (Exception ex)
                {

                    MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.IO, $"IO 控制初始化失敗: {ex.Message}");
                }
            }, ct);
        }

        /// <summary>
        /// IO Changed Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ModbusTCPIO_IOChanged(object sender, WISE4050.IOChangedEventArgs e)
        {
            if (e.DI0Raising)
            {
                ResetIdleTimer();
            }
            else if (e.DI0Falling)
            {
                // 觸發檢驗
                // 要做防彈跳
                Dispatcher.Invoke(() =>
                {
                    // 確認按鈕 Enabled
                    if (TriggerIns.IsEnabled)
                    {
                        TriggerIns.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    }
                });
            }

            // Debug.WriteLine($"{e.NewValue} {e.OldValue} {DateTime.Now:ss.fff}");
            // Debug.WriteLine($"{e.DI0Raising} {e.DI0Falling}");
            // Debug.WriteLine($"{e.DI3Raising} {e.DI3Falling}");
        }

        /// <summary>
        /// 初始化音效撥放器 (不需用Task)
        /// </summary>
        private void InitMediaPlayer()
        {
            Dispatcher.Invoke(() =>
            {
                PlayerNG = new MediaPlayer()
                {
                    Volume = 100
                };
                PlayerNG.Open(new Uri(Path.GetFullPath(@".\sound\NG.mp3")));

                PlayerAlarm = new MediaPlayer()
                {
                    Volume = 100
                };
                PlayerAlarm.Open(new Uri(Path.GetFullPath(@".\sound\Alarm.mp3")));
            });
        }
        #endregion

        #region 初始化 SpecList
        /// <summary>
        /// 初始化規格路徑
        /// </summary>
        private void InitSpecSettingDirectory()
        {
            // switch
            string directory = $@"{Directory.GetCurrentDirectory()}\{SpecDirectory}\{MainWindow.JawType}";

            // 若不存在則新增
            if (!Directory.Exists(directory))
            {
                // 新增路徑
                _ = Directory.CreateDirectory(directory);
            }

            SizeSpecSubTab.JsonDirectory = directory;
        }

        /// <summary>
        /// 載入規格設定 (因同一組件(namespace MCAJawIns.content)會使用，故為 internal)
        /// </summary>
        internal bool LoadSpecList(bool fromDb = true)
        {
            // 物件 綁定
            SizeSpecSubTab.MCAJaw = this;

            // 清除 集合
            _ = Dispatcher.InvokeAsync(() =>
            {
                //JawResultGroup.SizeSpecList.Clear();
                JawSizeSpecList.Source.Clear();
                JawInspection.LotResults.Clear();
            }).Wait();

            if (fromDb)
            {
                if (MainWindow.MongoAccess.Connected)
                {
                    FilterDefinition<MCAJawConfig> filter = Builders<MCAJawConfig>.Filter.Eq(nameof(MCAJawConfig.Type), nameof(MCAJawConfig.ConfigType.SPEC));

                    MainWindow.MongoAccess.FindOne(nameof(JawCollection.Configs), filter, out MCAJawConfig cfg);

                    if (cfg != null)
                    {
                        // 調用 Dispacher 變更集合
                        Dispatcher.Invoke(() =>
                        {
                            JawSpecSetting[] jawSpecs = cfg.DataArray.Select(x => BsonSerializer.Deserialize<JawSpecSetting>(x.ToBsonDocument())).ToArray();

                            JawInspection.LotResults.Add("good", new JawInspection.ResultElement("良品", "", 0, true));
                            foreach (JawSpecSetting item in jawSpecs)
                            {
                                // 加入尺寸規格列表
                                JawSizeSpecList.Source.Add(item);
                                // 加入批號檢驗結果 (初始化)
                                JawInspection.LotResults.Add(item.Key, new JawInspection.ResultElement(item.Item, item.Note, 0, item.Enable));
                            }
                        });
                        JawSizeSpecList.Save();

                        return true;
                    }
                    else
                    {
                        // 從本機文件載入 (遞迴)
                        return LoadSpecList(false);
                    }
                }
                else
                {
                    // 從本機文件載入 (遞迴)
                    return LoadSpecList(false);
                }
            }
            else
            {
                // 組成路徑字串
                string path = $@"{Directory.GetCurrentDirectory()}\{SpecDirectory}\{MainWindow.JawType}\{SpecPath}";

                if (File.Exists(path))
                {
                    using StreamReader reader = File.OpenText(path);
                    string jsonStr = reader.ReadToEnd();

                    if (jsonStr != string.Empty)
                    {
                        // 調用 Dispacher 變更集合
                        Dispatcher.Invoke(() =>
                        {
                            // 反序列化，載入 JSON FILE
                            JawSpecSetting[] list = JsonSerializer.Deserialize<JawSpecSetting[]>(jsonStr, new JsonSerializerOptions
                            {
                                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                            });

                            JawInspection.LotResults.Add("good", new JawInspection.ResultElement("良品", "", 0, true));
                            foreach (JawSpecSetting item in list)
                            {

                                // 加入尺寸規格列表
                                JawSizeSpecList.Source.Add(item);
                                // 加入批號檢驗結果 (初始化)
                                JawInspection.LotResults.Add(item.Key, new JawInspection.ResultElement(item.Item, item.Note, 0, item.Enable));
                            }
                        });
                        JawSizeSpecList.Save();

                        return true;
                    }
                    else
                    {
                        // 初始化尺寸規格
                        InitSizeSpec(MainWindow.JawType);

                        // 回傳 false
                        return false;
                    }
                }
                else // 若規格列表不存在
                {
                    // 初始化尺寸規格
                    InitSizeSpec(MainWindow.JawType);

                    // 回傳 false
                    return false;
                }
            }
        }

        /// <summary>
        /// 載入規格群組設定 (因同一組件(namespace MCAJawIns.content)會使用，故為 internal)
        /// </summary>
        /// <param name="fromDB"></param>
        internal void LoadSpecGroupList(bool fromDB = true)
        {
            if (fromDB)
            {
                if (MainWindow.MongoAccess.Connected)
                {
                    FilterDefinition<MCAJawConfig> filter = Builders<MCAJawConfig>.Filter.Eq(nameof(MCAJawConfig.Type), nameof(MCAJawConfig.ConfigType.SPECGROUP));

                    MainWindow.MongoAccess.FindOne(nameof(JawCollection.Configs), filter, out MCAJawConfig cfg);

                    if (cfg != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            JawSpecGroupSetting[] jawSpecs = cfg.DataArray.Select(x => BsonSerializer.Deserialize<JawSpecGroupSetting>(x.ToBsonDocument())).ToArray();

                            for (int i = 0; i < jawSpecs.Length; i++)
                            {
                                JawSizeSpecList.Groups[i].Content = jawSpecs[i].Content;
                                JawSizeSpecList.Groups[i].ColorString = jawSpecs[i].ColorString;
                            }
                        });
                        JawSizeSpecList.GroupSave();
                    }
                    else
                    {
                        LoadSpecGroupList(false);
                    }
                }
                else
                {
                    LoadSpecGroupList(false);
                }
            }
            else
            {
                // 組成路徑字串
                string path = $@"{Directory.GetCurrentDirectory()}\{SpecDirectory}\{MainWindow.JawType}\{SpecGroupPath}";

                if (File.Exists(path))
                {
                    using StreamReader reader = File.OpenText(path);
                    string jsonStr = reader.ReadToEnd();

                    if (jsonStr != string.Empty)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            JawSpecGroupSetting[] jawSpecs = JsonSerializer.Deserialize<JawSpecGroupSetting[]>(jsonStr, new JsonSerializerOptions
                            {
                                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                            });

                            for (int i = 0; i < jawSpecs.Length; i++)
                            {
                                JawSizeSpecList.Groups[i].Content = jawSpecs[i].Content;
                                JawSizeSpecList.Groups[i].ColorString = jawSpecs[i].ColorString;
                            }
                        });
                        JawSizeSpecList.GroupSave();
                    }
                }
            }
        }

        /// <summary>
        /// 初始化尺寸規格設定
        /// <para>※※※ 使用小Jaw的尺寸當成key，否則需要更改MCAJaw.xaml</para>
        /// </summary>
        private void InitSizeSpec(JawTypes type)
        {
            string[] keys = new string[] { "0.088R", "0.088L", "0.176", "0.008R", "0.008L", "0.013R", "0.013L", "0.024R", "0.024L", "back", "front", "bfDiff", "contour", "contourR", "contourL", "flatness" };
            string[] items = Array.Empty<string>();
            double[] center = Array.Empty<double>();
            double[] lowerc = Array.Empty<double>(); ;
            double[] upperc = Array.Empty<double>(); ;

            switch (type)
            {
                #region Jaw S
                case JawTypes.S:
                    items = new string[] { "0.088-R", "0.088-L", "0.176", "0.008-R", "0.008-L", "0.013-R", "0.013-L", "0.024-R", "0.024-L", "後開", "前開", "開度差", "輪廓度", "輪廓度R", "輪廓度L", "平直度" };
                    center = new double[] { 0.0880, 0.0880, 0.176, 0.008, 0.008, 0.013, 0.013, 0.0240, 0.0240, double.NaN, double.NaN, double.NaN, 0, 0, 0, 0 };
                    lowerc = new double[] { 0.0855, 0.0855, 0.173, 0.006, 0.006, 0.011, 0.011, 0.0225, 0.0225, 0.098, double.NaN, 0.0025, 0.000, 0.000, 0.000, 0.000 };
                    upperc = new double[] { 0.0905, 0.0905, 0.179, 0.010, 0.010, 0.015, 0.015, 0.0255, 0.0255, 0.101, double.NaN, 0.0110, 0.005, 0.005, 0.005, 0.007 };
                    break;
                #endregion
                #region Jaw M
                case JawTypes.M:
                    items = new string[] { "0.1195-R", "0.1195-L", "0.2395", "0.014-R", "0.014-L", "0.014-R", "0.014-L", "0.03225-R", "0.03225-L", "後開", "前開", "開度差", "輪廓度", "輪廓度R", "輪廓度L", "平直度" };
                    center = new double[] { 0.1195, 0.1195, 0.2395, 0.014, 0.014, 0.014, 0.014, 0.03225, 0.03225, double.NaN, double.NaN, double.NaN, 0, 0, 0, 0 };
                    lowerc = new double[] { 0.1170, 0.1170, 0.2360, 0.012, 0.012, 0.013, 0.013, 0.03150, 0.03150, 0.183, double.NaN, 0.0050, 0.000, 0.000, 0.000, 0.000 };
                    upperc = new double[] { 0.1220, 0.1220, 0.2430, 0.016, 0.016, 0.015, 0.015, 0.03300, 0.03300, 0.205, double.NaN, 0.0110, 0.005, 0.005, 0.005, 0.007 };
                    break;
                #endregion
                #region Jaw L
                case JawTypes.L:
                    items = new string[] { "0.088-R", "0.088-L", "0.176", "0.008-R", "0.008-L", "0.013-R", "0.013-L", "0.024-R", "0.024-L", "後開", "前開", "開度差", "輪廓度", "輪廓度R", "輪廓度L", "平直度" };
                    center = new double[] { 0.0880, 0.0880, 0.176, 0.008, 0.008, 0.013, 0.013, 0.0240, 0.0240, double.NaN, double.NaN, double.NaN, 0, 0, 0, 0 };
                    lowerc = new double[] { 0.0855, 0.0855, 0.173, 0.006, 0.006, 0.011, 0.011, 0.0225, 0.0225, 0.098, double.NaN, 0.0025, 0, 0, 0, 0 };
                    upperc = new double[] { 0.0905, 0.0905, 0.179, 0.010, 0.010, 0.015, 0.015, 0.0255, 0.0255, 0.101, double.NaN, 0.011, 0.005, 0.005, 0.005, 0.007 }; 
                    break;
                #endregion
                default:
                    break;
            }

#if false
            items = new string[] { "0.088-R", "0.088-L", "0.176", "0.008-R", "0.008-L", "0.013-R", "0.013-L", "0.024-R", "0.024-L", "後開", "前開", "開度差", "輪廓度", "輪廓度R", "輪廓度L", "平直度" };
            center = new double[] { 0.0880, 0.0880, 0.176, 0.008, 0.008, 0.013, 0.013, 0.0240, 0.0240, double.NaN, double.NaN, double.NaN, 0, 0, 0, 0 };
            lowerc = new double[] { 0.0855, 0.0855, 0.173, 0.006, 0.006, 0.011, 0.011, 0.0225, 0.0225, 0.098, double.NaN, 0.0025, 0, 0, 0, 0 };
            upperc = new double[] { 0.0905, 0.0905, 0.179, 0.010, 0.010, 0.015, 0.015, 0.0255, 0.0255, 0.101, double.NaN, 0.011, 0.005, 0.005, 0.005, 0.007 }; 
#endif

            // double[] correc = new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            double[] correc = new double[keys.Length];
            Array.Fill(correc, 0);
            double[] correc2 = new double[keys.Length];
            Array.Fill(correc2, 0);

            JawInspection.LotResults.Add("good", new JawInspection.ResultElement("良品", "", 0, true));
            for (int i = 0; i < keys.Length; i++)
            {
                // 調用 Dispacher 變更集合
                Dispatcher.Invoke(() =>
                {
                    // 加入尺寸規格表
                    // id = 0 means auto increase by source count
                    JawSpecSetting newItem = new JawSpecSetting(0, true, keys[i], items[i], center[i], lowerc[i], upperc[i], correc[i], correc2[i]);
                    JawSizeSpecList.AddNew(newItem);
                    // 加入批號檢驗結果 (初始化)
                    JawInspection.LotResults.Add(keys[i], new JawInspection.ResultElement(items[i], "", 0, true));
                });
            }
        }
        #endregion

        #region 主控版 , +/- 數量
        private void DockPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
            _ = (Window.GetWindow(this) as MainWindow).TitleGrid.Focus();
        }

        private void LotNumberCheckButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(JawInspection.LotNumber) && !string.IsNullOrWhiteSpace(JawInspection.LotNumber))
            {
                // 確認批號
                JawInspection.CheckLotNumber();
                // 該批設為未儲存
                JawInspection.SetLotInserted(false);
                // 清除 Focus
                DockPanel_MouseDown(null, null);
            }
            else
            {
                // 批號 TextBox Focus
                _ = LotText.Focus();
            }
        }

        private async void ResetCount_Click(object sender, RoutedEventArgs e)
        {
            if (JawInspection.LotNumberChecked && !JawInspection.LotInserted)
            {
                // Debug Mode or Check Dialog
                bool result = MainWindow.DebugMode || (bool)(await MaterialDesignThemes.Wpf.DialogHost.Show((sender as Button).CommandParameter, "MainDialog", (sender, e) =>
                {
                    Keyboard.ClearFocus();
                    _ = MainWindow.TitleGrid.Focus();
                }, null) ?? false);

                if (result)
                {
                    foreach (string key in JawInspection.LotResults.Keys)
                    {
                        JawInspection.LotResults[key].Count = 0;
                    }
                }
            }
            else
            {
                foreach (string key in JawInspection.LotResults.Keys)
                {
                    JawInspection.LotResults[key].Count = 0;
                }
            }
        }

        private void MinusButton_Click(object sender, RoutedEventArgs e)
        {
            string key = (sender as Button).CommandParameter.ToString();
            if (JawInspection.LotResults[key].Count > 0)
            {
                JawInspection.LotResults[key].Count--;
            }
        }

        private void PlusButton_Click(object sender, RoutedEventArgs e)
        {
            string key = (sender as Button).CommandParameter.ToString();
            JawInspection.LotResults[key].Count++;
        }
        #endregion

        #region 觸發檢測、結批、重置 Timer
        /// <summary>
        /// 觸發相機拍攝 (僅測試拍攝)
        /// </summary>
        /// <param name="sendder"></param>
        /// <param name="e"></param>
        private void TriggerCamera_Click(object sendder, RoutedEventArgs e)
        {
            DateTime t1 = DateTime.Now;

            Debug.WriteLine($"{t1:HH:mm:ss.fff}");

            bool ready = BaslerCam1.Camera.WaitForFrameTriggerReady(100, TimeoutHandling.Return);

            if (ready)
            {
                BaslerCam1.Camera.ExecuteSoftwareTrigger();
                using IGrabResult grabResult = BaslerCam1.Camera.StreamGrabber.RetrieveResult(125, TimeoutHandling.Return);

                if (grabResult?.GrabSucceeded == true)
                {
                    OpenCvSharp.Mat mat = BaslerFunc.GrabResultToMatMono(grabResult);

                    MainWindow.ImageSource1 = mat.ToImageSource();
                }
            }

            ready = BaslerCam2.Camera.WaitForFrameTriggerReady(100, TimeoutHandling.Return);

            if (ready)
            {
                BaslerCam2.Camera.ExecuteSoftwareTrigger();
                using IGrabResult grabResult = BaslerCam2.Camera.StreamGrabber.RetrieveResult(125, TimeoutHandling.Return);

                if (grabResult?.GrabSucceeded == true)
                {
                    OpenCvSharp.Mat mat = BaslerFunc.GrabResultToMatMono(grabResult);
                    MainWindow.ImageSource2 = mat.ToImageSource();
                }
            }


            ready = BaslerCam3.Camera.WaitForFrameTriggerReady(100, TimeoutHandling.Return);

            if (ready)
            {
                BaslerCam3.Camera.ExecuteSoftwareTrigger();
                using IGrabResult grabResult = BaslerCam3.Camera.StreamGrabber.RetrieveResult(125, TimeoutHandling.Return);

                if (grabResult?.GrabSucceeded == true)
                {
                    OpenCvSharp.Mat mat = BaslerFunc.GrabResultToMatMono(grabResult);
                    MainWindow.ImageSource3 = mat.ToImageSource();
                }
            }

            Debug.WriteLine($"{DateTime.Now:HH:mm:ss.fff}; {(DateTime.Now - t1).TotalMilliseconds} ms");
        }

        private void TriggerInspection_Click(object sender, RoutedEventArgs e)
        {
#if UNITTEST
            #region Unit Test
            // TriggerCamera_Click(sender, e);
            DateTime t1 = DateTime.Now;
            //MCAJawM M = new();
            Task.Run(() =>
            {
                //switch (MainWindow.JawType)
                //{
                //    case JawTypes.S:
                //        MCAJawS.CaptureImage(BaslerCam1, BaslerCam2, BaslerCam3);
                //        break;
                //    case JawTypes.M:
                //        MCAJawM.CaptureImage(BaslerCam1, BaslerCam2, BaslerCam3);
                //        break;
                //    case JawTypes.L:
                //        MCAJawL.CaptureImage(BaslerCam1, BaslerCam2, BaslerCam3);
                //        break;
                //}
                MCAJaw_.CaptureImage(BaslerCam1, BaslerCam2, BaslerCam3);
            }).ContinueWith(t =>
            {
                Debug.WriteLine($"It takes {(DateTime.Now - t1).TotalMilliseconds} ms");
            });
            #endregion
#else
            #region Production
            //(sender as Button).IsEnabled = false;
            //Task.Run(async () =>
            //{
            //    for (int i = 0; i < 150; i++)
            //    {
            if (Status != INS_STATUS.READY && Status != INS_STATUS.IDLE) { return; }
            DateTime t1 = DateTime.Now;

            Status = INS_STATUS.INSPECTING;

            // 清空當下 Collection
            JawResultGroup.Collection1.Clear();
            JawResultGroup.Collection2.Clear();
            JawResultGroup.Collection3.Clear();

            // bool b = await
            Task.Run(() =>
            {
                JawMeasurements _jawFullSpecIns = new(JawInspection.LotNumber);
                // MainWindow.JawInsSequence(BaslerCam1, BaslerCam2, BaslerCam3, _jawFullSpecIns);  // deprecated
                // switch (MainWindow.JawType)
                // {
                //     case JawTypes.S:
                //         //MainWindow.MCAJaw.MCAJawS.JawInsSequence(BaslerCam1, BaslerCam2, BaslerCam3, _jawFullSpecIns);
                //         MainWindow.MCAJaw.MCAJaw_.JawInsSequence(BaslerCam1, BaslerCam2, BaslerCam3, _jawFullSpecIns);
                //         break;
                //     case JawTypes.M:
                //         MainWindow.MCAJaw.MCAJawM.JawInsSequence(BaslerCam1, BaslerCam2, BaslerCam3, _jawFullSpecIns);
                //         break;
                //     case JawTypes.L:
                //         MainWindow.MCAJaw.MCAJawL.JawInsSequence(BaslerCam1, BaslerCam2, BaslerCam3, _jawFullSpecIns);
                //         break;
                // }
                MainWindow.MCAJaw.MCAJawPart.JawInsSequence(BaslerCam1, BaslerCam2, BaslerCam3, _jawFullSpecIns);

                return _jawFullSpecIns;
            }).ContinueWith(t =>
            {
                // 判斷是否插入資料庫
                JawMeasurements data = t.Result;
                data.OK = JawResultGroup.Col1Result && JawResultGroup.Col2Result && JawResultGroup.Col3Result;
                data.DateTime = DateTime.Now;
                // MongoAccess.InsertOne("Measurements", data);
                // MongoAccess.InsertOne(nameof(JawCollection.Measurements), data);

                // 檢驗失敗，發出 Alarm
                if (JawResultGroup.Collection1.Count == 0 && JawResultGroup.Collection2.Count == 0 && JawResultGroup.Collection3.Count == 0)
                {
                    Dispatcher.Invoke(() =>
                    {
                        PlayerAlarm.Position = TimeSpan.FromSeconds(0);
                        PlayerAlarm.Play();
                    });
                }
                // 檢驗成功，插入資料庫
                else
                {
                    MainWindow.SystemInfoTab.Env.PlusTotalParts();
                    MongoAccess.InsertOne(nameof(JawCollection.Measurements), data);
                }

                // 檢驗工件 NG，發出 NG 音效
                if (!data.OK)
                {
                    Dispatcher.Invoke(() =>
                    {
                        PlayerNG.Position = TimeSpan.FromSeconds(0);
                        PlayerNG.Play();
                    });
                }

                // 變更狀態為準備檢驗
                Status = INS_STATUS.READY;

                Debug.WriteLine($"One pc takes {(DateTime.Now - t1).TotalMilliseconds} ms");

                return data.OK;
            });
            //        SpinWait.SpinUntil(() => false, 3000);
            //    }
            //}).ContinueWith(t =>
            //{
            //    Dispatcher.Invoke(() =>
            //    {
            //        (sender as Button).IsEnabled = true;
            //    });
            //});
            #endregion
#endif
        }

        private async void FinishLot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool insert = (bool)(await MaterialDesignThemes.Wpf.DialogHost.Show((sender as Button).CommandParameter, "MainDialog", (sender, e) =>
                {
                    Keyboard.ClearFocus();
                    _ = MainWindow.TitleGrid.Focus();
                }, null) ?? false);

                if (insert)
                {
                    // if (MessageBox.Show("是否確認寫入資料庫？", "通知", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    // {
                    // 給予新 ID
                    JawInspection.ObjID = new ObjectId();
                    // 刷新時間
                    JawInspection.DateTime = DateTime.Now;
                    // 插入資料庫
                    // MongoAccess.InsertOne("Lots", JawInspection);
                    // Upsert 資料庫
                    FilterDefinition<JawInspection> filter = Builders<JawInspection>.Filter.Eq(nameof(JawInspection.LotNumber), JawInspection.LotNumber);
                    UpdateDefinition<JawInspection> update = Builders<JawInspection>.Update
                        .Set(nameof(JawInspection.DateTime), JawInspection.DateTime)
                        .Set(nameof(JawInspection.LotResults), JawInspection.LotResults);

                    MongoAccess.UpsertOne(nameof(JawCollection.Lots), filter, update);
                    // 標記這批已插入資料庫
                    JawInspection.SetLotInserted(true);
                }
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.APP, ex.Message);
            }
        }

        /// <summary>
        /// 設定 閒置監聽 
        /// </summary>
        /// <param name="seconds">閒置幾秒後切換狀態</param>
        private void SetIdleTimer(int seconds)
        {
            MainWindow.SystemInfoTab.Env.IdleChanged += SystemInfo_StatusIdle;
            MainWindow.SystemInfoTab.Env.SetIdleTimer(seconds);
#if deprecated
            //Debug.WriteLine($"{DateTime.Now:HH:mm:ss}");
            if (_idleTimer == null)
            {
                _idleTimer = new System.Timers.Timer()
                {
                    Interval = seconds * 1000,  // 1分鐘
                    AutoReset = false,
                };

                _idleTimer.Elapsed += (sender, e) =>
                {
                    // 變更狀態為 Idle
                    Status = INS_STATUS.IDLE;
                    // 開始計時 Idle
                    MainWindow.SystemInfoTab.SystemInfo.StartIdleWatch();
                };
                _idleTimer.Start();
            } 
#endif
        }

        private void SystemInfo_StatusIdle(object sender, Env.IdleChangedEventArgs e)
        {
            if (e.Idle)
            {
                Status = INS_STATUS.IDLE;
            }
            else
            {
                Status = INS_STATUS.READY;
            }
        }

        private void ResetIdleTimer()
        {
            MainWindow.SystemInfoTab.Env.ResetIdlTimer();
#if deprecated
            // 變更狀態為 Ready
            Status = INS_STATUS.READY;
            // 停止計時 Idle
            MainWindow.SystemInfoTab.SystemInfo.StopIdleWatch();
            // Reset Idle Timer
            if (_idleTimer != null)
            {
                _idleTimer.Stop();
                _idleTimer.Start();
            }  
#endif
        }
        #endregion

        #region Property Changed Event
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Dispose Method
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                PlayerNG.Close();
                PlayerAlarm.Close();
                // SoundNG.Dispose();
            }
            _disposed = true;
        }
        #endregion

        #region 測試區
        private void Button_Click(object sender, RoutedEventArgs e)
        {
#if false
            MediaPlayer player = new MediaPlayer();
            player.Open(new Uri(Path.GetFullPath(@".\sound\Alarm.wav")));
            player.Position = TimeSpan.FromSeconds(0);
            player.Play(); 
#endif

            MainWindow.SystemInfoTab.Env.EndSocketServer();

            // Task.Run(() =>
            // {
            //     MainWindow.SystemInfoTab.SystemInfo.PlusTotalParts();
            //     int parts = MainWindow.SystemInfoTab.SystemInfo.TotalParts;
            //     Debug.WriteLine($"{parts}");
            // });

            // JawResultGroup.Collection1.Clear();
            // JawResultGroup.Collection2.Clear();
            // JawResultGroup.Collection3.Clear();

            // JawResultGroup.Collection1.Add(new JawSpec("item1", 20, 10, 30, 22));
            // JawResultGroup.Collection1.Add(new JawSpec("item2", 10, 5, 15, 17));
            // JawResultGroup.Collection1.Add(new JawSpec("Group1", new SolidColorBrush(Colors.DarkCyan), 0, JawSpecGroups.Group1));

            // JawResultGroup.Collection2.Add(new JawSpec("item3", 25, 20, 33, 27));
            // JawResultGroup.Collection2.Add(new JawSpec("item4", 32, 28, 36, 33));

            // JawResultGroup.Collection3.Add(new JawSpec("item5", 55, 52, 57, 56));

#if false
            Task<int> t = await Task.Run(() =>
              {

                  Debug.WriteLine($"first start {DateTime.Now:HH:mm:ss.fff}");
                  SpinWait.SpinUntil(() => false, 1000);
                  Debug.WriteLine($"first end {DateTime.Now:HH:mm:ss.fff}");

                  return 1;
              }).ContinueWith(t =>
              {
                  Debug.WriteLine($"second ID {t.Id} {t.Result}");

                  Debug.WriteLine($"second start {DateTime.Now:HH:mm:ss.fff}");
                  SpinWait.SpinUntil(() => false, 1000);
                  Debug.WriteLine($"second end {DateTime.Now:HH:mm:ss.fff}");

                  return 2;
              }).ContinueWith(async t =>
              {
                  Debug.WriteLine($"Third ID {t.Id} {t.Result}");

                  await Task.WhenAll(Task.Run(() =>
                  {
                      SpinWait.SpinUntil(() => false, 1500);
                      Debug.WriteLine("wait 1500 ms");
                      return 10;
                  }),
                  Task.Run(() =>
                  {
                      SpinWait.SpinUntil(() => false, 3500);
                      Debug.WriteLine("wait 3500 ms");
                      return 11;
                  }),
                  Task.Run(() =>
                  {
                      SpinWait.SpinUntil(() => false, 2500);
                      Debug.WriteLine("wait 2500 ms");
                      return 12;
                  })).ContinueWith(tt =>
                  {

                      Debug.WriteLine($"tt {tt.Status} {tt.Result} {string.Join(",", tt.Result)}");
                  });

                  return 3;
              });

            Debug.WriteLine($"Task {t.Id} {t.Result}"); 
#endif
        }
        #endregion
    }

    #region MCA Jaw Config Definition
    [Obsolete("depreated feature")]
    public class MCAJawConfig_tmp
    {
        public MCAJawConfig_tmp()
        {
            DateTime = DateTime.Now;
        }

        [BsonId]
        public ObjectId ObjID { get; set; }

        [BsonElement(nameof(DataReserveMonths))]
        public ushort DataReserveMonths { get; set; }

        [BsonElement(nameof(DateTime))]
        public DateTime DateTime { get; set; }
    }
    #endregion

    #region Converter
    /// <summary>
    /// MCA Jaw 檢驗狀態顏色轉換器
    /// </summary>
    [ValueConversion(typeof(MCAJaw.INS_STATUS), typeof(SolidColorBrush))]
    public class MCAJawStatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not MCAJaw.INS_STATUS)
            {
                throw new ArgumentException("Value is invalid");
            }

            MCAJaw.INS_STATUS status = (MCAJaw.INS_STATUS)value;
            return status switch
            {
                MCAJaw.INS_STATUS.INIT => new SolidColorBrush(Color.FromArgb(0xbb, 0x21, 0x96, 0xf3)),
                MCAJaw.INS_STATUS.READY => new SolidColorBrush(Color.FromArgb(0xff, 0x4c, 0xAF, 0x50)),
                MCAJaw.INS_STATUS.INSPECTING => new SolidColorBrush(Color.FromArgb(0xff, 0x00, 0x96, 0x88)),
                MCAJaw.INS_STATUS.ERROR => new SolidColorBrush(Color.FromArgb(0xff, 0xE9, 0x1E, 0x63)),
                MCAJaw.INS_STATUS.IDLE => new SolidColorBrush(Color.FromArgb(0xff, 0xFF, 0xC1, 0x07)),
                MCAJaw.INS_STATUS.DEVELOPMENT => new SolidColorBrush(Color.FromArgb(0xbb, 0x2b, 0xa8, 0x9a)),
                MCAJaw.INS_STATUS.UNKNOWN => new SolidColorBrush(Color.FromArgb(0xbb, 0x9E, 0x9E, 0x9E)),

                _ => new SolidColorBrush(Colors.Red),   // Default
            };
            //return new SolidColorBrush(Colors.Red);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion
}
