using Ixen.Core.Language.Base;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Core.Visual.Styles.Parsers;
using System.Collections.Generic;
using System.Globalization;

namespace Ixen.Core.Language.Xns
{
    internal class XnsCompiler
    {
        public XnsCompiler()
        { }

        internal const string FROM_OFFSET = "from";
        internal const string TO_OFFSET = "to";

        public ClassesSet Compile(XnsNode node, List<LanguageError> errors)
        {
            var set = new ClassesSet();
            set.Classes = new List<StyleClass>();
            set.Keyframes = new List<KeyframesSet>();

            Add(node, set, errors);

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

        private void Add(XnsNode node, ClassesSet set, List<LanguageError> errors)
        {
            if (IsKeyframes(node))
            {
                set.Keyframes.Add(GetKeyframes(node, errors));
                return;
            }

            if (node.Styles.Count > 0)
            {
                set.Classes.Add(GetClass(node, errors));
            }

            foreach (var child in node.Children)
            {
                Add(child, set, errors);
            }
        }

        private static bool IsKeyframes(XnsNode node)
            => node.Name != null
                && node.Name.Length > 1
                && node.Name[0] == XnsTokenizer.KEYFRAMES_MARKER;

        private KeyframesSet GetKeyframes(XnsNode node, List<LanguageError> errors)
        {
            var frames = new List<Keyframe>();

            if (node.Styles.Count > 0)
            {
                errors.Add(new LanguageError(
                    LanguageErrorCode.SYNTAX,
                    $"A '{XnsTokenizer.KEYFRAMES_MARKER}{XnsTokenizer.KEYFRAMES_KEYWORD}' block holds offsets, not styles.",
                    node.NameIndex,
                    node.Name.Length));
            }

            foreach (XnsNode child in node.Children)
            {
                if (!TryParseOffset(child.Name, out float offset))
                {
                    errors.Add(new LanguageError(
                        LanguageErrorCode.SYNTAX,
                        $"'{child.Name}' is not a valid keyframe offset. Use a whole percentage, '{FROM_OFFSET}' or '{TO_OFFSET}'.",
                        child.NameIndex,
                        child.Name?.Length ?? 0));

                    continue;
                }

                if (child.Children.Count > 0)
                {
                    errors.Add(new LanguageError(
                        LanguageErrorCode.SYNTAX,
                        "A keyframe offset cannot contain a nested block.",
                        child.NameIndex,
                        child.Name.Length));
                }

                frames.Add(new Keyframe(offset, ToStyles(child, errors)));
            }

            return new KeyframesSet(node.Name.Substring(1), frames);
        }

        private static bool TryParseOffset(string name, out float offset)
        {
            offset = 0;

            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            if (name == FROM_OFFSET)
            {
                return true;
            }

            if (name == TO_OFFSET)
            {
                offset = 1f;
                return true;
            }

            if (name[name.Length - 1] != '%')
            {
                return false;
            }

            if (!int.TryParse(name.Substring(0, name.Length - 1), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out int percent) || percent < 0 || percent > 100)
            {
                return false;
            }

            offset = percent / 100f;
            return true;
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
