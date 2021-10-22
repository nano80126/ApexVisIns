using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace ApexVisIns.content
{
    public partial class DebugTab : StackPanel
    {

        private void CanExcute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void OpenDeviceCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = CamConnect.IsEnabled;
        }

        private void OpenDeviceCommand(object sender, ExecutedRoutedEventArgs e)
        {
            if ((bool)CamConnect.IsChecked)
            {
                CamConnect.IsChecked = false;
            }
            else
            {
                CamConnect.IsChecked = true;
            }
        }

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
    }
}
