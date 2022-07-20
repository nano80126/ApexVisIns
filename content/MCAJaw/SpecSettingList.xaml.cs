using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MCAJawIns.content
{
    /// <summary>
    /// SpecSettingList.xaml 的互動邏輯
    /// </summary>
    public partial class SpecSettingList : Border
    {

        #region Properties
        public MCAJaw MCAJaw { get; set; }
        #endregion


        /// <summary>
        /// JSON FILE 儲存路徑
        /// </summary>
        public string JsonPath { get; set; }

        public SpecSettingList()
        {
            InitializeComponent();
        }

        private void DockPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
            _ = (Window.GetWindow(this) as MainWindow).TitleGrid.Focus();
        }

        private void SpecSettingSave_Click(object sender, RoutedEventArgs e)
        {
            Product.JawSpecGroup jawSpecGroup = DataContext as Product.JawSpecGroup;


            string jsonStr = JsonSerializer.Serialize(jawSpecGroup.SpecList, new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString | System.Text.Json.Serialization.JsonNumberHandling.WriteAsString
            }); 

            Debug.WriteLine($"{jsonStr}");

            File.WriteAllText(JsonPath, jsonStr);

            _ = Task.Run(() => MCAJaw.LoadSpecList());
        }
    }
}
