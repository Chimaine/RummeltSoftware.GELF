using System;
using System.Net;
using JetBrains.Annotations;

namespace RummeltSoftware.Gelf.Listener {
    [PublicAPI]
    public interface IGelfListener : IDisposable {
        bool SupportsChunking { get; }

        bool SupportsCompression { get; }

        event EventHandler<GelfMessageReceivedEventArgs> MessageReceived;

        event EventHandler<GelfMessageDroppedEventArgs> MessageDropped;

        event EventHandler<GelfMessageReadExceptionEventArgs> ReadException;

        void Open(IPEndPoint endpoint);

        void Close();
    }
}