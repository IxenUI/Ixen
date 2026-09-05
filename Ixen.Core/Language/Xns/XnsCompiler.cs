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

        private static readonly string CONTAINER_AT =
            XnsTokenizer.KEYFRAMES_MARKER + XnsTokenizer.CONTAINER_KEYWORD;

        private static readonly string MEDIA_AT =
            XnsTokenizer.KEYFRAMES_MARKER + XnsTokenizer.MEDIA_KEYWORD;

        private static readonly string NO_CONTAINER =
            $"A top-level '{CONTAINER_AT}' block has no container to measure."
                + " Nest it inside the selector whose size it asks about.";

        private static readonly string TWO_CONTAINERS =
            $"A '{CONTAINER_AT}' block cannot sit inside another one with a selector between"
                + " them: the two would ask about different containers, and a rule carries one.";

        private static readonly string CONTAINER_HOLDS_SELECTORS =
            $"A '{CONTAINER_AT}' block holds selectors, not styles: a container query describes"
                + " what is inside the container, never the container itself.";

        private static readonly string MEDIA_HOLDS_SELECTORS =
            $"A top-level '{MEDIA_AT}' block holds selectors, not styles.";

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

        private void AddClasses(XnsNode node, XnsNode selector, MediaQuery media, MediaQuery container,
            int containerDepth, ClassesSet set, List<LanguageError> errors)
        {
            List<StyleDescriptor> styles = ToStyles(node, errors);

            foreach (string entry in StyleScope.Split(selector.Name))
            {
                string name = StyleScope.Bare(entry);
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

                List<string> scopes = StyleScope.BuildAll(selector, entry, n => n.Parent, n => n.Name);

                if (scopes == null)
                {
                    set.Classes.Add(NewClass(node, target, null, name, styles, media, container, containerDepth));
                    continue;
                }

                foreach (string scope in scopes)
                {
                    set.Classes.Add(NewClass(node, target, scope, name, styles, media, container, containerDepth));
                }
            }
        }

        private static StyleClass NewClass(XnsNode node, StyleClassTarget target, string scope,
            string name, List<StyleDescriptor> styles, MediaQuery media, MediaQuery container,
            int containerDepth)
            => new StyleClass(target, null, scope, name, styles, media)
            {
                Container = container,
                ContainerDepth = containerDepth,
                SourceIndex = node.NameIndex,
                SourceLength = node.Name == null ? 0 : node.Name.Length
            };

        private void Add(XnsNode node, ClassesSet set, List<LanguageError> errors)
            => Add(node, set, null, null, 0, errors);

        private void Add(XnsNode node, ClassesSet set, MediaQuery media, MediaQuery container,
            int containerDepth, List<LanguageError> errors)
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

            if (node.Container != null)
            {
                int depth = ScopeDepth(node);

                if (depth == 0)
                {
                    Report(node, XnsTokenizer.CONTAINER_KEYWORD, NO_CONTAINER, errors);
                    return;
                }

                if (container != null && depth != containerDepth)
                {
                    Report(node, XnsTokenizer.CONTAINER_KEYWORD, TWO_CONTAINERS, errors);
                    return;
                }

                container = GetContainer(node, container, errors);
                containerDepth = depth;

                if (container == null)
                {
                    return;
                }

                if (node.Styles.Count > 0)
                {
                    Report(node, XnsTokenizer.CONTAINER_KEYWORD, CONTAINER_HOLDS_SELECTORS, errors);
                }
            }
            else if (node.Media != null)
            {
                media = GetMedia(node, media, errors);

                if (media == null)
                {
                    return;
                }

                if (node.Styles.Count > 0)
                {
                    AddInherited(node, set, media, container, containerDepth, errors);
                }
            }
            else if (node.Styles.Count > 0)
            {
                AddClasses(node, node, media, container, containerDepth, set, errors);
            }

            foreach (var child in node.Children)
            {
                Add(child, set, media, container, containerDepth, errors);
            }
        }

        private static int ScopeDepth(XnsNode node)
        {
            int depth = 0;

            for (XnsNode current = node.Parent; current != null; current = current.Parent)
            {
                if (!string.IsNullOrEmpty(current.Name))
                {
                    depth++;
                }
            }

            return depth;
        }

        private static void Report(XnsNode node, string keyword, string message,
            List<LanguageError> errors)
            => errors.Add(new LanguageError(
                LanguageErrorCode.SYNTAX, message, node.NameIndex, keyword.Length + 1));

        private void AddInherited(XnsNode node, ClassesSet set, MediaQuery media, MediaQuery container,
            int containerDepth, List<LanguageError> errors)
        {
            XnsNode selector = node.Parent;

            while (selector != null && selector.Name == null)
            {
                selector = selector.Parent;
            }

            if (selector == null)
            {
                Report(node, XnsTokenizer.MEDIA_KEYWORD, MEDIA_HOLDS_SELECTORS, errors);
                return;
            }

            if (container != null)
            {
                Report(node, XnsTokenizer.MEDIA_KEYWORD, CONTAINER_HOLDS_SELECTORS, errors);
                return;
            }

            AddClasses(node, selector, media, container, containerDepth, set, errors);
        }

        private MediaQuery GetContainer(XnsNode node, MediaQuery outer, List<LanguageError> errors)
        {
            MediaQuery query = MediaQuery.Parse(_variables.IsEmpty
                ? node.Container
                : _variables.Substitute(node.Container, node.NameIndex, errors));

            if (query == null)
            {
                errors.Add(new LanguageError(
                    LanguageErrorCode.INVALID_STYLE_VALUE,
                    $"'{node.Container}' is not a valid container query. Use min-width, max-width, min-height,"
                        + " max-height or orientation, combined with 'and'.",
                    node.NameIndex,
                    XnsTokenizer.CONTAINER_KEYWORD.Length + 1));

                return null;
            }

            return outer == null ? query : outer.And(query);
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
                List<StyleDescriptor> styles = null;

                foreach (string entry in StyleScope.Split(child.Name))
                {
                    if (!TryParseOffset(entry, out float offset))
                    {
                        errors.Add(new LanguageError(
                            LanguageErrorCode.SYNTAX,
                            $"'{entry}' is not a valid keyframe offset. Use a whole percentage, '{FROM_OFFSET}' or '{TO_OFFSET}'.",
                            child.NameIndex,
                            child.Name?.Length ?? 0));

                        continue;
                    }

                    if (styles == null)
                    {
                        if (child.Children.Count > 0)
                        {
                            errors.Add(new LanguageError(
                                LanguageErrorCode.SYNTAX,
                                "A keyframe offset cannot contain a nested block.",
                                child.NameIndex,
                                child.Name.Length));
                        }

                        styles = ToStyles(child, errors);
                    }

                    frames.Add(new Keyframe(offset, styles));
                }
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
