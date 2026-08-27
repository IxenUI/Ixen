using Ixen.Core.Rendering;
using Ixen.Core.Visual.Styles.Descriptors;
using System.Runtime.CompilerServices;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class FilterStyleHandler : StyleHandler
    {
        public FilterStyleDescriptor Descriptor { get; private set; }

        private readonly FilterChain _chain;
        private readonly FilterStyleDescriptor _snapshot;

        private static readonly ConditionalWeakTable<FilterStyleDescriptor, FilterStyleHandler> _built =
            new ConditionalWeakTable<FilterStyleDescriptor, FilterStyleHandler>();

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
            if (_built.TryGetValue(descriptor, out FilterStyleHandler handler)
                && handler._snapshot.SameAs(descriptor))
            {
                return handler;
            }

            handler = new FilterStyleHandler(descriptor);

            _built.Remove(descriptor);
            _built.Add(descriptor, handler);

            return handler;
        }

        internal FilterChain Chain => _chain;
    }
}
