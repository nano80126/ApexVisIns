using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MCAJawIns
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
            switch ((Owner as MainWindow).JawType)
            {
                case JawTypes.S:
                    TypeSRadio.IsChecked = true;
                    break;
                case JawTypes.M:
                    TypeMRadio.IsChecked = true;
                    break;
                case JawTypes.L:
                    TypeLRadio.IsChecked = true;
                    break;
                default:
                    break;
            }
        }

        #region Command
        private void CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void ChangeTypeCommand(object sender, ExecutedRoutedEventArgs e)
        {
            // JawTypes type = (JawTypes)e.Parameter;
            (Owner as MainWindow).JawType = (JawTypes)e.Parameter;
        }
        
        private void ChangeModeCommand(object sender, ExecutedRoutedEventArgs e)
        {
            // (Owner as MainWindow).InitMode = (MainWindow.InitModes)Enum.Parse(typeof(MainWindow.InitModes), e.Parameter.ToString());
            (Owner as MainWindow).InitMode = (InitModes)e.Parameter;
            // (e.Source as RadioButton).IsChecked = true;
            DialogResult = true;
        }
        #endregion
    }
}
