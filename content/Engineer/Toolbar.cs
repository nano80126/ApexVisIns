using Basler.Pylon;
using OpenCvSharp;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using ApexVisIns.Algorithm;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Collections.Generic;

namespace ApexVisIns.content
{
    public partial class EngineerTab : StackPanel
    {
        #region Toolbar 元件事件
        /// <summary>
        /// 相機選擇 Selector 變更事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CamSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //ComboBox comboBox = sender as ComboBox;
            //int idx = comboBox.SelectedIndex;

            //Toolbar.DataContext = MainWindow.BaslerCams[idx];
            //Cam = MainWindow.BaslerCams[idx];
            // ConfigPanel.DataContext = MainWindow.BaslerCams[idx];
        }

        private void CamSelector_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            // 重置 Selected Index
            (sender as ComboBox).SelectedIndex = -1;
        }

        private void CamConnect_Checked(object sender, RoutedEventArgs e)
        {
            ToggleButton Toggle = sender as ToggleButton;

            BaslerCamInfo info = CamSelector.SelectedItem as BaslerCamInfo;
            int idx = CamSelector.SelectedIndex;

            //Toggle.IsChecked = BaslerFunc.Basler_Connect(MainWindow.BaslerCams[idx], info.SerialNumber);
            //Toggle.IsChecked = Basler_Connect(MainWindow.BaslerCams[idx], info.SerialNumber);
            Toggle.IsChecked = Basler_Connect(MainWindow.BaslerCam, info.SerialNumber);
        }

        private void CamConnect_Unchecked(object sender, RoutedEventArgs e)
        {
            ToggleButton Toggle = sender as ToggleButton;
            int idx = CamSelector.SelectedIndex;

            //Toggle.IsChecked = BaslerFunc.Basler_Disconnect(MainWindow.BaslerCams[idx]);
            //Toggle.IsChecked = Basler_Disconnect(MainWindow.BaslerCams[idx]);
            Toggle.IsChecked = Basler_Disconnect(MainWindow.BaslerCam);
        }

        private void SingleShot_Click(object sender, RoutedEventArgs e)
        {
            Basler_SingleGrab(MainWindow.BaslerCam);
        }

        private void ContinouseShot_Click(object sender, RoutedEventArgs e)
        {
            Basler_ContinousGrab(MainWindow.BaslerCam);
        }

        private void ToggleStreamGrabber_Click(object sender, RoutedEventArgs e)
        {
            if (!MainWindow.BaslerCam.IsTriggerMode)
            {
                Basler_StartStreamGrabber(MainWindow.BaslerCam);
            }
            else
            {
                Basler_StopStreamGrabber(MainWindow.BaslerCam);
            }

            //if (!MainWindow.BaslerCam.IsGrabberOpened)
            //{
            //    Basler_StartStreamGrabber(MainWindow.BaslerCam);
            //}
            //else
            //{
            //    Basler_StopStreamGrabber(MainWindow.BaslerCam);
            //}
        }

        private void RetrieveImage_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.BaslerCam.IsGrabbing && MainWindow.BaslerCam.IsTriggerMode)
            {
                Basler_StreamGrabber_RetrieveImage(MainWindow.BaslerCam);
            }

            // if (MainWindow.BaslerCam.IsGrabberOpened)
            // {
            //     Basler_StreamGrabber_RetrieveImage(MainWindow.BaslerCam);
            // }
        }

        private void ToggleCrosshair_Click(object sender, RoutedEventArgs e)
        {
            Crosshair.Enable = !Crosshair.Enable;
        }

        private void ToggleAssistRect_Click(object sender, RoutedEventArgs e)
        {
            AssistRect.Enable = !AssistRect.Enable;
        }

        /// <summary>
        /// Zoom Reset
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RatioTextblock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            if (e.ClickCount >= 2 && e.LeftButton == MouseButtonState.Pressed)
            {
                ZoomRatio = 100;
            }
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
                //cam.PropertyChange();
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, ex.Message);
                //throw;
            }
            return cam.IsOpen;
        }

        private bool Basler_Disconnect(BaslerCam cam)
        {
            try
            {
                cam.ConfigList.Clear();
                cam.Close();

                // GC 回收
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, ex.Message);
                //throw;
            }
            return false;
        }

        /// <summary>
        /// 啟動 StreamGrabber，此方法啟動時，改為 RetrieveResult 取得影像
        /// </summary>
        /// <param name="cam"></param>
        private void Basler_StartStreamGrabber(BaslerCam cam)
        {
            try
            {
                if (!cam.Camera.StreamGrabber.IsGrabbing)
                {
                    // 啟動觸發模式
                    cam.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.On);
                    cam.IsTriggerMode = true;

                    // 啟動 StreamGrabber，連續拍攝
                    cam.Camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByUser);

                    _ = cam.Camera.WaitForFrameTriggerReady(500, TimeoutHandling.ThrowException);
                    //cam.IsGrabberOpened = true;
                    //cam.IsContinuousGrabbing = false;

                    // 取消綁定事件
                    cam.Camera.StreamGrabber.ImageGrabbed -= StreamGrabber_ImageGrabbed;

                    // 清空 Image
                    // MainWindow.ImageSource = null;
                    // Indicator.ImageSource = null;
                    Indicator.Image = null;
                }
            }
            catch (TimeoutException T)
            {
                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, T.Message);
            }
            catch (InvalidOperationException I)
            {
                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, I.Message);
            }
            catch (Exception E)
            {
                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, E.Message);
            }
        }

        /// <summary>
        /// 停止 StreamGrabber，此方法啟動時
        /// </summary>
        /// <param name="cam"></param>
        private void Basler_StopStreamGrabber(BaslerCam cam)
        {
            try
            {
                if (cam.Camera.StreamGrabber.IsGrabbing)
                {
                    // 停止 StreamGrabber
                    cam.Camera.StreamGrabber.Stop();

                    // 關閉觸發模式
                    cam.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.Off);
                    cam.IsTriggerMode = false;
                    //cam.IsGrabberOpened = false;

                    cam.Camera.StreamGrabber.ImageGrabbed += StreamGrabber_ImageGrabbed;
                }
            }
            catch (TimeoutException T)
            {
                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, T.Message);
            }
            catch (InvalidOperationException I)
            {
                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, I.Message);
            }
            catch (Exception E)
            {
                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, E.Message);
            }
        }

        public static void Basler_SingleGrab(BaslerCam cam)
        {
            try
            {
                if (!cam.Camera.StreamGrabber.IsGrabbing)
                {
                    // 啟動 StreamGrabber，拍攝一張
                    cam.Camera.StreamGrabber.Start(1, GrabStrategy.LatestImages, GrabLoop.ProvidedByUser);

                    // cam.Camera.ExecuteSoftwareTrigger();
                    _ = cam.Camera.StreamGrabber.RetrieveResult(250, TimeoutHandling.ThrowException);

                    // Cv2.DestroyAllWindows();
                }
            }
            catch (TimeoutException T)
            {
                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, T.Message);
            }
            catch (InvalidOperationException I)
            {
                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, I.Message);
            }
            catch (Exception E)
            {
                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, E.Message);
            }
            finally
            {
                if (cam.Camera.StreamGrabber.IsGrabbing)
                {
                    cam.Camera.StreamGrabber.Stop();
                }
            }
        }

        public static void Basler_ContinousGrab(BaslerCam cam)
        {
            try
            {
                if (!cam.Camera.StreamGrabber.IsGrabbing)
                {
                    // cam.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.Off);
                    cam.Camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByStreamGrabber);

                    // 變更 Flag (連續拍攝)
                    // cam.IsContinuousGrabbing = true;
                }
                else
                {
                    cam.Camera.StreamGrabber.Stop();
                    // cam.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.On);

                    // 變更 Flag (不為連續拍攝)
                    // cam.IsContinuousGrabbing = false;
                }
            }
            catch (TimeoutException T)
            {
                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, T.Message);
            }
            catch (InvalidOperationException I)
            {
                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, I.Message);
            }
            catch (Exception E)
            {
                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, E.Message);
            }
        }

        /// <summary>
        /// Stream Grabber Retrieve Image
        /// </summary>
        /// <param name="cam"></param>
        private void Basler_StreamGrabber_RetrieveImage(BaslerCam cam)
        {
            // 耳朵檢測
            // MainWindow.ApexEarInspectionSequence(cam);
            // 窗戶檢驗
            // MainWindow.ApexWindpwInspectionSequence(cam);

            try
            {
                cam.Camera.ExecuteSoftwareTrigger();

                using IGrabResult grabResult = cam.Camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);
                Mat mat = BaslerFunc.GrabResultToMatMono(grabResult);

                Dispatcher.Invoke(() =>
                {
                    string sn = cam.Camera.CameraInfo[CameraInfoKey.SerialNumber];
                    //Debug.WriteLine($"SN:{sn}");
                    if (AssistRect.Area > 0)
                    {
#if false
                        OpenCvSharp.Rect roi = AssistRect.GetRect();
                        Mat roiMat = new(mat, roi);
                        Cv2.ImShow($"roi", new Mat(mat, roi));

                        Methods.GetRoiCanny(mat, roi, 75, 150, out Mat canny);
                        // Methods.GetContours(roiMat, roi.Location, 75, 150, out OpenCvSharp.Point[][] cons, out OpenCvSharp.Point[] con);
                        Methods.GetContoursFromCanny(canny, roi.Location, out _, out OpenCvSharp.Point[] con);

                        Methods.GetHoughHorizonalYPos(canny, roi.Top, out int YCount, out double[] Ypos, 5, 0);

                        int maxX = con.Min(c => c.X);
                        int maxY = con.Max(c => c.Y);

                        Debug.WriteLine($"X: {maxX} Y: {maxY}");
                        Debug.WriteLine($"{string.Join(",", Ypos)}");


                        //Methods.GetHoughVerticalXPos(canny, roi.Left, out int count, out double[] XPos);
                        //Methods.GetBottomHorizontalLine(canny, out double Ypos);

                        //Debug.WriteLine($"{XPos.Length} {string.Join(",", XPos)}");
                        //Cv2.ImShow($"roi", roiMat);
                        Cv2.ImShow($"canny", canny); 

#endif
                        //string sn = cam.Camera.CameraInfo[CameraInfoKey.SerialNumber];
                        //Debug.WriteLine($"SN:{sn}");

                        switch (sn)
                        {
                            case "24214356":    // 前相機
                                MainWindow.JawInsSequenceCam1(mat);
                                break;
                            case "24214384":    // 下相機
                                MainWindow.JawInsSequenceCam2(mat);
                                break;
                            case "24115540":    // 側相機
                                MainWindow.JawInsSequenceCam3(mat);
                                break;
                            default:
                                break;
                        }
                        //MainWindow.JawInsSequenceCam1(mat);
                        //MainWindow.JawInsSequenceCam2(mat);
                        //MainWindow.JawInsSequenceCam3(mat);
                    }

                    Indicator.Image = mat;
                });
            }
            catch (TimeoutException T)
            {
                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, T.Message);
            }
            catch (InvalidOperationException I)
            {
                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, I.Message);
            }
            catch (Exception E)
            {
                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, E.Message);
            }
        }

        #region Camera 開啟 / 關閉事件
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
            // BaslerCam baslerCam = Array.Find(MainWindow.BaslerCams, item => item.SerialNumber == serialNumber);
            BaslerCam Cam = MainWindow.BaslerCam;

            Cam.ModelName = modelName;

            Cam.WidthMax = (int)camera.Parameters[PLGigECamera.WidthMax].GetValue();
            Cam.HeightMax = (int)camera.Parameters[PLGigECamera.HeightMax].GetValue();

            #region Adjustable parameters
            camera.Parameters[PLGigECamera.CenterX].SetValue(false);    // 確保 OffsetX 沒有被鎖定
            camera.Parameters[PLGigECamera.CenterY].SetValue(false);    // 確保 OffsetY 沒有被鎖定
            //
            camera.Parameters[PLGigECamera.OffsetX].SetToMinimum();
            Cam.OffsetX = (int)camera.Parameters[PLGigECamera.OffsetX].GetValue();
            camera.Parameters[PLGigECamera.OffsetY].SetToMinimum();
            Cam.OffsetY = (int)camera.Parameters[PLGigECamera.OffsetY].GetValue();

            // CAM_WIDTH 待變更
            //if (!camera.Parameters[PLGigECamera.Width].TrySetValue(MainWindow.CAMWIDTH))
            //if (!camera.Parameters[PLGigECamera.Width].TrySetValue(MainWindow.CustomCameraParam.WIDTH))
            //{
            camera.Parameters[PLGigECamera.Width].SetToMaximum();  // 設為最大 WIDTH
            //}
            Cam.Width = (int)camera.Parameters[PLGigECamera.Width].GetValue();

            // CAM_HEIGHT 待變更
            //if (!camera.Parameters[PLGigECamera.Height].TrySetValue(MainWindow.CustomCameraParam.HEIGHT))
            //{
            camera.Parameters[PLGigECamera.Height].SetToMaximum(); // 設為最大 HEIGHT
            //}
            Cam.Height = (int)camera.Parameters[PLGigECamera.Height].GetValue();

            // 取得最大 OFFSET
            Cam.OffsetXMax = (int)camera.Parameters[PLGigECamera.OffsetX].GetMaximum();
            Cam.OffsetYMax = (int)camera.Parameters[PLGigECamera.OffsetY].GetMaximum();

            // 灰階設定 (之後皆為 mono)
            camera.Parameters[PLGigECamera.PixelFormat].SetValue(PLGigECamera.PixelFormat.Mono8);

            // FPS 設定
            camera.Parameters[PLGigECamera.AcquisitionFrameRateEnable].SetValue(true); // 鎖定 FPS (不需要太快張數)
            camera.Parameters[PLGigECamera.AcquisitionFrameRateAbs].SetValue(MainWindow.CAMFPS);      // 設定 FPS
            Cam.FPS = camera.Parameters[PLGigECamera.AcquisitionFrameRateAbs].GetValue();

            // 曝光時間設定
            camera.Parameters[PLGigECamera.ExposureMode].SetValue(PLGigECamera.ExposureMode.Timed);    // 曝光模式 Timed
            camera.Parameters[PLGigECamera.ExposureAuto].SetValue(PLGigECamera.ExposureAuto.Off);      // 關閉自動曝光
            camera.Parameters[PLGigECamera.ExposureTimeAbs].SetValue(MainWindow.CAMEXPOSURE);   // 10000 is default exposure time of acA2040

            Cam.ExposureTime = camera.Parameters[PLGigECamera.ExposureTimeAbs].GetValue();

            // 設定擷取模式為連續
            camera.Parameters[PLGigECamera.AcquisitionMode].SetValue(PLGigECamera.AcquisitionMode.Continuous);

            // 設定 Trigger
            camera.Parameters[PLGigECamera.TriggerSelector].SetValue(PLGigECamera.TriggerSelector.FrameStart);
            camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.Off);             // Trigger Mode On
            camera.Parameters[PLGigECamera.TriggerSource].SetValue(PLGigECamera.TriggerSource.Software);    // Sotfware Trigger // 觸發模式 ON 才有作用

            // Anaglog Control
            //camera.Parameters[PLGigECamera.GainRaw].SetValue(50);
            //camera.Parameters[PLGigECamera.GammaEnable].SetValue(false);
            //camera.Parameters[PLGigECamera.Gamma].SetValue(1);
            #endregion

            #region Grabber Event
            Cam.Camera.StreamGrabber.GrabStarted += StreamGrabber_GrabStarted;
            Cam.Camera.StreamGrabber.GrabStopped += StreamGrabber_GrabStopped;
            Cam.Camera.StreamGrabber.ImageGrabbed += StreamGrabber_ImageGrabbed;

            // 待變更 // 用來標示相機用途等 ...
            Cam.Camera.StreamGrabber.UserData = "abc";
            #endregion

            // 觸發 PropertyChange
            Cam.PropertyChange();

            // 同步 Config 和 Cam
            ConfigPanel.SyncConfiguration(Cam.Config, Cam);

            // 變更 Zoom Ratio
            ZoomRatio = 100;

            // 暫停 Camera Enumerator
            MainWindow.CameraEnumer.WorkerPause();
        }

        private void Camera_CameraClosing(object sender, EventArgs e)
        {
            // Camera cam = sender as Camera;
            // string serialNumber = cam.CameraInfo[CameraInfoKey.SerialNumber];

            // BaslerCam baslerCam = Array.Find(MainWindow.BaslerCams, item => item.SerialNumber == serialNumber);

            // baslerCam.Camera.StreamGrabber.GrabStarted -= StreamGrabber_GrabStarted;
            // baslerCam.Camera.StreamGrabber.GrabStopped -= StreamGrabber_GrabStopped;
            // baslerCam.Camera.StreamGrabber.ImageGrabbed -= StreamGrabber_ImageGrabbed;

            BaslerCam Cam = MainWindow.BaslerCam;

            Cam.Camera.StreamGrabber.GrabStarted -= StreamGrabber_GrabStarted;
            Cam.Camera.StreamGrabber.GrabStopped -= StreamGrabber_GrabStopped;
            Cam.Camera.StreamGrabber.ImageGrabbed -= StreamGrabber_ImageGrabbed;
        }

        private void Camera_CameraClosed(object sender, EventArgs e)
        {
            BaslerCam Cam = MainWindow.BaslerCam;
            Cam.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.Off);
            Cam.PropertyChange();

            //MainWindow.ImageSource = null;
            //Indicator.ImageSource = null;
            Indicator.Image = null;

            // 啟動 Camera Enumerator
            MainWindow.CameraEnumer.WorkerPause();
        }
        #endregion

        #region StreamGrabber 啟動 / 停止 / 拍攝事件
        private void StreamGrabber_GrabStarted(object sender, EventArgs e)
        {
            Debug.WriteLine("Grabber Start");
            MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.CAMERA, "Grabber started");

            MainWindow.BaslerCam.PropertyChange(nameof(MainWindow.BaslerCam.IsGrabbing));

            // FullMat = null;
            Cv2.DestroyAllWindows();
        }

        private void StreamGrabber_GrabStopped(object sender, GrabStopEventArgs e)
        {
            Debug.WriteLine("Grabber Stop");
            MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.CAMERA, "Grabber stoped");
            MainWindow.BaslerCam.PropertyChange(nameof(MainWindow.BaslerCam.IsGrabbing));
            //MainWindow.BaslerCam.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.On);
        }

        private void StreamGrabber_ImageGrabbed(object sender, ImageGrabbedEventArgs e)
        {
            IGrabResult grabResult = e.GrabResult;

            if (grabResult.GrabSucceeded)
            {
                // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // 
                Mat mat = BaslerFunc.GrabResultToMatMono(grabResult);   // 轉 MatMono 

                // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // 
                MainWindow.BaslerCam.Frames = (int)grabResult.ImageNumber;

                Debug.WriteLine($"Start: {DateTime.Now:ss.fff}");

                #region Assist Rect && AssistRect.Area > 0
                if (AssistRect.Enable)
                {
                    //Cv2.DestroyAllWindows();
                    #region Coding custom ROI Method here


                    #region UI Thread here
                    Dispatcher.Invoke(() =>
                    {



                    });
                    #endregion
                    #endregion
                }
                #endregion

                //Cv2.CvtColor(mat, mat, ColorConversionCodes.GRAY2BGR);
                //Cv2.Circle(mat, new OpenCvSharp.Point(500, 500), 5, Scalar.Red, 3);

                MainWindow.Dispatcher.Invoke(() =>
                {
                    //MainWindow.ImageSource = mat.ToImageSource();
                    //Indicator.ImageSource = mat.ToImageSource();
                    Indicator.Image = mat;
                    //Debug.WriteLine($"width: {width} maxWindowWidth: {maxWindowWidth}");
                    //Debug.WriteLine($"1st {step1done}, 2nd {step2done} 3rd {step3done} 4th {step4done}");

                });
                Debug.WriteLine($"End: {DateTime.Now:ss.fff}");
                Debug.WriteLine("------------------------------------------------");
            }
        }
        #endregion

        #endregion
    }
}
