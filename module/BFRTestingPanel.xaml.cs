using MaterialDesignThemes.Wpf;
using System.Diagnostics;
using System.Windows;
using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace ApexVisIns.module
{
    /// <summary>
    /// BFRTestingPanel.xaml 的互動邏輯
    /// </summary>
    public partial class BFRTestingPanel : Card
    {
        public MainWindow MainWindow { get; set; }

        //public BFR.Trail BFR { get; set; }

        public BFRTestingPanel()
        {
            InitializeComponent();
        }

        private void Card_Loaded(object sender, RoutedEventArgs e)
        {
            //BFR = FindResource(nameof(BFR)) as BFR.Trail;
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
            _ = MainWindow.TitleGrid.Focus();
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Keyboard.ClearFocus();
                _ = MainWindow.TitleGrid.Focus();
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            textBox.SelectAll();
        }

        private void Textbox_GotMouseCapture(object sender, MouseEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            textBox.SelectAll();
        }

        private void StartBFRbtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MainWindow.BFRTrail.TemperatureEnable && !MainWindow.Thermometer.IsSerialPortOpen)
                {
                    MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.BFR, "Thermometer is not connected.", MsgInformer.Message.MessageType.Info);
                    return;
                }

                MainWindow.BFRTrail.Start();
            }
            catch (InvalidOperationException ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.BFR, ex.Message, MsgInformer.Message.MessageType.Info);
            }
        }

        private void StopBFRbtn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.BFRTrail.Stop();
        }
    }
}
