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
using MaterialDesignThemes.Wpf;
using ApexVisIns.content;
using Automation.BDaq;
using System.Diagnostics;
using System.Threading;
using System.Collections;

namespace ApexVisIns.module
{
    /// <summary>
    /// DigitalIOPanel.xaml 的互動邏輯
    /// </summary>
    public partial class DigitalIOPanel : Card
    {
        #region 
        InstantDiCtrl instantDiCtrl;

        //IOController controller = new IOController("DemoDevice,BID#0", true);

        IOController Controller { get; set; }
        #endregion

        /// <summary>
        /// App Element
        /// </summary>
        public MainWindow MainWindow { get; set; }

        /// <summary>
        /// Parent Element
        /// </summary>
        public EngineerTab EngineerTab { get; set; }

        public DigitalIOPanel()
        {
            InitializeComponent();
        }

        private void Card_Loaded(object sender, RoutedEventArgs e)
        {
            Controller = DataContext as IOController;
            //// Controller.InstantDiCtrl

            if (!Controller.DiCtrlCreated)
            {
                Controller.InitializeDiCtrl();
                //Controller.DigitalInputChanged += Controller_DigitalInputChanged; ;
            }

            if (!Controller.DoCtrlCreated)
            {
                Controller.InitializeDoCtrl();
            }
        }

        /// <summary>
        /// 啟用中斷器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InterruptToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (!Controller.InterruptEnabled)
            {
                ErrorCode err = Controller.SetInterrutChannel(0, ActiveSignal.RisingEdge);
                Debug.WriteLine($"{err}");
                err = Controller.SetInterrutChannel(8, ActiveSignal.RisingEdge);
                Debug.WriteLine($"{err}");
                Controller.DigitalInputChanged += Controller_DigitalInputChanged;
                err = Controller.EnableInterrut();
                Debug.WriteLine($"{err}");
            }
        }

        /// <summary>
        /// 關閉中斷器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InterruptToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (Controller.InterruptEnabled)
            {
                Controller.DisableInterrupt();

                ErrorCode err = Controller.SetInterrutChannel(0, ActiveSignal.RisingEdge, false);
                Debug.WriteLine($"{err}");
                err = Controller.SetInterrutChannel(8, ActiveSignal.RisingEdge, false);
                Debug.WriteLine($"{err}");
                Controller.DigitalInputChanged -= Controller_DigitalInputChanged;
                err = Controller.DisableInterrupt();
                Debug.WriteLine($"{err}");
            }
        }
        private void Controller_DigitalInputChanged(object sender, IOController.DigitalInputChangedEventArgs e)
        {
            Debug.WriteLine($"{e.Port} {e.Bit} {e.Data}");
            //throw new NotImplementedException();
        }

        /// <summary>
        /// Di 變更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DoSetButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            byte[] objs = button.CommandParameter as byte[];
            // 當前值: (bool)button.Tag
            // 目標 Port: objs[0] 
            // 目標 Bit: objs[1]
            Controller.WriteDOBit(objs[0], objs[1], !(bool)button.Tag);
        }

        private void Test()
        {
            // Description => Set in Navigator
            instantDiCtrl = new()
            {
                SelectedDevice = new DeviceInformation("DemoDevice,BID#0")
            };

            Debug.WriteLine($"Port Count: {instantDiCtrl.PortCount}\r\n-----");

            // foreach (DioPort item in instantDiCtrl.Ports)
            // {
            //      Debug.WriteLine($"{item.DiInversePort}, {item.DirectionMask}, {item.Port}");
            // }

            Debug.WriteLine("=====================================================================");

            // PortCount x 8 = ChannelCountMax
            Debug.WriteLine($"channel Count Max: {instantDiCtrl.Features.ChannelCountMax}, {instantDiCtrl.Features.PortCount}\r\n------");

            Debug.WriteLine($"Port Programmable: {instantDiCtrl.Features.PortProgrammable}, {string.Join('|', instantDiCtrl.Features.PortsType)}");

            Debug.WriteLine($"DiSupport: {instantDiCtrl.Features.DiSupported}, {string.Join('|', instantDiCtrl.Features.DiDataMask)}");

            Debug.WriteLine($"DiNoiseFilterSupported: {instantDiCtrl.Features.DiNoiseFilterSupported}, {instantDiCtrl.Features.DiintSupported}");

            Debug.WriteLine($"DiintOfChannels: {string.Join('|', instantDiCtrl.Features.DiintOfChannels)}, {string.Join('|', instantDiCtrl.Features.DiintTriggerEdges)}");

            foreach (DeviceTreeNode item in instantDiCtrl.SupportedDevices)
            {
                Debug.WriteLine($"{item.Description}, {item.DeviceNumber}, {string.Join('|', item.ModulesIndex)}");
            }

            DiintChannel[] channels = instantDiCtrl.DiintChannels;

            if (channels != null)
            {
                foreach (DiintChannel ch in channels)
                {
                    Debug.WriteLine($"{ch.Channel} {ch.Enabled} {ch.Gated}");
                }
            }
        }

        private void DiRead_Click(object sender, RoutedEventArgs e)
        {
            ErrorCode err = Controller.ReadDI(0);
            Debug.WriteLine($"ErrorCode: {err}");
            err = Controller.ReadDI(1);
            Debug.WriteLine($"ErrorCode: {err}");
        }

        private void DoRead_Click(object sender, RoutedEventArgs e)
        {
            ErrorCode err = Controller.ReadDO(0);
            Debug.WriteLine($"ErrorCode: {err}");
            err = Controller.ReadDO(1);
            Debug.WriteLine($"ErrorCode: {err}");
        }

        private void DoWrite_Click(object sender, RoutedEventArgs e)
        {
            #region MyRegion
            //Controller.WriteDO(0, 0b10100110);
            //Controller.WriteDO(1, 0b00111001); 
            #endregion
            Controller.TriggerEvent();
        }

        private void SwitchInterrupt_Click(object sender, RoutedEventArgs e)
        {
            if (!Controller.InterruptEnabled)
            {
                //Controller.DisableInterrupt();

                ErrorCode err = Controller.SetInterrutChannel(0, ActiveSignal.RisingEdge);
                Debug.WriteLine($"{err}");
                err = Controller.SetInterrutChannel(8, ActiveSignal.RisingEdge);
                Debug.WriteLine($"{err}");

                err = Controller.EnableInterrut();
                Debug.WriteLine($"{err}");
            }
            else
            {
                Controller.DisableInterrupt();

                ErrorCode err = Controller.SetInterrutChannel(0, ActiveSignal.RisingEdge, false);
                Debug.WriteLine($"{err}");
                err = Controller.SetInterrutChannel(8, ActiveSignal.RisingEdge, false);
                Debug.WriteLine($"{err}");

                err = Controller.DisableInterrupt();
                Debug.WriteLine($"{err}");
            }
        }
    }
}
