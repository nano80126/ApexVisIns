using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
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
using MCAJawIns.Product;
using Microsoft.Win32;
using MongoDB.Bson;
using MongoDB.Driver;


namespace MCAJawIns.content
{
    /// <summary>
    /// DatabaseTab.xaml 的互動邏輯
    /// </summary>
    public partial class DatabaseTab : StackPanel, INotifyPropertyChanged
    {
        #region Variables
        private DateTime _startDate = DateTime.Today;
        private DateTime _endDate = DateTime.Today;
        private string _selectedLotNumber = string.Empty;
        #endregion

        #region Properties
        /// <summary>
        /// 主視窗物件
        /// </summary>
        public MainWindow MainWindow { get; set; }

        /// <summary>
        /// 批號查詢結果
        /// </summary>
        public ObservableCollection<JawInspection> JawInspections { get; set; } = new();

        /// <summary>
        /// 量測查詢結果
        /// </summary>
        public ObservableCollection<JawMeasurements> JawFullSpecInsCol { get; set; } = new();

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (value != _startDate)
                {
                    _startDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (value != _endDate)
                {
                    _endDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedLotNumber
        {
            get => _selectedLotNumber != string.Empty ? $"{_selectedLotNumber} 檢驗紀錄" : string.Empty;
            set
            {
                if (value != _selectedLotNumber)
                {
                    _selectedLotNumber = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion

        #region Flags
        /// <summary>
        /// 已載入旗標
        /// </summary>
        private bool loaded;

        /// <summary>
        /// 是否已經回收
        /// </summary>
        private bool recycled;
        #endregion

        public DatabaseTab()
        {
            InitializeComponent();

            MainWindow = (MainWindow)Application.Current.MainWindow;
        }

        /// <summary>
        /// 履歷 Tab 載入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDatabaseConfig(recycled);

            if (!loaded)
            {
                MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.APP, "資料庫頁面已載入");
                loaded = true;
            }
        }

        /// <summary>
        /// 履歷 Tab 卸載
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StackPanel_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// DatePicker & TimePicker 初始化
        /// </summary>
        [Obsolete("Deprecated")]
        private void InitDateTimePickers()
        {
            DatePicker.SelectedDate = DateTime.Today;

            if (StartTimePicker.Items.Count == 0 || EndTimePicker.Items.Count == 0)
            {
                StartTimePicker.Items.Clear();
                EndTimePicker.Items.Clear();

                DateTime st = DateTime.Today;
                for (int i = 0; i < 86400; i += 30 * 60)
                {
                    StartTimePicker.Items.Add($"{st.AddSeconds(i):HH:mm}");
                    EndTimePicker.Items.Add($"{st.AddSeconds(i):HH:mm}");
                }
                EndTimePicker.Items.Add($"{st.AddSeconds(86399):HH:mm}");

                StartTimePicker.SelectedIndex = 0;  // 00:00
                EndTimePicker.SelectedIndex = EndTimePicker.Items.Count - 1;    // 23:59
            }
        }

        /// <summary>
        /// Clear Focus 用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DockPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
            _ = (Window.GetWindow(this) as MainWindow).TitleGrid.Focus();
        }

        /// <summary>
        /// 重置選擇日期
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShortCutDatePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DateTime dt;
            int diff;
            switch ((sender as ListBox).SelectedIndex)
            {
                case 0:
                    dt = DateTime.Today;
                    StartDate = EndDate = dt;
                    break;
                case 1:
                    //Debug.WriteLine($"{DayOfWeek.s}");
                    diff = DayOfWeek.Sunday - DateTime.Today.DayOfWeek - 7;
                    StartDate = DateTime.Today.AddDays(diff).Date;
                    EndDate = DateTime.Today.AddDays(diff + 6).Date;
                    break;
                case 2:
                    diff = DayOfWeek.Sunday - DateTime.Today.DayOfWeek;
                    StartDate = DateTime.Today.AddDays(diff).Date;
                    EndDate = DateTime.Today.AddDays(diff + 6).Date;
                    break;
                case 3:
                    diff = DateTime.Today.Day;
                    dt = DateTime.Today.AddDays(-1 * diff + 1).Date;
                    StartDate = dt;
                    EndDate = dt.AddMonths(1).AddDays(-1);
                    break;
                default:
                    break;
            }
        }

        private void DatePicker_CalendarOpened(object sender, RoutedEventArgs e)
        {
            // 選擇最後一個 item (自訂)
            ShortCutDatePicker.SelectedItem = ShortCutDatePicker.Items[^1];
        }

        /// <summary>
        /// 日期區間搜尋
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DateTimeFindBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                JawInspections.Clear();

                //DateTime date = DatePicker.SelectedDate.Value;
                //string[] stTime = StartTimePicker.SelectedItem.ToString().Split(':');
                //string[] endTime = EndTimePicker.SelectedItem.ToString().Split(':');

                DateTime st = new(StartDate.Year, StartDate.Month, StartDate.Day, 0, 0, 0); // 00:00:00
                DateTime end = new(EndDate.Year, EndDate.Month, EndDate.Day, 23, 59, 59);   // 23:59:59

                // st < DateTime < end
                FilterDefinition<JawInspection> filter = Builders<JawInspection>.Filter.Gt(s => s.DateTime, st) & Builders<JawInspection>.Filter.Lt(s => s.DateTime, end);
                // filter &= Builders<JawInspection>.Filter.Lt(s => s.DateTime, end);

                MainWindow.MongoAccess.FindAll("Lots", filter, out List<JawInspection> data);

                foreach (JawInspection item in data)
                {
                    JawInspections.Add(item);
                    // Debug.WriteLine($"{item.ObjID} {item.DateTime}");
                }
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.DATABASE, ex.Message);
            }
        }

        /// <summary>
        /// 批號搜尋
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LotNumberFindBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                JawInspections.Clear();

                string lotNumber = LotNumberText.Text;

                FilterDefinition<JawInspection> filter = Builders<JawInspection>.Filter.Eq("LotNumber", lotNumber);

                MainWindow.MongoAccess.FindAll("Lots", filter, out List<JawInspection> data);

                foreach (JawInspection item in data)
                {
                    JawInspections.Add(item);
                    // Debug.WriteLine($"{item.LotNumber} {item.DateTime.ToLocalTime()}");
                }
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.DATABASE, ex.Message);
            }
        }

        /// <summary>
        /// 搜尋檢驗紀錄
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchResultsByLotNumber_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //RecordHeader.Text = string.Empty;
                SelectedLotNumber = string.Empty;
                JawFullSpecInsCol.Clear();
                string lotNumber = (sender as Button).CommandParameter as string;

                FilterDefinition<JawMeasurements> filter = Builders<JawMeasurements>.Filter.Eq("LotNumber", lotNumber);
                //& Builders<JawFullSpecIns>.Filter.;

                MainWindow.MongoAccess.FindAll("Measurements", filter, out List<JawMeasurements> data);

                //RecordHeader.Text = $"{lotNumber} 檢驗紀錄";
                SelectedLotNumber = lotNumber;
                if (data.Count > 0)
                {
                    SelectedLotNumber = lotNumber;
                    foreach (JawMeasurements item in data)
                    {
                        JawFullSpecInsCol.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.DATABASE, ex.Message);
            }
        }

        /// <summary>
        /// 輸出 .CSV
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ExportCSV_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new()
            {
                FileName = string.Empty,
                Filter = "CSV File(*.csv)|*.csv",
                InitialDirectory = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}"
            };


            if (saveFileDialog.ShowDialog() == true)
            {
                (sender as Button).IsEnabled = false;

                await Task.Run(() =>
                {
                    try
                    {
                        string path = saveFileDialog.FileName;
                        string lotNumber = _selectedLotNumber;

                        JawInspection header = JawInspections.First(e => e.LotNumber == lotNumber);

                        string time =
                                $"輸出日期,{DateTime.Now:yyyy:MM:dd}{Environment.NewLine}" +
                                $"輸出時間,{DateTime.Now:HH:mm:ss}{Environment.NewLine}{Environment.NewLine}";

                        string a = $"批號,{lotNumber}{Environment.NewLine}";
                        string b =
                                $"資料日期,{header.DateTime:yyyy:MM:dd}{Environment.NewLine}" +
                                $"資料時間,{header.DateTime:HH:mm:ss}{Environment.NewLine}";

                        string c = $"項目";
                        string d = $"數量";

                        foreach (string k in header.LotResults.Keys)
                        {
                            if (header.LotResults[k].Enable)
                            {
                                c += $",{header.LotResults[k].Name}";
                                d += $",{header.LotResults[k].Count}";
                            }
                        }

                        c += Environment.NewLine;
                        d += Environment.NewLine + Environment.NewLine;
                        // string e = Environment.NewLine;

                        string o = $"時間";
                        string p = string.Empty;

                        bool ResultHeaderAppended = false;
                        foreach (JawMeasurements item in JawFullSpecInsCol)
                        {
                            // 先轉 localtime
                            p += $"{item.DateTime.ToLocalTime():HH:mm:ss}";

                            // foreach (string key in item.Results.Keys)
                            foreach (string key in header.LotResults.Keys)
                            {
                                if (!item.Results.ContainsKey(key)) { continue; }
                                // if (key == "good") { continue; }

                                // 生成 Header
                                if (!ResultHeaderAppended) { o += $",{header.LotResults[key].Name}"; }
                                p += $",{item.Results[key]:f5}";
                            }

#if false
                            #region 待刪除
                            if (!ResultHeaderAppended) { o += $",平面度2"; }
                            p += $",{item.Results["flatness2"]:f5}";
                            #endregion  
#endif

                            ResultHeaderAppended = true;

                            p += (item.OK ? ",良品" : ",不良") + Environment.NewLine;
                        }
                        o += $",結果{Environment.NewLine}";

                        File.WriteAllText(path, time + a + b + c + d + o + p);
                    }
                    catch (Exception ex)
                    {
                        MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.APP, ex.Message);
                    }
                });

                (sender as Button).IsEnabled = true;
            }
        }

        /// <summary>
        /// 保留但不使用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            // if (JawFullSpecInsCol.Count < 10)
            // {

            //    JawFullSpecInsCol.Add(new JawFullSpecIns
            //    {
            //        LotNumber = "123",
            //        OK = false,
            //        DateTime = DateTime.Now,
            //        Results = new Dictionary<string, double>() {
            //            { "0.088R", 0.001 }
            //        }
            //    });
            //}
            //else
            //{
            //    JawFullSpecInsCol.Clear();
            //}
        }


        private void SaveDatabaseRecycle_Click(object sender, RoutedEventArgs e)
        {
            if (DataReserveSelector.SelectedIndex != -1)
            {
                ushort month = (ushort)DataReserveSelector.SelectedItem;

                try
                {
                    FilterDefinition<MCAJawConfig> filter = Builders<MCAJawConfig>.Filter.Empty;
                    UpdateDefinition<MCAJawConfig> update = Builders<MCAJawConfig>.Update.Set("DataReserveMonths", month);

                    UpdateResult result = MainWindow.MongoAccess.UpdateOne("Configs", filter, update);
                }
                catch (Exception ex)
                {
                    MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.DATABASE, ex.Message);
                }
            }
        }

        /// <summary>
        /// 載入資料庫設定
        /// </summary>
        private void LoadDatabaseConfig(bool recycling)
        {
            try
            {
                if (!MainWindow.MongoAccess.Connected) { return; }

                FilterDefinition<MCAJawConfig> filter = Builders<MCAJawConfig>.Filter.Empty;

                MainWindow.MongoAccess.FindOne("Configs", Builders<MCAJawConfig>.Filter.Empty, out MCAJawConfig config);

                if (config != null)
                {
                    DataReserveSelector.SelectedIndex = DataReserveSelector.Items.IndexOf(config.DataReserveMonths);
                }
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.DATABASE, ex.Message);
            }
        }

        /// <summary>
        /// 清除過期資料
        /// </summary>
        [Obsolete("deprecated, obsolete data has been deleted in MCAJaw.cs MongoInit()")]
        private void RecycleObsoleteData(ushort months)
        {
            try
            {
                // 清除舊資料
                // 清除舊資料

                DateTime dateTime = DateTime.Now.AddDays(months * -1);
                Debug.WriteLine($"{dateTime}");
                // Debug.WriteLine($"{dateTime.ToLocalTime()}");

                // 尋找在特定 filter 之前的資料
                FilterDefinition<JawInspection> filter = Builders<JawInspection>.Filter.Lt("DateTime", dateTime);

                DeleteResult result =  MainWindow.MongoAccess.DeleteMany("Lots", filter);


                //MainWindow.MongoAccess.FindAll("Lots", filter, out List<JawFullSpecIns> config);
                Debug.WriteLine($"configs: {result.DeletedCount}");

                // 設置已回收旗標
                recycled = true;
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.DATABASE, ex.Message);
            }
        }


        #region PropertyChanged 
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
