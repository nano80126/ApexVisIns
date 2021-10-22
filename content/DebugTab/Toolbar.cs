using Basler.Pylon;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ApexVisIns.content
{
    public partial class DebugTab : StackPanel
    {
        #region Toolbar 事件
        private void CamSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            int idx = comboBox.SelectedIndex;

            Toolbar.DataContext = MainWindow.BaslerCams[idx];
            Cam = MainWindow.BaslerCams[idx];
            // ConfigPanel.DataContext = MainWindow.BaslerCams[idx];
        }

        private void CamConnect_Checked(object sender, RoutedEventArgs e)
        {
            ToggleButton Toggle = sender as ToggleButton;

            BaslerCamInfo info = CamSelector.SelectedItem as BaslerCamInfo;
            int idx = CamSelector.SelectedIndex;

            //Toggle.IsChecked = BaslerFunc.Basler_Connect(MainWindow.BaslerCams[idx], info.SerialNumber);
            Toggle.IsChecked = Basler_Connect(MainWindow.BaslerCams[idx], info.SerialNumber);
            // Toggle.IsChecked = BaslerFunc.Basler_Connect(MainWindow.BaslerCam, info.SerialNumber);
        }

        private void CamConnect_Unchecked(object sender, RoutedEventArgs e)
        {
            ToggleButton Toggle = sender as ToggleButton;
            int idx = CamSelector.SelectedIndex;

            //Toggle.IsChecked = BaslerFunc.Basler_Disconnect(MainWindow.BaslerCams[idx]);
            Toggle.IsChecked = Basler_Disconnect(MainWindow.BaslerCams[idx]);
            // Toggle.IsChecked = BaslerFunc.Basler_Disconnect(MainWindow.BaslerCam);
        }

        private void SingleShot_Click(object sender, RoutedEventArgs e)
        {
            Basler_SingleGrab(Cam);
        }

        private void ContinouseShot_Click(object sender, RoutedEventArgs e)
        {
            Basler_ContinousGrab(Cam);
        }

        private void ToggleCrosshair_Click(object sender, RoutedEventArgs e)
        {
            Crosshair.Enable = !Crosshair.Enable;
        }

        private void ToggleAssistRect_Click(object sender, RoutedEventArgs e)
        {
            AssistRect.Enable = !AssistRect.Enable;
        }

        private void RatioTextblock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {




        }
        #endregion


        #region Toolbar 引發之 Basler Camera Event

        private bool Basler_Connect(BaslerCam cam, string serialNumber)
        {
            try
            {
                // 建立相機
                cam.CreateCam(serialNumber);

                // 綁定事件
                cam.Camera.CameraOpened += Camera_CameraOpened;
                cam.Camera.CameraClosing += Camera_CameraClosing;
                cam.Camera.CameraClosed += Camera_CameraClosed;

                // 開啟相機
                cam.Open();
                cam.PropertyChange();
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.C, ex.Message, MsgInformer.Message.MessageType.Warning);
                throw;
            }
            return cam.IsOpen;
        }

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
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.C, ex.Message, MsgInformer.Message.MessageType.Warning);
                //throw;
            }
            return false;
        }


        public void Basler_SingleGrab(BaslerCam cam)
        {
            try
            {
                if (!cam.Camera.StreamGrabber.IsGrabbing)
                {
                    cam.Camera.StreamGrabber.Start(1, GrabStrategy.LatestImages, GrabLoop.ProvidedByUser);

                    cam.Camera.ExecuteSoftwareTrigger();
                    _ = cam.Camera.StreamGrabber.RetrieveResult(250, TimeoutHandling.ThrowException);
                }
            }
            catch (TimeoutException T)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.C, T.Message, MsgInformer.Message.MessageType.Warning);
            }
            catch (InvalidOperationException I)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.C, I.Message, MsgInformer.Message.MessageType.Warning);
            }
            catch (Exception E)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.C, E.Message, MsgInformer.Message.MessageType.Warning);
            }
        }


        public void Basler_ContinousGrab(BaslerCam cam)
        {
            try
            {
                if (!cam.Camera.StreamGrabber.IsGrabbing)
                {
                    cam.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.Off);
                    cam.Camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByStreamGrabber);
                }
                else
                {
                    cam.Camera.StreamGrabber.Stop();
                }
            }
            catch (TimeoutException T)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.C, T.Message, MsgInformer.Message.MessageType.Warning);
            }
            catch (InvalidOperationException I)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.C, I.Message, MsgInformer.Message.MessageType.Warning);
            }
            catch (Exception E)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.C, E.Message, MsgInformer.Message.MessageType.Warning);
            }
        }


        #region 相機開啟 / 關閉事件
        private void Camera_CameraOpened(object sender, EventArgs e)
        {
            Camera camera = sender as Camera;

            #region HeartBeat Timeout 30 Seconds (程式中斷後 Timeount 秒數)
            camera.Parameters[PLGigECamera.GevHeartbeatTimeout].SetValue(1000 * 30);
            #endregion

            #region Camera Info
            string modelName = camera.CameraInfo[CameraInfoKey.ModelName];
            string serialNumber = camera.CameraInfo[CameraInfoKey.SerialNumber];
            #endregion

            /// Find camera of specific serial number
            //BaslerCam baslerCam = Array.Find(MainWindow.BaslerCams, item => item.SerialNumber == serialNumber);

            Cam.WidthMax = (int)camera.Parameters[PLGigECamera.WidthMax].GetValue();
            Cam.HeightMax = (int)camera.Parameters[PLGigECamera.HeightMax].GetValue();

            #region Adjustable parameters
            camera.Parameters[PLGigECamera.OffsetX].SetToMinimum();
            Cam.OffsetX = (int)camera.Parameters[PLGigECamera.OffsetX].GetValue();
            camera.Parameters[PLGigECamera.OffsetY].SetToMinimum();
            Cam.OffsetY = (int)camera.Parameters[PLGigECamera.OffsetY].GetValue();

            // CAM_WIDTH 待變更
            if (!camera.Parameters[PLGigECamera.Width].TrySetValue(MainWindow.CAM_WIDTH))
            {
                camera.Parameters[PLGigECamera.Width].SetToMaximum();  // must set to other value small than 2040 
            }
            Cam.Width = (int)camera.Parameters[PLGigECamera.Width].GetValue();

            // CAM_HEIGHT 待變更
            if (!camera.Parameters[PLGigECamera.Height].TrySetValue(MainWindow.CAM_HEIGHT))
            {
                camera.Parameters[PLGigECamera.Height].SetToMaximum(); // must set to other value small than 2040
            }
            Cam.Height = (int)camera.Parameters[PLGigECamera.Height].GetValue();

            // 取得最大 OFFSET
            Cam.OffsetXMax = (int)camera.Parameters[PLGigECamera.OffsetX].GetMaximum();
            Cam.OffsetYMax = (int)camera.Parameters[PLGigECamera.OffsetY].GetMaximum();

            // 灰階設定 (之後皆為 mono)
            camera.Parameters[PLGigECamera.PixelFormat].SetValue(PLGigECamera.PixelFormat.Mono8);

            // FPS 設定
            camera.Parameters[PLGigECamera.AcquisitionFrameRateEnable].SetValue(true); // 鎖定 FPS (不需要太快張數)
            camera.Parameters[PLGigECamera.AcquisitionFrameRateAbs].SetValue(12);      // 設定 FPS
            Cam.FPS = camera.Parameters[PLGigECamera.AcquisitionFrameRateAbs].GetValue();

            // 曝光時間設定
            camera.Parameters[PLGigECamera.ExposureMode].SetValue(PLGigECamera.ExposureMode.Timed);    // 曝光模式 Timed
            camera.Parameters[PLGigECamera.ExposureAuto].SetValue(PLGigECamera.ExposureAuto.Off);      // 關閉自動曝光
            camera.Parameters[PLGigECamera.ExposureTimeAbs].SetValue(10000);   // 10000 is default exposure time of acA2040

            Cam.ExposureTime = camera.Parameters[PLGigECamera.ExposureTimeAbs].GetValue();

            // 設定擷取模式為連續
            camera.Parameters[PLGigECamera.AcquisitionMode].SetValue(PLGigECamera.AcquisitionMode.Continuous);

            // 設定 Trigger
            camera.Parameters[PLGigECamera.TriggerSelector].SetValue(PLGigECamera.TriggerSelector.FrameStart);
            camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.On);
            camera.Parameters[PLGigECamera.TriggerSource].SetValue(PLGigECamera.TriggerSource.Software);
            #endregion

            #region Grabber Event
            Cam.Camera.StreamGrabber.GrabStarted += StreamGrabber_GrabStarted;
            Cam.Camera.StreamGrabber.GrabStopped += StreamGrabber_GrabStopped;
            Cam.Camera.StreamGrabber.ImageGrabbed += StreamGrabber_ImageGrabbed;
            //baslerCam.Camera.StreamGrabber.ImageGrabbed += content.DebugTab.StreamGrabber_ImageGrabbed;

            Cam.Camera.StreamGrabber.UserData = "abc";
            #endregion

            // 觸發 PropertyChange
            Cam.PropertyChange();

            // 變更 Zoom Ratio

            // 


        }

        private void Camera_CameraClosing(object sender, EventArgs e)
        {
            //Camera cam = sender as Camera;
            //string serialNumber = cam.CameraInfo[CameraInfoKey.SerialNumber];

            //BaslerCam baslerCam = Array.Find(MainWindow.BaslerCams, item => item.SerialNumber == serialNumber);

            //baslerCam.Camera.StreamGrabber.GrabStarted -= StreamGrabber_GrabStarted;
            //baslerCam.Camera.StreamGrabber.GrabStopped -= StreamGrabber_GrabStopped;
            //baslerCam.Camera.StreamGrabber.ImageGrabbed -= StreamGrabber_ImageGrabbed;

            Cam.Camera.StreamGrabber.GrabStarted -= StreamGrabber_GrabStarted;
            Cam.Camera.StreamGrabber.GrabStopped -= StreamGrabber_GrabStopped;
            Cam.Camera.StreamGrabber.ImageGrabbed -= StreamGrabber_ImageGrabbed;
        }

        private void Camera_CameraClosed(object sender, EventArgs e)
        {
            Cam.PropertyChange();
        }
        #endregion


        #region StreamGrabber 事件
        private void StreamGrabber_GrabStarted(object sender, EventArgs e)
        {
            Debug.WriteLine("Grabber Start");
            MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.C, "Grabber started");
            Cam.PropertyChange(nameof(Cam.IsGrabbing));
        }

        private void StreamGrabber_GrabStopped(object sender, GrabStopEventArgs e)
        {
            Debug.WriteLine("Grabber Stop");
            MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.C, "Grabber stoped");
            Cam.PropertyChange(nameof(Cam.IsGrabbing));
        }

        private void StreamGrabber_ImageGrabbed(object sender, ImageGrabbedEventArgs e)
        {
            IGrabResult grabResult = e.GrabResult;

            if (grabResult.GrabSucceeded)
            {
                // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // 
                Mat mat = BaslerFunc.GrabResultToMatMono(grabResult);   // 轉 MatMono 

                // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // 
                Cam.Frames = (int)grabResult.ImageNumber;

                //MainWindow.Dispatcher.Invoke(() =>
                //{
                //    MainWindow.ImageSource = mat.ToImageSource();
                //});

                Dispatcher.Invoke(() =>
                {
                    ImageSource = mat.ToImageSource();
                });
            }
        }
        #endregion

        #endregion
    }
}
