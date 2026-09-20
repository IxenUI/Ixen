using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Ixen.LanguageServer.UT.Protocol
{
    internal sealed class LspClient : IDisposable
    {
        private const int TIMEOUT = 30000;

        private readonly Process _process;
        private readonly Stream _input;
        private readonly Stream _output;
        private readonly List<JsonDocument> _kept = new List<JsonDocument>();
        private readonly List<JsonElement> _notifications = new List<JsonElement>();

        private int _nextId = 1;

        public LspClient()
        {
            string server = Path.Combine(AppContext.BaseDirectory, "Ixen.LanguageServer.dll");

            if (!File.Exists(server))
            {
                throw new FileNotFoundException("the server was not copied beside the tests", server);
            }

            ProcessStartInfo start = new ProcessStartInfo("dotnet", "exec \"" + server + "\"")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _process = Process.Start(start);
            _input = _process.StandardInput.BaseStream;
            _output = _process.StandardOutput.BaseStream;
        }

        public IReadOnlyList<JsonElement> Notifications => _notifications;

        public void Send(string method, string parameters)
        {
            string body = "{\"jsonrpc\":\"2.0\",\"method\":\"" + method + "\",\"params\":" + parameters + "}";

            Write(body);
        }

        public JsonElement Request(string method, string parameters)
        {
            int id = _nextId++;
            string body = "{\"jsonrpc\":\"2.0\",\"id\":" + id
                + ",\"method\":\"" + method + "\",\"params\":" + parameters + "}";

            Write(body);

            for (;;)
            {
                JsonElement message = Receive();

                if (message.TryGetProperty("id", out JsonElement answered)
                    && answered.ValueKind == JsonValueKind.Number && answered.GetInt32() == id)
                {
                    return message;
                }

                _notifications.Add(message);
            }
        }

        public JsonElement WaitForNotification(string method, string uriEnd)
        {
            foreach (JsonElement seen in _notifications)
            {
                if (Matches(seen, method, uriEnd))
                {
                    return seen;
                }
            }

            for (;;)
            {
                JsonElement message = Receive();

                _notifications.Add(message);

                if (Matches(message, method, uriEnd))
                {
                    return message;
                }
            }
        }

        private static bool Matches(JsonElement message, string method, string uriEnd)
        {
            if (!message.TryGetProperty("method", out JsonElement name) || name.GetString() != method)
            {
                return false;
            }

            if (uriEnd == null)
            {
                return true;
            }

            return message.TryGetProperty("params", out JsonElement parameters)
                && parameters.TryGetProperty("uri", out JsonElement uri)
                && uri.GetString().EndsWith(uriEnd, StringComparison.OrdinalIgnoreCase);
        }

        private void Write(string body)
        {
            byte[] payload = Encoding.UTF8.GetBytes(body);
            byte[] header = Encoding.ASCII.GetBytes("Content-Length: " + payload.Length + "\r\n\r\n");

            _input.Write(header, 0, header.Length);
            _input.Write(payload, 0, payload.Length);
            _input.Flush();
        }

        private JsonElement Receive()
        {
            int length = -1;

            for (;;)
            {
                string line = ReadLine();

                if (line == null)
                {
                    throw new IOException("the server closed its output");
                }

                if (line.Length == 0)
                {
                    break;
                }

                if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                {
                    length = int.Parse(line.Substring(15).Trim());
                }
            }

            if (length < 0)
            {
                throw new IOException("no content length in the answer");
            }

            byte[] payload = new byte[length];
            int read = 0;

            while (read < length)
            {
                int taken = _output.Read(payload, read, length - read);

                if (taken <= 0)
                {
                    throw new IOException("the answer was truncated");
                }

                read += taken;
            }

            JsonDocument document = JsonDocument.Parse(payload);

            _kept.Add(document);

            return document.RootElement;
        }

        private string ReadLine()
        {
            StringBuilder line = new StringBuilder();

            for (;;)
            {
                int value = _output.ReadByte();

                if (value < 0)
                {
                    return line.Length == 0 ? null : line.ToString();
                }

                if (value == '\r')
                {
                    if (_output.ReadByte() != '\n')
                    {
                        throw new IOException("a header line was not terminated by CRLF");
                    }

                    return line.ToString();
                }

                if (value == '\n')
                {
                    throw new IOException("a header line was terminated by LF alone");
                }

                line.Append((char)value);
            }
        }

        public void Dispose()
        {
            try
            {
                Send("exit", "null");
                _input.Flush();
            }
            catch (IOException)
            {
            }
            catch (ObjectDisposedException)
            {
            }

            if (!_process.WaitForExit(TIMEOUT))
            {
                _process.Kill(true);
            }

            foreach (JsonDocument document in _kept)
            {
                document.Dispose();
            }

            _process.Dispose();
        }

        public static string Quote(string value)
        {
            return JsonSerializer.Serialize(value);
        }
    }
}
