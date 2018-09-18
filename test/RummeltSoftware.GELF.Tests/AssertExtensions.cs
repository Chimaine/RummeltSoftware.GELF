using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RummeltSoftware.Gelf {
    internal static class AssertExtensions {
        public static void MessagesAreEqual(this Assert assert, GelfMessage a, GelfMessage b) {
            var ad = a.AsDictionary();
            var bd = b.AsDictionary();
            
            Assert.AreEqual(ad.Count, bd.Count);
            Assert.IsFalse(ad.Except(bd).Any());
        }
    }
}