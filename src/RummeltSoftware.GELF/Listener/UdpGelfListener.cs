using System;
using System.Net;
using System.Net.Sockets;
using RummeltSoftware.Gelf.Internal;
using RummeltSoftware.Gelf.Listener.Internal;

namespace RummeltSoftware.Gelf.Listener {
    /// <summary>
    /// UDP sockets based GELF listener, implementing the official spec for UDP. Supports compression and chunking.
    /// </summary>
    public sealed class UdpGelfListener : ChunkedGelfListenerBase {
        private Socket _socket;

        // ========================================


        public UdpGelfListener(GelfListenerSettings settings = null)
            : base(settings) { }


        // ========================================

        public override bool SupportsCompression => true;

        // ========================================


        protected override void OnOpening(IPEndPoint endpoint) {
            if (endpoint.AddressFamily != AddressFamily.InterNetwork && endpoint.AddressFamily != AddressFamily.InterNetworkV6)
                throw new ArgumentException($"{nameof(AddressFamily)} of {nameof(endpoint)} must be " +
                                            $"either {nameof(AddressFamily.InterNetwork)} or {nameof(AddressFamily.InterNetworkV6)}");

            _socket = new Socket(endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            _socket.Bind(endpoint);
        }


        protected override void OnOpened() {
            BeginReceive();
        }


        protected override void OnClosed() {
            _socket.Dispose();
        }


        // ========================================


        private void BeginReceive() {
            var buffer = ByteArrayPool.Rent(MaxChunkSize);
            var receiveEndpoint = GetReceiveEndpoint();
            _socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref receiveEndpoint, EndReceive, buffer);
        }


        private void EndReceive(IAsyncResult asyncResult) {
            if (!IsOpen)
                return;

            var receiveEndpoint = GetReceiveEndpoint();
            var bytesReceived = _socket.EndReceiveFrom(asyncResult, ref receiveEndpoint);
            var bytes = (byte[]) asyncResult.AsyncState;

            BeginReceive();
            NotifyRawMessageReceived(new RawGelfMessage(bytes, bytesReceived, (IPEndPoint) receiveEndpoint, DateTime.UtcNow));
        }


        private EndPoint GetReceiveEndpoint() {
            return _socket.AddressFamily == AddressFamily.InterNetwork
                ? new IPEndPoint(IPAddress.Any, 0)
                : new IPEndPoint(IPAddress.IPv6Any, 0);
        }
    }
}