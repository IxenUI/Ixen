using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class ColumnIndexStyleHandler : StyleHandler
    {
        public ColumnIndexStyleDescriptor Descriptor { get; private set; }

        public ColumnIndexStyleHandler()
            : this(new())
        { }

        public ColumnIndexStyleHandler(ColumnIndexStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }

    internal class RowIndexStyleHandler : StyleHandler
    {
        public RowIndexStyleDescriptor Descriptor { get; private set; }

        public RowIndexStyleHandler()
            : this(new())
        { }

        public RowIndexStyleHandler(RowIndexStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }

    internal class ColumnSpanStyleHandler : StyleHandler
    {
        public ColumnSpanStyleDescriptor Descriptor { get; private set; }

        public ColumnSpanStyleHandler()
            : this(new())
        { }

        public ColumnSpanStyleHandler(ColumnSpanStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }

    internal class RowSpanStyleHandler : StyleHandler
    {
        public RowSpanStyleDescriptor Descriptor { get; private set; }

        public RowSpanStyleHandler()
            : this(new())
        { }

        public RowSpanStyleHandler(RowSpanStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
