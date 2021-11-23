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
        //InstantDiCtrl instantDiCtrl;

        //IOController controller = new IOController("DemoDevice,BID#0", true);
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

        private void Test()
        {
            #if false
            // Description => Set in Navigator
            instantDiCtrl = new()
            {
                SelectedDevice = new DeviceInformation("DemoDevice,BID#0")
            };

            Debug.WriteLine($"Port Count: {instantDiCtrl.PortCount}\r\n-----");

            // foreach (DioPort item in instantDiCtrl.Ports)
            // {
            //     Debug.WriteLine($"{item.DiInversePort}, {item.DirectionMask}, {item.Port}");
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
            #endif
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Test();


            return;
            #if false
            instantDiCtrl = new()
            {
                SelectedDevice = new DeviceInformation("DemoDevice,BID#0")
            };


            _ = Task.Run(() =>
            {
                for (int i = 0; i < 8; i++)
                {
                    ReadDigitalInput();

                    SpinWait.SpinUntil(() => false, 100);
                }
            }); 
            #endif
        }

        private void ReadDigitalInput()
        {
#if false
            ErrorCode err;

            // for (int i = 0; i < instantDiCtrl.Features.PortCount; i++)
            // {
            err = instantDiCtrl.Read(0, out byte portData);
            if (err != ErrorCode.Success)
            {
                Debug.WriteLine(err);
                return;
            }

            Debug.WriteLine($"Port{0} : {portData.ToString("X2")} {portData}");
            err = instantDiCtrl.Read(1, out portData);
            if (err != ErrorCode.Success)
            {
                Debug.WriteLine(err);
                return;
            }

            Debug.WriteLine($"Port{1} : {portData.ToString("X2")} {portData}");

            //instantDiCtrl.ReadBit(0, 0, out byte data);
            //Debug.WriteLine($"Port{0} Bit0 : {data}");
            //instantDiCtrl.ReadBit(i, 1, out data);
            //Debug.WriteLine($"Port{i} Bit1 : {data}");
            //}  
#endif
        }

        private void Button_Click1(object sender, RoutedEventArgs e)
        {
            //BitArray arr = new BitArray(8, false);
            //BitArray arr2 = new BitArray(new bool[] { false, true });
            //BitArray arr3 = new BitArray(new byte[] { 129 });
            //BitArray arr4 = new BitArray(new int[] { 129 });

            ////arr.And(new BitArray(new int[] { 128 }));

            //Debug.WriteLine($"{arr.Length} {arr} {arr[0]} {arr[1]}");
            //Debug.WriteLine($"{arr2.Length} {arr2} {arr2[0]} {arr2[1]}");
            //Debug.WriteLine($"{arr3.Length} {arr3} {arr3[0]} {arr3[1]} {arr3[7]}");
            //Debug.WriteLine($"{arr4.Length} {arr4} {arr4[0]} {arr4[1]} {arr4[7]}");

#if false
            Debug.WriteLine($"{controller.DiArray0[0]} {controller.DiArray0[1]}");
            Debug.WriteLine($"{controller.DiArray1[0]} {controller.DiArray1[1]}");

            controller.Read();

            Debug.WriteLine($"{controller.DiArray0[0]} {controller.DiArray0[1]}");
            Debug.WriteLine($"{controller.DiArray1[0]} {controller.DiArray1[1]}");
#endif

            //Debug.WriteLine($"{controller.Read()}");

            //Debug.WriteLine(arr.Get(0));
            //Debug.WriteLine(arr.Get(1));
            //Debug.WriteLine(arr.Get(2));
            //Debug.WriteLine(arr.Get(3));
            //Debug.WriteLine(arr.Get(4));
        }

        //InstantDiCtrl InstantDiCtrl = new InstantDiCtrl()
        //{
        //    //SelectedDevice = new DeviceInformation();
        //}

    }
}
