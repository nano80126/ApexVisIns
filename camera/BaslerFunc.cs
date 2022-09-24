using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Basler.Pylon;
using OpenCvSharp;
using System.Windows.Media;
using MCAJawIns.Tab;
using System.Threading;

namespace MCAJawIns
{
    /// <summary>
    /// General Basler Function For Some Operation (暫時保留)
    /// </summary>
    public class BaslerFunc
    {
        private static readonly PixelDataConverter pxConverter = new();

#if false
        public static bool Basler_Connect(BaslerCam cam, string serialNumber)
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

        public static bool Basler_Disconnect(BaslerCam cam)
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

        public static void Basler_SingleGrab(BaslerCam cam)
        {
            try
            {
                if (!cam.Camera.StreamGrabber.IsGrabbing)
                {
                    cam.Camera.StreamGrabber.Start(1, Basler.Pylon.GrabStrategy.LatestImages, Basler.Pylon.GrabLoop.ProvidedByUser);


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


        /// <summary>
        /// 相機 Opened 事件，參數寫入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Camera_CameraOpened(object sender, EventArgs e)
        {
            Camera cam = sender as Camera;

        #region HeartBeat Timeout 30 Seconds (程式中斷後 Timeount 秒數)
            cam.Parameters[PLGigECamera.GevHeartbeatTimeout].SetValue(1000 * 30);
        #endregion

        #region Camera Info
            string modelName = cam.CameraInfo[CameraInfoKey.ModelName];
            string serialNumber = cam.CameraInfo[CameraInfoKey.SerialNumber];
        #endregion

            /// Find camera of specific serial number
            BaslerCam baslerCam = Array.Find(MainWindow.BaslerCams, item => item.SerialNumber == serialNumber);

            baslerCam.WidthMax = (int)cam.Parameters[PLGigECamera.WidthMax].GetValue();
            baslerCam.HeightMax = (int)cam.Parameters[PLGigECamera.HeightMax].GetValue();

        #region Adjustable parameters
            cam.Parameters[PLGigECamera.OffsetX].SetToMinimum();
            baslerCam.OffsetX = (int)cam.Parameters[PLGigECamera.OffsetX].GetValue();
            cam.Parameters[PLGigECamera.OffsetY].SetToMinimum();
            baslerCam.OffsetY = (int)cam.Parameters[PLGigECamera.OffsetY].GetValue();

            // CAM_WIDTH 待變更
            if (!cam.Parameters[PLGigECamera.Width].TrySetValue(MainWindow.CAM_WIDTH))
            {
                cam.Parameters[PLGigECamera.Width].SetToMaximum();  // must set to other value small than 2040 
            }
            baslerCam.Width = (int)cam.Parameters[PLGigECamera.Width].GetValue();

            // CAM_HEIGHT 待變更
            if (!cam.Parameters[PLGigECamera.Height].TrySetValue(MainWindow.CAM_HEIGHT))
            {
                cam.Parameters[PLGigECamera.Height].SetToMaximum(); // must set to other value small than 2040
            }
            baslerCam.Height = (int)cam.Parameters[PLGigECamera.Height].GetValue();

            // 取得最大 OFFSET
            baslerCam.OffsetXMax = (int)cam.Parameters[PLGigECamera.OffsetX].GetMaximum();
            baslerCam.OffsetYMax = (int)cam.Parameters[PLGigECamera.OffsetY].GetMaximum();

            // 灰階設定 (之後皆為 mono)
            cam.Parameters[PLGigECamera.PixelFormat].SetValue(PLGigECamera.PixelFormat.Mono8);

            // FPS 設定
            cam.Parameters[PLGigECamera.AcquisitionFrameRateEnable].SetValue(true); // 鎖定 FPS (不需要太快張數)
            cam.Parameters[PLGigECamera.AcquisitionFrameRateAbs].SetValue(12);      // 設定 FPS
            baslerCam.FPS = cam.Parameters[PLGigECamera.AcquisitionFrameRateAbs].GetValue();

            // 曝光時間設定
            cam.Parameters[PLGigECamera.ExposureMode].SetValue(PLGigECamera.ExposureMode.Timed);    // 曝光模式 Timed
            cam.Parameters[PLGigECamera.ExposureAuto].SetValue(PLGigECamera.ExposureAuto.Off);      // 關閉自動曝光
            cam.Parameters[PLGigECamera.ExposureTimeAbs].SetValue(10000);   // 10000 is default exposure time of acA2040

            baslerCam.ExposureTime = cam.Parameters[PLGigECamera.ExposureTimeAbs].GetValue();

            // 設定擷取模式為連續
            cam.Parameters[PLGigECamera.AcquisitionMode].SetValue(PLGigECamera.AcquisitionMode.Continuous);

            // 設定 Trigger
            cam.Parameters[PLGigECamera.TriggerSelector].SetValue(PLGigECamera.TriggerSelector.FrameStart);
            cam.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.On);
            cam.Parameters[PLGigECamera.TriggerSource].SetValue(PLGigECamera.TriggerSource.Software);
        #endregion

        #region Grabber Event
            baslerCam.Camera.StreamGrabber.GrabStarted += StreamGrabber_GrabStarted;
            baslerCam.Camera.StreamGrabber.GrabStopped += StreamGrabber_GrabStopped;
            //baslerCam.Camera.StreamGrabber.ImageGrabbed += StreamGrabber_ImageGrabbed;
            //baslerCam.Camera.StreamGrabber.ImageGrabbed += content.DebugTab.StreamGrabber_ImageGrabbed;

            baslerCam.Camera.StreamGrabber.UserData = "abc";
        #endregion

            // 觸發 PropertyChange
            baslerCam.PropertyChange();

            // 變更 Zoom Ratio

            // 
        }

        /// <summary>
        /// 相機 Closing 事件，解除事件綁定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Camera_CameraClosing(object sender, EventArgs e)
        {
            Camera cam = sender as Camera;
            string serialNumber = cam.CameraInfo[CameraInfoKey.SerialNumber];

            BaslerCam baslerCam = Array.Find(MainWindow.BaslerCams, item => item.SerialNumber == serialNumber);

            baslerCam.Camera.StreamGrabber.GrabStarted -= StreamGrabber_GrabStarted;
            baslerCam.Camera.StreamGrabber.GrabStopped -= StreamGrabber_GrabStopped;
            baslerCam.Camera.StreamGrabber.ImageGrabbed -= StreamGrabber_ImageGrabbed;
        }

        /// <summary>
        /// 相機 Closed 事件，觸發 PropertyChange
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Camera_CameraClosed(object sender, EventArgs e)
        {
            // 不變更 Title 

            // 暫不啟用 Enumerator

            Camera cam = sender as Camera;
            string serialNumber = cam.CameraInfo[CameraInfoKey.SerialNumber];

            BaslerCam baslerCam = Array.Find(MainWindow.BaslerCams, item => item.SerialNumber == serialNumber);

            baslerCam.PropertyChange();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void StreamGrabber_GrabStarted(object sender, EventArgs e)
        {
            Debug.WriteLine("Grabber Start");
            MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.C, "Grabber started");
            //DebugTab.Cam.PropertyChange("IsGrabbing");

        #region Reset Struct

        #endregion
        }

        private static void StreamGrabber_GrabStopped(object sender, GrabStopEventArgs e)
        {
            Debug.WriteLine("Grabber Stop");
            MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.C, "Grabber stoped");
            //content.DebugTab.Cam.PropertyChange("IsGrabbing");
        }

        /// <summary>
        /// 影像擷取事件，圖片處理進入點
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void StreamGrabber_ImageGrabbed(object sender, ImageGrabbedEventArgs e)
        {
            Debug.WriteLine(e.GrabResult.StreamGrabberUserData);

            IGrabResult grabResult = e.GrabResult;

            if (grabResult.GrabSucceeded)
            {
                Mat mat = GrabResultToMatMono(grabResult);

                // Frames 張數
                // (int)grabResult.ImageNumber;


                //MainWindow.ImgSrc = src;
            }

            Debug.WriteLine(e.GrabResult.ImageNumber);
        }

#endif

        /// <summary>
        /// Convert Basler Camera Data to Mat Color
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public static Bitmap GrabResultToBitmap(IGrabResult result)
        {
            Bitmap bitmap = new(result.Width, result.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);

            // pxConverter.OutputPixelFormat = PixelType.Mono8;
            pxConverter.OutputPixelFormat = PixelType.RGB8packed;
            // Console.WriteLine($"stride: {bmpData.Stride}, width: {bmpData.Width}, height: {bmpData.Height}");
            IntPtr intPtr = bmpData.Scan0;
            // pxConverter.Convert(intPtr, bmpData.Stride * bmpData.Height, result);
            pxConverter.Convert(intPtr, bmpData.Width * bmpData.Height * 3, result);
            bitmap.UnlockBits(bmpData);

            return bitmap;
        }

        /// <summary>
        /// Convert Basler Camera Data to Mat Mono
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public static Mat GrabResultToMatMono(IGrabResult result)
        {
            Mat mat = new(result.Height, result.Width, MatType.CV_8UC1);
            pxConverter.OutputPixelFormat = PixelType.Mono8;
            pxConverter.Convert(mat.Ptr(0), result.Width * result.Height, result);
            return mat;
        }

        /// <summary>
        /// Convert Basler Camera Data to Mat Color
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public static Mat GraResultToMatColor(IGrabResult result)
        {
            Mat mat = new(result.Height, result.Width, MatType.CV_8UC3);
            pxConverter.OutputPixelFormat = PixelType.RGB8packed;
            pxConverter.Convert(mat.Ptr(0), result.Width * result.Height * 3, result);
            return mat;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        /// <summary>
        /// 相機連線
        /// </summary>
        /// <param name="cam"></param>
        /// <param name="serialNumber"></param>
        /// <param name="userData"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public bool Basler_Connect(BaslerCam cam, string serialNumber, object userData, CancellationToken ct)
        {
            int retryCount = 0;
            //Debug.WriteLine($"{cam.IsConnected} {serialNumber} {userData}");

            while (!cam.IsOpen)
            {
                if (ct.IsCancellationRequested) { break; }

                if (retryCount > 3) { break; }

                try
                {
                    // 建立相機
                    cam.CreateCam(serialNumber);
                    // 先更新 SerialNumer，CameraOpened 事件比對時須用到
                    cam.SerialNumber = serialNumber;

                    // 綁定事件
                    cam.Camera.CameraOpened += Camera_CameraOpened; ;
                    cam.Camera.CameraClosing += Camera_CameraClosing; ;
                    cam.Camera.CameraClosed += Camera_CameraClosed; ;

                    // 設定 UserData, ImageGrabber 事件會用到
                    cam.Camera.StreamGrabber.UserData = userData;

                    // 開始相機
                    cam.Open();
                    cam.PropertyChange();

                }
                catch (Exception ex)
                {
                    MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, ex.Message);
                    // 重試次數++
                    retryCount++;
                    // 等待 200 ms
                    _ = SpinWait.SpinUntil(() => false, 200);
                }
            }
            return cam.IsOpen;
        }

        /// <summary>
        /// 相機關閉
        /// </summary>
        /// <param name="cam"></param>
        /// <returns></returns>
        public bool Basler_Disconnect(BaslerCam cam)
        {
            try
            {
                if (cam != null)
                {
                    cam.Camera.CameraOpened -= Camera_CameraOpened;
                    cam.Camera.CameraClosing -= Camera_CameraClosing;
                    cam.Camera.CameraClosed -= Camera_CameraClosed;
                    cam.Close();
                }

                // GC 回收
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ex)
            {
                MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, ex.Message);
                //throw;
            }

            return false;
        }

        /// <summary>
        /// 啟動 Grabber
        /// </summary>
        /// <param name="cam"></param>
        public void Basler_StartStreamGrabber(BaslerCam cam)
        {
            try
            {
                if (!cam.Camera.StreamGrabber.IsGrabbing)
                {
                    // 啟動觸發模式
                    cam.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.On);
                    cam.IsTriggerMode = true;

                    // 啟動 StreamGrabber 連續拍攝
                    cam.Camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByUser);

                    _ = cam.Camera.WaitForFrameTriggerReady(500, TimeoutHandling.ThrowException);
                    // cam.IsContinuousGrabbing = true;
                    // cam.IsContinuousGrabbing = false;

                    // 取消綁定事件
                    cam.Camera.StreamGrabber.ImageGrabbed -= StreamGrabber_ImageGrabbed;
                    // Debug.WriteLine($"{cam.Camera.StreamGrabber.UserData} {cam.Camera.StreamGrabber.UserData.GetType()}");
                }
            }
            catch (TimeoutException T)
            {
                MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, T.Message);
            }
            catch (InvalidOperationException I)
            {
                MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, I.Message);
            }
            catch (Exception E)
            {
                MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, E.Message);
            }
        }

        /// <summary>
        /// 停止 Grabber
        /// </summary>
        /// <param name="cam"></param>
        public void Basler_StopStreamGrabber(BaslerCam cam)
        {
            try
            {
                if (cam.Camera.StreamGrabber.IsGrabbing)
                {
                    // 停止 StreamGrabber
                    cam.Camera.StreamGrabber.Stop();
                    //cam.IsGrabberOpened = false;

                    // 關閉觸發模式
                    cam.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.Off);
                    cam.IsTriggerMode = false;

                    cam.Camera.StreamGrabber.ImageGrabbed += StreamGrabber_ImageGrabbed;
                }
            }
            catch (TimeoutException T)
            {
                MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, T.Message);
            }
            catch (InvalidOperationException I)
            {
                MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, I.Message);
            }
            catch (Exception E)
            {
                MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, E.Message);
            }
        }

        /// <summary>
        /// Grabber retrieve a mat
        /// </summary>
        /// <param name="cam">BaslerCam 物件</param>
        public Mat Basler_RetrieveResult(BaslerCam cam)
        {
            Mat mat = null;
            for (int i = 0; i < 3; i++)
            {
                bool ready = cam.Camera.WaitForFrameTriggerReady(100, TimeoutHandling.Return);
                if (ready)
                {
                    cam.Camera.ExecuteSoftwareTrigger();
                    IGrabResult grabResult = cam.Camera.StreamGrabber.RetrieveResult(125, TimeoutHandling.Return);

                    if (grabResult?.GrabSucceeded == true)
                    {
                        mat = BaslerFunc.GrabResultToMatMono(grabResult);
                        break;
                    }
                }
            }
            return mat;
        }

        /// <summary>
        /// 單張拍攝
        /// </summary>
        /// <param name="cam"></param>
        public void Basler_SingleGrab(BaslerCam cam)
        {
            try
            {
                if (!cam.Camera.StreamGrabber.IsGrabbing)
                {
                    // 啟動 StreamGrabber 拍攝一張
                    //cam.Camera.StreamGrabber.Start(1, GrabStrategy.LatestImages, GrabLoop.ProvidedByUser);
                    cam.Camera.StreamGrabber.Start(1, GrabStrategy.LatestImages, GrabLoop.ProvidedByStreamGrabber);

                    // cam.Camera.ExecuteSoftwareTrigger();
                    //_ = cam.Camera.StreamGrabber.RetrieveResult(250, TimeoutHandling.ThrowException);
                }
            }
            catch (TimeoutException T)
            {
                MsgInformer.AddError(MsgInformer.Message.MsgCode.CAMERA, T.Message);
            }
            catch (InvalidOperationException I)
            {
                MsgInformer.AddError(MsgInformer.Message.MsgCode.CAMERA, I.Message);
            }
            catch (Exception E)
            {
                MsgInformer.AddError(MsgInformer.Message.MsgCode.CAMERA, E.Message);
            }
        }

        /// <summary>
        /// 連續拍攝
        /// </summary>
        /// <param name="cam"></param>
        public void Basler_ContinousGrab(BaslerCam cam)
        {
            try
            {
                if (!cam.Camera.StreamGrabber.IsGrabbing)
                {
                    // 關閉 Trigger Mode
                    // cam.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.Off);
                    // 開始拍攝
                    cam.Camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByStreamGrabber);

                    // 變更 Flag 連續拍攝
                    // cam.IsContinuousGrabbing = true;
                }
                else
                {
                    // 停止開設
                    cam.Camera.StreamGrabber.Stop();
                    //cam.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.On);

                    // 變更 Flag
                    // cam.IsContinuousGrabbing = false;
                }
            }
            catch (TimeoutException T)
            {
                MsgInformer.AddError(MsgInformer.Message.MsgCode.CAMERA, T.Message);
            }
            catch (InvalidOperationException I)
            {
                MsgInformer.AddError(MsgInformer.Message.MsgCode.CAMERA, I.Message);
            }
            catch (Exception E)
            {
                MsgInformer.AddError(MsgInformer.Message.MsgCode.CAMERA, E.Message);
            }
        }

        /// <summary>
        /// 相機開啟事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Camera_CameraOpened(object sender, EventArgs e)
        {
            Camera camera = sender as Camera;

            #region HeartBeat Timeout 30 sec
            camera.Parameters[PLGigECamera.GevHeartbeatTimeout].SetValue(30 * 1000);
            #endregion

            #region Get basic camera info
            string modelName = camera.CameraInfo[CameraInfoKey.ModelName];
            string serialNumber = camera.CameraInfo[CameraInfoKey.SerialNumber];
            #endregion

            BaslerCam baslerCam = BaslerCams.First(e => e.SerialNumber == serialNumber);

            if (baslerCam != null)
            {
                baslerCam.ModelName = modelName;

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
                // 可能需要轉為客製
                baslerCam.Camera.StreamGrabber.ImageGrabbed += StreamGrabber_ImageGrabbed;
                #endregion

                baslerCam.PropertyChange();
            }
            else
            {
                MsgInformer.AddError(MsgInformer.Message.MsgCode.CAMERA, "相機 S/N 設置有誤");
            }
        }

        private void Camera_CameraClosing(object sender, EventArgs e)
        {
            Camera camera = sender as Camera;

            camera.StreamGrabber.ImageGrabbed -= StreamGrabber_GrabStarted;
            camera.StreamGrabber.GrabStopped -= StreamGrabber_GrabStopped;
            camera.StreamGrabber.ImageGrabbed -= StreamGrabber_ImageGrabbed;
        }

        private void Camera_CameraClosed(object sender, EventArgs e)
        {
            // nothing to do;
        }

        private void StreamGrabber_GrabStarted(object sender, EventArgs e)
        {
            Debug.WriteLine("Grabber Started");
            string userData = (sender as IStreamGrabber).UserData.ToString();

            BaslerCam baslerCam = Array.Find(BaslerCams, cam => cam.Camera?.StreamGrabber.UserData.ToString() == userData);
            baslerCam.PropertyChange(nameof(baslerCam.IsGrabbing));
        }

        private void StreamGrabber_GrabStopped(object sender, GrabStopEventArgs e)
        {
            Debug.WriteLine("Grabber Stopped");
            string userData = (sender as IStreamGrabber).UserData.ToString();

            BaslerCam baslerCam = Array.Find(BaslerCams, cam => cam.Camera?.StreamGrabber.UserData.ToString() == userData);
            baslerCam.PropertyChange(nameof(baslerCam.IsGrabbing));
            // throw new NotImplementedException();
        }

        /// <summary>
        /// StreamGrabber 擷取事件(測試使用，不上線使用)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StreamGrabber_ImageGrabbed(object sender, ImageGrabbedEventArgs e)
        {
            IGrabResult grabResult = e.GrabResult;

            if (grabResult.GrabSucceeded)
            {
                Mat mat = BaslerFunc.GrabResultToMatMono(grabResult);

                TargetFeature targetFeature = (TargetFeature)e.GrabResult.StreamGrabberUserData;

                switch (targetFeature)
                {
                    case TargetFeature.MCA_Front:
                        Dispatcher.Invoke(() =>
                        {
                            ImageSource1 = mat.ToImageSource();
                        });
                        break;
                    case TargetFeature.MCA_Bottom:
                        Dispatcher.Invoke(() =>
                        {
                            ImageSource2 = mat.ToImageSource();
                        });
                        break;
                    case TargetFeature.MCA_SIDE:
                        Dispatcher.Invoke(() =>
                        {
                            ImageSource3 = mat.ToImageSource();
                        });
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
