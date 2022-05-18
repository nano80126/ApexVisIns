using System;
using System.Collections.Generic;
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
        /// Informer 物件
        /// </summary>
        //public MsgInformer MsgInformer { get; set; }
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
                MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.APP, "履歷頁面已載入");
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
                DateTime date = DatePicker.SelectedDate.Value;
                string[] stTime = StartTimePicker.SelectedItem.ToString().Split(':');
                string[] endTime = EndTimePicker.SelectedItem.ToString().Split(':');

                DateTime st = new(date.Year, date.Month, date.Day, Convert.ToInt32(stTime[0], CultureInfo.CurrentCulture), Convert.ToInt32(stTime[1], CultureInfo.CurrentCulture), 0);
                DateTime end = new(date.Year, date.Month, date.Day, Convert.ToInt32(endTime[0], CultureInfo.CurrentCulture), Convert.ToInt32(endTime[1], CultureInfo.CurrentCulture), 0);

                FilterDefinition<JawInspection> filter = Builders<JawInspection>.Filter.Gt(s => s.DateTime, st);
                filter &= Builders<JawInspection>.Filter.Lt(s => s.DateTime, end);

                MainWindow.MongoAccess.FindOne("Lots", filter, out JawInspection ins);

                if (ins != null) Debug.WriteLine($"{ins.LotNumber} {ins.DateTime.ToLocalTime()}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void LotNumberFindBtn_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
