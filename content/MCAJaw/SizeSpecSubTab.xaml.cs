using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MCAJawIns.Product;
using MCAJawIns.Mongo;

using MongoDB.Driver;
using MongoDB.Bson;
using MCAJawConfig = MCAJawIns.Mongo.Config;
using System;

namespace MCAJawIns.content
{
    /// <summary>
    /// SpecSettingList.xaml 的互動邏輯
    /// </summary>
    public partial class SizeSpecSubTab : Border
    {
        #region Properties
        public MCAJaw MCAJaw { get; set; }
        #endregion

        /// <summary>
        /// JSON FILE 儲存路徑
        /// </summary>
        public string JsonPath { get; set; }

        public SizeSpecSubTab()
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

            string jsonStr = JsonSerializer.Serialize(specList.Source, new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString | System.Text.Json.Serialization.JsonNumberHandling.WriteAsString,
                WriteIndented = true,
            });

            File.WriteAllText(JsonPath, jsonStr);

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

            _ = Task.Run(() => MCAJaw.LoadSpecList());
        }
    }
}
