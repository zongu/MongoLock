
namespace MongoLock
{
    using System;
    using MongoDB.Bson.Serialization.Attributes;

    /// <summary>
    /// 結構鎖物件
    /// </summary>
    public class LockAcquire
    {
        /// <summary>
        /// _id 唯一值
        /// </summary>
        public string Resource { get; set; }

        /// <summary>
        /// 鎖ID
        /// </summary>
        public string LockId { get; set; }

        /// <summary>
        /// 過期時間
        /// </summary>
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime ExpireDateTime { get; set; }
    }
}
