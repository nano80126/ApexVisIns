using System.Windows;
using System.Windows.Input;
using Basler.Pylon;

namespace MCAJawIns.Panel
{
    /// <summary>
    /// OffsetPanel.xaml 的互動邏輯
    /// </summary>
    public partial class OffsetPanel : Control.CustomCard
    {
        /// <summary>
        /// MainWindow
        /// </summary>
        public MainWindow MainWindow { get; } = (MainWindow)Application.Current.MainWindow;

        /// <summary>
        /// Basler Camera Object
        /// </summary>
        public BaslerCam Cam { get; set; }

        public OffsetPanel()
        {
            InitializeComponent();

            // Set value when init
            // MainWindow = (MainWindow)Application.Current.MainWindow;
        }

        private void CustomCard_Loaded(object sender, RoutedEventArgs e)
        {
            Cam = DataContext as BaslerCam;
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
            _ = (Window.GetWindow(this) as MainWindow).TitleGrid.Focus();
        }

        private void CamLeftMove_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.BaslerCam?.Camera != null)
            {
                //BaslerCam cam = MainWindow.BaslerCam;

                int offset_x = Cam.OffsetX - 20 < 0 ? 0 : Cam.OffsetX - 20;
                if (Cam.Camera.Parameters[PLGigECamera.OffsetX].TrySetValue(offset_x))
                {
                    Cam.OffsetX = (int)Cam.Camera.Parameters[PLGigECamera.OffsetX].GetValue();
                }
            }
        }

        private void CamTopMove_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.BaslerCam?.Camera != null)
            {
                //BaslerCam cam = MainWindow.BaslerCam;

                int offset_y = Cam.OffsetY - 20 < 0 ? 0 : Cam.OffsetY - 20;
                if (Cam.Camera.Parameters[PLGigECamera.OffsetY].TrySetValue(offset_y))
                {
                    Cam.OffsetY = (int)Cam.Camera.Parameters[PLGigECamera.OffsetY].GetValue();
                }
            }
        }

        private void CamRightMove_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.BaslerCam?.Camera != null)
            {
                //BaslerCam cam = MainWindow.BaslerCam;

                int offset_x = Cam.OffsetX + 20 > Cam.OffsetXMax ? Cam.OffsetXMax : Cam.OffsetX + 20;
                if (Cam.Camera.Parameters[PLGigECamera.OffsetX].TrySetValue(offset_x))
                {
                    Cam.OffsetX = (int)Cam.Camera.Parameters[PLGigECamera.OffsetX].GetValue();
                }
            }
        }

        private void CamBottomMove_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.BaslerCam?.Camera != null)
            {
                //BaslerCam cam = MainWindow.BaslerCam;

                int offset_y = Cam.OffsetY + 20 > Cam.OffsetYMax ? Cam.OffsetYMax : Cam.OffsetY + 20;
                if (Cam.Camera.Parameters[PLGigECamera.OffsetY].TrySetValue(offset_y))
                {
                    Cam.OffsetY = (int)Cam.Camera.Parameters[PLGigECamera.OffsetY].GetValue();
                }
            }
        }

        private void CamCenterMove_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.BaslerCam?.Camera != null)
            {
                //BaslerCam cam = MainWindow.BaslerCam;

                int offset_x = (Cam.WidthMax - Cam.Width) / 2 % 2 == 0 ? (Cam.WidthMax - Cam.Width) / 2 : (Cam.WidthMax - Cam.Width) / 2 - 1;
                int offset_y = (Cam.HeightMax - Cam.Height) / 2 % 2 == 0 ? (Cam.HeightMax - Cam.Height) / 2 : (Cam.HeightMax - Cam.Height) / 2 - 1;

                if (Cam.Camera.Parameters[PLGigECamera.OffsetX].TrySetValue(offset_x))
                {
                    Cam.OffsetX = (int)Cam.Camera.Parameters[PLGigECamera.OffsetX].GetValue();
                }

                if (Cam.Camera.Parameters[PLGigECamera.OffsetY].TrySetValue(offset_y))
                {
                    Cam.OffsetY = (int)Cam.Camera.Parameters[PLGigECamera.OffsetY].GetValue();
                }
            }
        }
    }
}
