using System;
using JetBrains.Annotations;

namespace RummeltSoftware.Gelf.Listener {
    [PublicAPI]
    public sealed class GelfMessageReadExceptionEventArgs : EventArgs {
        public Exception Exception { get; }

        // ========================================


        public GelfMessageReadExceptionEventArgs(Exception exception) {
            Exception = exception;
        }
    }
}