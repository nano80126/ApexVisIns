
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
using MongoDB;
using MongoDB.Driver;
using MongoDB.Bson;
using MCAJawIns.Mongo;
using MCAJawInfo = MCAJawIns.Mongo.Info;

namespace MCAJawIns.content
{
    /// <summary>
    /// SystemInfoTab.xaml 的互動邏輯
    /// </summary>
    public partial class SystemInfoTab : StackPanel, INotifyPropertyChanged
    {
        #region Private
      
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

        /// <summary>
        /// 已初始化旗標 (確保程式初始化時不會重複觸發 System Info Timer)
        /// </summary>
        private bool inited;
        #endregion

        public SystemInfoTab()
        {
            InitializeComponent();

            MainWindow = (MainWindow)Application.Current.MainWindow;
        }

        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            if (inited)
            {
                GetSystemInfomation();
            }

            if (!loaded)
            {
                MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.APP, "系統資訊頁面已載入");
                inited = true;

                loaded = true;
            }
        }

        private void StackPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            SystemInfo.DisableTimer();
        }

        private void GetSystemInfomation()
        {

            SystemInfo.OS = $"{Environment.OSVersion.Version}";
            SystemInfo.SetPlateform(Environment.Is64BitProcess);
            SystemInfo.PID = Environment.ProcessId;
            SystemInfo.DotNetVer = $"{Environment.Version}";

            SystemInfo.PropertyChange();

            // if (inited && IsLoaded) { SystemInfo.EnableTimer(); }
            SystemInfo.EnableTimer();
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
            // SystemInfo.StartIdleWatch();
            // SystemInfo.GetAutoTimeInSeconds();
            int seconds = SystemInfo.GetTotalAutoTimeTnSeconds();

            Debug.WriteLine($"Seconds: {seconds}");

            Debug.WriteLine($"{SystemInfo.ToBsonDocument()}");
        }

        private void StopIdleTimer_Click(object sender, RoutedEventArgs e)
        {
            // SystemInfo.StopIdleWatch();

            MCAJawInfo info = new MCAJawInfo()
            {
                Type = MCAJawInfo.InfoTypes.System,
                Data = SystemInfo.ToBsonDocument(),
                InsertTime = DateTime.Now,
                UpdateTime = DateTime.Now
            };

            MainWindow.MongoAccess.InsertOne(nameof(JawCollection.Info), info);
        }
    }
}
