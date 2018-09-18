using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using RummeltSoftware.Gelf.Listener.Internal;

namespace RummeltSoftware.Gelf.Listener {
    /// <summary>
    /// TCP sockets based GELF listener, implementing the official spec for TCP. Does <b>NOT</b> support compression or chunking.
    /// </summary>
    public sealed class TcpGelfListener : GelfListenerBase {
        private readonly HashSet<TcpClientConnection> _activeConnections;

        private TcpListener _listener;

        // ========================================


        public TcpGelfListener(GelfListenerSettings settings = null)
            : base(settings) {
            _activeConnections = new HashSet<TcpClientConnection>();
        }


        // ========================================

        public override bool SupportsChunking => false;

        public override bool SupportsCompression => false;

        // ========================================


        protected override void OnOpening(IPEndPoint endpoint) {
            _listener = new TcpListener(endpoint);
        }


        protected override void OnOpened() {
            _listener.Start();
            BeginAccept();
        }


        protected override void OnClosed() {
            _listener.Stop();

            foreach (var connection in _activeConnections) {
                connection.Dispose();
            }

            _activeConnections.Clear();
        }


        private void BeginAccept() {
            try {
                _listener.BeginAcceptSocket(OnAccepted, null);
            }
            catch (Exception ex) {
                NotifyReadException(ex);
            }
        }


        private void OnAccepted(IAsyncResult asyncResult) {
            Socket socket;
            try {
                socket = _listener.EndAcceptSocket(asyncResult);
            }
            catch (Exception ex) {
                if (!(ex is ObjectDisposedException)) {
                    NotifyReadException(ex);
                }

                return;
            }

            BeginAccept();

            var connection = CreateConnection(socket);
            BeginReceive(connection);
        }


        private void BeginReceive(TcpClientConnection connection) {
            try {
                connection.BeginReceive(OnReceived);
            }
            catch (Exception ex) {
                CloseConnection(connection);

                if (!(ex is ObjectDisposedException)) {
                    NotifyReadException(ex);
                }
            }
        }


        private void OnReceived(IAsyncResult asyncResult) {
            if (!(asyncResult?.AsyncState is TcpClientConnection connection))
                throw new ArgumentException();

            List<RawGelfMessage> messages;
            try {
                messages = connection.EndReceive(asyncResult);
            }
            catch (Exception ex) {
                CloseConnection(connection);

                if (!(ex is ObjectDisposedException)) {
                    NotifyReadException(ex);
                }

                return;
            }

            BeginReceive(connection);

            if (messages == null)
                return;

            foreach (var message in messages) {
                NotifyRawMessageReceived(message);
            }
        }


        private TcpClientConnection CreateConnection(Socket socket) {
            var connection = new TcpClientConnection(socket, MaxUncompressedSize);
            _activeConnections.Add(connection);
            return connection;
        }


        private void CloseConnection(TcpClientConnection connection) {
            _activeConnections.Remove(connection);
            connection.Dispose();
        }
    }
}