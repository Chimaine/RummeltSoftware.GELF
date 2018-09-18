using System;
using JetBrains.Annotations;

namespace RummeltSoftware.Gelf {
    [PublicAPI]
    public class GelfException : Exception {
        internal GelfException() { }


        internal GelfException(string message)
            : base(message) { }


        internal GelfException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}