using Ixen.Core.Visual.Styles.Parsers;
using System.Collections.Generic;

namespace Ixen.Core.Visual.Styles
{
    internal static class StyleDefinitions
    {
        private static readonly Dictionary<string, StyleDefinition> _byName;
        private static readonly List<StyleDefinition> _all;

        internal static IReadOnlyList<StyleDefinition> All => _all;

        internal static StyleDefinition Find(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            return _byName.TryGetValue(name.ToLower(), out StyleDefinition definition) ? definition : null;
        }

        internal static IReadOnlyList<string> ValuesOf(string name)
            => Find(name)?.Values ?? new string[0];

        static StyleDefinitions()
        {
            _all = new List<StyleDefinition>
            {
                Define(StyleIdentifier.ANIMATION, v => new AnimationStyleParser(v),
                    p => ((AnimationStyleParser)p).Descriptor,
                    null, new[] { AnimationStyleParser.INFINITE, AnimationStyleParser.ALTERNATE,
                        AnimationStyleParser.NORMAL,
                        Easing.LINEAR, Easing.EASE_IN, Easing.EASE_OUT, Easing.EASE_IN_OUT }),

                Define(StyleIdentifier.BACKGROUND, v => new BackgroundStyleParser(v),
                    p => ((BackgroundStyleParser)p).Descriptor),

                Define(StyleIdentifier.BORDER, v => new BorderStyleParser(v),
                    p => ((BorderStyleParser)p).Descriptor,
                    null, new[] { "inner", "center", "outer" }),

                Define(StyleIdentifier.BOTTOM, v => new BottomStyleParser(v),
                    p => ((BottomStyleParser)p).Descriptor),

                Define(StyleIdentifier.COLOR, v => new ColorStyleParser(v),
                    p => ((ColorStyleParser)p).Descriptor),

                Define(StyleIdentifier.COLUMN_TEMPLATE, v => new ColumnTemplateStyleParser(v),
                    p => ((ColumnTemplateStyleParser)p).Descriptor),

                Define(StyleIdentifier.CORNER_RADIUS, v => new CornerRadiusStyleParser(v),
                    p => ((CornerRadiusStyleParser)p).Descriptor),

                Define(StyleIdentifier.CURSOR, v => new CursorStyleParser(v),
                    p => ((CursorStyleParser)p).Descriptor,
                    new[] { "default", "arrow", "hand", "pointer", "text", "caret", "wait", "crosshair", "ew-resize", "ns-resize" }),

                Define(StyleIdentifier.DOCK, v => new DockStyleParser(v),
                    p => ((DockStyleParser)p).Descriptor, new[] { "left", "top", "right", "bottom", "fill" }),

                Define(StyleIdentifier.COLUMN_INDEX, v => new ColumnIndexStyleParser(v),
                    p => ((ColumnIndexStyleParser)p).Descriptor, new[] { GridIndexStyleParser.AUTO }),

                Define(StyleIdentifier.ROW_INDEX, v => new RowIndexStyleParser(v),
                    p => ((RowIndexStyleParser)p).Descriptor, new[] { GridIndexStyleParser.AUTO }),

                Define(StyleIdentifier.COLUMN_SPAN, v => new ColumnSpanStyleParser(v),
                    p => ((ColumnSpanStyleParser)p).Descriptor),

                Define(StyleIdentifier.ROW_SPAN, v => new RowSpanStyleParser(v),
                    p => ((RowSpanStyleParser)p).Descriptor),

                Define(StyleIdentifier.FONT_FAMILY, v => new FontFamilyStyleParser(v),
                    p => ((FontFamilyStyleParser)p).Descriptor),

                Define(StyleIdentifier.FONT_SIZE, v => new FontSizeStyleParser(v),
                    p => ((FontSizeStyleParser)p).Descriptor),

                Define(StyleIdentifier.FONT_STYLE, v => new FontStyleStyleParser(v),
                    p => ((FontStyleStyleParser)p).Descriptor, new[] { "normal", "italic" }),

                Define(StyleIdentifier.FONT_WEIGHT, v => new FontWeightStyleParser(v),
                    p => ((FontWeightStyleParser)p).Descriptor, new[] { "normal", "bold" }),

                Define(StyleIdentifier.HEIGHT, v => new HeightStyleParser(v),
                    p => ((HeightStyleParser)p).Descriptor),

                Define(StyleIdentifier.LAYOUT, v => new LayoutStyleParser(v),
                    p => ((LayoutStyleParser)p).Descriptor,
                    new[] { "row", "column", "grid", "absolute", "fixed", "dock" }),

                Define(StyleIdentifier.LEFT, v => new LeftStyleParser(v),
                    p => ((LeftStyleParser)p).Descriptor),

                Define(StyleIdentifier.MARGIN, v => new MarginStyleParser(v),
                    p => ((MarginStyleParser)p).Descriptor),

                Define(StyleIdentifier.PADDING, v => new PaddingStyleParser(v),
                    p => ((PaddingStyleParser)p).Descriptor),

                Define(StyleIdentifier.RIGHT, v => new RightStyleParser(v),
                    p => ((RightStyleParser)p).Descriptor),

                Define(StyleIdentifier.ROW_TEMPLATE, v => new RowTemplateStyleParser(v),
                    p => ((RowTemplateStyleParser)p).Descriptor),

                Define(StyleIdentifier.TEXT_ALIGN, v => new TextAlignStyleParser(v),
                    p => ((TextAlignStyleParser)p).Descriptor,
                    new[] { "left", "center", "right", "top", "middle", "bottom" }),

                Define(StyleIdentifier.TEXT_OVERFLOW, v => new TextOverflowStyleParser(v),
                    p => ((TextOverflowStyleParser)p).Descriptor, new[] { "clip", "ellipsis" }),

                Define(StyleIdentifier.TEXT_WRAP, v => new TextWrapStyleParser(v),
                    p => ((TextWrapStyleParser)p).Descriptor, new[] { "wrap", "nowrap" }),

                Define(StyleIdentifier.TOP, v => new TopStyleParser(v),
                    p => ((TopStyleParser)p).Descriptor),

                Define(StyleIdentifier.TRANSITION, v => new TransitionStyleParser(v),
                    p => ((TransitionStyleParser)p).Descriptor,
                    null, new[] { "all", "background", "color", "border",
                        "width", "height", "left", "top", "right", "bottom",
                        Easing.LINEAR, Easing.EASE_IN, Easing.EASE_OUT, Easing.EASE_IN_OUT }),

                Define(StyleIdentifier.WIDTH, v => new WidthStyleParser(v),
                    p => ((WidthStyleParser)p).Descriptor)
            };

            _byName = new Dictionary<string, StyleDefinition>();

            foreach (StyleDefinition definition in _all)
            {
                _byName[definition.Name] = definition;
            }
        }

        private static StyleDefinition Define(string name,
            System.Func<string, StyleParser> createParser,
            System.Func<StyleParser, Descriptors.StyleDescriptor> getDescriptor,
            string[] values = null, string[] keywords = null)
            => new StyleDefinition(name, createParser, getDescriptor, values, keywords);
    }
}
