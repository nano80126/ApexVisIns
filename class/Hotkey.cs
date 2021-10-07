using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace ApexVisIns
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Always Can Excute
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CanExcute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void MinCommand(object sender, ExecutedRoutedEventArgs e)
        {
            Minbtn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }
        private void MaxCommand(object sender, ExecutedRoutedEventArgs e)
        {
            Maxbtn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }
        private void QuitCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !(bool)CamConnect.IsChecked;
            //#if BASLER
            //            // 相機不為開啟狀態
            //            e.CanExecute = !BaslerCam.IsConnected;
            //#elif UVC
            //            e.CanExecute = !UvcCam.IsOpen;
            //#endif
        }
        private void QuitCommand(object sender, ExecutedRoutedEventArgs e)
        {
            Quitbtn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }
        private void OpenDeviceCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            // Grabber 不為啟動狀態 // CamSelector 有選擇相機
            e.CanExecute = CamConnect.IsEnabled;
        }
        private void OpenDeviceCommand(object sender, ExecutedRoutedEventArgs e)
        {
            if ((bool)CamConnect.IsChecked)
            {
                CamConnect.IsChecked = false;   // This will raise uncheck event
                //CamConnect.IsChecked = Basler_Disconnect();
            }
            else
            {
                CamConnect.IsChecked = true;    // This will raise check event
                //BaslerCamInfo info = CamSelector.SelectedItem as BaslerCamInfo;
                //CamConnect.IsChecked = Basler_Connect(info.SerialNumber);
            }
        }

        private void StartGrabberCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            // 相機已開啟
            //e.CanExecute = camera != null && camera.IsOpen;
            e.CanExecute = false;   // 此功能已過時
        }

        //private void StartGrabberCommand(object sender, ExecutedRoutedEventArgs e)
        //{
        //    GrabberStartBtn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        //}

        private void SingleShotCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = SingleShot.IsEnabled;
        }

        private void SingleShotCommand(object sender, ExecutedRoutedEventArgs e)
        {
            SingleShot.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }

        private void ContinousShotCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ContinouseShot.IsEnabled;
        }

        private void ContinousShotCommand(object sender, ExecutedRoutedEventArgs e)
        {
            ContinouseShot.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }

        private void CrosshairOnCommnad(object sender, ExecutedRoutedEventArgs e)
        {
            Crosshair.Enable = !Crosshair.Enable;
        }

        private void AssisRectOnCommand(object sender, ExecutedRoutedEventArgs e)
        {
            AssistRect.Enable = !AssistRect.Enable;
        }

        private void SwitchTab1Command(object sender, ExecutedRoutedEventArgs e)
        {
            OnTabIndex = 0;
        }

        private void SwitchTab2Command(object sender, ExecutedRoutedEventArgs e)
        {
            OnTabIndex = 1;
        }

#if false
        private void PopupboxCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ConfigPopupBox.IsPopupOpen;  // Popupbox 是否開啟
        }

        private void ConfigSave(object sender, ExecutedRoutedEventArgs e)
        {
            ConfigWriteBtn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            ConfigPopupBox.IsPopupOpen = false;     // 需要下命令關閉 Popupbox
        }

        private void ConfigCancel(object sender, ExecutedRoutedEventArgs e)
        {
            ConfigCancelBtn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            ConfigPopupBox.IsPopupOpen = false;     // 需要下命令關閉 Popupbox
        }


        private void ApexAnalyzeCommand(object sender, ExecutedRoutedEventArgs e)
        {
            ApexAnalyzeBtn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        } 
#endif
    }
}
