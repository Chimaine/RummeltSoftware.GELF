using System;
using System.IO;
using System.Net;
using RummeltSoftware.Gelf.Client;
using RummeltSoftware.Gelf.Internal;
using RummeltSoftware.Gelf.Listener.Internal;

namespace RummeltSoftware.Gelf.Mocks {
    internal class MockGelfClient : GelfClientBase {
        public MockGelfClient(GelfClientSettings settings, bool supportsChunking, bool supportsCompression)
            : base(settings) {
            SupportsChunking = supportsChunking;
            SupportsCompression = supportsCompression;
        }


        public override bool SupportsChunking { get; }

        public override bool SupportsCompression { get; }

        public RawGelfMessage LastSendMessage { get; private set; }


        protected override void Send(MemoryStream stream) {
            var byteCount = (int) stream.Length;
            var bytes = ByteArrayPool.Rent(byteCount);
            var byteStream = new MemoryStream(bytes);
            stream.WriteTo(byteStream);
            LastSendMessage = new RawGelfMessage(bytes, byteCount, new IPEndPoint(IPAddress.Any, 0), DateTime.UtcNow);
        }


        public override void Dispose() {
            // NOOP
        }
    }
}