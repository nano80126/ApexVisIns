using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace MCAJawIns.Driver
{
    public class TCPIPBase : INotifyPropertyChanged, IDisposable
    {
        #region private
        protected bool _disposed;
        protected TcpClient _tcpClient;
        protected NetworkStream _networkStream;

        protected Timer _pollingTimer;
        protected double _interval = 100;
        #endregion

        public TCPIPBase() { }

        public string IP { get; set; }

        public int Port { get; set; }

        public double Interval
        {
            get => _interval;
            set
            {
                if (value != _interval)
                {
                    _interval = value;

                    _pollingTimer.Stop();
                    _pollingTimer.Interval = _interval;
                    _pollingTimer.Start();

                    OnPropertyChanged();
                }
            }
        }

        public bool Conneected => _tcpClient != null && _tcpClient.Connected;

        public virtual void Connect(int timeout = 1500)
        {
            if (_tcpClient == null)
            {
                // _tcpClient = new TcpClient(IP, Port);
                _tcpClient = new TcpClient();
                //_tcpClient.Connect(IP, Port);
                if (!_tcpClient.ConnectAsync(IP, Port).Wait(timeout))
                {
                    throw new SocketException(10060);
                }

                _networkStream = _tcpClient.GetStream();
                _pollingTimer = new Timer()
                {
                    AutoReset = true,
                    Interval = 50,
                    Enabled = true,
                };
                _pollingTimer.Elapsed += PollingTimer_Elapsed; ;
            }
            OnPropertyChanged(nameof(Conneected));
        }

        protected virtual void PollingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public virtual void Disconnect()
        {
            if (_pollingTimer != null && _pollingTimer.Enabled)
            {
                _pollingTimer.Stop();
                _pollingTimer.Dispose();
            }

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
        }

        public virtual void Pause()
        {
            _pollingTimer.Stop();
        }
        public virtual void Resume()
        {
            _pollingTimer.Start();
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

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _pollingTimer.Stop();
                    _pollingTimer.Dispose();

                    _networkStream.Close();
                    _networkStream.Dispose();

                    _tcpClient.Close();
                    _tcpClient.Dispose();
                }
                _pollingTimer = null;
                _networkStream = null;
                _tcpClient = null;

                _disposed = true;
            }
        }
    }
}
