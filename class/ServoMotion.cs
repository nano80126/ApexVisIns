using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using Advantech.Motion;

namespace ApexVisIns
{
    public class ServoMotion : INotifyPropertyChanged
    {
        #region Variables
        //private uint Result;
        private IntPtr DeviceHandle = IntPtr.Zero;
        private IntPtr[] AxisHandle = new IntPtr[8];

        private double _posCmd;
        private double _posAct;
        private string _currentStatus;
        private bool _deviceOpened;

        private Timer statusTimer;
        private int _sltAxis;
        #endregion

        /// <summary>
        /// EtherCAT board 開啟旗標
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

        public int SelectedAxis
        {
            get => _sltAxis;
            set
            {
                if (value != _sltAxis)
                {
                    _sltAxis = value;
                    OnPropertyChanged(nameof(SelectedAxis));
                }
            }
        }

        /// <summary>
        /// Status Timer 啟用旗標
        /// </summary>
        public bool StatusTimerOn
        {
            get => statusTimer.Enabled;
        }

        public ObservableCollection<DeviceList> BoardList { get; } = new ObservableCollection<DeviceList>();

        public double PosCommand
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

        public double PosActual
        {
            get => _posAct;
            set
            {
                if (value != _posAct)
                {
                    _posAct = value;
                    OnPropertyChanged(nameof(PosActual));
                }
            }
        }

        /// <summary>
        /// 伺服 Ready
        /// </summary>
        public AxisSignal ServoReady { get; set; } = new AxisSignal("SRDY");

        /// <summary>
        /// 伺服警報
        /// </summary>
        public AxisSignal ServoALM { get; set; } = new AxisSignal("ALM");

        /// <summary>
        /// Positive Limit Flag
        /// </summary>
        public AxisSignal LMTP { get; set; } = new AxisSignal("LMT+");

        /// <summary>
        /// Native Limit Flag
        /// </summary>
        public AxisSignal LMTN { get; set; } = new AxisSignal("LMT-");

        /// <summary>
        /// Servo On Flag
        /// </summary>
        public AxisSignal ServoOn { get; set; } = new AxisSignal("SVON");

        /// <summary>
        /// Servo Emergency Flag
        /// </summary>
        public AxisSignal ServoEMG { get; set; } = new AxisSignal("EMG");

        /// <summary>
        /// 當前軸狀態
        /// </summary>
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
            int retry = 0;          // 重試次數
            uint AxesCount = 0;     // 裝置軸數 
            uint DiChCount = 0;     // 裝置DI數

            if (!DeviceOpened)
            {
                bool rescanFlag;    // 重試 Flag
                do
                {
                    result = Motion.mAcm_DevOpen(deviceNum, ref DeviceHandle);
                    if (result != (int)ErrorCode.SUCCESS)
                    {
                        retry++;
                        rescanFlag = true;  // 重試Flag
                        if (retry > 5)
                        {
                            throw new Exception($"開啟 EtherCAT 卡失敗: Code[0x{result:X}]");
                        }
                    }
                    else
                    {
                        rescanFlag = false;
                    }
                } while (rescanFlag);

                // Get axis number of this device
                result = Motion.mAcm_GetU32Property(DeviceHandle, (uint)PropertyID.FT_DevAxesCount, ref AxesCount);
                if (result != (int)ErrorCode.SUCCESS)
                {
                    throw new Exception($"取得軸數量失敗: Code[0x{result:X}]");
                }
                MaxAxisCount = AxesCount;

                Axes.Clear();   // 清空軸數
                for (int i = 0; i < MaxAxisCount; i++)
                {
                    result = Motion.mAcm_AxOpen(DeviceHandle, (ushort)i, ref AxisHandle[i]);

                    if (result != (int)ErrorCode.SUCCESS)
                    {
                        throw new Exception($"開啟軸失敗: Code[0x{result:X}]");
                    }
                    Axes.Add($"{i}-Axis");

                    Debug.WriteLine($"Axis Count: {Axes.Count}");

                    // Set Commnad pos to 0
                    Motion.mAcm_AxSetCmdPosition(AxisHandle[i], 0);
                }

                // Get Di maximum number of this channel
                result = Motion.mAcm_GetU32Property(DeviceHandle, (uint)PropertyID.FT_DaqDiMaxChan, ref DiChCount);
                if (result != (uint)ErrorCode.SUCCESS)
                {
                    throw new Exception($"取得屬性 FT_DaqDiMaxChan 失敗: Code[0x{result:X}]");
                }

                // 這要幹麻? 
                for (int i = 0; i < DiChCount; i++)
                {
                    Debug.WriteLine($"{i}");
                }
                Debug.WriteLine(DiChCount);


                // 
                DeviceOpened = true;    // 裝置開啟旗標
                                        // 啟動 Timer 
            }
        }

        /// <summary>
        /// 關閉 Board
        /// </summary>
        public void CloseDevice()
        {
            // 紀錄軸狀態
            ushort[] AxisState = new ushort[MaxAxisCount];

            if (DeviceOpened)
            {
                // Get the axis's current state
                for (int i = 0; i < MaxAxisCount; i++)
                {
                    // 取得軸狀態
                    Motion.mAcm_AxGetState(AxisHandle[i], ref AxisState[i]);

                    if (AxisState[i] == (uint)Advantech.Motion.AxisState.STA_AX_ERROR_STOP)
                    {
                        // 若軸狀態為Error，重置軸狀態
                        Motion.mAcm_AxResetError(AxisHandle[i]);
                    }
                    // 命令軸減速至停止
                    Motion.mAcm_AxStopDec(AxisHandle[i]);
                }

                // Close Axes
                for (int i = 0; i < MaxAxisCount; i++)
                {
                    Motion.mAcm_AxClose(ref AxisHandle[i]);
                }
                MaxAxisCount = 0;
                // Close Device
                Motion.mAcm_DevClose(ref DeviceHandle);
                DeviceHandle = IntPtr.Zero; // 重置裝置 Handle
                DeviceOpened = false;       // 重置開啟旗標
            }
        }

        /// <summary>
        /// 啟用 Timer
        /// </summary>
        /// <param name="interval"></param>
        public void EnableTimer(int interval)
        {
            if (statusTimer == null)
            {
                statusTimer = new Timer(interval)
                {
                    AutoReset = true
                };

                statusTimer.Elapsed += StatusTimer_Elapsed;
                statusTimer.Start();
            }
            else
            {
                statusTimer.Interval = interval;
                statusTimer.Start();
            }
        }

        /// <summary>
        /// Timer tick event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StatusTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            GetMotionInformation();
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 關閉 Timer
        /// </summary>
        public void DisableTimer()
        {
            statusTimer?.Stop();
        }

        public void GetMotionInformation()
        {
            double cmd = 0;
            double pos = 0;
            ushort axState = 0;
            uint result;
            uint IOStatus = 0;

            if (DeviceOpened)
            {
                // Get current command position
                Motion.mAcm_AxGetCmdPosition(AxisHandle[0], ref cmd);
                // Get current actual position
                Motion.mAcm_AxGetActualPosition(AxisHandle[0], ref pos);

                PosCommand = cmd;
                PosActual = pos;

                result = Motion.mAcm_AxGetMotionIO(AxisHandle[0], ref IOStatus);

                if (result == (uint)ErrorCode.SUCCESS)
                {
                    SetMotionIOStatus(IOStatus);
                }
                // Get Axis current state
                Motion.mAcm_AxGetState(AxisHandle[0], ref axState);
                CurrentStatus = $"{(AxisState)axState}";
            }
        }
    
        /// <summary>
        /// 更新 Servo IO Status
        /// </summary>
        /// <param name="IOStatus"></param>
        private void SetMotionIOStatus(uint IOStatus)
        {
            ServoReady.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_RDY) == 0b01;

            ServoALM.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_RDY) == 0b01;

            LMTP.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_RDY) == 0b01;

            LMTN.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_RDY) == 0b01;

            ServoOn.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_RDY) == 0b01;

            ServoEMG.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_RDY) == 0b01;
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
        public class AxisSignal
        {
            public AxisSignal(string name)
            {
                Name = name;
                BitOn = false;
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
