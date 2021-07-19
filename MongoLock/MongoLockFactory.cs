
namespace MongoLock
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using MongoDB.Bson.Serialization;
    using MongoDB.Driver;

    /// <summary>
    /// mongodb 分佈式鎖工廠(持久層)
    /// </summary>
    public class MongoLockFactory
    {
        /// <summary>
        /// 取得工廠實例
        /// </summary>
        /// <param name="mongoClient"></param>
        /// <returns></returns>
        public static MongoLockFactory Create(MongoClient mongoClient)
            => new MongoLockFactory(mongoClient);

        private const string dbName = "MongoLock";

        private const string collectionName = "Lock";

        private MongoClient client;
        private IMongoDatabase db { get; set; }
        private IMongoCollection<LockAcquire> collection { get; set; }

        /// <summary>
        /// 程式運行起來後就會先執行
        /// </summary>
        static MongoLockFactory()
        {
            BsonClassMap.RegisterClassMap<LockAcquire>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
                cm.MapIdMember(p => p.Resource);
            });
        }

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="mongoClient"></param>
        public MongoLockFactory(MongoClient mongoClient)
        {
            this.client = mongoClient;
            this.db = this.client.GetDatabase(dbName);
            this.collection = this.db.GetCollection<LockAcquire>(collectionName);
            this.collection.Indexes.CreateMany(new List<CreateIndexModel<LockAcquire>>()
            {
                new CreateIndexModel<LockAcquire>(
                    Builders<LockAcquire>.IndexKeys.Descending(p => p.LockId)),
                new CreateIndexModel<LockAcquire>(
                    Builders<LockAcquire>.IndexKeys.Descending(p => p.ExpireDateTime),
                    // 考慮到程式壞掉或是沒有正常釋放鎖加個TTL
                    new CreateIndexOptions(){ ExpireAfter = TimeSpan.FromMilliseconds(1)})
            });
        }

        /// <summary>
        /// 加鎖
        /// </summary>
        /// <param name="resource">鎖KEY</param>
        /// <param name="ttl">鎖保留時間</param>
        /// <param name="waitTime">可被等待時間</param>
        /// <param name="retryTime">檢查間隔時間</param>
        /// <returns></returns>
        public IMongoLck CreateLock(string resource, TimeSpan ttl, TimeSpan waitTime, TimeSpan retryTime)
        {
            if (ttl < TimeSpan.Zero || ttl > TimeSpan.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(ttl), "The value of ttl in milliseconds is negative or is greater than MaxValue");
            }

            if (waitTime < TimeSpan.Zero || waitTime > TimeSpan.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(waitTime), "The value of waitTime in milliseconds is negative or is greater than MaxValue");
            }

            if(retryTime < TimeSpan.Zero || retryTime > waitTime)
            {
                throw new ArgumentOutOfRangeException(nameof(retryTime), "The value of retryTime in milliseconds is negative or is greater than waitTime");
            }

            bool isAcquire = false;
            int extendCount = 1;
            DateTime expireDateTime = DateTime.Now + waitTime;
            var lockAcquire = new LockAcquire()
            {
                Resource = resource,
                LockId = Guid.NewGuid().ToString(),
                ExpireDateTime = DateTime.Now + ttl
            };

            while (
                // 嘗試加鎖
                !this.TryInsert(lockAcquire, ref isAcquire) &&
                // 如果超過等待時間就不繼續做了
                expireDateTime > DateTime.Now + retryTime)
            {
                extendCount++;
                SpinWait.SpinUntil(() => false, retryTime);
            }

            return new MongoLck(this, isAcquire, extendCount, lockAcquire.LockId);
        }

        /// <summary>
        /// 釋放鎖
        /// </summary>
        /// <param name="lockId"></param>
        public void LockRelease(string lockId)
        {
            try
            {
                var filter = Builders<LockAcquire>.Filter.Eq(p => p.LockId, lockId);
                this.collection.DeleteOne(filter);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 嘗試更新
        /// </summary>
        /// <param name="lockAcquire"></param>
        /// <param name="isAcquare"></param>
        /// <returns></returns>
        private bool TryInsert(LockAcquire lockAcquire, ref bool isAcquare)
        {
            isAcquare = false;

            try
            {
                this.collection.InsertOne(lockAcquire);
                isAcquare = true;
                return isAcquare;
            }
            catch (MongoWriteException ex)
            {
                // KEY值重複，代表還有人鎖住
                if (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
                {
                    return isAcquare;
                }

                throw;
            }
        }
    }
}
