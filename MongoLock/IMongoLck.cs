
namespace MongoLock
{
    using System;

    /// <summary>
    /// 鎖物件
    /// </summary>
    public interface IMongoLck : IDisposable
    {
        /// <summary>
        /// 是否獲得鎖
        /// </summary>
        bool IsAcquired { get; }

        /// <summary>
        /// 延長次數
        /// </summary>
        int ExtendCount { get; }

        /// <summary>
        /// 鎖ID
        /// </summary>
        string LockId { get; }
    }
}
