using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class BackgroundStyleParser : StyleParser
    {
        private static readonly Regex _percent = new Regex(@"^([0-9]+(?:\.[0-9]+)?)%$");

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
            string[] entries = SplitLayers(_content?.Trim());

            if (entries == null || entries.Length == 0)
            {
                return false;
            }

            bool colorSeen = false;

            foreach (string entry in entries)
            {
                var layer = new BackgroundLayer();

                if (!ParseLayer(entry, layer, ref colorSeen))
                {
                    return false;
                }

                if (!layer.IsEmpty)
                {
                    Descriptor.Layers.Add(layer);
                }
            }

            return true;
        }

        private bool ParseLayer(string entry, BackgroundLayer layer, ref bool colorSeen)
        {
            string[] parts = Split(entry);

            if (parts == null || parts.Length == 0)
            {
                return false;
            }

            bool repeatSeen = false;

            foreach (string part in parts)
            {
                if (GradientParser.IsCall(part))
                {
                    if (layer.Gradient != null)
                    {
                        return false;
                    }

                    layer.Gradient = GradientParser.Parse(part);

                    if (layer.Gradient == null)
                    {
                        return false;
                    }

                    continue;
                }

                if (part[0] == '#')
                {
                    var color = new ColorStyleParser(part);

                    if (!color.IsValid || colorSeen)
                    {
                        return false;
                    }

                    Descriptor.Color = color.Descriptor.Value;
                    colorSeen = true;
                    continue;
                }

                if (TryRepeat(layer, part.ToLower()))
                {
                    repeatSeen = true;
                    continue;
                }

                if (TryFit(layer, part.ToLower()))
                {
                    continue;
                }

                if (TryPosition(layer, part.ToLower()))
                {
                    continue;
                }

                if (IsImageName(part))
                {
                    if (layer.ImageUrl != null)
                    {
                        return false;
                    }

                    layer.ImageUrl = part;
                    continue;
                }

                return false;
            }

            if (layer.ImageUrl == null)
            {
                return !repeatSeen && !layer.IsScaled && !layer.HasPosition;
            }

            return !((layer.RepeatX || layer.RepeatY) && layer.IsScaled);
        }

        private static bool IsImageName(string value)
        {
            int dot = value.LastIndexOf('.');

            if (dot <= 0 || dot >= value.Length - 1)
            {
                return false;
            }

            for (int index = dot + 1; index < value.Length; index++)
            {
                if (!char.IsLetter(value[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool TryPosition(BackgroundLayer layer, string value)
        {
            switch (value)
            {
                case LEFT:
                    return SetX(layer, 0f);

                case CENTER:
                    return SetX(layer, 0.5f);

                case RIGHT:
                    return SetX(layer, 1f);

                case TOP:
                    return SetY(layer, 0f);

                case MIDDLE:
                    return SetY(layer, 0.5f);

                case BOTTOM:
                    return SetY(layer, 1f);

                default:
                    return TryPercentPosition(layer, value);
            }
        }

        private static bool SetX(BackgroundLayer layer, float value)
        {
            if (layer.PositionX >= 0f)
            {
                return false;
            }

            layer.PositionX = value;

            return true;
        }

        private static bool SetY(BackgroundLayer layer, float value)
        {
            if (layer.PositionY >= 0f)
            {
                return false;
            }

            layer.PositionY = value;

            return true;
        }

        private static bool TryPercentPosition(BackgroundLayer layer, string value)
        {
            Match match = _percent.Match(value);

            if (!match.Success || !float.TryParse(match.Groups[1].Value, NumberStyles.Float,
                CultureInfo.InvariantCulture, out float percent))
            {
                return false;
            }

            float fraction = percent / 100f;

            return SetX(layer, fraction) || SetY(layer, fraction);
        }

        private static bool TryFit(BackgroundLayer layer, string value)
        {
            switch (value)
            {
                case AUTO:
                    layer.Fit = ObjectFit.None;
                    return true;

                case COVER:
                    layer.Fit = ObjectFit.Cover;
                    return true;

                case CONTAIN:
                    layer.Fit = ObjectFit.Contain;
                    return true;

                case FILL:
                case STRETCH:
                    layer.Fit = ObjectFit.Fill;
                    return true;

                default:
                    return false;
            }
        }

        private static bool TryRepeat(BackgroundLayer layer, string value)
        {
            switch (value)
            {
                case REPEAT:
                    layer.RepeatX = true;
                    layer.RepeatY = true;
                    return true;

                case REPEAT_X:
                    layer.RepeatX = true;
                    layer.RepeatY = false;
                    return true;

                case REPEAT_Y:
                    layer.RepeatX = false;
                    layer.RepeatY = true;
                    return true;

                case NO_REPEAT:
                    layer.RepeatX = false;
                    layer.RepeatY = false;
                    return true;

                default:
                    return false;
            }
        }

        private static string[] SplitLayers(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return null;
            }

            var entries = new List<string>();
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
                else if (c == ',' && depth == 0)
                {
                    entries.Add(current.ToString());
                    current.Clear();
                    continue;
                }

                current.Append(c);
            }

            entries.Add(current.ToString());

            return entries.ToArray();
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
