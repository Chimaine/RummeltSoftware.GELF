using System.IO;
using System.Net.Sockets;

namespace RummeltSoftware.Gelf.Client {
    /// <summary>
    /// TCP sockets based GELF client, implementing the official spec for TCP. Does <b>NOT</b> support compression or chunking.
    /// </summary>
    public sealed class TcpGelfClient : GelfClientBase {
        private readonly TcpClient _client;

        // ========================================


        public TcpGelfClient(string host, int port, GelfClientSettings settings)
            : base(settings) {
            Host = host;
            Port = port;

            _client = new TcpClient {
                LingerState = new LingerOption(false, 0),
                NoDelay = true,
            };
        }


        // ========================================

        public override bool SupportsChunking => false;

        public override bool SupportsCompression => false;

        public string Host { get; }

        public int Port { get; }

        // ========================================


        protected override void Send(MemoryStream stream) {
            if (!_client.Connected) {
                _client.Connect(Host, Port);
            }

            var networkStream = _client.GetStream();

            stream.WriteTo(networkStream);
            networkStream.WriteByte(0);
            networkStream.Flush();
        }


        // ========================================


        public override void Dispose() {
            _client.Dispose();
        }
    }
}