using Ixen.Core.Visual.Styles.Descriptors;
using System;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class BackgroundStyleParser : StyleParser
    {
        internal const string REPEAT = "repeat";
        internal const string REPEAT_X = "repeat-x";
        internal const string REPEAT_Y = "repeat-y";
        internal const string NO_REPEAT = "no-repeat";

        internal const string AUTO = "auto";
        internal const string COVER = "cover";
        internal const string CONTAIN = "contain";
        internal const string FILL = "fill";
        internal const string STRETCH = "stretch";

        public BackgroundStyleDescriptor Descriptor { get; } = new BackgroundStyleDescriptor();

        public BackgroundStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            string[] parts = _content?.Trim().Split(new[] { ' ', '\t' },
                StringSplitOptions.RemoveEmptyEntries);

            if (parts == null || parts.Length == 0)
            {
                return false;
            }

            bool repeatSeen = false;

            foreach (string part in parts)
            {
                if (part[0] == '#')
                {
                    var color = new ColorStyleParser(part);

                    if (!color.IsValid)
                    {
                        return false;
                    }

                    Descriptor.Color = color.Descriptor.Value;
                    continue;
                }

                if (TryRepeat(part.ToLower()))
                {
                    repeatSeen = true;
                    continue;
                }

                if (TryFit(part.ToLower()))
                {
                    continue;
                }

                if (IsImageName(part))
                {
                    Descriptor.ImageUrl = part;
                    continue;
                }

                return false;
            }

            if (Descriptor.ImageUrl == null)
            {
                return !repeatSeen && !Descriptor.IsScaled;
            }

            return !((Descriptor.RepeatX || Descriptor.RepeatY) && Descriptor.IsScaled);
        }

        private static bool IsImageName(string value)
        {
            int dot = value.LastIndexOf('.');

            return dot > 0 && dot < value.Length - 1;
        }

        private bool TryFit(string value)
        {
            switch (value)
            {
                case AUTO:
                    Descriptor.Fit = ObjectFit.None;
                    return true;

                case COVER:
                    Descriptor.Fit = ObjectFit.Cover;
                    return true;

                case CONTAIN:
                    Descriptor.Fit = ObjectFit.Contain;
                    return true;

                case FILL:
                case STRETCH:
                    Descriptor.Fit = ObjectFit.Fill;
                    return true;

                default:
                    return false;
            }
        }

        private bool TryRepeat(string value)
        {
            switch (value)
            {
                case REPEAT:
                    Descriptor.RepeatX = true;
                    Descriptor.RepeatY = true;
                    return true;

                case REPEAT_X:
                    Descriptor.RepeatX = true;
                    Descriptor.RepeatY = false;
                    return true;

                case REPEAT_Y:
                    Descriptor.RepeatX = false;
                    Descriptor.RepeatY = true;
                    return true;

                case NO_REPEAT:
                    Descriptor.RepeatX = false;
                    Descriptor.RepeatY = false;
                    return true;

                default:
                    return false;
            }
        }
    }
}
