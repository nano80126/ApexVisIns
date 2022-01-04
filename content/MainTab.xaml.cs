using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
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
using Basler.Pylon;
using OpenCvSharp;

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
            Initializer();

            MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.APP, "主頁面已載入");
        }

        /// <summary>
        /// Main Tab 卸載
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StackPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Main Tab Unload");
        }
        #endregion

        #region 初始化
        /// <summary>
        /// 建立初始化工作者
        /// </summary>
        private void Initializer()
        {
            _ = Task.Run(() =>
            {
                // 等待相機 Enumerator 搜尋完畢
                _ = SpinWait.SpinUntil(() => MainWindow.CameraEnumer.InitFlag == LongLifeWorker.InitFlags.Finished, 3000);

                InitCamera();

                // 等待 Motion Device 搜尋完畢
                _ = SpinWait.SpinUntil(() =>
                    MainWindow.MotionEnumer.InitFlag == LongLifeWorker.InitFlags.Finished ||
                    MainWindow.MotionEnumer.InitFlag == LongLifeWorker.InitFlags.Interrupt, 3000
                );

                if (MainWindow.MotionEnumer.InitFlag == LongLifeWorker.InitFlags.Finished)
                {
                    InitMotion();
                }

                // 等待 Com Port 搜尋完畢
                _ = SpinWait.SpinUntil(() => MainWindow.LightEnumer.InitFlag == LongLifeWorker.InitFlags.Finished, 3000);

                InitLightCtrls();

                //InitIOCtrl(); // 跨執行續

                // _ = SpinWait.SpinUntil(() => false, 1500);
                // MainWindow.MsgInformer.TargetProgressValue = 200;
            });

            // UX Progress Bar
            // 之後整合到 MsgInformer
            //_ = Task.Run(() =>
            //{
            //    while (MainWindow.ProgressValue < 100)
            //    {
            //        MainWindow.ProgressValue += 2;

            //        _ = SpinWait.SpinUntil(() => false, 50);
            //    }
            //});
        }

        /// <summary>
        /// 反初始化，
        /// 相機斷線、ComPort關閉
        /// </summary>
        private void Deinitializer()
        {
            BaslerCam1?.Close();
            BaslerCam2?.Close();
            BaslerCam3?.Close();
            BaslerCam4?.Close();

            if (Light24V != null && Light24V.IsComOpen)
            {
                _ = Light24V.TryResetAllValue();
                Light24V.ComClose();
            }

            if (Light_6V != null && Light_6V.IsComOpen)
            {
                _ = Light_6V.TryResetAllValue();
                Light_6V.ComClose();
            }


            // 關閉 Motion Control
            // 
            
        }

        /// <summary>
        /// 相機初始化，
        /// 須增加錯誤處置
        /// </summary>
        private void InitCamera()
        {
            if (CameraInitialized)
            {
                return;
            }

            // 1. 載入 DeviceConfigs
            // 2. 確認每個 Camera 的Target
            // 3. 開啟相機
            // 4. 載入 UserSet

            string path = @"./devices/device.json";

            if (!File.Exists(path))
            {
                //MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.APP, "相機設定檔不存在");
                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, "相機設定檔不存在");
            }
            else
            {
                using StreamReader reader = new StreamReader(path);
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
                                        //BaslerCam1.SerialNumber = device.SerialNumber;
                                        Basler_Conntect(BaslerCam1, device.SerialNumber, device.TargetFeature.ToString());
                                    }
                                    break;
                                case DeviceConfigBase.TargetFeatureType.Window:
                                    if (!MainWindow.BaslerCams[1].IsConnected)
                                    {
                                        BaslerCam2 = MainWindow.BaslerCams[1];
                                        //BaslerCam2.SerialNumber = device.SerialNumber;
                                        Basler_Conntect(BaslerCam2, device.SerialNumber, device.TargetFeature.ToString());
                                    }
                                    break;
                                case DeviceConfigBase.TargetFeatureType.Surface1:
                                    if (!MainWindow.BaslerCams[2].IsConnected)
                                    {
                                        BaslerCam3 = MainWindow.BaslerCams[2];
                                        //BaslerCam3.SerialNumber = device.SerialNumber;
                                        Basler_Conntect(BaslerCam3, device.SerialNumber, device.TargetFeature.ToString());
                                    }
                                    break;
                                case DeviceConfigBase.TargetFeatureType.Surface2:
                                    if (!MainWindow.BaslerCams[3].IsConnected)
                                    {
                                        BaslerCam4 = MainWindow.BaslerCams[3];
                                        //BaslerCam4.SerialNumber = device.SerialNumber;
                                        Basler_Conntect(BaslerCam4, device.SerialNumber, device.TargetFeature.ToString());
                                    }
                                    break;
                                default:
                                    break;
                            }
                            // 更新 Progress Value
                            //MainWindow.ProgressValue += 5;
                            MainWindow.MsgInformer.TargetProgressValue += 5;
                        }
                    }
                }
                else
                {
                    MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.CAMERA, "相機設定檔為空");
                }
            }

            CameraInitialized = true;

            // Dispatcher.Invoke(() => MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.APP, "相機初始化完成"));
            MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.CAMERA, "相機初始化完成");
            // MainWindow.ProgressValue += 20;
            // 更新progress value
        }

        /// <summary>
        /// 運動初始化
        /// </summary>
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
            // 6. 寫入 Config

            ServoMotion = MainWindow.ServoMotion;

            try
            {
                if (MainWindow.MotionEnumer.Count() > 0)
                {
                    uint deviceNumber = MainWindow.MotionEnumer.GetFirstDeivceNum();

                    if (!ServoMotion.DeviceOpened)
                    {
                        // ServoMotion.OpenDevice(deviceNumber);

                        // try
                        // {

                        // }
                        // catch (Exception)
                        // {
                        //     throw;
                        // }
                    }

                    // 更新 Progress Value
                    MainWindow.MsgInformer.ProgressValue += 20;

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
            finally
            {
                // 這邊需要暫停 Enumer
                MainWindow.MotionEnumer.WorkerPause();
            }
        }

        /// <summary>
        /// 光源控制初始化
        /// </summary>
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
                    //// 重置所有通道
                    //string res24V = Light24V.TryResetAllValue();

                    // if (res24V != string.Empty)
                    // {
                    //     Light24V.ComClose();
                    //     throw new Exception(res24V);
                    // }
                    if (!res24V)
                    {
                        throw new Exception("控制器沒有回應");
                    }
                }
                //MainWindow.ProgressValue += 10;     // 更新 Progress Value
                MainWindow.MsgInformer.ProgressValue += 10;     // 更新 Progress Value

                if (!Light_6V.IsComOpen)
                {
                    bool res_6V = Light_6V.ComOpen("COM2", 115200, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
                    //// 重置所有通道
                    //string res_6V = Light_6V.TryResetAllValue();

                    // if (res_6V != string.Empty)
                    // {
                    //     Light_6V.ComClose();
                    //     throw new Exception(res_6V);
                    // }

                    if (!res_6V)
                    {
                        throw new Exception("控制器沒有回應");
                    }
                }
                //MainWindow.ProgressValue += 10;     // 更新 Progress Value
                MainWindow.MsgInformer.ProgressValue += 10;     // 更新 Progress Value

                LightCtrlsInitiliazed = true;

                //Dispatcher.Invoke(() => MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.APP, "光源控制器初始化完成"));
                MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.LIGHT, "光源控制初始化完成");
            }
            catch (Exception ex)
            {
                //Dispatcher.Invoke(() => MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.APP, $"光源控制器初始化失敗: {ex.Message}"));
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.LIGHT, $"光源控制初始化失敗: {ex.Message}");
            }
            finally
            {
                // 這邊需要停止 LightEnumer
                MainWindow.LightEnumer.WorkerPause();
            }
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

            // 確認驅動安裝
            bool IO_DllIsValid = IOController.CheckDllVersion();

            if (IO_DllIsValid)
            {
                IOController = MainWindow.IOController;

                Dispatcher.Invoke(() =>
                {
                    // 初始化 DI
                    if (!IOController.DiCtrlCreated)
                    {
                        IOController.DigitalInputChanged += Controller_DigitalInputChanged;
                        IOController.InitializeDiCtrl();
                    }
                });
                //MainWindow.ProgressValue += 10; // 更新 Progress Value
                MainWindow.MsgInformer.ProgressValue += 10; // 更新 Progress Value


                Dispatcher.Invoke(() =>
                {
                    // 初始化 DO
                    if (!IOController.DoCtrlCreated)
                    {
                        IOController.InitializeDoCtrl();
                    }
                });
                //MainWindow.ProgressValue += 10; // 更新 Progress Value
                MainWindow.MsgInformer.ProgressValue += 10; // 更新 Progress Value
            }
            else
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.IO, "IO 控制驅動未安裝或版本不符");
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
            Deinitializer();
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

            //Deinitializer();


            Debug.WriteLine("-------------------Basler Camera Object Comparison-------------------------");

            Debug.WriteLine(BaslerCam1?.Equals(MainWindow.BaslerCams[0]));
            Debug.WriteLine(BaslerCam2?.Equals(MainWindow.BaslerCams[1]));
            Debug.WriteLine(BaslerCam3?.Equals(MainWindow.BaslerCams[2]));
            Debug.WriteLine(BaslerCam4?.Equals(MainWindow.BaslerCams[3]));
        }


        #region Basler 相機事件
        private bool Basler_Conntect(BaslerCam cam, string serialNumber, string userData)
        {
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
                // throw;
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
            BaslerCam baslerCam = null;
            if (serialNumber == BaslerCam1.SerialNumber)
            {
                baslerCam = BaslerCam1;
            }
            else if (serialNumber == BaslerCam2.SerialNumber)
            {
                baslerCam = BaslerCam2;
            }
            else if (serialNumber == BaslerCam3.SerialNumber)
            {
                baslerCam = BaslerCam3;
            }
            else if (serialNumber == BaslerCam4.SerialNumber)
            {
                baslerCam = BaslerCam4;
            }
            else
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.CAMERA, "相機 S/N 設置有誤");
            }
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

                MainWindow.Dispatcher.Invoke(() =>
                {
                    MainWindow.ImageSource = mat.ToImageSource();
                });

            }

            // throw new NotImplementedException();
        }

        #endregion

    }
}
