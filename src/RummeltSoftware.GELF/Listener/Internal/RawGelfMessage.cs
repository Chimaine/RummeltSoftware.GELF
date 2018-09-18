using System;
using System.Net;
using RummeltSoftware.Gelf.Internal;

namespace RummeltSoftware.Gelf.Listener.Internal {
    internal sealed class RawGelfMessage : IDisposable {
        public byte[] Bytes { get; }

        public int ByteCount { get; }

        public IPEndPoint ReceivedFrom { get; }

        public DateTime ReceivedAt { get; }

        // ========================================


        public RawGelfMessage(byte[] bytes, int byteCount, IPEndPoint receivedFrom, DateTime receivedAt) {
            Bytes = bytes;
            ByteCount = byteCount;
            ReceivedFrom = receivedFrom;
            ReceivedAt = receivedAt;
        }

        // ========================================

        // Normally should be suppressed by Dispose
        ~RawGelfMessage() {
            ReleaseResources();
        }


        public void Dispose() {
            ReleaseResources();
            GC.SuppressFinalize(this);
        }


        private void ReleaseResources() {
            ByteArrayPool.Return(Bytes);
        }
    }
}