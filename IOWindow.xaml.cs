using Automation.BDaq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
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
    public partial class IOWindow : Window, INotifyPropertyChanged
    {
        public MainWindow MainWindow { get; set; }


        public bool LoginFlag => MainWindow != null && MainWindow.LoginFlag;

        public IOWindow()
        {
            InitializeComponent();
            MainWindow = Owner as MainWindow;
        }

        public IOWindow(MainWindow mw)
        {
            InitializeComponent();
            MainWindow = mw;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //MainWindow = Owner as MainWindow;
            Debug.WriteLine($"IO Loaded");
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {

        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

        }

        private void TitleGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Minbtn_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            // 重置按鈕

            // 1. 重置錯誤
            // 2. 重新 Servo On
            // 3. 
            try
            {
                MainWindow.ServoMotion.ResetAllError();

                // 重新 Servo On
                MainWindow.ServoMotion.SetAllServoOn();
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }

        private void EmgButton_Click(object sender, RoutedEventArgs e)
        {
            // 即停按鈕 (軟體)
            // 

            try
            {

            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.APP, $"軟體急停發生錯誤 :{ex.Message}");
            }
        }

        private void RefreshIO()
        {
            //for (int i = 0; i < MainWindow.IOController.DiArrayColl.Count; i++)
            //{
            //    //ErrorCode err = MainWindow.IOController.ReadDI(i);
            //    //Debug.WriteLine($"ErrorCode: {err}");
            //}
        }

        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void PropertyChange(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
