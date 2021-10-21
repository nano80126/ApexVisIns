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

namespace ApexVisIns
{

    /// <summary>
    /// General Basler Function For Some Operation
    /// </summary>
    public class BaslerFunc
    {
        private readonly PixelDataConverter pxConverter = new();


        public static bool Basler_Connect(BaslerCam cam, string serialNumber)
        {
            try
            {
                cam.CreateCam(serialNumber);

                //
                cam.Camera.CameraOpened += Camera_CameraOpened;
                cam.Camera.CameraClosing += Camera_CameraClosing;
                cam.Camera.CameraClosed += Camera_CameraClosed;
                //
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
            baslerCam.Camera.StreamGrabber.ImageGrabbed += StreamGrabber_ImageGrabbed;

            #endregion
                
            // 觸發 PropertyChange
            baslerCam.PropertyChange();
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




            #region Reset Struct


            #endregion
        }


        private static void StreamGrabber_GrabStopped(object sender, GrabStopEventArgs e)
        {



        }


        private static void StreamGrabber_ImageGrabbed(object sender, ImageGrabbedEventArgs e)
        {



        }


        /// <summary>
        /// Convert Basler Camera Data to Mat Color
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public Bitmap GrabResultToBitmap(IGrabResult result)
        {
            Bitmap bitmap = new(result.Width, result.Height, PixelFormat.Format24bppRgb);
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
        public Mat GrabResultToMatMono(IGrabResult result)
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
        public Mat GraResultToMatColor(IGrabResult result)
        {
            Mat mat = new(result.Height, result.Width, MatType.CV_8UC3);
            pxConverter.OutputPixelFormat = PixelType.RGB8packed;
            pxConverter.Convert(mat.Ptr(0), result.Width * result.Height * 3, result);
            return mat;
        }
    }
}
