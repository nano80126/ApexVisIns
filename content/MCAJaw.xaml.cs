#define UNITTEST

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
using MCAJawIns.Mongo;
using MCAJawIns.Product;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MCAJawConfig = MCAJawIns.Mongo.Config;
using MCAJawInfo = MCAJawIns.Mongo.Info;

namespace MCAJawIns.content
{
    /// <summary>
    /// MCAJaw.xaml 的互動邏輯
    /// </summary>
    public partial class MCAJaw : StackPanel, INotifyPropertyChanged
    {
        #region Resources (xaml 內)
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

        #region Variables
        /// <summary>
        /// 初始化用 TokenSource
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        /// <summary>
        /// Main & Spec Setting
        /// </summary>
        private int _jawTab;

        /// <summary>
        /// 
        /// </summary>
        private System.Timers.Timer _idleTimer;

        /// <summary>
        /// NG 音效
        /// </summary>
        private readonly SoundPlayer SoundNG = new SoundPlayer(@".\sound\NG.wav");          // 3 短音
        //private readonly SoundPlayer SoundAlarm = new SoundPlayer(@".\sound\Alarm.wav");    // 4 極短音

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
            [Description("未知")]
            UNKNOWN = 9,
        }

        private INS_STATUS _status = INS_STATUS.UNKNOWN;
        #endregion

        #region Properties
        public MainWindow MainWindow { get; set; }

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

        #region Path
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
        private bool CameraInitialized { get; set; }
        private bool LightCtrlInitilized { get; set; }
        private bool IOCtrlInitialized { get; set; }
        private bool DatabaseInitialized { get; set; }
        #endregion

        public MCAJaw()
        {
            InitializeComponent();

            MainWindow = (MainWindow)Application.Current.MainWindow;
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
                        // 硬體初始化
                        if (!initializing) { InitPeripherals(); }
                        // 設定為自動模式 (轉至 MainWindow.xaml.cs)
                        // MainWindow.SystemInfoTab.SystemInfo.SetMode(true);
                        // 設定閒置計時器
                        SetIdleTimer(60);
                        break;
                    case InitModes.EDIT:
                        // 只連線 MongoDB
                        _ = Task.Run(() => InitMongoDB(_cancellationTokenSource.Token));

                        // _ = Task.Run(() => InitIOCtrl(_cancellationTokenSource.Token)); // delete this line
                        // _ = Task.Run(() => InitCamera(_cancellationTokenSource.Token)); // delete this line

                        // 設定為編輯模式 (轉至 MainWindow.xaml.cs)
                        // MainWindow.SystemInfoTab.SystemInfo.SetMode(false);
                        break;
                    default:
                        // 保留
                        break;
                }
            }
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

                #region TEST
                // Task.Run(() => { }).ContinueWitht=();
                #endregion

                // await Task.Run(() => { 
                // }).ContinueWith( async t=> { 
                //     //await Task.WhenAll()
                // });

                await InitMongoDB(token)
                    .ContinueWith(async t =>
                    {
                        if (token.IsCancellationRequested)
                        {
                            Status = INS_STATUS.UNKNOWN;
                            token.ThrowIfCancellationRequested();
                        }

                        await Task.WhenAll(
                            InitCamera(token),
                            InitLightCtrl(token),
                            InitIOCtrl(token));
                    })
                    .ContinueWith(t =>
                    {
                        // 終止初始化，狀態變更為未知
                        if (token.IsCancellationRequested)
                        {
                            Status = INS_STATUS.UNKNOWN;
                            token.ThrowIfCancellationRequested();
                        }

                        // 載入自動模式時間與檢驗數量
                        FilterDefinition<MCAJawInfo> filter = Builders<MCAJawInfo>.Filter.Eq(nameof(MCAJawInfo.Type), nameof(MCAJawInfo.InfoTypes.System));
                        SortDefinition<MCAJawInfo> sort = Builders<MCAJawInfo>.Sort.Descending(nameof(MCAJawInfo.UpdateTime));

                        MongoAccess.FindOneSort(nameof(JawCollection.Info), filter, sort, out MCAJawInfo info);

                        if (info != null)
                        {
                            string timeString = (string)info.Data[nameof(SystemInfo.TotalAutoTime)];
                            string[] split = timeString.Split(':');

                            TimeSpan timeSpan = new TimeSpan(int.Parse(split[0], CultureInfo.CurrentCulture),
                                int.Parse(split[1], CultureInfo.CurrentCulture),
                                int.Parse(split[2], CultureInfo.CurrentCulture));

                            MainWindow.SystemInfoTab.SystemInfo.SetTotalAutoTime((int)timeSpan.TotalSeconds);
                        }

                        return InitFlags.OK;
                    })
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
                            // 硬體初始化失敗
                            return InitFlags.INIT_PERIPHERALS_FAILED;
                        }

                        return InitFlags.OK;
                    })
                    .ContinueWith(t =>
                    {
                        // 終止初始化，狀態變更為閒置
                        if (token.IsCancellationRequested)
                        {
                            Status = INS_STATUS.UNKNOWN;
                            token.ThrowIfCancellationRequested();
                        }

                        // 啟動相機 StreamGrabber & Trigger Mode
                        if (t.Result == InitFlags.OK)
                        {
                            // 相機開啟 Grabber
                            for (int i = 0; i < MainWindow.BaslerCams.Length; i++)
                            {
                                BaslerCam cam = MainWindow.BaslerCams[i];

                                #region 載入 UserSet1 (可以刪除?)
                                if (!cam.IsGrabbing)
                                {
                                    // 這邊要防呆
                                    // cam.Camera.Parameters[PLGigECamera.UserSetSelector].SetValue("UserSet1");
                                    // cam.Camera.Parameters[PLGigECamera.UserSetLoad].Execute();

                                    // 確認為 UserSet1
                                    // string userSet = cam.Camera.Parameters[PLGigECamera.UserSetSelector].GetValue();
                                    // Debug.WriteLine($"{cam.ModelName} {userSet}");

                                    MainWindow.Basler_StartStreamGrabber(cam);
                                }
                                #endregion
                            }

                            if (!MainWindow.BaslerCams.All(cam => cam.IsTriggerMode))
                            {
                                // 開啟 Trigger Mode 失敗
                                return InitFlags.SET_CAMERA_TRIGGER_MODE_FAILED;
                            }

                            #region 確認相機鏡頭蓋取下
                            foreach (BaslerCam cam in MainWindow.BaslerCams)
                            {
                                OpenCvSharp.Mat mat = MainWindow.Basler_RetrieveResult(cam);
                                OpenCvSharp.Rect roi = new OpenCvSharp.Rect(mat.Width / 3, mat.Height / 3, mat.Width / 3, mat.Height / 3);
                                Methods.GetRoiOtsu(mat, roi, 0, 255, out OpenCvSharp.Mat otsu, out byte threshold);

                                mat.Dispose();
                                otsu.Dispose();

                                if (50 < threshold)
                                {
                                    MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, "相機鏡頭蓋未取下或有遮蔽物");
                                }
                            }
                            #endregion
                        }
                        else
                        {
                            return t.Result;
                        }

                        return InitFlags.OK;
                    })
                    .ContinueWith(t =>
                    {
                        if (t.Result != InitFlags.OK)
                        {
                            MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.APP, $"初始化過程失敗: Error Code {t.Result}");
                        }
                        else
                        {
                            _ = MainWindow.Dispatcher.Invoke(() => Status = INS_STATUS.READY);
                        }
                        initializing = false;
                        initialized = true;
                    });


#if false   // 暫時保留
                await Task.WhenAll(
                     InitCamera(token),
                     InitLightCtrl(token),
                     InitIOCtrl(token),
                     InitMongoDB(token))
                     .ContinueWith(t =>
                     {
                        // 終止初始化，狀態變更為閒置
                        if (token.IsCancellationRequested)
                         {
                            //StatusLabel.Text = "閒置";
                            Status = INS_STATUS.UNKNOWN;
                             token.ThrowIfCancellationRequested();
                         }

                        // 等待進度條滿，超過 5 秒則 Timeout
                        if (!SpinWait.SpinUntil(() => MainWindow.MsgInformer.ProgressValue >= 100, 5 * 1000))
                         {
                            // 硬體初始化失敗
                            return MainWindow.InitFlags.INIT_HARDWARE_FAILED;
                         }

                         return MainWindow.InitFlags.OK;
                     }, token)
                     .ContinueWith(t =>
                     {
                        // 終止初始化，狀態變更為閒置
                        if (token.IsCancellationRequested)
                         {
                            //StatusLabel.Text = "閒置";
                            Status = INS_STATUS.UNKNOWN;
                             token.ThrowIfCancellationRequested();
                         }

                        // 啟動 StreamGrabber & Triiger Mode 
                        if (t.Result == MainWindow.InitFlags.OK)
                         {
                            // 相機開啟 Grabber
                            for (int i = 0; i < MainWindow.BaslerCams.Length; i++)
                             {
                                 BaslerCam cam = MainWindow.BaslerCams[i];
                #region 載入 UserSet1
                                if (!cam.IsGrabbing)
                                 {
                                    // 這邊要防呆
                                    cam.Camera.Parameters[PLGigECamera.UserSetSelector].SetValue("UserSet1");
                                     cam.Camera.Parameters[PLGigECamera.UserSetLoad].Execute();

                                     MainWindow.Basler_StartStreamGrabber(cam);
                                 }
                #endregion
                            }

                             if (!MainWindow.BaslerCams.All(cam => cam.IsTriggerMode))
                             {
                                // 開啟 Trigger Mode 失敗
                                return MainWindow.InitFlags.INIT_HARDWARE_FAILED;
                             }

                #region 確認相機鏡頭蓋取下
                            foreach (BaslerCam cam in MainWindow.BaslerCams)
                             {
                                 OpenCvSharp.Mat mat = MainWindow.Basler_RetrieveResult(cam);
                                 OpenCvSharp.Rect roi = new OpenCvSharp.Rect(mat.Width / 3, mat.Height / 3, mat.Width / 3, mat.Height / 3);
                                 Methods.GetRoiOtsu(mat, roi, 0, 255, out OpenCvSharp.Mat otsu, out byte threshold);

                                 mat.Dispose();
                                 otsu.Dispose();

                                 if (50 < threshold)
                                 {
                                     MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, "相機鏡頭蓋未取下或有遮蔽物");
                                 }
                             }
                #endregion
                        }
                         else
                         {
                             return t.Result;
                         }

                         return MainWindow.InitFlags.OK;
                     })
                     .ContinueWith(t =>
                     {
                         if (t.Result != MainWindow.InitFlags.OK)
                         {
                             MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.APP, $"初始化過程失敗: Error Code {1}");
                         }
                         else
                         {
                             _ = MainWindow.Dispatcher.Invoke(() => Status = INS_STATUS.READY);
                         }
                         initialzing = false;
                     }, token); 
#endif
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
                        MainWindow.SystemInfoTab.SystemInfo.SetMongoVersion(version);

#if false               // 新增使用者、組態
                        MongoAccess.InsertOne("Configs", new MCAJawConfig()
                        {
                            DataReserveMonths = 6,
                        });

                        AuthLevel[] authLevels = new AuthLevel[] {
                            new AuthLevel() { Password = "intai", Level = 1 },
                            new AuthLevel() { Password = "qc", Level = 2 },
                            new AuthLevel() { Password = "eng", Level = 5 },
                        };
                        MongoAccess.InsertMany("Auth", authLevels);
#endif

                        // 載入使用者權限
                        MongoAccess.FindAll(nameof(JawCollection.Auth), Builders<AuthLevel>.Filter.Empty, out List<AuthLevel> levels);
                        foreach (AuthLevel item in levels)
                        {
                            MainWindow.PasswordDict.Add(item.Password, item.Level);
                        }
                        // 讀取 Size Spec 設定
                        LoadSpecList();

                        // 讀取 Size Group Spec 設定
                        LoadSpecGroupList();

#if false  // 移除過期資料
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
                    CameraConfigBase[] configs = Array.Empty<CameraConfigBase>();

                    if (MongoAccess?.Connected == true)
                    {
                        FilterDefinition<MCAJawConfig> filter = Builders<MCAJawConfig>.Filter.Eq(nameof(MCAJawConfig.Type), nameof(MCAJawConfig.ConfigType.CAMERA));
                        MongoAccess.FindOne(nameof(JawCollection.Configs), filter, out MCAJawConfig cfg);

                        if (cfg != null)
                        {
                            // 反序列化
                            configs = cfg.DataArray.Select(x => BsonSerializer.Deserialize<CameraConfigBase>(x.ToBsonDocument())).ToArray();
                        }
                    }
                    else if (File.Exists(path))
                    {
                        using StreamReader reader = new StreamReader(path);
                        string jsonStr = reader.ReadToEnd();

                        if (jsonStr != string.Empty)
                        {
                            // 反序列化
                            configs = JsonSerializer.Deserialize<CameraConfigBase[]>(jsonStr);
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

                    // 等待 Camera Enumerator 初始化
                    if (!SpinWait.SpinUntil(() => MainWindow.CameraEnumer.InitFlag == LongLifeWorker.InitFlags.Finished, 3000)) { throw new TimeoutException(); }

                    // 已連線之 Camera
                    List<BaslerCamInfo> cams = MainWindow.CameraEnumer.CamsSource.ToList();

                    // 排序 Devices
                    Array.Sort(configs, (a, b) => a.TargetFeature - b.TargetFeature);

                    _ = Parallel.ForEach(configs, (dev) =>
                    {
                        // 確認 Device 為在線上之 Camera 
                        if (cams.Exists(cam => cam.SerialNumber == dev.SerialNumber))
                        {
                            // Debug.WriteLine($"{dev.IP} {dev.TargetFeature}");
                            // SpinWait.SpinUntil(() => false, );
                            switch (dev.TargetFeature)
                            {
                                case CameraConfigBase.TargetFeatureType.MCA_Front:
                                    if (!MainWindow.BaslerCams[0].IsConnected)
                                    {
                                        BaslerCam1 = MainWindow.BaslerCams[0];
                                        if (MainWindow.Basler_Connect(BaslerCam1, dev.SerialNumber, dev.TargetFeature, ct))
                                        {
                                            _ = SpinWait.SpinUntil(() => false, 25);
                                            MainWindow.MsgInformer.AdvanceProgressValue(17);
                                        }
                                    }
                                    break;
                                case CameraConfigBase.TargetFeatureType.MCA_Bottom:
                                    if (!MainWindow.BaslerCams[1].IsConnected)
                                    {
                                        BaslerCam2 = MainWindow.BaslerCams[1];
                                        if (MainWindow.Basler_Connect(BaslerCam2, dev.SerialNumber, dev.TargetFeature, ct))
                                        {
                                            _ = SpinWait.SpinUntil(() => false, 50);
                                            MainWindow.MsgInformer.AdvanceProgressValue(17);
                                        }
                                    }
                                    break;
                                case CameraConfigBase.TargetFeatureType.MCA_SIDE:
                                    if (!MainWindow.BaslerCams[2].IsConnected)
                                    {
                                        BaslerCam3 = MainWindow.BaslerCams[2];
                                        if (MainWindow.Basler_Connect(BaslerCam3, dev.SerialNumber, dev.TargetFeature, ct))
                                        {
                                            _ = SpinWait.SpinUntil(() => false, 75);
                                            MainWindow.MsgInformer.AdvanceProgressValue(17);
                                        }
                                    }
                                    break;
                                case CameraConfigBase.TargetFeatureType.Null:
                                    MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.CAMERA, "相機目標特徵未設置");
                                    break;
                                default:
                                    MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.CAMERA, "相機目標特徵設置有誤");
                                    break;
                            }
                        }
                    });

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

                    //  }
                    //  else
                    //  {
                    //      throw new CameraException("相機設定檔為空");
                    //  }

                    //  else
                    //  {
                    //          throw new CameraException("相機設定檔不存在");
                    //  }
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

            Debug.WriteLine($"{e.NewValue} {e.OldValue} line 720");
            // Debug.WriteLine($"{e.NewValue} {e.OldValue} {DateTime.Now:ss.fff}");
            // Debug.WriteLine($"{e.DI0Raising} {e.DI0Falling}");
            // Debug.WriteLine($"{e.DI3Raising} {e.DI3Falling}");
        }
        #endregion

        #region 初始化 SpecList
        /// <summary>
        /// 初始化規格路徑
        /// </summary>
        private void InitSpecSettingDirectory()
        {
            string directory = $@"{Directory.GetCurrentDirectory()}\{SpecDirectory}";

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
        internal void LoadSpecList(bool fromDb = true)
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
                    Debug.WriteLine($"load from mongo, Line: 921");

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
                    }
                    else
                    {
                        // 從本機文件載入
                        LoadSpecList(false);
                    }
                }
                else
                {
                    // 從本機文件載入
                    LoadSpecList(false);
                }
            }
            else
            {
                string path = $@"{Directory.GetCurrentDirectory()}\{SpecDirectory}\{SpecPath}";

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
                    }
                    else
                    {
                        // 初始化尺寸規格
                        InitSizeSpec();
                    }
                }
                else // 若規格列表不存在
                {
                    // 初始化尺寸規格
                    InitSizeSpec();

#if false
                    string[] keys = new string[] { "0.088R", "0.088L", "0.176", "0.008R", "0.008L", "0.013R", "0.013L", "0.024R", "0.024L", "back", "front", "bfDiff", "contour", "contourR", "contourL", "flatness" };
                    string[] items = new string[] { "0.088-R", "0.088-L", "0.176", "0.008-R", "0.008-L", "0.013-R", "0.013-L", "0.024-R", "0.024-L", "後開", "前開", "開度差", "輪廓度", "輪廓度R", "輪廓度L", "平直度" };
                    double[] center = new double[] { 0.0880, 0.0880, 0.176, 0.008, 0.008, 0.013, 0.013, 0.0240, 0.0240, double.NaN, double.NaN, double.NaN, 0, 0, 0, 0 };
                    double[] lowerc = new double[] { 0.0855, 0.0855, 0.173, 0.006, 0.006, 0.011, 0.011, 0.0225, 0.0225, 0.098, double.NaN, 0.0025, 0, 0, 0, 0 };
                    double[] upperc = new double[] { 0.0905, 0.0905, 0.179, 0.010, 0.010, 0.015, 0.015, 0.0255, 0.0255, 0.101, double.NaN, 0.011, 0.005, 0.005, 0.005, 0.007 };
                    // double[] correc = new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                    double[] correc = new double[center.Length];
                    Array.Fill(correc, 0);
                    double[] correc2 = new double[center.Length];
                    Array.Fill(correc2, 0);

                    JawInspection.LotResults.Add("good", new JawInspection.ResultElement("良品", "", 0, true));
                    for (int i = 0; i < keys.Length; i++)
                    {
                        // 調用 Dispacher 變更集合
                        Dispatcher.Invoke(() =>
                        {
                            // 加入尺寸規格表
                            // id = 0 means auto increase by source count
                            // JawSizeSpecList.Source.Add(new JawSpecSetting(id, true, keys[i], items[i], center[i], lowerc[i], upperc[i], correc[i], correc2[i]));
                            JawSpecSetting newItem = new JawSpecSetting(0, true, keys[i], items[i], center[i], lowerc[i], upperc[i], correc[i], correc2[i]);
                            JawSizeSpecList.AddNew(newItem);
                            // 加入批號檢驗結果 (初始化)
                            JawInspection.LotResults.Add(keys[i], new JawInspection.ResultElement(items[i], "", 0, true));
                        });
                    } 
#endif

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
                                //Debug.WriteLine($"{JawSizeSpecList.Groups[i].Color}");
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
#if true
                string path = $@"{Directory.GetCurrentDirectory()}\{SpecDirectory}\{SpecGroupPath}";

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

                                Debug.WriteLine($"{JawSizeSpecList.Groups[i].ColorString}");
                            }
                        });
                        JawSizeSpecList.GroupSave();
                    }
                } 
#endif
            }
        }

        /// <summary>
        /// 初始化尺寸規格設定
        /// </summary>
        private void InitSizeSpec()
        {
            string[] keys = new string[] { "0.088R", "0.088L", "0.176", "0.008R", "0.008L", "0.013R", "0.013L", "0.024R", "0.024L", "back", "front", "bfDiff", "contour", "contourR", "contourL", "flatness" };
            string[] items = new string[] { "0.088-R", "0.088-L", "0.176", "0.008-R", "0.008-L", "0.013-R", "0.013-L", "0.024-R", "0.024-L", "後開", "前開", "開度差", "輪廓度", "輪廓度R", "輪廓度L", "平直度" };
            double[] center = new double[] { 0.0880, 0.0880, 0.176, 0.008, 0.008, 0.013, 0.013, 0.0240, 0.0240, double.NaN, double.NaN, double.NaN, 0, 0, 0, 0 };
            double[] lowerc = new double[] { 0.0855, 0.0855, 0.173, 0.006, 0.006, 0.011, 0.011, 0.0225, 0.0225, 0.098, double.NaN, 0.0025, 0, 0, 0, 0 };
            double[] upperc = new double[] { 0.0905, 0.0905, 0.179, 0.010, 0.010, 0.015, 0.015, 0.0255, 0.0255, 0.101, double.NaN, 0.011, 0.005, 0.005, 0.005, 0.007 };
            // double[] correc = new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            double[] correc = new double[center.Length];
            Array.Fill(correc, 0);
            double[] correc2 = new double[center.Length];
            Array.Fill(correc2, 0);

            JawInspection.LotResults.Add("good", new JawInspection.ResultElement("良品", "", 0, true));
            for (int i = 0; i < keys.Length; i++)
            {
                // 調用 Dispacher 變更集合
                Dispatcher.Invoke(() =>
                {
                    // 加入尺寸規格表
                    // id = 0 means auto increase by source count
                    // JawSizeSpecList.Source.Add(new JawSpecSetting(id, true, keys[i], items[i], center[i], lowerc[i], upperc[i], correc[i], correc2[i]));
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
            }
        }

        private void ResetCount_Click(object sender, RoutedEventArgs e)
        {
            if (JawInspection.LotNumberChecked && !JawInspection.LotInserted)
            {
                if (MessageBox.Show("該批資料尚未儲存，是否確定歸零數量？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    return;
                }
            }

            // JawInspection.ObjID = new MongoDB.Bson.ObjectId();
            foreach (string key in JawInspection.LotResults.Keys)
            {
                JawInspection.LotResults[key].Count = 0;
            }
            //JawInspection.SetLotInserted(false);
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
        }

        private void TriggerInspection_Click(object sender, RoutedEventArgs e)
        {
#if UNITTEST
            TriggerCamera_Click(sender, e);
#else
            if (Status != INS_STATUS.READY) { return; }
            DateTime t1 = DateTime.Now;

            // 清空當下 Collection
            JawResultGroup.Collection1.Clear();
            JawResultGroup.Collection2.Clear();
            JawResultGroup.Collection3.Clear();

            //Debug.WriteLine($"{DateTime.Now:mm:ss.fff}");

            Status = INS_STATUS.INSPECTING;

            //bool b = await
            Task.Run(() =>
            {
                JawMeasurements _jawFullSpecIns = new(JawInspection.LotNumber);
                MainWindow.JawInsSequence(BaslerCam1, BaslerCam2, BaslerCam3, _jawFullSpecIns);
                return _jawFullSpecIns;
            }).ContinueWith(t =>
            {
                // 判斷是否插入資料庫
                //if (true)
                //{
                JawMeasurements data = t.Result;
                data.OK = JawResultGroup.Col1Result && JawResultGroup.Col2Result && JawResultGroup.Col3Result;
                data.DateTime = DateTime.Now;
                MongoAccess.InsertOne("Measurements", data);
                //string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                //Debug.WriteLine(json);

                //return data.OK;
                //}
                Status = INS_STATUS.READY;

                Debug.WriteLine($"One pc takes {(DateTime.Now - t1).TotalMilliseconds} ms");

                if (!data.OK) { SoundNG.Play(); }

                return data.OK;
            });
#endif
        }

        private void FinishLot_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("是否確認寫入資料庫？", "通知", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
            {
                // 給予新 ID
                JawInspection.ObjID = new MongoDB.Bson.ObjectId();
                // 刷新時間
                JawInspection.DateTime = DateTime.Now;
                // 插入資料庫
                //MongoAccess.InsertOne("Lots", JawInspection);
                // Upsert 資料庫
                FilterDefinition<JawInspection> filter = Builders<JawInspection>.Filter.Eq("LotNumber", JawInspection.LotNumber);
                UpdateDefinition<JawInspection> update = Builders<JawInspection>.Update.Set("DateTime", JawInspection.DateTime).Set("LotResults", JawInspection.LotResults);
                MongoAccess.UpsertOne("Lots", filter, update);
                // 標記這批已插入資料庫
                JawInspection.SetLotInserted(true);
            }
        }

        /// <summary>
        /// 設定 閒置監聽 
        /// </summary>
        /// <param name="seconds">閒置幾秒後切換狀態</param>
        private void SetIdleTimer(int seconds)
        {
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
        }

        private void ResetIdleTimer()
        {
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
        }
        #endregion

        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            //JawResultGroup.Collection1.Add(new JawSpec("ABC", 0.5, 0.3, 0.7, 0.65));
            //JawResultGroup.Collection2.Add(new JawSpec("DEF", 0.8, 0.3, 1.3, 0.65));
            //JawResultGroup.Collection3.Add(new JawSpec("DDD", 0.005, 0.003, 0.007, 0.0075));

            // JawSizeSpecList.Groups[1].Color = Brushes.Transparent;
            // JawSizeSpecList.Groups[2].Color = Brushes.Transparent;
            // JawSizeSpecList.Groups[3].Color = Brushes.Transparent;
            // JawSizeSpecList.Groups[0].PropertyChange("Color");
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
        }
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
