using JetBrains.Annotations;

namespace RummeltSoftware.Gelf {
    /// <summary>
    ///     Syslog severity level.
    /// </summary>
    [PublicAPI]
    public enum SeverityLevel {
        Emergency = 0,
        Alert = 1,
        Critical = 2,
        Error = 3,
        Warning = 4,
        Notice = 5,
        Informational = 6,
        Debug = 7,
        Trace = 8,
    }
}