using Ixen.Core.Rendering;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class ColorStyleHandler : StyleHandler
    {
        private const string DEFAULT_COLOR = "#000000";

        public ColorStyleDescriptor Descriptor { get; private set; }
        public Brush Brush { get; private set; }

        private readonly string _valueSource;
        private readonly int _tokens;

        public ColorStyleHandler()
            : this(new(), null)
        { }

        public ColorStyleHandler(ColorStyleDescriptor descriptor, StyleTokens tokens)
            : base()
        {
            Descriptor = descriptor;

            string value = string.IsNullOrWhiteSpace(descriptor.Value)
                ? DEFAULT_COLOR
                : descriptor.Value;

            Brush = new Brush(new Color(StyleColors.Resolve(tokens, value)), true);

            _valueSource = descriptor.Value;
            _tokens = StyleColors.VersionOf(tokens);
        }

        internal static ColorStyleHandler For(ColorStyleDescriptor descriptor, StyleTokens tokens)
        {
            if (descriptor.Handler is ColorStyleHandler handler
                && handler._valueSource == descriptor.Value
                && handler._tokens == StyleColors.VersionOf(tokens))
            {
                return handler;
            }

            handler = new ColorStyleHandler(descriptor, tokens);

            descriptor.Handler = handler;

            return handler;
        }
    }
}
