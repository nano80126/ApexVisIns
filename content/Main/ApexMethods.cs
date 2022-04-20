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
    /*
    與 MainTab.xaml 相同 Class
     */
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

        /// <summary>
        /// 開始表面檢測
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartSurfaceIns_Click(object sender, RoutedEventArgs e)
        {
            StartSurfaceIns();
        }

        #region Methods，方法
        /// <summary>
        /// 開始窗戶、耳朵相機連續拍攝
        /// </summary>
        private void StartWindowEarCameraContinous()
        {
            MainWindow.StartWindowEarCameraContinous();
#if false
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
#endif
        }

        /// <summary>
        /// 停止窗戶、耳朵相機連續拍攝
        /// </summary>
        private void StopWindowEarCameraContinous()
        {
            MainWindow.StopWindowEarCameraContinous();
#if false
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
#endif
        }

        /// <summary>
        /// 開始管件表面連續拍攝
        /// </summary>
        private void StartSurfaceCameraContinous()
        {
            MainWindow.StartSurfaceCameraContinous();
#if false
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
#endif
        }

        /// <summary>
        /// 停止管件表面連續拍攝
        /// </summary>
        private void StopSurfaceCameraContinous()
        {
            MainWindow.StopSurfaceCameraContinous();
#if false
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
#endif
        }

        /// <summary>
        /// 開始窗戶、耳朵相機 Grabber
        /// </summary>
        private void StartWindowEarGrabber()
        {
            MainWindow.StartWindowEarGrabber();
#if false
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
#endif
        }

        /// <summary>
        /// 停止窗戶、耳朵相機 Grabber
        /// </summary>
        private void StopWindowEarGrabber()
        {
            MainWindow.StopWindowEarGrabber();
#if false
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
#endif
        }

        /// <summary>
        /// 開始管件角度校正
        /// </summary>
        private void StartAngleCorrection()
        {
            Cv2.DestroyAllWindows();

            #region 重置 Flags
            MainWindow.ApexAngleCorrectionFlags.CorrectionMode = 8;     // 未定
            MainWindow.ApexAngleCorrectionFlags.OtsuThreshlod = 0;      // Otsu 閾值
            MainWindow.ApexAngleCorrectionFlags.CheckModeStep = 0;      // 確認校正模式
            MainWindow.ApexAngleCorrectionFlags.Steps = 0;              // 當前步序
            MainWindow.ApexAngleCorrectionFlags.LastWindowWidth = 0;    // 窗戶寬度不會超過 400 // 一般約 380
            MainWindow.ApexAngleCorrectionFlags.MaxWindowWidth = 0;     // 窗戶最大值
            #endregion

            //// 變更光源，啟動馬達
            //MainWindow.PreCheckCorrectionMode();

            //Task.Run(() =>
            //{

            // 啟動 Grabber
            StartWindowEarGrabber();
            // 
            MainWindow.ApexAngleCorrectionSequence(BaslerCam1);
            //});

            // 停止Grabber
            // StopWindowEarGrabber();

            return;
#if false
            // 確認校正模式 & Otsu值
            MainWindow.CheckCorrectionMode(BaslerCam1, out byte mode);

            // 
            MainWindow.ApexAngleCorrectionFlags.CorrectionMode = mode;

            // 
            Debug.WriteLine($"Direction: {MainWindow.ApexAngleCorrectionFlags.CorrectionMode}");

            // 停止Grabber
            StopWindowEarGrabber();

            // 只有調整光源
            MainWindow.PreAngleCorrection();

            switch (MainWindow.ApexAngleCorrectionFlags.CorrectionMode)
            {
                case 0:     // 快正轉
                    ServoMotion.Axes[1].SetAxisVelParam(50, 500, 10000, 10000);
                    ServoMotion.Axes[1].VelMove(0);
                    MainWindow.ApexAngleCorrectionFlags.Steps = 1;
                    break;
                case 1:     // 快逆轉
                    ServoMotion.Axes[1].SetAxisVelParam(50, 500, 10000, 10000);
                    ServoMotion.Axes[1].VelMove(1);
                    MainWindow.ApexAngleCorrectionFlags.Steps = 1;
                    break;
                case 2:     // 慢正轉
                    ServoMotion.Axes[1].SetAxisVelParam(20, 200, 4000, 4000);
                    ServoMotion.Axes[1].VelMove(0);
                    MainWindow.ApexAngleCorrectionFlags.Steps = 2;
                    break;
                case 3:     // 慢逆轉
                    ServoMotion.Axes[1].SetAxisVelParam(20, 200, 4000, 4000);
                    ServoMotion.Axes[1].VelMove(1);
                    MainWindow.ApexAngleCorrectionFlags.Steps = 2;
                    break;
                case 4:     // 低速正轉
                    ServoMotion.Axes[1].SetAxisVelParam(5, 50, 1000, 1000);
                    ServoMotion.Axes[1].VelMove(0);
                    MainWindow.ApexAngleCorrectionFlags.Steps = 3;
                    break;
                case 5:     // 低速逆轉
                    ServoMotion.Axes[1].SetAxisVelParam(5, 50, 1000, 1000);
                    ServoMotion.Axes[1].VelMove(1);
                    MainWindow.ApexAngleCorrectionFlags.Steps = 3;
                    break;
                case 6:     // 正接近
                    MainWindow.ApexAngleCorrectionFlags.Steps = 4;
                    break;
                case 7:     // 逆接近
                    MainWindow.ApexAngleCorrectionFlags.Steps = 4;
                    break;
                default:    // 8 未定
                    break;
            }

            Debug.WriteLine($"{MainWindow.ApexAngleCorrectionFlags.Steps}");
            Debug.WriteLine($"{MainWindow.ApexAngleCorrectionFlags.LastWindowWidth}");
            Debug.WriteLine($"{MainWindow.ApexAngleCorrectionFlags.MaxWindowWidth}");

            // return;

            // 開始相機連續拍攝
            StartWindowEarCameraContinous(); 
#endif
        }

        /// <summary>
        /// 開始角度校正
        /// </summary>
        [Obsolete("Old methods")]
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
        /// 結束管件角度校正
        /// </summary>
        private void EndAngleCorrection()
        {
            StopWindowEarCameraContinous();
        }

        /// <summary>
        /// 開始序列檢測，檢驗窗戶、耳朵毛邊
        /// </summary>
        private void StartSequenceIns()
        {
            Cv2.DestroyAllWindows();

            // 重置 Flag
            MainWindow.ApexDefectInspectionStepsFlags.Steps = 0;

            StartWindowEarGrabber();

            MainWindow.ApexWindowEarInspectionSequence(BaslerCam1, BaslerCam2);

            // StopWindowEarCameraContinous();
        }

        /// <summary>
        /// 開始管件表面檢驗
        /// </summary>
        private void StartSurfaceIns()
        {
            Cv2.DestroyAllWindows();

            // 重置 Flag
            MainWindow.ApexDefectInspectionStepsFlags.SurfaceSteps = 0;
            // 停止驗窗戶
            MainWindow.ApexDefectInspectionStepsFlags.SurfaceInsOn = 0;
            // 保留一下
            //MainWindow.ApexDefectInspectionStepsFlags.WindowInsOn = 0;

            //StartSurfaceCameraContinous();
            StartWindowEarGrabber();    // 啟動窗戶、耳朵相機 Grabber
            // 
            MainWindow.ApexSurfaceInspectionSequence(BaslerCam1, BaslerCam2, BaslerCam3, BaslerCam4);
        }
        #endregion
    }
}
