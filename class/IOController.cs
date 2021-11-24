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
        public InstantDoCtrl InstantDoCtrl { get; set; }

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
                    SelectedDevice = new DeviceInformation(_description)
                };

                // 新增 Collection, 全部拉低(等待讀取)
                DiArrayColl.Clear();
                for (int i = 0; i < InstantDiCtrl.PortCount; i++)
                {
                    DiArrayColl.Add(new ObservableCollection<bool>() { false, false, false, false, false, false, false, false });
                }
            }
            else
            {
                throw new ArgumentNullException("Set description before initialization.");
            }
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

                // 新增 BitArray, 
            }
            else
            {
                throw new ArgumentNullException("Set description before initialization.");
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

        #region 待刪除
        public ObservableCollection<bool> DiArray0 { get; } = new ObservableCollection<bool>() { false, false, false, false, false, false, false, false };

        /// <summary>
        /// Digital Input Port#1, Bit8 ~ Bit15
        /// </summary>
        public BitArray DiArray1 { get; } = new BitArray(8);

        /// <summary>
        /// Digital Input Port#2, Bit16 ~ Bit23
        /// </summary>
        public BitArray DiArray2 { get; } = new BitArray(8);

        /// <summary>
        /// Digital Input Port#3, Bit24 ~ Bit31
        /// </summary>
        public BitArray DiArray3 { get; } = new BitArray(8); 
        #endregion

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
                Debug.WriteLine($"Read: {data}");
                for (int i = 0; i < DiArrayColl[port].Count; i++)
                {
                    //boolArray[i] = ((data >> (i % 8)) & 0b01) == 0b01;
                    DiArrayColl[port][i] = ((data >> (i % 8)) & 0b01) == 0b01;
                }
            }
            return err;
        }

        /// <summary>
        /// DI Bit 讀取 (待修正)
        /// </summary>
        /// <param name="port"></param>
        /// <param name="bit"></param>
        /// <returns></returns>
        public ErrorCode ReadDIBit(int port, int bit)
        {
            ErrorCode err = InstantDiCtrl.ReadBit(port, bit, out byte data);

            if (err == ErrorCode.Success)
            {
                Debug.WriteLine($"ReadBit: {data}");
                DiArrayColl[port][bit] = data == 0b01;
            }
            return err;
        }


        public ErrorCode ReadDO(int port)
        {
            ErrorCode err = InstantDoCtrl.Read(port, out byte data);
            if (err == ErrorCode.Success)
            {

            }
            return err;
        }

        public ErrorCode ReadDOBit(int port, int bit)
        {
            ErrorCode err = InstantDoCtrl.ReadBit(port, bit, out byte data);
            if (err == ErrorCode.Success)
            {

            }
            return err;
        }

        /// <summary>
        /// 變更 DI (待修正)
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
        /// 反轉 DI (待修正)
        /// </summary>
        /// <param name="port"></param>
        /// <param name="bit"></param>
        public void InvertDI(int port, int bit)
        {
            BitArray bitArray = (BitArray)GetType().GetProperty($"DiArray{port}").GetValue(this);
            bitArray[bit] = !bitArray[bit];
            OnPropertyChanged($"DiArray{port}");
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
    }
}
