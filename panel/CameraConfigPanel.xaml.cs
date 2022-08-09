using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Basler.Pylon;
using MaterialDesignThemes.Wpf;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using MCAJawIns.content;
using System.ComponentModel;
using System.Windows.Data;


namespace MCAJawIns.Panel
{
    /// <summary>
    /// CameraConfigPanel.xaml 的互動邏輯
    /// </summary>
    public partial class CameraConfigPanel : Control.CustomCard
    {
        /// <summary>
        /// 繼承 主視窗
        /// </summary>
        public MainWindow MainWindow { get; set; }
        /// <summary>
        /// 上層視窗 (待確認)
        /// </summary>
        public EngineerTab EngineerTab { get; set; }
        /// <summary>
        /// Basler Camera Obj
        /// </summary>
        public BaslerCam Cam { get; set; }

        /// <summary>
        /// Basler 組態
        /// </summary>
        public BaslerConfig BaslerConfig { get; set; }
        /// <summary>
        /// Config 路徑
        /// </summary>
        private string ConfigsDirectory { get; } = @"./configs";

        public CameraConfigPanel()
        {
            InitializeComponent();
        }

        private void CustomCard_Loaded(object sender, RoutedEventArgs e)
        {
            Cam = DataContext as BaslerCam;

            if (BaslerConfig == null)
            {
                BaslerConfig = FindResource(nameof(BaslerConfig)) as BaslerConfig;
            }
        }

        ///// <summary>
        ///// 
        ///// </summary>
        //private void SetBinding()
        //{

        //}

        private void Textbox_GotFocus(object sender, RoutedEventArgs e)
        {
            (sender as TextBox).SelectAll();
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
            _ = (Window.GetWindow(this) as MainWindow).TitleGrid.Focus();
        }

        private void ConfigPopupBox_Opened(object sender, RoutedEventArgs e)
        {

        }

        private void ConfigPopupBox_Closed(object sender, RoutedEventArgs e)
        {

        }

        private void ConfigSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ConfigDelBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ConfigSaveBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ConfigWriteBtn_Click(object sender, RoutedEventArgs e)
        {

        }


    }
}
