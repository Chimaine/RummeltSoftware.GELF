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
                             IReadOnlyDictionary<string, object> additionalFields) {
            Version = version;
            Host = host;
            ShortMessage = shortMessage;
            FullMessage = fullMessage;
            Timestamp = timestamp;
            Level = level;
            AdditionalFields = additionalFields;
        }


        // ========================================


        /// <summary>
        /// Returns a copy of this message that can be changed by an action on a <see cref="GelfMessageBuilder"/>.
        /// </summary>
        /// <param name="with">An action on a <see cref="GelfMessageBuilder"/> that can make changes before the copy is returned.</param>
        /// <returns>A copy of this message with the changes in the given action applied.</returns>
        public GelfMessage With([NotNull] Action<GelfMessageBuilder> with) {
            if (with == null)
                throw new ArgumentNullException(nameof(with));

            var builder = GelfMessageBuilder.FromMessage(this);
            with(builder);
            return builder.Build();
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


        // ========================================


        internal GelfMessage Clone() {
            var additionalFields = new Dictionary<string, object>(AdditionalFields.Count);
            foreach (var pair in AdditionalFields) {
                additionalFields.Add(pair.Key, pair.Value);
            }

            return new GelfMessage(Version, Host, ShortMessage, FullMessage, Timestamp, Level, additionalFields);
        }
    }
}