using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using MongoDB.Bson;
using MongoDB.Driver;


namespace MCAJawIns
{

    /// <summary>
    /// 連結 Mongo 資料庫
    /// </summary>
    public class MongoAccess : INotifyPropertyChanged, IDisposable
    {
        #region Private
        /// <summary>
        /// MongoDB Client
        /// </summary>
        private MongoClient client;
        private bool _disposed;
        private bool _connected;
        #endregion

        #region Public
        public string Host { get; set; }

        public int Port { get; set; }

        public string Database { get; private set; } = string.Empty;

        /// <summary>
        /// 是否連線
        /// </summary>
        public bool Connected
        {
            get => _connected;
            set
            {
                if (value != _connected)
                {
                    _connected = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion

        public MongoAccess()
        {

        }

        public MongoAccess(string host, int port)
        {
            Host = host;
            Port = port;
        }

        /// <summary>
        /// Mongo 連線，
        /// 僅用於沒有權限要求時
        /// </summary>
        [Obsolete("僅用於沒權有權限要求時測試用")]
        public void Connect()
        {
            if (client == null)
            {
                // MongoUrl url = new MongoUrl(mongoUrl);
                client = new MongoClient(new MongoClientSettings
                {
                    //Credential = MongoCredential.CreateCredential("fanuc", "root", "0000"),
                    Server = new MongoServerAddress(Host, Port),
                    DirectConnection = true
                });
            }
        }

        /// <summary>
        /// MongoDB 連線
        /// </summary>
        /// <param name="dbName">資料庫名稱</param>
        /// <param name="user">使用者</param>
        /// <param name="pwd">密碼</param>
        /// <param name="timeout">Timeout (ms)</param>
        /// <returns></returns>
        public void Connect(string dbName, string user, string pwd, double timeout = 5000)
        {
            try
            {
                client = new MongoClient(new MongoClientSettings
                {
                    //Credential = MongoCredential.CreateCredential("fanuc", "root", "0000"),
                    Credential = MongoCredential.CreateCredential(dbName, user, pwd),
                    Server = new MongoServerAddress(Host, Port),
                    DirectConnection = true,
                    //ConnectTimeout = new TimeSpan(0, 0, 0, 1, 500),
                    //SocketTimeout = new TimeSpan(0, 0, 0, 1, 500),
                    ServerSelectionTimeout = TimeSpan.FromMilliseconds(1500), // 選擇 Server timeout (嘗試連線時之timeout)
                });


                BsonDocument db = client.GetDatabase(dbName).RunCommand((Command<BsonDocument>)"{ping:1}");
                Database = dbName;
                Connected = true;
            }
            catch (MongoException)
            {
                // 若異常則斷線
                client.Cluster.Dispose();
                client = null;
                Database = string.Empty;
                Connected = false;
                throw;
            }
        }

        /// <summary>
        /// MongoDB 斷線
        /// </summary>
        public void Disconnect()
        {
            try
            {
                if (!Connected) { return; }

                if (client != null)
                {
                    client.Cluster.Dispose();
                    client = null;
                    Database = string.Empty;
                    Connected = false;
                }
            }
            catch (MongoException)
            {
                throw;
            }
        }

        /// <summary>
        /// 取得 Mongo 版本
        /// </summary>
        /// <returns></returns>
        public string GetVersion()
        {
            if (client != null)
            {
                BsonDocument info = client.GetDatabase(Database).RunCommand<BsonDocument>(new BsonDocument() { { "buildInfo", 1 } });
                return (string)info.GetValue("version");
            }
            else
            {
                throw new MongoException("MongoDB connection is not established");
            }
        }

        /// <summary>
        /// 建立 Collection
        /// </summary>
        /// <param name="collection">集合名稱</param>
        /// <returns>若有新增回傳 true; 否則回傳 false</returns>
        public bool CreateCollection(string collection)
        {
            try
            {
                IMongoDatabase db = client.GetDatabase(Database);
                // 先確認 Collection 是否存在，再新增
                bool exist = db.ListCollectionNames().ToList().Contains(collection);
                if (!exist)
                {
                    db.CreateCollection(collection);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (MongoException)
            {
                throw;
            }
        }

        /// <summary>
        /// 捨棄 Collection
        /// </summary>
        /// <param name="collection">集合名稱</param>
        public void DropCollection(string collection)
        {
            try
            {
                client.GetDatabase(Database).DropCollection(collection);
            }
            catch (MongoException)
            {
                throw;
            }
        }

        /// <summary>
        /// 新增一項索引
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cName"></param>
        /// <param name="indexModel"></param>
        public void CreateIndexOne<T>(string cName, CreateIndexModel<T> indexModel)
        {
            if (client != null)
            {
                IMongoDatabase db = client.GetDatabase(Database);
                IMongoCollection<T> collection = db.GetCollection<T>(cName);

                collection.Indexes.CreateOne(indexModel);
            }
            else
            {
                throw new MongoException("MongoDB connection is not established");
            }
        }

        /// <summary>
        /// 新增多項索引
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cName"></param>
        /// <param name="list"></param>
        public void CreateIndexMany<T>(string cName, List<CreateIndexModel<T>> list)
        {
            if (client != null)
            {
                IMongoDatabase db = client.GetDatabase(Database);
                IMongoCollection<T> collection = db.GetCollection<T>(cName);

                collection.Indexes.CreateMany(list);
            }
            else
            {
                throw new MongoException("MongoDB connection is not established");
            }
        }

        /// <summary>
        /// 取得索引
        /// </summary>
        /// <param name="cName"></param>
        public void GetIndexes<T>(string cName, out List<BsonDocument> list)
        {
            if (client != null)
            {
                IMongoDatabase db = client.GetDatabase(Database);
                IMongoCollection<T> collection = db.GetCollection<T>(cName);

                list = collection.Indexes.List().ToList();
            }
            else
            {
                throw new MongoException("MongoDB connection is not established");
            }
        }

        /// <summary>
        /// 列出所有 Database
        /// </summary>
        /// <param name="client">Mongo Client</param>
        public void ListDatabases(out List<string> dbNames)
        {
            try
            {
                dbNames = client.ListDatabaseNames().ToList();
            }
            catch (MongoException)
            {
                throw;
            }
        }

        /// <summary>
        /// 列出 Database 內所有 Collections
        /// </summary>
        /// <param name="client">Mongo Client</param>
        /// <param name="dbName">Database</param>
        public void ListCollections(out List<string> collections)
        {
            try
            {
                IMongoDatabase db = client.GetDatabase(Database);
                collections = db.ListCollectionNames().ToList();
            }
            catch (MongoException)
            {
                throw;
            }
        }

        /// <summary>
        /// 插入一筆 document
        /// </summary>
        /// <param name="client">Mongo Client</param>
        /// <param name="dbName">資料庫名稱</param>
        /// <param name="cName">集合名稱</param>
        /// <param name="item">物件 (Document)</param>
        public void InsertOne<T>(string cName, T item)
        {
            if (client != null)
            {
                IMongoDatabase db = client.GetDatabase(Database);
                IMongoCollection<T> collection = db.GetCollection<T>(cName);
                collection.InsertOne(item);
            }
            else
            {
                throw new MongoException("MongoDB connection is not established");
            }
        }

        /// <summary>
        /// 插入多筆 Documents (list)
        /// </summary>
        /// <param name="client">Mongo Client</param>
        /// <param name="dbName">資料庫名稱</param>
        /// <param name="cName">集合名稱</param>
        /// <param name="list">列表</param>
        public void InsertMany<T>(string cName, List<T> list)
        {
            if (client != null)
            {
                IMongoDatabase db = client.GetDatabase(Database);
                IMongoCollection<T> collection = db.GetCollection<T>(cName);
                collection.InsertMany(list);
            }
            else
            {
                throw new MongoException("MongoDB connection is not established");
            }
        }

        /// <summary>
        /// 插入多筆 Documents (array)
        /// </summary>
        /// <param name="client">Mongo Client</param>
        /// <param name="dbName">資料庫名稱</param>
        /// <param name="cName">集合名稱</param>
        /// <param name="list">列表</param>
        public void InsertMany<T>(string cName, T[] list)
        {
            if (client != null)
            {
                IMongoDatabase db = client.GetDatabase(Database);
                IMongoCollection<T> collection = db.GetCollection<T>(cName);
                collection.InsertMany(list);
            }
            else
            {
                throw new MongoException("MongoDB connection is not established");
            }
        }

        /// <summary>
        /// 搜尋第一筆符合條件 Document
        /// </summary>
        /// <param name="cName">集合名</param>
        /// <param name="filter">過濾器</param>
        /// <param name="data">(out) 資料</param>
        public void FindOne<T>(string cName, FilterDefinition<T> filter, out T data, bool first = true)
        {
            if (client != null)
            {
                IMongoDatabase db = client.GetDatabase(Database);
                IMongoCollection<T> collection = db.GetCollection<T>(cName);
                data = collection.Find(filter).FirstOrDefault();
            }
            else
            {
                throw new MongoException("MongoDB connection is not established");
            }
        }

        public void FindOneSort<T>(string cName, FilterDefinition<T> filter, SortDefinition<T> sort, out T data)
        {
            if (client != null)
            {
                IMongoDatabase db = client.GetDatabase(Database);
                IMongoCollection<T> collection = db.GetCollection<T>(cName);

                data = collection.Find(filter).Sort(sort).FirstOrDefault();
            }
            else
            {
                throw new MongoException("MongoDB connection is not established");
            }
        }

        /// <summary>
        /// 搜尋符合條件 Document
        /// </summary>
        /// <param name="cName">集合名</param>
        /// <param name="filter">過濾器</param>
        /// <param name="limit">搜尋比數</param>
        /// <param name="data">(out) 資料</param>
        public void FindMany<T>(string cName, FilterDefinition<T> filter, int limit, out List<T> data)
        {
            if (client != null)
            {
                IMongoDatabase db = client.GetDatabase(Database);
                IMongoCollection<T> collection = db.GetCollection<T>(cName);
                // FilterDefinition<T> filter = Builders<T>.Filter.Gt(field, dateTime.GetStartOfDay());
                data = collection.Find(filter).Limit(limit).ToList();
            }
            else
            {
                throw new MongoException("MongoDB connection is not established");
            }
        }

        /// <summary>
        /// 搜尋所有符合條件 Document
        /// </summary>
        /// <param name="cName">集合名</param>
        /// <param name="filter">過濾器</param>
        /// <param name="data">(out) 資料</param>
        public void FindAll<T>(string cName, FilterDefinition<T> filter, out List<T> data)
        {
            if (client != null)
            {
                IMongoDatabase db = client.GetDatabase(Database);
                IMongoCollection<T> collection = db.GetCollection<T>(cName);
                // FilterDefinition<T> filter = Builders<T>.Filter.Gt(field, dateTime.GetStartOfDay());
                data = collection.Find(filter).ToList();
            }
            else
            {
                throw new MongoException("MongoDB connection is not established");
            }
        }

        public UpdateResult UpdateOne<T>(string cName, FilterDefinition<T> filter, UpdateDefinition<T> update)
        {
            if (client != null)
            {
                IMongoDatabase db = client.GetDatabase(Database);
                IMongoCollection<T> collection = db.GetCollection<T>(cName);

                return collection.UpdateOne(filter, update);
            }
            else
            {
                throw new MongoException("MongoDB connection is not established");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cName">集合名稱</param>
        /// <param name="filter"></param>
        /// <param name="update"></param>
        /// <returns></returns>
        public UpdateResult UpsertOne<T>(string cName, FilterDefinition<T> filter, UpdateDefinition<T> update)
        {
            if (client != null)
            {
                IMongoDatabase db = client.GetDatabase(Database);
                IMongoCollection<T> collection = db.GetCollection<T>(cName);

                return collection.UpdateOne(filter, update, new UpdateOptions()
                {
                    IsUpsert = true,
                });
            }
            else
            {
                throw new MongoException("MongoDB connection is not established");
            }
        }

        [Obsolete("此方法須確認後才能使用")]
        public UpdateResult UpsertMany<T>(string cName, FilterDefinition<T> filter, UpdateDefinition<T> update)
        {
            if (client != null)
            {
                IMongoDatabase db = client.GetDatabase(Database);
                IMongoCollection<T> collection = db.GetCollection<T>(cName);

                return collection.UpdateMany(filter, update, new UpdateOptions()
                {
                    IsUpsert = true,
                });
            }
            else
            {
                throw new MongoException("MongoDB connection is not established");
            }
        }

        [Obsolete("待完成")]
        public void BulkWrite<T>(string cName)
        {
            if (client != null)
            {
                IMongoDatabase db = client.GetDatabase(Database);
                IMongoCollection<T> collection = db.GetCollection<T>(cName);

            }
            else
            {
                throw new MongoException("MongoDB connection is not established");
            }
        }

        public void Empty<T>(string cName)
        {
            if (client != null)
            {
                IMongoDatabase db = client.GetDatabase(Database);
                IMongoCollection<T> collection = db.GetCollection<T>(cName);

                _ = collection.DeleteMany(Builders<T>.Filter.Empty);
            }
            else
            {
                throw new MongoException("MongoDB connection is not established");
            }
        }

        public DeleteResult DeleteMany<T>(string cName, FilterDefinition<T> filter)
        {
            if (client != null)
            {
                IMongoDatabase db = client.GetDatabase(Database);
                IMongoCollection<T> collection = db.GetCollection<T>(cName);

                return collection.DeleteMany(filter);
            }
            else
            {
                throw new MongoException("MongoDB connection is not established");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) { return; }

            if (disposing)
            {

            }
            _disposed = true;
        }
    }

    /// <summary>
    /// DateTime Extension
    /// </summary>
    public static class DateTimeEx
    {
        public static DateTime GetStartOfDay(this DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0);
        }

        public static DateTime GetEndOfDay(this DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, 23, 59, 59);
        }
    }
}