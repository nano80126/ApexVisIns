using ApexVisIns.Product;
using Basler.Pylon;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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

namespace ApexVisIns.content
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
        /// Jaw 規格設定 (包含檢驗結果)
        /// </summary>
        public JawSpecGroup JawSpecGroup { get; set; }

        // public JawSpecGroup JawSpecGroup2 { get; set; }
        #endregion

        #region Variables
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        private int _jawTab;

        private Task _testTask;
        private CancellationTokenSource _testCancelTokenSource;

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

        private string SpecDirectory { get; } = @$"specification";

        private string SpecPath { get; } = $@"MCAJaw.json";
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
        private bool initialzing;
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
            InitSpecSettingPath();
        }

        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            #region 綁定 Resource
            JawInspection = FindResource("JawInspection") as JawInspection;
            JawSpecGroup = FindResource("SpecGroup") as JawSpecGroup;
            if (!JawSpecGroup.SyncBinding) { JawSpecGroup.EnableCollectionBinding(); }
            #endregion

            // 載入規格設定
            LoadSpecList();

            switch (MainWindow.InitMode)
            {
                case MainWindow.InitModes.AUTO:
                    // 硬體初始化
                    if (!initialzing)
                    {
                        InitHardware();
                    }
                    break;
                case MainWindow.InitModes.EDIT:
                    // 只連線 MongoDB
                    _ = Task.Run(() => InitMongoDB(_cancellationTokenSource.Token));
                    ////InitMongoDB(_cancellationTokenSource.Token).Wait();
                    break;
                default:
                    // 保留
                    break;
            }

            #region 初始化
            //InitLightCtrl(_cancellationTokenSource.Token).Wait();
            //InitIOCtrl(_cancellationTokenSource.Token).Wait();

            //byte[] data = new byte[] { 0x01, 0x03, 0x20, 0x00, 0x00, 0x01 };
            //byte[] crc = SerialPortBase.CRC16LH(new byte[] { 0x01, 0x03, 0x20, 0x00, 0x00, 0x01 });
            //Debug.WriteLine($"{string.Join(" , ", crc)}");
            //Debug.WriteLine($"{string.Join(" , ", data.Concat(crc))}");
            #endregion

            if (!loaded)
            {
                MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.APP, "主頁面已載入");
                loaded = true;
            }
        }

        private void StackPanel_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        #region 初始化
        /// <summary>
        /// 硬體初始化
        /// </summary>
        private async void InitHardware()
        {
            try
            {
                // 若正在初始化，直接 return
                if (initialzing)
                {
                    return;
                }

                CancellationToken token = _cancellationTokenSource.Token;

                initialzing = true;

                Status = INS_STATUS.INIT;
                await Task.WhenAll(
                    InitCamera(token),
                    InitLightCtrl(token),
                    InitIOCtrl(token),
                    InitMongoDB(token)).ContinueWith(t =>
                    {
                        // 終止初始化，狀態變更為閒置
                        if (token.IsCancellationRequested)
                        {
                            //StatusLabel.Text = "閒置";
                            Status = INS_STATUS.IDLE;
                            token.ThrowIfCancellationRequested();
                        }

                        // 等待進度條滿，超過 5 秒則 Timeout
                        if (!SpinWait.SpinUntil(() => MainWindow.MsgInformer.ProgressValue >= 100, 5 * 1000))
                        {
                            // 硬體初始化失敗
                            return MainWindow.InitFlags.INIT_HARDWARE_FAILED;
                        }

                        // Debug.WriteLine($"ProgressValue: {MainWindow.MsgInformer.ProgressValue}");
                        // Debug.WriteLine($"TargetProgressValue: {MainWindow.MsgInformer.TargetProgressValue}");

                        return MainWindow.InitFlags.OK;
                    }, token).ContinueWith(t =>
                    {
                        // 終止初始化，狀態變更為閒置
                        if (token.IsCancellationRequested)
                        {
                            //StatusLabel.Text = "閒置";
                            Status = INS_STATUS.IDLE;
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
                        }
                        else
                        {
                            return t.Result;
                        }

                        return MainWindow.InitFlags.OK;
                    }).ContinueWith(t =>
                    {
                        if (t.Result != MainWindow.InitFlags.OK)
                        {
                            MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.APP, $"初始化過程失敗: Error Code {1}");
                        }
                        else
                        {
                            MainWindow.Dispatcher.Invoke(() => Status = INS_STATUS.READY);
                        }
                        initialzing = false;
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
        /// 硬體關閉
        /// </summary>
        private void DisableHardware()
        {
            LightCOM2?.ComClose();
            LightCOM2?.Dispose();

            ModbusTCPIO?.Disconnect();
            ModbusTCPIO?.Dispose();
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
                    // string path = @"./devices/device.json";
                    string path = $@"{Directory.GetCurrentDirectory()}\cameras\camera.json";

                    if (File.Exists(path))
                    {
                        using StreamReader reader = new StreamReader(path);
                        string json = reader.ReadToEnd();

                        if (json != string.Empty)
                        {
                            // json 反序列化
                            CameraConfigBase[] devices = JsonSerializer.Deserialize<CameraConfigBase[]>(json);

                            if (!SpinWait.SpinUntil(() => MainWindow.CameraEnumer.InitFlag == LongLifeWorker.InitFlags.Finished, 3000))
                            {
                                throw new TimeoutException();
                            }
                            
                            // 已連線之 Camera
                            List<BaslerCamInfo> cams = MainWindow.CameraEnumer.CamsSource.ToList();
                            
                            // 排序 Devices
                            Array.Sort(devices, (a, b) => a.TargetFeature - b.TargetFeature);

                            Parallel.ForEach(devices, (dev) =>
                            {
                                Debug.WriteLine($"{dev.SerialNumber} {dev.Model} {dev.TargetFeature}");

                                // 確認 Device 為在線上之 Camera 
                                if (cams.Exists(cam => cam.SerialNumber == dev.SerialNumber))
                                {
                                    // Debug.WriteLine($"{dev.IP} {dev.TargetFeature}");

                                    switch (dev.TargetFeature)
                                    {
                                        case CameraConfigBase.TargetFeatureType.MCA_Front:
                                            if (!MainWindow.BaslerCams[0].IsConnected)
                                            {
                                                BaslerCam1 = MainWindow.BaslerCams[0];
                                                if (MainWindow.Basler_Connect(BaslerCam1, dev.SerialNumber, dev.TargetFeature, ct))
                                                {
                                                    MainWindow.MsgInformer.TargetProgressValue += 17;
                                                }
                                            }
                                            break;
                                        case CameraConfigBase.TargetFeatureType.MCA_Bottom:
                                            if (!MainWindow.BaslerCams[1].IsConnected)
                                            {
                                                BaslerCam2 = MainWindow.BaslerCams[1];
                                                if (MainWindow.Basler_Connect(BaslerCam2, dev.SerialNumber, dev.TargetFeature, ct))
                                                {
                                                    MainWindow.MsgInformer.TargetProgressValue += 17;
                                                }
                                            }
                                            break;
                                        case CameraConfigBase.TargetFeatureType.MCA_SIDE:
                                            if (!MainWindow.BaslerCams[2].IsConnected)
                                            {
                                                BaslerCam3 = MainWindow.BaslerCams[2];
                                                if (MainWindow.Basler_Connect(BaslerCam3, dev.SerialNumber, dev.TargetFeature, ct))
                                                {
                                                    MainWindow.MsgInformer.TargetProgressValue += 17;
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
                        MainWindow.MsgInformer.TargetProgressValue += 17;

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
                        MainWindow.MsgInformer.TargetProgressValue += 17;

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

        private void ModbusTCPIO_IOChanged(object sender, WISE4050.IOChangedEventArgs e)
        {
            if (e.DI0 == false)
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
            //Debug.WriteLine($"{e.Value} {e.DI0} {e.DI1} {e.DI2} {e.DI3}");
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
                    MongoAccess.Connect("MCAJaw", "intaiUser", "mcajaw", 1500);

                    if (MongoAccess.Connected)
                    {
                        MongoAccess.CreateCollection("Lots");
                        MongoAccess.CreateCollection("Spec");
                        MongoAccess.CreateCollection("Auth");

#if false
                        AuthLevel[] authLevels = new AuthLevel[] {
                            new AuthLevel() { Password = "intai", Level = 1 },
                            new AuthLevel() { Password = "qc", Level = 2 },
                            new AuthLevel() { Password = "eng", Level = 5 },
                        };
                        MongoAccess.InsertMany("Auth", authLevels);
#endif

                        MongoAccess.FindAll("Auth", Builders<AuthLevel>.Filter.Empty, out List<AuthLevel> levels);
                        foreach (AuthLevel item in levels)
                        {
                            MainWindow.PasswordDict.Add(item.Password, item.Level);
                            //Debug.WriteLine($"{item.Password} {item.Level}");
                        }

                        MainWindow.MsgInformer.TargetProgressValue += 17;

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
                    // 不切的話，message 太長
                    MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.DATABASE, $"資料庫初始化失敗: {ex.Message.Split(new string[] { "\n", ". " }, StringSplitOptions.RemoveEmptyEntries)[0]}");
                }
            }, ct);
        }
        #endregion


        #region 初始化 SpecList
        private void InitSpecSettingPath()
        {
            string directory = $@"{Directory.GetCurrentDirectory()}\{SpecDirectory}";
            string path = $@"{directory}\{SpecPath}";

            if (!Directory.Exists(directory))
            {
                // 新增路徑
                _ = Directory.CreateDirectory(directory);
                // 新增檔案
                _ = File.CreateText(path);
            }
            else if (!File.Exists(path))
            {
                // 新增檔案
                _ = File.CreateText(path);
            }

            SettingList.JsonPath = path;
        }

        private void LoadSpecList()
        {
            if (JawSpecGroup.SpecList.Count > 0 && JawInspection.LotResults.Count > 0) { return; }
            else
            {
                JawSpecGroup.SpecList.Clear();
                JawInspection.LotResults.Clear();
            }

            string path = $@"{Directory.GetCurrentDirectory()}\{SpecDirectory}\{SpecPath}";

            using StreamReader reader = File.OpenText(path);
            string jsonStr = reader.ReadToEnd();

            if (jsonStr != string.Empty)
            {
                // 反序列化，載入 JSON FILE
                List<JawSpecSetting> list = JsonSerializer.Deserialize<List<JawSpecSetting>>(jsonStr, new JsonSerializerOptions
                {
                    NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                });


                JawInspection.LotResults.Add("good", new JawInspection.ResultElement("良品", "", 0));
                foreach (JawSpecSetting element in list)
                {
                    JawSpecGroup.SpecList.Add(element);
                    JawInspection.LotResults.Add(element.Key, new JawInspection.ResultElement(element.Item, element.Note, 0));
                }
            }
            else // 若規格列表不存在
            {
                string[] keys = new string[] { "0.088R", "0.088L", "0.088T", "0.008R", "0.008L", "0.013R", "0.013L", "0.024R", "0.024L", "back", "front", "bfDiff", "contour", "flatness" };
                string[] items = new string[] { "0.088-R", "0.088-L", "0.088合", "0.008-R", "0.008-L", "0.013-R", "0.013-L", "0.024-R", "0.024-L", "後開", "前開", "開度差", "輪廓度", "平面度" };
                double[] center = new double[] { 0.0880, 0.0880, 0.176, 0.008, 0.008, 0.013, 0.013, 0.0240, 0.0240, double.NaN, double.NaN, double.NaN, 0, 0 };
                double[] lowerc = new double[] { 0.0855, 0.0855, 0.173, 0.006, 0.006, 0.011, 0.011, 0.0225, 0.0225, 0.098, double.NaN, 0.0025, 0, 0 };
                double[] upperc = new double[] { 0.0905, 0.0905, 0.179, 0.010, 0.010, 0.015, 0.015, 0.0255, 0.0255, 0.101, double.NaN, 0.011, 0.005, 0.007 };
                double[] correc = new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                double[] correc2 = new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

                JawInspection.LotResults.Add("good", new JawInspection.ResultElement("良品", "", 0));
                for (int i = 0; i < keys.Length; i++)
                {
                    int id = JawSpecGroup.SpecList.Count + 1;
                    JawSpecGroup.SpecList.Add(new JawSpecSetting(id, true, keys[i], items[i], center[i], lowerc[i], upperc[i], correc[i], correc2[i]));
                    JawInspection.LotResults.Add(keys[i], new JawInspection.ResultElement(items[i], "", 0));
                }
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

        #region 觸發檢測
        private void TriggerInspection_Click(object sender, RoutedEventArgs e)
        {
#if true
            //if (_testTask != null && _testTask.Status == TaskStatus.Running) { return; }

            //Debug.WriteLine($"{_testTask.Status}");
            //_testCancelTokenSource = new();

            if (_testCancelTokenSource != null && !_testCancelTokenSource.IsCancellationRequested)
            {
                _testCancelTokenSource.Cancel();
                return;
            }

            _testCancelTokenSource = new();

            _testTask = Task.Run(async () =>
            {
                for (int i = 0; i < 150; i++)
                {
                    if (_testCancelTokenSource.IsCancellationRequested) { break; }

                    // MainWindow.ListJawParam();
                    DateTime t1 = DateTime.Now;
#endif

                    if (Status != INS_STATUS.READY) { return; }

                    // 清空當下 Collection
                    JawSpecGroup.Collection1.Clear();
                    JawSpecGroup.Collection2.Clear();
                    JawSpecGroup.Collection3.Clear();

                    //Debug.WriteLine($"{DateTime.Now:mm:ss.fff}");

                    Status = INS_STATUS.INSPECTING;

                    //bool b = await
                    await Task.Run(() =>
                    {
                        JawFullSpecIns _jawFullSpecIns = new(JawInspection.LotNumber);
                        MainWindow.JawInsSequence(BaslerCam1, BaslerCam2, BaslerCam3, _jawFullSpecIns);
                        return _jawFullSpecIns;
                    }).ContinueWith(t =>
                    {
                        // 判斷是否插入資料庫
                        //if (true)
                        //{
                        JawFullSpecIns data = t.Result;
                        data.OK = JawSpecGroup.Col1Result && JawSpecGroup.Col2Result && JawSpecGroup.Col3Result;
                        data.DateTime = DateTime.Now;
                        MongoAccess.InsertOne("Spec", data);
                        //string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                        //Debug.WriteLine(json);

                        //return data.OK;
                        //}
                        Status = INS_STATUS.READY;

                        Debug.WriteLine($"One pc takes {(DateTime.Now - t1).TotalMilliseconds} ms");

                        return data.OK;
                        //return 
                    });

                    //if (!b) break;

#if true
                    Debug.WriteLine($"{DateTime.Now:mm:ss.fff}");

                    _ = SpinWait.SpinUntil(() => false || _testCancelTokenSource.IsCancellationRequested, 3000);
                }
            }, _testCancelTokenSource.Token).ContinueWith(t =>
            {
                _testCancelTokenSource.Cancel();
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
                MongoAccess.InsertOne("Lots", JawInspection);
                // 標記這批已插入資料庫
                JawInspection.SetLotInserted(true);
            }

            //TestDic testDic = new TestDic
            //{
            //    _id = new MongoDB.Bson.ObjectId(),
            //    Value = {
            //        { "123", 456 },
            //        { "456", 456 },
            //        { "789", 789 },
            //    }
            //};

            //MongoAccess.InsertOne("Lots", testDic);

            //MongoAccess.FindAll("Lots", Builders<TestDic>.Filter.Empty, out List<TestDic> data);

            //foreach (var item in data)
            //{
            //    Debug.WriteLine($"{string.Join(",", item.Value.Keys)}");

            //    Debug.WriteLine($"{item.Value.Keys} {item.Value.Count}");
            //}

            //Debug.WriteLine($"{JawInspection.LotResults["good"].Name} {JawInspection.LotResults["good"].Note} {JawInspection.LotResults["good"].Count}");
            //JawInspection.LotResults["good"] = new JawInspection.ResultElement("123", "456", 10);
            //Debug.WriteLine($"{JawInspection.LotResults["good"].Name} {JawInspection.LotResults["good"].Note} {JawInspection.LotResults["good"].Count}" );

            //// string json = JsonSerializer.Serialize(JawInspection, new JsonSerializerOptions { WriteIndented = true });
            //// Debug.WriteLine(json);
            //// _ = 
        }
        #endregion


#if false
        /// <summary>
        /// 單張拍攝
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SingleGrab_Click(object sender, RoutedEventArgs e)
        {



        }

        /// <summary>
        /// 啟動連續拍攝
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartContinuousGrab_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < MainWindow.BaslerCams.Length; i++)
            {
                if (!MainWindow.BaslerCams[i].IsGrabbing)
                {
                    MainWindow.Basler_ContinousGrab(MainWindow.BaslerCams[i]);
                }
            }
        }


        /// <summary>
        /// 停止連續拍攝
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopContinuousGrab_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < MainWindow.BaslerCams.Length; i++)
            {
                if (MainWindow.BaslerCams[i].IsGrabbing)
                {
                    MainWindow.Basler_ContinousGrab(MainWindow.BaslerCams[i]);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CameraTrigger_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < MainWindow.BaslerCams.Length; i++)
            {
                BaslerCam cam = MainWindow.BaslerCams[i];

                cam.Camera.ExecuteSoftwareTrigger();

                IGrabResult grabResult = cam.Camera.StreamGrabber.RetrieveResult(250, TimeoutHandling.ThrowException);
                OpenCvSharp.Mat mat = BaslerFunc.GrabResultToMatMono(grabResult);

                OpenCvSharp.Cv2.ImShow($"mat{i}", mat);
                // if (MainWindow.BaslerCams[i].IsGrabbing)
                // {
                //     MainWindow.Basler_ContinousGrab(MainWindow.BaslerCams[i]);
                // }
            }
        }
#endif

        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    
    }

    /// <summary>
    /// MCA Jaw 檢驗狀態顏色轉換器
    /// </summary>
    [ValueConversion(typeof(MCAJaw.INS_STATUS), typeof(SolidColorBrush))]
    public class MCAJawStatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is MCAJaw.INS_STATUS))
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
}
