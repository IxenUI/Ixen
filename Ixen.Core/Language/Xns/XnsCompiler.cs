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
            StyleDefinition definition = StyleDefinitions.Find(xnsStyle.Name);

            if (definition == null)
            {
                errors.Add(new LanguageError(
                    LanguageErrorCode.UNKNOWN_STYLE,
                    $"Unknown style property '{xnsStyle.Name}'.",
                    xnsStyle.NameIndex,
                    xnsStyle.Name?.Length ?? 0));

                return null;
            }

            return Validated(definition, xnsStyle, errors);
        }
        private StyleDescriptor Validated(StyleDefinition definition, XnsStyle xnsStyle, List<LanguageError> errors)
        {
            StyleParser parser = definition.CreateParser(xnsStyle.Value);

            if (parser.IsValid)
            {
                return definition.DescriptorOf(parser);
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
