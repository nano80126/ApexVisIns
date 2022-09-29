using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Basler.Pylon;
using MCAJawIns.Tab;

namespace MCAJawIns.Panel
{
    /// <summary>
    /// ConfigPanel.xaml 的互動邏輯
    /// </summary>
    public partial class ConfigPanel : Control.CustomCard
    {
        #region Fields
        /// <summary>
        /// Config 路徑
        /// </summary>
        private readonly string configsDirectory = @"./configs";

        /// <summary>
        /// Basler Camera Obj
        /// </summary>
        private BaslerCam cam;
        #endregion

        #region Properties
        /// <summary>
        /// MainWindow
        /// </summary>
        public MainWindow MainWindow { get; } = (MainWindow)Application.Current.MainWindow;
        /// <summary>
        /// 上層視窗 (待確認)
        /// </summary>
        public EngineerTab EngineerTab { get; set; }

#if temporary
        /// <summary>
        /// Basler 組態
        /// </summary>
        public BaslerConfig BaslerConfig { get; set; } 
#endif
        #endregion

        public ConfigPanel()
        {
            InitializeComponent();
        }

        private void CustomCard_Loaded(object sender, RoutedEventArgs e)
        {
            cam = DataContext as BaslerCam;

#if temporary
            //if (BaslerConfig == null)
            //{
            //    BaslerConfig = FindResource(nameof(BaslerConfig)) as BaslerConfig;
            //}  
#endif
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
                Converter = new Converter.NumberNotEqualConverter(),
                ConverterParameter = -1,
                FallbackValue = false,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            _ = ConfigDelBtn?.SetBinding(IsEnabledProperty, binding);
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
            cam = DataContext as BaslerCam;

            Initialize_JsonFile();

            ConfigDelBtnSetBinding();
        }

        private void ConfigPopupBox_Closed(object sender, RoutedEventArgs e)
        {
            if (cam?.Camera != null) { SyncConfiguration(cam.Config, cam); }

            // 重置 Selected Index
            ConfigSelector.SelectedIndex = -1;
        }

        /// <summary>
        /// 載入 Config Json File
        /// </summary>
        private void Initialize_JsonFile()
        {
            if (string.IsNullOrEmpty(cam?.ModelName)) { return; }

            string path = $@"{configsDirectory}/{cam.ModelName}";

            if (Directory.Exists(path))
            {
                string[] files = Directory.GetFiles(path, "*.json", SearchOption.TopDirectoryOnly);
                files = Array.ConvertAll(files, file => file = Path.GetFileNameWithoutExtension(file));

                foreach (string file in files)
                {
                    if (!cam.ConfigList.Contains(file))
                    {
                        cam.ConfigList.Add(file);
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
                string path = $@"{configsDirectory}/{cam.ModelName}/{file}.json";

                if (File.Exists(path))
                {
                    using StreamReader reader = File.OpenText(path);
                    string json = reader.ReadToEnd();

                    BaslerConfig config = JsonSerializer.Deserialize<BaslerConfig>(json);

                    #region 更新當前 Basler Config
                    cam.Config.Name = config.Name;
                    cam.Config.Width = config.Width;
                    cam.Config.Height = config.Height;
                    cam.Config.FPS = config.FPS;
                    cam.Config.ExposureTime = config.ExposureTime;
                    cam.Config.Save();
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
            string path = $@"{configsDirectory}/{cam.ModelName}/{cam.Config.Name}.json";
            bool IsExist = File.Exists(path);

            string jsonStr = JsonSerializer.Serialize(cam.Config, new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText(path, jsonStr);
            cam.Config.Save();

            if (!IsExist)   // 若原先不存在，則新增
            {
                cam.ConfigList.Add(cam.Config.Name);
            }
        }

        private void ConfigWriteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (cam?.Camera != null)
            {
                // BaslerCam baslerCam = MainWindow.BaslerCam;
                Camera camera = cam.Camera;

                // BaslerCam.ConfigName = BaslerCam.Config.Name;
                cam.ConfigName = cam.Config.Name;

                // 歸零 offset
                camera.Parameters[PLGigECamera.OffsetX].SetToMinimum();
                camera.Parameters[PLGigECamera.OffsetY].SetToMinimum();

                #region 寫入 Width & Height
                // 嘗試寫入 Width
                if (!camera.Parameters[PLGigECamera.Width].TrySetValue(cam.Config.Width))
                {
                    camera.Parameters[PLGigECamera.Width].SetToMaximum();
                }
                cam.Config.Width = cam.Width = (int)camera.Parameters[PLGigECamera.Width].GetValue();

                // 嘗試寫入 Height
                if (!camera.Parameters[PLGigECamera.Height].TrySetValue(cam.Config.Height))
                {
                    camera.Parameters[PLGigECamera.Height].SetToMaximum();
                }
                cam.Config.Height = cam.Height = (int)camera.Parameters[PLGigECamera.Height].GetValue(); 

                // Width、Height 已變更, 更新 Offset Max 
                cam.OffsetXMax = (int)camera.Parameters[PLGigECamera.OffsetX].GetMaximum();
                cam.OffsetYMax = (int)camera.Parameters[PLGigECamera.OffsetY].GetMaximum();
                #endregion

                #region ROI 置中
                camera.Parameters[PLGigECamera.CenterX].SetValue(true);                 // 會鎖定 Offset
                camera.Parameters[PLGigECamera.CenterY].SetValue(true);                 // 會鎖定 Offset
                cam.OffsetX = (int)camera.Parameters[PLGigECamera.OffsetX].GetValue();  // 取得當前 OffsetX
                cam.OffsetY = (int)camera.Parameters[PLGigECamera.OffsetY].GetValue();  // 取得當前 OffsetY
                camera.Parameters[PLGigECamera.CenterX].SetValue(false);                // 解鎖 Center
                camera.Parameters[PLGigECamera.CenterY].SetValue(false);                // 解鎖 Center  
                #endregion

                #region 寫入 FPS
                camera.Parameters[PLGigECamera.AcquisitionFrameRateAbs].SetValue(cam.Config.FPS);
                cam.Config.FPS = cam.FPS = camera.Parameters[PLGigECamera.AcquisitionFrameRateAbs].GetValue();
                #endregion

                #region 寫入曝光時間
                camera.Parameters[PLGigECamera.ExposureTimeAbs].SetValue(cam.Config.ExposureTime);   // 10000 is default exposure time of acA2040
                cam.Config.ExposureTime = cam.ExposureTime = camera.Parameters[PLGigECamera.ExposureTimeAbs].GetValue();
                #endregion

                cam.SetUserSet(null);
                cam.PropertyChange();

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
                string path = $@"{configsDirectory}/{cam.ModelName}/{file}.json";

                if (File.Exists(path))
                {
                    File.Delete(path);

                    _ = cam.ConfigList.Remove(file);
                }
            }
        }

        [Obsolete]
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string jsonStr = JsonSerializer.Serialize(cam, new JsonSerializerOptions()
            {
                WriteIndented = true
            });

            Debug.WriteLine($"{jsonStr}");
        }

        [Obsolete]
        private void UserSetLoad_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"123");
        }

        private void UserSet_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string userSet = (sender as ListBox).SelectedItem as string;

            Debug.WriteLine($"{userSet} {cam.UserSet}");

            if (cam.UserSet != userSet)
            {
                Camera camera = cam.Camera;

                // 載入 UserSet
                camera.Parameters[PLGigECamera.UserSetSelector].SetValue(userSet);
                camera.Parameters[PLGigECamera.UserSetLoad].Execute();
                // Width, Height
                cam.Config.Width = cam.Width = (int)camera.Parameters[PLGigECamera.Width].GetValue();
                cam.Config.Height = cam.Height = (int)camera.Parameters[PLGigECamera.Height].GetValue();
                // OffsetMax, Offset
                cam.OffsetXMax = (int)camera.Parameters[PLGigECamera.OffsetX].GetMaximum();
                cam.OffsetYMax = (int)camera.Parameters[PLGigECamera.OffsetY].GetMaximum();
                cam.OffsetX = (int)camera.Parameters[PLGigECamera.OffsetX].GetValue();
                cam.OffsetY = (int)camera.Parameters[PLGigECamera.OffsetY].GetValue();
                // FPS, Exposure
                cam.Config.FPS = cam.FPS = camera.Parameters[PLGigECamera.AcquisitionFrameRateAbs].GetValue();
                cam.Config.ExposureTime = cam.ExposureTime = camera.Parameters[PLGigECamera.ExposureTimeAbs].GetValue();

                cam.Config.Name = cam.ConfigName = null;

                cam.SetUserSet(userSet);
                cam.PropertyChange();

                // Reset Image
                EngineerTab.Indicator.Image = null;

                // Reset ZoomRatio
                EngineerTab.ZoomRatio = 100;
            }
        }
    }
}
