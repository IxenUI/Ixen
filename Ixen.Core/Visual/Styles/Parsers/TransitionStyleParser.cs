using Ixen.Core.Visual.Styles.Descriptors;
using System.Globalization;
using Easing = Ixen.Core.Visual.Easing;

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

            if (parts == null || parts.Length < 2)
            {
                return false;
            }

            int index = 0;

            while (index < parts.Length)
            {
                string property = parts[index].ToLower();

                if (!IsAnimatable(property) || index + 1 >= parts.Length
                    || !TryParseDuration(parts[index + 1], out int duration))
                {
                    return false;
                }

                index += 2;

                var easing = EasingKind.Linear;
                int delay = 0;

                for (int extra = 0; extra < 2 && index < parts.Length; extra++)
                {
                    string token = parts[index].ToLower();

                    if (Easing.TryParse(token, out EasingKind parsed))
                    {
                        easing = parsed;
                        index++;
                        continue;
                    }

                    if (TryParseDuration(token, out int parsedDelay))
                    {
                        delay = parsedDelay;
                        index++;
                        continue;
                    }

                    break;
                }

                Descriptor.Specs[property] = new TransitionSpec
                {
                    Duration = duration,
                    Delay = delay,
                    Easing = easing
                };
            }

            return true;
        }

        private static bool IsAnimatable(string property)
            => property == TransitionStyleDescriptor.ALL
                || property == StyleIdentifier.BACKGROUND
                || property == StyleIdentifier.COLOR
                || property == StyleIdentifier.BORDER
                || property == StyleIdentifier.WIDTH
                || property == StyleIdentifier.HEIGHT
                || property == StyleIdentifier.LEFT
                || property == StyleIdentifier.TOP
                || property == StyleIdentifier.RIGHT
                || property == StyleIdentifier.BOTTOM;

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
