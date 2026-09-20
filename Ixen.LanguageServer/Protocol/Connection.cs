using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Ixen.LanguageServer.Protocol
{
    internal sealed class Connection
    {
        private const string CONTENT_LENGTH = "Content-Length:";
        private const int MAX_PAYLOAD = 64 * 1024 * 1024;

        private readonly Stream _input;
        private readonly Stream _output;
        private readonly object _gate = new object();

        public Connection(Stream input, Stream output)
        {
            _input = input;
            _output = output;
        }

        public JsonDocument Read()
        {
            int length = -1;

            while (true)
            {
                string line = ReadLine();

                if (line == null)
                {
                    return null;
                }

                if (line.Length == 0)
                {
                    break;
                }

                if (line.StartsWith(CONTENT_LENGTH, StringComparison.OrdinalIgnoreCase))
                {
                    string value = line.Substring(CONTENT_LENGTH.Length).Trim();

                    if (!int.TryParse(value, out length) || length < 0 || length > MAX_PAYLOAD)
                    {
                        return null;
                    }
                }
            }

            if (length < 0)
            {
                return null;
            }

            byte[] payload = new byte[length];
            int read = 0;

            while (read < length)
            {
                int taken = _input.Read(payload, read, length - read);

                if (taken <= 0)
                {
                    return null;
                }

                read += taken;
            }

            try
            {
                return JsonDocument.Parse(payload);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public void Write(byte[] payload)
        {
            byte[] header = Encoding.ASCII.GetBytes("Content-Length: " + payload.Length + "\r\n\r\n");

            lock (_gate)
            {
                _output.Write(header, 0, header.Length);
                _output.Write(payload, 0, payload.Length);
                _output.Flush();
            }
        }

        private string ReadLine()
        {
            StringBuilder line = new StringBuilder();

            while (true)
            {
                int value = _input.ReadByte();

                if (value < 0)
                {
                    return line.Length == 0 ? null : line.ToString();
                }

                if (value == '\n')
                {
                    return line.ToString();
                }

                if (value != '\r')
                {
                    line.Append((char)value);
                }
            }
        }
    }
}
