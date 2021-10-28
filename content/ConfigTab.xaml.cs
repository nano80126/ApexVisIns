using System;
using System.Collections.Generic;
using System.Diagnostics;
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


namespace ApexVisIns.content
{
    /// <summary>
    /// ConfigTab.xaml 的互動邏輯
    /// </summary>
    public partial class ConfigTab : StackPanel
    {
        #region Resources

        #endregion

        #region Varibles
        public MainWindow MainWindow { get; set; }
        #endregion


        public ConfigTab()
        {
            InitializeComponent();
        }

        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Config Tab Load");
        }

        private void StackPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Config Tab Unload");
        }

        private void DeviceAdd_Click(object sender, RoutedEventArgs e)
        {
            BaslerCamInfo info = CameraSelector.SelectedItem as BaslerCamInfo;

            if (info != null)
            {
                MainWindow.DeviceConfigs.Add(new DeviceConfig(info.FullName, info.Model, info.IP, info.MAC, info.SerialNumber));

                //MainWindow.DeviceConfigColl.Add(new DeviceConfig(info.FullName, info.Model, info.IP, info.MAC, info.SerialNumber));

                foreach (DeviceConfig config in MainWindow.DeviceConfigs)
                {
                    Debug.WriteLine(config.FullName);
                    Debug.WriteLine(config.Model);
                    Debug.WriteLine("");
                }

                //foreach (DeviceConfig config in MainWindow.DeviceConfigColl)
                //{
                //    Debug.WriteLine(config.FullName);
                //    Debug.WriteLine(config.Model);
                //    Debug.WriteLine("");
                //}
            }
        }

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

            Camera camera = new Camera(serialNumber);

            //camera.CameraInfo[CameraInfoKey.DeviceID];

            Debug.WriteLine($"{(DeviceCard.DataContext as DeviceConfig).FullName}");
            Debug.WriteLine($"{(DeviceCard.DataContext as DeviceConfig).Model}");
            Debug.WriteLine($"User Define Name {camera.CameraInfo[CameraInfoKey.UserDefinedName]}");
            Debug.WriteLine($"Info {camera.CameraInfo[CameraInfoKey.ManufacturerInfo]}");
            Debug.WriteLine($"Vendor Name {camera.CameraInfo[CameraInfoKey.VendorName]}");
            Debug.WriteLine($"Model Name: {camera.CameraInfo[CameraInfoKey.ModelName]}");
            Debug.WriteLine($"Device Ver. {camera.CameraInfo[CameraInfoKey.DeviceVersion]}");
            Debug.WriteLine($"Type {camera.CameraInfo[CameraInfoKey.DeviceType]}");
            //Debug.WriteLine($"Device ID {camera.CameraInfo[CameraInfoKey.DeviceID]}");

            //Debug.WriteLine($"{camera.Parameters[PLGigECamera.DeviceVersion]}");
            //Debug.WriteLine($"{camera.Parameters[PLGigECamera.WidthMax]}");
        }

        private void CameraOpen_Click(object sender, RoutedEventArgs e)
        {
            if (DeviceCard?.DataContext != null)
            {
                try
                {


                    DeviceConfig config = DeviceCard.DataContext as DeviceConfig;
                    string serialNumber = config.SerialNumber;

                    MainWindow.BaslerCam.CreateCam(serialNumber);
                    MainWindow.BaslerCam.Open();

                    Camera camera = MainWindow.BaslerCam.Camera;

                    // UserSet
                    config.UserSetEnum = camera.Parameters[PLGigECamera.UserSetSelector].GetAllValues().ToArray();
                    config.UserSet = camera.Parameters[PLGigECamera.UserSetSelector].GetValue();

                    config.MaxWidth = (int)camera.Parameters[PLGigECamera.WidthMax].GetValue();
                    config.MaxHeight = (int)camera.Parameters[PLGigECamera.HeightMax].GetValue();

                    config.Width = (int)camera.Parameters[PLGigECamera.Width].GetValue();
                    config.Height = (int)camera.Parameters[PLGigECamera.Height].GetValue();

                    config.OffsetX = (int)camera.Parameters[PLGigECamera.OffsetX].GetValue();
                    config.OffsetY = (int)camera.Parameters[PLGigECamera.OffsetY].GetValue();
                    // // // // // // // // // // // // // /

                    config.TriggerModeEnum = camera.Parameters[PLGigECamera.TriggerSelector].GetAllValues().ToArray();
                    config.TriggerSelector = camera.Parameters[PLGigECamera.TriggerSelector].GetValue();
                    config.TriggerModeEnum = camera.Parameters[PLGigECamera.TriggerMode].GetAllValues().ToArray();
                    config.TriggerMode = camera.Parameters[PLGigECamera.TriggerMode].GetValue();
                    config.TriggerSourceEnum = camera.Parameters[PLGigECamera.TriggerSource].GetAllValues().ToArray();
                    config.TriggerSource = camera.Parameters[PLGigECamera.TriggerSource].GetValue();

                    config.ExposureModeEnum = camera.Parameters[PLGigECamera.ExposureMode].GetAllValues().ToArray();
                    config.ExposureMode = camera.Parameters[PLGigECamera.ExposureMode].GetValue();

                    config.ExposureAutoEnum = camera.Parameters[PLGigECamera.ExposureAuto].GetAllValues().ToArray();
                    config.ExposureAuto = camera.Parameters[PLGigECamera.ExposureAuto].GetValue();

                }
                catch (Exception ex)
                {
                    MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.C, ex.Message, MsgInformer.Message.MessageType.Error);
                    throw;
                }
            }
            else
            {
                Debug.WriteLine($"{DeviceCard.DataContext} : false");
            }
        }

        private void CameraClose_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.BaslerCam.Close();
        }
    }
}
