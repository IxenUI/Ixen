using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using System.Linq;
using System.Collections.Generic;

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
            }

            Properties = properties;
        }

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
