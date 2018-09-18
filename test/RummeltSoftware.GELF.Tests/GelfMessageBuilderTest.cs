using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RummeltSoftware.Gelf {
    [TestClass]
    public class GelfMessageBuilderTest {
        [TestMethod]
        public void TestRequiredFields() {
            var noHostBuilder = new GelfMessageBuilder().Message("Message but no host");
            var noMessageBuilder = new GelfMessageBuilder().Host("OnlyHostNoMessage");
            var bothFieldsBuilder = new GelfMessageBuilder().Host("TheHost").Message("With a message");

            Assert.ThrowsException<InvalidOperationException>(() => new GelfMessageBuilder().Build());
            Assert.ThrowsException<InvalidOperationException>(() => noHostBuilder.Build());
            Assert.ThrowsException<InvalidOperationException>(() => noMessageBuilder.Build());

            Assert.ThrowsException<ArgumentException>(() => new GelfMessageBuilder().Host(null));
            Assert.ThrowsException<ArgumentException>(() => new GelfMessageBuilder().Host(""));
            Assert.ThrowsException<ArgumentException>(() => new GelfMessageBuilder().Message(null));
            Assert.ThrowsException<ArgumentException>(() => new GelfMessageBuilder().Message(""));

            var message = bothFieldsBuilder.Build();
            Assert.AreEqual("TheHost", message.Host);
            Assert.AreEqual("With a message", message.ShortMessage);
        }


        [TestMethod]
        public void TestUnsetFieldsAreNull() {
            var builder = new GelfMessageBuilder("TheHost", "With a message");

            var message = builder.Build();

            Assert.IsNull(message.FullMessage);
            Assert.IsNull(message.Level);
            Assert.IsNull(message.Timestamp);
            Assert.IsNull(message.AdditionalFields);
        }


        [TestMethod]
        public void TestStandardFields() {
            var timestamp = DateTime.UtcNow;

            var builder = new GelfMessageBuilder("TheHost", "With a message")
                          .FullMessage("This is a long message")
                          .Level(SeverityLevel.Critical)
                          .Timestamp(timestamp);

            var message = builder.Build();

            Assert.AreEqual("TheHost", message.Host);
            Assert.AreEqual("With a message", message.ShortMessage);
            Assert.AreEqual("This is a long message", message.FullMessage);
            Assert.AreEqual(SeverityLevel.Critical, message.Level);
            Assert.AreEqual(timestamp, message.Timestamp);

            Assert.ThrowsException<ArgumentException>(() => new GelfMessageBuilder().Timestamp(new DateTime(1,1,1,0,0,0, DateTimeKind.Local)));
        }


        [TestMethod]
        public void TestAdditionalFields() {

            Assert.ThrowsException<ArgumentException>(() => new GelfMessageBuilder().AdditionalField(null, null));
            Assert.ThrowsException<ArgumentException>(() => new GelfMessageBuilder().AdditionalField("", null));
            Assert.ThrowsException<ArgumentException>(() => new GelfMessageBuilder().AdditionalField("noUnderscore", null));
            Assert.ThrowsException<ArgumentException>(() => new GelfMessageBuilder().AdditionalField("_white space", null));
            Assert.ThrowsException<ArgumentNullException>(() => new GelfMessageBuilder().AdditionalField("_nullvalue", null));

            var builder = new GelfMessageBuilder("TheHost", "With a message")
                          .AdditionalField("_a_string", "An additional field string")
                          .AdditionalField("_Test_double", 1337.42)
                          .AdditionalField("_And_an_int", 42);

            var message = builder.Build();

            Assert.IsNotNull(message.AdditionalFields);
            Assert.IsTrue(message.AdditionalFields.ContainsKey("_a_string"));
            Assert.IsTrue(message.AdditionalFields.ContainsKey("_Test_double"));
            Assert.IsTrue(message.AdditionalFields.ContainsKey("_And_an_int"));

            Assert.AreEqual("An additional field string", message.AdditionalFields["_a_string"]);
            Assert.AreEqual(1337.42, message.AdditionalFields["_Test_double"]);
            Assert.AreEqual(42, message.AdditionalFields["_And_an_int"]);
        }

        [TestMethod]
        public void TestAsDictionary()
        {
            var timestamp = DateTime.UtcNow;

            var builder = new GelfMessageBuilder("TheHost", "With a message")
                          .FullMessage("This is a long message")
                          .Level(SeverityLevel.Critical)
                          .Timestamp(timestamp)
                          .AdditionalField("_a_string", "An additional field string")
                          .AdditionalField("_Test_double", 1337.42)
                          .AdditionalField("_And_an_int", 42);

            var dict = builder.Build().AsDictionary();

            Assert.AreEqual("1.1", dict["version"]);
            Assert.AreEqual("TheHost", dict["host"]);
            Assert.AreEqual("With a message", dict["short_message"]);
            Assert.AreEqual("This is a long message", dict["full_message"]);
            Assert.AreEqual(SeverityLevel.Critical, dict["level"]);
            Assert.AreEqual(timestamp, dict["timestamp"]);
            Assert.AreEqual("An additional field string", dict["_a_string"]);
            Assert.AreEqual(1337.42, dict["_Test_double"]);
            Assert.AreEqual(42, dict["_And_an_int"]);
        }
    }
}