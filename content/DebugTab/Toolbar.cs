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

        private void CamConnect_Checked(object sender, RoutedEventArgs e)
        {
            ToggleButton Toggle = sender as ToggleButton;

            BaslerCamInfo info = CamSelector.SelectedItem as BaslerCamInfo;
            //Toggle.IsChecked = 

        }


        private void CamConnect_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void ToggleCrosshair_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ToggleAssistRect_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SingleShot_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ContinouseShot_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RatioTextblock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }
    }
}
