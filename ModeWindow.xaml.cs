using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ApexVisIns
{
    /// <summary>
    /// ModeWindow.xaml 的互動邏輯
    /// </summary>
    public partial class ModeWindow : Window
    {
        public ModeWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AutoModeRadio.IsChecked = true;
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            MainWindow.InitModes mode = (MainWindow.InitModes)Enum.Parse(typeof(MainWindow.InitModes), (sender as RadioButton).CommandParameter.ToString());
            (Owner as MainWindow).InitMode = mode;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F1:
                    AutoModeRadio.SetValue(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, true);
                    break;
                case Key.F2:
                    WarmModeRadio.SetValue(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, true);
                    //EditModeRadio.SetValue(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, true);
                    break;
                case Key.F3:
                    EditModeRadio.SetValue(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, true);
                    break;
                default:
                    break;
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F1:
                    (Owner as MainWindow).InitMode = MainWindow.InitModes.AUTO;
                    DialogResult = true;
                    Close();
                    break;
                case Key.F2:
                    (Owner as MainWindow).InitMode = MainWindow.InitModes.WARM;
                    DialogResult = true;
                    Close();
                    break;
                case Key.F3:
                    (Owner as MainWindow).InitMode = MainWindow.InitModes.EDIT;
                    DialogResult = true;
                    break;
                default:
                    break;
            }
        }
    }
}
