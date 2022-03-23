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
            if (!MainWindow.BaslerCam.IsGrabberOpened)
            {
                Basler_StartStreamGrabber(MainWindow.BaslerCam);
            }
            else
            {
                Basler_StopStreamGrabber(MainWindow.BaslerCam);
            }
        }

        private void RetrieveImage_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.BaslerCam.IsGrabberOpened)
            {
                MainWindow.ApexDefectInspectionStepsFlags.EarSteps = 0;
                MainWindow.ApexDefectInspectionStepsFlags.WindowSteps = 0;
                Basler_StreamGrabber_RetrieveImage(MainWindow.BaslerCam);
            }
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
        /// 啟動 StreamGrabber，
        /// 此方法啟動時，改為 RetrieveResult 取得影像，
        /// 
        /// </summary>
        /// <param name="cam"></param>
        private void Basler_StartStreamGrabber(BaslerCam cam)
        {
            try
            {
                if (!cam.Camera.StreamGrabber.IsGrabbing)
                {
                    // 啟動 StreamGrabber，連續拍攝
                    cam.Camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByUser);

                    cam.Camera.WaitForFrameTriggerReady(500, TimeoutHandling.ThrowException);
                    cam.IsGrabberOpened = true;
                    cam.IsContinuousGrabbing = false;

                    // 
                    cam.Camera.StreamGrabber.ImageGrabbed -= StreamGrabber_ImageGrabbed;

                    // 清空 Image
                    MainWindow.ImageSource = null;
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

        private void Basler_StopStreamGrabber(BaslerCam cam)
        {
            try
            {
                if (cam.Camera.StreamGrabber.IsGrabbing)
                {
                    cam.Camera.StreamGrabber.Stop();
                    cam.IsGrabberOpened = false;

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

                    cam.Camera.ExecuteSoftwareTrigger();
                    _ = cam.Camera.StreamGrabber.RetrieveResult(250, TimeoutHandling.ThrowException);
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
                    cam.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.Off);
                    cam.Camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByStreamGrabber);

                    // 變更 Flag (連續拍攝)
                    cam.IsContinuousGrabbing = true;
                }
                else
                {
                    cam.Camera.StreamGrabber.Stop();
                    cam.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.On);

                    // 變更 Flag (不為連續拍攝)
                    cam.IsContinuousGrabbing = false;
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
        private async void Basler_StreamGrabber_RetrieveImage(BaslerCam cam)
        {
            // 耳朵檢測
            MainWindow.ApexEarInspectionSequence(cam);
            // 窗戶檢驗
            // MainWindow.ApexWindpwInspectionSequence(cam);


            return;
            try
            {

#if false
                OpenCvSharp.Rect roiL = new();
                OpenCvSharp.Rect roiR = new();

                double top = 0;
                double bottom = 0;
                double[] xPos = Array.Empty<double>();
                double[] xPos2 = Array.Empty<double>();
                double[] xPos3 = Array.Empty<double>();
                double[] xArray = Array.Empty<double>();

                int Loop = 0;
                Debug.WriteLine($"Start: {DateTime.Now:ss.fff}");

                Cv2.DestroyAllWindows();
                while (MainWindow.ApexDefectInspectionStepsFlags.WindowSteps != 0b10000)
                {
                    Debug.WriteLine($"Loop: {Loop} Steps: {MainWindow.ApexDefectInspectionStepsFlags.WindowSteps}");
                    if (Loop++ >= 16)
                    {
                        break;
                    }

                    switch (MainWindow.ApexDefectInspectionStepsFlags.WindowSteps)
                    {
                        case 0b0000:    // 0
                            await MainWindow.PreWindowInspectionRoi();
                            MainWindow.ApexDefectInspectionStepsFlags.WindowSteps += 0b01;
                            continue;
                        case 0b0010:    // 2
                            MainWindow.PreWindowInspectionRoi2();
                            MainWindow.ApexDefectInspectionStepsFlags.WindowSteps += 0b01;
                            continue;
                        case 0b0100:    // 4
                            MainWindow.PreWindowInspectionRoi3();
                            MainWindow.ApexDefectInspectionStepsFlags.WindowSteps += 0b01;
                            continue;
                        case 0b0110:    // 6
                            MainWindow.PreWindowInspection();
                            MainWindow.ApexDefectInspectionStepsFlags.WindowSteps += 0b01;
                            continue;
                        case 0b1000:    // 8
                            MainWindow.PreWindowInspection2();
                            MainWindow.ApexDefectInspectionStepsFlags.WindowSteps += 0b01;
                            continue;
                        case 0b1010:    // 10
                            MainWindow.PreWindowInspection3();
                            MainWindow.ApexDefectInspectionStepsFlags.WindowSteps += 0b01;
                            continue;
                        case 0b1100:    // 12 // 開啟側光
                            MainWindow.PreWindowInspectionSide();
                            MainWindow.ApexDefectInspectionStepsFlags.WindowSteps += 0b01;
                            continue;
                        case 0b1110:    // 14 // 開啟側光
                            MainWindow.PreWindowInspectionSide2();
                            MainWindow.ApexDefectInspectionStepsFlags.WindowSteps += 0b01;
                            continue;
                        default:
                            break;
                    }

                    cam.Camera.ExecuteSoftwareTrigger();

                    using IGrabResult grabResult = cam.Camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);
                    Debug.WriteLine($"Frames: {grabResult.ImageNumber}");

                    if (grabResult.GrabSucceeded)
                    {
                        Mat mat = BaslerFunc.GrabResultToMatMono(grabResult);   // 轉 MatMono 

                        switch (MainWindow.ApexDefectInspectionStepsFlags.WindowSteps)
                        {
                            case 0b0001:    // 1 ROI
                                MainWindow.WindowInspectionTopBottomEdge(mat, out top, out bottom);
                                MainWindow.WindowInspectionRoi(mat, out xPos, out roiL, out roiR);

                                if (xPos.Length == 7)    // 有找到 7 個分界點
                                {
                                    xArray = xPos;
                                    xPos = null;
                                    roiL.Y = roiR.Y = (int)top;
                                    roiL.Height = roiR.Height = (int)(bottom - top);
                                    MainWindow.ApexDefectInspectionStepsFlags.WindowSteps = 0b0110;
                                }
                                else
                                {
                                    MainWindow.ApexDefectInspectionStepsFlags.WindowSteps += 0b01;
                                }

                                break;
                            case 0b0011:    // 3 ROI
                                //MainWindow.WindowInspectionTopBottomLimit(mat, out yPos);
                                MainWindow.WindowInspectionRoi(mat, out xPos2, out roiL, out roiR);

                                if (xPos2.Length == 7)  // 有找到 7 個分界點
                                {
                                    xArray = xPos2;
                                    xPos2 = null;
                                    roiL.Y = roiR.Y = (int)top;
                                    roiL.Height = roiR.Height = (int)(bottom - top);
                                    MainWindow.ApexDefectInspectionStepsFlags.WindowSteps = 0b0110;
                                }
                                else
                                {
                                    MainWindow.ApexDefectInspectionStepsFlags.WindowSteps += 0b01;
                                }
                                break;
                            case 0b0101:    // 5 ROI
                                MainWindow.WindowInspectionRoi(mat, out xPos3, out roiL, out roiR);

                                if (xPos3.Length == 7)   // 有找到 7 個分界點
                                {
                                    xArray = xPos3;
                                    xPos3 = null;
                                    roiL.Y = roiR.Y = (int)top;
                                    roiL.Height = roiR.Height = (int)(bottom - top);
                                    MainWindow.ApexDefectInspectionStepsFlags.WindowSteps = 0b0110;
                                }
                                else
                                {
                                    // 如果取三次ROI還失敗，這邊合併並抽取 (30 pixel 間隔)
                                    xArray = xPos.Concat(xPos2).Concat(xPos3).OrderBy(x => x).ToArray();

                                    List<double> xList = new();
                                    for (int i = 0; i < xArray.Length; i++)
                                    {
                                        if (i == 0 || xArray[i - 1] + 30 < xArray[i])
                                        {
                                            xList.Add(xArray[i]);
                                        }
                                    }
                                    xArray = xList.ToArray();
                                    xList.Clear();

                                    roiL = new OpenCvSharp.Rect((int)xArray[1] - 20, (int)top, (int)(xArray[2] - xArray[1]) + 40, (int)(bottom - top));
                                    roiR = new OpenCvSharp.Rect((int)xArray[^3] - 20, (int)top, (int)(xArray[^2] - xArray[^3]) + 40, (int)(bottom - top));

                                    xPos = xPos2 = xPos3 = null;
                                    MainWindow.ApexDefectInspectionStepsFlags.WindowSteps = 0b0110;
                                }
                                break;
                            case 0b0111:    // 7
                                MainWindow.WindowInspection(mat, xArray, roiL, roiR);
                                MainWindow.ApexDefectInspectionStepsFlags.WindowSteps += 0b01;

                                #region 待刪除
                                Mat m1 = new();
                                Cv2.Resize(mat, m1, new OpenCvSharp.Size(mat.Width / 2, mat.Height / 2));
                                Cv2.ImShow("mat1", m1);
                                Cv2.MoveWindow("mat1", 0, 0);
                                #endregion
                                break;
                            case 0b1001:    // 9
                                MainWindow.WindowInspection(mat, xArray, roiL, roiR);
                                MainWindow.ApexDefectInspectionStepsFlags.WindowSteps += 0b01;

                                #region 待刪除
                                Mat m2 = new();
                                Cv2.Resize(mat, m2, new OpenCvSharp.Size(mat.Width / 2, mat.Height / 2));
                                Cv2.ImShow("mat2", m2);
                                Cv2.MoveWindow("mat2", 600, 0);
                                #endregion
                                break;
                            case 0b1011:    // 11
                                MainWindow.WindowInspection(mat, xArray, roiL, roiR);
                                MainWindow.ApexDefectInspectionStepsFlags.WindowSteps += 0b01;

                                #region 待刪除
                                Mat m3 = new();

                                Cv2.Rectangle(mat, roiL, Scalar.Gray, 2);
                                Cv2.Rectangle(mat, roiR, Scalar.Gray, 2);

                                Cv2.Resize(mat, m3, new OpenCvSharp.Size(mat.Width / 2, mat.Height / 2));
                                Cv2.ImShow("mat3", m3);
                                Cv2.MoveWindow("mat3", 1200, 0);
                                #endregion
                                break;
                            case 0b1101:    // 13 // 側光
                                OpenCvSharp.Rect roiTop = new((int)xArray[2], (int)(top - 80), (int)(xArray[4] - xArray[2]), 120);
                                MainWindow.WindowInspectionSideLight(mat, roiTop, 0);
                                MainWindow.ApexDefectInspectionStepsFlags.WindowSteps += 0b01;

                                #region 待刪除
                                Mat otsu1 = new();

                                Cv2.Rectangle(mat, roiTop, Scalar.Gray, 2);
                                Cv2.Resize(mat, otsu1, new OpenCvSharp.Size(mat.Width / 2, mat.Height / 2));
                                Cv2.ImShow("Otsu1", otsu1);
                                Cv2.MoveWindow("Otsu1", 300, 0);
                                #endregion
                                break;
                            case 0b1111:    // 15 // 側光
                                OpenCvSharp.Rect roiBot = new((int)xArray[2], (int)bottom - 40, (int)(xArray[4] - xArray[2]), 120);
                                MainWindow.WindowInspectionSideLight(mat, roiBot, 1);
                                MainWindow.ApexDefectInspectionStepsFlags.WindowSteps += 0b01;

                                #region 待刪除
                                Mat otsu2 = new();

                                Cv2.Rectangle(mat, roiBot, Scalar.Gray, 2);
                                Cv2.Resize(mat, otsu2, new OpenCvSharp.Size(mat.Width / 2, mat.Height / 2));
                                Cv2.ImShow("Otsu2", otsu2);
                                Cv2.MoveWindow("Otsu2", 900, 0);
                                #endregion
                                break;
                            default:
                                break;
                        }
                        MainWindow.ImageSource = mat.ToImageSource();
                    }
                }

                Debug.WriteLine($"Stop: {DateTime.Now:ss.fff}"); 
#endif
      
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
            camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.On);
            camera.Parameters[PLGigECamera.TriggerSource].SetValue(PLGigECamera.TriggerSource.Software);
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
            Cam.PropertyChange();

            MainWindow.ImageSource = null;

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

                MainWindow.Dispatcher.Invoke(() =>
                {
                    Debug.WriteLine($"Start: {DateTime.Now:ss.fff}");

                    //if (MainWindow.ApexAngleCorrectionFlags.Steps != 0b1111)
                    //{
                    //    MainWindow.AngleCorrection(mat);
                    //}
                    // MainWindow.WindowInspection(mat);

                    #region Assist Rect
                    if (AssistRect.Enable && AssistRect.Area > 0)
                    {
                        //Cv2.DestroyAllWindows();
                        #region Coding custom ROI Method here
                        //Mat sharp = new();
                        //InputArray kernel = InputArray.Create(new double[3, 3] {
                        //    { -0.8, 2.4, -0.8 },
                        //    { -0.8, 2.4, -0.8 },
                        //    { -0.8, 2.4, -0.8 }
                        //});
                        //Cv2.Filter2D(mat, sharp, MatType.CV_8U, kernel, new OpenCvSharp.Point(-1, -1), 0);


                        //Methods.GetRoiOtsu(sharp, AssistRect.GetRect(), 0, 50, out Mat Otsu, out byte value);
                        Methods.GetRoiVerticalFilter2D(mat, AssistRect.GetRect(), 1.8, -0.6, out Mat filter);                        
                        //Methods.GetRoiCanny(mat, AssistRect.GetRect(), 10, 20, out Mat Canny);

                        //Cv2.FindContours(Canny, out OpenCvSharp.Point[][] cons, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple, AssistRect.GetRect().Location);
                        // 這邊要過濾過短 contours 

                        Cv2.ImShow("ROI canny", filter);
                        Cv2.MoveWindow("ROI canny", 20, 20);
                        #endregion
                    }
                    #endregion

#if false
                    #region 耳朵檢驗
                    switch (MainWindow.ApexDefectInspectionStepsFlags.EarSteps)
                    {
                        // 取得 ROI (單邊)
                        case 0b0000:
                            MainWindow.GetEarInspectionRoi(mat, out OpenCvSharp.Rect LeftROI, out OpenCvSharp.Rect RightROI);
                            break;
                        // 檢驗瑕疵
                        case 0b0001:
                            //MainWindow.EarInspection(mat, LeftROI, RightROI);
                            break;
                        // 取得 ROI (另一邊)
                        case 0b0010:

                            break;
                        // 檢驗瑕疵
                        case 0b0011:
                            break;
                    }
                    #endregion

                    //Methods.GetRoiOtsu(mat, LeftROI, 0, 255, out Mat Otsu1, out double th1);
                    //Methods.GetRoiOtsu(mat, RightROI, 0, 255, out Mat Otsu2, out double th2);

                    //Methods.GetRoiCanny(mat, LeftROI, 75, 150, out Mat Canny1);
                    //Methods.GetRoiCanny(mat, RightROI, 75, 150, out Mat Canny2);

                    //Debug.WriteLine($"th1: {th1}, th2: {th2}");

                    //Cv2.Rectangle(mat, LeftROI, Scalar.Gray, 2);
                    //Cv2.Rectangle(mat, RightROI, Scalar.Gray, 2);

                    //Cv2.ImShow("Otsu1", Otsu1);
                    //Cv2.MoveWindow("Otsu1", 100, 0);
                    //Cv2.ImShow("Otsu2", Otsu2);
                    //Cv2.MoveWindow("Otsu2", 300, 0);

                    //Cv2.ImShow("Canny1", Canny1);
                    //Cv2.MoveWindow("Canny1", 500, 0);
                    //Cv2.ImShow("Canny2", Canny2);
                    //Cv2.MoveWindow("Canny2", 700, 0);
#endif

                    //Cv2.CvtColor(mat, mat, ColorConversionCodes.GRAY2BGR);
                    //Cv2.Circle(mat, new OpenCvSharp.Point(500, 500), 5, Scalar.Red, 3);

                    MainWindow.ImageSource = mat.ToImageSource();
                    //Debug.WriteLine($"width: {width} maxWindowWidth: {maxWindowWidth}");
                    //Debug.WriteLine($"1st {step1done}, 2nd {step2done} 3rd {step3done} 4th {step4done}");
                    Debug.WriteLine($"End: {DateTime.Now:ss.fff}");
                    Debug.WriteLine("------------------------------------------------");
                });
            }
        }
        #endregion


        #endregion
    }
}
