using Ixen.Core.Rendering;
using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class FilterStyleHandler : StyleHandler
    {
        public FilterStyleDescriptor Descriptor { get; private set; }

        private readonly FilterChain _chain;

        public FilterStyleHandler()
            : this(new())
        { }

        public FilterStyleHandler(FilterStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;

            if (descriptor.IsDeclared)
            {
                _chain = new FilterChain(descriptor);
            }
        }

        internal FilterChain Chain => _chain;
    }
}
