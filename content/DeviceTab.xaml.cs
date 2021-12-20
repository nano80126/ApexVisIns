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
    /// DeviceTab.xaml 的互動邏輯
    /// </summary>
    public partial class DeviceTab : StackPanel
    {
        #region Resources

        #endregion

        #region Varibles
        /// <summary>
        /// CamsSource.CollectionChanged Evnet has bound flag
        /// </summary>
        private bool EventHasBound;
        /// <summary>
        /// Device Configs Directory
        /// </summary>
        private string DevicesDirectory { get; } = @"./devices";
        public MainWindow MainWindow { get; set; }
        #endregion

        public DeviceTab()
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
            #region 綁定事件、載入 Configs
            // 綁定 Collection 變更事件
            if (!EventHasBound) // 避免重複綁定
            {
                MainWindow.CameraEnumer.CamsSource.CollectionChanged += CamsSource_CollectionChanged;
                EventHasBound = true;
            }
            // 載入
            LoadDeviceConfigs();
            #endregion
            MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.APP, "裝置組態頁面已載入");
        }

        /// <summary>
        /// Config Tab Unload 事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StackPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            #region 保留，確認無用途則刪除
            // 取消 Collection 變更事件
            if (EventHasBound)  // 確認已綁定
            {
                MainWindow.CameraEnumer.CamsSource.CollectionChanged -= CamsSource_CollectionChanged;
                EventHasBound = false;
            }
            #endregion
        }

        /// <summary>
        /// MainWindow.CamsSource Collection 變更事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CamsSource_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // 三個動作
            // 1. 新增 => MainWindow.DeviceConfigs Config.Online 設為 True
            // 2. 移除 => MainWindow.DeviceConfigs Config.Online 設為 Off
            // 3. 清空 => MainWindow.DeviceConfigs Online 全部設為 Off

            List<BaslerCamInfo> list;
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:  // Add();
                    // 取得已連線相機 List
                    list = (sender as ObservableCollection<BaslerCamInfo>).ToList();
                    //foreach (BaslerCamInfo item in list)
                    //{
                    //    if (jsonCfgInfo.Exists(e => e.SerialNumber == item.SerialNumber))
                    //    {
                    //        DeviceConfig config = new(item.FullName, item.Model, item.IP, item.MAC, item.SerialNumber)
                    //        {
                    //            VendorName = item.VendorName,
                    //            CameraType = item.CameraType,
                    //            Online = true   // 標記為 "在線"
                    //        };
                    //        // 不應該有新增事件
                    //        Dispatcher.Invoke(() => MainWindow.DeviceConfigs.Add(config));
                    //    }
                    //}
                    // 循環比較，標記已連線之相機
                    foreach (DeviceConfig device in MainWindow.DeviceConfigs)
                    {
                        if (list.Any(item => item.SerialNumber == device.SerialNumber))
                        {
                            Dispatcher.Invoke(() => device.Online = true);
                            break; // 測試是否有新增複數的可能
                        }
                    }

                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:   // Remove();
                    // 取得已連線相機 LIst
                    list = (sender as ObservableCollection<BaslerCamInfo>).ToList();
                    // 循環比較，標記已斷線之相機
                    foreach (DeviceConfig cfg in MainWindow.DeviceConfigs)
                    {
                        if (!list.Any(info => info.SerialNumber == cfg.SerialNumber))
                        {
                            Dispatcher.Invoke(() => cfg.Online = false);
                            break;
                        }
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:    // Clear();
                    // 全部設為 Offline
                    Dispatcher.Invoke(() =>
                    {
                        foreach (DeviceConfig config in MainWindow.DeviceConfigs)
                        {
                            config.Online = false;
                        }
                    });
                    break;
            }
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
                    BaslerCamInfo[] devices = JsonSerializer.Deserialize<BaslerCamInfo[]>(jsonStr);

                    List<BaslerCamInfo> cams = MainWindow.CameraEnumer.CamsSource.ToList();

                    // 還是需要 和 MainWindow.CameraEnumer.CamsSource 比較
                    if (devices.Length > MainWindow.DeviceConfigs.Count)
                    {
                        foreach (BaslerCamInfo d in devices)
                        {
                            // 判斷 Json Config 尚未新增進 
                            if (!MainWindow.DeviceConfigs.Any(e => e.SerialNumber == d.SerialNumber))
                            {
                                DeviceConfig config = new(d.FullName, d.Model, d.IP, d.MAC, d.SerialNumber)
                                {
                                    VendorName = d.VendorName,
                                    CameraType = d.CameraType,
                                    //Online = false
                                    // CameraEnumer CamsSource 有物件且有被新增過
                                    Online = cams.Count > 0 && cams.Exists(e => e.SerialNumber == d.SerialNumber)
                                };
                                MainWindow.DeviceConfigs.Add(config);
                            }
                        }
                    }

                    // 初始化後就不為 null, 之後由 DeviceConfigs Collection Change 來尋找交集
                    // 待移除
#if false
                    if (jsonCfgInfo == null || jsonCfgInfo.Count != tempList.Count)
                    {
                        jsonCfgInfo = JsonSerializer.Deserialize<List<BaslerCamInfo>>(jsonStr);
#if false
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
                    #endregion
#endif
                    } 
#endif
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

        /// <summary>
        /// 相機新增至列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

            int idx = Array.FindIndex(MainWindow.DeviceConfigs.ToArray(), cfg => cfg.SerialNumber == serialNumber);

            //DeviceCard.DataContext = Array.Find(MainWindow.DeviceConfigs.ToArray(), cfg => cfg.SerialNumber == serialNumber);
            DeviceCard.DataContext = MainWindow.DeviceConfigs[idx];
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
        /// 刪除列表元素
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
        /// 相機開啟
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CameraOpen_Click(object sender, RoutedEventArgs e)
        {
            if (DeviceCard?.DataContext != null)
            {
                try
                {
                    DeviceConfig config = DeviceCard.DataContext as DeviceConfig;
                    string serialNumber = config.SerialNumber;

                    // 優化 UX，不會卡住 UI 執行緒 (測試中)
                    await Task.Run(() =>
                    {
                        MainWindow.BaslerCam.CreateCam(serialNumber);
                        MainWindow.BaslerCam.Camera.CameraOpened += Camera_CameraOpened; // 為了寫 Timeout 設定
                        MainWindow.BaslerCam.Open();
                        MainWindow.BaslerCam.PropertyChange(nameof(MainWindow.BaslerCam.IsOpen));

                        Camera camera = MainWindow.BaslerCam.Camera;

                        // 讀取 camera 的 config
                        ReadConfig(camera, config);
                        // 更新 UserSet Read
                        config.UserSetRead = config.UserSet;
                    });
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
                    MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.CAMERA, ex.Message);
                    //throw;
                }
            }
            else
            {
                Debug.WriteLine($"{DeviceCard.DataContext} : false");
            }
        }

        /// <summary>
        /// 相機開啟事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
            //config.IP = camera.Parameters[CameraInfoKey]
            // 更新 IP
            config.IP = camera.CameraInfo[CameraInfoKey.DeviceIpAddress];
            config.PropertyChange(nameof(config.IP)); // 由於 IP 在 BaslerCamInfo 裡，內部不會觸發 IP PropertyChanged
            //Debug.WriteLine($"{config.DeviceVersion} {config.FirmwareVersion}");

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
        /// 待刪除
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
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.CAMERA, ex.Message);
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


        /// <summary>
        /// 待刪除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine($"{(sender as ComboBox).SelectedItem}");
        }

        //private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    ComboBox comboBox = sender as ComboBox;
        //    Debug.WriteLine($"{comboBox.SelectedItem} {comboBox.SelectedValue}");
        //}
    }
}
