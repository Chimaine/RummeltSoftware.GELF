using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RummeltSoftware.Gelf.Client.Internal;
using RummeltSoftware.Gelf.Internal;

namespace RummeltSoftware.Gelf {
    [TestClass]
    public class GelfMessageEncoderTest {
        private static JObject CreateJsonFromMsg(GelfMessage msg) {
            var encoder = new GelfMessageEncoder();
            var stream = new MemoryStream();
            encoder.Encode(msg, stream);
            stream.Position = 0L;

            using (var reader = new StreamReader(stream, GelfHelper.Encoding))
            using (var jsonReader = new JsonTextReader(reader)) {
                return JObject.Load(jsonReader);
            }
        }


        [TestMethod]
        public void TestVersionRequired() {
            var msg = new GelfMessage(null, null, null, null, null, null, null);
            var encoder = new GelfMessageEncoder();
            var stream = new MemoryStream();

            Assert.ThrowsException<GelfMessageFormatException>(() => encoder.Encode(msg, stream));
        }


        [TestMethod]
        public void TestEmptyMessage() {
            var msg = new GelfMessage("1.1", null, null, null, null, null, null);
            var json = CreateJsonFromMsg(msg);

            Assert.AreEqual(3, json.Count);
            Assert.AreEqual("1.1", json["version"].ToObject<string>());
            Assert.AreEqual(JTokenType.Null, json["host"].Type);
            Assert.AreEqual(JTokenType.Null, json["short_message"].Type);
        }


        [TestMethod]
        public void TestMinimalMessage() {
            var host = "theHost";
            var shortMessage = "a test message";
            var msg = new GelfMessage("1.1", host, shortMessage, null, null, null, null);
            var json = CreateJsonFromMsg(msg);

            Assert.AreEqual(3, json.Count);
            Assert.AreEqual("1.1", json["version"].ToObject<string>());
            Assert.AreEqual(host, json["host"].ToObject<string>());
            Assert.AreEqual(shortMessage, json["short_message"].ToObject<string>());
        }


        [TestMethod]
        public void TestOptionalFields() {
            var host = "theHost";
            var shortMessage = "a test message";
            var fullMessage = "A full message";
            var timestamp = DateTime.UtcNow;
            var level = SeverityLevel.Informational;
            var additionalFields = new Dictionary<string, object>() {
                ["_additional_field"] = "foo",
            };

            var msg = new GelfMessage("1.1", host, shortMessage, fullMessage, timestamp, level, additionalFields);
            var json = CreateJsonFromMsg(msg);

            Assert.AreEqual(7, json.Count);
            Assert.AreEqual("1.1", json["version"].ToObject<string>());
            Assert.AreEqual(host, json["host"].ToObject<string>());
            Assert.AreEqual(shortMessage, json["short_message"].ToObject<string>());
            Assert.AreEqual(fullMessage, json["full_message"].ToObject<string>());
            Assert.AreEqual(timestamp.ToUnixTimestamp(), json["timestamp"].ToObject<double>());
            Assert.AreEqual((int) level, json["level"].ToObject<int>());
            Assert.AreEqual("foo", json["_additional_field"].ToObject<string>());
        }
    }
}