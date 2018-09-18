using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using RummeltSoftware.Gelf.Internal;

namespace RummeltSoftware.Gelf.Listener.Internal {
    /// <summary>
    /// <b>NOT</b> thread-safe.
    /// </summary>
    internal class GelfMessageDecoder {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // ========================================

        private readonly GelfJsonStreamReader _reader;
        private readonly Dictionary<string, object> _fieldBuffer;

        // ========================================


        public GelfMessageDecoder() {
            _reader = new GelfJsonStreamReader();
            _fieldBuffer = new Dictionary<string, object>(16);
        }


        // ========================================


        [CanBeNull]
        public virtual GelfMessageReceivedEventArgs Decode([NotNull] RawGelfMessage rawMessage) {
            using (rawMessage) {
                var bytes = rawMessage.Bytes;
                var compressionMethod = GetCompressionMethod(bytes);

                GelfMessage message;
                if (compressionMethod == CompressionMethod.None) {
                    using (var stream = new MemoryStream(bytes, 0, rawMessage.ByteCount, false)) {
                        message = Decode(stream);
                    }
                }
                else {
                    using (var compressed = new MemoryStream(bytes, 0, rawMessage.ByteCount, false))
                    using (var uncompressed = MemoryStreamPool.GetStream("GelfListener.Decode")) {
                        Decompress(compressed, uncompressed, compressionMethod);
                        uncompressed.Position = 0L;

                        message = Decode(uncompressed);
                    }
                }

                return new GelfMessageReceivedEventArgs(message, rawMessage.ReceivedFrom, rawMessage.ReceivedAt);
            }
        }


        // ========================================


        protected GelfMessage Decode(Stream stream) {
            _fieldBuffer.Clear();

            _reader.ReadStart(stream);
            _reader.ReadAllFields(_fieldBuffer);

            if (!_fieldBuffer.TryGetValue("version", out var version))
                throw new GelfMessageFormatException("Message does not have a version field");

            switch (version as string) {
                case "1.1":
                    return Decode_v1_1();
                default:
                    throw new GelfMessageFormatException("Unsupported GELF version: " + version);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private GelfMessage Decode_v1_1() {
            if (!_fieldBuffer.TryGetString("host", out var host))
                throw new GelfMessageFormatException("Message does not have a valid host field");
            if (!_fieldBuffer.TryGetString("short_message", out var shortMessage))
                throw new GelfMessageFormatException("Message does not have a valid short_message field");

            string fullMessage = null;
            if (_fieldBuffer.TryGetValue("full_message", out var fullMessageObj)) {
                fullMessage = fullMessageObj as string;
            }

            DateTime? timestamp = null;
            if (_fieldBuffer.TryGetValue("timestamp", out var timestampObj)) {
                timestamp = ParseAsDateTime(timestampObj);
            }

            SeverityLevel? severityLevel = null;
            if (_fieldBuffer.TryGetValue("level", out var levelObj)) {
                severityLevel = ParseAsSeverityLevel(levelObj);
            }

            var additionalFields = GetAdditionalFields();

            // Add optional deprecated fields as additional fields
            if (_fieldBuffer.TryGetValue("facility", out var facilityObj)) {
                additionalFields["_facility"] = facilityObj;
            }

            if (_fieldBuffer.TryGetValue("line", out var lineObj)) {
                additionalFields["_line"] = lineObj;
            }

            if (_fieldBuffer.TryGetValue("file", out var fileObj)) {
                additionalFields["_file"] = fileObj;
            }

            return new GelfMessage("1.1", host, shortMessage, fullMessage, timestamp, severityLevel, additionalFields);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Dictionary<string, object> GetAdditionalFields() {
            var result = new Dictionary<string, object>();
            foreach (var pair in _fieldBuffer) {
                if (!pair.Key.StartsWith("_"))
                    continue;

                result[pair.Key] = pair.Value;
            }

            return result;
        }


        // ========================================


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static DateTime ParseAsDateTime(object obj) {
            switch (obj) {
                case int i:
                    return UnixEpoch.AddSeconds(i);
                case long l:
                    return UnixEpoch.AddSeconds(l);
                case double d:
                    return UnixEpoch.AddSeconds(d);
            }

            throw new ArgumentException($"Cannot parse {obj.GetType().FullName} as {nameof(DateTime)})");
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static SeverityLevel ParseAsSeverityLevel(object obj) {
            return (SeverityLevel) (int) Convert.ChangeType(obj, typeof(int));
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static CompressionMethod GetCompressionMethod(byte[] bytes, int offset = 0) {
            if (IsGzipCompressed(bytes, offset))
                return CompressionMethod.GZIP;
            return CompressionMethod.None;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Decompress(Stream input, Stream output, CompressionMethod method) {
            using (var deflater = CreateDecompressingStream(method, input)) {
                deflater.CopyToPooled(output);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static Stream CreateDecompressingStream(CompressionMethod method, Stream input) {
            switch (method) {
                case CompressionMethod.GZIP:
                    return new GZipStream(input, CompressionMode.Decompress, true);
                default:
                    throw new ArgumentOutOfRangeException(nameof(method), method, "Unknown compression method");
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsGzipCompressed(byte[] bytes, int offset) {
            return bytes[offset] == 0x1F && bytes[offset + 1] == 0x8B;
        }
    }
}