using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using MCAJawIns.Mongo;
using MCAJawIns.Product;
using MongoDB.Bson;
using MongoDB.Driver;
using MCAJawConfig = MCAJawIns.Mongo.Config;

namespace MCAJawIns.Tab
{
    /// <summary>
    /// SpecSettingList.xaml 的互動邏輯
    /// </summary>
    public partial class SizeSpecSubTab : Border
    {
        #region Private
        //private SolidColorBrush[] ColorArray;
        #endregion

        #region Properties
        public MCAJaw MCAJaw { get; set; }
        #endregion


        /// <summary>
        /// JSON FILE 儲存路徑
        /// </summary>
        public string JsonDirectory { get; set; }

        public SizeSpecSubTab()
        {
            InitializeComponent();
        }

        private void Border_Loaded(object sender, RoutedEventArgs e)
        {
            //ColorArray = GroupItems.FindResource("ColorArray") as SolidColorBrush[];

            //foreach (SolidColorBrush item in ColorArray)
            //{
            //    System.Diagnostics.Debug.WriteLine($"{item.Color}");
            //}
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
            _ = (Window.GetWindow(this) as MainWindow).TitleGrid.Focus();
        }


        private void SpecSettingSave_Click(object sender, RoutedEventArgs e)
        {
#if false
            JawResultGroup group = DataContext as JawResultGroup;
            string jsonStr = JsonSerializer.Serialize(group.SizeSpecList, new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString | System.Text.Json.Serialization.JsonNumberHandling.WriteAsString
            }); 
#endif

            JawSizeSpecList specList = DataContext as JawSizeSpecList;

            #region 寫入本地 JSON
            string jsonStr = JsonSerializer.Serialize(specList.Source, new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString,
                WriteIndented = true,
            });
            File.WriteAllText(@$"{JsonDirectory}\MCAJaw.json", jsonStr);
            #endregion

            #region 寫入資料庫
            try
            {
                BsonArray bsonArray = new BsonArray(specList.Source.Count);
                foreach (JawSpecSetting item in specList.Source)
                {
                    _ = bsonArray.Add(item.ToBsonDocument());
                }

                FilterDefinition<MCAJawConfig> filter = Builders<MCAJawConfig>.Filter.Eq(nameof(MCAJawConfig.Type), nameof(MCAJawConfig.ConfigType.SPEC));
                UpdateDefinition<MCAJawConfig> update = Builders<MCAJawConfig>.Update
                    .Set(nameof(MCAJawConfig.DataArray), bsonArray)
                    .Set(nameof(MCAJawConfig.UpdateTime), DateTime.Now)
                    .SetOnInsert(nameof(MCAJawConfig.InsertTime), DateTime.Now);

                _ = MainWindow.MongoAccess.UpsertOne(nameof(JawCollection.Configs), filter, update);
            }
            catch (MongoException ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.DATABASE, ex.Message);
            }
            #endregion

            specList.Save();
            _ = Task.Run(() => MCAJaw.LoadSpecList(true));
        }

        private void SpecSettingGroupSave_Click(object sender, RoutedEventArgs e)
        {

            JawSizeSpecList specList = DataContext as JawSizeSpecList;

            #region 寫入本地 JSON
            string jsonStr = JsonSerializer.Serialize(specList.Groups.ToArray(), new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true,
            });
            File.WriteAllText(@$"{JsonDirectory}\Group.json", jsonStr);
            #endregion

            #region 寫入資料庫
            try
            {
                BsonArray bsonArray = new BsonArray(specList.Groups.Count);

                foreach (JawSpecGroupSetting item in specList.Groups)
                {
                    _ = bsonArray.Add(item.ToBsonDocument());
                }

                FilterDefinition<MCAJawConfig> filter = Builders<MCAJawConfig>.Filter.Eq(nameof(MCAJawConfig.Type), nameof(MCAJawConfig.ConfigType.SPECGROUP));
                UpdateDefinition<MCAJawConfig> update = Builders<MCAJawConfig>.Update
                    .Set(nameof(MCAJawConfig.DataArray), bsonArray)
                    .Set(nameof(MCAJawConfig.UpdateTime), DateTime.Now)
                    .SetOnInsert(nameof(MCAJawConfig.InsertTime), DateTime.Now);

                _ = MainWindow.MongoAccess.UpsertOne(nameof(JawCollection.Configs), filter, update);
            }
            catch (MongoException ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.DATABASE, ex.Message);
            }
            #endregion

            specList.GroupSave();
            // 不用重新載入，因 Group 設定不會影響批次結果
        }

        /// <summary>
        /// 重置 Combobox (選擇第一個 item)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboBox_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            comboBox.SelectedItem = comboBox.Items[0];
        }
    }
}
