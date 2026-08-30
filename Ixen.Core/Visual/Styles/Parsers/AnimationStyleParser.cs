using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Globalization;
using Easing = Ixen.Core.Visual.Easing;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class AnimationStyleParser : StyleParser
    {
        internal const string INFINITE = "infinite";
        internal const string ALTERNATE = "alternate";
        internal const string NORMAL = "normal";
        internal const string FORWARDS = "forwards";
        internal const string NO_FILL = "none";

        private const char ITERATIONS_SUFFIX = 'x';

        public AnimationStyleDescriptor Descriptor { get; } = new();

        public AnimationStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            string[] parts = _content?.Trim().Split(new[] { ' ', '\t' },
                StringSplitOptions.RemoveEmptyEntries);

            if (parts == null || parts.Length < 2 || !IsName(parts[0]))
            {
                return false;
            }

            if (!StyleDuration.TryParse(parts[1], out int duration))
            {
                return false;
            }

            Descriptor.Name = parts[0];
            Descriptor.Duration = duration;

            for (int index = 2; index < parts.Length; index++)
            {
                string token = parts[index].ToLower();

                if (Easing.TryParse(token, out EasingKind easing))
                {
                    Descriptor.Easing = easing;
                    continue;
                }

                if (token == INFINITE)
                {
                    Descriptor.Iterations = AnimationStyleDescriptor.INFINITE;
                    continue;
                }

                if (token == ALTERNATE)
                {
                    Descriptor.Alternate = true;
                    continue;
                }

                if (token == NORMAL)
                {
                    Descriptor.Alternate = false;
                    continue;
                }

                if (token == FORWARDS)
                {
                    Descriptor.Fill = AnimationFill.Forwards;
                    continue;
                }

                if (token == NO_FILL)
                {
                    Descriptor.Fill = AnimationFill.None;
                    continue;
                }

                if (TryParseIterations(token, out int iterations))
                {
                    Descriptor.Iterations = iterations;
                    continue;
                }

                if (StyleDuration.TryParse(token, out int delay))
                {
                    Descriptor.Delay = delay;
                    continue;
                }

                return false;
            }

            return true;
        }

        private static bool IsName(string value)
        {
            if (string.IsNullOrEmpty(value) || (!char.IsLetter(value[0]) && value[0] != '_'))
            {
                return false;
            }

            foreach (char c in value)
            {
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryParseIterations(string value, out int iterations)
        {
            iterations = 0;

            if (value.Length < 2 || value[value.Length - 1] != ITERATIONS_SUFFIX)
            {
                return false;
            }

            return int.TryParse(value.Substring(0, value.Length - 1), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out iterations) && iterations > 0;
        }
    }
}
