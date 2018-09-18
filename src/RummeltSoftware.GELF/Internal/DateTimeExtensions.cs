using System;

namespace RummeltSoftware.Gelf.Internal {
    internal static class DateTimeExtensions {
        public static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // ========================================


        public static double ToUnixTimestamp(this DateTime dateTime) {
            return Math.Round((dateTime - UnixEpoch).TotalSeconds, 3);
        }
    }
}