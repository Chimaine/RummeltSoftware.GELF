using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using RummeltSoftware.Gelf.Client.Internal;
using RummeltSoftware.Gelf.Internal;

namespace RummeltSoftware.Gelf {
    [TestClass]
    public class GelfJsonStreamWriterTest {
        /// <summary>
        /// Check if the formatter produces valid JSON.
        /// </summary>
        [TestMethod]
        public void TestValidJson() {
            var writer = new GelfJsonStreamWriter();
            string result;
            using (var stream = MemoryStreamPool.GetStream()) {
                writer.Start(stream);
                writer.Write("version", 1.1);
                writer.Write("host", "foo");
                writer.Write("timestamp", null);
                writer.Finish();

                result = GelfHelper.Encoding.GetString(stream.GetBuffer(), 0, (int) stream.Length);
            }

            JToken.Parse(result);
        }


        [TestMethod]
        public void TestNullField() {
            var writer = new GelfJsonStreamWriter();
            string result;
            using (var stream = MemoryStreamPool.GetStream()) {
                writer.Start(stream);
                writer.Write("testField", null);
                writer.Finish();

                result = GelfHelper.Encoding.GetString(stream.GetBuffer(), 0, (int) stream.Length);
            }

            var json = JToken.Parse(result);
            Assert.AreEqual(JTokenType.Null, json["testField"].Type);
        }


        [TestMethod]
        public void TestStringField() {
            var writer = new GelfJsonStreamWriter();
            string result;
            using (var stream = MemoryStreamPool.GetStream()) {
                writer.Start(stream);
                writer.Write("testField", "foobar");
                writer.Finish();

                result = GelfHelper.Encoding.GetString(stream.GetBuffer(), 0, (int) stream.Length);
            }

            var json = JToken.Parse(result);
            Assert.AreEqual(JTokenType.String, json["testField"].Type);
            Assert.AreEqual("foobar", json.Value<string>("testField"));
        }


        [TestMethod]
        public void TestNumberFields() {
            var writer = new GelfJsonStreamWriter();
            string result;
            using (var stream = MemoryStreamPool.GetStream()) {
                writer.Start(stream);
                writer.Write("testField_int", 42);
                writer.Write("testField_double", 4.2);
                writer.Finish();

                result = GelfHelper.Encoding.GetString(stream.GetBuffer(), 0, (int) stream.Length);
            }

            var json = JToken.Parse(result);
            Assert.AreEqual(JTokenType.Integer, json["testField_int"].Type);
            Assert.AreEqual(42, json.Value<int>("testField_int"));

            Assert.AreEqual(JTokenType.Float, json["testField_double"].Type);
            Assert.AreEqual(4.2, json.Value<double>("testField_double"));
        }


        [TestMethod]
        public void TestAutoFlush() {
            var writer = new GelfJsonStreamWriter(128);
            string result;
            using (var stream = MemoryStreamPool.GetStream()) {
                writer.Start(stream);
                writer.Write("version", 1.1);
                writer.Write("host", "foo");
                writer.Write("full_message", @"
                    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nulla et congue lorem. Proin vel pulvinar neque, in pulvinar mi. Quisque sit amet tempus eros. Donec eget nisl augue. Aliquam erat volutpat. Nulla nulla purus, viverra cursus efficitur vitae, facilisis laoreet massa. Mauris finibus est facilisis orci cursus, ut porta enim efficitur. Nullam quis tristique arcu. In hac habitasse platea dictumst. Praesent aliquet pulvinar orci, non dictum lectus facilisis vitae.
                    In vestibulum et orci nec porttitor. Duis semper enim quis sapien gravida elementum. In eu quam sit amet lorem scelerisque fringilla. Curabitur et ante id mauris pharetra mollis. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Nulla consectetur egestas diam, in ullamcorper metus hendrerit vel. Cras lacinia, metus eu lacinia ornare, urna nunc ultricies mauris, eu accumsan tellus dolor et magna.
                    Duis eleifend, diam quis ullamcorper convallis, diam nibh tincidunt lorem, in vehicula metus tellus ac erat nullam.");
                writer.Finish();

                result = GelfHelper.Encoding.GetString(stream.GetBuffer(), 0, (int) stream.Length);
            }

            JToken.Parse(result);
        }


        [TestMethod]
        public void TestWriteLongFieldName() {
            var fieldName = "ThisIsAVeryLongFieldNameToForceABufferOverflow";

            var writer = new GelfJsonStreamWriter(7);
            string result;
            using (var stream = MemoryStreamPool.GetStream()) {
                writer.Start(stream);
                writer.Write(fieldName, "foobar");
                writer.Finish();

                result = GelfHelper.Encoding.GetString(stream.GetBuffer(), 0, (int) stream.Length);
            }

            var json = JToken.Parse(result);
            Assert.AreEqual(JTokenType.String, json[fieldName].Type);
            Assert.AreEqual("foobar", json.Value<string>(fieldName));
        }


        [TestMethod]
        public void TestWriteLongUnescaped() {
            var fieldName = "foobar";
            var writer = new GelfJsonStreamWriter(7);
            string result;
            using (var stream = MemoryStreamPool.GetStream()) {
                writer.Start(stream);
                writer.Write(fieldName, long.MaxValue);
                writer.Finish();

                result = GelfHelper.Encoding.GetString(stream.GetBuffer(), 0, (int) stream.Length);
            }

            var json = JToken.Parse(result);
            Assert.AreEqual(JTokenType.Integer, json[fieldName].Type);
            Assert.AreEqual(long.MaxValue, json.Value<long>(fieldName));
        }


        [TestMethod]
        public void TestSimpleEscape() {
            var value = "\t\r\n\"\\/\f\b";
            var writer = new GelfJsonStreamWriter(128);
            string result;
            using (var stream = MemoryStreamPool.GetStream()) {
                writer.Start(stream);
                writer.Write("testField", value);
                writer.Finish();

                result = GelfHelper.Encoding.GetString(stream.GetBuffer(), 0, (int) stream.Length);
            }

            var json = JToken.Parse(result);
            Assert.AreEqual(value, json.Value<string>("testField"));
        }


        [TestMethod]
        public void TestUnicodeEscape() {
            var value = "\u001F_Test_\u007F_\u0080_\u009F";
            var writer = new GelfJsonStreamWriter(128);
            string result;
            using (var stream = MemoryStreamPool.GetStream()) {
                writer.Start(stream);
                writer.Write("testField", value);
                writer.Finish();

                result = GelfHelper.Encoding.GetString(stream.GetBuffer(), 0, (int) stream.Length);
            }

            var json = JToken.Parse(result);
            Assert.AreEqual(value, json.Value<string>("testField"));
        }


        [TestMethod]
        public void TestAutoTypeWrite() {
            var writer = new GelfJsonStreamWriter();
            string result;
            using (var stream = MemoryStreamPool.GetStream()) {
                writer.Start(stream);
                writer.Write("int", (object) 42);
                writer.Write("long", (object) 42L);
                writer.Write("string", (object) "string");
                writer.Write("double", (object) 13.37);
                writer.Write("object", (object) DateTime.MinValue);
                writer.Write("null", (object) null);
                writer.Finish();

                result = GelfHelper.Encoding.GetString(stream.GetBuffer(), 0, (int) stream.Length);
            }

            var json = JToken.Parse(result);
            Assert.AreEqual(JTokenType.Integer, json["int"].Type);
            Assert.AreEqual(JTokenType.Integer, json["long"].Type);
            Assert.AreEqual(JTokenType.String, json["string"].Type);
            Assert.AreEqual(JTokenType.Float, json["double"].Type);
            Assert.AreEqual(JTokenType.String, json["object"].Type);
            Assert.AreEqual(JTokenType.Null, json["null"].Type);
        }


        [TestMethod]
        public void TestIntegers() {
            var writer = new GelfJsonStreamWriter();
            string result;
            using (var stream = MemoryStreamPool.GetStream()) {
                writer.Start(stream);
                writer.Write("A", 0);
                writer.Write("B", int.MinValue);
                writer.Write("C", int.MaxValue);
                writer.Write("D", -1);
                writer.Finish();

                result = GelfHelper.Encoding.GetString(stream.GetBuffer(), 0, (int) stream.Length);
            }

            var json = JToken.Parse(result);
            Assert.AreEqual(0, json.Value<int>("A"));
            Assert.AreEqual(int.MinValue, json.Value<int>("B"));
            Assert.AreEqual(int.MaxValue, json.Value<int>("C"));
            Assert.AreEqual(-1, json.Value<int>("D"));
        }


        [TestMethod]
        public void TestLongs() {
            var writer = new GelfJsonStreamWriter();
            string result;
            using (var stream = MemoryStreamPool.GetStream()) {
                writer.Start(stream);
                writer.Write("A", 0L);
                writer.Write("B", long.MinValue);
                writer.Write("C", long.MaxValue);
                writer.Write("D", -1L);
                writer.Finish();

                result = GelfHelper.Encoding.GetString(stream.GetBuffer(), 0, (int) stream.Length);
            }

            var json = JToken.Parse(result);
            Assert.AreEqual(0L, json.Value<long>("A"));
            Assert.AreEqual(long.MinValue, json.Value<long>("B"));
            Assert.AreEqual(long.MaxValue, json.Value<long>("C"));
            Assert.AreEqual(-1L, json.Value<long>("D"));
        }


        [TestMethod]
        public void TestDoubles() {
            var writer = new GelfJsonStreamWriter();
            string result;
            using (var stream = MemoryStreamPool.GetStream()) {
                writer.Start(stream);
                writer.Write("A", 0.0);
                writer.Write("B", double.MinValue);
                writer.Write("C", double.MaxValue);
                writer.Write("D", double.Epsilon);
                writer.Write("E", -1.0);
                writer.Finish();

                result = GelfHelper.Encoding.GetString(stream.GetBuffer(), 0, (int) stream.Length);
            }

            var json = JToken.Parse(result);
            Assert.AreEqual(0.0, json.Value<double>("A"));
            Assert.AreEqual(double.MinValue, json.Value<double>("B"));
            Assert.AreEqual(double.MaxValue, json.Value<double>("C"));
            Assert.AreEqual(double.Epsilon, json.Value<double>("D"));
            Assert.AreEqual(-1.0, json.Value<double>("E"));
        }
    }
}