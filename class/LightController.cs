using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ApexVisIns
{
    public class LightEnumer : LongLifeWorker
    {
        private readonly object _CollectionLock = new();

        public ObservableCollection<string> ComPortSource { get; set; } = new ObservableCollection<string>();

        private void ComPortSourceAdd(string comPort)
        {
            lock (_CollectionLock)
            {
                ComPortSource.Add(comPort);
            }
        }

        private void ComPortSourceClear()
        {
            lock (_CollectionLock)
            {
                ComPortSource.Clear();
            }
        }

        public override void WorkerStart()
        {
            BindingOperations.EnableCollectionSynchronization(ComPortSource, _CollectionLock);
            base.WorkerStart();
        }

        public override void DoWork()
        {
            try
            {
                string[] portNames = SerialPort.GetPortNames();

                if (portNames.Length == 0)
                {
                    ComPortSourceClear();
                    _ = SpinWait.SpinUntil(() => false, 500);
                }

                foreach (string com in portNames)
                {
                    if (!ComPortSource.Contains(com))
                    {
                        ComPortSourceAdd(com);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        //public void CloseSerialPort()
        //{
        //    if (serialPort != null && serialPort.IsOpen)
        //    {
        //        serialPort.Close();
        //    }
        //    OnPropertyChanged(nameof(IsSerialPortOpen));
        //}

        //public override void WorkerEnd()
        //{
        //    //CloseSerialPort();
        //    base.WorkerEnd();
        //}


        //public event PropertyChangedEventHandler PropertyChanged;
        //private void OnPropertyChanged(string propertyName = null)
        //{
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //}
    }


    public class LightController : INotifyPropertyChanged
    {
        private int _channels;
        
        private int[] channelValue;

        public LightController()
        {
        }

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="chs">控制器光源通道數</param>
        public LightController(int chs)
        {
            Channels = chs;
            channelValue = new int[chs];
        }

        public SerialPort SerialPort { get; set; }

        /// <summary>
        /// ComPort 是否開啟
        /// </summary>
        public bool IsComOpen => SerialPort != null && SerialPort.IsOpen;

        /// <summary>
        /// 光源通道數
        /// </summary>
        public int Channels
        {
            get => _channels;
            set
            {
                _channels = value;
                channelValue = new int[value];
            }
        }

        /// <summary>
        /// 開啟 COM
        /// </summary>
        /// <param name="com"></param>
        public void ComOpen(string com)
        {
            SerialPort = new SerialPort(com, 9600, Parity.None, 8, StopBits.One);
            SerialPort.Open();
            OnPropertyChanged(nameof(IsComOpen));
        }

        /// <summary>
        /// 開啟 COM
        /// </summary>
        /// <param name="com"></param>
        /// <param name="baudRate"></param>
        /// <param name="parity"></param>
        /// <param name="dataBits"></param>
        /// <param name="stopBits"></param>
        public void ComOpen (string com, int baudRate, Parity parity, int dataBits,StopBits stopBits)
        {
            SerialPort = new SerialPort(com, baudRate, parity, dataBits, stopBits);
            SerialPort.Open();
            OnPropertyChanged(nameof(IsComOpen));
        }

        /// <summary>
        /// 關閉 COM
        /// </summary>
        public void ComClose()
        {
            SerialPort.Close();
            OnPropertyChanged(nameof(IsComOpen));
        }

        public int GetChannelValue(int ch)
        {
            return channelValue[ch];
        }

        /// <summary>
        /// 控制命令寫入 (需要帶 \r\n)
        /// </summary>
        /// <param name="str">命令</param>
        /// <returns>0.2秒內回傳結果</returns>
        public string Write(string str)
        {
            SerialPort.Write(str);

            return SerialPort.ReadLine();
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
