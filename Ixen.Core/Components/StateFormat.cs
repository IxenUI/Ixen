using System.Collections.Generic;
using System.Text;

namespace Ixen.Core.Components
{
    internal static class StateFormat
    {
        internal const int VERSION = 1;

        private const string HEADER = "ixen-state";
        private const char SEPARATOR = '\t';
        private const char ESCAPE = '%';
        private static readonly char[] _lines = { '\n' };

        internal static string Write(Dictionary<string, ComponentState> states)
        {
            var text = new StringBuilder();

            text.Append(HEADER).Append(SEPARATOR).Append(VERSION).Append('\n');

            foreach (KeyValuePair<string, ComponentState> state in states)
            {
                foreach (KeyValuePair<string, string> entry in state.Value.Values)
                {
                    text.Append(Escaped(state.Key)).Append(SEPARATOR)
                        .Append(Escaped(entry.Key)).Append(SEPARATOR)
                        .Append(Escaped(entry.Value)).Append('\n');
                }
            }

            return text.ToString();
        }

        internal static Dictionary<string, Dictionary<string, string>> Read(string text)
        {
            var states = new Dictionary<string, Dictionary<string, string>>();

            if (string.IsNullOrEmpty(text))
            {
                return states;
            }

            string[] lines = text.Split(_lines);

            if (!IsHeader(lines[0]))
            {
                return states;
            }

            for (int index = 1; index < lines.Length; index++)
            {
                string[] fields = lines[index].TrimEnd('\r').Split(SEPARATOR);

                if (fields.Length != 3)
                {
                    continue;
                }

                string path = Unescaped(fields[0]);

                if (!states.TryGetValue(path, out Dictionary<string, string> values))
                {
                    values = new Dictionary<string, string>();
                    states[path] = values;
                }

                values[Unescaped(fields[1])] = Unescaped(fields[2]);
            }

            return states;
        }

        private static bool IsHeader(string line)
        {
            string[] fields = line.TrimEnd('\r').Split(SEPARATOR);

            return fields.Length == 2
                && fields[0] == HEADER
                && fields[1] == VERSION.ToString();
        }

        private static string Escaped(string value)
        {
            var text = new StringBuilder(value.Length);

            foreach (char character in value)
            {
                switch (character)
                {
                    case ESCAPE: text.Append("%25"); break;
                    case SEPARATOR: text.Append("%09"); break;
                    case '\n': text.Append("%0A"); break;
                    case '\r': text.Append("%0D"); break;
                    default: text.Append(character); break;
                }
            }

            return text.ToString();
        }

        private static string Unescaped(string value)
        {
            if (value.IndexOf(ESCAPE) < 0)
            {
                return value;
            }

            var text = new StringBuilder(value.Length);

            for (int index = 0; index < value.Length; index++)
            {
                if (value[index] != ESCAPE || index + 2 >= value.Length)
                {
                    text.Append(value[index]);
                    continue;
                }

                switch (value.Substring(index + 1, 2))
                {
                    case "25": text.Append(ESCAPE); index += 2; break;
                    case "09": text.Append(SEPARATOR); index += 2; break;
                    case "0A": text.Append('\n'); index += 2; break;
                    case "0D": text.Append('\r'); index += 2; break;
                    default: text.Append(value[index]); break;
                }
            }

            return text.ToString();
        }
    }
}
