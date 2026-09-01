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

        private XnsVariables _variables = new XnsVariables();

        public ClassesSet Compile(XnsNode node, List<LanguageError> errors)
        {
            var set = new ClassesSet();
            set.Classes = new List<StyleClass>();
            set.Keyframes = new List<KeyframesSet>();

            _variables = XnsVariables.Resolve(node?.Variables, errors);

            _mixins.Clear();
            CollectMixins(node);

            Add(node, set, errors);

            return set;
        }

        private readonly Dictionary<string, XnsNode> _mixins = new Dictionary<string, XnsNode>();

        private void CollectMixins(XnsNode node)
        {
            if (node == null)
            {
                return;
            }

            if (node.Mixin != null)
            {
                _mixins[node.Mixin] = node;
            }

            foreach (XnsNode child in node.Children)
            {
                CollectMixins(child);
            }
        }

        private static bool IsMixin(XnsNode node) => node.Mixin != null;

        private string GetScope(XnsNode node)
            => StyleScope.Build(node, n => n.Parent, n => n.Name);

        private StyleClass GetClass(XnsNode node, XnsNode selector, MediaQuery media,
            List<LanguageError> errors)
        {
            string name = StyleScope.Bare(selector.Name);
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

            return new StyleClass(target, null, GetScope(selector), name, ToStyles(node, errors), media)
            {
                SourceIndex = node.NameIndex,
                SourceLength = node.Name == null ? 0 : node.Name.Length
            };
        }

        private void Add(XnsNode node, ClassesSet set, List<LanguageError> errors)
            => Add(node, set, null, errors);

        private void Add(XnsNode node, ClassesSet set, MediaQuery media, List<LanguageError> errors)
        {
            if (IsKeyframes(node))
            {
                set.Keyframes.Add(GetKeyframes(node, errors));
                return;
            }

            if (IsMixin(node))
            {
                return;
            }

            if (node.Media != null)
            {
                media = GetMedia(node, media, errors);

                if (media == null)
                {
                    return;
                }

                if (node.Styles.Count > 0)
                {
                    AddInherited(node, set, media, errors);
                }
            }
            else if (node.Styles.Count > 0)
            {
                set.Classes.Add(GetClass(node, node, media, errors));
            }

            foreach (var child in node.Children)
            {
                Add(child, set, media, errors);
            }
        }

        private void AddInherited(XnsNode node, ClassesSet set, MediaQuery media,
            List<LanguageError> errors)
        {
            XnsNode selector = node.Parent;

            while (selector != null && selector.Name == null)
            {
                selector = selector.Parent;
            }

            if (selector == null)
            {
                errors.Add(new LanguageError(
                    LanguageErrorCode.SYNTAX,
                    $"A top-level '{XnsTokenizer.KEYFRAMES_MARKER}{XnsTokenizer.MEDIA_KEYWORD}' block holds selectors, not styles.",
                    node.NameIndex,
                    XnsTokenizer.MEDIA_KEYWORD.Length + 1));

                return;
            }

            set.Classes.Add(GetClass(node, selector, media, errors));
        }

        private MediaQuery GetMedia(XnsNode node, MediaQuery outer, List<LanguageError> errors)
        {
            MediaQuery query = MediaQuery.Parse(_variables.IsEmpty
                ? node.Media
                : _variables.Substitute(node.Media, node.NameIndex, errors));

            if (query == null)
            {
                errors.Add(new LanguageError(
                    LanguageErrorCode.INVALID_STYLE_VALUE,
                    $"'{node.Media}' is not a valid media query. Use min-width, max-width, min-height,"
                        + " max-height or orientation, combined with 'and'.",
                    node.NameIndex,
                    XnsTokenizer.MEDIA_KEYWORD.Length + 1));

                return null;
            }

            return outer == null ? query : outer.And(query);
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

            Expand(xnsNode.Styles, styles, new HashSet<string>(), 0, errors);

            return styles;
        }

        private const int MAX_INCLUDE_DEPTH = 16;

        private void Expand(List<XnsStyle> source, List<StyleDescriptor> styles,
            HashSet<string> including, int depth, List<LanguageError> errors)
        {
            if (depth > MAX_INCLUDE_DEPTH)
            {
                return;
            }

            foreach (XnsStyle xnsStyle in source)
            {
                if (xnsStyle.Include != null)
                {
                    Include(xnsStyle, styles, including, depth, errors);
                    continue;
                }

                StyleDescriptor descriptor = ToStyleDescriptor(Copy(xnsStyle), errors);

                if (descriptor != null)
                {
                    styles.Add(descriptor);
                }
            }
        }

        private void Include(XnsStyle xnsStyle, List<StyleDescriptor> styles,
            HashSet<string> including, int depth, List<LanguageError> errors)
        {
            if (!_mixins.TryGetValue(xnsStyle.Include, out XnsNode mixin))
            {
                errors.Add(new LanguageError(
                    LanguageErrorCode.INVALID_STYLE_VALUE,
                    $"'{xnsStyle.Include}' is not a declared mixin.",
                    xnsStyle.NameIndex,
                    XnsTokenizer.INCLUDE_KEYWORD.Length + 1));

                return;
            }

            if (!including.Add(xnsStyle.Include))
            {
                errors.Add(new LanguageError(
                    LanguageErrorCode.INVALID_STYLE_VALUE,
                    $"mixin '{xnsStyle.Include}' includes itself.",
                    xnsStyle.NameIndex,
                    XnsTokenizer.INCLUDE_KEYWORD.Length + 1));

                return;
            }

            if (mixin.Children.Count > 0)
            {
                errors.Add(new LanguageError(
                    LanguageErrorCode.SYNTAX,
                    $"mixin '{xnsStyle.Include}' holds selectors, and a mixin may only hold declarations.",
                    mixin.NameIndex,
                    XnsTokenizer.MIXIN_KEYWORD.Length + 1));
            }

            Expand(mixin.Styles, styles, including, depth + 1, errors);

            including.Remove(xnsStyle.Include);
        }

        private static XnsStyle Copy(XnsStyle style)
            => new XnsStyle
            {
                Name = style.Name,
                Value = style.Value,
                NameIndex = style.NameIndex,
                ValueIndex = style.ValueIndex
            };

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
            if (!_variables.IsEmpty)
            {
                xnsStyle.Value = _variables.Substitute(xnsStyle.Value, xnsStyle.ValueIndex, errors);
            }

            xnsStyle.Value = XnsCalc.Evaluate(xnsStyle.Value, xnsStyle.ValueIndex, errors);

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
