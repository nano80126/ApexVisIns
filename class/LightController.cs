using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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

    /// <summary>
    /// 光源通道 Channel
    /// </summary>
    public class LightChannel : INotifyPropertyChanged
    {
        private int _value;

        public LightChannel()
        {
        }

        public LightChannel(string channel, int value)
        {
            Channel = channel;
            Value = value;
        }

        public string Channel { get; set; }

        public int Value
        {
            get => _value;
            set
            {
                if (value != _value)
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 光源控制器物件
    /// </summary>
    public class LightController : INotifyPropertyChanged
    {
        private int _channelNumber;
        //private int _channelOn;

        public LightController()
        {
        }

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="chs">控制器光源通道數</param>
        public LightController(int chs)
        {
            ChannelNumber = chs;

            Channels.Clear();
            for (int i = 0; i < ChannelNumber; i++)
            {
                Channels.Add(new LightChannel($"Ch{i + 1}", 0));
            }

            //Channels = new ObservableCollection<LightChannel>()
            //{
            //    new LightChannel()
            //    {
            //        Channel = 
            //    }
            //}
        }

        public SerialPort SerialPort { get; set; }

        /// <summary>
        /// ComPort 是否開啟
        /// </summary>
        public bool IsComOpen => SerialPort != null && SerialPort.IsOpen;

        /// <summary>
        /// 光源通道數
        /// </summary>
        public int ChannelNumber
        {
            get => _channelNumber;
            set
            {
                _channelNumber = value;
                Channels.Clear(); // 先清除 Collection
                for (int i = 0; i < _channelNumber; i++)
                {
                    Channels.Add(new LightChannel($"Ch{i + 1}", 0));
                }
            }
        }

        /// <summary>
        /// 通道 Source
        /// </summary>
        public ObservableCollection<LightChannel> Channels { get; set; } = new ObservableCollection<LightChannel>();

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
        public void ComOpen(string com, int baudRate, Parity parity, int dataBits, StopBits stopBits)
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

        /// <summary>
        /// 歸零所有通道
        /// </summary>
        public void ResetAllValue()
        {
            string cmd = string.Empty;
            for (int i = 1; i <= ChannelNumber; i++)
            {
                cmd += $"{i},0,";
            }
            cmd = $"{cmd.TrimEnd(',')}\r\n";

            try
            {
                _ = Write(cmd);
                foreach (LightChannel ch in Channels) // 歸零 channel
                {
                    ch.Value = 0;
                }
            }
            catch (TimeoutException T)
            {
                throw new TimeoutException($"重置光源設置失敗: {T.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"重置光源設置失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 設定通道光源大小,
        /// 後端使用
        /// </summary>
        /// <param name="ch">通道</param>
        /// <param name="value">目標設定值</param>
        public void SetCannelValue(int ch, int value)
        {
            if (ch < 0 && ch <= ChannelNumber)
            {
                string cmd = $"{ch},{value}\r\n";
                _ = Write(cmd);     // 寫入控制器
                Channels[ch - 1].Value = value;  // 變更通道值
            }
            else
            {
                throw new ArgumentException("指令的通道不存在");
            }
        }

        /// <summary>
        /// 取得通道光源大小
        /// </summary>
        /// <param name="ch">Ch1: 0, CH2: 1, ...</param>
        /// <returns></returns>
        public int GetChannelValue(int ch)
        {
            if (0 < ch && ch <= ChannelNumber)
            {
                return Channels[ch - 1].Value;    // 回傳通道值
            }
            else
            {
                throw new ArgumentException("指令的通道不存在");
            }
        }

        /// <summary>
        /// 控制命令寫入 (需要帶 \r\n)
        /// </summary>
        /// <param name="str">命令</param>
        /// <returns>0.2秒內回傳結果</returns>
        public string Write(string str, int timeout = 200)
        {
            try
            {
                SerialPort.ReadTimeout = timeout;
                SerialPort.Write(str);

                string ret = SerialPort.ReadLine();
                return ret;
            }
            catch (TimeoutException)
            {
                throw;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
