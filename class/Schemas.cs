using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MCAJawIns.Mongo
{
    /// <summary>
    /// 應用程式使用資料庫名稱
    /// </summary>
    public enum JawCollection
    {
        /// <summary>
        /// 記錄使用者權限
        /// </summary>
        Auth,
        /// <summary>
        /// 紀錄各項設定
        /// </summary>
        Configs,
        /// <summary>
        /// 紀錄系統資訊
        /// </summary>
        Info,
        /// <summary>
        /// 紀錄批次資料
        /// </summary>
        Lots,
        /// <summary>
        /// 紀錄量測資料
        /// </summary>
        Measurements
    }


    public enum AuthRoles
    {
        /// <summary>
        /// 一般使用者
        /// </summary>
        User,
        /// <summary>
        /// 品保
        /// </summary>
        Quaiity,
        /// <summary>
        /// 工程師
        /// </summary>
        Engineer,
        /// <summary>
        /// 管理者
        /// </summary>
        Admin
    }

    /// <summary>
    /// 應用程式權限
    /// </summary>
    public class AuthLevel
    {
        [BsonId]
        public ObjectId ObjID { get; set; }

        /// <summary>
        /// 角色
        /// </summary>
        [BsonElement(nameof(Role))]
        public string Role { get; set; }

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
            /// 規格分組
            /// </summary>
            SPECGROUP,
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
        public BsonArray DataArray { get; set; } = new BsonArray();

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
        public BsonDocument Data { get; set; } = null;

        /// <summary>
        /// Bson Array 資料
        /// </summary>
        [BsonElement(nameof(DataArray))]
        public BsonArray DataArray { get; set; } = new BsonArray();

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
}
