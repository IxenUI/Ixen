using Ixen.Core.Language.Base;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Core.Visual.Styles.Parsers;
using System.Collections.Generic;

namespace Ixen.Core.Language.Xns
{
    internal class XnsCompiler
    {
        public XnsCompiler()
        { }

        public ClassesSet Compile(XnsNode node, List<LanguageError> errors)
        {
            var set = new ClassesSet();
            set.Classes = new List<StyleClass>();

            AddClass(node, set.Classes, errors);

            return set;
        }

        private string GetScope(XnsNode node)
            => StyleScope.Build(node, n => n.Parent, n => n.Name);

        private StyleClass GetClass(XnsNode node, List<LanguageError> errors)
        {
            string name = node.Name;
            var target = StyleClassTarget.ElementName;

            if (name.StartsWith("."))
            {
                target = StyleClassTarget.ClassName;
                name = name.Substring(1);
            }
            else if (name.StartsWith("#"))
            {
                target = StyleClassTarget.ElementType;
                name = name.Substring(1);
            }

            return new StyleClass(target, null, GetScope(node), name, ToStyles(node, errors));
        }

        private void AddClass(XnsNode node, List<StyleClass> list, List<LanguageError> errors)
        {
            if (node.Styles.Count > 0)
            {
                list.Add(GetClass(node, errors));
            }

            foreach (var child in node.Children)
            {
                AddClass(child, list, errors);
            }
        }

        private List<StyleDescriptor> ToStyles(XnsNode xnsNode, List<LanguageError> errors)
        {
            var styles = new List<StyleDescriptor>();

            foreach (var xnsStyle in xnsNode.Styles)
            {
                StyleDescriptor descriptor = ToStyleDescriptor(xnsStyle, errors);

                if (descriptor != null)
                {
                    styles.Add(descriptor);
                }
            }

            return styles;
        }

        private StyleDescriptor ToStyleDescriptor(XnsStyle xnsStyle, List<LanguageError> errors)
        {
            switch (xnsStyle.Name.ToLower())
            {
                case StyleIdentifier.BACKGROUND:
                    return Validated(new BackgroundStyleParser(xnsStyle.Value), p => p.Descriptor, xnsStyle, errors);

                case StyleIdentifier.BORDER:
                    return Validated(new BorderStyleParser(xnsStyle.Value), p => p.Descriptor, xnsStyle, errors);

                case StyleIdentifier.COLOR:
                    return Validated(new ColorStyleParser(xnsStyle.Value), p => p.Descriptor, xnsStyle, errors);

                case StyleIdentifier.COLUMN_TEMPLATE:
                    return Validated(new ColumnTemplateStyleParser(xnsStyle.Value), p => p.Descriptor, xnsStyle, errors);

                case StyleIdentifier.CORNER_RADIUS:
                    return Validated(new CornerRadiusStyleParser(xnsStyle.Value), p => p.Descriptor, xnsStyle, errors);

                case StyleIdentifier.FONT_FAMILY:
                    return Validated(new FontFamilyStyleParser(xnsStyle.Value), p => p.Descriptor, xnsStyle, errors);

                case StyleIdentifier.FONT_SIZE:
                    return Validated(new FontSizeStyleParser(xnsStyle.Value), p => p.Descriptor, xnsStyle, errors);

                case StyleIdentifier.HEIGHT:
                    return Validated(new HeightStyleParser(xnsStyle.Value), p => p.Descriptor, xnsStyle, errors);

                case StyleIdentifier.LAYOUT:
                    return Validated(new LayoutStyleParser(xnsStyle.Value), p => p.Descriptor, xnsStyle, errors);

                case StyleIdentifier.MARGIN:
                    return Validated(new MarginStyleParser(xnsStyle.Value), p => p.Descriptor, xnsStyle, errors);

                case StyleIdentifier.PADDING:
                    return Validated(new PaddingStyleParser(xnsStyle.Value), p => p.Descriptor, xnsStyle, errors);

                case StyleIdentifier.ROW_TEMPLATE:
                    return Validated(new RowTemplateStyleParser(xnsStyle.Value), p => p.Descriptor, xnsStyle, errors);

                case StyleIdentifier.TEXT_ALIGN:
                    return Validated(new TextAlignStyleParser(xnsStyle.Value), p => p.Descriptor, xnsStyle, errors);

                case StyleIdentifier.TEXT_WRAP:
                    return Validated(new TextWrapStyleParser(xnsStyle.Value), p => p.Descriptor, xnsStyle, errors);

                case StyleIdentifier.WIDTH:
                    return Validated(new WidthStyleParser(xnsStyle.Value), p => p.Descriptor, xnsStyle, errors);

                default:
                    errors.Add(new LanguageError(
                        LanguageErrorCode.UNKNOWN_STYLE,
                        $"Unknown style property '{xnsStyle.Name}'.",
                        xnsStyle.NameIndex,
                        xnsStyle.Name?.Length ?? 0));
                    return null;
            }
        }

        private StyleDescriptor Validated<TParser>(TParser parser, System.Func<TParser, StyleDescriptor> selector,
            XnsStyle xnsStyle, List<LanguageError> errors)
            where TParser : StyleParser
        {
            if (parser.IsValid)
            {
                return selector(parser);
            }

            errors.Add(new LanguageError(
                LanguageErrorCode.INVALID_STYLE_VALUE,
                $"Invalid value '{xnsStyle.Value}' for style property '{xnsStyle.Name}'.",
                xnsStyle.ValueIndex,
                xnsStyle.Value?.Length ?? 0));

            return null;
        }
    }
}
