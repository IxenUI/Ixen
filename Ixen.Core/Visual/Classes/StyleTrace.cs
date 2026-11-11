using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Ixen.Core.Visual.Classes
{
    public sealed class StyleTraceEntry
    {
        public StyleClassTarget Target { get; }
        public string Selector { get; }
        public string Scope { get; }
        public string Media { get; }
        public string Container { get; }
        public bool IsDefault { get; }
        public IReadOnlyList<string> Properties { get; }

        internal StyleTraceEntry(StyleClass styleClass, bool isDefault)
        {
            IsDefault = isDefault;
            Target = styleClass.Target;
            Selector = Describe(styleClass);
            Scope = styleClass.Scope;
            Media = styleClass.Media?.Source;
            Container = styleClass.Container?.Source;

            var properties = new List<string>();

            foreach (StyleDescriptor style in styleClass.Styles)
            {
                if (!properties.Contains(style.Identifier))
                {
                    properties.Add(style.Identifier);
                }

                _values[style.Identifier] = style.Text;
            }

            Properties = properties;
        }

        private readonly Dictionary<string, string> _values = new();

        public string ValueOf(string property)
            => property != null && _values.TryGetValue(property, out string value) ? value : null;

        private static string Describe(StyleClass styleClass)
        {
            string sigil = styleClass.Target == StyleClassTarget.ClassName
                ? "."
                : styleClass.Target == StyleClassTarget.ElementType
                    ? "#"
                    : string.Empty;

            string selector = sigil + styleClass.Name;

            if (styleClass.Negations == null)
            {
                return selector;
            }

            foreach (string negation in styleClass.Negations.Split(StyleScope.SELECTOR_SEPARATOR))
            {
                selector += StyleScope.NOT_OPEN + negation + StyleScope.NOT_CLOSE;
            }

            return selector;
        }
    }

    public sealed class StyleTrace
    {
        private readonly List<StyleTraceEntry> _applied = new();
        private readonly List<string> _properties = new();

        public VisualElement Element { get; }
        public string Name { get; }
        public string TypeName { get; }
        public IReadOnlyList<string> Classes { get; }
        public IReadOnlyList<string> States { get; }
        public int ChildIndex { get; }
        public int ChildCount { get; }

        public IReadOnlyList<StyleTraceEntry> Applied => _applied;
        public IReadOnlyList<string> Properties => _properties;

        internal StyleTrace(VisualElement element)
        {
            Element = element;
            Name = element.Name;
            TypeName = element.TypeName;
            Classes = new List<string>(element.Classes);
            States = new List<string>(element.States);
            ChildIndex = -1;
            ChildCount = 0;

            if (StyleStructural.Position(element, out int index, out int count))
            {
                ChildIndex = index;
                ChildCount = count;
            }
        }

        public StyleTraceEntry WinnerOf(string property)
        {
            for (int index = _applied.Count - 1; index >= 0; index--)
            {
                if (_applied[index].Properties.Contains(property))
                {
                    return _applied[index];
                }
            }

            return null;
        }

        public string ValueOf(string property) => WinnerOf(property)?.ValueOf(property);

        public string ResolvedValueOf(string property)
        {
            string value = ValueOf(property);

            if (value == null || Tokens == null || value.IndexOf(StyleTokens.MARKER) < 0)
            {
                return value;
            }

            var resolved = new StringBuilder(value.Length);
            int index = 0;

            while (index < value.Length)
            {
                if (value[index] != StyleTokens.MARKER)
                {
                    resolved.Append(value[index]);
                    index++;

                    continue;
                }

                int end = index + 1;

                while (end < value.Length && (char.IsLetterOrDigit(value[end]) || value[end] == '_' || value[end] == '-'))
                {
                    end++;
                }

                resolved.Append(Tokens.Resolve(value.Substring(index, end - index)));
                index = end;
            }

            return resolved.ToString();
        }

        internal StyleTokens Tokens { get; set; }

        private readonly Dictionary<string, VisualElement> _inheritedFrom = new();
        private readonly Dictionary<string, StyleTraceEntry> _inheritedRules = new();

        public IReadOnlyCollection<string> InheritedProperties => _inheritedFrom.Keys;

        public VisualElement InheritedFrom(string property)
            => property != null && _inheritedFrom.TryGetValue(property, out VisualElement origin) ? origin : null;

        public StyleTraceEntry InheritedRule(string property)
            => property != null && _inheritedRules.TryGetValue(property, out StyleTraceEntry rule) ? rule : null;

        internal void Inherit(string property, VisualElement origin, StyleTraceEntry rule)
        {
            _inheritedFrom[property] = origin;
            _inheritedRules[property] = rule;
        }

        public string ComputedValueOf(string property)
        {
            VisualElementStylesHandlers handlers = Element.StylesHandlers;

            if (handlers == null)
            {
                return null;
            }

            switch (property)
            {
                case StyleIdentifier.WIDTH:
                    return Pixels(Element.ActualWidth);

                case StyleIdentifier.HEIGHT:
                    return Pixels(Element.ActualHeight);

                case StyleIdentifier.COLOR:
                    return Hex(handlers.Color.Brush?.Color);

                case StyleIdentifier.BACKGROUND:
                    return handlers.Background.Descriptor?.Color == null ? null : Hex(handlers.Background.Color);

                case StyleIdentifier.FONT_SIZE:
                    return Pixels(FontSpec.From(handlers, Element.Host?.FontScale ?? 1).Size);

                case StyleIdentifier.FONT_WEIGHT:
                    return FontSpec.From(handlers).Weight.ToString(CultureInfo.InvariantCulture);

                case StyleIdentifier.OPACITY:
                    return handlers.Opacity.Descriptor.Value.ToString("0.###", CultureInfo.InvariantCulture);

                default:
                    return null;
            }
        }

        private static string Pixels(float value)
            => value.ToString("0.##", CultureInfo.InvariantCulture) + "px";

        private static string Hex(Color? color)
        {
            if (color == null)
            {
                return null;
            }

            SkiaSharp.SKColor sk = color.Value.SKColor;

            return "#" + sk.Alpha.ToString("X2") + sk.Red.ToString("X2") + sk.Green.ToString("X2") + sk.Blue.ToString("X2");
        }

        internal void Record(StyleClass styleClass, bool isDefault)
        {
            var entry = new StyleTraceEntry(styleClass, isDefault);

            _applied.Add(entry);

            foreach (string property in entry.Properties)
            {
                if (!_properties.Contains(property))
                {
                    _properties.Add(property);
                }
            }
        }
    }
}
