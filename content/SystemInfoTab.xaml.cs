using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using MaterialDesignThemes.Wpf;


namespace MCAJawIns.content
{
    /// <summary>
    /// SystemInfoTab.xaml 的互動邏輯
    /// </summary>
    public partial class SystemInfoTab : StackPanel, INotifyPropertyChanged
    {
        #region Private

        [Obsolete("測試用")]
        System.Timers.Timer tmr;
        #endregion

        #region Properties
        /// <summary>
        /// 主視窗物件
        /// </summary>
        public MainWindow MainWindow { get; set; }

        public SystemInfo SystemInfo { get; set; } = new SystemInfo();
        #endregion


        #region Flags
        /// <summary>
        /// 已載入旗標
        /// </summary>
        private bool loaded;
        #endregion

        public SystemInfoTab()
        {
            InitializeComponent();
        }


        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
#if false
            Debug.WriteLine($"Plateform {Environment.OSVersion.Platform}");
            Debug.WriteLine($"Version {Environment.OSVersion.Version}");

            Debug.WriteLine($"Version {Environment.OSVersion.Version.Major}");
            Debug.WriteLine($"Version {Environment.OSVersion.Version.Minor}");
            Debug.WriteLine($"Version {Environment.OSVersion.Version.Build}");
            Debug.WriteLine($"Version {Environment.OSVersion.Version.Revision}");

            Debug.WriteLine($"{Environment.OSVersion.VersionString}");

            Debug.WriteLine($"PID {Environment.ProcessId}");
            Debug.WriteLine($"x64 {Environment.Is64BitProcess}");

            Debug.WriteLine($"x64 {Environment.MachineName}");
            Debug.WriteLine($".NET {Environment.Version}");
            Debug.WriteLine($"-------------------------------------------------"); 
#endif
            GetSystemInfomation();

            if (!loaded)
            {
                MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.APP, "系統資訊頁面已載入");
                loaded = true;
            }
        }

        private void StackPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            SystemInfo.DisableTimer();
            //SystemInfo.StopIdleTimer();
        }

        private void GetSystemInfomation()
        {
            SystemInfo.OS = $"{Environment.OSVersion.Version}";
            SystemInfo.SetPlateform(Environment.Is64BitProcess);
            // SystemInfo. = Environment.Is64BitProcess;
            SystemInfo.PID = Environment.ProcessId;
            SystemInfo.DotNetVer = $"{Environment.Version}";

            //SystemInfo.SetMongoVersion($"{MainWindow.MongoAccess.GetVersion()}");
            SystemInfo.PropertyChange();

            if (IsFocused) { SystemInfo.EnableTimer(); }
            //SystemInfo.StartIdleTimer();
        }

        #region Property Changed Event
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        private void StartIdleTimer_Click(object sender, RoutedEventArgs e)
        {
            SystemInfo.StartIdleWatch();

            //if (tmr == null)
            //{
            //    tmr = new System.Timers.Timer
            //    {
            //        Interval = 5 * 1000,
            //        AutoReset = false
            //    };


            //    Debug.WriteLine($"{DateTime.Now:HH:mm:ss}");
            //    tmr.Elapsed += (sender, e) =>
            //    {
            //        Debug.WriteLine($"{DateTime.Now:HH:mm:ss}");
            //    };
            //    tmr.Start();
            //    //tmr.
            //}
            //else
            //{
            //    Debug.WriteLine($"{DateTime.Now:HH:mm:ss}");
            //    tmr.Stop();
            //    tmr.Start();
            //}
        }
        

        private void StopIdleTimer_Click(object sender, RoutedEventArgs e)
        {
            SystemInfo.StopIdleWatch();
            //tmr.Stop();
        }
    }
}
