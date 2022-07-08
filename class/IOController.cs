using Automation.BDaq;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;

namespace MCAJawIns
{
    /// <summary>
    /// Advantech PCI 1730U I/O Card 控制器
    /// </summary>
    public class IOController : INotifyPropertyChanged, IDisposable
    {
        #region Private elements
        private readonly object _CollectionLock = new();

        private string _description;
        private bool _interruptEnabled;
        private int _interruptCount;

        //[Obsolete]
        //private Task debounceTask;

        private System.Timers.Timer debounceTimer;

        private bool _disposed;
        private bool _diCtrlCreated;
        private bool _doCtrlCreated;
        private bool _doLocked = true;

        /// <summary>
        /// DI 集合鎖
        /// </summary>
        private readonly object _diCollLock = new();
        /// <summary>
        /// DO 集合鎖
        /// </summary>
        private readonly object _doCollLock = new();
        /// <summary>
        /// 中斷器集合
        /// </summary>
        private readonly object _intCollLock = new();
        #endregion

        /// <summary>
        /// Digital Input Instant
        /// </summary>
        public InstantDiCtrl InstantDiCtrl { get; set; }

        /// <summary>
        /// Digital Output Instant
        /// </summary>
        public InstantDoCtrl InstantDoCtrl { get; set; }

        /// <summary>
        /// 啟用的 Digital Input Ports
        /// </summary>
        public int EnabledDiPorts { get; set; } = 999;

        /// <summary>
        /// 啟用的 Digital Output Ports
        /// </summary>
        public int EnabledDoPorts { get; set; } = 999;

        /// <summary>
        /// 建構子
        /// </summary>
        public IOController()
        {
        }

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="description"></param>
        /// <param name="initialize"></param>
        public IOController(string description, bool initialize = false)
        {
            _description = description;

            if (initialize)
            {
                InstantDiCtrl = new InstantDiCtrl() { SelectedDevice = new DeviceInformation(description) };
            }
        }

        /// <summary>
        /// 中斷是否啟用
        /// </summary>
        public bool InterruptEnabled
        {
            get => _interruptEnabled;
            set
            {
                if (value != _interruptEnabled)
                {
                    _interruptEnabled = value;
                    //OnPropertyChanged(nameof(InterruptEnabled));
                    OnPropertyChanged();
                }
            }
        }
        
        /// <summary>
        /// 中斷計數器
        /// </summary>
        public int InterruptCount
        {
            get => _interruptCount;
            set
            {
                _interruptCount = value;
                //OnPropertyChanged(nameof(InterruptCount));
                OnPropertyChanged();
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
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// DI Port 數
        /// </summary>
        public int DiPortCount => InstantDiCtrl.Features.PortCount;

        /// <summary>
        /// DI 通道數
        /// </summary>
        public int DiChannelCount => InstantDiCtrl.Features.ChannelCountMax;

        /// <summary>
        /// Do Port 數
        /// </summary>
        public int DoPortCount => InstantDoCtrl.Features.PortCount;

        /// <summary>
        /// Do 通道數
        /// </summary>
        public int DoChannelCount => InstantDoCtrl.Features.ChannelCountMax;

        /// <summary>
        /// DI Collection
        /// </summary>
        public ObservableCollection<ObservableCollection<bool>> DiArrayColl { get; } = new ObservableCollection<ObservableCollection<bool>>();

        /// <summary>
        /// DO Collection
        /// </summary>
        public ObservableCollection<ObservableCollection<bool>> DoArrayColl { get; set; } = new ObservableCollection<ObservableCollection<bool>>();

        /// <summary>
        /// 可啟用 Interrupt 之通道
        /// </summary>
        public ObservableCollection<InterruptChannel> Interrupts { get; private set; } = new ObservableCollection<InterruptChannel>();


        /// <summary>
        /// 確認 DLL 已安裝且版本符合
        /// </summary>
        /// <returns></returns>
        public static bool CheckDllVersion()
        {
            string fileName = Environment.SystemDirectory + @"\biodaq.dll"; // SystemDirectory : System32

            if (File.Exists(fileName))
            {
                string fileVersion = FileVersionInfo.GetVersionInfo(fileName).FileVersion;

                string[] strSplit = fileVersion.Split(',');

                if (Convert.ToUInt16(strSplit[0], CultureInfo.CurrentCulture) < 2)
                {
                    return false;
                }
            }
            else
            {
                //throw new DllNotFoundException("Motion 控制驅動未安裝");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 啟用 Collecion Binding，
        /// 必須為主執行緒呼叫
        /// </summary>
        public void EnableCollectionBinding()
        {
            BindingOperations.EnableCollectionSynchronization(DiArrayColl, _diCollLock);
            BindingOperations.EnableCollectionSynchronization(DoArrayColl, _doCollLock);
            BindingOperations.EnableCollectionSynchronization(Interrupts, _intCollLock);
        }

        /// <summary>
        /// 停用 Collection Binding，
        /// 必須為主執行緒呼叫
        /// </summary>
        public void DisableCollectionBinding()
        {
            BindingOperations.DisableCollectionSynchronization(DiArrayColl);
            BindingOperations.DisableCollectionSynchronization(DoArrayColl);
            BindingOperations.DisableCollectionSynchronization(Interrupts);
        }

        /// <summary>
        /// 初始化 DI Control
        /// </summary>
        /// <param name="ports">啟用之 Port 數</param>
        public void InitializeDiCtrl()
        {
            if (_description != string.Empty)
            {
                try
                {
                    InstantDiCtrl = new InstantDiCtrl()
                    {
                        SelectedDevice = new DeviceInformation(_description),
                    };
                    DiCtrlCreated = true;
                }
                catch (DllNotFoundException)
                {
                    throw;
                }
                catch (Exception)
                {
                    throw;
                }


                lock (_diCollLock)
                {
                    // 新增 Collection, 全部拉低(等待讀取)
                    DiArrayColl.Clear();
                    for (int i = 0; i < InstantDiCtrl.PortCount && i < EnabledDiPorts; i++)
                    {
                        //ObservableCollection<bool> subCollection = new ObservableCollection<bool>() { false, false, false, false, false, false, false, false };
                        DiArrayColl.Add(new ObservableCollection<bool>() { false, false, false, false, false, false, false, false });
                    }
                }

                lock (_intCollLock)
                {
                    Interrupts.Clear();
                    foreach (DiintChannel item in InstantDiCtrl.DiintChannels)
                    {
                        // 判斷 Ch 號, 小於啟用 Port 數 * 8 個 Ch
                        if (item.Channel < EnabledDiPorts * 8)
                        {
                            Interrupts.Add(new InterruptChannel(item.Channel));
                        }
                    }
                }
            }
            else
            {
                throw new ArgumentNullException("Device desciption 未設定");
            }
        }

        /// <summary>
        /// 初始化 DO Control
        /// </summary>
        /// <param name="ports">啟用之 Port 數</param>
        public void InitializeDoCtrl()
        {
            if (_description != string.Empty)
            {
                try
                {
                    InstantDoCtrl = new InstantDoCtrl()
                    {
                        SelectedDevice = new DeviceInformation(_description)
                    };
                    DoCtrlCreated = true;
                }
                catch (DllNotFoundException)
                {
                    throw;
                }
                catch (Exception)
                {
                    throw;
                }

                lock (_doCollLock)
                {
                    DoArrayColl.Clear();
                    // 新增 Collection, 全部拉低
                    for (int i = 0; i < InstantDoCtrl.PortCount && i < EnabledDoPorts; i++)
                    {
                        DoArrayColl.Add(new ObservableCollection<bool>() { false, false, false, false, false, false, false, false });
                    }
                }
            }
            else
            {
                throw new ArgumentNullException("Set description before initialization.");
            }
        }

        /// <summary>
        /// DI Controll 是否建立
        /// </summary>
        public bool DiCtrlCreated
        {
            get => _diCtrlCreated;
            private set
            {
                if (value != _diCtrlCreated)
                {
                    _diCtrlCreated = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// DO Controll 是否建立
        /// </summary>
        public bool DoCtrlCreated
        {
            get => _doCtrlCreated;
            private set
            {
                if (value != _doCtrlCreated)
                {
                    _doCtrlCreated = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 鎖定 DO，避免手動操作
        /// </summary>
        public bool DOLocked
        {
            get => _doLocked;
        }

        /// <summary>
        /// Interrupt Channel Object
        /// </summary>
        public class InterruptChannel
        {
            public InterruptChannel(int channel)
            {
                Channel = channel;
            }

            public int Channel { get; }

            public bool Enabled { get; set; }
        }

        /// <summary>
        /// 設定中斷器，設定過後必須重設 SnapStart()
        /// </summary>
        /// <param name="ch">通道號碼</param>
        /// <param name="signal">觸發條件(上升/下降)</param>
        /// <param name="enable">啟用/停用</param>
        /// <returns></returns>
        public ErrorCode SetInterruptChannel(int ch, ActiveSignal signal, bool enable = true)
        {
            if (DiCtrlCreated)
            {
                DiintChannel diintChannel = Array.Find(InstantDiCtrl.DiintChannels, e => e.Channel == ch);

                if (diintChannel != null)
                {
                    InstantDiCtrl.SnapStop();

                    diintChannel.Enabled = enable;
                    diintChannel.TrigEdge = signal;

                    InstantDiCtrl.SnapStart();
                    return ErrorCode.Success;
                }
                else
                {
                    return ErrorCode.ErrorIntrNotAvailable;
                }
            }
            return ErrorCode.Success;
        }

        public DiintChannel[] InterruptEnabledChannel => InstantDiCtrl.DiintChannels.Where(e => e.Enabled).ToArray();

        //private void InstantDiCtrl_Interrupt(object sender, DiSnapEventArgs e)
        //{
        //    // 觸發條件: 上升邊緣、下降邊緣、雙邊緣
        //    // 節流邏輯
        //    //if (debounceTask == null || debounceTask.IsCompleted)
        //    //{
        //    //    debounceTask = Task.Run(() =>
        //    //    {
        //            // 節流，125ms內不允許第二次輸入
        //            _ = SpinWait.SpinUntil(() => false, 125);

        //            InterruptCount++;
        //            int port = e.SrcNum / 8;
        //            byte bit = (byte)(e.SrcNum % 8);
        //            // Trigger Digital Changed
        //            OnDigitalInputChanged(port, bit, ((e.PortData[port] >> bit) & 0b01) == 0b01);
        //            lock (_CollectionLock)
        //            {
        //                for (int i = 0; i < e.Length && i < EnabledDiPorts; i++)
        //                {
        //                    SetDI(i, e.PortData[i]);
        //                }
        //            }
        //    //    });
        //    //}
        //}

        /// <summary>
        /// 中斷事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InstantDiCtrl_Interrupt(object sender, DiSnapEventArgs e)
        {
            // lock (this)
            // {
            InterruptCount++;
            //Debug.WriteLine($"{DateTime.Now:HH:mm:ss:fff}");
            // return;

            if (debounceTimer == null)
            {
                debounceTimer = new System.Timers.Timer(50)
                {
                    AutoReset = false,
                };

                debounceTimer.Elapsed += (o, e2) =>
                {
                    debounceTimer.Stop();
                    debounceTimer.Close();
                    debounceTimer = null;

                    InterruptCount++;
                    int port = e.SrcNum / 8;
                    byte bit = (byte)(e.SrcNum % 8);
                    // Trigger Digital Changed
                    OnDigitalInputChanged(port, bit, ((e.PortData[port] >> bit) & 0b01) == 0b01);

                    if (_interruptEnabled)
                    {
                        lock (_CollectionLock)
                        {
                            for (int i = 0; i < e.Length && i < EnabledDiPorts; i++)
                            {
                                UpdateDIColl(i, e.PortData[i]);
                            }
                        }
                    }
                };
            }
            debounceTimer.Stop();
            debounceTimer.Start();
            //}
        }

        /// <summary>
        /// 設定 Digital Input
        /// </summary>
        /// <param name="port"></param>
        /// <param name="data"></param>
        private void UpdateDIColl(int port, int data)
        {
            for (int ch = 0; ch < DiArrayColl[port].Count; ch++)
            {
                DiArrayColl[port][ch] = ((data >> (ch % 8)) & 0b01) == 0b1;
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
                InstantDiCtrl.Interrupt += InstantDiCtrl_Interrupt;

                ErrorCode err = InstantDiCtrl.SnapStart();
                if (err == ErrorCode.Success)
                {
                    foreach (ObservableCollection<bool> collection in DiArrayColl)
                    {
                        BindingOperations.EnableCollectionSynchronization(collection, _CollectionLock);
                    }
                    _interruptEnabled = true;
                    InterruptCount = 0;
                }
                return err;
            }
            else
            {
                throw new InvalidOperationException("在生成 DI 實例之前不允許啟用中斷");
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
                InstantDiCtrl.Interrupt -= InstantDiCtrl_Interrupt;

                ErrorCode err = InstantDiCtrl.SnapStop();
                if (err == ErrorCode.Success)
                {
                    foreach (ObservableCollection<bool> collection in DiArrayColl)
                    {
                        BindingOperations.DisableCollectionSynchronization(collection);
                    }
                    _interruptEnabled = false;
                }
                return err;
            }
            else
            {
                throw new InvalidOperationException("在生成 DI 實例之前不允許停用中斷");
            }
        }

        /// <summary>
        /// 讀取 DI
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
                    DiArrayColl[port][i] = ((data >> (i % 8)) & 0b01) == 0b01;
                }
            }
            return err;
        }

        /// <summary>
        /// 讀取 DI
        /// </summary>
        /// <param name="port"></param>
        /// <returns>byte value</returns>
        public byte ReadDIValue(int port)
        {
            ErrorCode err = InstantDiCtrl.Read(port, out byte data);

            if (err == ErrorCode.Success)
            {
                Debug.WriteLine($"Read DI Value: {data}");
                //for (int i = 0; i < DiArrayColl[port].Count; i++)
                //{
                //    DiArrayColl[port][i] = ((data >> (i % 8)) & 0b01) == 0b01;
                //}
                return data;
            }
            throw new Exception($"DI 讀取失敗 {err}");
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
        /// 讀取 DI Bit
        /// </summary>
        /// <param name="port"></param>
        /// <returns>boolean</returns>
        public bool ReadDIBitValue(int port, int bit)
        {
            ErrorCode err = InstantDiCtrl.ReadBit(port, bit, out byte data);

            if (err == ErrorCode.Success)
            {
                Debug.WriteLine($"Read DI Bit Value: {data}");
                //DiArrayColl[port][bit] = data == 0b01;
                return data == 0b01;
            }
            throw new Exception($"DI 讀取失敗 {err}");
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

        public void LockDO()
        {
            _doLocked = true;
            OnPropertyChanged(nameof(DOLocked));
        }

        public void UnlockDO()
        {
            _doLocked = false;
            OnPropertyChanged(nameof(DOLocked));
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

            if (!_doLocked)
            {
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
            else
            {
                throw new InvalidOperationException("DO 鎖定中");
            }
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
            else if (bit is < 0 or >= 8)    // 模式比對，必須為常數值
            {
                throw new ArgumentOutOfRangeException("Invalid bit value, argument bit must be set from 0 to 8");
            }

            if (!_doLocked)
            {

                ErrorCode err = InstantDoCtrl.WriteBit(port, bit, Convert.ToByte(value));
                if (err == ErrorCode.Success)
                {
                    DoArrayColl[port][bit] = value;
                }
                return err;
            }
            else
            {
                throw new InvalidOperationException("DO 鎖定中");
            }
        }

        // public event DIChangedEventHandler DIChangedEventHandler;

        public delegate void DigitalInputChangedEventHandler(object sender, DigitalInputChangedEventArgs e);
        public event DigitalInputChangedEventHandler DigitalInputChanged;

        /// <summary>
        /// 觸發 DI Changed 事件
        /// </summary>
        /// <param name="port"></param>
        /// <param name="bit"></param>
        /// <param name="value"></param>
        private void OnDigitalInputChanged(int port, byte bit, bool value)
        {
            DigitalInputChanged?.Invoke(this, new DigitalInputChangedEventArgs(port, bit, value));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void PropertyChange(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 處置物件
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                DisableCollectionBinding();

                // Code here
                InstantDiCtrl?.Dispose();
                InstantDiCtrl = null;
                InstantDoCtrl?.Dispose();
                InstantDoCtrl = null;

                //debounceTask?.Dispose();
                //debounceTask = null;

                debounceTimer?.Dispose();
                debounceTimer = null;
            }
            _disposed = true;
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
