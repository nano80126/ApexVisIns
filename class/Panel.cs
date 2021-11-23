using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Globalization;
using System.Windows.Input;
using Basler.Pylon;
using System.Windows.Controls.Primitives;

namespace ApexVisIns
{
    public partial class MainWindow : System.Windows.Window
    {
        private string ConfigsDirectory { get; } = @"./configs";

        /// <summary>
        /// 初始化JSON檔案路徑,
        /// Combobox列出組態檔
        /// </summary>
        private void Initialize_JsonFile()
        {
            if (Directory.Exists(ConfigsDirectory))
            {
                string[] files = Directory.GetFiles(ConfigsDirectory, "*.json", SearchOption.TopDirectoryOnly);
                files = Array.ConvertAll(files, file => file = Path.GetFileNameWithoutExtension(file));

                foreach (string file in files)
                {
                    if (!BaslerCam.ConfigList.Contains(file))
                    {
                        BaslerCam.ConfigList.Add(file);
                    }
                }
            }
            else
            {
                _ = Directory.CreateDirectory(ConfigsDirectory);
            }
        }

        /// <summary>
        /// Reset Textbox focused
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
            _ = TitleGrid.Focus();
        }

        #region Config Panel
        private void ConfigPopupBox_Opened(object sender, RoutedEventArgs e)
        {
            Initialize_JsonFile();
        }

        private void ConfigPopupBox_Closed(object sender, RoutedEventArgs e)
        {
            /// /// 
            if (BaslerCam?.Camera != null)
            {
                SyncConfiguration(BaslerCam.Config, BaslerCam);
            }
            //ConfigSelector.SelectedIndex = -1;
        }

        /// <summary>
        /// 同步 Config 和 Camera
        /// </summary>
        /// <param name="config">目標組態</param>
        /// <param name="camera">來源相機</param>
        private void SyncConfiguration(BaslerConfig config, BaslerCam camera)
        {
#if false
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
#endif
        }

#if false
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
                    BaslerCam.Config.Name = config.Name;
                    ConfigName.Text = config.Name;
                    BaslerCam.Config.Width = config.Width;
                    ConfigWidth.Text = $"{config.Width}";
                    BaslerCam.Config.Height = config.Height;
                    ConfigHeight.Text = $"{config.Height}";
                    BaslerCam.Config.FPS = config.FPS;
                    ConfigFPS.Text = $"{config.FPS}";
                    BaslerCam.Config.ExposureTime = config.ExposureTime;
                    ConfigExposureTime.Text = $"{config.ExposureTime}";
                    BaslerCam.Config.Save();
                    #endregion
                }
                else
                {
                    // 
                    Debug.WriteLine("組態檔不存在");
                }
            }
        }

#endif

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

#if false
        private void ConfigWriteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (BaslerCam != null && BaslerCam.Camera != null)
            {
                Camera camera = BaslerCam.Camera;

                //BaslerCam.ConfigName = BaslerCam.Config.Name;
                BaslerCam.ConfigName = ConfigName.Text;

                // 歸零 offset
                camera.Parameters[PLGigECamera.OffsetX].SetToMinimum();
                camera.Parameters[PLGigECamera.OffsetY].SetToMinimum();


                // 嘗試寫入 Width
                if (!camera.Parameters[PLGigECamera.Width].TrySetValue(Convert.ToInt32(ConfigWidth.Text, CultureInfo.CurrentCulture)))
                {
                    camera.Parameters[PLGigECamera.Width].SetToMaximum();
                }
                BaslerCam.Config.Width = BaslerCam.Width = (int)camera.Parameters[PLGigECamera.Width].GetValue();

                // 嘗試寫入 Height
                if (!camera.Parameters[PLGigECamera.Height].TrySetValue(Convert.ToInt32(ConfigHeight.Text, CultureInfo.CurrentCulture)))
                {
                    camera.Parameters[PLGigECamera.Height].SetToMaximum();
                }
                BaslerCam.Config.Height = BaslerCam.Height = (int)camera.Parameters[PLGigECamera.Height].GetValue();

                // Width、Height 已變更, 更新 Offset Max 
                BaslerCam.OffsetXMax = (int)camera.Parameters[PLGigECamera.OffsetX].GetMaximum();
                BaslerCam.OffsetYMax = (int)camera.Parameters[PLGigECamera.OffsetY].GetMaximum();

                // 寫入 FPS
                camera.Parameters[PLGigECamera.AcquisitionFrameRateAbs].SetValue(Convert.ToDouble(ConfigFPS.Text, CultureInfo.CurrentCulture));
                BaslerCam.Config.FPS = BaslerCam.FPS = camera.Parameters[PLGigECamera.AcquisitionFrameRateAbs].GetValue();

                // 寫入曝光時間
                camera.Parameters[PLGigECamera.ExposureTimeAbs].SetValue(Convert.ToDouble(ConfigExposureTime.Text, CultureInfo.CurrentCulture));   // 10000 is default exposure time of acA2040
                BaslerCam.Config.ExposureTime = BaslerCam.ExposureTime = camera.Parameters[PLGigECamera.ExposureTimeAbs].GetValue();

                BaslerCam.PropertyChange();

                // offset 置中 
                //CamCenterMove.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                OffsetPanel.CamCenterMove.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

                Indicator.Image = null;
                // 重置 Image
                ImageSource = null;
                // 重置縮放率
                ZoomRatio = 100;
            }
        }

        private void ConfigSaveBtn_Click(object sender, RoutedEventArgs e)
        {
            // 按下儲存 Property才會變更

            BaslerCam.Config.Width = Convert.ToInt32(ConfigWidth.Text, CultureInfo.CurrentCulture);
            BaslerCam.Config.Height = Convert.ToInt32(ConfigHeight.Text, CultureInfo.CurrentCulture);
            BaslerCam.Config.FPS = Convert.ToDouble(ConfigFPS.Text, CultureInfo.CurrentCulture);
            BaslerCam.Config.ExposureTime = Convert.ToDouble(ConfigExposureTime.Text, CultureInfo.CurrentCulture);
            BaslerCam.Config.Name = ConfigName.Text;
            BaslerCam.Config.Save();

            string path = $@"./configs/{BaslerCam.Config.Name}.json";
            bool IsExist = File.Exists(path);

            string jsonStr = JsonSerializer.Serialize(BaslerCam.Config, new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText(path, jsonStr);

            if (!IsExist) // 若原先不存在，則新增
            {
                BaslerCam.ConfigList.Add(BaslerCam.Config.Name);
            }


            //

            Debug.WriteLine($"{BaslerCam.Config.Width} , {ConfigWidth.Text}");
            Debug.WriteLine($"{BaslerCam.Config.Height} , {ConfigHeight.Text}");
            Debug.WriteLine($"{BaslerCam.Config.FPS} , {ConfigFPS.Text}");
            Debug.WriteLine($"{BaslerCam.Config.ExposureTime} , {ConfigExposureTime.Text}");
            Debug.WriteLine($"{BaslerCam.Config.Name} , {ConfigName.Text}");


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

                    _ = BaslerCam.ConfigList.Remove(file);
                    // 從 config list 移除
                }
            }
        } 
#endif
        #endregion

        #region Offset Panel
#if false
        private void CamLeftMove_Click(object sender, RoutedEventArgs e)
        {
            if (BaslerCam?.Camera != null)
            {
                int offset_x = BaslerCam.OffsetX - 20 < 0 ? 0 : BaslerCam.OffsetX - 20;
                if (BaslerCam.Camera.Parameters[PLGigECamera.OffsetX].TrySetValue(offset_x))
                {
                    BaslerCam.OffsetX = (int)BaslerCam.Camera.Parameters[PLGigECamera.OffsetX].GetValue();
                }
            }
        }

        private void CamTopMove_Click(object sender, RoutedEventArgs e)
        {
            if (BaslerCam?.Camera != null)
            {
                int offset_y = BaslerCam.OffsetY - 20 < 0 ? 0 : BaslerCam.OffsetY - 20;
                if (BaslerCam.Camera.Parameters[PLGigECamera.OffsetY].TrySetValue(offset_y))
                {
                    BaslerCam.OffsetY = (int)BaslerCam.Camera.Parameters[PLGigECamera.OffsetY].GetValue();
                }
            }
        }

        private void CamRightMove_Click(object sender, RoutedEventArgs e)
        {
            if (BaslerCam?.Camera != null)
            {
                int offset_x = BaslerCam.OffsetX + 20 > BaslerCam.OffsetXMax ? BaslerCam.OffsetXMax : BaslerCam.OffsetX + 20;
                if (BaslerCam.Camera.Parameters[PLGigECamera.OffsetX].TrySetValue(offset_x))
                {
                    BaslerCam.OffsetX = (int)BaslerCam.Camera.Parameters[PLGigECamera.OffsetX].GetValue();
                }
            }
        }

        private void CamBottomMove_Click(object sender, RoutedEventArgs e)
        {
            if (BaslerCam?.Camera != null)
            {
                int offset_y = BaslerCam.OffsetY + 20 > BaslerCam.OffsetYMax ? BaslerCam.OffsetYMax : BaslerCam.OffsetY + 20;
                if (BaslerCam.Camera.Parameters[PLGigECamera.OffsetY].TrySetValue(offset_y))
                {
                    BaslerCam.OffsetY = (int)BaslerCam.Camera.Parameters[PLGigECamera.OffsetY].GetValue();
                }
            }
        }

        private void CamCenterMove_Click(object sender, RoutedEventArgs e)
        {
            if (BaslerCam?.Camera != null)
            {
                int offset_x = (BaslerCam.WidthMax - BaslerCam.Width) / 2 % 2 == 0 ? (BaslerCam.WidthMax - BaslerCam.Width) / 2 : (BaslerCam.WidthMax - BaslerCam.Width) / 2 - 1;
                int offset_y = (BaslerCam.HeightMax - BaslerCam.Height) / 2 % 2 == 0 ? (BaslerCam.HeightMax - BaslerCam.Height) / 2 : (BaslerCam.HeightMax - BaslerCam.Height) / 2 - 1;

                if (BaslerCam.Camera.Parameters[PLGigECamera.OffsetX].TrySetValue(offset_x))
                {
                    BaslerCam.OffsetX = (int)BaslerCam.Camera.Parameters[PLGigECamera.OffsetX].GetValue();
                }

                if (BaslerCam.Camera.Parameters[PLGigECamera.OffsetY].TrySetValue(offset_y))
                {
                    BaslerCam.OffsetY = (int)BaslerCam.Camera.Parameters[PLGigECamera.OffsetY].GetValue();
                }
            }
        } 
#endif
        #endregion

        #region 

        #endregion
    }
}
