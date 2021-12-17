using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Controls;
using System;

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
        private void MainTabCanExcute(object sender, CanExecuteRoutedEventArgs e)
        {
            // MainTab is focused
            e.CanExecute = OnTabIndex == 0;
        }
        private void DeviceTabCanExcute(object sender, CanExecuteRoutedEventArgs e)
        {
            // DeviceTab is focused
            e.CanExecute = OnTabIndex == 1;
        }
        private void EngineerTabCanExcute(object sender, CanExecuteRoutedEventArgs e)
        {
            // EngineerTab is focused
            e.CanExecute = OnTabIndex == 3;
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
            //e.CanExecute = !(bool)CamConnect.IsChecked;
            e.CanExecute = !BaslerCam.IsConnected && BaslerCams.All(item => !item.IsConnected);
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
            switch (OnTabIndex)
            {
                case 3:
                    e.CanExecute = EngineerTab.CamConnect.IsEnabled;
                    break;
                default:
                    break;
            }
            //e.CanExecute = CamConnect.IsEnabled;
        }
        private void OpenDeviceCommand(object sender, ExecutedRoutedEventArgs e)
        {
            switch (OnTabIndex)
            {
                case 3: // Engineer Tab
                    if ((bool)EngineerTab.CamConnect.IsChecked)
                    {
                        EngineerTab.CamConnect.IsChecked = false;
                    }
                    else
                    {
                        EngineerTab.CamConnect.IsChecked = true;
                    }
                    break;
                default:
                    break;
            }
            // if ((bool)CamConnect.IsChecked)
            // {
            //     CamConnect.IsChecked = false;   // This will raise uncheck event
            //     //CamConnect.IsChecked = Basler_Disconnect();
            // }
            // else
            // {
            //     CamConnect.IsChecked = true;    // This will raise check event
            //     //BaslerCamInfo info = CamSelector.SelectedItem as BaslerCamInfo;
            //     //CamConnect.IsChecked = Basler_Connect(info.SerialNumber);
            // }
        }
        
        [Obsolete("不須另外啟動")]
        private void StartGrabberCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            // 相機已開啟
            // e.CanExecute = camera != null && camera.IsOpen;
            e.CanExecute = false;   // 此功能已過時
        }

        //private void StartGrabberCommand(object sender, ExecutedRoutedEventArgs e)
        //{
        //    GrabberStartBtn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        //}
        private void SingleShotCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            switch (OnTabIndex)
            {
                case 3:
                    e.CanExecute = EngineerTab.SingleShot.IsEnabled;
                    break;
                default:
                    e.CanExecute = false;
                    break;
            }
        }
        
        private void SingleShotCommand(object sender, ExecutedRoutedEventArgs e)
        {
            switch (OnTabIndex)
            {
                case 3:
                    EngineerTab.SingleShot.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    break;
                default:
                    break;
            }
            //SingleShot.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }
        
        private void ContinousShotCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            switch (OnTabIndex)
            {
                case 3:
                    e.CanExecute = EngineerTab.ContinouseShot.IsEnabled;
                    break;
                default:
                    e.CanExecute = false;
                    break;
            }
            //e.CanExecute = ContinouseShot.IsEnabled;
        }
        
        private void ContinousShotCommand(object sender, ExecutedRoutedEventArgs e)
        {
            switch (OnTabIndex)
            {
                case 3:
                    EngineerTab.ContinouseShot.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    break;
                default:
                    break;
            }
            //ContinouseShot.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }
        
        private void CrosshairOnCommnad(object sender, ExecutedRoutedEventArgs e)
        {
            switch (OnTabIndex)
            {
                case 3: // 工程師Tab
                    EngineerTab.Crosshair.Enable = !EngineerTab.Crosshair.Enable;
                    break;
                default:
                    break;
            }
        }
       
        private void AssisRectOnCommand(object sender, ExecutedRoutedEventArgs e)
        {
            switch (OnTabIndex)
            {
                case 3: // 工程師Tab
                    EngineerTab.AssistRect.Enable = !EngineerTab.AssistRect.Enable;
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// Switch to Tab 1
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SwitchTab1Command(object sender, ExecutedRoutedEventArgs e)
        {
            OnTabIndex = 0;
        }

        /// <summary>
        /// Switch to Tab 2
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SwitchTab2Command(object sender, ExecutedRoutedEventArgs e)
        {
            OnTabIndex = 1;
        }

        /// <summary>
        /// Switch to Tab 3
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SwitchTab3Command(object sender, ExecutedRoutedEventArgs e)
        {
            OnTabIndex = 2;
        }

        /// <summary>
        /// Switch Tab with parameter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SwitchTabCommand(object sender, ExecutedRoutedEventArgs e)
        {
            byte idx = byte.Parse(e.Parameter as string);
            // 確保 idx 不會超過 TabItems 數目
            OnTabIndex = idx < AppTabControl.Items.Count && (AppTabControl.Items[idx] as TabItem).IsEnabled ? idx : OnTabIndex;
        }

        private void GlobalTest(object sender, ExecutedRoutedEventArgs e)
        {
            Debug.WriteLine($"{OnTabIndex}");

            Debug.WriteLine($"{EngineerTab.CamSelector.Visibility}");
            Debug.WriteLine($"{EngineerTab.SingleShot.Visibility}");
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
