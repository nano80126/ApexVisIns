using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Basler.Pylon;

namespace MCAJawIns
{
    public abstract class LongLifeWorker : IDisposable
    {
        // 避免多餘呼叫
        private bool _disposed;

        protected Task Worker { get; set; }

        /// <summary>
        /// Sampling interval
        /// </summary>
        public int Interval { get; set; } = 1000;

        /// <summary>
        /// If worker paused flag
        /// </summary>
        public bool Paused { get; set; }

        /// <summary>
        /// 初始化旗標 Enum
        /// </summary>
        public enum InitFlags
        {
            Starting = 0,
            Interrupt = 1,
            Finished = 2,
        }

        /// <summary>
        /// 初始化旗標
        /// </summary>
        public InitFlags InitFlag { get; protected set; } = InitFlags.Starting;

        /// <summary>
        /// If worker completed flag
        /// </summary>
        public bool Completed => Worker != null && Worker.IsCompleted;

        /// <summary>
        /// Cancel worker token
        /// </summary>
        protected CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();

        [Obsolete("deprecated")]
        public virtual void Initialize()
        {
            CancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Worker Start
        /// </summary>
        public virtual void WorkerStart()
        {
            if (Worker == null)
            {
                Worker = Task.Factory.StartNew(() =>
                {
                    while (!CancellationTokenSource.IsCancellationRequested)
                    {
                        SpinWait.SpinUntil(() => !Paused || CancellationTokenSource.IsCancellationRequested);
                        if (CancellationTokenSource.IsCancellationRequested)
                        {
                            break;
                        }

                        DoWork();
                        _ = SpinWait.SpinUntil(() => CancellationTokenSource.IsCancellationRequested, Interval);
                    }
                }, TaskCreationOptions.LongRunning);
            }
            else
            {
                throw new InvalidOperationException($"Worker is running, status: {Worker.Status}");
            }
        }

        /// <summary>
        /// Worker end
        /// </summary>
        public virtual void WorkerEnd()
        {
            if (!CancellationTokenSource.IsCancellationRequested)
            {
                CancellationTokenSource.Cancel();
            }
            //if (Worker != null)
            //{
            //    Worker.Wait();
            //    Worker.Dispose();
            //    Worker = null;
            //}
        }

        /// <summary>
        /// Worker resume
        /// </summary>
        public void WorkerResume()
        {
            Paused = false;
        }

        /// <summary>
        /// Worker pause
        /// </summary>
        public void WorkerPause()
        {
            Paused = true;
        }

        public virtual void DoWork()
        {
            try
            {
                throw new NotImplementedException("This method must be reimplemented");
            }
            catch (NotImplementedException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                Worker.Wait();
                Worker.Dispose();
                Worker = null;
            }

            _disposed = true;
        }
    }


    [Obsolete("No use in this project")]
    public class Thermometer : LongLifeWorker, INotifyPropertyChanged
    {
        private double _temperature;

        private SerialPort serialPort;

        /// <summary>
        /// Serial Port Opened
        /// </summary>
        public bool IsSerialPortOpen => serialPort != null && serialPort.IsOpen;

        /// <summary>
        /// 測得溫度
        /// </summary>
        public double Temperature
        {
            get => _temperature;
            set
            {
                if (value != _temperature)
                {
                    _temperature = value;
                    OnPropertyChanged(nameof(Temperature));
                }
            }
        }

        public void OpenSerialPort(string com = null)
        {
            string[] portNames = com == null ? SerialPort.GetPortNames() : new string[] { com };
            //Console.WriteLine(string.Join(", ", portNames));
            if (portNames.Length > 0)
            {
                serialPort = new SerialPort(portNames[0], 9600, Parity.Even, 8, StopBits.One);
                //serialPort.DataReceived += SerialPort_DataReceived;

                try
                {
                    serialPort.Open();

                    if (serialPort.IsOpen)
                    {
                        WorkerStart();
                        OnPropertyChanged(nameof(IsSerialPortOpen));
                    }
                }
                catch (Exception ex)
                {
                    // Display in message list
                    Debug.WriteLine($"Exception occurred : {ex.Message}");
                }
            }
            else
            {
                //Display in message list
                Debug.WriteLine("No serial port found");
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Console.WriteLine(serialPort.ReadBufferSize);
        }

        public void CloseSerialPort()
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.Close();
            }
            OnPropertyChanged(nameof(IsSerialPortOpen));
        }

        public override void WorkerEnd()
        {
            CloseSerialPort();
            base.WorkerEnd();
        }

        /// <summary>
        /// 工作內容
        /// </summary>
        public override void DoWork()
        {
            try
            {
                // Stop number: 0x01, Function code: 0x03 (Read Holding Registers)
                // Temperature address: 0x2000, Length: 0x0001, CRC: 0x8FCA
                byte[] data_write = new byte[] { 0x01, 0x03, 0x20, 0x00, 0x00, 0x01, 0x8F, 0xCA };

                serialPort.Write(data_write, 0, data_write.Length);

#if true
                byte[] data_read = new byte[7];
                _ = serialPort.Read(data_read, 0, data_read.Length);

                // (-32768 ~ 32767) / 10
                if ((data_read[3] & 0b10000000) == 0b10000000) // if bit7 is 1 
                {
                    Temperature = ((data_read[3] << 8) + data_read[4] - 65536) / 10.0;
                }
                else
                {
                    Temperature = ((data_read[3] << 8) + data_read[4]) / 10.0;
                } 
#endif

                //string[] array = Array.ConvertAll(data_read, b => b.ToString("X").PadLeft(2, '0'));
                //Console.WriteLine(string.Join(",", array));
            }
            catch (Exception ex)
            {
                // Display in message list
                Debug.WriteLine($"Thermometer exception occurred: {ex.Message}");

                // 寫入失敗，關閉通訊埠，終止worker
                // 新建Task，否則死鎖
                Task.Run(WorkerEnd);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

#if false
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
#endif
}