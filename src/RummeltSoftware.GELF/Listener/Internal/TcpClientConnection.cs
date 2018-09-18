using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using JetBrains.Annotations;
using RummeltSoftware.Gelf.Internal;

namespace RummeltSoftware.Gelf.Listener.Internal {
    internal sealed class TcpClientConnection : IDisposable {
        private readonly Socket _socket;
        private readonly byte[] _messageBuffer;
        private int _messageBufferOffset;

        private bool _isDisposed;


        public TcpClientConnection(Socket socket, int messageBufferSize) {
            _socket = socket;
            _messageBuffer = ByteArrayPool.Rent(messageBufferSize);
        }


        public void BeginReceive(AsyncCallback callback) {
            var offset = _messageBufferOffset;
            var size = _messageBufferOffset - _messageBuffer.Length;
            _socket.BeginReceive(_messageBuffer, offset, size, SocketFlags.None, callback, this);
        }


        [CanBeNull]
        public List<RawGelfMessage> EndReceive(IAsyncResult asyncResult) {
            var receivedBytes = _socket.EndReceive(asyncResult);
            var buffer = _messageBuffer;
            var bufferOffset = _messageBufferOffset;
            var bufferPosition = bufferOffset + receivedBytes;

            List<RawGelfMessage> messages = null;

            if (bufferOffset < 0 || bufferPosition > buffer.Length) // Eliminate array bound checks
                throw new IndexOutOfRangeException();

            var messageStart = 0;
            for (var i = bufferOffset; i < bufferPosition; i++) {
                if (buffer[i] != 0)
                    continue;

                var messageLength = i - messageStart;
                var messageBuffer = ByteArrayPool.Rent(messageLength);

                Buffer.BlockCopy(buffer, messageStart, messageBuffer, 0, messageLength);

                var message = CreateMessage(messageBuffer, messageLength);
                (messages ?? (messages = new List<RawGelfMessage>(2))).Add(message);

                messageStart = i + 1;
            }

            // At least 1 complete message
            if (messageStart > 0) {
                // Move remaining incomplete messages to 0 offset
                if (messageStart < bufferPosition) {
                    var size = bufferPosition - messageStart;
                    Buffer.BlockCopy(buffer, messageStart, buffer, 0, size);
                }

                _messageBufferOffset = 0;
            }
            // No complete message yet
            else {
                _messageBufferOffset = bufferPosition;
            }

            return messages;
        }


        private RawGelfMessage CreateMessage(byte[] bytes, int byteCount) {
            var from = _socket.RemoteEndPoint as IPEndPoint;
            return new RawGelfMessage(bytes, byteCount, from, DateTime.UtcNow);
        }


        public void Dispose() {
            if (_isDisposed)
                return;

            _isDisposed = true;

            ReleaseResources();
            GC.SuppressFinalize(this);
        }


        ~TcpClientConnection() {
            ReleaseResources();
        }


        private void ReleaseResources() {
            ByteArrayPool.Return(_messageBuffer);

            _socket.Close();
        }
    }
}