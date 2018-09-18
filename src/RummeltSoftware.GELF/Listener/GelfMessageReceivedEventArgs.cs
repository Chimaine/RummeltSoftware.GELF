using System;
using System.Net;
using JetBrains.Annotations;

namespace RummeltSoftware.Gelf.Listener {
    [PublicAPI]
    public sealed class GelfMessageReceivedEventArgs : EventArgs {
        public GelfMessage Message { get; }

        public IPEndPoint RecievedFrom { get; }

        public DateTime RecievedAt { get; }

        // ========================================


        public GelfMessageReceivedEventArgs(GelfMessage message, IPEndPoint recievedFrom, DateTime recievedAt) {
            Message = message;
            RecievedAt = recievedAt;
            RecievedFrom = recievedFrom;
        }
    }
}