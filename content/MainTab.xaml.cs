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


        #region 
        private IOController IOController;

        private LightController Light24V;

        private LightController Light_6V;

        private BaslerCam BaslerCam1;
        private BaslerCam BaslerCam2;
        private BaslerCam BaslerCam3;
        private BaslerCam BaslerCam4;
        #endregion


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

            //InitLighCtrls();

            //InitMotion();

            //InitCamera();

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
                // 等待相機連線
                SpinWait.SpinUntil(() => MainWindow.CameraEnumer.CamsSource.Count > 0, 3000);

                InitCamera();

                InitMotion();

                InitLighCtrls();

                InitIOCtrl();
            });
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

            if (Light24V != null)
            {
                _ = Light24V.TryResetAllValue();
                Light24V.ComClose();
            }

            if (Light_6V != null)
            {
                _ = Light_6V.TryResetAllValue();
                Light_6V.ComClose();
            }
        }
             

        /// <summary>
        /// 相機初始化，
        /// 須增加錯誤處置
        /// </summary>
        private void InitCamera()
        {
            // 1. 載入 DeviceConfigs
            // 2. 確認每個 Camera 的Target
            // 3. 開啟相機
            // 4. 載入 UserSet

            string path = @"./devices/device.json";

            if (!File.Exists(path))
            {
                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.APP, "相機設定檔不存在");

                Debug.WriteLine("device config is not existing");
            }
            else
            {
                using StreamReader reader = new StreamReader(path);
                string jsonStr = reader.ReadToEnd();

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
                        Debug.WriteLine($"S/N:{device.SerialNumber}");

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
                    }
                }
            }

            //Dispatcher.Invoke(() => MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.APP, "相機初始化完成"));
            MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.APP, "相機初始化完成");
            // 更新progress value
        }

        /// <summary>
        /// 運動初始化
        /// </summary>
        private void InitMotion()
        {
            // 1. 確認驅動
            // 2. Get Device
            // 3. 取得 Motor Configs
            // 4. Device Open
            // 5. Axis Open
            // 6. 寫入 Config

            bool DllIsValid = ServoMotion.CheckDllVersion();

            if (DllIsValid)
            {



            }

            //Dispatcher.Invoke(() => MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.APP, "運動軸初始化完成"));
            MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.APP, "運動軸初始化完成");
            // 更新progress value
        }

        /// <summary>
        /// 光源控制初始化
        /// </summary>
        private void InitLighCtrls()
        {
            Light24V = MainWindow.LightCtrls[0];
            Light_6V = MainWindow.LightCtrls[1];


            try
            {
                if (!Light24V.IsComOpen)
                {
                    Light24V.ComOpen("COM1", 115200, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
                    // 重置所有通道
                    string res24V = Light24V.TryResetAllValue();

                    if (res24V != string.Empty)
                    {
                        Light24V.ComClose();
                        throw new Exception(res24V);
                    }
                }

                if (!Light_6V.IsComOpen)
                {
                    Light_6V.ComOpen("COM2", 115200, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
                    // 重置所有通道
                    string res_6V = Light_6V.TryResetAllValue();

                    if (res_6V != string.Empty)
                    {
                        Light_6V.ComClose();
                        throw new Exception(res_6V);
                    }
                }

                LightCtrlsInitiliazed = true;

                //Dispatcher.Invoke(() => MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.APP, "光源控制器初始化完成"));
                MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.APP, "光源控制器初始化完成");
            }
            catch (Exception ex)
            {
                //Dispatcher.Invoke(() => MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.APP, $"光源控制器初始化失敗: {ex.Message}"));
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.APP, $"光源控制器初始化失敗: {ex.Message}");
            }
            finally
            {
                // 這邊需要停止 LightEnumer

                // 更新progress value
            }
        }

        /// <summary>
        /// IO 控制器初始化
        /// </summary>
        private void InitIOCtrl()
        {
            bool IO_DllIsValid = IOController.CheckDllVersion();

            if (IO_DllIsValid)
            {
                IOController = MainWindow.IOController;

                // 初始化 DI
                if (!IOController.DiCtrlCreated)
                {
                    IOController.DigitalInputChanged += Controller_DigitalInputChanged;
                    IOController.InitializeDiCtrl();
                }

                // 初始化 DO
                if (!IOController.DoCtrlCreated)
                {
                    IOController.InitializeDoCtrl();
                }
            }
            else
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.APP, "IO 控制驅動未安裝或版本不符");
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
                Debug.WriteLine(ex.Message);

                
                // MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, ex.Message);
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

            Debug.WriteLine($"UserData : " + baslerCam.Camera.StreamGrabber.UserData);
        }

        private void Camera_CameraClosing(object sender, EventArgs e)
        {


            // throw new NotImplementedException();
        }

        private void Camera_CameraClosed(object sender, EventArgs e)
        {



            // throw new NotImplementedException();
        }

        private void StreamGrabber_GrabStarted(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void StreamGrabber_GrabStopped(object sender, GrabStopEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void StreamGrabber_ImageGrabbed(object sender, ImageGrabbedEventArgs e)
        {
            throw new NotImplementedException();
        }
        #endregion

      
    }
}
