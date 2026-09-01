using Ixen.Core.Visual.Styles.Descriptors;
using System;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal static class HandlerCache<TDescriptor, THandler>
        where TDescriptor : StyleDescriptor
        where THandler : class
    {
        internal static THandler For(TDescriptor descriptor, Func<TDescriptor, THandler> create)
        {
            if (descriptor == null)
            {
                return create(null);
            }

            if (descriptor.Handler is THandler handler)
            {
                return handler;
            }

            THandler built = create(descriptor);

            descriptor.Handler = built;

            return built;
        }
    }
}
