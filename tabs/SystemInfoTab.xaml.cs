using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using SystemInfo;
using MongoDB.Bson;

namespace MCAJawIns.Tab
{
    /// <summary>
    /// SystemInfoTab.xaml 的互動邏輯
    /// </summary>
    public partial class SystemInfoTab : StackPanel, INotifyPropertyChanged
    {
        #region Private
        [Obsolete]
        private string InformationPath { get; } = @"info.json";
        #endregion

        #region Properties
        /// <summary>
        /// 主視窗物件
        /// </summary>
        public MainWindow MainWindow { get; } = (MainWindow)Application.Current.MainWindow;

        public Env Env { get; set; } = new Env();

        public NetWorkInfoCollection NetworkInfos { get; set; } = new NetWorkInfoCollection();
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

            // Set value when init
            // MainWindow = (MainWindow)Application.Current.MainWindow;
            // 初始化路徑
            // InitInfoPath();
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
            //Env.DisableTimer();
        }

        private void GetSystemInfomation()
        {
            //SystemInfo.OS = $"{Environment.OSVersion.Version}";
            //SystemInfo.SetPlateform(Environment.Is64BitProcess);
            //SystemInfo.PID = Environment.ProcessId;
            //SystemInfo.DotNetVer = $"{Environment.Version}";

            Env.PropertyChange();

            // if (inited && IsLoaded) { SystemInfo.EnableTimer(); }
            Env.EnableTimer();
        }

        /// <summary>
        /// 
        /// </summary>
        [Obsolete("No demands create info path")]
        private void InitInfoPath()
        {
            string path = $@"{Directory.GetCurrentDirectory()}\{InformationPath}";

            if (!File.Exists(path))
            {
                _ = File.CreateText(path);
            }
        }

        #region Property Changed Event
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        [Obsolete]
        private void StartIdleTimer_Click(object sender, RoutedEventArgs e)
        {
            // SystemInfo.StartIdleWatch();
            // SystemInfo.GetAutoTimeInSeconds(); 
            //int seconds = SystemInfo.GetTotalAutoTimeTnSeconds();
            //Debug.WriteLine($"Seconds: {seconds}");
            Debug.WriteLine($"{Env.ToBsonDocument()}");

            string jsonStr = JsonSerializer.Serialize(Env, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            });

            File.WriteAllText(@$"{Directory.GetCurrentDirectory()}\info.json", jsonStr);
            Debug.WriteLine($"{jsonStr}");
        }

        [Obsolete]
        private void StopIdleTimer_Click(object sender, RoutedEventArgs e)
        {
            string path = $@"{Directory.GetCurrentDirectory()}\info.json";


            using StreamReader reader = File.OpenText(path);
            string jsonStr = reader.ReadToEnd();

            if (jsonStr != string.Empty)
            {
                SystemInfo systemInfo = JsonSerializer.Deserialize<SystemInfo>(jsonStr, new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                Debug.WriteLine($"{systemInfo.OS} {systemInfo.MongoVer}");
                Debug.WriteLine($"{systemInfo.Plateform} {systemInfo.AutoTime}");
                Debug.WriteLine($"{systemInfo.TotalHours} {systemInfo.TotalAutoTime}");
                Debug.WriteLine($"{systemInfo.SoftVer}");
                Debug.WriteLine($"{systemInfo.AutoTime}");
            }

            // SystemInfo.StopIdleWatch();
            // MCAJawInfo info = new MCAJawInfo()
            // {
            //     Type = MCAJawInfo.InfoTypes.System,
            //     Data = SystemInfo.ToBsonDocument(),
            //     InsertTime = DateTime.Now,
            //     UpdateTime = DateTime.Now
            // };

            // MainWindow.MongoAccess.InsertOne(nameof(JawCollection.Info), info);

        }
    }
}
