using Basler.Pylon;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ApexVisIns.content
{
    /// <summary>
    /// MainTab.xaml 的互動邏輯
    /// </summary>
    public partial class MainTab : StackPanel
    {
        #region Resources

        #endregion


        #region Variables
        /// <summary>
        /// 主視窗物件
        /// </summary>
        public MainWindow MainWindow { get; set; }

        /// <summary>
        /// 相機初始化 Flag
        /// </summary>
        private bool CameraInitialized { get; set; }

        /// <summary>
        /// 運動初始化 Flag
        /// </summary>
        private bool MotionInitialized { get; set; }

        /// <summary>
        /// 光源控制器初始化 Flag
        /// </summary>
        private bool LightCtrlsInitiliazed { get; set; }

        /// <summary>
        /// IO 卡初始化 Flag
        /// </summary>
        private bool IoInitialized { get; set; }

        /// <summary>
        /// 初始化工作 CancellationTokenSource
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        #endregion


        #region Local Object (方便 CALL)
        /// <summary>
        /// IO 控制器
        /// </summary>
        private IOController IOController;
        /// <summary>
        /// 24V 光源控制器
        /// </summary>
        private LightController Light24V;
        /// <summary>
        /// 6V 光源控制器
        /// </summary>
        private LightController Light_6V;

        /// <summary>
        /// 相機 1
        /// </summary>
        private BaslerCam BaslerCam1;
        /// <summary>
        /// 相機 2
        /// </summary>
        private BaslerCam BaslerCam2;
        /// <summary>
        /// 相機 3
        /// </summary>
        private BaslerCam BaslerCam3;
        /// <summary>
        /// 相機 4
        /// </summary>
        private BaslerCam BaslerCam4;
        /// <summary>
        /// Motion 控制器
        /// </summary>
        private ServoMotion ServoMotion;
        #endregion

        public MainTab()
        {
            InitializeComponent();
        }

        #region Load & UnLoad
        /// <summary>
        /// Main Tab 載入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            Initializer();            //Initializer();

            MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.APP, "主頁面已載入");

            //Task.Run(() =>
            //{
            //    try
            //    {
            //        //Dispatcher.Invoke(() =>
            //        //{
            //            throw new Exception("測試錯誤");
            //        //});
            //    }
            //    catch (Exception ex)
            //    {
            //        Debug.WriteLine(ex.Message);
            //    }
            //});
        }

        /// <summary>
        /// Main Tab 卸載
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StackPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine("Main Tab Unload");
        }
        #endregion

        #region 初始化
        /// <summary>
        /// 建立初始化工作者
        /// </summary>
        private void Initializer()
        {
            _ = Task<int>.Run(() =>
            {
                // 硬體初始化 // 硬體初始化 // 硬體初始化
                MainWindow.ApexDefect.CurrentStep = 0;  // 步序 : 硬體初始化

                // 等待相機 Enumerator 搜尋完畢
                if (SpinWait.SpinUntil(() => MainWindow.CameraEnumer.InitFlag == LongLifeWorker.InitFlags.Finished, 1000))
                {
                    //_ = SpinWait.SpinUntil(() => MainWindow.CameraEnumer.InitFlag == LongLifeWorker.InitFlags.Finished, 3000);
                    InitCamera();
                }
                else
                {
                    MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.APP, "相機 Enumerator 啟動失敗");
                }

                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    MainWindow.ApexDefect.CurrentStep = -1;
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                }

                //if (SpinWait.SpinUntil(() => MainWindow.ServoMotion.MotionDevices.Count > 0, 1000))
                if (ServoMotion.CheckDllVersion())
                {
                    //_ = SpinWait.SpinUntil(() => MainWindow.ServoMotion.MotionDevices.Count > 0, 3000);
                    InitMotion();
                }
                else
                {
                    //MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.APP, "");
                    MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.APP, "MOTION 控制驅動未安裝或版本不符");
                }

                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    MainWindow.ApexDefect.CurrentStep = -1;
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                }

                // 等待 Com Port 搜尋完畢
                if (SpinWait.SpinUntil(() => MainWindow.LightEnumer.InitFlag == LongLifeWorker.InitFlags.Finished, 1000))
                {
                    //_ = SpinWait.SpinUntil(() => MainWindow.LightEnumer.InitFlag == LongLifeWorker.InitFlags.Finished, 3000);
                    InitLightCtrls();
                }
                else
                {
                    MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.APP, "光源控制 Enumerator 啟動失敗");
                }

                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    MainWindow.ApexDefect.CurrentStep = -1;
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                }

                InitIOCtrl();

                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    MainWindow.ApexDefect.CurrentStep = -1;
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                }

                // _ = SpinWait.SpinUntil(() => false, 1500);
                // MainWindow.MsgInformer.TargetProgressValue = 200;

                // 等待 5 秒
                if (!SpinWait.SpinUntil(() => MainWindow.MsgInformer.ProgressValue == 100, 5 * 1000))
                {
                    // 硬體初始化失敗
                    MainWindow.ApexDefect.StepError = true;
                    return 1;   // 
                }

                // 暫停 Worker 
                MainWindow.CameraEnumer.WorkerPause();
                MainWindow.LightEnumer.WorkerPause();

                return 0;
            }, _cancellationTokenSource.Token).ContinueWith<int>(t =>
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    MainWindow.ApexDefect.CurrentStep = -1;
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                }

                if (t.Result == 0)
                {
                    // 原點復歸 // 原點復歸 // 原點復歸
                    MainWindow.ApexDefect.CurrentStep = 1;  // 步序 : 原點復歸 

                    MotionReturnZero().Wait();

                    if (!MainWindow.ApexDefect.ZeroReturned)
                    {
                        MainWindow.ApexDefect.StepError = true;
                        return 2;
                    }
                    return 0;
                }
                else
                {
                    return t.Result;
                }
            }, _cancellationTokenSource.Token).ContinueWith<int>(t =>
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    MainWindow.ApexDefect.CurrentStep = -1;
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                }

                if (t.Result == 0)
                {
                    // 規格選擇 // 規格選擇 // 規格選擇
                    // 原點復歸成功 // 進入人員操作
                    MainWindow.ApexDefect.CurrentStep = 2;  // 步序 : 規格選擇 
                    return 0;
                }
                else
                {
                    return t.Result;
                }
            }, _cancellationTokenSource.Token);
        }

        /// <summary>
        /// 反初始化，
        /// 相機斷線、ComPort關閉
        /// </summary>
        private void CloseHardware()
        {
            BaslerCam1?.Close();
            BaslerCam2?.Close();
            BaslerCam3?.Close();
            BaslerCam4?.Close();

            // 關閉軸卡
            if (ServoMotion != null && ServoMotion.DeviceOpened)
            {
                ServoMotion.SetAllServoOff();
                ServoMotion.DisableAllTimer();
                ServoMotion.CloseDevice();
            }

            // 關閉 24V 光源
            if (Light24V != null && Light24V.IsComOpen)
            {
                _ = Light24V.TryResetAllValue();
                Light24V.ComClose();
            }

            // 關閉 6V 光源
            if (Light_6V != null && Light_6V.IsComOpen)
            {
                _ = Light_6V.TryResetAllValue();
                Light_6V.ComClose();
            }
        }

        /// <summary>
        /// 相機初始化
        /// </summary>
        [Obsolete("待轉移至新方法")]
        private void InitCamera()
        {
            if (CameraInitialized) { return; }

            // 1. 載入 DeviceConfigs
            // 2. 確認每個 Camera 的Target
            // 3. 開啟相機
            // 4. 載入 UserSet

            string path = @"./devices/device.json";

            if (!File.Exists(path))
            {
                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, "相機設定檔不存在");
            }
            else
            {
                using StreamReader reader = new(path);
                string jsonStr = reader.ReadToEnd();

                if (jsonStr != string.Empty)
                {
                    // 有組態的 camera
                    DeviceConfigBase[] devices = JsonSerializer.Deserialize<DeviceConfigBase[]>(jsonStr);

                    // 在線上的 camera
                    List<BaslerCamInfo> cams = MainWindow.CameraEnumer.CamsSource.ToList();

                    // Devices 排序
                    Array.Sort(devices, (a, b) => a.TargetFeature - b.TargetFeature);

                    foreach (DeviceConfigBase device in devices)
                    {
                        // 確認有組態的 camera 在線上
                        if (cams.Exists(cam => cam.SerialNumber == device.SerialNumber))
                        {
                            switch (device.TargetFeature)
                            {
                                case DeviceConfigBase.TargetFeatureType.Ear:
                                    if (!MainWindow.BaslerCams[0].IsConnected)
                                    {
                                        BaslerCam1 = MainWindow.BaslerCams[0];
                                        _ = Basler_Conntect(BaslerCam1, device.SerialNumber, device.TargetFeature);
                                        MainWindow.MsgInformer.TargetProgressValue += 5;
                                    }
                                    break;
                                case DeviceConfigBase.TargetFeatureType.Window:
                                    if (!MainWindow.BaslerCams[1].IsConnected)
                                    {
                                        BaslerCam2 = MainWindow.BaslerCams[1];
                                        _ = Basler_Conntect(BaslerCam2, device.SerialNumber, device.TargetFeature);
                                        MainWindow.MsgInformer.TargetProgressValue += 5;
                                    }
                                    break;
                                case DeviceConfigBase.TargetFeatureType.Surface1:
                                    if (!MainWindow.BaslerCams[2].IsConnected)
                                    {
                                        BaslerCam3 = MainWindow.BaslerCams[2];
                                        _ = Basler_Conntect(BaslerCam3, device.SerialNumber, device.TargetFeature);
                                        MainWindow.MsgInformer.TargetProgressValue += 5;
                                    }
                                    break;
                                case DeviceConfigBase.TargetFeatureType.Surface2:
                                    if (!MainWindow.BaslerCams[3].IsConnected)
                                    {
                                        BaslerCam4 = MainWindow.BaslerCams[3];
                                        _ = Basler_Conntect(BaslerCam4, device.SerialNumber, device.TargetFeature);
                                        MainWindow.MsgInformer.TargetProgressValue += 5;
                                    }
                                    break;
                                default:
                                    MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, "相機目標特徵未設置");
                                    break;
                            }
                        }
                    }

                    // Motion 初始化旗標
                    CameraInitialized = true;

                    MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.CAMERA, "相機初始化完成");
                }
                else
                {
                    MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.CAMERA, "相機設定檔為空");
                }
            }

            // CameraInitialized = true;

            // Dispatcher.Invoke(() => MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.APP, "相機初始化完成"));
            // MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.CAMERA, "相機初始化完成");
            // MainWindow.ProgressValue += 20;
            // 更新progress value
        }

        /// <summary>
        /// 相機初始化
        /// </summary>
        /// <param name="ct">CancellationToken</param>s
        private void InitCamera(CancellationToken ct)
        {
            if (CameraInitialized) { return; }

            if (ct.IsCancellationRequested)
            {
                ct.ThrowIfCancellationRequested();
            }

            // 1. 載入 json // 2. 確認每個 Camera 之 Target // 3. 開啟相機 // 4. 載入 UserSet

            try
            {
                string path = @"./devices/device.json";

                if (File.Exists(path))
                {
                    using StreamReader reader = new(path);
                    string jsonStr = reader.ReadToEnd();

                    if (jsonStr != string.Empty)
                    {
                        // 組態反序列化
                        DeviceConfigBase[] devices = JsonSerializer.Deserialize<DeviceConfigBase[]>(jsonStr);

                        // 已連線之 Camera
                        List<BaslerCamInfo> cams = MainWindow.CameraEnumer.CamsSource.ToList();

                        // 排序 Devices 
                        Array.Sort(devices, (a, b) => a.TargetFeature - b.TargetFeature);

                        foreach (DeviceConfigBase device in devices)
                        {
                            // 確認 Device 為在線上之 Camera
                            if (cams.Exists(cam => cam.SerialNumber == device.SerialNumber))
                            {
                                switch (device.TargetFeature)
                                {
                                    case DeviceConfigBase.TargetFeatureType.Ear:
                                        if (!MainWindow.BaslerCams[0].IsConnected)
                                        {
                                            BaslerCam1 = MainWindow.BaslerCams[0];
                                            _ = Basler_Conntect(BaslerCam1, device.SerialNumber, device.TargetFeature);
                                            MainWindow.MsgInformer.TargetProgressValue += 5;
                                        }
                                        break;
                                    case DeviceConfigBase.TargetFeatureType.Window:
                                        if (!MainWindow.BaslerCams[1].IsConnected)
                                        {
                                            BaslerCam2 = MainWindow.BaslerCams[1];
                                            _ = Basler_Conntect(BaslerCam2, device.SerialNumber, device.TargetFeature);
                                            MainWindow.MsgInformer.TargetProgressValue += 5;
                                        }
                                        break;
                                    case DeviceConfigBase.TargetFeatureType.Surface1:
                                        if (!MainWindow.BaslerCams[2].IsConnected)
                                        {
                                            BaslerCam3 = MainWindow.BaslerCams[2];
                                            _ = Basler_Conntect(BaslerCam3, device.SerialNumber, device.TargetFeature);
                                            MainWindow.MsgInformer.TargetProgressValue += 5;
                                        }
                                        break;
                                    case DeviceConfigBase.TargetFeatureType.Surface2:
                                        if (!MainWindow.BaslerCams[3].IsConnected)
                                        {
                                            BaslerCam4 = MainWindow.BaslerCams[3];
                                            _ = Basler_Conntect(BaslerCam4, device.SerialNumber, device.TargetFeature);
                                            MainWindow.MsgInformer.TargetProgressValue += 5;
                                        }
                                        break;
                                    case DeviceConfigBase.TargetFeatureType.Null:
                                    default:
                                        MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, "相機目標特徵未設置");
                                        break;
                                }
                            }
                        }

                        // 設置初始化完成旗標
                        CameraInitialized = true;

                        // 確認所有相機已連線
                        if (MainWindow.BaslerCams.All(cam => cam.IsConnected))
                        {
                            MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.CAMERA, "相機初始化完成");
                        }
                        else
                        {
                            //MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.CAMERA, "相機未完全初始化");
                            throw new Exception("相機未完全初始化");
                        }
                    }
                    else
                    {
                        //MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.CAMERA, "相機設定檔為空");
                        throw new Exception("相機設定檔為空");
                    }
                }
                else
                {
                    throw new Exception("相機設定檔不存在");
                    //MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.CAMERA, "相機設定檔不存在");
                }
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.CAMERA, $"相機初始化失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 運動初始化
        /// </summary>
        [Obsolete("待轉移至新方法")]
        private void InitMotion()
        {
            if (MotionInitialized)
            {
                return;
            }
            // 1. 確認驅動 (移動到 Motion Enumer)
            // 2. Get Device
            // 3. 取得 Motor Configs
            // 4. Device Open
            // 5. Axis Open
            // 6. 載入 Config

            ServoMotion = MainWindow.ServoMotion;

            try
            {
                ServoMotion.ListAvailableDevices();
                if (ServoMotion.MotionDevices.Count > 0)
                {
                    // uint deviceNumber = MainWindow.MotionEnumer.GetFirstDeivceNum();
                    uint deviceNumber = MainWindow.ServoMotion.MotionDevices[0].DeviceNumber;

                    if (!ServoMotion.DeviceOpened)
                    {
                        // 開啟軸卡，重置
                        ServoMotion.OpenDevice(deviceNumber);
                        // 確認軸卡開啟
                        if (ServoMotion.DeviceOpened)
                        {
                            // 啟動 Timer 
                            ServoMotion.EnableAllTimer(100);

                            // 重置全部軸錯誤
                            ServoMotion.ResetAllError();

                            // 全部軸 Servo ON
                            ServoMotion.SetAllServoOn();

                            // 全軸 Servo On
                            // foreach (MotionAxis axis in ServoMotion.Axes)
                            // {
                            //     axis.SetServoOn();
                            // }

                            #region 載入 Config
                            string motionPath = $@"{Environment.CurrentDirectory}\motions\motion.json";

                            using StreamReader reader = File.OpenText(motionPath);
                            string jsonStr = reader.ReadToEnd();

                            if (jsonStr != string.Empty)
                            {
                                MotionVelParam[] velParams = JsonSerializer.Deserialize<MotionVelParam[]>(jsonStr);

                                foreach (MotionVelParam item in velParams)
                                {
                                    MotionAxis axis = MainWindow.ServoMotion.Axes.FirstOrDefault(axis => axis.SlaveNumber == item.SlaveNumber);
                                    if (axis != null)
                                    {
                                        axis.LoadFromVelParam(item);
                                        // 寫入參數
                                        axis.SetGearRatio();
                                        axis.SetJogVelParam();
                                        axis.SetHomeVelParam();
                                        axis.SetAxisVelParam();
                                    }
                                }
                            }
                            else
                            {
                                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.MOTION, $"Motion 設定檔為空");
                            }
                            #endregion
                        }
                        else
                        {
                            throw new Exception($"軸卡開啟失敗");
                        }
                    }

                    // 更新 Progress Value
                    MainWindow.MsgInformer.TargetProgressValue += 20;

                    // Motion 初始化旗標
                    MotionInitialized = true;

                    MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.MOTION, "MOTION 控制初始化完成");
                }
                else // 軸卡未連線
                {
                    throw new Exception("找不到控制軸卡");
                }
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, $"MOTION 控制初始化失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 運動控制初始化
        /// </summary>
        /// <param name="ct">CancellationToken</param>
        private void InitMotion(CancellationToken ct)
        {
            if (MotionInitialized) { return; }

            if (ct.IsCancellationRequested)
            {
                ct.ThrowIfCancellationRequested();
            }

            // 1. 列出 Device // 2. 開啟 Device // 3. 重置錯誤 // 4. 全部軸 Servo On // 5. 載入 Configs

            ServoMotion = MainWindow.ServoMotion;

            try
            {
                ServoMotion.ListAvailableDevices();
                // 確認 Device 存在
                if (ServoMotion.MotionDevices.Count > 0)
                {
                    // 開啟第一張 Device
                    uint deviceNumber = MainWindow.ServoMotion.MotionDevices[0].DeviceNumber;

                    if (!ServoMotion.DeviceOpened)
                    {
                        // 開啟軸卡
                        ServoMotion.OpenDevice(deviceNumber);

                        // 確認軸卡開啟
                        if (ServoMotion.DeviceOpened)
                        {
                            // 啟動 Timer 
                            ServoMotion.EnableAllTimer(100);

                            // 重置全部軸錯誤
                            ServoMotion.ResetAllError();

                            // 全部軸 Servo ON
                            ServoMotion.SetAllServoOn();

                            #region 載入 Config
                            string motionPath = $@"{Environment.CurrentDirectory}\motions\motion.json";

                            using StreamReader reader = File.OpenText(motionPath);
                            string jsonStr = reader.ReadToEnd();

                            if (jsonStr != string.Empty)
                            {
                                MotionVelParam[] velParams = JsonSerializer.Deserialize<MotionVelParam[]>(jsonStr);

                                foreach (MotionVelParam item in velParams)
                                {
                                    MotionAxis axis = MainWindow.ServoMotion.Axes.FirstOrDefault(axis => axis.SlaveNumber == item.SlaveNumber);
                                    if (axis != null)
                                    {
                                        axis.LoadFromVelParam(item);
                                        // 寫入參數
                                        axis.SetGearRatio();
                                        axis.SetJogVelParam();
                                        axis.SetHomeVelParam();
                                        axis.SetAxisVelParam();
                                    }
                                }
                            }
                            else
                            {
                                throw new Exception("馬達設定檔為空");
                            }
                            #endregion
                        }
                        else
                        {
                            throw new Exception("軸卡開啟失敗");
                        }
                    } // End of OpenDevice

                    // 更新 Progress Value
                    MainWindow.MsgInformer.TargetProgressValue += 20;

                    // 設置 Motion 初始化完成旗標
                    MotionInitialized = true;

                    // 更新 Informer 
                    MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.MOTION, "運動控制初始化完成");
                }
                else
                {
                    throw new Exception("找不到控制軸卡");
                }
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, $"運動控制初始化失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 馬達原點復歸
        /// </summary>
        private async Task MotionReturnZero()
        {
            if (ServoMotion != null && ServoMotion.DeviceOpened)
            {
                if (SpinWait.SpinUntil(() => ServoMotion.Axes.All(axis => axis.CurrentStatus == "READY"), 3000))
                {
                    // 確認 IO (光電開關)

                    MainWindow.ApexDefect.ZeroReturned = false;
                    MainWindow.ApexDefect.ZeroReturning = true;

                    await ServoMotion.Axes[0].PositiveWayHomeMove(true);

                    MainWindow.ApexDefect.ZeroReturning = false;
                    MainWindow.ApexDefect.ZeroReturned = true;
                }
                else
                {
                    MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, $"伺服軸狀態不允許啟動原點復歸");
                }
            }
        }

        /// <summary>
        /// 光源控制初始化
        /// </summary>
        [Obsolete("待轉移至新方法")]
        private void InitLightCtrls()
        {
            if (LightCtrlsInitiliazed)
            {
                return;
            }

            Light24V = MainWindow.LightCtrls[0];
            Light_6V = MainWindow.LightCtrls[1];

            try
            {
                if (!Light24V.IsComOpen)
                {
                    bool res24V = Light24V.ComOpen("COM1", 115200, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);

                    if (!res24V)
                    {
                        throw new Exception("24V 控制器沒有回應");
                    }
                }
                MainWindow.MsgInformer.TargetProgressValue += 10;     // 更新 Progress Value
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.LIGHT, $"光源控制初始化失敗: {ex.Message}");
            }

            try
            {
                if (!Light_6V.IsComOpen)
                {
                    bool res_6V = Light_6V.ComOpen("COM2", 115200, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);

                    if (!res_6V)
                    {
                        throw new Exception("6V 控制器沒有回應");
                    }
                }
                MainWindow.MsgInformer.TargetProgressValue += 10;     // 更新 Progress Value
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.LIGHT, $"光源控制初始化失敗: {ex.Message}");
            }


            if (Light24V.IsComOpen && Light_6V.IsComOpen)
            {
                MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.LIGHT, "光源控制初始化完成");

                MainWindow.LightEnumer.WorkerPause();
                LightCtrlsInitiliazed = true;

                #region MyRegion
                //Task.Run(() =>
                //{
                //    int b = 0;
                //    while (b++ < 10000)
                //    {
                //        for (int i = 1; i <= Light_6V.ChannelNumber; i++)
                //        {
                //            try
                //            {
                //                if (b % 2 == 0)
                //                {
                //                    Light_6V.SetChannelValue(i, 160);
                //                }
                //                else
                //                {
                //                    Light_6V.SetChannelValue(i, 0);
                //                }
                //            }
                //            catch (Exception ex)
                //            {
                //                Debug.WriteLine(ex.Message);
                //                return;
                //            }
                //        }
                //        SpinWait.SpinUntil(() => false, 33);
                //    }
                //}); 
                #endregion
            }


            //if (Light24V.IsComOpen)
            //{
            //    Task.Run(() =>
            //    {
            //        int a = 0;
            //        while (a++ < 10000)
            //        {
            //            for (int i = 1; i <= Light24V.ChannelNumber; i++)
            //            {
            //                try
            //                {
            //                    if (a % 2 == 0)
            //                    {
            //                        Light24V.SetChannelValue(i, 160);
            //                    }
            //                    else
            //                    {
            //                        Light24V.SetChannelValue(i, 0);
            //                    }
            //                }
            //                catch (Exception ex)
            //                {
            //                    Debug.WriteLine(ex.Message);
            //                    return;
            //                }
            //            }
            //            SpinWait.SpinUntil(() => false, 50);
            //            Debug.WriteLine($"{DateTime.Now:HH:mm:ss.fff}");
            //        }
            //    });
            //}
        }

        /// <summary>
        /// 光源控制初始化
        /// </summary>
        /// <param name="ct">CancellationToken</param>
        private void InitLightCtrls(CancellationToken ct)
        {
            if (LightCtrlsInitiliazed) { return; }

            if (ct.IsCancellationRequested)
            {
                ct.ThrowIfCancellationRequested();
            }

            #region 舊
            Light24V = MainWindow.LightCtrls[0];
            Light_6V = MainWindow.LightCtrls[1];

            try
            {
                if (!Light24V.IsComOpen)
                {
                    bool res24V = Light24V.ComOpen("COM1", 115200, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);

                    if (!res24V)
                    {
                        throw new Exception("24V 控制沒有回應");
                    }
                    MainWindow.MsgInformer.TargetProgressValue += 10;
                }
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.LIGHT, ex.Message);
            }

            try
            {
                if (!Light_6V.IsComOpen)
                {
                    bool res_6V = Light_6V.ComOpen("COM2", 115200, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);

                    if (!res_6V)
                    {
                        throw new Exception("6V 控制沒有回應");
                    }
                    MainWindow.MsgInformer.TargetProgressValue += 10;
                }
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.LIGHT, ex.Message);
            }
            #endregion

            try
            {
                for (int i = 0; i < MainWindow.LightCtrls.Length; i++)
                {
                    switch (i)
                    {
                        case 0:
                            Light24V = MainWindow.LightCtrls[i];
                            break;
                        case 1:
                            Light_6V = MainWindow.LightCtrls[i];
                            break;
                        default:
                            break;
                    }

                    MainWindow.LightCtrls[i].ComOpen($"COM{i + 1}", 115200, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
                }

                if (MainWindow.LightCtrls.All(ctrl => ctrl.IsComOpen))
                {
                    LightCtrlsInitiliazed = true;
                    MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.LIGHT, "光源控制初始化完成");
                }
                else
                {
                    throw new Exception("光源控制未完全初始化");
                }
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.LIGHT, $"光源初始化失敗: {ex.Message}");
            }


            #region 舊
            if (Light24V.IsComOpen && Light_6V.IsComOpen)
            {
                MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.LIGHT, "光源控制初始化完成");

                LightCtrlsInitiliazed = true;
            } 
            #endregion
        }

        /// <summary>
        /// IO 控制器初始化
        /// </summary>
        private void InitIOCtrl()
        {
            if (IoInitialized)
            {
                return;
            }

            IOController = MainWindow.IOController;

            try
            {
                // 確認驅動安裝 // bool IO_DllIsValid = IOController.CheckDllVersion();
                if (IOController.CheckDllVersion())
                {
                    // IOContoller 內部沒有Dispacher
                    //Dispatcher.Invoke(() =>
                    //{
                    // 初始化 DI Control
                    if (!IOController.DiCtrlCreated)
                    {
                        IOController.DigitalInputChanged += Controller_DigitalInputChanged;
                        IOController.InitializeDiCtrl();
                    }
                    //});
                    //MainWindow.ProgressValue += 10; // 更新 Progress Value
                    MainWindow.MsgInformer.TargetProgressValue += 10; // 更新 Progress Value


                    // IOContoller 內部沒有 Dispacher
                    //Dispatcher.Invoke(() =>
                    //{
                    // 初始化 DO Control
                    if (!IOController.DoCtrlCreated)
                    {
                        IOController.InitializeDoCtrl();
                    }
                    //});
                    //MainWindow.ProgressValue += 10; // 更新 Progress Value
                    MainWindow.MsgInformer.TargetProgressValue += 10; // 更新 Progress Value

                    IoInitialized = true;

                    //Dispatcher.Invoke(() => MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.APP, "光源控制器初始化完成"));
                    MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.LIGHT, "IO 控制初始化完成");
                }
                else
                {
                    throw new DllNotFoundException("IO 驅動未安裝或版本不符");
                    //MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.IO, "IO 控制驅動未安裝或版本不符");
                }
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.IO, $"IO 控制初始化失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 中斷事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Controller_DigitalInputChanged(object sender, IOController.DigitalInputChangedEventArgs e)
        {
            Debug.WriteLine($"{e.Port} {e.Bit} {e.Data}");
            //  throw new NotImplementedException();
        }
        #endregion

        #region 原點復歸

        #endregion

        /// <summary>
        /// 規格選擇變更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SpecSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 1. 確認是否原點復歸
            // 2. 確認尺寸計算脈波數

            MessageBox.Show((sender as ListBox).SelectedIndex.ToString());
        }

        /// <summary>
        /// 反初始化按鈕，
        /// 和已連線、開啟之裝置斷開連線
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeinitBtn_Click(object sender, RoutedEventArgs e)
        {
            // 終止 初始化 過程
            _cancellationTokenSource.Cancel();
            // 反初始化
            CloseHardware();
            // 啟動 Enumerator
            MainWindow.CameraEnumer.WorkerResume();
            MainWindow.LightEnumer.WorkerResume();
        }

        /// <summary>
        /// 開始相機抓取
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartCameraGrab_Click(object sender, RoutedEventArgs e)
        {
            foreach (BaslerCam cam in MainWindow.BaslerCams)
            {
                if (cam.IsOpen)
                {
                    Basler_ContinousGrab(cam);
                }
            }
        }

        /// <summary>
        /// 停止相機抓取
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopCameraGrab_Click(object sender, RoutedEventArgs e)
        {
            foreach (BaslerCam cam in MainWindow.BaslerCams)
            {
                if (cam.IsOpen)
                {
                    //Basler_ContinousGrab(cam);
                    cam.Camera.StreamGrabber.Stop();
                }
            }
        }

        /// <summary>
        /// 測試完刪除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [Obsolete("確認硬體連線狀態")]
        private void CheckHwStatus_Click(object sender, RoutedEventArgs e)
        {
            foreach (BaslerCam cam in MainWindow.BaslerCams)
            {
                Debug.WriteLine(cam.IsConnected);
            }

            foreach (LightController light in MainWindow.LightCtrls)
            {
                Debug.WriteLine(light.IsComOpen);
            }

            Debug.WriteLine(MainWindow.ServoMotion.MaxAxisCount);

            // Deinitializer();


            Debug.WriteLine("-------------------Basler Camera Object Comparison-------------------------");

            Debug.WriteLine(BaslerCam1?.Equals(MainWindow.BaslerCams[0]));
            Debug.WriteLine(BaslerCam2?.Equals(MainWindow.BaslerCams[1]));
            Debug.WriteLine(BaslerCam3?.Equals(MainWindow.BaslerCams[2]));
            Debug.WriteLine(BaslerCam4?.Equals(MainWindow.BaslerCams[3]));
        }

        #region Basler 相機事件
        /// <summary>
        /// 相機連線，
        /// 重連線功能 (default times is 3)
        /// </summary>
        /// <param name="cam"></param>
        /// <param name="serialNumber"></param>
        /// <param name="userData"></param>
        /// <returns></returns>
        private bool Basler_Conntect(BaslerCam cam, string serialNumber, object userData)
        {
            int retryCount = 0;

            // retry 連線
            while (!cam.IsOpen)
            {
                if (retryCount > 3)
                {
                    break;
                }

                try
                {
                    // 建立相機
                    cam.CreateCam(serialNumber);
                    // 先更新 SerialNumer，CameraOpened 事件比對時須用到
                    cam.SerialNumber = serialNumber;

                    // 綁定事件
                    cam.Camera.CameraOpened += Camera_CameraOpened;
                    cam.Camera.CameraClosing += Camera_CameraClosing;
                    cam.Camera.CameraClosed += Camera_CameraClosed;

                    // 設定 UserData，ImageGrabbed 事件須用到，用來判斷是哪台相機的影像
                    cam.Camera.StreamGrabber.UserData = userData;

                    // 開啟相機
                    cam.Open();
                    cam.PropertyChange();
                }
                catch (Exception ex)
                {
                    MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, ex.Message);
                    // 重試次數++
                    retryCount++;
                    // 等待 200 ms
                    _ = SpinWait.SpinUntil(() => false, 200);
                }
            }
            return cam.IsOpen;
        }

        /// <summary>
        /// 相機關閉，保留
        /// </summary>
        /// <param name="cam"></param>
        /// <returns></returns>
        private bool Basler_Disconnect(BaslerCam cam)
        {
            try
            {
                cam.Close();

                // GC 回收
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, ex.Message);
            }
            return false;
        }

        private void Basler_ContinousGrab(BaslerCam cam)
        {
            try
            {
                if (!cam.Camera.StreamGrabber.IsGrabbing)
                {
                    cam.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.Off);
                    cam.Camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByStreamGrabber);
                }
            }
            catch (TimeoutException T)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.CAMERA, T.Message);
            }
            catch (InvalidOperationException I)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.CAMERA, I.Message);
            }
            catch (Exception E)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.CAMERA, E.Message);
            }
        }

        private void Camera_CameraOpened(object sender, EventArgs e)
        {
            Camera camera = sender as Camera;

            #region HeartBeat Timeout 30 seconds (相機失去連線後 Timeout 秒數)
            camera.Parameters[PLGigECamera.GevHeartbeatTimeout].SetValue(30 * 1000);
            #endregion

            #region Get basic camera info
            string modelName = camera.CameraInfo[CameraInfoKey.ModelName];
            string serialNumber = camera.CameraInfo[CameraInfoKey.SerialNumber];
            #endregion

            #region 比對 S/N，確認是哪台相機開啟
            // 測試直接比對 MainWidow.BaslerCam

            //BaslerCam baslerCam = null;
            BaslerCam baslerCam = MainWindow.BaslerCams.First(e => e.SerialNumber == serialNumber);

            if (baslerCam == null)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.CAMERA, "相機 S/N 設置有誤");
            }

            //if (serialNumber == BaslerCam1.SerialNumber)
            //{
            //    baslerCam = BaslerCam1;
            //}
            //else if (serialNumber == BaslerCam2.SerialNumber)
            //{
            //    baslerCam = BaslerCam2;
            //}
            //else if (serialNumber == BaslerCam3.SerialNumber)
            //{
            //    baslerCam = BaslerCam3;
            //}
            //else if (serialNumber == BaslerCam4.SerialNumber)
            //{
            //    baslerCam = BaslerCam4;
            //}
            //else
            //{
            //    MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.CAMERA, "相機 S/N 設置有誤");
            //}
            baslerCam.ModelName = modelName;
            #endregion

            #region 相機組態同步
            baslerCam.WidthMax = (int)camera.Parameters[PLGigECamera.WidthMax].GetValue();
            baslerCam.HeightMax = (int)camera.Parameters[PLGigECamera.HeightMax].GetValue();

            baslerCam.OffsetX = (int)camera.Parameters[PLGigECamera.OffsetX].GetValue();
            baslerCam.OffsetY = (int)camera.Parameters[PLGigECamera.OffsetY].GetValue();

            baslerCam.Width = (int)camera.Parameters[PLGigECamera.Width].GetValue();
            baslerCam.Height = (int)camera.Parameters[PLGigECamera.Height].GetValue();

            baslerCam.OffsetXMax = (int)camera.Parameters[PLGigECamera.OffsetX].GetMaximum();
            baslerCam.OffsetYMax = (int)camera.Parameters[PLGigECamera.OffsetY].GetMaximum();

            baslerCam.FPS = camera.Parameters[PLGigECamera.AcquisitionFrameRateAbs].GetValue();

            baslerCam.ExposureTime = camera.Parameters[PLGigECamera.ExposureTimeAbs].GetValue();
            #endregion

            #region 事件綁定
            baslerCam.Camera.StreamGrabber.GrabStarted += StreamGrabber_GrabStarted;
            baslerCam.Camera.StreamGrabber.GrabStopped += StreamGrabber_GrabStopped;
            baslerCam.Camera.StreamGrabber.ImageGrabbed += StreamGrabber_ImageGrabbed;
            #endregion

            // baslerCam.Camera.StreamGrabber.UserData = baslerCam.
            baslerCam.PropertyChange();

            Debug.WriteLine($"UserData : " + baslerCam.Camera.StreamGrabber.UserData + ", LINE: 671");
        }

        private void Camera_CameraClosing(object sender, EventArgs e)
        {
            Camera camera = sender as Camera;

            camera.StreamGrabber.GrabStarted -= StreamGrabber_GrabStarted;
            camera.StreamGrabber.GrabStopped -= StreamGrabber_GrabStopped;
            camera.StreamGrabber.ImageGrabbed -= StreamGrabber_ImageGrabbed;
        }

        private void Camera_CameraClosed(object sender, EventArgs e)
        {

        }

        private void StreamGrabber_GrabStarted(object sender, EventArgs e)
        {
            Debug.WriteLine("Grabber Started");

            string userData = (sender as IStreamGrabber).UserData.ToString();
            Debug.WriteLine(userData);      // userData equal TargetFeature

            // Call PropertyChanged ? IsGrabbing
        }

        private void StreamGrabber_GrabStopped(object sender, GrabStopEventArgs e)
        {
            Debug.WriteLine("Grabber Stoped");

            // Call PropertyChanged ? IsGrabbing
        }

        private void StreamGrabber_ImageGrabbed(object sender, ImageGrabbedEventArgs e)
        {
            IGrabResult grabResult = e.GrabResult;

            if (grabResult.GrabSucceeded)
            {
                // // // // // // // // // // // // // // // // // // // // // // // // // // // // // //
                Mat mat = BaslerFunc.GrabResultToMatMono(grabResult);

                // // // // // // // // // // // // // // // // // // // // // // // // // // // // // //
                // Call PropertyChanged ? Frames

                DeviceConfigBase.TargetFeatureType targetFeatureType = (DeviceConfigBase.TargetFeatureType)e.GrabResult.StreamGrabberUserData;

                switch (targetFeatureType)
                {
                    case DeviceConfigBase.TargetFeatureType.Ear:
                        MainWindow.Dispatcher.Invoke(() => MainWindow.ImageSource1 = mat.ToImageSource());
                        break;
                    case DeviceConfigBase.TargetFeatureType.Window:
                        MainWindow.Dispatcher.Invoke(() => MainWindow.ImageSource2 = mat.ToImageSource());
                        break;
                    case DeviceConfigBase.TargetFeatureType.Surface1:
                        MainWindow.Dispatcher.Invoke(() => MainWindow.ImageSource3 = mat.ToImageSource());
                        break;
                    case DeviceConfigBase.TargetFeatureType.Surface2:
                        MainWindow.Dispatcher.Invoke(() => MainWindow.ImageSource4 = mat.ToImageSource());
                        break;
                }

                // Debug.WriteLine($"{userData}");

                // MainWindow.Dispatcher.Invoke(() =>
                // {
                //     MainWindow.ImageSource = mat.ToImageSource();
                // });
            }

            // throw new NotImplementedException();
        }
        #endregion

        private void StartInspect_Click(object sender, RoutedEventArgs e)
        {
            // 1. ApexDefect Timer Start
            // 2. 啟動相機

            MainWindow.ApexDefect.Start();

        }

        private void StopInspect_Click(object sender, RoutedEventArgs e)
        {
            // 1. ApexDefect Timer Stop
            // 2. 啟動相機

            MainWindow.ApexDefect.Stop();
        }
    }
}
