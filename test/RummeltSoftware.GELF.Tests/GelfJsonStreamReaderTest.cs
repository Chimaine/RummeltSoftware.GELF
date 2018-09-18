using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RummeltSoftware.Gelf.Listener.Internal;

namespace RummeltSoftware.Gelf {
    [TestClass]
    public class GelfJsonStreamReaderTest {
        private static Dictionary<string, object> ReadFromString(string s) {
            var bytes = GelfHelper.Encoding.GetBytes(s);

            var fields = new Dictionary<string, object>();
            using (var stream = new MemoryStream(bytes)) {
                var reader = new GelfJsonStreamReader();
                reader.ReadStart(stream);
                reader.ReadAllFields(fields);
            }

            return fields;
        }


        [TestMethod]
        public void TestEmptyMessage() {
            var json = "{}";
            var fields = ReadFromString(json);

            Assert.AreEqual(0, fields.Count);
        }


        [TestMethod]
        public void TestSingleField() {
            var json = "{ \"testField1\": \"foobar\" }";
            var fields = ReadFromString(json);

            Assert.AreEqual(1, fields.Count);
            Assert.IsTrue(fields.ContainsKey("testField1"));
        }


        [TestMethod]
        public void TestMultipleFields() {
            var json = "{\"testField1\":\"foobar1\",\"testField2\":\"foobar2\"}";
            var fields = ReadFromString(json);

            Assert.AreEqual(2, fields.Count);
            Assert.IsTrue(fields.ContainsKey("testField1"));
            Assert.IsTrue(fields.ContainsKey("testField2"));
        }


        [TestMethod]
        public void TestString() {
            var json = "{ \"testField1\": \"foobar\" }";
            var fields = ReadFromString(json);

            Assert.AreEqual("foobar", fields["testField1"]);
        }


        [TestMethod]
        public void TestIntegers() {
            var json = "{ \"testField1\": 1234, \"testField3\": 140737488355328" +
                       ", \"testField2\": -1234, \"testField4\": -140737488355328 }";
            var fields = ReadFromString(json);

            Assert.AreEqual(typeof(int), fields["testField1"].GetType());
            Assert.AreEqual(typeof(int), fields["testField2"].GetType());
            Assert.AreEqual(1234, fields["testField1"]);
            Assert.AreEqual(-1234, fields["testField2"]);

            Assert.AreEqual(typeof(long), fields["testField3"].GetType());
            Assert.AreEqual(typeof(long), fields["testField4"].GetType());
            Assert.AreEqual(140737488355328, fields["testField3"]);
            Assert.AreEqual(-140737488355328, fields["testField4"]);
        }


        [TestMethod]
        public void TestFloats() {
            var json = "{ \"testField1\": 1.2345, \"testField2\": -1.2345" +
                       ", \"testField3\": 1.2345E-02, \"testField4\": -1.2345E-02 }";
            var fields = ReadFromString(json);

            Assert.AreEqual(typeof(double), fields["testField1"].GetType());
            Assert.AreEqual(typeof(double), fields["testField2"].GetType());

            Assert.AreEqual(1.2345, fields["testField1"]);
            Assert.AreEqual(-1.2345, fields["testField2"]);

            Assert.AreEqual(typeof(double), fields["testField3"].GetType());
            Assert.AreEqual(typeof(double), fields["testField4"].GetType());
            Assert.AreEqual(0.012345, fields["testField3"]);
            Assert.AreEqual(-0.012345, fields["testField4"]);
        }


        [TestMethod]
        public void TestEscapeChars() {
            var shortEscapesName = "Foo\\\"Bar";
            var shortEscapes = "\\\" \\\\ \\/ \\b \\f \\n \\r \\t";

            var unicodeEscapesName = "Unibar";
            var unicodeEscapes = "Hello Unicode: \\u0192, \\u21A0, \\u21a0";

            var json = "{ \"" + shortEscapesName + "\": \"" + shortEscapes + "\"" +
                       ", \"" + unicodeEscapesName + "\": \"" + unicodeEscapes + "\" }";
            var fields = ReadFromString(json);

            Assert.AreEqual("\" \\ / \b \f \n \r \t", fields["Foo\"Bar"]);
            Assert.AreEqual("Hello Unicode: ƒ, ↠, ↠", fields["Unibar"]);
        }


        [TestMethod]
        public void TestBufferResidue() {
            var stream = File.OpenRead(Path.Combine("JSON", "Test_Overflow.json"));
            var reader = new GelfJsonStreamReader();
            reader.ReadStart(stream);

            var fields = new Dictionary<string, object>();
            reader.ReadAllFields(fields);
        }
    }
}