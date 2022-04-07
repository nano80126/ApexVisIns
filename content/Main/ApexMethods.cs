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
        /// <summary>
        /// 啟動角度校正
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AngleCorrectionButton_Click(object sender, RoutedEventArgs e)
        {

            StartAngleCorrection();
            //CheckRotateDirection();
        }

        /// <summary>
        /// 停止相機連續拍攝
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopCamera_Click(object sender, RoutedEventArgs e)
        {
            StopWindowEarCameraContinous();
            StopSurfaceCameraContinous();
        }

        /// <summary>
        /// 序列檢測
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SequenceInspection_Click(object sender, RoutedEventArgs e)
        {
            StartSequenceIns();
        }

        private void StartSurfaceIns_Click(object sender, RoutedEventArgs e)
        {
            StartSurfaceIns();
        }

        #region Methods
        /// <summary>
        /// 開始窗戶、耳朵相機連續拍攝
        /// </summary>
        private void StartWindowEarCameraContinous()
        {
            // 窗戶
            if (!BaslerCam1.IsContinuousGrabbing && !BaslerCam1.IsGrabberOpened)
            {
                BaslerCam1.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.Off);
                BaslerCam1.Camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByStreamGrabber);

                BaslerCam1.IsContinuousGrabbing = true;
            }

            // 耳朵
            if (!BaslerCam2.IsContinuousGrabbing && !BaslerCam2.IsGrabberOpened)
            {
                BaslerCam2.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.Off);
                BaslerCam2.Camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByStreamGrabber);

                BaslerCam2.IsContinuousGrabbing = true;
            }
        }

        private void StartSurfaceCameraContinous()
        {
            // 表面 1 
            if (!BaslerCam3.IsContinuousGrabbing && !BaslerCam3.IsGrabberOpened)
            {
                BaslerCam3.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.Off);
                BaslerCam3.Camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByStreamGrabber);

                BaslerCam3.IsContinuousGrabbing = true;
            }

            // 表面 2
            if (!BaslerCam4.IsContinuousGrabbing && !BaslerCam4.IsGrabberOpened)
            {
                BaslerCam4.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.Off);
                BaslerCam4.Camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByStreamGrabber);

                BaslerCam4.IsContinuousGrabbing = true;
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
        /// 停止窗戶、耳朵相機連續拍攝
        /// </summary>
        private void StopSurfaceCameraContinous()
        {
            if (BaslerCam3.Camera.StreamGrabber.IsGrabbing && BaslerCam3.IsContinuousGrabbing)
            {
                BaslerCam3.Camera.StreamGrabber.Stop();
                BaslerCam3.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.On);

                BaslerCam3.IsContinuousGrabbing = false;
            }

            if (BaslerCam4.Camera.StreamGrabber.IsGrabbing && BaslerCam4.IsContinuousGrabbing)
            {
                BaslerCam4.Camera.StreamGrabber.Stop();
                BaslerCam4.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.On);

                BaslerCam4.IsContinuousGrabbing = false;
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
        /// 管件角度校正，
        /// 
        /// </summary>
        private void StartAngleCorrection()
        {
            Cv2.DestroyAllWindows();

            // 變更光源
            MainWindow.PreCheckRatationWay();

            #region Flags
            MainWindow.ApexAngleCorrectionFlags.CorrectionMode = 7;     // 未定
            MainWindow.ApexAngleCorrectionFlags.OtsuThreshlod = 0;      // Otsu 閾值
            MainWindow.ApexAngleCorrectionFlags.Steps = 1;              // 當前步序
            MainWindow.ApexAngleCorrectionFlags.LastWindowWidth = 0;    // 窗戶寬度不會超過 400 // 一般約 380
            MainWindow.ApexAngleCorrectionFlags.MaxWindowWidth = 0;     // 窗戶最大值
            #endregion

            // 啟動 Grabber
            StartWindowEarGrabber();

            // 確認校正模式 & Otsu值
            MainWindow.CheckRatationWay(BaslerCam1, out byte direction);
            MainWindow.ApexAngleCorrectionFlags.CorrectionMode = direction;

            Debug.WriteLine($"Direction: {MainWindow.ApexAngleCorrectionFlags.CorrectionMode}");

            // 停止Grabber
            StopWindowEarGrabber();

            // 只有調整光源
            MainWindow.PreAngleCorrection();

            switch (MainWindow.ApexAngleCorrectionFlags.CorrectionMode)
            {
                case 0: // 快正
                    ServoMotion.Axes[1].SetAxisVelParam(50, 500, 10000, 10000);
                    ServoMotion.Axes[1].VelMove(0);
                    MainWindow.ApexAngleCorrectionFlags.Steps = 1;
                    break;
                case 1: // 快逆
                    ServoMotion.Axes[1].SetAxisVelParam(50, 500, 10000, 10000);
                    ServoMotion.Axes[1].VelMove(1);
                    MainWindow.ApexAngleCorrectionFlags.Steps = 1;
                    break;
                case 2: // 慢正
                    ServoMotion.Axes[1].SetAxisVelParam(20, 200, 4000, 4000);
                    ServoMotion.Axes[1].VelMove(0);
                    MainWindow.ApexAngleCorrectionFlags.Steps = 2;
                    break;
                case 3: // 慢逆
                    ServoMotion.Axes[1].SetAxisVelParam(20, 200, 4000, 4000);
                    ServoMotion.Axes[1].VelMove(1);
                    MainWindow.ApexAngleCorrectionFlags.Steps = 2;
                    break;
                case 4: // 低速正
                    ServoMotion.Axes[1].SetAxisVelParam(5, 50, 1000, 1000);
                    ServoMotion.Axes[1].VelMove(0);
                    MainWindow.ApexAngleCorrectionFlags.Steps = 3;
                    break;
                case 5: // 低速逆
                    ServoMotion.Axes[1].SetAxisVelParam(5, 50, 1000, 1000);
                    ServoMotion.Axes[1].VelMove(1);
                    MainWindow.ApexAngleCorrectionFlags.Steps = 3;
                    break;
                case 6: // 正接近
                    MainWindow.ApexAngleCorrectionFlags.Steps = 4;
                    break;
                case 7: // 逆接近
                    MainWindow.ApexAngleCorrectionFlags.Steps = 4;
                    break;
                default:    // 8 未定
                    break;
            }

            StartWindowEarCameraContinous();
        }

        /// <summary>
        /// 角度校正開始
        /// </summary>
        private void StartAngleCorrection_old()
        {
            Cv2.DestroyAllWindows();

            MainWindow.PreAngleCorrection();

            #region Flags
            MainWindow.ApexAngleCorrectionFlags.Steps = 1;              // 當前步序
            MainWindow.ApexAngleCorrectionFlags.LastWindowWidth = 0;    // 窗戶寬度不會超過 400 // 一般約 380
            MainWindow.ApexAngleCorrectionFlags.MaxWindowWidth = 0;     // 窗戶最大值
            MainWindow.ApexAngleCorrectionFlags.OtsuThreshlod = 0;      // Otsu 閾值
            //MainWindow.ApexAngleCorrectionFlags.Direction = 5;          // 未定方向
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

            // StopWindowEarCameraContinous();
        }


        private void StartSurfaceIns()
        {
            Cv2.DestroyAllWindows();

            MainWindow.PreSurfaceIns();

            StartSurfaceCameraContinous();
        }
        #endregion
    }
}
