using Basler.Pylon;
using MaterialDesignThemes.Wpf;
using System.Diagnostics;
using System.Windows;

namespace MCAJawIns.module
{
    /// <summary>
    /// OffsetPanel.xaml 的互動邏輯
    /// </summary>
    public partial class OffsetPanel : Card
    {
        /// <summary>
        /// MainWindow 物件
        /// </summary>
        public MainWindow MainWindow { get; set; }

        public OffsetPanel()
        {
            InitializeComponent();
        }

        private void CamLeftMove_Click(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine($"{MainWindow.BaslerCam}");
            //Debug.WriteLine($"Camera: {MainWindow.BaslerCam.Camera == null}");
            // 確認相機已開啟
            if (MainWindow.BaslerCam?.Camera != null)
            {
                BaslerCam cam = MainWindow.BaslerCam;

                int offset_x = cam.OffsetX - 20 < 0 ? 0 : cam.OffsetX - 20;
                if (cam.Camera.Parameters[PLGigECamera.OffsetX].TrySetValue(offset_x))
                {
                    cam.OffsetX = (int)cam.Camera.Parameters[PLGigECamera.OffsetX].GetValue();
                }
            }
        }

        private void CamTopMove_Click(object sender, RoutedEventArgs e)
        {
            // 確認相機已開啟
            if (MainWindow.BaslerCam?.Camera != null)
            {
                BaslerCam cam = MainWindow.BaslerCam;

                int offset_y = cam.OffsetY - 20 < 0 ? 0 : cam.OffsetY - 20;
                if (cam.Camera.Parameters[PLGigECamera.OffsetY].TrySetValue(offset_y))
                {
                    cam.OffsetY = (int)cam.Camera.Parameters[PLGigECamera.OffsetY].GetValue();
                }
            }
        }

        private void CamRightMove_Click(object sender, RoutedEventArgs e)
        {
            // 確認相機已開啟
            if (MainWindow.BaslerCam?.Camera != null)
            {
                BaslerCam cam = MainWindow.BaslerCam;

                int offset_x = cam.OffsetX + 20 > cam.OffsetXMax ? cam.OffsetXMax : cam.OffsetX + 20;
                if (cam.Camera.Parameters[PLGigECamera.OffsetX].TrySetValue(offset_x))
                {
                    cam.OffsetX = (int)cam.Camera.Parameters[PLGigECamera.OffsetX].GetValue();
                }
            }
        }

        private void CamBottomMove_Click(object sender, RoutedEventArgs e)
        {
            //if (MainWindow.BaslerCam?.Camera != null)
            // 確認相機已開啟
            if (MainWindow.BaslerCam?.Camera != null)
            {
                BaslerCam cam = MainWindow.BaslerCam;

                int offset_y = cam.OffsetY + 20 > cam.OffsetYMax ? cam.OffsetYMax : cam.OffsetY + 20;
                if (cam.Camera.Parameters[PLGigECamera.OffsetY].TrySetValue(offset_y))
                {
                    cam.OffsetY = (int)cam.Camera.Parameters[PLGigECamera.OffsetY].GetValue();
                }
            }
        }

        private void CamCenterMove_Click(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine($"{MainWindow.BaslerCam} {MainWindow.BaslerCam.IsConnected}");
            //foreach (BaslerCam cam in MainWindow.BaslerCams)
            //{
            //    Debug.WriteLine($"{cam} {cam.IsConnected}");
            //}

            //if (MainWindow.BaslerCam?.Camera != null)
            // 確認相機已開啟
            if (MainWindow.BaslerCam?.Camera != null)
            {
                BaslerCam cam = MainWindow.BaslerCam;

                int offset_x = (cam.WidthMax - cam.Width) / 2 % 2 == 0 ? (cam.WidthMax - cam.Width) / 2 : (cam.WidthMax - cam.Width) / 2 - 1;
                int offset_y = (cam.HeightMax - cam.Height) / 2 % 2 == 0 ? (cam.HeightMax - cam.Height) / 2 : (cam.HeightMax - cam.Height) / 2 - 1;

                if (cam.Camera.Parameters[PLGigECamera.OffsetX].TrySetValue(offset_x))
                {
                    cam.OffsetX = (int)cam.Camera.Parameters[PLGigECamera.OffsetX].GetValue();
                }

                if (cam.Camera.Parameters[PLGigECamera.OffsetY].TrySetValue(offset_y))
                {
                    cam.OffsetY = (int)cam.Camera.Parameters[PLGigECamera.OffsetY].GetValue();
                }
                //Debug.WriteLine($"{cam.OffsetX} {cam.OffsetY}");
                //Debug.WriteLine($"{cam.Camera.Parameters[PLGigECamera.OffsetX].GetValue()}");
                //Debug.WriteLine($"{cam.Camera.Parameters[PLGigECamera.OffsetY].GetValue()}");
            }
        }
    }
}
