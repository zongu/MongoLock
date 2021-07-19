
namespace MongoLock
{
    /// <summary>
    /// 鎖物件
    /// </summary>
    public class MongoLck : IMongoLck
    {
        private MongoLockFactory factory;

        private bool isAcquired;

        private int extendCount;

        private string lockId;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="factory">分佈式鎖工廠</param>
        /// <param name="isAcquired">是否獲取到鎖</param>
        /// <param name="extendCount">嘗試獲取鎖次數</param>
        /// <param name="lockId">Lock Id</param>
        public MongoLck(MongoLockFactory factory, bool isAcquired, int extendCount, string lockId)
        {
            this.factory = factory;
            this.isAcquired = isAcquired;
            this.extendCount = extendCount;
            this.lockId = lockId;
        }

        /// <summary>
        /// 是否獲得鎖
        /// </summary>
        public bool IsAcquired
            => this.isAcquired;

        /// <summary>
        /// 延長次數
        /// </summary>
        public int ExtendCount
            => this.extendCount;

        /// <summary>
        /// 鎖ID
        /// </summary>
        public string LockId
            => this.lockId;

        /// <summary>
        /// 釋放資源
        /// </summary>
        public void Dispose()
        {
            if (this.factory != null && !string.IsNullOrEmpty(this.lockId))
            {
                this.factory.LockRelease(this.lockId);
            }
        }
    }
}
