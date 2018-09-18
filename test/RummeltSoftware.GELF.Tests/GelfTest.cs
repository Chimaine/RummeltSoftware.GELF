using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RummeltSoftware.Gelf {
    [TestClass]
    public class GelfTest
    {
        [TestMethod]
        public void TestCanonicalizeFieldName_NotNullOrEmpty() {
            Assert.ThrowsException<ArgumentException>(() => GelfHelper.CanonicalizeFieldName(null));
            Assert.ThrowsException<ArgumentException>(() => GelfHelper.CanonicalizeFieldName(""));
        }

        [TestMethod]
        public void TestCanonicalizeFieldName_Underscores() {
            Assert.AreEqual("thisshould_keep_underscores", 
                            GelfHelper.CanonicalizeFieldName("thisshould_keep_underscores"));
        }

        [TestMethod]
        public void TestCanonicalizeFieldName_Dots()
        {
            Assert.AreEqual("dots_shouldbereplacedwith_underscores", 
                            GelfHelper.CanonicalizeFieldName("dots.shouldbereplacedwith.underscores"));
        }

        [TestMethod]
        public void TestCanonicalizeFieldName_CamelCase()
        {
            Assert.AreEqual("camel_case_shouldbecome_lowercasewith_underscores", 
                            GelfHelper.CanonicalizeFieldName("CamelCaseShouldbecomeLowercasewithUnderscores"));
        }

        [TestMethod]
        public void TestCanonicalizeFieldName_TrailingAcronyms()
        {
            Assert.AreEqual("trailingacronymsshouldbekept_together",
                            GelfHelper.CanonicalizeFieldName("trailingacronymsshouldbekeptTOGETHER"));
        }


        [TestMethod]
        public void TestCanonicalizeFieldName_Combined() {
            Assert.AreEqual("camel_case_id_test_string",
                            GelfHelper.CanonicalizeFieldName("CamelCaseID_Test.String"));
        }
    }
}