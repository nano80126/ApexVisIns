using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Libmongocrypt;


namespace ApexVisIns.MongoDB
{



    /// <summary>
    /// 連結 Mongo 資料庫
    /// </summary>
    public class MongoAccess
    {
        private static MongoClient client;
        private bool _connected;


        public bool Connected => _connected;


        /// <summary>
        /// 連線資料庫
        /// </summary>
        /// <param name="dbName">資料庫名稱</param>
        /// <param name="user">使用者</param>
        /// <param name="pwd">密碼</param>
        private static void Connect(string dbName, string user, string pwd)
        {
            if (client == null)
            {
                MongoClientSettings settings = new MongoClientSettings
                {
                    Credential = MongoCredential.CreateCredential(dbName, user, pwd),
                    Server = new MongoServerAddress("localhost", 27017),
                    DirectConnection = true
                };

                client = new MongoClient(settings);
            }
        }

        /// <summary>
        /// 建立 Collection
        /// </summary>
        /// <param name="dbName">Database Name</param>
        /// <param name="collection">Collection Name</param>
        /// <param name=""></param>
        public static void CreateCollection(string dbName, string collection)
        {
            try
            {
                IMongoDatabase database = client.GetDatabase(dbName);

                bool exist = database.ListCollectionNames().ToList().Contains(collection);

                if (!exist)
                {
                    database.CreateCollection(collection);
                }
            }
            catch (MongoException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 建立 Collection with options
        /// </summary>
        /// <param name="dbName"></param>
        /// <param name="collection"></param>
        /// <param name="options"></param>
        public static void CreateCollection(string dbName, string collection, CreateCollectionOptions options)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 列出資料庫
        /// </summary>
        /// <returns></returns>
        public static string[] ListDatabase()
        {
            return client != null ? client.ListDatabaseNames().ToList().ToArray() : throw new MongoException("資料庫尚未建立連線");
        }

        /// <summary>
        /// 列出集合
        /// </summary>
        /// <param name="dbName"></param>
        /// <returns></returns>
        public static string[] ListCollections(string dbName)
        {
            if (client != null)
            {
                List<string> cols = client.GetDatabase(dbName).ListCollectionNames().ToList();
                return cols.ToArray();
            }
            else
            {
                throw new MongoException("資料庫尚未建立連線");
            }
        }

        /// <summary>
        /// 插入單筆
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dbName"></param>
        /// <param name="colName"></param>
        /// <param name="item"></param>
        public static void InsertOne<T>(string dbName, string colName, T item)
        {
            if (client != null)
            {
                IMongoDatabase db = client.GetDatabase(dbName);
                IMongoCollection<T> collection = db.GetCollection<T>(colName);

                collection.InsertOne(item);
            }
            else
            {
                throw new MongoException("資料庫尚未建立連線");
            }
        }

        /// <summary>
        /// 插入多筆
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dbName"></param>
        /// <param name="colName"></param>
        /// <param name="list"></param>
        public static void InsertMany<T>(string dbName, string colName, List<T> list)
        {
            if (client != null)
            {
                IMongoDatabase db = client.GetDatabase(dbName);
                IMongoCollection<T> collection = db.GetCollection<T>(colName);
                collection.InsertMany(list);
            }
            else
            {
                throw new MongoException("資料庫尚未建立連線");
            }
        }



    }
}