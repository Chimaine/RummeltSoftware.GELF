using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace RummeltSoftware.Gelf.Client.Internal {
    /// <summary>
    /// Highly specialized JSON writer that only implements what is needed to write GELF.
    /// </summary>
    internal sealed class GelfJsonStreamWriter {
        private readonly Encoder _encoder;

        private readonly char[] _charBuffer;
        private readonly byte[] _byteBuffer;

        private readonly int _flushThreshold;
        private int _charPos;

        // ========================================

        public Stream Stream { get; private set; }

        // ========================================


        public GelfJsonStreamWriter(int bufferSize = 1024) {
            _encoder = GelfHelper.Encoding.GetEncoder();

            _charBuffer = new char[bufferSize];
            _byteBuffer = new byte[GelfHelper.Encoding.GetMaxByteCount(bufferSize)];
            _flushThreshold = bufferSize - 6;
        }


        // ========================================


        public void Start(Stream stream) {
            Stream = stream;

            _charPos = 0;
            _charBuffer[_charPos++] = '{';
        }


        public void Write(string field, object value) {
            switch (value) {
                case string s:
                    Write(field, s);
                    break;
                case int i:
                    Write(field, i);
                    break;
                case long l:
                    Write(field, l);
                    break;
                case double d:
                    Write(field, d);
                    break;
                default:
                    Write(field, value?.ToString());
                    break;
            }
        }


        public void Write(string field, string value) {
            if (value == null) {
                WritePropertyKey(field);
                WriteNull();
            }
            else {
                WritePropertyKey(field);
                WritePropertyValue(value);
            }

            WriteFieldDelimiter();
        }


        public void Write(string field, int value) {
            WritePropertyKey(field);
            WriteUnescaped(value.ToString(CultureInfo.InvariantCulture));
            WriteFieldDelimiter();
        }


        public void Write(string field, long value) {
            WritePropertyKey(field);
            WriteUnescaped(value.ToString(CultureInfo.InvariantCulture));
            WriteFieldDelimiter();
        }


        public void Write(string field, double value) {
            WritePropertyKey(field);
            WriteUnescaped(value.ToString("G17", CultureInfo.InvariantCulture));
            WriteFieldDelimiter();
        }


        public void Finish() {
            _charBuffer[_charPos - 1] = '}'; // Overrides last ,
            Flush();

            Stream = null;
        }


        // ========================================


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WritePropertyKey(string key) {
            _charBuffer[_charPos++] = '"';
            WriteEscaped(key);
            _charBuffer[_charPos++] = '"';
            _charBuffer[_charPos++] = ':';
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WritePropertyValue(string value) {
            _charBuffer[_charPos++] = '"';
            WriteEscaped(value);
            _charBuffer[_charPos++] = '"';
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteFieldDelimiter() {
            _charBuffer[_charPos++] = ',';
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteNull() {
            _charBuffer[_charPos++] = 'n';
            _charBuffer[_charPos++] = 'u';
            _charBuffer[_charPos++] = 'l';
            _charBuffer[_charPos++] = 'l';
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteUnescaped(string s) {
            if (s.Length <= _charBuffer.Length) {
                FlushIfNeeded(s.Length);

                s.CopyTo(0, _charBuffer, _charPos, s.Length);
                _charPos += s.Length;
            }
            else {
                foreach (var c in s) {
                    FlushIfNeeded();
                    _charBuffer[_charPos++] = c;
                }
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteEscaped(string s) {
            for (var i = 0; i < s.Length; i++) {
                FlushIfNeeded();

                var c = s[i];
                switch (c) {
                    case '\"':
                        _charBuffer[_charPos++] = '\\';
                        _charBuffer[_charPos++] = '"';
                        break;
                    case '\\':
                        _charBuffer[_charPos++] = '\\';
                        _charBuffer[_charPos++] = '\\';
                        break;
                    case '/':
                        _charBuffer[_charPos++] = '\\';
                        _charBuffer[_charPos++] = '/';
                        break;
                    case '\b':
                        _charBuffer[_charPos++] = '\\';
                        _charBuffer[_charPos++] = 'b';
                        break;
                    case '\f':
                        _charBuffer[_charPos++] = '\\';
                        _charBuffer[_charPos++] = 'f';
                        break;
                    case '\n':
                        _charBuffer[_charPos++] = '\\';
                        _charBuffer[_charPos++] = 'n';
                        break;
                    case '\r':
                        _charBuffer[_charPos++] = '\\';
                        _charBuffer[_charPos++] = 'r';
                        break;
                    case '\t':
                        _charBuffer[_charPos++] = '\\';
                        _charBuffer[_charPos++] = 't';
                        break;
                    default: {
                        var ci = (int) c;

                        // C0 & C1 characters
                        if ((c == 0x007F) || (c <= 0x001F) || (0x0080 <= c && c <= 0x009F)) {
                            _charBuffer[_charPos++] = '\\';
                            _charBuffer[_charPos++] = 'u';
                            _charBuffer[_charPos++] = ToHexChar((ci >> 12) & 15);
                            _charBuffer[_charPos++] = ToHexChar((ci >> 8) & 15);
                            _charBuffer[_charPos++] = ToHexChar((ci >> 4) & 15);
                            _charBuffer[_charPos++] = ToHexChar(ci & 15);
                        }
                        else {
                            _charBuffer[_charPos++] = c;
                        }

                        break;
                    }
                }
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static char ToHexChar(int b) {
            return b < 10 ? (char) (b + 48) : (char) (b + 55);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FlushIfNeeded(int spaceNeeded = 1) {
            if ((_charPos + spaceNeeded) > _flushThreshold) {
                Flush();
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void Flush() {
            int bytesWritten;
            fixed (char* charPtr = &_charBuffer[0])
            fixed (byte* bytePtr = &_byteBuffer[0]) {
                bytesWritten = _encoder.GetBytes(charPtr, _charPos, bytePtr, _byteBuffer.Length, true);
            }

            Stream.Write(_byteBuffer, 0, bytesWritten);
            _charPos = 0;
        }
    }
}