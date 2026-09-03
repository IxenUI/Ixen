using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Collections.Generic;
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
            string[] entries = _content?.Split(',');

            if (entries == null || entries.Length == 0)
            {
                return false;
            }

            var animations = new List<AnimationSpec>();

            foreach (string entry in entries)
            {
                var spec = new AnimationSpec();

                if (!ParseOne(entry, spec))
                {
                    return false;
                }

                animations.Add(spec);
            }

            Descriptor.Animations = animations;

            return true;
        }

        private static bool ParseOne(string content, AnimationSpec spec)
        {
            string[] parts = content?.Trim().Split(new[] { ' ', '\t' },
                StringSplitOptions.RemoveEmptyEntries);

            if (parts == null || parts.Length < 2 || !IsName(parts[0]))
            {
                return false;
            }

            if (!StyleDuration.TryParse(parts[1], out int duration))
            {
                return false;
            }

            spec.Name = parts[0];
            spec.Duration = duration;

            for (int index = 2; index < parts.Length; index++)
            {
                string token = parts[index].ToLower();

                if (Easing.TryParse(token, out EasingKind easing))
                {
                    spec.Easing = easing;
                    continue;
                }

                if (token == INFINITE)
                {
                    spec.Iterations = AnimationStyleDescriptor.INFINITE;
                    continue;
                }

                if (token == ALTERNATE)
                {
                    spec.Alternate = true;
                    continue;
                }

                if (token == NORMAL)
                {
                    spec.Alternate = false;
                    continue;
                }

                if (token == FORWARDS)
                {
                    spec.Fill = AnimationFill.Forwards;
                    continue;
                }

                if (token == NO_FILL)
                {
                    spec.Fill = AnimationFill.None;
                    continue;
                }

                if (TryParseIterations(token, out int iterations))
                {
                    spec.Iterations = iterations;
                    continue;
                }

                if (StyleDuration.TryParse(token, out int delay))
                {
                    spec.Delay = delay;
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
