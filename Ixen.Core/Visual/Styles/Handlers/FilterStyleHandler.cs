using Ixen.Core.Rendering;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class FilterStyleHandler : StyleHandler
    {
        public FilterStyleDescriptor Descriptor { get; private set; }

        private readonly FilterChain _chain;
        private readonly FilterStyleDescriptor _snapshot;
        private readonly int _tokens;

        public FilterStyleHandler()
            : this(new(), null)
        { }

        public FilterStyleHandler(FilterStyleDescriptor descriptor, StyleTokens tokens)
            : base()
        {
            Descriptor = descriptor;
            _snapshot = descriptor.Snapshot();
            _tokens = StyleColors.VersionOf(tokens);

            if (descriptor.IsDeclared)
            {
                _chain = new FilterChain(descriptor, tokens);
            }
        }

        internal static FilterStyleHandler For(FilterStyleDescriptor descriptor, StyleTokens tokens)
        {
            if (descriptor.Handler is FilterStyleHandler handler
                && handler._snapshot.SameAs(descriptor)
                && handler._tokens == StyleColors.VersionOf(tokens))
            {
                return handler;
            }

            handler = new FilterStyleHandler(descriptor, tokens);

            descriptor.Handler = handler;

            return handler;
        }

        internal FilterChain Chain => _chain;
    }
}
