using Ixen.Core.Visual.Styles.Descriptors;
using System;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class ObjectPositionStyleParser : StyleParser
    {
        internal const string LEFT = PositionReader.LEFT;
        internal const string CENTER = PositionReader.CENTER;
        internal const string RIGHT = PositionReader.RIGHT;
        internal const string TOP = PositionReader.TOP;
        internal const string MIDDLE = PositionReader.MIDDLE;
        internal const string BOTTOM = PositionReader.BOTTOM;

        private const int MAX_PARTS = 4;

        private static readonly char[] _separators = { ' ', '\t' };

        public ObjectPositionStyleDescriptor Descriptor { get; } = new();

        public ObjectPositionStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            string content = _content?.Trim();

            if (string.IsNullOrEmpty(content))
            {
                return false;
            }

            string[] parts = content.ToLower().Split(_separators, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0 || parts.Length > MAX_PARTS)
            {
                return false;
            }

            var reader = new PositionReader();

            foreach (string part in parts)
            {
                if (!reader.Accept(part))
                {
                    return false;
                }
            }

            if (reader.HasX)
            {
                Descriptor.X = reader.X;
                Descriptor.OffsetX = reader.OffsetX;
            }

            if (reader.HasY)
            {
                Descriptor.Y = reader.Y;
                Descriptor.OffsetY = reader.OffsetY;
            }

            return true;
        }
    }
}
