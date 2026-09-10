using Ixen.Core.Visual.Styles.Descriptors;
using System.Collections.Generic;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class AreaTemplateStyleParser : StyleParser
    {
        internal const char ROW_SEPARATOR = '/';

        public AreaTemplateStyleDescriptor Descriptor { get; } = new();

        public AreaTemplateStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            if (_content == null)
            {
                return false;
            }

            foreach (string row in _content.Split(ROW_SEPARATOR))
            {
                string[] cells = SplitTokens(row);

                if (cells == null || cells.Length < 1)
                {
                    return false;
                }

                if (Descriptor.Rows.Count > 0 && cells.Length != Descriptor.Rows[0].Length)
                {
                    return false;
                }

                foreach (string cell in cells)
                {
                    if (!IsAreaName(cell))
                    {
                        return false;
                    }
                }

                Descriptor.Rows.Add(cells);
            }

            return AllRectangular();
        }

        private bool AllRectangular()
        {
            foreach (string name in Descriptor.Names())
            {
                if (!Descriptor.IsRectangular(name))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsAreaName(string cell)
        {
            if (cell == AreaTemplateStyleDescriptor.EMPTY)
            {
                return true;
            }

            if (cell.Length < 1 || (!char.IsLetter(cell[0]) && cell[0] != '_'))
            {
                return false;
            }

            foreach (char c in cell)
            {
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
                {
                    return false;
                }
            }

            return true;
        }
    }

    internal class GridAreaStyleParser : StyleParser
    {
        public GridAreaStyleDescriptor Descriptor { get; } = new();

        public GridAreaStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            string value = _content?.Trim();

            if (string.IsNullOrEmpty(value) || !IsName(value))
            {
                return false;
            }

            Descriptor.Value = value;

            return true;
        }

        private static bool IsName(string value)
        {
            if (!char.IsLetter(value[0]) && value[0] != '_')
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
    }
}
