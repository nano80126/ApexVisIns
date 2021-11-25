using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Automation.BDaq;


namespace ApexVisIns
{

    /// <summary>
    /// Advantech PCI 1730U I/O Card 控制器
    /// </summary>
    public class IOController : INotifyPropertyChanged
    {
        #region Private elements
        private string _description;
        #endregion

        public InstantDiCtrl InstantDiCtrl { get; set; }

        private bool _diCtrlCreated;

        public InstantDoCtrl InstantDoCtrl { get; set; }

        private bool _doCtrlCreated;

        public IOController()
        {
        }

        public IOController(string description, bool initialize = false)
        {
            _description = description;

            if (initialize)
            {
                InstantDiCtrl = new InstantDiCtrl() { SelectedDevice = new DeviceInformation(description) };
            }
        }

        /// <summary>
        /// 初始化 DI Control
        /// </summary>
        public void InitializeDiCtrl()
        {
            if (_description != string.Empty)
            {
                InstantDiCtrl = new InstantDiCtrl()
                {
                    SelectedDevice = new DeviceInformation(_description),
                };

                #region interrupt 之 channel
                InstantDiCtrl.Interrupt += InstantDiCtrl_Interrupt;
                DiintChannel[] channels = InstantDiCtrl.DiintChannels;
                Debug.WriteLine($"{channels.Length}");

                channels[0].Enabled = true;
                channels[1].Enabled = true;

                channels[0].TrigEdge = ActiveSignal.BothEdge;
                channels[1].TrigEdge = ActiveSignal.BothEdge;

                // foreach (DiintChannel item in channels)
                // {
                //     Debug.WriteLine($"{item.Channel} {item.Enabled} {item.} {item.TrigEdge}");
                // }

                //ErrorCode err = InstantDiCtrl.SnapStart();
                #endregion

                _diCtrlCreated = true;

                // 新增 Collection, 全部拉低(等待讀取)
                DiArrayColl.Clear();
                for (int i = 0; i < InstantDiCtrl.PortCount; i++)
                {
                    DiArrayColl.Add(new ObservableCollection<bool>() { false, false, false, false, false, false, false, false });
                }

                // 測試
                // DioPort[] ports = InstantDiCtrl.Ports;
                // for (int i = 0; i < ports.Length; i++)
                // {
                //     // input: 0b00
                //     Debug.WriteLine($"Di Direction: {ports[i].DirectionMask}");
                // }
            }
            else
            {
                throw new ArgumentNullException("Set description before initialization.");
            }
        }

        private void InstantDiCtrl_Interrupt(object sender, DiSnapEventArgs e)
        {
            Debug.WriteLine("Interrupt");
            Debug.WriteLine($"{e.Id} {e.Length} {e.SrcNum}");
            Debug.WriteLine($"PortData: {string.Join(" | ", e.PortData)}");
        }

        /// <summary>
        /// 初始化 DO Control
        /// </summary>
        public void InitializeDoCtrl()
        {
            if (_description != string.Empty)
            {
                InstantDoCtrl = new InstantDoCtrl()
                {
                    SelectedDevice = new DeviceInformation(_description)
                };
                _doCtrlCreated = true;

                // 新增 Collection, 全部拉低
                for (int i = 0; i < InstantDoCtrl.PortCount; i++)
                {
                    DoArrayColl.Add(new ObservableCollection<bool>() { false, false, false, false, false, false, false, false });
                }

                // 測試 
                // DioPort[] ports = InstantDoCtrl.Ports;
                // for (int i = 0; i < ports.Length; i++)
                // {
                //     // Output: 0b01
                //     Debug.WriteLine($"Do Direction: {ports[i].DirectionMask}");
                // }
            }
            else
            {
                throw new ArgumentNullException("Set description before initialization.");
            }
        }

        public bool DiCtrlCreated
        {
            get => _diCtrlCreated;
        }

        public bool DoCtrlCreated
        {
            get => _doCtrlCreated;
        }


        public DiintChannel[] GetInterruptChannel()
        {
            DiintChannel[] channels = InstantDiCtrl.DiintChannels;
            return channels;
        }

        /// <summary>
        /// 設定 Channel 啟用中斷
        /// </summary>
        /// <param name="ch">通道</param>
        /// <param name="signel">觸發邊緣</param>
        /// <param name="enable">啟用 / 停用</param>
        /// <returns></returns>
        public ErrorCode SetInterrutChannel(int ch, ActiveSignal signel, bool enable = true)
        {
            if (DiCtrlCreated)
            {
                bool success = false;
                DiintChannel[] channels = InstantDiCtrl.DiintChannels;

                foreach (DiintChannel channel in channels)
                {
                    if (channel.Channel == ch)
                    {
                        channel.Enabled = enable;
                        channel.TrigEdge = signel;
                        success = true;
                        break;
                    }
                }
                return success ? ErrorCode.Success : ErrorCode.ErrorIntrNotAvailable;
            }
            else
            {
                throw new InvalidOperationException("Create DiCtrl before enable interrupt.");
            }
        }

        /// <summary>
        /// 啟用中斷器
        /// </summary>
        /// <returns></returns>
        public ErrorCode EnableInterrut()
        {
            if (DiCtrlCreated)
            {
                ErrorCode err = InstantDiCtrl.SnapStart();
                return err;
            }
            else
            {
                throw new InvalidOperationException("Create DiCtrl before enable interrupt.");
            }
        }

        /// <summary>
        /// 停止中斷器
        /// </summary>
        /// <returns></returns>
        public ErrorCode DisableInterrupt()
        {
            if (DiCtrlCreated)
            {
                ErrorCode err = InstantDiCtrl.SnapStop();
                return err;
            }
            else
            {
                throw new InvalidOperationException("Create DiCtrl before enable interrupt.");
            }
        }

        /// <summary>
        /// IO Card Description
        /// </summary>
        public string Description
        {
            get => _description;
            set
            {
                if (value != _description)
                {
                    _description = value;
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        /// <summary>
        /// DI Port 數
        /// </summary>
        public int DiPortCount
        {
            get => InstantDiCtrl.Features.PortCount;
        }

        /// <summary>
        /// DI 通道數
        /// </summary>
        public int DiChannelCount
        {
            get => InstantDiCtrl.Features.ChannelCountMax;
        }

        public int DoPortCount
        {
            get => InstantDoCtrl.Features.PortCount;
        }

        public int DoChannelCount
        {
            get => InstantDoCtrl.Features.ChannelCountMax;
        }

        /// <summary>
        /// DI Collection
        /// </summary>
        public ObservableCollection<ObservableCollection<bool>> DiArrayColl { get; } = new ObservableCollection<ObservableCollection<bool>>();

        /// <summary>
        /// DO Collection
        /// </summary>
        public ObservableCollection<ObservableCollection<bool>> DoArrayColl { get; set; } = new ObservableCollection<ObservableCollection<bool>>();

        /// <summary>
        /// Digital Input Port#0, Bit0 ~ Bit7
        /// </summary>
        //public BitArray DiArray0 { get; } = new BitArray(8);

        /// <summary>
        /// DI 讀取
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public ErrorCode ReadDI(int port)
        {
            ErrorCode err = InstantDiCtrl.Read(port, out byte data);
            if (err == ErrorCode.Success)
            {
                Debug.WriteLine($"Read DI: {data}");
                for (int i = 0; i < DiArrayColl[port].Count; i++)
                {
                    //boolArray[i] = ((data >> (i % 8)) & 0b01) == 0b01;
                    DiArrayColl[port][i] = ((data >> (i % 8)) & 0b01) == 0b01;
                }
            }
            return err;
        }

        /// <summary>
        /// DI Bit 讀取
        /// </summary>
        /// <param name="port"></param>
        /// <param name="bit"></param>
        /// <returns></returns>
        public ErrorCode ReadDIBit(int port, int bit)
        {
            ErrorCode err = InstantDiCtrl.ReadBit(port, bit, out byte data);

            if (err == ErrorCode.Success)
            {
                Debug.WriteLine($"Read DI Bit: {data}");
                DiArrayColl[port][bit] = data == 0b01;
            }
            return err;
        }

        /// <summary>
        /// DO 回讀
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public ErrorCode ReadDO(int port)
        {
            ErrorCode err = InstantDoCtrl.Read(port, out byte data);
            if (err == ErrorCode.Success)
            {
                Debug.WriteLine($"Read DO: {data}");
                for (int i = 0; i < DoArrayColl[port].Count; i++)
                {
                    DoArrayColl[port][i] = ((data >> (i % 8)) & 0b01) == 0b01;
                }
            }
            return err;
        }

        /// <summary>
        /// DO Bit 回讀
        /// </summary>
        /// <param name="port"></param>
        /// <param name="bit"></param>
        /// <returns></returns>
        public ErrorCode ReadDOBit(int port, int bit)
        {
            ErrorCode err = InstantDoCtrl.ReadBit(port, bit, out byte data);
            if (err == ErrorCode.Success)
            {
                Debug.WriteLine($"Read DO Bit: {data}");
                DoArrayColl[port][bit] = data == 0b01;
            }
            return err;
        }

        /// <summary>
        /// DO 寫入
        /// </summary>
        /// <param name="port">目標 Port</param>
        /// <param name="value">Value (0x00 ~ 0xFF)</param>
        /// <returns></returns>
        public ErrorCode WriteDO(int port, byte value)
        {
            if (0 > port || port >= DoPortCount)
            {
                throw new ArgumentOutOfRangeException("Invalid port number or out of range");
            }

            ErrorCode err = InstantDoCtrl.Write(port, value);
            if (err == ErrorCode.Success)
            {
                for (int i = 0; i < DoArrayColl[port].Count; i++)
                {
                    DoArrayColl[port][i] = ((value >> (i % 8)) & 0b01) == 0b01;
                }
            }
            return err;
        }

        /// <summary>
        /// DO Bit 寫入
        /// </summary>
        /// <param name="port">目標 Port</param>
        /// <param name="bit">目標 Bit</param>
        /// <param name="value">Bit Value, 0 or 1</param>
        public ErrorCode WriteDOBit(int port, byte bit, bool value)
        {
            if (0 > port || port >= DoPortCount)
            {
                throw new ArgumentOutOfRangeException("Invalid port number or out of range");
            }
            else if (0 > bit || bit >= 8)
            {
                throw new ArgumentOutOfRangeException("Invalid bit value, argument bit must be set from 0 to 8");
            }

            ErrorCode err = InstantDoCtrl.WriteBit(port, bit, Convert.ToByte(value));
            if (err == ErrorCode.Success)
            {
                DoArrayColl[port][bit] = value;
            }
            return err;
        }

        /// <summary>
        /// 變更 DI (待刪除)
        /// </summary>
        /// <param name="port">目標 Port</param>
        /// <param name="bit">指定 Bit</param>
        /// <param name="value">指定 Value</param>
        public void ChangeDI(int port, int bit, bool value)
        {
            BitArray bitArray = (BitArray)GetType().GetProperty($"DiArray{port}").GetValue(this);
            bitArray[bit] = value;
            OnPropertyChanged($"DiArray{port}");
        }

        /// <summary>
        /// 反轉 DI (待刪除)
        /// </summary>
        /// <param name="port"></param>
        /// <param name="bit"></param>
        public void InvertDI(int port, int bit)
        {
            BitArray bitArray = (BitArray)GetType().GetProperty($"DiArray{port}").GetValue(this);
            bitArray[bit] = !bitArray[bit];
            OnPropertyChanged($"DiArray{port}");
        }

        /// <summary>
        /// 測試用
        /// </summary>
        public void TriggerEvent()
        {
            OnDigitalInputChanged();
        }

        //public event DIChangedEventHandler DIChangedEventHandler;

        public delegate void DigitalInputChangedEventHandler(object sender, DigitalInputChangedEventArgs e);
        public event DigitalInputChangedEventHandler DigitalInputChanged;

        private void OnDigitalInputChanged()
        {
            DigitalInputChanged?.Invoke(this, new DigitalInputChangedEventArgs(0, 0, true));
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void PropertyChange(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public class DigitalInputChangedEventArgs : EventArgs
        {
            public int Port { get; }
            public byte Bit { get; }
            public bool Data { get; }

            public DigitalInputChangedEventArgs(int port, byte bit, bool data)
            {
                Port = port;
                Bit = bit;
                Data = data;
            }
        }
    }
}
