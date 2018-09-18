using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RummeltSoftware.Gelf.Client.Internal;
using RummeltSoftware.Gelf.Listener.Internal;

namespace RummeltSoftware.Gelf {
    [TestClass]
    public class GelfMessageDecoderTest {
        [TestMethod]
        public void TestVersionRequired() {
            var raw = Encode("{}");
            var decoder = new GelfMessageDecoder();

            Assert.ThrowsException<GelfMessageFormatException>(() => decoder.Decode(raw));
        }


        [TestMethod]
        public void TestSupportedVersions() {
            var bogus = Encode("{ \"version\": \"foo\", \"host\": \"A\", \"short_message\": \"B\" }");
            var v1_1 = Encode("{ \"version\": \"1.1\", \"host\": \"A\", \"short_message\": \"B\" }");
            var decoder = new GelfMessageDecoder();

            Assert.ThrowsException<GelfMessageFormatException>(() => decoder.Decode(bogus));
            Assert.AreEqual("1.1", decoder.Decode(v1_1)?.Message.Version);
        }


        [TestMethod]
        public void TestHostRequired() {
            var raw = Encode("{ \"version\": \"1.1\", \"short_message\": \"B\" }");
            var decoder = new GelfMessageDecoder();

            Assert.ThrowsException<GelfMessageFormatException>(() => decoder.Decode(raw));
        }


        [TestMethod]
        public void TestShortMessageRequired() {
            var raw = Encode("{ \"version\": \"1.1\", \"host\": \"A\" }");
            var decoder = new GelfMessageDecoder();

            Assert.ThrowsException<GelfMessageFormatException>(() => decoder.Decode(raw));
        }


        [TestMethod]
        public void TestMinimalMessage() {
            var raw = Encode(new GelfMessageBuilder("A", "B").Build());
            var decoder = new GelfMessageDecoder();

            var decodedMessage = decoder.Decode(raw)?.Message;

            Assert.IsNotNull(decodedMessage);

            Assert.AreEqual(GelfMessageBuilder.Version, decodedMessage.Version);
            Assert.AreEqual("A", decodedMessage.Host);
            Assert.AreEqual("B", decodedMessage.ShortMessage);

            Assert.IsNull(decodedMessage.FullMessage);
            Assert.IsNull(decodedMessage.Level);
            Assert.IsNull(decodedMessage.Timestamp);
            Assert.AreEqual(0, decodedMessage.AdditionalFields.Count);
        }


        [TestMethod]
        public void TestFullMessage() {
            var raw = Encode(new GelfMessageBuilder("A", "B").FullMessage("C").Build());
            var decoder = new GelfMessageDecoder();

            var decodedMessage = decoder.Decode(raw)?.Message;
            Assert.IsNotNull(decodedMessage);

            Assert.AreEqual("C", decodedMessage.FullMessage);

            Assert.IsNull(decodedMessage.Level);
            Assert.IsNull(decodedMessage.Timestamp);
            Assert.AreEqual(0, decodedMessage.AdditionalFields.Count);
        }


        [TestMethod]
        public void TestLevel() {
            var raw = Encode(new GelfMessageBuilder("A", "B").Level(SeverityLevel.Warning).Build());
            var decoder = new GelfMessageDecoder();

            var decodedMessage = decoder.Decode(raw)?.Message;
            Assert.IsNotNull(decodedMessage);

            Assert.AreEqual(SeverityLevel.Warning, decodedMessage.Level);

            Assert.IsNull(decodedMessage.FullMessage);
            Assert.IsNull(decodedMessage.Timestamp);
            Assert.AreEqual(0, decodedMessage.AdditionalFields.Count);
        }


        [TestMethod]
        public void TestTimestamp() {
            var dtDouble = new DateTime(2018, 2, 23, 10, 32, 29, 198, DateTimeKind.Utc);
            var raw = Encode(new GelfMessageBuilder("A", "B").Timestamp(dtDouble).Build());
            var decoder = new GelfMessageDecoder();

            var decodedMessage = decoder.Decode(raw)?.Message;
            Assert.IsNotNull(decodedMessage);

            Assert.AreEqual(dtDouble, decodedMessage.Timestamp);

            Assert.IsNull(decodedMessage.FullMessage);
            Assert.IsNull(decodedMessage.Level);
            Assert.AreEqual(0, decodedMessage.AdditionalFields.Count);

            var dtInt = new DateTime(2018, 2, 23, 10, 32, 29, 0, DateTimeKind.Utc);
            var dtLong = new DateTime(2038, 1, 19, 03, 14, 08, 0, DateTimeKind.Utc);

            raw = Encode(new GelfMessageBuilder("A", "B").Timestamp(dtInt).Build());
            decodedMessage = decoder.Decode(raw)?.Message;
            Assert.AreEqual(dtInt, decodedMessage?.Timestamp);

            raw = Encode(new GelfMessageBuilder("A", "B").Timestamp(dtLong).Build());
            decodedMessage = decoder.Decode(raw)?.Message;
            Assert.AreEqual(dtLong, decodedMessage?.Timestamp);
        }


        [TestMethod]
        public void TestAdditionalFields() {
            var raw = Encode(new GelfMessageBuilder("A", "B")
                             .AdditionalField("_foo", "bar")
                             .Build());
            var decoder = new GelfMessageDecoder();

            var decodedMessage = decoder.Decode(raw)?.Message;
            Assert.IsNotNull(decodedMessage);

            Assert.AreEqual(1, decodedMessage.AdditionalFields.Count);
            Assert.AreEqual("bar", (string) decodedMessage.AdditionalFields["_foo"]);
        }


        [TestMethod]
        public void TestMultipleAdditionalFieldsWithSameKey() {
            var raw = Encode("{ \"version\": \"1.1\", \"host\": \"A\", \"short_message\": \"B\"" +
                             ", \"_foo\": \"C\", \"_foo\": \"D\" }");
            var decoder = new GelfMessageDecoder();

            var decodedMessage = decoder.Decode(raw)?.Message;
            Assert.IsNotNull(decodedMessage);

            Assert.AreEqual(1, decodedMessage.AdditionalFields.Count);
            Assert.AreEqual("D", (string) decodedMessage.AdditionalFields["_foo"]);
        }


        [TestMethod]
        public void TestDeprecatedFields() {
            var raw = Encode("{ \"version\": \"1.1\", \"host\": \"A\", \"short_message\": \"B\"" +
                             ", \"facility\": \"C\", \"line\": 42, \"file\": \"E\" }");
            var decoder = new GelfMessageDecoder();

            var decodedMessage = decoder.Decode(raw)?.Message;
            Assert.IsNotNull(decodedMessage);

            Assert.AreEqual(3, decodedMessage.AdditionalFields.Count);
            Assert.AreEqual("C", (string) decodedMessage.AdditionalFields["_facility"]);
            Assert.AreEqual(42, (int) decodedMessage.AdditionalFields["_line"]);
            Assert.AreEqual("E", (string) decodedMessage.AdditionalFields["_file"]);
        }


        [TestMethod]
        public void TestGzipCompressed() {
            var raw = EncodeGzip(new GelfMessageBuilder("A", "B").Build());
            var decoder = new GelfMessageDecoder();

            var decodedMessage = decoder.Decode(raw)?.Message;

            Assert.IsNotNull(decodedMessage);

            Assert.AreEqual(GelfMessageBuilder.Version, decodedMessage.Version);
            Assert.AreEqual("A", decodedMessage.Host);
            Assert.AreEqual("B", decodedMessage.ShortMessage);

            Assert.IsNull(decodedMessage.FullMessage);
            Assert.IsNull(decodedMessage.Level);
            Assert.IsNull(decodedMessage.Timestamp);
            Assert.AreEqual(0, decodedMessage.AdditionalFields.Count);
        }


        [TestMethod]
        public void TestMultipleReads() {
            var raw = EncodeGzip(new GelfMessageBuilder("A", "B").Build());
            var decoder = new GelfMessageDecoder();

            for (var i = 0; i < 100; i++) {
                var decodedMessage = decoder.Decode(raw)?.Message;
                Assert.IsNotNull(decodedMessage);
                Assert.AreEqual("A", decodedMessage.Host);
                Assert.AreEqual("B", decodedMessage.ShortMessage);
            }
        }


        // ========================================


        private static RawGelfMessage Encode(GelfMessage msg) {
            var encoder = new GelfMessageEncoder();
            var stream = new MemoryStream();
            encoder.Encode(msg, stream);

            return new RawGelfMessage(stream.GetBuffer(), (int) stream.Position, new IPEndPoint(IPAddress.Any, 0), DateTime.UtcNow);
        }


        private static RawGelfMessage Encode(string msg) {
            var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream, GelfHelper.Encoding, 1024, true)) {
                writer.Write(msg);
                writer.Flush();
            }

            return new RawGelfMessage(stream.GetBuffer(), (int) stream.Position, new IPEndPoint(IPAddress.Any, 0), DateTime.UtcNow);
        }


        private static RawGelfMessage EncodeGzip(GelfMessage msg) {
            var encoder = new GelfMessageEncoder();
            var uncompressed = new MemoryStream();
            encoder.Encode(msg, uncompressed);

            var compressed = new MemoryStream();
            using (var deflater = new GZipStream(compressed, CompressionMode.Compress, true)) {
                deflater.Write(uncompressed.GetBuffer(), 0, (int) uncompressed.Position);
            }

            return new RawGelfMessage(compressed.GetBuffer(), (int) compressed.Position, new IPEndPoint(IPAddress.Any, 0), DateTime.UtcNow);
        }
    }
}