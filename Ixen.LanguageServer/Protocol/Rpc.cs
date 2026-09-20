using System;
using System.IO;
using System.Text.Json;

namespace Ixen.LanguageServer.Protocol
{
    internal static class Rpc
    {
        public const int METHOD_NOT_FOUND = -32601;
        public const int INTERNAL_ERROR = -32603;

        public static byte[] Result(JsonElement id, Action<Utf8JsonWriter> body)
        {
            return Build(writer =>
            {
                writer.WriteString("jsonrpc", "2.0");
                writer.WritePropertyName("id");
                id.WriteTo(writer);
                writer.WritePropertyName("result");

                if (body == null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    body(writer);
                }
            });
        }

        public static byte[] Error(JsonElement id, int code, string message)
        {
            return Build(writer =>
            {
                writer.WriteString("jsonrpc", "2.0");
                writer.WritePropertyName("id");
                id.WriteTo(writer);
                writer.WriteStartObject("error");
                writer.WriteNumber("code", code);
                writer.WriteString("message", message ?? string.Empty);
                writer.WriteEndObject();
            });
        }

        public static byte[] Notification(string method, Action<Utf8JsonWriter> body)
        {
            return Build(writer =>
            {
                writer.WriteString("jsonrpc", "2.0");
                writer.WriteString("method", method);
                writer.WritePropertyName("params");

                if (body == null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    body(writer);
                }
            });
        }

        private static byte[] Build(Action<Utf8JsonWriter> body)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (Utf8JsonWriter writer = new Utf8JsonWriter(stream))
                {
                    writer.WriteStartObject();
                    body(writer);
                    writer.WriteEndObject();
                }

                return stream.ToArray();
            }
        }
    }
}
