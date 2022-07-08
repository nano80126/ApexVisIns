using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace MCAJawIns.Driver
{
    internal interface ISerialPortBase
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

        public void ComOpen(int baudRate, Parity parity, int dataBits, StopBits stopBits);

        public void ComClose();
        //public void Write(string cmd);
        //public string ReadLine();
    }


    public abstract class SerialPortBase : ISerialPortBase, INotifyPropertyChanged, IDisposable
    {
        #region Variables
        protected SerialPort _serialPort;
        protected int _timeout = 200;
        protected bool _disposed;
        #endregion

        public string ComPort { get; set; }

        public bool IsComOpen => _serialPort != null && _serialPort.IsOpen;

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
        /// 開啟 COM (預設 9600, N, 8, 1)
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
        protected virtual void Write(string cmd)
        {
            try
            {
                _serialPort.Write(cmd);
            }
            catch (Exception)
            {
                throw;
            }
            //throw new NotImplementedException();
        }

        protected virtual void Write(byte[] data)
        {
            try
            {
                _serialPort.Write(data, 0, data.Length);
            }
            catch (Exception)
            {
                throw;
            }
        }

        protected virtual void Read(byte[] buffer, out int length)
        {
            try
            {
                length = _serialPort.Read(buffer, 0, buffer.Length);
            }
            catch (Exception)
            {
                throw;
            }
        }

        protected virtual string ReadLine()
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Modbus 專用 CRC 計算
        /// </summary>
        /// <param name="dataBytes"></param>
        /// <returns></returns>
        protected byte[] CRC16LH(byte[] dataBytes)
        {
            ushort crc = 0xffff;
            ushort polynom = 0xA001;

            for (int i = 0; i < dataBytes.Length; i++)
            {
                crc ^= dataBytes[i];    // XOR
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x01) == 0x01)
                    {
                        crc >>= 1;
                        crc ^= polynom;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }

            byte[] result = BitConverter.GetBytes(crc);
            return result;
        }

        /// <summary>
        /// Modbus 專用 CRC 計算
        /// </summary>
        /// <param name="dataBytes"></param>
        /// <returns></returns>
        protected byte[] CRC16HL(byte[] dataBytes)
        {
            ushort crc = 0xffff;
            ushort polynom = 0xA001;

            for (int i = 0; i < dataBytes.Length; i++)
            {
                crc ^= dataBytes[i];
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x01) == 0x01)
                    {
                        crc >>= 1;
                        crc ^= polynom;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }

            byte[] result = BitConverter.GetBytes(crc).Reverse().ToArray();
            return result;
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
            if (!_disposed)
            {
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
}
