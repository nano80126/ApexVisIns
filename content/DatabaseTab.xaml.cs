using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
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
using ApexVisIns.Product;
using MongoDB.Bson;
using MongoDB.Driver;


namespace ApexVisIns.content
{
    /// <summary>
    /// DatabaseTab.xaml 的互動邏輯
    /// </summary>
    public partial class DatabaseTab : StackPanel
    {
        #region Variables

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
        public ObservableCollection<JawFullSpecIns> JawFullSpecInsCol { get; set; } = new();
        #endregion

        #region Flags
        /// <summary>
        /// 已載入旗標
        /// </summary>
        private bool loaded; 
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
            InitDateTimePickers();

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

        private void DateTimeFindBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                JawInspections.Clear();

                DateTime date = DatePicker.SelectedDate.Value;
                string[] stTime = StartTimePicker.SelectedItem.ToString().Split(':');
                string[] endTime = EndTimePicker.SelectedItem.ToString().Split(':');

                DateTime st = new(date.Year, date.Month, date.Day, Convert.ToInt32(stTime[0], CultureInfo.CurrentCulture), Convert.ToInt32(stTime[1], CultureInfo.CurrentCulture), 0);
                DateTime end = new(date.Year, date.Month, date.Day, Convert.ToInt32(endTime[0], CultureInfo.CurrentCulture), Convert.ToInt32(endTime[1], CultureInfo.CurrentCulture), 0);

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

        private void SearchResultsByLotNumber_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RecordHeader.Text = string.Empty;
                JawFullSpecInsCol.Clear();
                string lotNumber = (sender as Button).CommandParameter as string;

                FilterDefinition<JawFullSpecIns> filter = Builders<JawFullSpecIns>.Filter.Eq("LotNumber", lotNumber);

                MainWindow.MongoAccess.FindAll("Spec", filter, out List<JawFullSpecIns> data);


                if (data.Count > 0)
                {
                    RecordHeader.Text = $"{lotNumber} 檢驗紀錄";
                    foreach (JawFullSpecIns item in data)
                    {
                        JawFullSpecInsCol.Add(item);
                        // Debug.WriteLine($"{item.LotNumber} {item.DateTime.ToLocalTime()} {item.OK} {string.Join(",", item.Results.Keys)}");
                    }
                }
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.DATABASE, ex.Message);
            }

        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {

            if (JawFullSpecInsCol.Count < 10)
            {

                JawFullSpecInsCol.Add(new JawFullSpecIns
                {
                    LotNumber = "123",
                    OK = false,
                    DateTime = DateTime.Now,
                    Results = new Dictionary<string, double>() {
                        { "0.088R", 0.001 }
                    }
                });
            } else
            {
                JawFullSpecInsCol.Clear();
            }




        }
    }
}
