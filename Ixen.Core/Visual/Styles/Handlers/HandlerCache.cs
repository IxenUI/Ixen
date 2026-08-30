using System.Runtime.CompilerServices;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal static class HandlerCache<TDescriptor, THandler>
        where TDescriptor : class
        where THandler : class
    {
        private static readonly ConditionalWeakTable<TDescriptor, THandler> _built =
            new ConditionalWeakTable<TDescriptor, THandler>();

        internal static THandler For(TDescriptor descriptor,
            ConditionalWeakTable<TDescriptor, THandler>.CreateValueCallback create)
        {
            if (descriptor == null)
            {
                return create(null);
            }

            if (_built.TryGetValue(descriptor, out THandler handler))
            {
                return handler;
            }

            return _built.GetValue(descriptor, create);
        }
    }
}
