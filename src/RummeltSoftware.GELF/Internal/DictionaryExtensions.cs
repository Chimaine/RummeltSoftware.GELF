using System.Collections.Generic;

namespace RummeltSoftware.Gelf.Internal {
    public static class DictionaryExtensions {
        public static bool TryGetString<T>(this Dictionary<T, object> dict, T key, out string value) {
            if (dict.TryGetValue(key, out var resultObj) && (resultObj is string resultStr)) {
                value = resultStr;
                return true;
            }
            else {
                value = null;
                return false;
            }
        }
    }
}