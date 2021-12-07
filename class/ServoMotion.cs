using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Advantech.Motion;

namespace ApexVisIns
{
    public class ServoMotion : INotifyPropertyChanged
    {
        #region Variables
        //private uint Result;
        private IntPtr DeviceHandle = IntPtr.Zero;
        private IntPtr[] AxisHand = new IntPtr[8];
        
        private int _posCmd;
        private int _posFdbk;
        private string _currentStatus;
        private bool _deviceOpened;
        #endregion

        /// <summary>
        /// EtherCAT board 已開啟
        /// </summary>
        public bool DeviceOpened
        {
            get => _deviceOpened;
            set
            {
                if (value != _deviceOpened)
                {
                    _deviceOpened = value;
                    OnPropertyChanged(nameof(DeviceOpened));
                }
            }
        }

        public ObservableCollection<DeviceList> BoardList { get; } = new ObservableCollection<DeviceList>();

        public int PosCommand
        {
            get => _posCmd;
            set
            {
                if (value != _posCmd)
                {
                    _posCmd = value;
                    OnPropertyChanged(nameof(PosCommand));
                }
            }
        }

        public int PosFeedback
        {
            get => _posFdbk;
            set
            {
                if (value != _posFdbk)
                {
                    _posFdbk = value;
                    OnPropertyChanged(nameof(PosFeedback));
                }
            }
        }

        public AxisSignal ServoReady { get; set; } = new AxisSignal("SRDY", false);

        public AxisSignal ServoAlm { get; set; } = new AxisSignal("ALM", false);

        public AxisSignal LMTP { get; set; } = new AxisSignal("LMT+", false);

        public AxisSignal LMTN { get; set; } = new AxisSignal("LMT-", false);

        public AxisSignal SVON { get; set; } = new AxisSignal("SVON", false);

        public AxisSignal EMG { get; set; } = new AxisSignal("EMG", false);

        public string CurrentStatus
        {
            get => _currentStatus;
            set
            {
                if (value != _currentStatus)
                {
                    _currentStatus = value;
                    OnPropertyChanged(nameof(CurrentStatus));
                }
            }
        }

        /// <summary>
        /// 最大軸數
        /// </summary>
        public uint MaxAxisCount { get; private set; }

        /// <summary>
        /// DI 數量
        /// </summary>
        public uint DiCount { get; private set; }

        public ObservableCollection<string> Axes { get; } = new ObservableCollection<string>();

        /// <summary>
        /// 開啟 Board
        /// </summary>
        public void OpenDevice(uint deviceNum)
        {
            uint result;
            bool rescan = false;
            uint AxesPerDev = 0;
            uint DiChannel = 0;

            do
            {
                result = Motion.mAcm_DevOpen(deviceNum, ref DeviceHandle);
                if (result != (int)ErrorCode.SUCCESS)
                {

                }
                else
                {
                    rescan = false;
                }
            } while (rescan == true);

            result = Motion.mAcm_GetU32Property(DeviceHandle, (uint)PropertyID.FT_DevAxesCount, ref AxesPerDev);
            if (result != (int)ErrorCode.SUCCESS)
            {

                return;
            }
            MaxAxisCount = AxesPerDev;

            for (int i = 0; i < MaxAxisCount; i++)
            {
                result = Motion.mAcm_AxOpen(DeviceHandle, (ushort)i, ref AxisHand[i]);

                if (result != (int)ErrorCode.SUCCESS)
                {

                    return;
                }
                Axes.Add($"{i}-Axis");

                Debug.WriteLine($"Axis Count: {Axes.Count}");

                double cmdPosition = 0;
                Motion.mAcm_AxSetCmdPosition(AxisHand[i], cmdPosition);
            }

            result = Motion.mAcm_GetU32Property(DeviceHandle, (uint)PropertyID.FT_DaqDiMaxChan, ref DiChannel);
            if (result != (uint)ErrorCode.SUCCESS)
            {

            }
            for (int i = 0; i < DiChannel; i++)
            {
                Debug.WriteLine($"{i}");
            }
            Debug.WriteLine(DiChannel);

            DeviceOpened = true;
        }

        /// <summary>
        /// 關閉 Board
        /// </summary>
        public void CloseDevice()
        {
            ushort[] usAxisState = new ushort[MaxAxisCount];

            if (DeviceOpened)
            {
                // Get the axis's current state
                for (int i = 0; i < MaxAxisCount; i++)
                {
                    Motion.mAcm_AxGetState(AxisHand[i], ref usAxisState[i]);

                    if (usAxisState[i] == (uint)AxisState.STA_AX_ERROR_STOP)
                    {
                        // 若軸狀態為Error，重置軸狀態
                        Motion.mAcm_AxResetError(AxisHand[i]);
                    }
                    // 命令軸減速置停止
                    Motion.mAcm_AxStopDec(AxisHand[i]);
                }

                // Close Axes
                for (int i = 0; i < MaxAxisCount; i++)
                {
                    Motion.mAcm_AxClose(ref AxisHand[i]);
                }
                MaxAxisCount = 0;
                // Close Device
                Motion.mAcm_DevClose(ref DeviceHandle);
                DeviceHandle = IntPtr.Zero;
                DeviceOpened = false;


            }
        }

        /// <summary>
        /// 重置位置
        /// </summary>
        public void ResetPos()
        {


        }

        /// <summary>
        /// 重置錯誤
        /// </summary>
        public void ResetError()
        {

        }

        /// <summary>
        /// 軸 IO 狀態
        /// </summary>
        public struct AxisSignal
        {
            public AxisSignal(string name, bool bitOn)
            {
                this.Name = name;
                this.BitOn = bitOn;
            }

            /// <summary>
            /// 信號名稱
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// 信號 On / Off
            /// </summary>
            public bool BitOn { get; set; }
        }

        public class DeviceList
        {
            public DeviceList(DEV_LIST dev)
            {
                DeviceName = dev.DeviceName;
                DeviceNumber = dev.DeviceNum;
                NumOfSubDevice = dev.NumofSubDevice;
            }

            public DeviceList(string deviceName, uint deviceNnumber, short numOfSubDevice)
            {
                DeviceName = deviceName;
                DeviceNumber = deviceNnumber;
                NumOfSubDevice = numOfSubDevice;
            }

            public string DeviceName { get; }

            public uint DeviceNumber { get; }

            public short NumOfSubDevice { get; }
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
