using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RummeltSoftware.Gelf {
    [TestClass]
    public class GelfMessageTest {
        [TestMethod]
        public void TestWith() {
            var msg1 = new GelfMessageBuilder("TheHost", "With a message")
                       .FullMessage("This is a long message")
                       .Level(SeverityLevel.Critical)
                       .Timestamp(DateTime.UtcNow)
                       .AdditionalField("_a_string", "An additional field string")
                       .AdditionalField("_Test_double", 1337.42)
                       .AdditionalField("_And_an_int", 42).Build();

            Assert.ThrowsException<ArgumentNullException>(() => msg1.With(null));

            var msg2 = msg1.With(m => m.Message("Changed message")
                                       .AdditionalField("_added_Field", "Something"));

            Assert.AreEqual(msg1.Version, msg2.Version);
            Assert.AreEqual(msg1.Host, msg2.Host);
            Assert.AreEqual("Changed message", msg2.ShortMessage);
            Assert.AreEqual(msg1.FullMessage, msg2.FullMessage);
            Assert.AreEqual(msg1.Level, msg2.Level);
            Assert.AreEqual(msg1.Timestamp, msg2.Timestamp);
            Assert.AreEqual(msg1.AdditionalFields["_a_string"], msg2.AdditionalFields["_a_string"]);
            Assert.AreEqual(msg1.AdditionalFields["_Test_double"], msg2.AdditionalFields["_Test_double"]);
            Assert.AreEqual(msg1.AdditionalFields["_And_an_int"], msg2.AdditionalFields["_And_an_int"]);
            Assert.AreEqual("Something", msg2.AdditionalFields["_added_Field"]);
        }
    }
}