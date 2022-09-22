using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using MCAJawIns.Driver;

namespace MCAJawIns
{
    /// <summary>
    /// 光源控制器通道
    /// </summary>
    public class LightChannel : INotifyPropertyChanged
    {
        private ushort _value;

        public LightChannel()
        {
        }

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="channel">通道名</param>
        /// <param name="value">數值</param>
        public LightChannel(string channel, ushort value)
        {
            Channel = channel;
            Value = value;
        }

        public string Channel { get; set; }

        public ushort Value
        {
            get => _value;
            set
            {
                if (value != _value)
                {
                    _value = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    public class LightSerial : SerialPortBase
    {
        #region Varibles
        private int _channelNumber;
        //private bool _disposed;
        #endregion

        public LightSerial()
        {
        }

        public LightSerial(int chs)
        {
            ChannelNumber = chs;
            Channels.Clear();
            for (int i = 0; i < ChannelNumber; i++)
            {
                Channels.Add(new LightChannel($"Ch{i + 1}", 0));
            }
        }

        /// <summary>
        /// 光源通道數
        /// </summary>
        public int ChannelNumber
        {
            get => _channelNumber;
            set
            {
                _channelNumber = value;
                Channels.Clear();
                for (int i = 0; i < _channelNumber; i++)
                {
                    Channels.Add(new LightChannel($"Ch{i + 1}", 0));
                }
            }
        }

        public ObservableCollection<LightChannel> Channels { get; set; } = new ObservableCollection<LightChannel>();

        ///// <summary>
        ///// 開啟 COM，
        ///// 預設 9600, N, 8, 1
        ///// </summary>
        ///// <param name="com">COM PORT 名稱</param>
        //public override void ComOpen(string com)
        //{
        //    base.ComOpen(com);
        //}

        ///// <summary>
        ///// 開啟 COM
        ///// </summary>
        ///// <param name="com">COM PORT 名稱</param>
        ///// <param name="baudRate"></param>
        ///// <param name="parity"></param>
        ///// <param name="dataBits"></param>
        ///// <param name="stopBits"></param>
        //public override void ComOpen(string com, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        //{
        //    base.ComOpen(com, baudRate, parity, dataBits, stopBits);
        //}

        ///// <summary>
        ///// 開啟 COM
        ///// </summary>
        ///// <param name="com">COM PORT 名稱</param>
        ///// <param name="baudRate"></param>
        ///// <param name="parity"></param>
        ///// <param name="dataBits"></param>
        ///// <param name="stopBits"></param>
        //public override void ComOpen(int baudRate, Parity parity, int dataBits, StopBits stopBits)
        //{
        //    base.ComOpen(baudRate, parity, dataBits, stopBits);
        //}

        //public override void ComClose()
        //{
        //    base.ComClose();
        //}

        protected override void Write(string str)
        {
            base.Write(str);
            //try
            //{
            //    _serialPort.Write(str);
            //}
            //catch (Exception)
            //{
            //    throw;
            //}
        }

        protected override string ReadLine()
        {
            try
            {
                return _serialPort.ReadLine();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool Test(out string result)
        {
            result = string.Empty;
            if (IsComOpen)
            {
                try
                {
                    Write("\r\n");
                    _ = ReadLine();
                    return true;
                }
                catch (TimeoutException T)
                {
                    result = $"控制器沒有回應 {T.Message}";
                    return false;
                }
            }
            else
            {
                result = "SerialPort 未開啟";
                return false;
            }
        }

        /// <summary>
        /// 重置所有通道
        /// </summary>
        public void ResetAllChannel()
        {
            if (IsComOpen)
            {
                string cmd = string.Empty;
                for (int i = 1; i < ChannelNumber; i++)
                {
                    cmd += $"{i},0,";
                }
                cmd += $"{ChannelNumber},0\r\n";

                try
                {
                    Write(cmd);
                    _ = ReadLine();
                    foreach (LightChannel ch in Channels)
                    {
                        ch.Value = 0;
                    }
                }
                catch (TimeoutException T)
                {
                    throw new LightCtrlException($"重置光源失敗: {T.Message}");
                }
                catch (Exception ex)
                {
                    throw new LightCtrlException($"重置光源失敗: {ex.Message}");
                }
            }
            else
            {
                throw new LightCtrlException("光源控制器 SerialPort 未開啟");
            }
        }

        /// <summary>
        /// 嘗試重置所有通道
        /// </summary>
        /// <param name="result"></param>
        /// <returns>若成功回傳 true, 否則 false</returns>
        public bool TryResetAllChannel(out string result)
        {
            if (IsComOpen)
            {
                result = string.Empty;
                string cmd = string.Empty;
                for (int i = 1; i < ChannelNumber; i++)
                {
                    cmd += $"{i},0,";
                }
                cmd += $"{ChannelNumber},0\r\n";

                try
                {
                    Write(cmd);
                    _ = ReadLine();
                    foreach (LightChannel ch in Channels)
                    {
                        ch.Value = 0;
                    }
                    return true;
                }
                catch (TimeoutException T)
                {
                    result = $"重置光源失敗: {T.Message}";
                    return false;
                }
                catch (Exception ex)
                {
                    result = $"重置光源失敗: {ex.Message}";
                    return false;
                }
            }
            else
            {
                result = "SerialPort 未開啟";
                return false;
            }
        }

        /// <summary>
        /// 設定通道 Value (ch1 ~)
        /// </summary>
        /// <param name="ch">通道 (從 1 開始而非 0)</param>
        /// <param name="value">設定值</param>
        public void SetChannelValue(int ch, ushort value)
        {
            if (0 < ch && ch <= ChannelNumber)
            {
                string cmd = $"{ch},{value}\r\n";

                try
                {
                    Write(cmd);
                    _ = ReadLine();
                    Channels[ch - 1].Value = value;
                }
                catch (TimeoutException T)
                {
                    throw new LightCtrlException($"光源設置失敗: {T.Message}");
                }
                catch (Exception ex)
                {
                    throw new LightCtrlException($"光源設置失敗: {ex.Message}");
                }
            }
            else
            {
                throw new ArgumentException("指定的通道不存在");
            }
        }

        /// <summary>
        /// 寫入當前通道所設置的值 (通常僅為 LightPanel 使用)
        /// </summary>
        public void SetAllChannelValue()
        {
            if (IsComOpen)
            {
                string cmd = string.Empty;

                if (ChannelNumber > 0) { cmd += $"1,{Channels[0].Value},"; }
                if (ChannelNumber > 1) { cmd += $"2,{Channels[1].Value},"; }
                if (ChannelNumber > 2) { cmd += $"3,{Channels[2].Value},"; }
                if (ChannelNumber > 3) { cmd += $"4,{Channels[3].Value},"; }
                cmd = $"{cmd.TrimEnd(',')}\r\n";

                try
                {
                    Write(cmd);
                    _ = ReadLine();
                }
                catch (TimeoutException T)
                {
                    throw new LightCtrlException($"光源設置失敗: {T.Message}");
                }
                catch (Exception ex)
                {
                    throw new LightCtrlException($"光源設置失敗: {ex.Message}");
                }
            }
            else
            {
                throw new LightCtrlException("光源控制器 SerialPort 未開啟");
            }
        }

        /// <summary>
        /// 一次設置所有通道
        /// </summary>
        /// <param name="ch1">Ch1 設定值</param>
        /// <param name="ch2">Ch2 設定值</param>
        /// <param name="ch3">Ch3 設定值</param>
        /// <param name="ch4">Ch4 設定值</param>
        public void SetAllChannelValue(ushort ch1 = 0, ushort ch2 = 0, ushort ch3 = 0, ushort ch4 = 0)
        {
            if (IsComOpen)
            {
                string cmd = $"1,{ch1},2,{ch2},3,{ch3},4,{ch4}\r\n";
                try
                {
                    // Debug.WriteLine($"Write {DateTime.Now:ss.fff}");
                    Write(cmd);
                    _ = ReadLine();
                    // Debug.WriteLine($"Wrtire done {DateTime.Now:ss.fff}");
                    //string read = ReadLine();
                    // Debug.WriteLine($"Read {read} {DateTime.Now:ss.fff}");

                    if (ChannelNumber > 0) { Channels[0].Value = ch1; }
                    if (ChannelNumber > 1) { Channels[1].Value = ch2; }
                    if (ChannelNumber > 2) { Channels[2].Value = ch3; }
                    if (ChannelNumber > 3) { Channels[3].Value = ch4; }
                }
                catch (TimeoutException T)
                {
                    throw new LightCtrlException($"光源設置失敗: {T.Message}");
                }
                catch (Exception ex)
                {
                    throw new LightCtrlException($"光源設置失敗: {ex.Message}");
                }
            }
            else
            {
                throw new LightCtrlException("光源控制器 SerialPort 未開啟");
            }
        }

        /// <summary>
        /// 取得通道 Value
        /// </summary>
        /// <param name="ch">通道</param>
        /// <returns>返回通道值</returns>
        public int GetChannelValue(int ch)
        {
            return 0 < ch && ch <= ChannelNumber ? Channels[ch - 1].Value : throw new ArgumentException("指定的通道不存在");
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_serialPort.IsOpen) { _serialPort.Close(); }
                    _serialPort.Dispose();
                    _serialPort = null;
                }
                _disposed = true;
            }
        }
    }

#if false


    [Obsolete("use SerialEnumer instead")]
    public class LightEnumer : LongLifeWorker
    {
        private readonly object _CollectionLock = new();

        public ObservableCollection<string> ComPortSource { get; set; } = new ObservableCollection<string>();

        /// <summary>
        /// 初始化旗標
        /// </summary>
        //public LongLifeWorker.InitFlags InitFlag { get; private set; } = InitFlags.Starting;

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

        public override void WorkerEnd()
        {
            BindingOperations.DisableCollectionSynchronization(ComPortSource);
            base.WorkerEnd();
        }

        public override void DoWork()
        {
            try
            {
                string[] portNames = SerialPort.GetPortNames();

                if (portNames.Length == 0)
                {
                    ComPortSourceClear();
                    InitFlag = InitFlags.Finished;
                    _ = SpinWait.SpinUntil(() => CancellationTokenSource.IsCancellationRequested, 3000);
                }

                foreach (string com in portNames)
                {
                    if (!ComPortSource.Contains(com))
                    {
                        ComPortSourceAdd(com);
                    }
                }

                InitFlag = InitFlags.Finished;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// ComPort 數量
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            return ComPortSource.Count;
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
    /// 光源控制器物件
    /// </summary>
    [Obsolete("deprecate object")]
    public class LightController : INotifyPropertyChanged
    {
        private int _channelNumber;
        private SerialPort _serialPort;
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

        public string ComPort { get; set; }

        /// <summary>
        /// ComPort 是否開啟
        /// </summary>
        public bool IsComOpen => _serialPort != null && _serialPort.IsOpen;

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
        public bool ComOpen(string com)
        {
            ComPort = com;
            _serialPort = new SerialPort(ComPort, 9600, Parity.None, 8, StopBits.One);
            _serialPort.Open();
            bool success = Ping();
            OnPropertyChanged(nameof(IsComOpen));
            return success;
        }

        /// <summary>
        /// 開啟 COM
        /// </summary>
        /// <param name="com"></param>
        /// <param name="baudRate"></param>
        /// <param name="parity"></param>
        /// <param name="dataBits"></param>
        /// <param name="stopBits"></param>
        /// <returns>Serial Port 連線狀態</returns>
        public bool ComOpen(string com, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            ComPort = com;
            _serialPort = new SerialPort(com, baudRate, parity, dataBits, stopBits);
            _serialPort.Open();
            bool success = Ping();
            OnPropertyChanged(nameof(IsComOpen));
            return success;
        }

        /// <summary>
        /// 開啟 COM
        /// </summary>
        public void ComOpen(int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            _serialPort = new SerialPort(ComPort, baudRate, parity, dataBits, stopBits);
            _serialPort.Open();
            OnPropertyChanged(nameof(IsComOpen));
        }

        /// <summary>
        /// 關閉 COM
        /// </summary>
        /// <returns>Serial Port 連線狀態</returns>
        public void ComClose()
        {
            if (IsComOpen)
            {
                _serialPort.Close();
                OnPropertyChanged(nameof(IsComOpen));
            }
        }

        private bool Ping()
        {
            if (_serialPort.IsOpen)
            {
                string cmd = string.Empty;
                for (int i = 0; i < ChannelNumber; i++)
                {
                    cmd += $"{i},0,";
                }
                cmd = $"{cmd.TrimEnd(',')}\r\n";
                //Debug.WriteLine($"{cmd}");

                try
                {
                    _ = Write(cmd);
                    return true;
                }
                catch (TimeoutException)
                {
                    // Timeout 則直接關閉 Port
                    _serialPort.Close();
                    //throw;
                    return false;
                }
            }
            return false;
        }


        /// <summary>
        /// 歸零所有通道
        /// </summary>
        public void ResetAllValue()
        {
            if (IsComOpen)
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
        }

        /// <summary>
        /// 重置所有通道
        /// </summary>
        /// <returns>Error Message</returns>
        public string TryResetAllValue()
        {
            if (IsComOpen)
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
                    return $"重置光源設置失敗 {T.Message}";
                    //return new TimeoutException($"重置光源設置失敗: {T.Message}");
                }
                catch (Exception ex)
                {
                    return $"重置光源設置失敗 {ex.Message}";
                    //return new Exception($"重置光源設置失敗: {ex.Message}");
                }
                return string.Empty;
            }
            return "光源控制器 ComPort未開啟";
        }

        /// <summary>
        /// 設定通道光源大小,
        /// 後端使用
        /// </summary>
        /// <param name="ch">通道</param>
        /// <param name="value">目標設定值</param>
        public void SetChannelValue(int ch, ushort value)
        {
            if (0 < ch && ch <= ChannelNumber)
            {
                try
                {
                    string cmd = $"{ch},{value}\r\n";
                    _ = Write(cmd);     // 寫入控制器
                    Channels[ch - 1].Value = value;  // 變更通道值
                }
                catch (Exception)
                {
                    throw;
                }
            }
            else
            {
                throw new ArgumentException("指令的通道不存在");
            }
        }

        /// <summary>
        /// 取得通道光源大小，
        /// 後端使用
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
                _serialPort.ReadTimeout = timeout;
                _serialPort.Write(str);

                string ret = _serialPort.ReadLine();
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
#endif
}