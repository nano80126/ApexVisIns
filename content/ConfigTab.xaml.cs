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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using Basler.Pylon;


namespace ApexVisIns.content
{
    /// <summary>
    /// ConfigTab.xaml 的互動邏輯
    /// </summary>
    public partial class ConfigTab : StackPanel
    {
        #region Resources

        #endregion

        /// <summary>
        /// json file 載入之 List
        /// </summary>
        private List<BaslerCamInfo> jsonCfgInfo;

        /// <summary>
        /// Device 路徑
        /// </summary>

        #region Varibles
        private bool EventHasBound = false;
        private string DevicesDirectory { get; } = @"./devices";
        public MainWindow MainWindow { get; set; }
        #endregion

        public ConfigTab()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Config Tab Load 事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Config Tab Load");
            #region 保留， 確認無用途則刪除

            // 綁定 Collection 變更事件
            if (!EventHasBound) // 避免重複綁定
            {
                MainWindow.CameraEnumer.CamsSource.CollectionChanged += CamsSource_CollectionChanged;
                EventHasBound = true;
            }

            // 載入
            LoadDeviceConfigs();
            #endregion
        }

        /// <summary>
        /// Config Tab Unload 事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StackPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Config Tab Unload");

            #region 保留，確認無用途則刪除
            // 取消 Collection 變更事件
            if (EventHasBound)  // 確認已綁定
            {
                MainWindow.CameraEnumer.CamsSource.CollectionChanged -= CamsSource_CollectionChanged;
                EventHasBound = false;
            }
            #endregion
        }

        private void CamsSource_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Action.Remove 也要新增
            // DeviceConfig 要新增是否在線 Property
            // 


            // 若有新相機連線，跟jsonConfigList比較，若有紀錄則新增
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                List<BaslerCamInfo> list = (sender as ObservableCollection<BaslerCamInfo>).ToList();

                Debug.WriteLine($"{list.Count}");

                if (list.Count > 0)
                {
                    foreach (BaslerCamInfo item in list)
                    {
                        Debug.WriteLine($"{item.Model} {item.SerialNumber}");
                    }
                }

                Debug.WriteLine("-----------------------------------");
                foreach (BaslerCamInfo item in jsonCfgInfo)
                {
                    Debug.WriteLine($"{item.Model} {item.SerialNumber}");
                }

                foreach (BaslerCamInfo item in list)
                {
                    if (jsonCfgInfo.Exists(e => e.SerialNumber == item.SerialNumber))
                    {
                        DeviceConfig config = new(item.FullName, item.Model, item.IP, item.MAC, item.SerialNumber)
                        {
                            VendorName = item.VendorName,
                            CameraType = item.CameraType,
                            Online = true
                        };
                        Dispatcher.Invoke(() => MainWindow.DeviceConfigs.Add(config));
                    }
                }
            }

            // 之後可能改為有紀錄的全部列出
            // 再用 ICON 標示是否有連線
        }

        /// <summary>
        /// 載入 device.json
        /// </summary>
        private void LoadDeviceConfigs()
        {
            string path = $@"{DevicesDirectory}/device.json";

            if (!Directory.Exists(DevicesDirectory))
            {
                // 新增路徑
                _ = Directory.CreateDirectory(DevicesDirectory);
                // 新增檔案
                _ = File.CreateText(path);
            }
            else if (!File.Exists(path))
            {
                // 新增檔案
                _ = File.CreateText(path);
            }
            else
            {
                using StreamReader reader = File.OpenText(path);
                string jsonStr = reader.ReadToEnd();

                if (jsonStr != string.Empty)
                {
                    // 反序列化
                    //List<BaslerCamInfo> infos = JsonSerializer.Deserialize<List<BaslerCamInfo>>(jsonStr);
                    List<BaslerCamInfo> tempList = JsonSerializer.Deserialize<List<BaslerCamInfo>>(jsonStr);

                    //Debug.WriteLine($"{jsonCfgInfo == null} {jsonCfgInfo?.Count} {tempList.Count}");
                    //Debug.WriteLine($"jsonCfgInfo {jsonCfgInfo == null}");
                    //if (jsonCfgInfo != null)
                    //{
                    //    Debug.WriteLine($"{jsonCfgInfo.Count != tempList.Count}");
                    //}

                    // 需要由 Collection Change 來新增

                    // 初始化後就不為 null
                    if (jsonCfgInfo == null || jsonCfgInfo.Count != tempList.Count)
                    {
                        //Debug.WriteLine($"{jsonCfgInfo} {tempList}");

                        // Debug.WriteLine($"{jsonCfgInfo.Count} {tempList.Count}");

                        jsonCfgInfo = JsonSerializer.Deserialize<List<BaslerCamInfo>>(jsonStr);

                        #region 需要 與 CameraEnumer 比較
                        // 當前有連線之相機
                        List<BaslerCamInfo> camsOnLink = MainWindow.CameraEnumer.CamsSource.ToList();

                        // 循環確認是否為已加入使用之相機 (有儲存在 json file 裡)
                        foreach (BaslerCamInfo item in camsOnLink)
                        {
                            if (jsonCfgInfo.Exists(e => e.SerialNumber == item.SerialNumber))
                            {
                                DeviceConfig config = new(item.FullName, item.Model, item.IP, item.MAC, item.SerialNumber)
                                {
                                    VendorName = item.VendorName,
                                    CameraType = item.CameraType
                                };
                                MainWindow.DeviceConfigs.Add(config);
                            }
                        }
                    }
                    #endregion
                }
            }
        }

        /// <summary>
        /// 相機選擇 Combobox Right Click Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CameraSelector_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            (sender as ComboBox).SelectedIndex = -1;
        }

        private void DeviceAdd_Click(object sender, RoutedEventArgs e)
        {
            if (CameraSelector.SelectedItem is BaslerCamInfo info)
            {
                if (!MainWindow.DeviceConfigs.Any(cfg => cfg.SerialNumber == info.SerialNumber))
                {
                    DeviceConfig config = new(info.FullName, info.Model, info.IP, info.MAC, info.SerialNumber)
                    {
                        VendorName = info.VendorName,
                        CameraType = info.CameraType,
                        //DeviceVersion = info.DeviceVersion
                    };

                    //MainWindow.DeviceConfigs.Add(new DeviceConfig(info.FullName, info.Model, info.IP, info.MAC, info.SerialNumber));
                    MainWindow.DeviceConfigs.Add(config);
                }
            }
        }

        /// <summary>
        /// 備份用 (待刪除)
        /// </summary>
        private void FunctionBack()
        {
            BaslerCamInfo info = CameraSelector.SelectedItem as BaslerCamInfo;

            if (info != null)
            {
                Debug.WriteLine($"{info.FullName} {info.Model} {info.IP}");

                Debug.WriteLine($"{info.MAC} {info.SerialNumber}");

                Camera camera = new(info.SerialNumber);

                if (camera.Open(1000, TimeoutHandling.ThrowException))
                {
                    int MaxWidth = (int)camera.Parameters[PLGigECamera.WidthMax].GetValue();
                    int MaxHeight = (int)camera.Parameters[PLGigECamera.HeightMax].GetValue();

                    Debug.WriteLine($"{MaxWidth} {MaxHeight}");
                    //Debug.WriteLine()

                    camera.Parameters[PLGigECamera.GevHeartbeatTimeout].SetValue(1000 * 30);

                    Debug.WriteLine(camera.CameraInfo[CameraInfoKey.FriendlyName]);

                    string str = camera.Parameters[PLGigECamera.UserSetSelector].GetValue();
                    Debug.WriteLine($"當前 UserSet {str}");

                    List<string> strs = camera.Parameters[PLGigECamera.UserSetSelector].GetAllValues().ToList();

                    foreach (string ss in strs)
                    {
                        Debug.WriteLine(ss);
                    }

                    camera.Parameters[PLGigECamera.UserSetSelector].SetValue(PLGigECamera.UserSetSelector.UserSet1);
                    camera.Parameters[PLGigECamera.UserSetLoad].Execute();

                    int width = (int)camera.Parameters[PLGigECamera.Width].GetValue();
                    int height = (int)camera.Parameters[PLGigECamera.Height].GetValue();


                    double fps = camera.Parameters[PLGigECamera.AcquisitionFrameRateAbs].GetValue();
                    double exposure = camera.Parameters[PLGigECamera.ExposureTimeAbs].GetValue();

                    camera.Parameters[PLGigECamera.ExposureMode].GetAllValues();
                    camera.Parameters[PLGigECamera.ExposureMode].SetValue(PLGigECamera.ExposureMode.Off);

                    Debug.WriteLine($"{width} {height}");

                    Debug.WriteLine($"{fps} {exposure}");

                    //camera.Parameters[PLGigECamera.Width].SetValue(2040);
                    //camera.Parameters[PLGigECamera.Height].SetValue(2040);
                    //camera.Parameters[PLGigECamera.UserSetSave].Execute();
                }

                //foreach (DeviceConfig config in MainWindow.DeviceConfigs)
                //{
                //    Debug.WriteLine($"{config.Name}");
                //}

                camera.Close();
            }
        }

        /// <summary>
        /// 清除 Focus 用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
            _ = (Window.GetWindow(this) as MainWindow).TitleGrid.Focus();
        }

        /// <summary>
        /// 變更選中之 Device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            string serialNumber = radioButton.CommandParameter as string;

            //DeviceConfig config = Array.Find(MainWindow.DeviceConfigs.ToArray(), cfg => cfg.SerialNumber == serialNumber);
            DeviceCard.DataContext = Array.Find(MainWindow.DeviceConfigs.ToArray(), cfg => cfg.SerialNumber == serialNumber);
            //MainWindow.DeviceConfigs.IndexOf();

            //Debug.WriteLine((DeviceCard.DataContext as DeviceConfig).VendorName + " VendorName");
            //Debug.WriteLine((DeviceCard.DataContext as DeviceConfig).CameraType + " CameraType");
            //Debug.WriteLine((DeviceCard.DataContext as DeviceConfig).DeviceVersion + " DeviceVersion");
             
        }

        /// <summary>
        /// Camera 組態儲存
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeviceConfigSave_Click(object sender, RoutedEventArgs e)
        {
            string path = $@"{DevicesDirectory}/device.json";
            //string jsonStr = JsonSerializer.Serialize(MainWindow.DeviceConfigs, new JsonSerializerOptions { WriteIndented = true });

            BaslerCamInfo[] infos = MainWindow.DeviceConfigs.Select(item => new BaslerCamInfo()
            {
                VendorName = item.VendorName,
                FullName = item.FullName,
                Model = item.Model,
                SerialNumber = item.SerialNumber,
                CameraType = item.CameraType,
                MAC = item.MAC
            }).ToArray();
            string jsonStr = JsonSerializer.Serialize(infos, new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(path, jsonStr);
        }

        /// <summary>
        /// Radio
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RadioDelButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            string serialNumber = button.CommandParameter as string;

            foreach (DeviceConfig config in MainWindow.DeviceConfigs)
            {
                if (config.SerialNumber == serialNumber)
                {
                    MainWindow.DeviceConfigs.Remove(config);
                    break;
                }
            }
            //Debug.WriteLine($"{button.CommandParameter}");
        }

        /// <summary>
        /// 相機 開啟
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CameraOpen_Click(object sender, RoutedEventArgs e)
        {
            if (DeviceCard?.DataContext != null)
            {
                try
                {
                    DeviceConfig config = DeviceCard.DataContext as DeviceConfig;
                    string serialNumber = config.SerialNumber;

                    MainWindow.BaslerCam.CreateCam(serialNumber);
                    MainWindow.BaslerCam.Camera.CameraOpened += Camera_CameraOpened; // 為了寫 Timeout 設定
                    MainWindow.BaslerCam.Open();
                    MainWindow.BaslerCam.PropertyChange(nameof(MainWindow.BaslerCam.IsOpen));

                    Camera camera = MainWindow.BaslerCam.Camera;

                    // 讀取 camera 的 config
                    ReadConfig(camera, config);
                    // 更新 UserSet Read
                    config.UserSetRead = config.UserSet;
#if false
                    // config.VendorName = camera.CameraInfo[CameraInfoKey.VendorName];
                    // config.CameraType = camera.CameraInfo[CameraInfoKey.DeviceType];
                    config.DeviceVersion = camera.Parameters[PLGigECamera.DeviceVersion].GetValue();
                    config.FirmwareVersion = camera.Parameters[PLGigECamera.DeviceFirmwareVersion].GetValue();

                    // UserSet
                    config.UserSetEnum = camera.Parameters[PLGigECamera.UserSetSelector].GetAllValues().ToArray();
                    config.UserSet = camera.Parameters[PLGigECamera.UserSetSelector].GetValue();

                    // // // // // // // // // // // // // /
                    config.SensorWidth = (int)camera.Parameters[PLGigECamera.SensorWidth].GetValue();
                    config.SensorHeight = (int)camera.Parameters[PLGigECamera.SensorHeight].GetValue();

                    config.MaxWidth = (int)camera.Parameters[PLGigECamera.WidthMax].GetValue();
                    config.MaxHeight = (int)camera.Parameters[PLGigECamera.HeightMax].GetValue();

                    config.Width = (int)camera.Parameters[PLGigECamera.Width].GetValue();
                    config.Height = (int)camera.Parameters[PLGigECamera.Height].GetValue();

                    config.OffsetX = (int)camera.Parameters[PLGigECamera.OffsetX].GetValue();
                    config.OffsetY = (int)camera.Parameters[PLGigECamera.OffsetY].GetValue();
                    // // // // // // // // // // // // // /
                    config.TriggerSelectorEnum = camera.Parameters[PLGigECamera.TriggerSelector].GetAllValues().ToArray();
                    config.TriggerSelector = camera.Parameters[PLGigECamera.TriggerSelector].GetValue();
                    config.TriggerModeEnum = camera.Parameters[PLGigECamera.TriggerMode].GetAllValues().ToArray();
                    config.TriggerMode = camera.Parameters[PLGigECamera.TriggerMode].GetValue();
                    config.TriggerSourceEnum = camera.Parameters[PLGigECamera.TriggerSource].GetAllValues().ToArray();
                    config.TriggerSource = camera.Parameters[PLGigECamera.TriggerSource].GetValue();

                    config.ExposureModeEnum = camera.Parameters[PLGigECamera.ExposureMode].GetAllValues().ToArray();
                    config.ExposureMode = camera.Parameters[PLGigECamera.ExposureMode].GetValue();
                    // // // // // // // // // // // // // /

                    config.ExposureAutoEnum = camera.Parameters[PLGigECamera.ExposureAuto].GetAllValues().ToArray();
                    config.ExposureAuto = camera.Parameters[PLGigECamera.ExposureAuto].GetValue();
                    config.ExposureTime = camera.Parameters[PLGigECamera.ExposureTimeAbs].GetValue();

                    config.FixedFPS = camera.Parameters[PLGigECamera.AcquisitionFrameRateEnable].GetValue();
                    config.FPS = camera.Parameters[PLGigECamera.AcquisitionFrameRateAbs].GetValue();
                    // // // // // // // // // // // // // /  
#endif
                }
                catch (Exception ex)
                {
                    MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.C, ex.Message, MsgInformer.Message.MessageType.Error);
                    //throw;
                }
            }
            else
            {
                Debug.WriteLine($"{DeviceCard.DataContext} : false");
            }
        }

        private void Camera_CameraOpened(object sender, EventArgs e)
        {
            Camera camera = sender as Camera;
            // Timeout 設定 30 秒
            camera.Parameters[PLGigECamera.GevHeartbeatTimeout].SetValue(1000 * 30);
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 相機關閉
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CameraClose_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.BaslerCam.Close();
            MainWindow.BaslerCam.PropertyChange(nameof(MainWindow.BaslerCam.IsOpen));
        }

        /// <summary>
        /// 讀取 Config,
        /// 從相機讀取 Config
        /// </summary>
        /// <param name="camera">來源相機</param>
        /// <param name="config">目標組態</param>
        private static void ReadConfig(Camera camera, DeviceConfig config)
        {
            config.DeviceVersion = camera.Parameters[PLGigECamera.DeviceVersion].GetValue();
            config.FirmwareVersion = camera.Parameters[PLGigECamera.DeviceFirmwareVersion].GetValue();

            // UserSet
            config.UserSetEnum = camera.Parameters[PLGigECamera.UserSetSelector].GetAllValues().ToArray();
            config.UserSet = camera.Parameters[PLGigECamera.UserSetSelector].GetValue();

            // // // // // // // // // // // // // // // // // // // // // // // //
            // int sensorW = (int)camera.Parameters[PLGigECamera.SensorWidth].GetValue();
            // int sensorH = (int)camera.Parameters[PLGigECamera.SensorHeight].GetValue();
            #region AOI Control
            config.SensorWidth = (int)camera.Parameters[PLGigECamera.SensorWidth].GetValue();
            config.SensorHeight = (int)camera.Parameters[PLGigECamera.SensorHeight].GetValue();

            config.MaxWidth = (int)camera.Parameters[PLGigECamera.WidthMax].GetValue();
            config.MaxHeight = (int)camera.Parameters[PLGigECamera.HeightMax].GetValue();

            config.Width = (int)camera.Parameters[PLGigECamera.Width].GetValue();
            config.Height = (int)camera.Parameters[PLGigECamera.Height].GetValue();

            config.OffsetX = (int)camera.Parameters[PLGigECamera.OffsetX].GetValue();
            config.OffsetY = (int)camera.Parameters[PLGigECamera.OffsetY].GetValue();

            config.CenterX = camera.Parameters[PLGigECamera.CenterX].GetValue();    // UserSet 實際上不會記錄
            config.CenterY = camera.Parameters[PLGigECamera.CenterY].GetValue();    // UserSet 實際上不會記錄 
            #endregion

            #region Trigger
            config.TriggerSelectorEnum = camera.Parameters[PLGigECamera.TriggerSelector].GetAllValues().ToArray();
            config.TriggerSelector = camera.Parameters[PLGigECamera.TriggerSelector].GetValue();
            config.TriggerModeEnum = camera.Parameters[PLGigECamera.TriggerMode].GetAllValues().ToArray();
            config.TriggerMode = camera.Parameters[PLGigECamera.TriggerMode].GetValue();
            config.TriggerSourceEnum = camera.Parameters[PLGigECamera.TriggerSource].GetAllValues().ToArray();
            config.TriggerSource = camera.Parameters[PLGigECamera.TriggerSource].GetValue();
            #endregion

            #region Exposure
            config.ExposureModeEnum = camera.Parameters[PLGigECamera.ExposureMode].GetAllValues().ToArray();
            config.ExposureMode = camera.Parameters[PLGigECamera.ExposureMode].GetValue();
            config.ExposureAutoEnum = camera.Parameters[PLGigECamera.ExposureAuto].GetAllValues().ToArray();
            config.ExposureAuto = camera.Parameters[PLGigECamera.ExposureAuto].GetValue();
            config.ExposureTime = camera.Parameters[PLGigECamera.ExposureTimeAbs].GetValue();
            #endregion

            config.FixedFPS = camera.Parameters[PLGigECamera.AcquisitionFrameRateEnable].GetValue();
            config.FPS = camera.Parameters[PLGigECamera.AcquisitionFrameRateAbs].GetValue();
        }

        /// <summary>
        /// 更新 Config
        /// Config 寫入 Camera
        /// </summary>
        /// <param name="config">來源組態</param>
        /// <param name="camera">目標相機</param>
        private static void UpdateConfig(DeviceConfig config, Camera camera)
        {
            try
            {
                camera.Parameters[PLGigECamera.OffsetX].SetToMinimum();
                camera.Parameters[PLGigECamera.OffsetY].SetToMinimum();

                camera.Parameters[PLGigECamera.Width].SetValue(config.Width);
                camera.Parameters[PLGigECamera.Height].SetValue(config.Height);

                //bool b1 =  camera.Parameters[PLGigECamera.OffsetX].TrySetValue(config.OffsetX);
                camera.Parameters[PLGigECamera.OffsetX].SetValue(config.OffsetX);
                //bool b2 =  camera.Parameters[PLGigECamera.OffsetY].TrySetValue(config.OffsetY);
                camera.Parameters[PLGigECamera.OffsetY].SetValue(config.OffsetY);

                //Debug.WriteLine($"OffsetX: { camera.Parameters[PLGigECamera.OffsetX].IsWritable}");
                //Debug.WriteLine($"OffsetY: { camera.Parameters[PLGigECamera.OffsetY].IsWritable}");
                //Debug.WriteLine($"b1: {b1}, b2: {b2}");
#if false
                if (!camera.Parameters[PLGigECamera.OffsetX].TrySetValue(config.OffsetX))
                {
                    //Debug.WriteLine($"Offset X changed");
                }
                else
                {
                    Debug.WriteLine($"Offset X changed");
                }


                if (!camera.Parameters[PLGigECamera.OffsetY].TrySetValue(config.OffsetY))
                {
                    //Debug.WriteLine($"Offset Y changed");
                }
                else
                {
                    Debug.WriteLine($"Offset Y changed");
                } 
#endif
                camera.Parameters[PLGigECamera.CenterX].SetValue(config.CenterX);   // UserSet 不會記錄
                camera.Parameters[PLGigECamera.CenterY].SetValue(config.CenterY);   // UserSet 不會記錄

                camera.Parameters[PLGigECamera.TriggerSelector].SetValue(config.TriggerSelector);
                camera.Parameters[PLGigECamera.TriggerMode].SetValue(config.TriggerMode);
                camera.Parameters[PLGigECamera.TriggerSource].SetValue(config.TriggerSource);

                camera.Parameters[PLGigECamera.ExposureMode].SetValue(config.ExposureMode);
                camera.Parameters[PLGigECamera.ExposureAuto].SetValue(config.ExposureAuto);
                camera.Parameters[PLGigECamera.ExposureTimeAbs].SetValue(config.ExposureTime);

                camera.Parameters[PLGigECamera.AcquisitionFrameRateEnable].SetValue(config.FixedFPS);
                camera.Parameters[PLGigECamera.AcquisitionFrameRateAbs].SetValue(config.FPS);
            }
            catch (ArgumentOutOfRangeException A)
            {
                throw new ArgumentOutOfRangeException($"相機組態寫入失敗: {A.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"相機組態寫入失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private static void SaveConfig()
        {

        }

        /// <summary>
        /// 讀取 UserSet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReadUserSet_Click(object sender, RoutedEventArgs e)
        {
            // Get UsetSet string and read from camera
            string userSet = (DeviceCard.DataContext as DeviceConfig).UserSet;

            DeviceConfig config = DeviceCard.DataContext as DeviceConfig;
            Camera camera = MainWindow.BaslerCam.Camera;

            camera.Parameters[PLGigECamera.UserSetSelector].SetValue(userSet);
            camera.Parameters[PLGigECamera.UserSetLoad].Execute();

            // 讀取 camera 的 config
            ReadConfig(camera, config);
            // 更新 UserSet Read
            config.UserSetRead = userSet;
            // Debug.WriteLine($"{userSet}");
        }

        /// <summary>
        /// 更新 Config
        /// Config 寫入 Camera
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void UpdateUserSet_Click(object sender, RoutedEventArgs e)
        //{
        //    DeviceConfig config = DeviceCard.DataContext as DeviceConfig;
        //    Camera camera = MainWindow.BaslerCam.Camera;

        //    try
        //    {
        //        UpdateConfig(config, camera);
        //    }
        //    catch (Exception ex)
        //    {
        //        // 這邊要修改 (Error 格式怪怪的)
        //        MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.C, ex.Message, MsgInformer.Message.MessageType.Error);
        //    }
        //}

        /// <summary>
        /// 寫入 UserSet (主要儲存至相機)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WriteUserSet_Click(object sender, RoutedEventArgs e)
        {
            DeviceConfig config = DeviceCard.DataContext as DeviceConfig;
            Camera camera = MainWindow.BaslerCam.Camera;

            try
            {
                // 更新 Config
                UpdateConfig(config, camera);
                // UserSet 紀錄
                camera.Parameters[PLGigECamera.UserSetSave].Execute();
            }
            catch (Exception ex)
            {
                // 這邊要修改 (Error 格式有問題)
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.C, ex.Message, MsgInformer.Message.MessageType.Error);
            }

            //camera.Parameters[PLGigECamera.UserSetSave].Execute();
            Debug.WriteLine("Save UserSet");
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            textBox.SelectAll();
        }

        private void TextBox_GotMouseCapture(object sender, MouseEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            textBox.SelectAll();
        }

        // 待移除
        private void OffsetTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            // Tag: MaxWidth or MaxHeight
            textBox.Text = $"{Convert.ToInt32(textBox.Tag) - Convert.ToInt32(textBox.Text)}";
            Debug.WriteLine(textBox.Tag);
        }

   


        //private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    ComboBox comboBox = sender as ComboBox;
        //    Debug.WriteLine($"{comboBox.SelectedItem} {comboBox.SelectedValue}");
        //}
    }
}
