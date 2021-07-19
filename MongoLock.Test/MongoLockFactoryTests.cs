
namespace MongoLock.Test
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using MongoDB.Driver;

    [TestClass]
    public class MongoLockFactoryTests
    {
        private MongoLockFactory factory;

        private const string mongoConn = @"mongodb://localhost:27017";

        [TestInitialize]
        public void Init()
        {
            var client = new MongoClient(mongoConn);
            var db = client.GetDatabase("MongoLock");
            db.DropCollection("Lock");

            this.factory = new MongoLockFactory(client);
        }

        [TestMethod]
        public void 加鎖測試()
        {
            var lck = this.factory.CreateLock("TEST001", TimeSpan.FromMilliseconds(10000), TimeSpan.FromMilliseconds(10000), TimeSpan.FromMilliseconds(300));
            Assert.IsTrue(lck.IsAcquired);
        }

        [TestMethod]
        public void 釋放鎖測試()
        {
            using (var lck = this.factory.CreateLock("TEST001", TimeSpan.FromMilliseconds(10000), TimeSpan.FromMilliseconds(10000), TimeSpan.FromMilliseconds(300)))
            {
                Assert.IsTrue(lck.IsAcquired);
            }
        }

        [TestMethod]
        public void 取不到鎖測試()
        {
            using (var lck1 = this.factory.CreateLock("TEST001", TimeSpan.FromMilliseconds(10000), TimeSpan.FromMilliseconds(10000), TimeSpan.FromMilliseconds(300)))
            using (var lck2 = this.factory.CreateLock("TEST001", TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(300)))
            {
                Assert.IsTrue(lck1.IsAcquired);
                Assert.IsFalse(lck2.IsAcquired);
                Assert.AreEqual(lck2.ExtendCount, 4);
            }
        }
    }
}
