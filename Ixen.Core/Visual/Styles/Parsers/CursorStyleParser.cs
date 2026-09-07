using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Globalization;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class CursorStyleParser : StyleParser
    {
        private static readonly char[] _separators = { ' ', '\t' };

        public CursorStyleDescriptor Descriptor { get; } = new();

        public CursorStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            string[] parts = _content?.Trim()
                .Split(_separators, StringSplitOptions.RemoveEmptyEntries);

            if (parts == null || parts.Length == 0)
            {
                return false;
            }

            CursorKind kind = CursorKind.Unset;
            string image = null;
            int hotspots = 0;
            int hotspotX = 0;
            int hotspotY = 0;

            foreach (string part in parts)
            {
                if (int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out int hotspot))
                {
                    if (hotspots == 0)
                    {
                        hotspotX = hotspot;
                    }
                    else if (hotspots == 1)
                    {
                        hotspotY = hotspot;
                    }
                    else
                    {
                        return false;
                    }

                    hotspots++;
                    continue;
                }

                if (IsImageName(part))
                {
                    if (image != null)
                    {
                        return false;
                    }

                    image = part;
                    continue;
                }

                CursorKind named = Named(part);

                if (named == CursorKind.Unset || kind != CursorKind.Unset)
                {
                    return false;
                }

                kind = named;
            }

            if (image == null)
            {
                if (hotspots > 0 || kind == CursorKind.Unset)
                {
                    return false;
                }

                Descriptor.Value = kind;

                return true;
            }

            if (kind != CursorKind.Unset || hotspots == 1)
            {
                return false;
            }

            Descriptor.Value = CursorKind.Image;
            Descriptor.Image = image;
            Descriptor.HotspotX = hotspotX;
            Descriptor.HotspotY = hotspotY;

            return true;
        }

        private static CursorKind Named(string value)
        {
            switch (value.ToLower())
            {
                case "default":
                case "arrow":
                    return CursorKind.Default;

                case "hand":
                case "pointer":
                    return CursorKind.Hand;

                case "text":
                case "caret":
                    return CursorKind.Text;

                case "wait":
                    return CursorKind.Wait;

                case "crosshair":
                    return CursorKind.Crosshair;

                case "ew-resize":
                    return CursorKind.ResizeHorizontal;

                case "ns-resize":
                    return CursorKind.ResizeVertical;

                case "nesw-resize":
                    return CursorKind.ResizeDiagonalUp;

                case "nwse-resize":
                    return CursorKind.ResizeDiagonalDown;

                case "move":
                    return CursorKind.Move;

                case "not-allowed":
                    return CursorKind.NotAllowed;

                case "help":
                    return CursorKind.Help;

                case "progress":
                    return CursorKind.Progress;

                case "none":
                    return CursorKind.Hidden;

                default:
                    return CursorKind.Unset;
            }
        }
    }
}
