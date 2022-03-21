using Basler.Pylon;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ApexVisIns.Product;
using System.Runtime.InteropServices;

namespace ApexVisIns.content
{
    public partial class MainTab : StackPanel
    {
        private void AngleCorrectionButton_Click(object sender, RoutedEventArgs e)
        {
            StartAngleCorrection();
            #region MyRegion
            //Cv2.DestroyAllWindows();

            //MainWindow.PreAngleCorrection();

            //#region Flags
            //MainWindow.ApexAngleCorrectionFlags.Steps = 0;
            //MainWindow.ApexAngleCorrectionFlags.LastWindowWidth = 400;
            //MainWindow.ApexAngleCorrectionFlags.MaxWindowWidth = 0;
            //MainWindow.ApexAngleCorrectionFlags.Direction = 1;
            //#endregion

            //_ = SpinWait.SpinUntil(() => false, 500);

            //// 窗戶
            //if (!BaslerCam1.IsContinuousGrabbing)
            //{
            //    BaslerCam1.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.Off);
            //    BaslerCam1.Camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByStreamGrabber);

            //    BaslerCam1.IsContinuousGrabbing = true;
            //}

            //// 耳朵
            //if (!BaslerCam2.IsContinuousGrabbing)
            //{
            //    BaslerCam2.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.Off);
            //    BaslerCam2.Camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByStreamGrabber);

            //    BaslerCam2.IsContinuousGrabbing = true;
            //} 
            #endregion
        }

        private void StopCamera_Click(object sender, RoutedEventArgs e)
        {
            StopWindowEarCameraContinous();
        }

        private void SequenceInspection_Click(object sender, RoutedEventArgs e)
        {
            StartSequenceIns();
        }

        #region Methods
        /// <summary>
        /// 開始窗戶、耳朵相機連續拍攝
        /// </summary>
        private void StartWindowEarCameraContinous()
        {
            // 窗戶
            if (!BaslerCam1.IsContinuousGrabbing)
            {
                BaslerCam1.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.Off);
                BaslerCam1.Camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByStreamGrabber);

                BaslerCam1.IsContinuousGrabbing = true;
            }

            // 耳朵
            if (!BaslerCam2.IsContinuousGrabbing)
            {
                BaslerCam2.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.Off);
                BaslerCam2.Camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByStreamGrabber);

                BaslerCam2.IsContinuousGrabbing = true;
            }
        }

        /// <summary>
        /// 停止窗戶、耳朵相機連續拍攝
        /// </summary>
        private void StopWindowEarCameraContinous()
        {
            if (BaslerCam1.Camera.StreamGrabber.IsGrabbing && BaslerCam1.IsContinuousGrabbing)
            {
                BaslerCam1.Camera.StreamGrabber.Stop();
                BaslerCam1.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.On);

                BaslerCam1.IsContinuousGrabbing = false;
            }

            if (BaslerCam2.Camera.StreamGrabber.IsGrabbing && BaslerCam2.IsContinuousGrabbing)
            {
                BaslerCam2.Camera.StreamGrabber.Stop();
                BaslerCam2.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.On);

                BaslerCam2.IsContinuousGrabbing = false;
            }
        }

        /// <summary>
        /// 開始窗戶、耳朵相機 Grabber
        /// </summary>
        private void StartWindowEarGrabber()
        {
            if (!BaslerCam1.IsGrabberOpened && !BaslerCam1.IsGrabbing)
            {
                // 啟動 StreamGrabber 連續拍攝
                BaslerCam1.Camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByUser);

                BaslerCam1.Camera.WaitForFrameTriggerReady(500, TimeoutHandling.ThrowException);
                BaslerCam1.IsGrabberOpened = true;
                BaslerCam1.IsContinuousGrabbing = false;

                // 
                BaslerCam1.Camera.StreamGrabber.ImageGrabbed -= StreamGrabber_ImageGrabbed;

            }

            if (!BaslerCam2.IsGrabberOpened && !BaslerCam2.IsGrabbing)
            {
                // 啟動 StreamGrabber 連續拍攝
                BaslerCam2.Camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByUser);

                BaslerCam2.Camera.WaitForFrameTriggerReady(500, TimeoutHandling.ThrowException);
                BaslerCam2.IsGrabberOpened = true;
                BaslerCam2.IsContinuousGrabbing = false;

                // 
                BaslerCam2.Camera.StreamGrabber.ImageGrabbed -= StreamGrabber_ImageGrabbed;
            }
        }

        /// <summary>
        /// 停止窗戶、耳朵相機 Grabber
        /// </summary>
        private void StopWindowEarGrabber()
        {
            if (BaslerCam1.IsGrabbing)
            {
                // 啟動 StreamGrabber 連續拍攝
                BaslerCam1.Camera.StreamGrabber.Stop();
                BaslerCam1.IsGrabberOpened = false;

                // 
                BaslerCam1.Camera.StreamGrabber.ImageGrabbed += StreamGrabber_ImageGrabbed;

            }

            if (BaslerCam2.IsGrabbing)
            {
                // 啟動 StreamGrabber 連續拍攝
                BaslerCam2.Camera.StreamGrabber.Stop();
                BaslerCam2.IsGrabberOpened = false;

                // 
                BaslerCam2.Camera.StreamGrabber.ImageGrabbed += StreamGrabber_ImageGrabbed;
            }
        }

        /// <summary>
        /// 開始角度校正
        /// </summary>
        private void StartAngleCorrection()
        {
            Cv2.DestroyAllWindows();

            MainWindow.PreAngleCorrection();

            #region Flags
            MainWindow.ApexAngleCorrectionFlags.Steps = 0;
            MainWindow.ApexAngleCorrectionFlags.LastWindowWidth = 400;  // 窗戶寬度不會超過 400 // 一般約 380
            MainWindow.ApexAngleCorrectionFlags.MaxWindowWidth = 0;
            MainWindow.ApexAngleCorrectionFlags.Direction = 1;
            #endregion

            _ = SpinWait.SpinUntil(() => false, 500);

            StartWindowEarCameraContinous();
        }

        /// <summary>
        /// 角度校正結束
        /// </summary>
        private void EndAngleCorrection()
        {
            StopWindowEarCameraContinous();
            // Cv2.DestroyAllWindows();
        }


        /// <summary>
        /// 開始序列檢測
        /// </summary>
        private void StartSequenceIns()
        {
            Cv2.DestroyAllWindows();

            MainWindow.ApexDefectInspectionStepsFlags.Steps = 0;

            StartWindowEarGrabber();

            MainWindow.ApexWindowEarInspectionSequence(BaslerCam1, BaslerCam2);
            //StopWindowEarCameraContinous();
        }
        #endregion
    }
}
