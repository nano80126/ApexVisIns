using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Libmongocrypt;


namespace ApexVisIns
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
        public bool Connected {
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
                    ServerSelectionTimeout = TimeSpan.FromMilliseconds(1500) // 選擇 Server timeout (嘗試連線時之timeout)
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
        /// 建立 Collection
        /// </summary>
        /// <param name="collection">集合名稱</param>
        public void CreateCollection(string collection)
        {
            try
            {
                // 先確認 Collection 是否存在，再新增
                bool exist = client.GetDatabase(Database).ListCollectionNames().ToList().Contains(collection);
                if (!exist) { client.GetDatabase(Database).CreateCollection(collection); }
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
                //Console.WriteLine("MongoDB Client is not initialized");
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
                //Console.WriteLine("MongoDB Client is not initialized");
            }
        }


        /// <summary>
        /// 搜尋第一筆符合條件 Document
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dbName"></param>
        /// <param name="cName"></param>
        public void FindOne<T>(string dbName, string cName)
        {
            if (client != null)
            {
                IMongoDatabase db = client.GetDatabase(dbName);
                IMongoCollection<T> collection = db.GetCollection<T>(cName);

                var filter = Builders<T>.Filter.Lt("DateTime", DateTime.Now);

                var d = collection.Find<T>(filter).ToList();

                foreach (var item in d)
                {

                }
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
}