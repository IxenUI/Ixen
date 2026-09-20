using System.Text.Json;

namespace Ixen.LanguageServer.Protocol
{
    internal static class Messages
    {
        public static bool TryProperty(JsonElement owner, string name, out JsonElement value)
        {
            value = default;

            return owner.ValueKind == JsonValueKind.Object && owner.TryGetProperty(name, out value);
        }

        public static string Text(JsonElement owner, string name)
        {
            return TryProperty(owner, name, out JsonElement value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }

        public static int Number(JsonElement owner, string name, int fallback)
        {
            return TryProperty(owner, name, out JsonElement value) && value.ValueKind == JsonValueKind.Number
                ? value.GetInt32()
                : fallback;
        }

        public static string DocumentUri(JsonElement parameters)
        {
            return TryProperty(parameters, "textDocument", out JsonElement document)
                ? Text(document, "uri")
                : null;
        }

        public static bool Position(JsonElement parameters, out int line, out int character)
        {
            line = 0;
            character = 0;

            if (!TryProperty(parameters, "position", out JsonElement position))
            {
                return false;
            }

            line = Number(position, "line", -1);
            character = Number(position, "character", -1);

            return line >= 0 && character >= 0;
        }
    }
}
