using Ixen.Core.Visual.Styles.Descriptors;
using System.Globalization;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class TransitionStyleParser : StyleParser
    {
        public TransitionStyleDescriptor Descriptor { get; } = new();

        public TransitionStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            string[] parts = _content?.Trim().Split(new[] { ' ', '\t' },
                System.StringSplitOptions.RemoveEmptyEntries);

            if (parts == null || parts.Length == 0 || parts.Length % 2 != 0)
            {
                return false;
            }

            for (int i = 0; i < parts.Length; i += 2)
            {
                string property = parts[i].ToLower();

                if (!IsAnimatable(property) || !TryParseDuration(parts[i + 1], out int duration))
                {
                    return false;
                }

                Descriptor.Durations[property] = duration;
            }

            return true;
        }

        private static bool IsAnimatable(string property)
            => property == TransitionStyleDescriptor.ALL
                || property == StyleIdentifier.BACKGROUND
                || property == StyleIdentifier.COLOR
                || property == StyleIdentifier.BORDER;

        private static bool TryParseDuration(string value, out int duration)
        {
            duration = 0;

            string trimmed = value.ToLower();

            if (trimmed.EndsWith("ms"))
            {
                trimmed = trimmed.Substring(0, trimmed.Length - 2);
            }
            else if (trimmed.EndsWith("s"))
            {
                if (!float.TryParse(trimmed.Substring(0, trimmed.Length - 1),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out float seconds))
                {
                    return false;
                }

                duration = (int)(seconds * 1000);
                return duration > 0;
            }

            if (!int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out duration))
            {
                return false;
            }

            return duration > 0;
        }
    }
}
