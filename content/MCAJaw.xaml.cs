﻿using System;
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


namespace ApexVisIns.content
{
    /// <summary>
    /// MCAJaw.xaml 的互動邏輯
    /// </summary>
    public partial class MCAJaw : StackPanel
    {
        #region Resources
        public JawSpecGroup JawSpecGroup { get; set; }
        #endregion

        #region Variables
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        #endregion

        #region Properties
        public MainWindow MainWindow { get; set; }
        #endregion

        #region Local Object (方便呼叫)
        /// <summary>
        /// WISE-4050/LAN IO 控制器
        /// </summary>
        private ModbusTCPIO ModbusTCPIO;
        /// <summary>
        /// 24V 光源控制器
        /// </summary>
        private LightSerial Light24V;
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
        }

        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            JawSpecGroup = FindResource("SpecGroup") as JawSpecGroup;

            if (JawSpecGroup.SpecCollection.Count == 0)
            {
                for (int i = 0; i < 10; i++)
                {
                    JawSpecGroup.SpecCollection.Add(new JawSpec($"項目 {i}", i, i - 0.02 * i, i + 0.02 * i, i - 0.03 * i, i + 0.03 * i));
                }
            }


            InitIOCtrl(_cancellationTokenSource.Token).Wait();

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


                    }, token).ContinueWith(t =>
                    {


                        MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.APP, $"初始化過程失敗: Error Code {1}");
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
                    string path = @"./devices/device.json";

                    if (File.Exists(path))
                    {
                        using StreamReader reader = new StreamReader(path);
                        string json = reader.ReadToEnd();

                        if (json != string.Empty)
                        {
                            // json 反序列化
                            DeviceConfigBase[] devices = JsonSerializer.Deserialize<DeviceConfigBase[]>(json);

                            if (!SpinWait.SpinUntil(() => MainWindow.CameraEnumer.InitFlag == LongLifeWorker.InitFlags.Finished, 3000))
                            {
                                throw new TimeoutException();
                            }

                            // 已連線之 Camera
                            List<BaslerCamInfo> cams = MainWindow.CameraEnumer.CamsSource.ToList();

                            // 排序 Devices
                            Array.Sort(devices, (a, b) => a.TargetFeature - b.TargetFeature);

                            Parallel.ForEach(devices, (dev) => {
                                // 確認 Device 為在線上之 Camera 
                                if (cams.Exists(cam => cam.SerialNumber == dev.SerialNumber))
                                {
                                    switch (dev.TargetFeature)
                                    {
                                        case DeviceConfigBase.TargetFeatureType.MCA_Front:
                                            if (!MainWindow.BaslerCams[0].IsConnected)
                                            {
                                                BaslerCam1 = MainWindow.BaslerCams[0];


                                            }
                                            break;
                                        case DeviceConfigBase.TargetFeatureType.MCA_Bottom:
                                            if (!MainWindow.BaslerCams[1].IsConnected)
                                            {
                                                BaslerCam2 = MainWindow.BaslerCams[1];


                                            }
                                            break;
                                        case DeviceConfigBase.TargetFeatureType.MCA_SIDE:
                                            if (!MainWindow.BaslerCams[2].IsConnected)
                                            {
                                                BaslerCam3 = MainWindow.BaslerCams[2];


                                            }
                                            break;
                                        case DeviceConfigBase.TargetFeatureType.Null:
                                            MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.CAMERA, "相機目標特徵未設置");
                                            break;
                                        default:
                                            MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.CAMERA, "相機目標特徵設置有誤");
                                            break;
                                    }
                                }
                            });

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
                if (IOCtrlInitialized) { return; }

                if (ct.IsCancellationRequested) { ct.ThrowIfCancellationRequested(); }

                try
                {
                    if (!SpinWait.SpinUntil(() => MainWindow.LightEnumer.InitFlag == LongLifeWorker.InitFlags.Finished, 3000))
                    {
                        throw new TimeoutException("COM Port 列舉逾時");
                    }

                    string result = string.Empty;
                    foreach (LightSerial ctrl in MainWindow.LightCtrls)
                    {
                        switch (ctrl.ComPort)
                        {
                            case "COM1":
                                Light24V = ctrl;
                                Light24V.ComOpen(115200, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);

                                if (!Light24V.Test(out result))
                                {
                                    // 關閉 COM 
                                    Light24V.ComClose();
                                    // 拋出異常
                                    throw new Exception($"24V {result}");
                                }
                                else
                                {
                                    // 重置所有通道
                                    Light24V.ResetAllChannel();
                                    // 更新 Progress Bar
                                    MainWindow.MsgInformer.TargetProgressValue += 10;
                                }
                                break;
                            default:
                                break;
                        }
                    }

                    LightCtrlInitilized = true;

                    if (Light24V.IsComOpen)
                    {
                        MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.LIGHT, "光源控制初始化完成");
                    }
                    else
                    {
                        throw new ShouldNotBeReachedException("此區塊不應該到達");
                    }
                }
                catch (Exception ex)
                {
                    MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.LIGHT, $"光源初始化失敗: {ex.Message}");
                }
            });
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

                    if (ModbusTCPIO.Conneected)
                    {
                        IOCtrlInitialized = true;
                        MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.IO, "IO 控制初始化完成");
                    }
                }
                catch (Exception ex)
                {

                    MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.IO, $"IO 控制初始化失敗: {ex.Message}");
                }
            });
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


        private void MinusButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine((sender as Button).CommandParameter);
        }

        private void PlusButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine((sender as Button).CommandParameter);
        }

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
    }
}
