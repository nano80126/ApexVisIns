using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace ApexVisIns.content
{
    /// <summary>
    /// DatabaseTab.xaml 的互動邏輯
    /// </summary>
    public partial class DatabaseTab : StackPanel
    {
        public DatabaseTab()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 履歷 Tab 載入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            InitDateTimePickers();

            MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.APP, "履歷頁面已載入");
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
    }
}
