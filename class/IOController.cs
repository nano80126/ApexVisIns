using System;
using System.Collections;
using System.Collections.Generic;
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
        private BitArray _bitArray0 = new BitArray(8);
        private BitArray _bitArray1 = new BitArray(8);
        private BitArray _bitArray2 = new BitArray(8);
        private BitArray _bitArray3 = new BitArray(8);
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
        /// 初始化 Instance
        /// </summary>
        public void Initialize()
        {
            if (_description != string.Empty)
            {
                InstantDiCtrl = new InstantDiCtrl()
                {
                    SelectedDevice = new DeviceInformation(_description)
                };
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

        /// <summary>
        /// Digital Input Port#0, Bit0 ~ Bit7
        /// </summary>
        public BitArray DiArray0
        {
            get => _bitArray0;
        }

        /// <summary>
        /// Digital Input Port#1, Bit8 ~ Bit15
        /// </summary>
        public BitArray DiArray1
        {
            get => _bitArray1;
        }

        /// <summary>
        /// Digital Input Port#2, Bit16 ~ Bit23
        /// </summary>
        public BitArray DiArray2
        {
            get => _bitArray2;
        }

        /// <summary>
        /// Digital Input Port#3, Bit24 ~ Bit31
        /// </summary>
        public BitArray DiArray3
        {
            get => _bitArray3;
        }

        public void Read()
        {
            byte[] data = new byte[16];

            InstantDiCtrl.Read(0, 2, data);

            Debug.WriteLine($"{string.Join(',', data)}");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void PropertyChange(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
