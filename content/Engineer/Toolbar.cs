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
        #region Toolbar 事件
        
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

        public static void Basler_SingleGrab(BaslerCam cam)
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
                }
                else
                {
                    cam.Camera.StreamGrabber.Stop();
                    cam.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.On);
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

        #region StreamGrabber 事件
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
                    Debug.WriteLine($"{DateTime.Now:ss.fff}");

                    //Methods.GetHoughLinesV(mat, new OpenCvSharp.Rect(110, 1150, 60, 200), 75, 150, out LineSegmentPoint[] lineV);

                    //MainWindow.CounterPos(mat);

#if true

                    OpenCvSharp.Rect roi = new OpenCvSharp.Rect(100, 840, 1000, 240);


                    #region 手動框選
                    if (false && AssistRect.Enable && AssistRect.Width * AssistRect.Height > 0)
                    {
                        Cv2.DestroyAllWindows();

                        Methods.GetRoiCanny(mat, AssistRect.GetRect(), 75, 150, out Mat cannyWindow1);
                        Methods.GetRoiCanny(mat, AssistRect.GetRect(), 60, 120, out Mat cannyWindow2);
                        Methods.GetRoiCanny(mat, AssistRect.GetRect(), 35, 70, out Mat cannyWindow3);
                        Methods.GetRoiCanny(mat, AssistRect.GetRect(), 35, 150, out Mat cannyWindow4);

                        Cv2.Resize(cannyWindow1, cannyWindow1, new OpenCvSharp.Size(AssistRect.Width * 2 / 3, AssistRect.Height * 2 / 3));
                        Cv2.Resize(cannyWindow2, cannyWindow2, new OpenCvSharp.Size(AssistRect.Width * 2 / 3, AssistRect.Height * 2 / 3));
                        Cv2.Resize(cannyWindow3, cannyWindow3, new OpenCvSharp.Size(AssistRect.Width * 2 / 3, AssistRect.Height * 2 / 3));
                        Cv2.Resize(cannyWindow4, cannyWindow4, new OpenCvSharp.Size(AssistRect.Width * 2 / 3, AssistRect.Height * 2 / 3));
                        Cv2.ImShow("window Canny1", cannyWindow1);
                        Cv2.ImShow("window Canny2", cannyWindow2);
                        Cv2.ImShow("window Canny3", cannyWindow3);
                        Cv2.ImShow("window Canny4", cannyWindow4);

                        Cv2.FindContours(cannyWindow1, out OpenCvSharp.Point[][] cons1, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxNone);
                        Cv2.FindContours(cannyWindow2, out OpenCvSharp.Point[][] cons2, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxNone);
                        Cv2.FindContours(cannyWindow3, out OpenCvSharp.Point[][] cons3, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxNone);
                        Cv2.FindContours(cannyWindow4, out OpenCvSharp.Point[][] cons4, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxNone);

                        if (cons3.Length > 0)
                        {
                            Mat conMat = new Mat(cannyWindow1.Height, cannyWindow1.Width, MatType.CV_8UC1, Scalar.Black);

                            for (int i = 0; i < cons3.Length; i++)
                            {
                                Cv2.DrawContours(conMat, cons3, i, Scalar.White, 2);
                            }
                            Cv2.ImShow("contours", conMat);
                        }

                        Debug.WriteLine($"cons1 {cons1.Length}");
                        Debug.WriteLine($"cons2 {cons2.Length}");
                        Debug.WriteLine($"cons3 {cons3.Length}");
                        Debug.WriteLine($"cons4 {cons4.Length}");
                    }
                    #endregion


                    Methods.GetRoiCanny(mat, roi, 60, 100, out Mat canny);
                    Methods.GetHoughVerticalXPos(canny, roi.X, out int count, out double[] xPos);

                    #region 陣列抽取
                    List<double> xPosList = new();
                    for (int i = 0; i < xPos.Length; i++)
                    {
                        if (i == 0 || xPos[i - 1] + 5 < xPos[i])
                        {
                            xPosList.Add(xPos[i]);
                        }
                    }
                    xPos = xPosList.ToArray();
                    xPosList.Clear();
                    xPosList = null;
                    #endregion

                    Debug.WriteLine($"count : {count};   {string.Join(" , ", xPos.Select(x => Math.Round(x, 2)))}");

                    // 窗戶管內邊緣
                    int cIdx = Array.FindIndex(xPos, 0, x => 750 < x && x < 780);
                    Debug.WriteLine($"center Index {cIdx}");

                    if (count >= 7)
                    {
                        OpenCvSharp.Rect leftRoiWindow = new OpenCvSharp.Rect((int)xPos[1] - 20, 225, (int)xPos[cIdx - 1] - (int)xPos[1] + 40, 1400);
                        OpenCvSharp.Rect rightRoiWindow = new OpenCvSharp.Rect((int)xPos[cIdx + 1] - 20, 225, (int)xPos[^2] - (int)xPos[cIdx + 1] + 40, 1400);

                        #region 邊緣特徵抽取
                        Cv2.DestroyAllWindows();

                        Mat leftRoiMat = new(mat, leftRoiWindow);
                        Mat rightRoiMat = new(mat, rightRoiWindow);

                        #region 取得窗戶 Canny
                        Methods.GetCanny(leftRoiMat, 75, 150, out Mat cannyWindow1);
                        Methods.GetCanny(leftRoiMat, 60, 120, out Mat cannyWindow2);
                        Methods.GetCanny(leftRoiMat, 40, 80, out Mat cannyWindow3);
                        Methods.GetCanny(leftRoiMat, 35, 150, out Mat cannyWindow4);

                        Methods.GetCanny(rightRoiMat, 75, 150, out Mat cannyWindow11);
                        Methods.GetCanny(rightRoiMat, 60, 120, out Mat cannyWindow22);
                        Methods.GetCanny(rightRoiMat, 40, 80, out Mat cannyWindow33);
                        Methods.GetCanny(rightRoiMat, 35, 150, out Mat cannyWindow44);
                        #endregion


                        //Cv2.FindContours(cannyWindow1, out OpenCvSharp.Point[][] cons1, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple);
                        //Cv2.FindContours(cannyWindow2, out OpenCvSharp.Point[][] cons2, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple);
                        //Cv2.FindContours(cannyWindow3, out OpenCvSharp.Point[][] cons3, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple);
                        //Cv2.FindContours(cannyWindow4, out OpenCvSharp.Point[][] cons4, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple);

                        Cv2.FindContours(cannyWindow3 - cannyWindow2 - cannyWindow1, out OpenCvSharp.Point[][] consDiff1, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(leftRoiWindow.X, leftRoiWindow.Y));

                        //Cv2.FindContours(cannyWindow11, out OpenCvSharp.Point[][] cons11, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple);
                        //Cv2.FindContours(cannyWindow22, out OpenCvSharp.Point[][] cons22, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple);
                        //Cv2.FindContours(cannyWindow33, out OpenCvSharp.Point[][] cons33, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple);
                        //Cv2.FindContours(cannyWindow44, out OpenCvSharp.Point[][] cons44, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple);

                        Cv2.FindContours(cannyWindow33 - cannyWindow22 - cannyWindow11, out OpenCvSharp.Point[][] consDiff2, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(rightRoiWindow.X, rightRoiWindow.Y));

                        //Cv2.ArcLength(cons1[0], false);

                        //OpenCvSharp.Point[][] except = cons3.Except(cons1).Except(cons2).Except(cons4).ToArray();
                        //if (cons3.Length > 0)
                        //{
                        Mat conMat1 = new(cannyWindow1.Height, cannyWindow1.Width + 500, MatType.CV_8UC1, Scalar.Black);
                        Mat conMat2 = new(cannyWindow11.Height, cannyWindow11.Width + 1200, MatType.CV_8UC1, Scalar.Black);

                        //OpenCvSharp.Point[][] consConcat1 = cons1.Concat(cons2).Concat(cons3).Concat(cons4).ToArray();
                        //for (int i = 0; i < consDiff1.Length; i++)
                        //{
                        //    Cv2.DrawContours(conMat1, consDiff1, i, Scalar.White, 2);
                        //}

                        // 過濾小點點，串接陣列，過濾邊緣點
                        OpenCvSharp.Point[] filter1 = consDiff1.Where(c => c.Length > 20).Aggregate(Array.Empty<OpenCvSharp.Point>(), (acc, c) => acc.Concat(c).ToArray()).Where(pt =>
                        {
                            return xPos[1] + 3 < pt.X && pt.X < xPos[cIdx - 1] - 3;
                        }).ToArray();

                        Debug.WriteLine($"1: {filter1.Length}");
                        if (filter1.Length > 30)
                        {
                            Cv2.DrawContours(conMat1, new OpenCvSharp.Point[][] { filter1 }, 0, Scalar.White, 2, LineTypes.AntiAlias);

                            Debug.WriteLine($"Length: {consDiff1.Length} {filter1.Length}");
                            Debug.WriteLine($"{filter1[0]}");
                            Debug.WriteLine($"{filter1[1]}");
                            Debug.WriteLine($"{filter1[2]}");
                            Debug.WriteLine($"1: {filter1.Length}");
                        }

                        // 過濾小點點，串接陣列，過濾邊緣點
                        OpenCvSharp.Point[] filter2 = consDiff2.Where(c => c.Length > 20).Aggregate(Array.Empty<OpenCvSharp.Point>(), (acc, c) => acc.Concat(c).ToArray()).Where(pt =>
                        {
                            return xPos[cIdx + 1] + 3 < pt.X && pt.X < xPos[^2] - 3;
                        }).ToArray();

                        Debug.WriteLine($"2: {filter2.Length}");
                        if (filter2.Length > 30)
                        {
                            Cv2.DrawContours(conMat2, new OpenCvSharp.Point[][] { filter2 }, 0, Scalar.White, 2, LineTypes.AntiAlias);

                            Debug.WriteLine($"Length: {consDiff2.Length} {filter2.Length}");
                            Debug.WriteLine($"{filter2[0]}");
                            Debug.WriteLine($"{filter2[1]}");
                            Debug.WriteLine($"{filter2[2]}");
                            Debug.WriteLine($"2: {filter2.Length}");
                        }

                        //Mat conMat2 = new Mat(cannyWindow11.Height, cannyWindow11.Width, MatType.CV_8UC1, Scalar.Black);

                        //for (int i = 0; i < cons2.Length; i++)
                        //{
                        //    Cv2.DrawContours(conMat2, consDiff2, i, Scalar.White, 2);
                        //}

                        //Cv2.Resize(conMat1, conMat1, new OpenCvSharp.Size(leftRoiWindow.Width * 2 / 3, leftRoiWindow.Height * 2 / 3));
                        //Cv2.Resize(conMat2, conMat2, new OpenCvSharp.Size(rightRoiWindow.Width * 2 / 3, rightRoiWindow.Height * 2 / 3));

                        //Cv2.Resize(conMat1, conMat1, new OpenCvSharp.Size(leftRoiWindow.Width * 3 / 5, leftRoiWindow.Height * 3 / 5));
                        //Cv2.Resize(conMat2, conMat2, new OpenCvSharp.Size(rightRoiWindow.Width * 3 / 5, rightRoiWindow.Height * 3 / 5));


                        Cv2.ImShow("conMat1", conMat1);
                        Cv2.MoveWindow("conMat1", 10, -200);
                        Cv2.ImShow("conMat2", conMat2);
                        Cv2.MoveWindow("conMat2", 110, -200);

                        Cv2.ImShow("conMat22", cannyWindow33 - cannyWindow22 - cannyWindow11);


#if false

                        Cv2.ImShow("canny1", cannyWindow1);
                        Cv2.MoveWindow("canny1", 210, 0);
                        Cv2.ImShow("canny2", cannyWindow2);
                        Cv2.MoveWindow("canny2", 310, 0);
                        Cv2.ImShow("canny3", cannyWindow3);
                        Cv2.MoveWindow("canny3", 410, 0);
                        Cv2.ImShow("canny4", cannyWindow4);
                        Cv2.MoveWindow("canny4", 510, 0); 
#endif
                        //}

                        //Debug.WriteLine($"cons1 {cons1.Length} {cons1.Aggregate(0, (acc, c) => acc + c.Length)}  ;;;   cons111 {cons11.Length} {cons11.Aggregate(0, (acc, c) => acc + c.Length)}");
                        //Debug.WriteLine($"cons2 {cons2.Length} {cons2.Aggregate(0, (acc, c) => acc + c.Length)}  ;;;   cons122 {cons22.Length} {cons22.Aggregate(0, (acc, c) => acc + c.Length)}");
                        //Debug.WriteLine($"cons3 {cons3.Length} {cons3.Aggregate(0, (acc, c) => acc + c.Length)}  ;;;   cons133 {cons33.Length} {cons33.Aggregate(0, (acc, c) => acc + c.Length)}");
                        //Debug.WriteLine($"cons4 {cons4.Length} {cons4.Aggregate(0, (acc, c) => acc + c.Length)}  ;;;   cons144 {cons44.Length} {cons44.Aggregate(0, (acc, c) => acc + c.Length)}");
                        //Debug.WriteLine($"consDiff1 {consDiff1.Length} {consDiff1.Aggregate(0, (acc, c) => acc + c.Length)}  ;;;   consDiff2 {consDiff2.Length} {consDiff2.Aggregate(0, (acc, c) => acc + c.Length)}");
                        #endregion


                        #region 畫出標示 (之後移除)
                        // 找出 / 標示分界點
                        for (int i = 0; i < xPos.Length; i++)
                        {
                            Cv2.Circle(mat, new OpenCvSharp.Point(xPos[i], 960), 7, Scalar.Black, 3);
                        }
                        // 標示 窗戶 ROI
                        Cv2.Rectangle(mat, leftRoiWindow, Scalar.Gray, 2);
                        // 標示 窗戶 ROI
                        Cv2.Rectangle(mat, rightRoiWindow, Scalar.Gray, 2);
                        #endregion
                    }


                    //if (count == 4 &&　false)
                    //{
                    //    OpenCvSharp.Rect a1 = new OpenCvSharp.Rect((int)xPos[1] - 100, 200, (int)(xPos[2] - xPos[1]) + 200, 1400);

                    //    Methods.GetRoiCanny(mat, a1, 30, 100, out Mat cannyWindow);

                    //    Cv2.ImShow("window canny", cannyWindow);

                    //    Methods.GetHoughLinesV(mat, a1, 75, 150, out LineSegmentPoint[] lines);


                    //    foreach (LineSegmentPoint line in lines)
                    //    {
                    //        Cv2.Line(mat, line.P1, line.P2, Scalar.Yellow, 3);
                    //    }

                    //    Cv2.Rectangle(mat, a1, Scalar.Gray, 2);
                    //}
#endif


                    #endregion


                    // draw rect
                    //Cv2.Rectangle(mat, roi1, Scalar.Gray, 2);
                    //Cv2.Rectangle(mat, roi2, Scalar.Gray, 2);
                    //Cv2.Rectangle(mat, a1, Scalar.Gray, 2);
                    //Cv2.ImShow("window canny", cannyWindow);

                    MainWindow.ImageSource = mat.ToImageSource();
                    //Debug.WriteLine($"width: {width} maxWindowWidth: {maxWindowWidth}");
                    //Debug.WriteLine($"1st {step1done}, 2nd {step2done} 3rd {step3done} 4th {step4done}");
                    Debug.WriteLine($"{DateTime.Now:ss.fff}");
                    Debug.WriteLine("------------------------------------------------");
                });
            }
        }
        #endregion


        #region 
        private async Task TurnTubeToZeroPos()
        {
            await Task.Run(async () =>
            {
                Camera camera = MainWindow.BaslerCam.Camera;
                // 設為 Trigger Mode
                camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.On);

                if (!camera.StreamGrabber.IsGrabbing)
                {
                    // 啟動 Grabber
                    camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByUser);
                }

                double windowWidth = 0;
                int moveCount = 0;
                double pulse = 0;

                //while (true)
                //{
                camera.WaitForFrameTriggerReady(3000, TimeoutHandling.Return);
                camera.ExecuteSoftwareTrigger();


                using IGrabResult grabResult = camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.Return);
                double pps = MainWindow.ServoMotion.Axes[1].PosActual;

                if (grabResult.GrabSucceeded)
                {
                 


                }
                else
                {
                    Debug.WriteLine(grabResult.ErrorCode);
                }
                //}

                //Debug.WriteLine($"Width: {windowWidth}, Pulse: {pulse}");
            });
        }


        #region 包 struct
        bool step1done = false;
        bool step2done = false;
        bool step3done = false;
        bool step4done = false;
        #endregion

        double lastWindowWidth = 0;
        double maxWindowWidth = 0;


        /// <summary>
        /// 工件對位用結構
        /// </summary>
        struct CounterPos1
        {
            ushort lastWindowWidth;
            ushort maxWindowWidth;
            byte steps;
        }


        private void Method()
        {

        }

        #endregion
    }
}
