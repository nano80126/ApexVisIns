using Basler.Pylon;
using OpenCvSharp;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

/*
 無用 cs ?
 */

namespace ApexVisIns
{
    public partial class MainWindow : System.Windows.Window
    {
        /// <summary>
        /// Basler 相機連線
        /// </summary>
        /// <param name="serialNumber"></param>
        /// <returns></returns>
        private bool Basler_Connect(string serialNumber)
        {
            //if (camera == null)
            //{
            try
            {
                BaslerCam.CreateCam(serialNumber);
                //camera = new BaslerCam(serialNumber);
                //camera.Camera = new Camera(serialNumber);

                BaslerCam.Camera.CameraOpened += BaslerCam_CameraOpened;
                BaslerCam.Camera.CameraClosing += BaslerCam_CameraClosing;
                BaslerCam.Camera.CameraClosed += BaslerCam_CameraClosed;

                //BaslerCam.Camera.Open();
                BaslerCam.Open();
                BaslerCam.PropertyChange();
            }
            catch (Exception ex)
            {
                MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, ex.Message);
                return false;
            }
            //}
            return BaslerCam.IsOpen;
        }

        /// <summary>
        /// Basler 相機斷線 ()
        /// </summary>
        /// <returns></returns>
        private bool Basler_Disconnect()
        {
            try
            {
                // 釋放相機物件
                BaslerCam.Close();
                //BaslerCam.Camera.Close();
                //BaslerCam.Camera.Dispose();
                //BaslerCam.Camera = null;
                // GC 回收
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ex)
            {
                MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, ex.Message);
            }
            return false;
        }

        /// <summary>
        /// Basler 相機單張擷取
        /// </summary>
        private void Basler_SingleGrab()
        {
            try
            {
                if (!BaslerCam.Camera.StreamGrabber.IsGrabbing)
                {
                    //camera.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.On);
                    BaslerCam.Camera.StreamGrabber.Start(1, GrabStrategy.LatestImages, GrabLoop.ProvidedByUser);

                    // 等待 Trigger Ready // 暫保留 (啟用可能丟出 exception)
                    // if (camera.Camera.WaitForFrameTriggerReady(1000, TimeoutHandling.ThrowException))
                    // {
                    BaslerCam.Camera.ExecuteSoftwareTrigger();
                    _ = BaslerCam.Camera.StreamGrabber.RetrieveResult(250, TimeoutHandling.ThrowException);
                    // }
                }
            }
            catch (TimeoutException T)
            {
                // Display in message list
                MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, T.Message);

            }
            catch (InvalidOperationException Invalid)
            {
                // Display in message list
                MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, Invalid.Message);

            }
            catch (Exception ex)
            {
                // Display in message list
                MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, ex.Message);
            }
            finally
            {
                // 關閉 StreamGrabber
                BaslerCam.Camera.StreamGrabber.Stop();
                //Console.WriteLine($"IsGrabbing : {BaslerCam.Camera.StreamGrabber.IsGrabbing}");
            }
        }

        /// <summary>
        /// Basler 相機連續擷取
        /// </summary>
        private void Basler_ContinouseGrab()
        {
            try
            {
                if (!BaslerCam.Camera.StreamGrabber.IsGrabbing)
                {
                    BaslerCam.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.Off);          // 關閉 trigger 模式
                    BaslerCam.Camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByStreamGrabber);     // 啟動 Grabber
                }
                else
                {
                    BaslerCam.Camera.StreamGrabber.Stop();                                                                 // 停止 Grabber 
                    BaslerCam.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.On);           // 啟動 trigger 模式
                }
            }
            catch (TimeoutException T)
            {
                MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, T.Message);
            }
            catch (InvalidOperationException Invalid)
            {
                MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, Invalid.Message);
            }
            catch (Exception ex)
            {
                MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, ex.Message);
            }
            //finally
            //{
            //    Console.WriteLine($"IsGrabbing : {camera.Camera.StreamGrabber.IsGrabbing}");
            //}
        }

        /// <summary>
        /// 影像擷取事件，圖片處理進入點，待分離
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StreamGrabber_ImageGrabbed(object sender, ImageGrabbedEventArgs e)
        {
            IGrabResult grabResult = e.GrabResult;
            if (grabResult.GrabSucceeded)
            {
                // // // // // // // // // // // // // // // // // //

                //Console.WriteLine($"Image number: {grabResult.ImageNumber}");
                //Console.WriteLine($"Size: {grabResult.Width}, {grabResult.Height}");

                // // // // // // // // // // // // // // // // // //
                // Bitmap bmp = GrabResultToBitmap(grabResult);  // Conver
                // Mat mat = GrabResultToMatColor(grabResult);
                Mat mat = GrabResultToMatMono(grabResult);
                // Mat m = GrabResultToMat(grabResult);
                // // // // // // // // // // // // // // // // // //
                BaslerCam.Frames = (int)grabResult.ImageNumber;

#if NOPROCESS
                Dispatcher.Invoke(() =>
                {
                    //IndicateRGB.Image = mat;
                    ImageSource = mat.ToImageSource();
                });
#else   // PROCESSs

                // // // // // // 開始辨識 // // // // // //
                if (IsProcessing)
                {
                    Dispatcher.Invoke(() =>
                    {
                        //Frames = (int)grabResult.ImageNumber;
                        //WpfImage.Source = bmp.ToBitmapSource();
                        //IndicateRGB.Image = bmp.ToMat();
                        ImageSource = mat.ToImageSource();
                    });
                }
                else
                {
                    IsProcessing = true;
                    // // // // // // // // // // // // // // // // // //
#if DEBUG
                    processSw.Restart();
#endif
                    #region 圖像處理邏輯
                    //Debug.WriteLine($"{mat.Width} {mat.Height}");
                    //ProcessNitinol(mat);


                    ProcessApex(mat);

                    //ProcessApex(mat);
                    //mat.SaveImage("apex1.jpg");
                    #endregion
#if DEBUG
                    Debug.WriteLine($"Processing takes {processSw.ElapsedMilliseconds} ms");
                    processSw.Stop();
#endif
                    // // // // // // // // // // // // // // // // // //
                    IsProcessing = false;
                }

                // GC at each 60 frames
                // if (grabResult.ImageNumber % 60 == 0)
                // {
                //     //GC.Collect();
                //     //GC.WaitForPendingFinalizers();
                // }
#endif
            }
        }

        /// <summary>
        /// Basler StreamGrabber 開始擷取事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StreamGrabber_GrabStarted(object sender, EventArgs e)
        {
            Debug.WriteLine("Grabber Start");
            //MsgInformer.AddError(MsgInformer.Message.MsgCode.C, "Grabber started", MsgInformer.Message.MessageType.Info);
            MsgInformer.AddInfo(MsgInformer.Message.MsgCode.CAMERA, "Grabber started");
            BaslerCam.PropertyChange("IsGrabbing");    // Update Property
        }

        /// <summary>
        /// Basler StreamGrabber 停止擷取事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StreamGrabber_GrabStopped(object sender, GrabStopEventArgs e)
        {
            //camera.Camera.StreamGrabber.GrabStarted -= StreamGrabber_GrabStarted;
            //camera.Camera.StreamGrabber.GrabStopped -= StreamGrabber_GrabStopped;
            //camera.Camera.StreamGrabber.ImageGrabbed -= StreamGrabber_ImageGrabbed;
            BaslerCam.PropertyChange("IsGrabbing");    // Update Property
            Debug.WriteLine("Grabber Stop");
            //MsgInformer.AddError(MsgInformer.Message.MsgCode.C, "Grabber stoped", MsgInformer.Message.MessageType.Info);
            MsgInformer.AddInfo(MsgInformer.Message.MsgCode.CAMERA, "Grabber stoped");
        }

        /// <summary>
        /// 相機 opened 事件，參數寫入在這
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BaslerCam_CameraOpened(object sender, EventArgs e)
        {
            #region Camera Config
            Camera cam = sender as Camera;

            #region Heartbeat Timeout 設定 30 秒 (若程式中斷，與相機斷線秒數)
            cam.Parameters[PLGigECamera.GevHeartbeatTimeout].SetValue(1000 * 30);
            #endregion

            // 取得最大解析
            BaslerCam.WidthMax = (int)cam.Parameters[PLGigECamera.WidthMax].GetValue();
            BaslerCam.HeightMax = (int)cam.Parameters[PLGigECamera.HeightMax].GetValue();
            // Console.WriteLine($"Max Resolution {camera.WidthMax}, {camera.HeightMax}");

            #region 可調整參數
            // 取得 OFFSET
            cam.Parameters[PLGigECamera.OffsetX].SetToMinimum();
            BaslerCam.OffsetX = (int)cam.Parameters[PLGigECamera.OffsetX].GetValue();
            cam.Parameters[PLGigECamera.OffsetY].SetToMinimum();
            BaslerCam.OffsetY = (int)cam.Parameters[PLGigECamera.OffsetY].GetValue();

            // 設定解析度, 嘗試設定為目標解析度, 失敗則設為最大值
            if (!cam.Parameters[PLGigECamera.Width].TrySetValue(CAMWIDTH))
            {
                cam.Parameters[PLGigECamera.Width].SetToMaximum();  // must set to other value small than 2040 
            }
            BaslerCam.Width = (int)cam.Parameters[PLGigECamera.Width].GetValue();

            if (!cam.Parameters[PLGigECamera.Height].TrySetValue(CAMHEIGHT))
            {
                cam.Parameters[PLGigECamera.Height].SetToMaximum(); // must set to other value small than 2040
            }
            BaslerCam.Height = (int)cam.Parameters[PLGigECamera.Height].GetValue();

            // 取得最大 OFFSET
            BaslerCam.OffsetXMax = (int)cam.Parameters[PLGigECamera.OffsetX].GetMaximum();
            BaslerCam.OffsetYMax = (int)cam.Parameters[PLGigECamera.OffsetY].GetMaximum();

            // 灰階設定
            cam.Parameters[PLGigECamera.PixelFormat].SetValue(PLGigECamera.PixelFormat.Mono8);

            // FPS 設定
            cam.Parameters[PLGigECamera.AcquisitionFrameRateEnable].SetValue(true); // 強制鎖定 FPS
            cam.Parameters[PLGigECamera.AcquisitionFrameRateAbs].SetValue(12);      // 設定 FPS
            BaslerCam.FPS = cam.Parameters[PLGigECamera.AcquisitionFrameRateAbs].GetValue();

            // 曝光時間設定
            cam.Parameters[PLGigECamera.ExposureMode].SetValue(PLGigECamera.ExposureMode.Timed);
            cam.Parameters[PLGigECamera.ExposureAuto].SetValue(PLGigECamera.ExposureAuto.Off);
            cam.Parameters[PLGigECamera.ExposureTimeAbs].SetValue(10000);   // 10000 is default exposure time of acA2040
            //BaslerCam.ExposureTimeAbs = cam.Parameters[PLGigECamera.ExposureTimeAbs].GetValue();
            BaslerCam.ExposureTime = cam.Parameters[PLGigECamera.ExposureTimeAbs].GetValue();

            // 設定擷取模式為連續
            cam.Parameters[PLGigECamera.AcquisitionMode].SetValue(PLGigECamera.AcquisitionMode.Continuous);

            // 設定 trigger
            cam.Parameters[PLGigECamera.TriggerSelector].SetValue(PLGigECamera.TriggerSelector.FrameStart);
            cam.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.On);
            cam.Parameters[PLGigECamera.TriggerSource].SetValue(PLGigECamera.TriggerSource.Software);
            // Console.WriteLine(cam.WaitForFrameTriggerReady(500, TimeoutHandling.ThrowException)); // do not know how to use // FAQ
            #endregion

            #region Grabber Event 
            BaslerCam.Camera.StreamGrabber.GrabStarted += StreamGrabber_GrabStarted;
            BaslerCam.Camera.StreamGrabber.GrabStopped += StreamGrabber_GrabStopped;
            BaslerCam.Camera.StreamGrabber.ImageGrabbed += StreamGrabber_ImageGrabbed;
            #endregion


            string modelName = cam.CameraInfo[CameraInfoKey.ModelName];
            string serialNumber = cam.CameraInfo[CameraInfoKey.SerialNumber];
            #endregion

            // 觸發 PropertyChanged
            BaslerCam.PropertyChange();
            //SyncConfiguration(BaslerCam.Config, BaslerCam);     // 同步組態 和 相機當前設定
            //ConfigPanel.SyncConfiguration(BaslerCam.Config, BaslerCam);     // 同步組態 和 相機當前設定

            // 變更縮放大小
            //ZoomRatio = 100;    // Set Zoom Ratio to 100, or it will occurred some bugs

            // 更改 Title
            Title = $"Model: {modelName}, S/N: {serialNumber}";

            // 暫停 Camera Finder
            //finder.Pause();
            CameraEnumer.WorkerPause();
        }

        /// <summary>
        /// 相機關閉事件，解除事件綁定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BaslerCam_CameraClosing(object sender, EventArgs e)
        {
            BaslerCam.Camera.StreamGrabber.GrabStarted -= StreamGrabber_GrabStarted;
            BaslerCam.Camera.StreamGrabber.GrabStopped -= StreamGrabber_GrabStopped;
            BaslerCam.Camera.StreamGrabber.ImageGrabbed -= StreamGrabber_ImageGrabbed;
            // CamConnect.DataContext = null;
        }

        /// <summary>
        /// 相機 closed 事件，變更Title
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BaslerCam_CameraClosed(object sender, EventArgs e)
        {
            // 更改 Title
            Title = "No camera opened";

            // 回復 Camera Finder
            //finder.Resume();
            CameraEnumer.WorkerResume();    // 繼續尋找相機

            // 觸發 PropertyChange
            BaslerCam.PropertyChange();
        }

        /// <summary>
        /// Convert Grab Result to Bitmap
        /// </summary>
        /// <param name="result">GrabResult</param>
        /// <returns>Bitmap</returns>
        private Bitmap GrabResultToBitmap(IGrabResult result)
        {
            Bitmap bitmap = new Bitmap(result.Width, result.Height, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);

            pxConverter.OutputPixelFormat = PixelType.RGB8packed;
            IntPtr intPtr = bmpData.Scan0;
            pxConverter.Convert(intPtr, bmpData.Width * bmpData.Height * 3, result);
            bitmap.UnlockBits(bmpData);

            return bitmap;
        }

        /// <summary>
        /// Convert Grab Result to Mono Mat
        /// </summary>
        /// <param name="result">GrabResult</param>
        /// <returns>Mono Mat</returns>
        private Mat GrabResultToMatMono(IGrabResult result)
        {
            Mat mat = new(result.Height, result.Width, MatType.CV_8UC1);
            //pxConverter.OutputPixelFormat = PixelType.Mono8;  //預設為 mono
            pxConverter.Convert(mat.Ptr(0), result.Width * result.Height, result);
            return mat;
        }

        /// <summary>
        /// Convert GraB Result to Color Mat
        /// </summary>
        /// <param name="result">GrabResult</param>
        /// <returns>Color Mat</returns>
        private Mat GrabResultToMatColor(IGrabResult result)
        {
            Mat mat = new(result.Height, result.Width, MatType.CV_8UC3);
            pxConverter.OutputPixelFormat = PixelType.RGB8packed;
            pxConverter.Convert(mat.Ptr(0), result.Width * result.Height * 3, result);
            return mat;
        }
    }
}
