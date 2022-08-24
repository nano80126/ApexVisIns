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


namespace MCAJawIns.content
{
    /// <summary>
    /// SystemInfoTab.xaml 的互動邏輯
    /// </summary>
    public partial class SystemInfoTab : StackPanel, INotifyPropertyChanged
    {
        #region Private
        private bool _x64;


        #endregion

        #region Properties
        /// <summary>
        /// 主視窗物件
        /// </summary>
        public MainWindow MainWindow { get; set; }

        // First
        public string OS { get; set; }
        public int PID { get; set; }
        public string Plateform => _x64 ? "64位元" : "32位元";
        public string DotNetVer { get; set; }
        public string MongoVer { get; set; }
        public string SystemTime => $"{DateTime.Now:HH:mm:ss}";

        // Second
        public string SoftVer { get; set; } = "1.0.0";
        public bool AutoStatus { get; set; } = false;
        public string AutoTime { get; set; }
        public string TotalAutoTime { get; set; }
        public string TotalParts { get; set; }

        #endregion

        public SystemInfoTab()
        {
            InitializeComponent();
        }


        #region Property Changed Event
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"Plateform {Environment.OSVersion.Platform}");
            Debug.WriteLine($"Version {Environment.OSVersion.Version}");

            Debug.WriteLine($"Version {Environment.OSVersion.Version.Major}");
            Debug.WriteLine($"Version {Environment.OSVersion.Version.Minor}");
            Debug.WriteLine($"Version {Environment.OSVersion.Version.Build}");
            Debug.WriteLine($"Version {Environment.OSVersion.Version.Revision}");

            Debug.WriteLine($"{Environment.OSVersion.VersionString}");
            // Debug.WriteLine($"{Environment.OSVersion.ServicePack}");

            Debug.WriteLine($"PID {Environment.ProcessId}");
            Debug.WriteLine($"x64 {Environment.Is64BitProcess}");

            Debug.WriteLine($"x64 {Environment.MachineName}");
            Debug.WriteLine($".NET {Environment.Version}");
            Debug.WriteLine($"-------------------------------------------------");
        }

        private void StackPanel_Unloaded(object sender, RoutedEventArgs e)
        {


        }
    }
}
