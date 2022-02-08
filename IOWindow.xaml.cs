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
    /// IO_Window.xaml 的互動邏輯
    /// </summary>
    public partial class IOWindow : Window
    {
        private MainWindow _mainWindow;


        public IOWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _mainWindow = Owner as MainWindow;



        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void TitleGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            // 重置按鈕
            
            // 1. 重置錯誤
            // 2. 重新 Servo On
            // 3. 
            
            MainWindow.ServoMotion.ResetAllError();


        }

        private void EmgButton_Click(object sender, RoutedEventArgs e)
        {
            // 即停按鈕 (軟體)


        }
    }
}
