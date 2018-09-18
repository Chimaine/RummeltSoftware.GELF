using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace RummeltSoftware.Gelf {
    [PublicAPI]
    public sealed class GelfMessage {
        public string Version { get; }

        public string Host { get; }

        public string ShortMessage { get; }

        public string FullMessage { get; }

        public DateTime? Timestamp { get; }

        public SeverityLevel? Level { get; }

        public IReadOnlyDictionary<string, object> AdditionalFields { get; }

        // ========================================


        internal GelfMessage(string version, string host, string shortMessage, string fullMessage, DateTime? timestamp, SeverityLevel? level,
                             Dictionary<string, object> additionalFields) {
            Version = version;
            Host = host;
            ShortMessage = shortMessage;
            FullMessage = fullMessage;
            Timestamp = timestamp;
            Level = level;
            AdditionalFields = additionalFields;
        }


        // ========================================


        public IDictionary<string, object> AsDictionary() {
            var result = new Dictionary<string, object>() {
                ["version"] = Version,
                ["host"] = Host,
                ["short_message"] = ShortMessage,
            };

            if (!string.IsNullOrEmpty(FullMessage))
                result["full_message"] = FullMessage;
            if (Timestamp.HasValue)
                result["timestamp"] = Timestamp.Value;
            if (Level.HasValue)
                result["level"] = Level.Value;

            if (AdditionalFields != null) {
                foreach (var pair in AdditionalFields) {
                    result[pair.Key] = pair.Value;
                }
            }

            return result;
        }
    }
}