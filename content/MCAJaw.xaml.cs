using System;
using System.Collections.Generic;
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
using ApexVisIns.Product;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Text.Json;
using Basler.Pylon;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;

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

        private int _jawTab = 0;

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
        #endregion

        #region Local Object (方便呼叫)
        /// <summary>
        /// WISE-4050/LAN IO 控制器
        /// </summary>
        private ModbusTCPIO ModbusTCPIO;
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
        #endregion

        #region Flags
        private bool loaded;
        private bool CameraInitialized { get; set; }
        private bool LightCtrlInitilized { get; set; }
        private bool IOCtrlInitialized { get; set; }
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
            #endregion

            //JawSpecGroup2 = FindResource("SpecGroup") as JawSpecGroup;
            #region 新增假資料

            if (JawSpecGroup.Collection1.Count == 0)
            {
                for (int i = 0; i < 8; i++)
                {
                    JawSpecGroup.Collection1.Add(new JawSpec($"項目 {i}", i, i - 0.02 * i, i + 0.02 * i, i - 0.03 * i, i + 0.03 * i));
                    //JawSpecGroup1.SpecCollection.Add(new JawSpec($"項目 {i}", i, i - 0.02 * i, i + 0.02 * i, i - 0.03 * i, i + 0.03 * i));
                }
            }

            if (JawSpecGroup.Collection2.Count == 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    JawSpecGroup.Collection2.Add(new JawSpec($"項目 {i}", i, i - 0.03 * i, i + 0.03 * i, i - 0.04 * i, i + 0.04 * i));
                }
            }
            #endregion

            // 載入規格設定
            LoadSpecList();



            //if (JawSpecGroup.SpecList.Count == 0)
            //{
            //    for (int i = 0; i < 10; i++)
            //    {
            //        JawSpecGroup.SpecList.Add(new JawSpecSetting()
            //        {
            //            Key = JawSpecGroup.SpecList.Count + 1,
            //            Item = $"項目P{i}",
            //            Note = string.Empty
            //        });
            //    }
            //}

            #region 初始化
            //InitLightCtrl(_cancellationTokenSource.Token).Wait();
            //InitIOCtrl(_cancellationTokenSource.Token).Wait();

            // 硬體初始化
            // InitHardware();
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
                CancellationToken token = _cancellationTokenSource.Token;

                await Task.WhenAll(
                    InitCamera(token),
                    InitLightCtrl(token),
                    InitIOCtrl(token)).ContinueWith(t =>
                    {
                        // 終止初始化，狀態變更為閒置
                        if (token.IsCancellationRequested)
                        {
                            StatusLabel.Text = "閒置";
                            token.ThrowIfCancellationRequested();
                        }

                        //等待進度條滿
                        if (!SpinWait.SpinUntil(() => MainWindow.MsgInformer.ProgressValue == 50, 5 * 1000))
                        {
                            // 硬體初始化失敗
                            return MainWindow.InitFlags.INIT_HARDWARE_FAILED;
                        }

                        return MainWindow.InitFlags.OK;
                    }, token).ContinueWith(t =>
                    {
                        // 終止初始化，狀態變更為閒置
                        if (token.IsCancellationRequested)
                        {
                            StatusLabel.Text = "閒置";
                            token.ThrowIfCancellationRequested();
                        }

                        // 啟動 StreamGrabber & Triiger Mode 

                        if (t.Result == MainWindow.InitFlags.OK)
                        {
                            // 相機開啟 Grabber
                            for (int i = 0; i < MainWindow.BaslerCams.Length; i++)
                            {
                                BaslerCam cam = MainWindow.BaslerCams[i];
                                MainWindow.Basler_StartStreamGrabber(cam);
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
                            MainWindow.Dispatcher.Invoke(() =>
                            {
                                StatusLabel.Text = "初始化完成";
                            });
                        }
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
                                    Debug.WriteLine($"{dev.IP} {dev.TargetFeature}");

                                    switch (dev.TargetFeature)
                                    {
                                        case CameraConfigBase.TargetFeatureType.MCA_Front:
                                            if (!MainWindow.BaslerCams[0].IsConnected)
                                            {
                                                BaslerCam1 = MainWindow.BaslerCams[0];
                                                if (MainWindow.Basler_Connect(BaslerCam1, dev.SerialNumber, dev.TargetFeature, ct))
                                                {
                                                    MainWindow.MsgInformer.TargetProgressValue += 10;
                                                }
                                            }
                                            break;
                                        case CameraConfigBase.TargetFeatureType.MCA_Bottom:
                                            if (!MainWindow.BaslerCams[1].IsConnected)
                                            {
                                                BaslerCam2 = MainWindow.BaslerCams[1];
                                                if (MainWindow.Basler_Connect(BaslerCam2, dev.SerialNumber, dev.TargetFeature, ct))
                                                {
                                                    MainWindow.MsgInformer.TargetProgressValue += 10;
                                                }
                                            }
                                            break;
                                        case CameraConfigBase.TargetFeatureType.MCA_SIDE:
                                            if (!MainWindow.BaslerCams[2].IsConnected)
                                            {
                                                BaslerCam3 = MainWindow.BaslerCams[2];
                                                if (MainWindow.Basler_Connect(BaslerCam3, dev.SerialNumber, dev.TargetFeature, ct))
                                                {
                                                    MainWindow.MsgInformer.TargetProgressValue += 10;
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

                                if (!LightCOM2.Test(out result))
                                {
                                    // 關閉 COM 
                                    LightCOM2.ComClose();
                                    // 拋出異常
                                    throw new Exception($"24V {result}");
                                }
                                else
                                {
                                    // 重置所有通道
                                    LightCOM2.ResetAllChannel();
                                    // 更新 Progress Bar
                                    MainWindow.MsgInformer.TargetProgressValue += 10;
                                }
                                break;
                            default:
                                break;
                        }
                    }

                    if (LightCOM2.IsComOpen)
                    {
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

                if (ct.IsCancellationRequested)
                {
                    ct.ThrowIfCancellationRequested();
                }

                ModbusTCPIO = MainWindow.ModbusTCPIO;

                try
                {
                    ModbusTCPIO.Connect();
                    ModbusTCPIO.IOChanged += ModbusTCPIO_IOChanged;

                    MainWindow.MsgInformer.TargetProgressValue += 10;

                    if (ModbusTCPIO.Conneected)
                    {
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

        private void ModbusTCPIO_IOChanged(object sender, ModbusTCPIO.IOChangedEventArgs e)
        {
            if (e.DI0)
            {
                // 觸發檢驗
                // 要做防彈跳
            }

            Debug.WriteLine($"{e.Value} {e.DI0} {e.DI1} {e.DI2} {e.DI3}");
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


                JawInspection.LotResult.Add("良品", new JawInspection.ResultElement("良品", "", 0));
                foreach (JawSpecSetting element in list)
                {
                    JawSpecGroup.SpecList.Add(element);
                    JawInspection.LotResult.Add(element.Item, new JawInspection.ResultElement(element.Item, element.Note, 0));

                    //JawInspection.LotResult[element.Item].PropertyChange();
                    //Debug.WriteLine($"{element.Item}");
                }
                

                //Debug.WriteLine($"1: {JawInspection.LotResult["0.008-R"].Name}");
                //Debug.WriteLine($"2: {JawInspection.LotResult["0.008-R"].Count}");
            }
            else
            {
                string[] items = new string[] { "0.88-R", "0.88-L", "0.008-R", "0.008-L", "0.013-R", "0.013-L", "0.024-R", "0.024-L", "後開", "前開", "開度差", "0.005MAX", "平面度" };
                double[] center = new double[] { 0.88, 0.88, 0.008, 0.008, 0.013, 0.013, 0.024, 0.024, double.NaN, double.NaN, double.NaN, 0, 0 };
                double[] lowerc = new double[] { 0.0855, 0.0855, 0.006, 0.006, 0.011, 0.011, 0.0225, 0.0225, 0.098, double.NaN, 0.0025, 0, 0 };
                double[] upperc = new double[] { 0.0905, 0.0905, 0.01, 0.01, 0.015, 0.015, 0.0255, 0.0255, 0.101, double.NaN, 0.011, 0.005, 0.007 };
                double[] correc = new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

                for (int i = 0; i < items.Length; i++)
                {
                    int id = JawSpecGroup.SpecList.Count + 1;
                    JawSpecGroup.SpecList.Add(new JawSpecSetting(id, true, items[i], center[i], lowerc[i], upperc[i], correc[i]));
                    //JawInspection.LotResult.Add(items[i], new JawInspection.ResultElement(items[i], "", 0));
                }
                JawInspection.LotResult.Add("良品", new JawInspection.ResultElement("123", "", 0));
                JawInspection.LotResult.Add("0.008-R", new JawInspection.ResultElement("0.008-R", "", 0));


                //Debug.WriteLine($"1: {JawInspection.LotResult.Keys}");
                //foreach (var item in JawInspection.LotResult.Keys)
                //{
                //    Debug.WriteLine($"2: {item}");
                //}
            }
        }
        #endregion


        #region 測試
        private void MinusButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine((sender as Button).CommandParameter);
        }

        private void PlusButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine((sender as Button).CommandParameter);
        }
        #endregion


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





        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        } 
        #endregion



        #region 待刪除
        ModbusTCPIO _modbusTCPIO = new();

     

        /// <summary>
        /// Tcp 連線
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TcpConnect_Click(object sender, RoutedEventArgs e)
        {
            _modbusTCPIO = new()
            {
                IP = "192.168.1.1",
                Port = 502
            };

            _modbusTCPIO.IOChanged += ModbusTCPIO_IOChanged;

            _modbusTCPIO.Connect();

            Debug.WriteLine($"Connected: {_modbusTCPIO.Conneected}");
        }

        /// <summary>
        /// Tcp 斷線
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TcpDisconnect_Click(object sender, RoutedEventArgs e)
        {
            _modbusTCPIO.Disconnect();
        }
        #endregion

       
    }
}
