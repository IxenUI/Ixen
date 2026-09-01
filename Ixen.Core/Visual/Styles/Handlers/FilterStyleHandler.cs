using Ixen.Core.Rendering;
using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class FilterStyleHandler : StyleHandler
    {
        public FilterStyleDescriptor Descriptor { get; private set; }

        private readonly FilterChain _chain;
        private readonly FilterStyleDescriptor _snapshot;

        public FilterStyleHandler()
            : this(new())
        { }

        public FilterStyleHandler(FilterStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
            _snapshot = descriptor.Snapshot();

            if (descriptor.IsDeclared)
            {
                _chain = new FilterChain(descriptor);
            }
        }

        internal static FilterStyleHandler For(FilterStyleDescriptor descriptor)
        {
            if (descriptor.Handler is FilterStyleHandler handler
                && handler._snapshot.SameAs(descriptor))
            {
                return handler;
            }

            handler = new FilterStyleHandler(descriptor);

            descriptor.Handler = handler;

            return handler;
        }

        internal FilterChain Chain => _chain;
    }
}
