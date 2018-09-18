using System.IO;
using System.Runtime.CompilerServices;
using RummeltSoftware.Gelf.Internal;

namespace RummeltSoftware.Gelf.Client.Internal {
    internal sealed class GelfMessageEncoder {
        private readonly GelfJsonStreamWriter _writer;

        // ========================================


        public GelfMessageEncoder() {
            _writer = new GelfJsonStreamWriter();
        }


        // ========================================


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Encode(GelfMessage message, Stream stream) {
            switch (message.Version) {
                case "1.1":
                    Encode_v1_1(message, stream);
                    break;
                default:
                    throw new GelfMessageFormatException("Unsupported GELF version: " + message.Version);
            }
        }


        // ========================================


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Encode_v1_1(GelfMessage message, Stream stream) {
            var writer = _writer;

            writer.Start(stream);
            writer.Write("version", message.Version);
            writer.Write("host", message.Host);
            writer.Write("short_message", message.ShortMessage);

            if (!string.IsNullOrEmpty(message.FullMessage))
                writer.Write("full_message", message.FullMessage);
            if (message.Timestamp.HasValue)
                writer.Write("timestamp", message.Timestamp.Value.ToUnixTimestamp());
            if (message.Level.HasValue)
                writer.Write("level", (int) message.Level.Value);

            if (message.AdditionalFields != null) {
                foreach (var entry in message.AdditionalFields) {
                    writer.Write(entry.Key, entry.Value);
                }
            }

            writer.Finish();
        }
    }
}