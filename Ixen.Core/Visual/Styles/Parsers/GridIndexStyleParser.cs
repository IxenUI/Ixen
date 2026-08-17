using Ixen.Core.Visual.Styles.Descriptors;
using System.Globalization;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class GridIndexStyleParser : StyleParser
    {
        internal const string AUTO = "auto";

        public GridIndexStyleDescriptor Descriptor { get; } = new GridIndexStyleDescriptor();

        public GridIndexStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            string value = _content?.Trim().ToLower();

            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            if (value == AUTO)
            {
                Descriptor.Value = GridIndexStyleDescriptor.AUTO;
                return true;
            }

            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index)
                || index < 0)
            {
                return false;
            }

            Descriptor.Value = index;
            return true;
        }
    }

    internal class ColumnIndexStyleParser : GridIndexStyleParser
    {
        public new ColumnIndexStyleDescriptor Descriptor { get; } = new ColumnIndexStyleDescriptor();

        public ColumnIndexStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            bool valid = base.Parse();

            if (valid)
            {
                Descriptor.Set(base.Descriptor);
            }

            return valid;
        }
    }

    internal class RowIndexStyleParser : GridIndexStyleParser
    {
        public new RowIndexStyleDescriptor Descriptor { get; } = new RowIndexStyleDescriptor();

        public RowIndexStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            bool valid = base.Parse();

            if (valid)
            {
                Descriptor.Set(base.Descriptor);
            }

            return valid;
        }
    }

    internal class GridSpanStyleParser : StyleParser
    {
        public GridSpanStyleDescriptor Descriptor { get; } = new GridSpanStyleDescriptor();

        public GridSpanStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            if (!int.TryParse(_content?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int span)
                || span < 1)
            {
                return false;
            }

            Descriptor.Value = span;
            return true;
        }
    }

    internal class ColumnSpanStyleParser : GridSpanStyleParser
    {
        public new ColumnSpanStyleDescriptor Descriptor { get; } = new ColumnSpanStyleDescriptor();

        public ColumnSpanStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            bool valid = base.Parse();

            if (valid)
            {
                Descriptor.Set(base.Descriptor);
            }

            return valid;
        }
    }

    internal class RowSpanStyleParser : GridSpanStyleParser
    {
        public new RowSpanStyleDescriptor Descriptor { get; } = new RowSpanStyleDescriptor();

        public RowSpanStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            bool valid = base.Parse();

            if (valid)
            {
                Descriptor.Set(base.Descriptor);
            }

            return valid;
        }
    }
}
