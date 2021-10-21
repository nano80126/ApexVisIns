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
using ApexVisIns.content;

namespace ApexVisIns.module
{
    /// <summary>
    /// ConfigPanel.xaml 的互動邏輯
    /// </summary>
    public partial class ConfigPanel : Card
    {
        /// <summary>
        /// 繼承 主視窗
        /// </summary>
        public MainWindow MainWindow { get; set; }
        /// <summary>
        /// 上層視窗 (待確認)
        /// </summary>
        public DebugTab DebugTab { get; set; }
        /// <summary>
        /// Config 路徑
        /// </summary>
        private string ConfigsDirectory { get; } = @"./configs";

        public ConfigPanel()
        {
            InitializeComponent();
        }

        private void Textbox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            textBox.SelectAll();
        }

        private void Textbox_GotMouseCapture(object sender, MouseEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            textBox.SelectAll();
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
            _ = (Window.GetWindow(this) as MainWindow).TitleGrid.Focus();
        }

        private void Initialize_JsonFile()
        {
            if (Directory.Exists(ConfigsDirectory))
            {
                string[] files = Directory.GetFiles(ConfigsDirectory, "*.json", SearchOption.TopDirectoryOnly);
                files = Array.ConvertAll(files, file => file = System.IO.Path.GetFileNameWithoutExtension(file));

                foreach (string file in files)
                {
                    //if (!BaslerCam.ConfigList.Contains(file))
                    //{
                    //    BaslerCam.ConfigList.Add(file);
                    //}
                    if (!MainWindow.BaslerCam.ConfigList.Contains(file))
                    {
                        MainWindow.BaslerCam.ConfigList.Add(file);
                    }
                }
            }
            else
            {
                _ = Directory.CreateDirectory(ConfigsDirectory);
            }
        }

        private void ConfigPopupBox_Opened(object sender, RoutedEventArgs e)
        {
            Initialize_JsonFile();
        }

        private void ConfigPopupBox_Closed(object sender, RoutedEventArgs e)
        {
            /// /// 
            if (MainWindow.BaslerCam?.Camera != null)
            {
                SyncConfiguration(MainWindow.BaslerCam.Config, MainWindow.BaslerCam);
            }
            ConfigSelector.SelectedIndex = -1;
        }

        public void SyncConfiguration(BaslerConfig config, BaslerCam camera)
        {
            config.Name = camera.ConfigName;
            ConfigName.Text = camera.ConfigName;
            config.Width = camera.Width;
            ConfigWidth.Text = $"{camera.Width}";
            config.Height = camera.Height;
            ConfigHeight.Text = $"{camera.Height}";
            config.FPS = camera.FPS;
            ConfigFPS.Text = $"{camera.FPS:F1}";
            config.ExposureTime = camera.ExposureTime;
            ConfigExposureTime.Text = $"{config.ExposureTime}";
        }

        private void ConfigSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string file = (sender as ComboBox).SelectedItem as string;

            if (!string.IsNullOrWhiteSpace(file))
            {
                string path = $@"./configs/{file}.json";

                if (File.Exists(path))
                {
                    using StreamReader reader = File.OpenText(path);
                    string json = reader.ReadToEnd();

                    BaslerConfig config = JsonSerializer.Deserialize<BaslerConfig>(json);

                    #region 更新當前 Basler Config
                    BaslerCam baslerCam = MainWindow.BaslerCam;
                    baslerCam.Config.Name = config.Name;
                    ConfigName.Text = config.Name;
                    baslerCam.Config.Width = config.Width;
                    ConfigWidth.Text = $"{config.Width}";
                    baslerCam.Config.Height = config.Height;
                    ConfigHeight.Text = $"{config.Height}";
                    baslerCam.Config.FPS = config.FPS;
                    ConfigFPS.Text = $"{config.FPS}";
                    baslerCam.Config.ExposureTime = config.ExposureTime;
                    ConfigExposureTime.Text = $"{config.ExposureTime}";
                    baslerCam.Config.Save();
                    #endregion
                }
                else
                {
                    // 
                    Debug.WriteLine("組態檔不存在");
                }
            }
        }

        private void ConfigSaveBtn_Click(object sender, RoutedEventArgs e)
        {
            // 按下儲存 Property才會變更
            BaslerCam baslerCam = MainWindow.BaslerCam;
            baslerCam.Config.Width = Convert.ToInt32(ConfigWidth.Text, CultureInfo.CurrentCulture);
            baslerCam.Config.Height = Convert.ToInt32(ConfigHeight.Text, CultureInfo.CurrentCulture);
            baslerCam.Config.FPS = Convert.ToDouble(ConfigFPS.Text, CultureInfo.CurrentCulture);
            baslerCam.Config.ExposureTime = Convert.ToDouble(ConfigExposureTime.Text, CultureInfo.CurrentCulture);
            baslerCam.Config.Name = ConfigName.Text;
            baslerCam.Config.Save();

            string path = $@"./configs/{baslerCam.Config.Name}.json";
            bool IsExist = File.Exists(path);

            string jsonStr = JsonSerializer.Serialize(baslerCam.Config, new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText(path, jsonStr);

            if (!IsExist) // 若原先不存在，則新增
            {
                baslerCam.ConfigList.Add(baslerCam.Config.Name);
            }

            //
            //Debug.WriteLine($"{BaslerCam.Config.Width} , {ConfigWidth.Text}");
            //Debug.WriteLine($"{BaslerCam.Config.Height} , {ConfigHeight.Text}");
            //Debug.WriteLine($"{BaslerCam.Config.FPS} , {ConfigFPS.Text}");
            //Debug.WriteLine($"{BaslerCam.Config.ExposureTime} , {ConfigExposureTime.Text}");
            //Debug.WriteLine($"{BaslerCam.Config.Name} , {ConfigName.Text}");
        }

        private void ConfigWriteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.BaslerCam?.Camera != null)
            {
                BaslerCam baslerCam = MainWindow.BaslerCam;
                Camera camera = MainWindow.BaslerCam.Camera;

                //BaslerCam.ConfigName = BaslerCam.Config.Name;
                baslerCam.ConfigName = ConfigName.Text;

                // 歸零 offset
                camera.Parameters[PLGigECamera.OffsetX].SetToMinimum();
                camera.Parameters[PLGigECamera.OffsetY].SetToMinimum();


                // 嘗試寫入 Width
                if (!camera.Parameters[PLGigECamera.Width].TrySetValue(Convert.ToInt32(ConfigWidth.Text, CultureInfo.CurrentCulture)))
                {
                    camera.Parameters[PLGigECamera.Width].SetToMaximum();
                }
                baslerCam.Config.Width = baslerCam.Width = (int)camera.Parameters[PLGigECamera.Width].GetValue();

                // 嘗試寫入 Height
                if (!camera.Parameters[PLGigECamera.Height].TrySetValue(Convert.ToInt32(ConfigHeight.Text, CultureInfo.CurrentCulture)))
                {
                    camera.Parameters[PLGigECamera.Height].SetToMaximum();
                }
                baslerCam.Config.Height = baslerCam.Height = (int)camera.Parameters[PLGigECamera.Height].GetValue();

                // Width、Height 已變更, 更新 Offset Max 
                baslerCam.OffsetXMax = (int)camera.Parameters[PLGigECamera.OffsetX].GetMaximum();
                baslerCam.OffsetYMax = (int)camera.Parameters[PLGigECamera.OffsetY].GetMaximum();

                // 寫入 FPS
                camera.Parameters[PLGigECamera.AcquisitionFrameRateAbs].SetValue(Convert.ToDouble(ConfigFPS.Text, CultureInfo.CurrentCulture));
                baslerCam.Config.FPS = baslerCam.FPS = camera.Parameters[PLGigECamera.AcquisitionFrameRateAbs].GetValue();

                // 寫入曝光時間
                camera.Parameters[PLGigECamera.ExposureTimeAbs].SetValue(Convert.ToDouble(ConfigExposureTime.Text, CultureInfo.CurrentCulture));   // 10000 is default exposure time of acA2040
                baslerCam.Config.ExposureTime = baslerCam.ExposureTime = camera.Parameters[PLGigECamera.ExposureTimeAbs].GetValue();

                baslerCam.PropertyChange();

                // offset 置中 
                //CamCenterMove.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                MainWindow.OffsetPanel.CamCenterMove.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

                MainWindow.Indicator.Image = null;
                // 重置 Image
                MainWindow.ImageSource = null;
                // 重置縮放率
                MainWindow.ZoomRatio = 100;
            }
        }

        private void ConfigDelBtn_Click(object sender, RoutedEventArgs e)
        {
            string file = ConfigSelector.SelectedItem as string;

            if (file == string.Empty)
            {
                Debug.WriteLine("請選擇欲刪除檔案");
                return;
            }

            if (MessageBox.Show("是否確認刪除?", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                string path = $@"./configs/{file}.json";

                if (File.Exists(path))
                {
                    File.Delete(path);

                    _ = MainWindow.BaslerCam.ConfigList.Remove(file);
                    // 從 config list 移除
                }
            }
        }
    }
}
