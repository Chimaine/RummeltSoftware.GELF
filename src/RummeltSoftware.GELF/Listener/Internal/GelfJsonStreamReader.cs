using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace RummeltSoftware.Gelf.Listener.Internal {
    /// <summary>
    /// Highly specialized JSON reader that only implements what is needed to read GELF.
    /// </summary>
    internal sealed class GelfJsonStreamReader {
        private readonly Decoder _decoder;

        private readonly char[] _charBuffer;
        private readonly byte[] _byteBuffer;
        private readonly StringBuilder _stringBuffer;

        private int _charPos;
        private int _maxCharPos;
        private bool _eos;

        // ========================================

        public Stream Stream { get; private set; }

        // ========================================


        public GelfJsonStreamReader(int bufferSize = 1024) {
            _decoder = GelfHelper.Encoding.GetDecoder();

            var actualBufferSize = GelfHelper.Encoding.GetMaxByteCount(bufferSize);
            _charBuffer = new char[actualBufferSize];
            _byteBuffer = new byte[actualBufferSize];
            _stringBuffer = new StringBuilder(1024);
        }


        // ========================================


        public void ReadStart(Stream stream) {
            Stream = stream;
            _charPos = 0;
            _maxCharPos = 0;
            _eos = false;

            EnsureReadable(2);
            if (!Seek('{'))
                throw new EndOfStreamException("Reached EOS while seeking for start of JSON");
        }


        public bool TryReadField(out string key, out object value) {
            if (!Seek('"')) {
                key = null;
                value = null;
                return false;
            }

            key = ReadEscapedString();
            SeekStartOfValue();
            value = ReadFieldValue();

            return true;
        }


        public void ReadAllFields(Dictionary<string, object> target) {
            while (TryReadField(out var key, out var value)) {
                target[key] = value;
            }
        }


        // ========================================


        private object ReadFieldValue() {
            var c = _charBuffer[_charPos];
            switch (c) {
                case 'n':
                case 'N':
                    return null;
                case '"':
                    return ReadEscapedString();
            }

            if (c == '-' || char.IsDigit(c))
                return ReadNumber();

            throw new IOException($"Expected start of string, number or null, got {c}");
        }


        private object ReadNumber() {
            _stringBuffer.Clear();

            var hasFractals = false;
            for (var i = _charPos; i < _charBuffer.Length; _charPos++, i++) {
                var c = _charBuffer[i];
                switch (c) {
                    case '.':
                        hasFractals = true;
                        break;
                    //case 'e':
                    //case 'E':
                    //case '-':
                    //    break;
                    case ',':
                    case '}':
                    case var _ when char.IsWhiteSpace(c): {
                        if (hasFractals) {
                            const NumberStyles style = NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint;
                            return double.Parse(_stringBuffer.ToString(), style, CultureInfo.InvariantCulture);
                        }
                        else {
                            const NumberStyles style = NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent;
                            var number = long.Parse(_stringBuffer.ToString(), style, CultureInfo.InvariantCulture);
                            if ((int.MinValue <= number) && (number <= int.MaxValue))
                                return (int) number;
                            return number;
                        }
                    }
                }

                _stringBuffer.Append(c);

                EnsureReadable();
            }

            throw new EndOfStreamException();
        }


        private bool Seek(char target) {
            for (var i = _charPos; i < _charBuffer.Length; _charPos++, i++) {
                var c = _charBuffer[i];
                if (c == target)
                    return true;
                if (c == '}')
                    return false;
                if (_charPos < _maxCharPos)
                    continue;
                if (_eos)
                    return false;

                ReadStream();
            }

            return false;
        }


        private void SeekStartOfValue() {
            for (var i = _charPos; i < _charBuffer.Length; _charPos++, i++) {
                var c = _charBuffer[i];
                if (!char.IsWhiteSpace(c) && (c != ':'))
                    return;

                EnsureReadable();
            }
        }


        private string ReadEscapedString() {
            _stringBuffer.Clear();

            _charPos++; // Skip "

            // We might hit the end of the buffer without reaching the end of the string.
            for (;;) {
                for (; _charPos < _charBuffer.Length; _charPos++) {
                    var c = _charBuffer[_charPos];
                    switch (c) {
                        case '"':
                            _charPos++; // Skip "
                            return _stringBuffer.ToString();
                        case '\\':
                            _charPos++; // Skip \
                            ReadEscapedChar();
                            break;
                        default:
                            _stringBuffer.Append(c);
                            break;
                    }
                }

                EnsureReadable();
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReadEscapedChar() {
            var c = _charBuffer[_charPos];
            switch (c) {
                case '"':
                case '\\':
                case '/':
                    _stringBuffer.Append(c);
                    return;
                case 'b':
                    _stringBuffer.Append('\b');
                    return;
                case 'f':
                    _stringBuffer.Append('\f');
                    return;
                case 'n':
                    _stringBuffer.Append('\n');
                    return;
                case 'r':
                    _stringBuffer.Append('\r');
                    return;
                case 't':
                    _stringBuffer.Append('\t');
                    return;
                case 'u': {
                    _charPos++;
                    EnsureReadable(4);

                    var ic = (FromHexChar(_charBuffer[_charPos++]) << 12) |
                             (FromHexChar(_charBuffer[_charPos++]) << 8) |
                             (FromHexChar(_charBuffer[_charPos++]) << 4) |
                             (FromHexChar(_charBuffer[_charPos]));

                    _stringBuffer.Append((char) ic);
                    return;
                }
                default:
                    throw new IOException($"Unexpected escaped character: {c}");
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FromHexChar(char c) {
            if ('0' <= c && c <= '9')
                return c - '0';
            if ('A' <= c && c <= 'F')
                return c - ('A' - 10);
            if ('a' <= c && c <= 'f')
                return c - ('a' - 10);
            throw new ArgumentOutOfRangeException();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureReadable() {
            if (_charPos < _maxCharPos)
                return;
            if (_eos)
                throw new EndOfStreamException();

            ReadStream();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureReadable(int count) {
            if ((_charPos + count) < _maxCharPos)
                return;
            if (_eos)
                throw new EndOfStreamException();

            ReadStream();

            if ((_charPos + count) > _maxCharPos)
                throw new EndOfStreamException();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void ReadStream() {
            // Stream is controlled by us and will be some kind of in-memory stream.
            // These streams will only return less than count when they reach EOS.
            var bytesRead = Stream.Read(_byteBuffer, 0, _byteBuffer.Length);
            _eos = bytesRead < _byteBuffer.Length;
            _charPos = 0;

            if (bytesRead == 0) {
                _maxCharPos = 0;
                return;
            }

            fixed (byte* bytePtr = &_byteBuffer[0])
            fixed (char* charPtr = &_charBuffer[0]) {
                // No worries about overflowing the char buffer here. We scaled the byte buffer for the worst case (1 byte == 1 char)
                _maxCharPos = _decoder.GetChars(bytePtr, bytesRead, charPtr, _charBuffer.Length, _eos);
            }
        }
    }
}