using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace RummeltSoftware.Gelf {
    [PublicAPI]
    public sealed class GelfMessageBuilder {
        public static readonly string Version = "1.1";

        private string _host;
        private string _shortMessage;

        private string _fullMessage;
        private DateTime? _timestamp;
        private SeverityLevel? _level;

        private Dictionary<string, object> _additionalFields;

        // ========================================

        public GelfMessageBuilder() { }


        public GelfMessageBuilder(string host, string message) {
            _host = host;
            _shortMessage = message;
        }


        // ========================================


        /// <summary>
        /// Creates and initializes a <see cref="GelfMessageBuilder"/> with the fields of the given <see cref="GelfMessage"/>.
        /// </summary>
        /// <param name="message">The message which fields are used to initialize the <see cref="GelfMessageBuilder"/> instance</param>
        /// <returns>A <see cref="GelfMessageBuilder"/> instance initialized with the fields of the given <see cref="GelfMessage"/></returns>
        public static GelfMessageBuilder FromMessage(GelfMessage message) {
            var builder = new GelfMessageBuilder(message.Host, message.ShortMessage)
                          .FullMessage(message.FullMessage)
                          .Timestamp(message.Timestamp)
                          .Level(message.Level);

            foreach (var additionalField in message.AdditionalFields) {
                builder.AdditionalField(additionalField.Key, additionalField.Value);
            }

            return builder;
        }


        // ========================================


        public GelfMessageBuilder Host([NotNull] string host) {
            if (string.IsNullOrEmpty(host))
                throw new ArgumentException($"{nameof(host)} cannot be null or empty", nameof(host));

            _host = host;
            return this;
        }


        public GelfMessageBuilder Message([NotNull] string message) {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentException($"{nameof(message)} cannot be null or empty", nameof(message));

            _shortMessage = message;
            return this;
        }


        public GelfMessageBuilder FullMessage(string message) {
            _fullMessage = message;
            return this;
        }


        public GelfMessageBuilder Timestamp(DateTime? timestamp) {
            if (timestamp.HasValue && (timestamp.Value.Kind != DateTimeKind.Utc))
                throw new ArgumentException($"{nameof(timestamp)} must be UTC if set", nameof(timestamp));

            _timestamp = timestamp;
            return this;
        }


        public GelfMessageBuilder Level(SeverityLevel? level) {
            _level = level;
            return this;
        }


        public GelfMessageBuilder AdditionalField([NotNull] string key, [NotNull] object value) {
            switch (value) {
                case int v: return AdditionalField(key, v);
                case double v: return AdditionalField(key, v);
                case string v: return AdditionalField(key, v);
                default: return AdditionalField(key, value?.ToString());
            }
        }


        public GelfMessageBuilder AdditionalField([NotNull] string key, [NotNull] string value) {
            if (!GelfHelper.IsValidAdditionalFieldName(key))
                throw new ArgumentException($"Invalid GELF additional field name: {key}", nameof(key));
            if (value == null)
                throw new ArgumentNullException(nameof(value), "Value cannot be null");

            EnsureAdditionalFieldsDictionary();

            _additionalFields[key] = value;
            return this;
        }


        public GelfMessageBuilder AdditionalField([NotNull] string key, int value) {
            if (!GelfHelper.IsValidAdditionalFieldName(key))
                throw new ArgumentException($"Invalid GELF additional field name: {key}");

            EnsureAdditionalFieldsDictionary();

            _additionalFields[key] = value;
            return this;
        }


        public GelfMessageBuilder AdditionalField([NotNull] string key, double value) {
            if (!GelfHelper.IsValidAdditionalFieldName(key))
                throw new ArgumentException($"Invalid GELF additional field name: {key}");

            EnsureAdditionalFieldsDictionary();

            _additionalFields[key] = value;
            return this;
        }


        public GelfMessage Build() {
            if (_host == null)
                throw new InvalidOperationException("Host must be set");
            if (_shortMessage == null)
                throw new InvalidOperationException("ShortMessage must be set");

            return new GelfMessage(Version, _host, _shortMessage, _fullMessage, _timestamp, _level, _additionalFields);
        }


        // ========================================


        private void EnsureAdditionalFieldsDictionary() {
            if (_additionalFields == null) {
                _additionalFields = new Dictionary<string, object>();
            }
        }
    }
}