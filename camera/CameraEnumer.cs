using Basler.Pylon;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Data;

namespace MCAJawIns
{
    /// <summary>
    /// Basler Camera Enumerator
    /// Basler 相機枚舉器
    /// </summary>
    public class CameraEnumer : LongLifeWorker, INotifyPropertyChanged
    {
        #region Fields
        private readonly object _camsSourceLock = new();
        private readonly object _cameraConfigsLock = new();
        private bool _cameraConfigSaved;
        #endregion

        #region Properties
        /// <summary>
        /// 目前連線之Camera Source
        /// </summary>
        public ObservableCollection<CameraConfigBase> CamsSource { get; set; } = new ObservableCollection<CameraConfigBase>();
        /// <summary>
        /// JSON FILE 儲存之CONFIG
        /// </summary>
        public ObservableCollection<CameraConfig> CameraConfigs { get; set; } = new ObservableCollection<CameraConfig>();

        public bool CameraCofingSaved
        {
            get => _cameraConfigSaved;
            set
            {
                if (value != _cameraConfigSaved)
                {
                    _cameraConfigSaved = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion

        #region 建構子
        public CameraEnumer()
        {
            //CameraConfigs.CollectionChanged += CameraConfigs_CollectionChanged;
        } 
        #endregion

        #region CamsSource 操作
        /// <summary>
        /// 新增相機至 CamsSource
        /// </summary>
        /// <param name="info"></param>
        private void AddCamsSource(CameraConfigBase info)
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
        private void RemoveCamsSource(CameraConfigBase info)
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

        #region CameraConfigs 操作
        /// <summary>
        /// 新增 Config 至 CameraConfig
        /// </summary>
        /// <param name="config"></param>
        private void AddCameraConfigs(CameraConfig config)
        {
            lock (_cameraConfigsLock)
            {
                CameraConfigs.Add(config);
            }
        }

        /// <summary>
        /// 從 CameraConfigs 移除指定物件
        /// </summary>
        /// <param name="config"></param>
        private void RemoveCameraConfigs(CameraConfig config)
        {
            lock (_cameraConfigsLock)
            {
                CameraConfigs.Remove(config);
            }
        }

        /// <summary>
        /// 清空 CameraConfigs
        /// </summary>
        private void ClearCameraConfigs()
        {
            lock (_cameraConfigsLock)
            {
                if (CameraConfigs.Count > 0)
                {
                    CameraConfigs.Clear();
                }
            }
        }

        /// <summary>
        /// 變更 CameraConfigs
        /// </summary>
        private void ChangeCameraConfigs(int idx, string propertyName, object value)
        {
            lock (_cameraConfigsLock)
            {
                CameraConfigs[idx].GetType().GetProperty(propertyName).SetValue(CameraConfigs[idx], value);
            }
        }

        /// <summary>
        /// 全部相機設為 Online
        /// </summary>
        /// <param name="online"></param>
        private void SetAllCameraConfigsOnline(bool online)
        {
            lock (_cameraConfigsLock)
            {
                foreach (CameraConfig cfg in CameraConfigs)
                {
                    cfg.Online = online;
                }
            }
        }

        /// <summary>
        /// 儲存 Camera Config
        /// </summary>
        public void ConfigSave()
        {
            _cameraConfigSaved = true;
            OnPropertyChanged(nameof(CameraCofingSaved));
        }
        #endregion

        /// <summary>
        /// 工作開始
        /// </summary>
        public override void WorkerStart()
        {
            BindingOperations.EnableCollectionSynchronization(CamsSource, _camsSourceLock);
            BindingOperations.EnableCollectionSynchronization(CameraConfigs, _cameraConfigsLock);
            CamsSource.CollectionChanged += CamsSource_CollectionChanged;
            CameraConfigs.CollectionChanged += CameraConfigs_CollectionChanged;
            base.WorkerStart();
        }

        private void CamsSource_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            List<CameraConfigBase> list;
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    // Get CamsSource 
                    list = (sender as ObservableCollection<CameraConfigBase>).ToList();
                    for (int i = 0; i < CameraConfigs.Count; i++)
                    {
                        if (list.Any(cam => cam.SerialNumber == CameraConfigs[i].SerialNumber))
                        {
                            ChangeCameraConfigs(i, "Online", true);
                        }
                    }

                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    // Get CamsSource
                    list = (sender as ObservableCollection<CameraConfigBase>).ToList();

                    for (int i = 0; i < CameraConfigs.Count; i++)
                    {
                        if (!list.Any(cam => cam.SerialNumber == CameraConfigs[i].SerialNumber))
                        {
                            ChangeCameraConfigs(i, "Online", false);
                        }
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:

                    SetAllCameraConfigsOnline(false);
                    break;
            }
            // throw new NotImplementedException();
        }

        private void CameraConfigs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // // // // // // 
            //System.Diagnostics.Debug.WriteLine($"{e.Action}; {e.NewItems}; {e.OldItems} CameraEnumer.cs line 179");
            // // // // // // 

            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    CameraConfig newItem = e.NewItems[0] as CameraConfig;
                    //System.Diagnostics.Debug.WriteLine($"{newItem.VendorName} {newItem.Model} {newItem.Name}");
                    //newItem.PropertyChanged += Item_PropertyChanged;
                    newItem.BasicPropertyChanged += Item_BasicPropertyChanged;
                    newItem.LensConfig.PropertyChanged += LensConfig_PropertyChanged;
                    CameraCofingSaved = false;
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    CameraConfig oldItem = e.OldItems[0] as CameraConfig;
                    //System.Diagnostics.Debug.WriteLine($"{oldItem.VendorName} {oldItem.Model} {oldItem.Name}");
                    //oldItem.PropertyChanged -= Item_PropertyChanged;
                    oldItem.LensConfig.PropertyChanged -= LensConfig_PropertyChanged;
                    oldItem.BasicPropertyChanged -= Item_BasicPropertyChanged;
                    CameraCofingSaved = false;
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                default:
                    break;
            }
        }


        private void Item_BasicPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine(e.PropertyName);
            CameraCofingSaved = false;
        }

        private void LensConfig_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine($"{e.PropertyName} line: 259");
            CameraCofingSaved = false;
        }


        /// <summary>
        /// 工作結束
        /// </summary>
        public override void WorkerEnd()
        {
            BindingOperations.DisableCollectionSynchronization(CamsSource);
            BindingOperations.DisableCollectionSynchronization(CameraConfigs);
            CamsSource.CollectionChanged -= CamsSource_CollectionChanged;
            CameraConfigs.CollectionChanged -= CameraConfigs_CollectionChanged;
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
                        CameraConfigBase camInfo = new(info[CameraInfoKey.FriendlyName], info[CameraInfoKey.ModelName], info[CameraInfoKey.DeviceIpAddress], info[CameraInfoKey.DeviceMacAddress], info[CameraInfoKey.SerialNumber])
                        {
                            VendorName = info[CameraInfoKey.VendorName],
                            CameraType = info[CameraInfoKey.DeviceType],
                            // DeviceVersion = info[CameraInfoKey.DeviceVersion],
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
            CameraConfigs.Clear();
            base.Dispose(disposing);
        }

        #region Property Changed Event
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
