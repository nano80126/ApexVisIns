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
            Cv2.DestroyAllWindows();

            MainWindow.PreAngleCorrection();

            #region Flags
            MainWindow.ApexAngleCorrectionFlags.Steps = 0;
            MainWindow.ApexAngleCorrectionFlags.LastWindowWidth = 400;
            MainWindow.ApexAngleCorrectionFlags.MaxWindowWidth = 0;
            MainWindow.ApexAngleCorrectionFlags.Direction = 1;
            #endregion

            _ = SpinWait.SpinUntil(() => false, 500);

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

        private void StopCamera_Click(object sender, RoutedEventArgs e)
        {
            if (BaslerCam1.Camera.StreamGrabber.IsGrabbing)
            {
                BaslerCam1.Camera.StreamGrabber.Stop();
                BaslerCam1.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.On);

                BaslerCam1.IsContinuousGrabbing = false;
            }

            if (BaslerCam2.Camera.StreamGrabber.IsGrabbing)
            {
                BaslerCam2.Camera.StreamGrabber.Stop();
                BaslerCam2.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.On);

                BaslerCam2.IsContinuousGrabbing = false;
            }


            Debug.WriteLine(Marshal.SizeOf(MainWindow.ApexAngleCorrectionFlags));
        }


        private void StopWindowEarCamera()
        {
            if (BaslerCam1.Camera.StreamGrabber.IsGrabbing)
            {
                BaslerCam1.Camera.StreamGrabber.Stop();
                BaslerCam1.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.On);

                BaslerCam1.IsContinuousGrabbing = false;
            }

            if (BaslerCam2.Camera.StreamGrabber.IsGrabbing)
            {
                BaslerCam2.Camera.StreamGrabber.Stop();
                BaslerCam2.Camera.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.On);

                BaslerCam2.IsContinuousGrabbing = false;
            }
        }

    }
}
