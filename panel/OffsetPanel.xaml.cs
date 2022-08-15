﻿using System.Windows;
using Basler.Pylon;

namespace MCAJawIns.Panel
{
    /// <summary>
    /// OffsetPanel.xaml 的互動邏輯
    /// </summary>
    public partial class OffsetPanel : Control.CustomCard
    {
        /// <summary>
        /// Basler Camera Object
        /// </summary>
        public BaslerCam Cam { get; set; }

        public OffsetPanel()
        {
            InitializeComponent();
        }

        private void CustomCard_Loaded(object sender, RoutedEventArgs e)
        {
            Cam = DataContext as BaslerCam;
        }

        private void CamLeftMove_Click(object sender, RoutedEventArgs e)
        {
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
            }
        }
    }
}
