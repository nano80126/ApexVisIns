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
using System.Text.Json;
using System.IO;

namespace ApexVisIns.content
{
    /// <summary>
    /// SpecSettingList.xaml 的互動邏輯
    /// </summary>
    public partial class SpecSettingList : Border
    {
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

            #region
            //Debug.WriteLine($"{jawSpecGroup.Collection1.Count}");
            //Debug.WriteLine($"{jawSpecGroup.Collection2.Count}");
            //Debug.WriteLine($"{jawSpecGroup.Collection3.Count}");
            //foreach (Product.JawSpecSetting item in jawSpecGroup.SpecList)
            //{
            //    Debug.WriteLine($"{item.Item}");
            //}  
            #endregion 無用

            string jsonStr = JsonSerializer.Serialize(jawSpecGroup.SpecList, new JsonSerializerOptions
            {
                WriteIndented = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString | System.Text.Json.Serialization.JsonNumberHandling.WriteAsString
            });
            File.WriteAllText(JsonPath, jsonStr);
        }

     
    }
}
