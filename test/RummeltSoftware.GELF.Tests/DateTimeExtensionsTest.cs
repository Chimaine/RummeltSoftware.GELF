using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RummeltSoftware.Gelf.Internal;

namespace RummeltSoftware.Gelf {
    [TestClass]
    public class DateTimeExtensionsTest {
        [TestMethod]
        public void TestUnixEpoch() {
            var dtEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var dt2018 = new DateTime(2018, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var timestampEpoch = dtEpoch.ToUnixTimestamp();
            var timestamp2018 = dt2018.ToUnixTimestamp();

            Assert.AreEqual(0, timestampEpoch);
            Assert.AreEqual(1514764800.0, timestamp2018);
        }
    }
}