﻿using Basler.Pylon;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ApexVisIns.content
{
    /// <summary>
    /// CameraTab.xaml 的互動邏輯
    /// </summary>
    public partial class CameraTab : StackPanel
    {
        #region Resources

        #endregion

        #region Varibles
        /// <summary>
        /// Camera 組態路徑, Camera Configs Directory
        /// </summary>
        private string CamerasDirectory { get; } = @"cameras";
        private string CamerasPath { get; } = @"camera.json";

        /// <summary>
        /// Cameras for CameraTab, only useing in this tab. 
        /// </summary>
        private readonly List<BaslerCam> _camerasList = new();
        /// <summary>
        /// Index of DeivceCam in use
        /// </summary>
        private int _devInUse = -1;
        #endregion

        #region Properties
        /// <summary>
        /// 主視窗物件
        /// </summary>
        public MainWindow MainWindow { get; set; }
        /// <summary>
        /// Informer 物件
        /// </summary>
        //public MsgInformer MsgInformer { get; set; }
        #endregion

        #region Flags
        /// <summary>
        /// 已載入旗標
        /// </summary>
        private bool loaded;
        #endregion

        public CameraTab()
        {
            InitializeComponent();

            MainWindow = (MainWindow)Application.Current.MainWindow;
            // 初始化路徑
            InitCamerasConfigPath();
        }

        /// <summary>
        /// Config Tab Load 事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            // 載入 cameras config
            LoadCamerasConfig();

            if (!loaded)
            {
                MainWindow.MsgInformer?.AddInfo(MsgInformer.Message.MsgCode.APP, "裝置組態頁面已載入");
                loaded = true;
            }
        }

        /// <summary>
        /// Config Tab Unload 事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StackPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            // 取消 Collection 變更事件
            // if (EventHasBound)  // 確認已綁定
            // {
            //     MainWindow.CameraEnumer.CamsSource.CollectionChanged -= CamsSource_CollectionChanged;
            //     EventHasBound = false;
            // }
        }

        /// <summary>
        /// 初始化 cameras Config 路徑
        /// </summary>
        private void InitCamerasConfigPath()
        {
            string directory = $@"{Directory.GetCurrentDirectory()}\{CamerasDirectory}";
            string path = $@"{directory}\{CamerasPath}";

            if (!Directory.Exists(directory))
            {
                // 新增路徑
                _ = Directory.CreateDirectory(directory);
                // 新增檔案
                _ = File.CreateText(path);
            }
            else if (!File.Exists(path))
            {
                // 新增檔案
                _ = File.CreateText(path);
            }
        }

        /// <summary>
        /// 載入 camera.json
        /// </summary>
        private void LoadCamerasConfig()
        {
            string path = $@"{Directory.GetCurrentDirectory()}\{CamerasDirectory}\{CamerasPath}";
            ////return;

            //Debug.WriteLine($"dir: {directory}");
            //Debug.WriteLine($"path: {path}");

            //if (!Directory.Exists(directory))
            //{
            //    // 新增路徑
            //    _ = Directory.CreateDirectory(directory);
            //    // 新增檔案
            //    _ = File.CreateText(path);
            //}
            //else if (!File.Exists(path))
            //{
            //    // 新增檔案
            //    _ = File.CreateText(path);
            //}
            //else
            //{
            using StreamReader reader = File.OpenText(path);
            string jsonStr = reader.ReadToEnd();

            if (jsonStr != string.Empty)
            {
                // 反序列化，載入JSON FILE
                CameraConfigBase[] cameras = JsonSerializer.Deserialize<CameraConfigBase[]>(jsonStr);

                // 目前有連線的相機
                BaslerCamInfo[] cams = MainWindow?.CameraEnumer.CamsSource.ToArray();

                // JSON FILE 儲存之 CameraConfig
                CameraConfig[] cameraConfig = MainWindow?.CameraEnumer.CameraConfigs.ToArray();

                if (cameras.Length > cameraConfig.Length)
                {
                    foreach (CameraConfigBase d in cameras)
                    {
                        if (!cameraConfig.Any(e => e.SerialNumber == d.SerialNumber))
                        {
                            CameraConfig config = new(d.FullName, d.Model, d.IP, d.MAC, d.SerialNumber)
                            {
                                VendorName = d.VendorName,
                                CameraType = d.CameraType,
                                TargetFeature = d.TargetFeature,
                                // 
                                Online = cams.Length > 0 && cams.Any(e => e.SerialNumber == d.SerialNumber)
                            };
                            MainWindow?.CameraEnumer.CameraConfigs.Add(config);
                        }
                    }
                }
            }
            else
            {
                MainWindow.MsgInformer?.AddInfo(MsgInformer.Message.MsgCode.CAMERA, "相機設定檔為空");
            }
            //}
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
        private void CameraAdd_Click(object sender, RoutedEventArgs e)
        {
            if (CameraSelector.SelectedItem is BaslerCamInfo info)
            {
                ObservableCollection<CameraConfig> cameraConfigs = MainWindow.CameraEnumer.CameraConfigs;

                if (!cameraConfigs.Any(cfg => cfg.SerialNumber == info.SerialNumber))
                {
                    CameraConfig config = new(info.FullName, info.Model, info.IP, info.MAC, info.SerialNumber)
                    {
                        VendorName = info.VendorName,
                        CameraType = info.CameraType,
                        Online = true,
                        // TargetFeature = 0
                    };

                    cameraConfigs.Add(config);
                }
                cameraConfigs = null;   // 
                // CameraConfigSaved Flag set false
                MainWindow.CameraEnumer.CameraCofingSaved = false;
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
        /// 變更選中之 Camera
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            string serialNumber = radioButton.CommandParameter as string;

            int idx = Array.FindIndex(MainWindow.CameraEnumer.CameraConfigs.ToArray(), cfg => cfg.SerialNumber == serialNumber);

            if (idx > -1)
            {
                CameraCard.DataContext = MainWindow.CameraEnumer.CameraConfigs[idx];

                if (MainWindow.CameraEnumer.CameraConfigs[idx].Online)
                {
                    // serialNumber 已經在列表中
                    if (_camerasList.Exists(e => e.SerialNumber == serialNumber))
                    {
                        _devInUse = _camerasList.FindIndex(0, _camerasList.Count, e => e.SerialNumber == serialNumber);
                        #region 變更 DataContext
                        CameraStatusBorder.DataContext = _camerasList[_devInUse];
                        CameraOpen.DataContext = _camerasList[_devInUse];
                        CameraClose.DataContext = _camerasList[_devInUse];
                        UserSetActionPanel.DataContext = _camerasList[_devInUse];
                        #endregion
                    }
                    else // 不在列表中，新增一台新物件
                    {
                        BaslerCam baslerCam = new()
                        {
                            ConfigName = "Default",
                            Config = new BaslerConfig("Default"),
                            SerialNumber = serialNumber
                        };
                        _camerasList.Add(baslerCam);

                        #region 變更 DataContext
                        CameraStatusBorder.DataContext = baslerCam;
                        CameraOpen.DataContext = baslerCam;
                        CameraClose.DataContext = baslerCam;
                        UserSetActionPanel.DataContext = baslerCam;
                        #endregion

                        _devInUse = _camerasList.IndexOf(baslerCam);
                    }
                }
                else
                {
                    #region 移除 DataContext
                    CameraStatusBorder.DataContext = null;
                    CameraOpen.DataContext = null;
                    CameraClose.DataContext = null;
                    UserSetActionPanel.DataContext = null;
                    #endregion
                }
            }
        }

        /// <summary>
        /// CameraCard DataContext Changed 事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CameraCard_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            MainWindow.CameraEnumer.CameraCofingSaved = false;
        }

        /// <summary>
        /// Camera 組態儲存
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CameraConfigSave_Click(object sender, RoutedEventArgs e)
        {
            string path = $@"{Directory.GetCurrentDirectory()}\{CamerasDirectory}\{CamerasPath}";

            CameraConfigBase[] infos = MainWindow.CameraEnumer.CameraConfigs.Select(item => new CameraConfigBase()
            {
                VendorName = item.VendorName,
                FullName = item.FullName,
                Model = item.Model,
                SerialNumber = item.SerialNumber,
                CameraType = item.CameraType,
                IP = item.IP,
                MAC = item.MAC,
                TargetFeature = item.TargetFeature
            }).ToArray();

            string jsonStr = JsonSerializer.Serialize(infos, new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(path, jsonStr);
            MainWindow.CameraEnumer.CameraCofingSaved = true;
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

            foreach (CameraConfig config in MainWindow.CameraEnumer.CameraConfigs)
            {
                if (config.SerialNumber == serialNumber)
                {
                    MainWindow.CameraEnumer.CameraConfigs.Remove(config);
                    break;
                }
            }
            // Debug.WriteLine($"{button.CommandParameter}");
        }

        /// <summary>
        /// 相機開啟
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CameraOpen_Click(object sender, RoutedEventArgs e)
        {
            if (CameraCard?.DataContext != null)
            {
                try
                {
                    CameraConfig config = CameraCard.DataContext as CameraConfig;
                    string serialNumber = config.SerialNumber;
                    BaslerCam baslerCam = _camerasList[_devInUse];

                    // 優化 UX，不會卡住 UI 執行緒 (測試中)
                    string res = await Task.Run(() =>
                    {
                        try
                        {
                            //MainWindow.BaslerCam.CreateCam(serialNumber);
                            //MainWindow.BaslerCam.Camera.CameraOpened += Camera_CameraOpened; // 為了寫 Timeout 設定
                            //MainWindow.BaslerCam.Open();
                            //MainWindow.BaslerCam.PropertyChange(nameof(MainWindow.BaslerCam.IsOpen));

                            //Camera camera = MainWindow.BaslerCam.Camera;
                            /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// 

                            baslerCam.CreateCam(serialNumber);
                            baslerCam.Camera.CameraOpened += Camera_CameraOpened; // 為了寫 Timeout 設定
                            baslerCam.Open();
                            baslerCam.PropertyChange(nameof(baslerCam.IsOpen));

                            Camera camera = baslerCam.Camera;

                            // 讀取 camera 的 config
                            ReadConfig(camera, config);
                            // 更新 UserSet Read
                            config.UserSetRead = config.UserSet;

                            return string.Empty;
                        }
                        catch (Exception ex)
                        {
                            return ex.Message;
                        }
                    });

                    if (res != string.Empty)
                    {
                        throw new Exception(res);
                    }
                }
                catch (Exception ex)
                {
                    MainWindow.MsgInformer?.AddError(MsgInformer.Message.MsgCode.CAMERA, ex.Message);
                }
            }
            //else
            //{
            //    Debug.WriteLine($"{CameraCard.DataContext} : false");
            //}
        }

        /// <summary>
        /// 相機開啟事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Camera_CameraOpened(object sender, EventArgs e)
        {
            Camera camera = sender as Camera;
            // 斷線 Timeout 設定 30 秒
            camera.Parameters[PLGigECamera.GevHeartbeatTimeout].SetValue(1000 * 30);
        }

        /// <summary>
        /// 相機關閉
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CameraClose_Click(object sender, RoutedEventArgs e)
        {
            BaslerCam baslerCam = _camerasList[_devInUse];

            //MainWindow.BaslerCam.Close();
            //MainWindow.BaslerCam.PropertyChange(nameof(MainWindow.BaslerCam.IsOpen));
            /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// 
            baslerCam.Close();
            baslerCam.PropertyChange(nameof(baslerCam.IsOpen));
        }

        /// <summary>
        /// 讀取 Config,
        /// 從相機讀取 Config
        /// </summary>
        /// <param name="camera">來源相機</param>
        /// <param name="config">目標組態</param>
        private static void ReadConfig(Camera camera, CameraConfig config)
        {
            try
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

                #region FPS
                config.FixedFPS = camera.Parameters[PLGigECamera.AcquisitionFrameRateEnable].GetValue();
                config.FPS = camera.Parameters[PLGigECamera.AcquisitionFrameRateAbs].GetValue();
                #endregion

                #region Anglog Control
                config.GainAutoEnum = camera.Parameters[PLGigECamera.GainAuto].GetAllValues().ToArray();
                config.GainAuto = camera.Parameters[PLGigECamera.GainAuto].GetValue();
                config.Gain = (int)camera.Parameters[PLGigECamera.GainRaw].GetValue();
                config.BlackLevel = (int)camera.Parameters[PLGigECamera.BlackLevelRaw].GetValue();
                config.GammaEnable = camera.Parameters[PLGigECamera.GammaEnable].GetValue();
                config.GammaSelectorEnum = camera.Parameters[PLGigECamera.GammaSelector].GetAllValues().ToArray();

                //Debug.WriteLine($"{camera.Parameters[PLGigECamera.GammaEnable].GetValue()}");
                //Debug.WriteLine($"{string.Join(",", camera.Parameters[PLGigECamera.GammaSelector].GetAllValues().ToArray())}");

                if (config.GammaSelectorEnum.Length > 0)
                {
                    config.GammaSelector = camera.Parameters[PLGigECamera.GammaSelector].GetValue();
                }
                else
                {
                    config.GammaSelector = string.Empty;
                }
                config.Gamma = camera.Parameters[PLGigECamera.Gamma].GetValue();
                #endregion

                string userSet = camera.Parameters[PLGigECamera.UserSetDefaultSelector].GetValue();
                Debug.WriteLine($"{userSet}");
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 更新 Config
        /// Config 寫入 Camera
        /// </summary>
        /// <param name="config">來源組態</param>
        /// <param name="camera">目標相機</param>
        private static void UpdateConfig(CameraConfig config, Camera camera)
        {
            try
            {
                camera.Parameters[PLGigECamera.CenterX].SetValue(false);
                camera.Parameters[PLGigECamera.CenterY].SetValue(false);

                camera.Parameters[PLGigECamera.OffsetX].SetToMinimum();
                camera.Parameters[PLGigECamera.OffsetY].SetToMinimum();

                camera.Parameters[PLGigECamera.Width].SetValue(config.Width);
                camera.Parameters[PLGigECamera.Height].SetValue(config.Height);

                // bool b1 =  camera.Parameters[PLGigECamera.OffsetX].TrySetValue(config.OffsetX);
                // bool b2 =  camera.Parameters[PLGigECamera.OffsetY].TrySetValue(config.OffsetY);
                camera.Parameters[PLGigECamera.OffsetX].SetValue(config.OffsetX);
                camera.Parameters[PLGigECamera.OffsetY].SetValue(config.OffsetY);

                // camera.Parameters[PLGigECamera.CenterX].SetValue(config.CenterX);   // UserSet 不會記錄
                // camera.Parameters[PLGigECamera.CenterY].SetValue(config.CenterY);   // UserSet 不會記錄

                camera.Parameters[PLGigECamera.TriggerSelector].SetValue(config.TriggerSelector);
                camera.Parameters[PLGigECamera.TriggerMode].SetValue(config.TriggerMode);
                camera.Parameters[PLGigECamera.TriggerSource].SetValue(config.TriggerSource);

                camera.Parameters[PLGigECamera.ExposureMode].SetValue(config.ExposureMode);
                camera.Parameters[PLGigECamera.ExposureAuto].SetValue(config.ExposureAuto);
                camera.Parameters[PLGigECamera.ExposureTimeAbs].SetValue(config.ExposureTime);

                camera.Parameters[PLGigECamera.AcquisitionFrameRateEnable].SetValue(config.FixedFPS);
                camera.Parameters[PLGigECamera.AcquisitionFrameRateAbs].SetValue(config.FPS);

                camera.Parameters[PLGigECamera.GainAuto].SetValue(config.GainAuto);
                // 是否標示上下限 ?
                camera.Parameters[PLGigECamera.GainRaw].SetValue(config.Gain);
                // 是否標示上下限 ?
                camera.Parameters[PLGigECamera.BlackLevelRaw].SetValue(config.BlackLevel);
                camera.Parameters[PLGigECamera.GammaEnable].SetValue(config.GammaEnable);
                camera.Parameters[PLGigECamera.GammaSelector].SetValue(config.GammaSelector);
                // 是否標示上下限 ?
                camera.Parameters[PLGigECamera.Gamma].SetValue(config.Gamma);

            }
            //catch (ArgumentOutOfRangeException A)
            //{
            //    throw new ArgumentOutOfRangeException($"相機組態寫入失敗: {A.Message}");
            //}
            catch (Exception)
            {
                //throw new Exception($"相機組態寫入失敗: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 待刪除
        /// </summary>
        private static void SaveConfig() { }

        /// <summary>
        /// 讀取 UserSet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReadUserSet_Click(object sender, RoutedEventArgs e)
        {
            // Get UsetSet string and read from camera
            string userSet = (CameraCard.DataContext as CameraConfig).UserSet;

            CameraConfig config = CameraCard.DataContext as CameraConfig;
            //Camera camera = MainWindow.BaslerCam.Camera;
            Camera camera = _camerasList[_devInUse].Camera;

            camera.Parameters[PLGigECamera.UserSetSelector].SetValue(userSet);
            camera.Parameters[PLGigECamera.UserSetLoad].Execute();

            // 讀取 camera 的 config
            ReadConfig(camera, config);
            // 更新 UserSet Read
            config.UserSetRead = userSet;
            // Debug.WriteLine($"{userSet}");
        }

        /// <summary>
        /// 設為 預設 UserSet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetDefaultUserSet_Click(object sender, RoutedEventArgs e)
        {
            CameraConfig config = CameraCard.DataContext as CameraConfig;
            //Camera camera = MainWindow.BaslerCam.Camera;
            Camera camera = _camerasList[_devInUse].Camera;

            camera.Parameters[PLGigECamera.UserSetDefaultSelector].SetValue(config.UserSet);
        }

        /// <summary>
        /// 寫入 UserSet (主要儲存至相機)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WriteUserSet_Click(object sender, RoutedEventArgs e)
        {
            CameraConfig config = CameraCard.DataContext as CameraConfig;
            //Camera camera = MainWindow.BaslerCam.Camera;
            Camera camera = _camerasList[_devInUse].Camera;

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
                MainWindow.MsgInformer?.AddError(MsgInformer.Message.MsgCode.CAMERA, ex.Message);
            }
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
        /// 目標特徵變更 ComboBox Selection Changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TargetFeatureCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //ComboBox combobox = sender as ComboBox;
            //Debug.WriteLine($"{combobox.SelectedIndex} {combobox.SelectedItem}");
            MainWindow.CameraEnumer.CameraCofingSaved = false;
        }

        //private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    ComboBox comboBox = sender as ComboBox;
        //    Debug.WriteLine($"{comboBox.SelectedItem} {comboBox.SelectedValue}");
        //}
    }
}
