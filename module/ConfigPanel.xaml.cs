using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Basler.Pylon;
using MaterialDesignThemes.Wpf;
using MCAJawIns.content;

// 
// 此檔案測試完作廢
// 此檔案測試完作廢
// 此檔案測試完作廢
// 

namespace MCAJawIns.module
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
        public EngineerTab EngineerTab { get; set; }
        /// <summary>
        /// Basler Camera Obj
        /// </summary>
        public BaslerCam Cam { get; set; }

        /// <summary>
        /// 
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

        private void Card_Loaded(object sender, RoutedEventArgs e)
        {
            Cam = DataContext as BaslerCam;

            if (BaslerConfig == null)
            {
                // 建立在 ConfigPanel.xaml 裡
                BaslerConfig = FindResource(nameof(BaslerConfig)) as BaslerConfig;
            }
        }

        /// <summary>
        /// 綁定 Binding
        /// 因為 Popupbox 一開始不會建立, 需要從後端綁定
        /// 
        /// </summary>
        private void SetBinding()
        {
            Binding binding = new()
            {
                Mode = BindingMode.OneWay,
                ElementName = "ConfigSelector",
                Path = new PropertyPath("SelectedIndex"),
                Converter = new Converter.NumberNotEqualConverter(),
                ConverterParameter = -1,
                FallbackValue = false,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            _ = ConfigDelBtn.SetBinding(IsEnabledProperty, binding);
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
            // Focus MainWindow 
            _ = (Window.GetWindow(this) as MainWindow).TitleGrid.Focus();
        }

        private void ConfigPopupBox_Opened(object sender, RoutedEventArgs e)
        {
            Cam = DataContext as BaslerCam;
            //SyncConfiguration(Cam.Config, Cam);
            // 載入 Config files
            Initialize_JsonFile();
            // ConfigDelBtn 綁定Config Selector 
            SetBinding();
            // Debug.WriteLine("Combobox opened");
        }

        private void ConfigPopupBox_Closed(object sender, RoutedEventArgs e)
        {
            // // 
            // if (MainWindow.BaslerCam?.Camera != null)
            if (Cam?.Camera != null)
            {
                // 同步 Config 和 Cam
                SyncConfiguration(Cam.Config, Cam);
            }
            // 重置 Selected Index 
            ConfigSelector.SelectedIndex = -1;
            // Debug.WriteLine($"{Cam?.Camera != null}");
            // Debug.WriteLine("Combobox closed");
        }

        /// <summary>
        /// 載入 Config Json File
        /// </summary>
        private void Initialize_JsonFile()
        {
            if (string.IsNullOrEmpty(Cam?.ModelName))
            {
                return;
            }

            string path = $@"{ConfigsDirectory}/{Cam.ModelName}";

            if (Directory.Exists(path))
            {
                string[] files = Directory.GetFiles(path, "*.json", SearchOption.TopDirectoryOnly);
                files = Array.ConvertAll(files, file => file = Path.GetFileNameWithoutExtension(file));

                foreach (string file in files)
                {
                    // if (!BaslerCam.ConfigList.Contains(file))
                    // {
                    //     BaslerCam.ConfigList.Add(file);
                    // }
                    // if (!MainWindow.BaslerCam.ConfigList.Contains(file))

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
        /// 同步 config 和 camera
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
                    // BaslerCam baslerCam = MainWindow.BaslerCam;
                    Cam.Config.Name = config.Name;
                    //ConfigName.Text = config.Name;
                    Cam.Config.Width = config.Width;
                    //ConfigWidth.Text = $"{config.Width}";
                    Cam.Config.Height = config.Height;
                    //ConfigHeight.Text = $"{config.Height}";
                    Cam.Config.FPS = config.FPS;
                    //ConfigFPS.Text = $"{config.FPS}";
                    Cam.Config.ExposureTime = config.ExposureTime;
                    //ConfigExposureTime.Text = $"{config.ExposureTime}";
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
            // 按下儲存 Property 才會變更
            // BaslerCam baslerCam = MainWindow.BaslerCam;
            // BaslerCam baslerCam = DataContext as BaslerCam;
#if false
            Cam.Config.Width = Convert.ToInt32(ConfigWidth.Text, CultureInfo.CurrentCulture);
            Cam.Config.Height = Convert.ToInt32(ConfigHeight.Text, CultureInfo.CurrentCulture);
            Cam.Config.FPS = Convert.ToDouble(ConfigFPS.Text, CultureInfo.CurrentCulture);
            Cam.Config.ExposureTime = Convert.ToDouble(ConfigExposureTime.Text, CultureInfo.CurrentCulture);
            Cam.Config.Name = ConfigName.Text;
            Cam.Config.Save();
#endif

            string path = $@"{ConfigsDirectory}/{Cam.ModelName}/{Cam.Config.Name}.json";
            bool IsExist = File.Exists(path);

            string jsonStr = JsonSerializer.Serialize(Cam.Config, new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText(path, jsonStr);
            Cam.Config.Save();

            if (!IsExist) // 若原先不存在，則新增
            {
                Cam.ConfigList.Add(Cam.Config.Name);
            }
        }

        private void ConfigWriteBtn_Click(object sender, RoutedEventArgs e)
        {
            // if (MainWindow.BaslerCam?.Camera != null)
            if (Cam?.Camera != null)
            {
                // BaslerCam baslerCam = MainWindow.BaslerCam;
                Camera camera = Cam.Camera;

                // BaslerCam.ConfigName = BaslerCam.Config.Name;
                Cam.ConfigName = Cam.Config.Name;

                // 歸零 offset
                camera.Parameters[PLGigECamera.OffsetX].SetToMinimum();
                camera.Parameters[PLGigECamera.OffsetY].SetToMinimum();

                // 嘗試寫入 Width // 
                // if (!camera.Parameters[PLGigECamera.Width].TrySetValue(Convert.ToInt32(ConfigWidth.Text, CultureInfo.CurrentCulture)))
                if (!camera.Parameters[PLGigECamera.Width].TrySetValue(Cam.Config.Width))
                {
                    camera.Parameters[PLGigECamera.Width].SetToMaximum();
                }
                Cam.Config.Width = Cam.Width = (int)camera.Parameters[PLGigECamera.Width].GetValue();

                // 嘗試寫入 Height // 
                if (!camera.Parameters[PLGigECamera.Height].TrySetValue(Cam.Config.Height))
                {
                    camera.Parameters[PLGigECamera.Height].SetToMaximum();
                }
                Cam.Config.Height = Cam.Height = (int)camera.Parameters[PLGigECamera.Height].GetValue();

                // Width、Height 已變更, 更新 Offset Max 
                Cam.OffsetXMax = (int)camera.Parameters[PLGigECamera.OffsetX].GetMaximum();
                Cam.OffsetYMax = (int)camera.Parameters[PLGigECamera.OffsetY].GetMaximum();

                // ROI 置中
                camera.Parameters[PLGigECamera.CenterX].SetValue(true);     // 會鎖定 Offset
                camera.Parameters[PLGigECamera.CenterY].SetValue(true);     // 會鎖定 Offset
                Cam.OffsetX = (int)camera.Parameters[PLGigECamera.OffsetX].GetValue();  // 取得當前 OffsetX
                Cam.OffsetY = (int)camera.Parameters[PLGigECamera.OffsetY].GetValue();  // 取得當前 OffsetY
                camera.Parameters[PLGigECamera.CenterX].SetValue(false);    // 解鎖 Center
                camera.Parameters[PLGigECamera.CenterY].SetValue(false);    // 解鎖 Center 

                // 寫入 FPS
                camera.Parameters[PLGigECamera.AcquisitionFrameRateAbs].SetValue(Cam.Config.FPS);
                Cam.Config.FPS = Cam.FPS = camera.Parameters[PLGigECamera.AcquisitionFrameRateAbs].GetValue();

                // 寫入曝光時間
                camera.Parameters[PLGigECamera.ExposureTimeAbs].SetValue(Cam.Config.ExposureTime);   // 10000 is default exposure time of acA2040
                Cam.Config.ExposureTime = Cam.ExposureTime = camera.Parameters[PLGigECamera.ExposureTimeAbs].GetValue();
                Cam.PropertyChange();


                // 重置 ImageSource，因為 Width & Height 有變更
                // MainWindow.ImageSource = null;
                // EngineerTab.Indicator.ImageSource = null;
                EngineerTab.Indicator.Image = null;

                // Reset ZoomRatio // 這邊需要修正
                EngineerTab.ZoomRatio = 100;

                // offset 置中 
                // CamCenterMove.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                // MainWindow.OffsetPanel.CamCenterMove.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

                // MainWindow.Indicator.Image = null;
                // 重置 Image
                // MainWindow.ImageSource = null;
                // 重置縮放率
                // MainWindow.ZoomRatio = 100;
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

                    // _ = MainWindow.BaslerCam.ConfigList.Remove(file);
                    _ = Cam.ConfigList.Remove(file);
                    // 從 config list 移除
                }
            }
        }
    }
}
