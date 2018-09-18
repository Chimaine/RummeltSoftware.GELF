using System;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace RummeltSoftware.Gelf {
    [PublicAPI]
    public static class GelfHelper {
        /// <summary>
        /// Gelf Encoding is always UTF-8 without BOM.
        /// </summary>
        public static readonly Encoding Encoding = new UTF8Encoding(false);

        // ========================================

        private static readonly Regex AdditionalFieldNameExpr = new Regex(@"^_[\w\.\-]*$", RegexOptions.Compiled);

        // ========================================


        public static bool IsValidAdditionalFieldName(string fieldName) {
            return (fieldName != null) && AdditionalFieldNameExpr.IsMatch(fieldName) && (fieldName != "_id");
        }


        // ========================================


        /// <summary>
        /// Converts PascalCase to snake_case and replaces dots with underscores.
        /// Respects uppercase abbreviations.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string CanonicalizeFieldName([NotNull] string input) {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Cannot canonicalize null or empty input", nameof(input));

            var sb = new StringBuilder(input.Length);

            for (var i = 0; i < input.Length; i++) {
                var c = input[i];
                if (char.IsUpper(c)) {
                    if (i == 0) {
                        sb.Append(char.ToLower(c));
                    }
                    else {
                        if (!char.IsUpper(input[i - 1]) && (sb[sb.Length - 1] != '_')) {
                            sb.Append('_');
                        }

                        sb.Append(char.ToLower(c));
                    }
                }
                else if (c == '.') {
                    sb.Append('_');
                }
                else {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}