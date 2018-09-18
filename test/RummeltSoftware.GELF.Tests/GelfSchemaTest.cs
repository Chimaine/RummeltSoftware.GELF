using System.IO;
using System.Resources;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace RummeltSoftware.Gelf {
    [TestClass]
    public class GelfSchemaTest {
        [TestMethod]
        public void Test_Valid() {
            var schema = LoadSchema();
            var json = JObject.Parse(File.ReadAllText("JSON/Test_Valid.json"));

            Assert.IsTrue(json.IsValid(schema));
        }


        [TestMethod]
        public void Test_AdditionalProperty() {
            var schema = LoadSchema();
            var json = JObject.Parse(File.ReadAllText("JSON/Test_AdditionalProperty.json"));

            Assert.IsFalse(json.IsValid(schema));
        }


        [TestMethod]
        public void Test_MissingVersion() {
            var schema = LoadSchema();
            var json = JObject.Parse(File.ReadAllText("JSON/Test_MissingVersion.json"));

            Assert.IsFalse(json.IsValid(schema));
        }


        [TestMethod]
        public void Test_InvalidAdditionalField() {
            var schema = LoadSchema();
            var json = JObject.Parse(File.ReadAllText("JSON/Test_InvalidAdditionalField.json"));

            Assert.IsFalse(json.IsValid(schema));
        }


        internal static JSchema LoadSchema() {
            var rootNamespace = typeof(GelfHelper).Namespace;
            string content;
            using (var stream = typeof(GelfHelper).Assembly.GetManifestResourceStream($"{rootNamespace}.GELF.1.1.schema.json"))
            using (var reader = new StreamReader(stream ?? throw new MissingManifestResourceException($"{rootNamespace}.GELF.1.1.schema.json"))) {
                content = reader.ReadToEnd();
            }

            return JSchema.Parse(content);
        }
    }
}