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

namespace LockPlate
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
            //AutoModeRadio.IsChecked = true;
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        //private void RadioButton_Checked(object sender, RoutedEventArgs e)
        //{
        //    MainWindow.InitModes mode = (MainWindow.InitModes)Enum.Parse(typeof(MainWindow.InitModes), (sender as RadioButton).CommandParameter.ToString());
        //    (Owner as MainWindow).InitMode = mode;
        //}

        //private void Button_Click(object sender, RoutedEventArgs e)
        //{
        //    DialogResult = true;
        //    Close();
        //}

        #region Command
        private void CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void ChangeModeCommand(object sender, ExecutedRoutedEventArgs e)
        {
            (Owner as MainWindow).InitMode = (MainWindow.InitModes)Enum.Parse(typeof(MainWindow.InitModes), e.Parameter.ToString());
            (e.Source as RadioButton).IsChecked = true;
            DialogResult = true;
        }
        #endregion
    }
}
