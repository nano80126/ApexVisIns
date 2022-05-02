using Basler.Pylon;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Data;

namespace ApexVisIns
{
    /// <summary>
    /// Basler Camera Enumerator
    /// Basler 相機枚舉器
    /// </summary>
    public class CameraEnumer : LongLifeWorker, INotifyPropertyChanged
    {
        #region private
        private readonly object _camsSourceLock = new();
        private readonly object _deviceConfigsLock = new();
        private bool _deviceConfigSaved; 
        #endregion

        /// <summary>
        /// 目前連線之Camera Source
        /// </summary>
        public ObservableCollection<BaslerCamInfo> CamsSource { get; set; } = new ObservableCollection<BaslerCamInfo>();
        /// <summary>
        /// JSON FILE 儲存之CONFIG
        /// </summary>
        public ObservableCollection<DeviceConfig> DeviceConfigs { get; set; } = new ObservableCollection<DeviceConfig>();

        public bool DeviceCofingSaved
        {
            get => _deviceConfigSaved;
            set
            {
                if (value != _deviceConfigSaved)
                {
                    _deviceConfigSaved = value;
                    OnPropertyChanged();
                }
            }
        }

        #region CamsSource 操作
        /// <summary>
        /// 新增相機至 CamsSource
        /// </summary>
        /// <param name="info"></param>
        private void AddCamsSource(BaslerCamInfo info)
        {
            lock (_camsSourceLock)
            {
                CamsSource.Add(info);
            }
        }
        /// <summary>
        /// 從 CamsSource 移除指定相機
        /// </summary>
        /// <param name="info"></param>
        private void RemoveCamsSource(BaslerCamInfo info)
        {
            lock (_camsSourceLock)
            {
                CamsSource.Remove(info);
            }
        }
        /// <summary>
        /// 清除 CamsSource 集合
        /// </summary>
        private void ClearCamsSource()
        {
            lock (_camsSourceLock)
            {
                if (CamsSource.Count > 0)
                {
                    CamsSource.Clear();
                }
            }
        }
        #endregion

        #region DeviceConfigs 操作
        /// <summary>
        /// 新增 Config 至 DeviceConfig
        /// </summary>
        /// <param name="config"></param>
        private void AddDeviceConfigs(DeviceConfig config)
        {
            lock (_deviceConfigsLock)
            {
                DeviceConfigs.Add(config);
            }
        }
        /// <summary>
        /// 從 DeviceConfigs 移除指定物件
        /// </summary>
        /// <param name="config"></param>
        private void RemoveDeviceConfigs(DeviceConfig config)
        {
            lock (_deviceConfigsLock)
            {
                DeviceConfigs.Remove(config);
            }
        }
        /// <summary>
        /// 清空 DeviceConfigs
        /// </summary>
        private void ClearDeviceConfigs()
        {
            lock (_deviceConfigsLock)
            {
                if (DeviceConfigs.Count > 0)
                {
                    DeviceConfigs.Clear();
                }
            }
        }

        /// <summary>
        /// 變更 DeviceConfigs
        /// </summary>
        private void ChangeDeviceConfigs(int idx, string propertyName, object value)
        {
            lock (_deviceConfigsLock)
            {
                DeviceConfigs[idx].GetType().GetProperty(propertyName).SetValue(DeviceConfigs[idx], value);
            }
        }

        /// <summary>
        /// 全部相機設為 Online
        /// </summary>
        /// <param name="online"></param>
        private void AllSetOnlineDeviceConfigs(bool online)
        {
            lock (_deviceConfigsLock)
            {
                foreach (DeviceConfig cfg in DeviceConfigs)
                {
                    cfg.Online = online;
                }
            }
        }
        #endregion

        public override void WorkerStart()
        {
            BindingOperations.EnableCollectionSynchronization(CamsSource, _camsSourceLock);
            BindingOperations.EnableCollectionSynchronization(DeviceConfigs, _deviceConfigsLock);
            CamsSource.CollectionChanged += CamsSource_CollectionChanged;
            base.WorkerStart();
        }

        private void CamsSource_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            List<BaslerCamInfo> list;
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    // Get CamsSource 
                    list = (sender as ObservableCollection<BaslerCamInfo>).ToList();

                    //foreach (DeviceConfig dev in DeviceConfigs)
                    //{
                    //    if (list.Any(cam => cam.SerialNumber == dev.SerialNumber))
                    //    {
                    //        //ChangeDeviceConfigs();
                    //    }
                    //}
                    for (int i = 0; i < DeviceConfigs.Count; i++)
                    {
                        if (list.Any(cam => cam.SerialNumber == DeviceConfigs[i].SerialNumber))
                        {
                            ChangeDeviceConfigs(i, "Online", true);
                        }
                    }

                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    // Get CamsSource
                    list = (sender as ObservableCollection<BaslerCamInfo>).ToList();

                    // foreach (DeviceConfig dev in DeviceConfigs)
                    // {
                    //     if (!list.Any(cam => cam.SerialNumber == dev.SerialNumber))
                    //     {
                    //     }
                    // }
                    for (int i = 0; i < DeviceConfigs.Count; i++)
                    {
                        if (!list.Any(cam => cam.SerialNumber == DeviceConfigs[i].SerialNumber))
                        {
                            ChangeDeviceConfigs(i, "Online", false);
                        }
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    // CamsSource
                    // for (int i = 0; i < DeviceConfigs.Count; i++)
                    // {
                    //     ChangeDeviceConfigs(i, "Online", false);
                    // }
                    AllSetOnlineDeviceConfigs(false);
                    break;
            }
            // throw new NotImplementedException();
        }

        public override void WorkerEnd()
        {
            BindingOperations.DisableCollectionSynchronization(CamsSource);
            BindingOperations.DisableCollectionSynchronization(DeviceConfigs);
            CamsSource.CollectionChanged -= CamsSource_CollectionChanged;
            base.WorkerEnd();
        }
        /// <summary>
        /// 工作內容
        /// </summary>
        public override void DoWork()
        {
            try
            {
                List<ICameraInfo> cams = CameraFinder.Enumerate();

                if (cams.Count == 0)
                {
                    ClearCamsSource();                          // 清空 Cams Source
                    InitFlag = InitFlags.Finished;
                    _ = SpinWait.SpinUntil(() => CancellationTokenSource.IsCancellationRequested, 3000);  // 等待三秒
                    return;
                }

                // 循環 cams, 若不在 CamsSource 內, 則新增
                foreach (ICameraInfo info in cams)
                {
                    if (!CamsSource.Any(item => item.SerialNumber == info[CameraInfoKey.SerialNumber]))
                    {
                        BaslerCamInfo camInfo = new(info[CameraInfoKey.FriendlyName], info[CameraInfoKey.ModelName], info[CameraInfoKey.DeviceIpAddress], info[CameraInfoKey.DeviceMacAddress], info[CameraInfoKey.SerialNumber])
                        {
                            VendorName = info[CameraInfoKey.VendorName],
                            CameraType = info[CameraInfoKey.DeviceType],
                            //DeviceVersion = info[CameraInfoKey.DeviceVersion],
                        };
                        AddCamsSource(camInfo);

                        // 需要變更
                        // 當有相機被移除
                        // CamsSourceRemove(camInfo);
                    }
                }

                // Cams Source Count > cams Count => 移除未連線之 BaslerInfo 
                // 確認一下功能
                if (CamsSource.Count > cams.Count)
                {
                    for (int i = 0; i < CamsSource.Count; i++)
                    {
                        if (!cams.Any(e => e[CameraInfoKey.SerialNumber] == CamsSource[i].SerialNumber))
                        {
                            RemoveCamsSource(CamsSource[i]);
                        }
                    }

                    // foreach (BaslerCamInfo camInfo in CamsSource)
                    // {
                    //     if (!cams.Any(e => e[CameraInfoKey.SerialNumber] == camInfo.SerialNumber))
                    //     {
                    //         RemoveCamsSource(camInfo);
                    //     }
                    // }
                }

                InitFlag = InitFlags.Finished;
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.CAMERA, ex.Message);
                // Debug.WriteLine(ex.Message);
            }
        }

        protected override void Dispose(bool disposing)
        {
            CamsSource.Clear();
            DeviceConfigs.Clear();
            base.Dispose(disposing);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
