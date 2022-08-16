using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Basler.Pylon;
using MCAJawIns.content;

namespace MCAJawIns.Panel
{
    /// <summary>
    /// ConfigPanel.xaml 的互動邏輯
    /// </summary>
    public partial class ConfigPanel : Control.CustomCard
    {
#if false
        /// <summary>
        /// 繼承 主視窗
        /// </summary>
        public MainWindow MainWindow { get; set; }
#endif
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

        public ConfigPanel()
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

        /// <summary>
        /// 綁定 EnableProperty
        /// </summary>
        private void ConfigDelBtnSetBinding()
        {
            Binding binding = new Binding()
            {
                Mode = BindingMode.OneWay,
                ElementName = nameof(ConfigSelector),
                Path = new PropertyPath(nameof(ConfigSelector.SelectedIndex)),
                ConverterParameter = -1,
                FallbackValue = false,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            _ = ConfigDelBtn.SetBinding(IsEnabledProperty, binding);
        }

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
            Cam = DataContext as BaslerCam;

            Initialize_JsonFile();

            ConfigDelBtnSetBinding();
        }

        private void ConfigPopupBox_Closed(object sender, RoutedEventArgs e)
        {
            if (Cam?.Camera != null) { SyncConfiguration(Cam.Config, Cam); }

            // 重置 Selected Index
            ConfigSelector.SelectedIndex = -1;
        }

        /// <summary>
        /// 載入 Config Json File
        /// </summary>
        private void Initialize_JsonFile()
        {
            if (string.IsNullOrEmpty(Cam?.ModelName)) { return; }

            string path = $@"{ConfigsDirectory}/{Cam.ModelName}";

            if (Directory.Exists(path))
            {
                string[] files = Directory.GetFiles(path, "*.json", SearchOption.TopDirectoryOnly);
                files = Array.ConvertAll(files, file => file = Path.GetFileNameWithoutExtension(file));

                foreach (string file in files)
                {
                    if (!Cam.ConfigList.Contains(file))
                    {
                        Cam.ConfigList.Add(file);
                    }
                }
            }
            else
            {
                _ = Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// 同步 Config 和 Camera
        /// </summary>
        /// <param name="config">目標組態</param>
        /// <param name="camera">來源相機</param>
        public void SyncConfiguration(BaslerConfig config, BaslerCam camera)
        {
            config.Name = camera.ConfigName;
            config.Width = camera.Width;
            config.Height = camera.Height;
            config.FPS = camera.FPS;
            config.ExposureTime = camera.ExposureTime;
        }

        private void ConfigSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string file = (sender as ComboBox).SelectedItem as string;

            if (!string.IsNullOrWhiteSpace(file))
            {
                string path = $@"{ConfigsDirectory}/{Cam.ModelName}/{file}.json";

                if (File.Exists(path))
                {
                    using StreamReader reader = File.OpenText(path);
                    string json = reader.ReadToEnd();

                    BaslerConfig config = JsonSerializer.Deserialize<BaslerConfig>(json);

                    #region 更新當前 Basler Config
                    Cam.Config.Name = config.Name;
                    Cam.Config.Width = config.Width;
                    Cam.Config.Height = config.Height;
                    Cam.Config.FPS = config.FPS;
                    Cam.Config.ExposureTime = config.ExposureTime;
                    Cam.Config.Save();
                    #endregion
                }
                else
                {
                    MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, "組態檔不存在");
                }
            }
        }

        private void ConfigSaveBtn_Click(object sender, RoutedEventArgs e)
        {
            string path = $@"{ConfigsDirectory}/{Cam.ModelName}/{Cam.Config.Name}.json";
            bool IsExist = File.Exists(path);

            string jsonStr = JsonSerializer.Serialize(Cam.Config, new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText(path, jsonStr);
            Cam.Config.Save();

            if (!IsExist)   // 若原先不存在，則新增
            {
                Cam.ConfigList.Add(Cam.Config.Name);
            }
        }

        private void ConfigWriteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Cam?.Camera != null)
            {
                // BaslerCam baslerCam = MainWindow.BaslerCam;
                Camera camera = Cam.Camera;

                // BaslerCam.ConfigName = BaslerCam.Config.Name;
                Cam.ConfigName = Cam.Config.Name;

                // 歸零 offset
                camera.Parameters[PLGigECamera.OffsetX].SetToMinimum();
                camera.Parameters[PLGigECamera.OffsetY].SetToMinimum();

                // 嘗試寫入 Width
                if (!camera.Parameters[PLGigECamera.Width].TrySetValue(Cam.Config.Width))
                {
                    camera.Parameters[PLGigECamera.Width].SetToMaximum();
                }
                Cam.Config.Width = Cam.Width = (int)camera.Parameters[PLGigECamera.Width].GetValue();

                // 嘗試寫入 Height
                if (!camera.Parameters[PLGigECamera.Height].TrySetValue(Cam.Config.Height))
                {
                    camera.Parameters[PLGigECamera.Height].SetToMaximum();
                }
                Cam.Config.Height = Cam.Height = (int)camera.Parameters[PLGigECamera.Height].GetValue();

                // Width、Height 已變更, 更新 Offset Max 
                Cam.OffsetXMax = (int)camera.Parameters[PLGigECamera.OffsetX].GetMaximum();
                Cam.OffsetYMax = (int)camera.Parameters[PLGigECamera.OffsetY].GetMaximum();

                // ROI 置中
                camera.Parameters[PLGigECamera.CenterX].SetValue(true);                 // 會鎖定 Offset
                camera.Parameters[PLGigECamera.CenterY].SetValue(true);                 // 會鎖定 Offset
                Cam.OffsetX = (int)camera.Parameters[PLGigECamera.OffsetX].GetValue();  // 取得當前 OffsetX
                Cam.OffsetY = (int)camera.Parameters[PLGigECamera.OffsetY].GetValue();  // 取得當前 OffsetY
                camera.Parameters[PLGigECamera.CenterX].SetValue(false);                // 解鎖 Center
                camera.Parameters[PLGigECamera.CenterY].SetValue(false);                // 解鎖 Center 

                // 寫入 FPS
                camera.Parameters[PLGigECamera.AcquisitionFrameRateAbs].SetValue(Cam.Config.FPS);
                Cam.Config.FPS = Cam.FPS = camera.Parameters[PLGigECamera.AcquisitionFrameRateAbs].GetValue();

                // 寫入曝光時間
                camera.Parameters[PLGigECamera.ExposureTimeAbs].SetValue(Cam.Config.ExposureTime);   // 10000 is default exposure time of acA2040
                Cam.Config.ExposureTime = Cam.ExposureTime = camera.Parameters[PLGigECamera.ExposureTimeAbs].GetValue();
                Cam.PropertyChange();

                // 重置 ImageSource，因為 Width & Height 有變更
                EngineerTab.Indicator.Image = null;

                // Reset ZoomRatio
                EngineerTab.ZoomRatio = 100;
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
                string path = $@"{ConfigsDirectory}/{Cam.ModelName}/{file}.json";

                if (File.Exists(path))
                {
                    File.Delete(path);

                    _ = Cam.ConfigList.Remove(file);
                }
            }
        }
    }
}
