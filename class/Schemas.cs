using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace MCAJawIns
{
    /// <summary>
    /// 應用程式使用資料庫名稱
    /// </summary>
    public enum JawCollection
    {
        Auth,
        Configs,
        Info,
        Lots,
        Measurements
    }

    /// <summary>
    /// 應用程式權限
    /// </summary>
    public class AuthLevel
    {
        [BsonId]
        public ObjectId ObjID { get; set; }

        /// <summary>
        /// 密碼
        /// </summary>
        [BsonElement(nameof(Password))]
        public string Password { get; set; }

        /// <summary>
        /// 等級
        /// </summary>
        [BsonElement(nameof(Level))]
        public int Level { get; set; }
    }


    /// <summary>
    /// 應用程式用組態
    /// </summary>
    public class Config
    {
        /// <summary>
        /// 組態類別
        /// </summary>
        public enum ConfigType
        {
            /// <summary>
            /// 相機
            /// </summary>
            CAMERA,
            /// <summary>
            /// 鏡頭
            /// </summary>
            LENS,
            /// <summary>
            /// 規格
            /// </summary>
            SPEC,
            /// <summary>
            /// 資料庫維護
            /// </summary>
            DATABASE,
        }

        [BsonId]
        public ObjectId ObjID { get; set; }

        /// <summary>
        /// 類別
        /// </summary>
        [BsonElement(nameof(Type))]
        public string Type { get; set; }

        /// <summary>
        /// 物件資料
        /// </summary>
        [BsonElement(nameof(Data))]
        public BsonDocument Data { get; set; } = null;

        /// <summary>
        /// 陣列資料
        /// </summary>
        [BsonElement(nameof(DataArray))]
        public BsonArray DataArray { get; set; } = null;

        /// <summary>
        /// 資料插入時間
        /// </summary>
        [BsonElement(nameof(InsertTime))]
        public DateTime InsertTime { get; set; }

        /// <summary>
        /// 資料更新時間
        /// </summary>
        [BsonElement(nameof(UpdateTime))]
        public DateTime UpdateTime { get; set; }
    }

    /// <summary>
    /// 應用程式資訊
    /// </summary>
    public class Info
    {
        public enum InfoTypes
        {
            /// <summary>
            /// 系統自動記錄之資料
            /// </summary>
            System,
            /// <summary>
            /// 使用者操作變更之資料
            /// </summary>
            User,
            /// <summary>
            /// Log 紀錄
            /// </summary>
            Log,
        }

        [BsonId]
        public ObjectId ObjID { get; set; }

        /// <summary>
        /// 資料類別
        /// </summary>
        [BsonElement(nameof(Type))]
        public InfoTypes Type { get; set; }

        /// <summary>
        /// Bson 資料
        /// </summary>
        [BsonElement(nameof(Data))]
        public BsonDocument Data { get; set; }

        /// <summary>
        /// Bson Array 資料
        /// </summary>
        [BsonElement(nameof(DataArray))]
        public BsonArray DataArray { get; set; }

        /// <summary>
        /// 資料插入時間
        /// </summary>
        [BsonElement(nameof(InsertTime))]
        public DateTime InsertTime { get; set; }

        /// <summary>
        /// 資料更新時間
        /// </summary>
        [BsonElement(nameof(UpdateTime))]
        public DateTime UpdateTime { get; set; }

#if false
        /// <summary>
        /// Int 型態資料
        /// </summary>
        [BsonElement(nameof(Numbers))]
        public Dictionary<string, int> Numbers;

        /// <summary>
        /// String 型態資料
        /// </summary>
        [BsonElement(nameof(Strings))]
        public Dictionary<string, string> Strings; 
#endif
    }
}
