using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MCAJawIns
{
    interface ICustomSerial
    {
        /// <summary>
        /// COM 名稱
        /// </summary>
        public string ComPort { get; set; }

        /// <summary>
        /// COM 是否開啟
        /// </summary>
        public bool IsComOpen { get; }

        /// <summary>
        /// 讀取資料 Timeout
        /// </summary>
        public int Timeout { get; set; }

        public void ComOpen(string com);

        public void ComOpen(string com, int baudRate, Parity parity, int dataBits, StopBits stopBits);

        public void ComClose();

        public void Write(string cmd);

        public string ReadLine();
    }


    [Obsolete("use SerialPortBase instead")]
    public abstract class CustomSerial : ICustomSerial, INotifyPropertyChanged, IDisposable
    {
        #region Variables
        protected SerialPort _serialPort;
        protected int _timeout = 200;
        private bool _disposed;
        #endregion

        public string ComPort { get; set; }

        public bool IsComOpen { get => _serialPort != null && _serialPort.IsOpen; }

        public int Timeout
        {
            get => _timeout;
            set
            {
                _timeout = value;
                if (_serialPort != null)
                {
                    _serialPort.ReadTimeout = _timeout;
                }
            }
        }

        /// <summary>
        /// 開啟 COM
        /// </summary>
        /// <param name="com"></param>
        public virtual void ComOpen(string com)
        {
            ComPort = com;
            _serialPort = new SerialPort(ComPort, 9600, Parity.None, 8, StopBits.One)
            {
                ReadTimeout = Timeout
            };
            _serialPort.Open();
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
        public virtual void ComOpen(string com, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            ComPort = com;
            _serialPort = new SerialPort(ComPort, baudRate, parity, dataBits, stopBits)
            {
                ReadTimeout = Timeout
            };
            _serialPort.Open();
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
        public virtual void ComOpen(int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            _serialPort = new SerialPort(ComPort, baudRate, parity, dataBits, stopBits)
            {
                ReadTimeout = Timeout
            };
            _serialPort.Open();
            OnPropertyChanged(nameof(IsComOpen));
        }

        /// <summary>
        /// 關閉 COM
        /// </summary>
        public virtual void ComClose()
        {
            if (IsComOpen)
            {
                _serialPort.Close();
                OnPropertyChanged(nameof(IsComOpen));
            }
        }

        /// <summary>
        /// 寫入命令
        /// </summary>
        /// <param name="cmd"></param>
        public virtual void Write(string cmd)
        {
            throw new NotImplementedException();
        }

        public virtual string ReadLine()
        {
            throw new NotImplementedException();
        }

        public virtual void PropertyChange(string propertyName = null)
        {
            OnPropertyChanged(propertyName);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) { return; }

            if (disposing)
            {
                _serialPort.Close();
                _serialPort.Dispose();
                _serialPort = null;
            }

            _disposed = true;
        }
    }
}
