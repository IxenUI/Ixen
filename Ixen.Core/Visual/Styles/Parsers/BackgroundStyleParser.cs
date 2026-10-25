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
            string[] parts = SplitTokens(entry);

            if (parts == null || parts.Length == 0)
            {
                return false;
            }

            bool repeatSeen = false;
            var position = new PositionReader();

            foreach (string part in parts)
            {
                if (position.Accept(part.ToLower()))
                {
                    continue;
                }

                if (position.IsInvalid)
                {
                    return false;
                }

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

                if (StyleColors.IsValue(part))
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

            if ((position.HasX && position.X < 0) || (position.HasY && position.Y < 0))
            {
                return false;
            }

            if (position.HasX)
            {
                layer.PositionX = position.X;
                layer.OffsetX = position.OffsetX;
            }

            if (position.HasY)
            {
                layer.PositionY = position.Y;
                layer.OffsetY = position.OffsetY;
            }

            if (layer.ImageUrl == null)
            {
                return !repeatSeen && !layer.IsScaled && !layer.HasPosition;
            }

            return !((layer.RepeatX || layer.RepeatY) && layer.IsScaled);
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

    }
}
