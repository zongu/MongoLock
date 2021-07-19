
namespace MongoLock.Sample
{
    using System;
    using MongoDB.Driver;

    class Program
    {
        const string mongoConn = @"mongodb://localhost:27017";

        /// <summary>
        /// mongo分佈式鎖介面範例
        /// </summary>
        public interface ISampleLocker
        {
            /// <summary>
            /// 加鎖
            /// </summary>
            /// <returns></returns>
            IMongoLck GrabLock(string key);
        }

        /// <summary>
        /// mongo分佈式鎖範例
        /// </summary>
        public class SampleLocker : ISampleLocker
        {
            /// <summary>
            /// mongo分佈式鎖工廠
            /// </summary>
            private MongoLockFactory factory;

            public SampleLocker(MongoLockFactory factory)
            {
                this.factory = factory;
            }

            /// <summary>
            /// 加鎖
            /// </summary>
            /// <returns></returns>
            public IMongoLck GrabLock(string key)
            {
                string resource = $"SampleLocker:{key}";
                return this.GrabLock(resource, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3), TimeSpan.FromMilliseconds(300));
            }

            /// <summary>
            /// 加鎖
            /// </summary>
            /// <param name="resource">需要加鎖的KEY</param>
            /// <param name="ttl">鎖有效存活時間</param>
            /// <param name="waitTime">要不到鎖的等待間</param>
            /// <param name="retryTime">要不到鎖重新要鎖等待時間</param>
            /// <returns></returns>
            private IMongoLck GrabLock(string resource, TimeSpan ttl, TimeSpan waitTime, TimeSpan retryTime)
                => this.factory.CreateLock(resource, ttl, waitTime, retryTime);
        }

        /// <summary>
        /// 非關連式資料庫服務
        /// </summary>
        internal class NoSqlService
        {
            private static Lazy<MongoLockFactory> lazyDistributedLockService;

            public static MongoLockFactory DistributedLockService
            {
                get
                {
                    if (lazyDistributedLockService == null)
                    {
                        lazyDistributedLockService = new Lazy<MongoLockFactory>(() =>
                        {
                            return MongoLockFactory.Create(new MongoClient(mongoConn));
                        });
                    }

                    return lazyDistributedLockService.Value;
                }
            }
        }

        static void Main(string[] args)
        {
            try
            {
                ISampleLocker locker = new SampleLocker(NoSqlService.DistributedLockService);

                using (IMongoLck lck = locker.GrabLock("MongoLockSample"))
                {
                    // 取不到鎖
                    if (!lck.IsAcquired)
                    {
                        throw new Exception($"Can Not Acquire MongoLockSample, LockId:{lck.LockId}");
                    }

                    // 取到鎖之後開始處理需要備份布式鎖保護資源運用
                    // ..............
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.Read();
        }
    }
}
