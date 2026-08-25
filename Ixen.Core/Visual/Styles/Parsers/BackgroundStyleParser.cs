using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Collections.Generic;
using System.Text;

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

        internal const string LEFT = "left";
        internal const string CENTER = "center";
        internal const string RIGHT = "right";
        internal const string TOP = "top";
        internal const string MIDDLE = "middle";
        internal const string BOTTOM = "bottom";

        public BackgroundStyleDescriptor Descriptor { get; } = new BackgroundStyleDescriptor();

        public BackgroundStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            string[] parts = Split(_content?.Trim());

            if (parts == null || parts.Length == 0)
            {
                return false;
            }

            bool repeatSeen = false;

            foreach (string part in parts)
            {
                if (GradientParser.IsCall(part))
                {
                    if (Descriptor.Gradient != null)
                    {
                        return false;
                    }

                    Descriptor.Gradient = GradientParser.Parse(part);

                    if (Descriptor.Gradient == null)
                    {
                        return false;
                    }

                    continue;
                }

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

                if (TryPosition(part.ToLower()))
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
                return !repeatSeen && !Descriptor.IsScaled && !Descriptor.HasPosition;
            }

            return !((Descriptor.RepeatX || Descriptor.RepeatY) && Descriptor.IsScaled);
        }

        private static bool IsImageName(string value)
        {
            int dot = value.LastIndexOf('.');

            return dot > 0 && dot < value.Length - 1;
        }

        private bool TryPosition(string value)
        {
            switch (value)
            {
                case LEFT:
                    Descriptor.PositionX = 0f;
                    return true;

                case CENTER:
                    Descriptor.PositionX = 0.5f;
                    return true;

                case RIGHT:
                    Descriptor.PositionX = 1f;
                    return true;

                case TOP:
                    Descriptor.PositionY = 0f;
                    return true;

                case MIDDLE:
                    Descriptor.PositionY = 0.5f;
                    return true;

                case BOTTOM:
                    Descriptor.PositionY = 1f;
                    return true;

                default:
                    return false;
            }
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

        private static string[] Split(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return null;
            }

            var parts = new List<string>();
            var current = new StringBuilder();
            int depth = 0;

            foreach (char c in content)
            {
                if (c == '(')
                {
                    depth++;
                }
                else if (c == ')')
                {
                    depth--;
                }
                else if ((c == ' ' || c == '\t') && depth == 0)
                {
                    if (current.Length > 0)
                    {
                        parts.Add(current.ToString());
                        current.Clear();
                    }

                    continue;
                }

                current.Append(c);
            }

            if (current.Length > 0)
            {
                parts.Add(current.ToString());
            }

            return depth == 0 ? parts.ToArray() : null;
        }
    }
}
