using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ApexVisIns
{
    public class ModbusTCPIO : INotifyPropertyChanged, IDisposable
    {

        #region private fields
        private bool _disposed;

        private TcpClient _tcpClient;
        private NetworkStream _networkStream;

        private System.Timers.Timer _pollingTimer;
        private double _interval = 100;
        private byte _value;
        #endregion

        public ModbusTCPIO() { }

        public ModbusTCPIO(string iP, int port, double interval)
        {
            IP = iP;
            Port = port;
            Interval = interval;
        }

        #region Properties
        /// <summary>
        /// 控制器 IP
        /// </summary>
        public string IP { get; set; }
        /// <summary>
        /// 控制器 Port
        /// </summary>
        public int Port { get; set; }
        /// <summary>
        /// IO 控制器是否連線
        /// </summary>
        public bool Conneected => _tcpClient != null && _tcpClient.Connected;
        /// <summary>
        /// IO 讀取間隔
        /// </summary>
        public double Interval
        {
            get => _interval;
            set
            {
                if (value != _interval)
                {
                    _pollingTimer.Stop();
                    _pollingTimer.Interval = value;
                    _pollingTimer.Start();

                    _interval = value;
                    OnPropertyChanged();
                }
            }
        }

        public byte Value
        {
            get => _value;
            private set
            {
                _value = value;
            }
        }
        #endregion

        /// <summary>
        /// 與 IO 控制器連線
        /// </summary>
        /// <returns>IO控制器連線狀態</returns>
        public bool Connect()
        {
            if (_tcpClient == null)
            {
                _tcpClient = new TcpClient(IP, Port);
                _networkStream = _tcpClient.GetStream();

                _pollingTimer = new System.Timers.Timer()
                {
                    AutoReset = true,
                    Interval = 50,
                    Enabled = true,
                };
                _pollingTimer.Elapsed += PollingTimer_Elapsed;
            }
            OnPropertyChanged(nameof(Conneected));
            return _tcpClient.Connected;
        }

        private void PollingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            ReadIO();
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 暫停讀取 IO
        /// </summary>
        private void Pause()
        {
            _pollingTimer.Stop();
        }

        /// <summary>
        /// 恢復讀取 IO
        /// </summary>
        private void Resume()
        {
            _pollingTimer.Start();
        }

        /// <summary>
        /// 與 IO 控制器斷線
        /// </summary>
        /// <returns>IO控制器連線狀態</returns>
        public bool Disconnect()
        {
            if (_pollingTimer.Enabled)
            {
                _pollingTimer.Stop();
                _pollingTimer.Dispose();
            }

            // 確認 TcpClient 不為 null 且韋連線狀態
            if (_tcpClient != null && _tcpClient.Connected)
            {
                _networkStream.Close();
                _tcpClient.Close();

                _networkStream.Dispose();
                _tcpClient.Dispose();
            }
            _pollingTimer = null;
            _networkStream = null;
            _tcpClient = null;
            OnPropertyChanged(nameof(Conneected));
            return false;
        }

        /// <summary>
        /// 讀取 IO
        /// </summary>
        private void ReadIO()
        {
            byte[] msg = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x06,
                0x01, 0x02, 0x00, 0x00, 0x00, 0x04 }; // 站號 1；讀取 Coil；0x00 0x00 開始；長度 0x04；

            if (_networkStream.CanWrite)
            {
                _networkStream.Write(msg, 0, msg.Length);

                byte[] data = new byte[16];
                int bytes = _networkStream.Read(data, 0, data.Length);

                if (data[bytes - 1] != Value)
                {
                    Value = data[bytes - 1];
                    OnIOChanged(Value);
                }
            }
            else
            {
                throw new WISE4050Exception("Network Stream 無法寫入");
            }
        }

        public delegate void IOChangedEventHandler(object sender, IOChangedEventArgs e);

        public event IOChangedEventHandler IOChanged;

        public class IOChangedEventArgs : EventArgs
        {
            public bool DI0 => (Value & 0b01) == 0b01;
            public bool DI1 => ((Value >> 1) & 0b01) == 0b01;
            public bool DI2 => ((Value >> 2) & 0b01) == 0b01;
            public bool DI3 => ((Value >> 3) & 0b01) == 0b01;

            public byte Value { get; }
            public IOChangedEventArgs(byte value)
            {
                Value = value;
            }
        }

        protected void OnIOChanged(byte value)
        {
            IOChanged?.Invoke(this, new IOChangedEventArgs(value));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) { return; }

            if (disposing)
            {
                _pollingTimer.Stop();
                _pollingTimer.Dispose();
                _pollingTimer = null;

                _networkStream.Close();
                _networkStream.Dispose();
                _networkStream = null;

                _tcpClient.Close();
                _tcpClient.Dispose();
                _tcpClient = null;
            }
            _disposed = true;
        }
    }
}
