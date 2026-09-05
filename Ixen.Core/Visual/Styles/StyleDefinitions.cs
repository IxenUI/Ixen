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
                Define(StyleIdentifier.ANCHOR, v => new AnchorStyleParser(v),
                    p => ((AnchorStyleParser)p).Descriptor),

                Define(StyleIdentifier.ANCHOR_PLACEMENT, v => new AnchorPlacementStyleParser(v),
                    p => ((AnchorPlacementStyleParser)p).Descriptor,
                    new[] { "below", "above", "left", "right", "start", "center", "end", "noflip" }),

                Define(StyleIdentifier.ANIMATION, v => new AnimationStyleParser(v),
                    p => ((AnimationStyleParser)p).Descriptor,
                    null, new[] { AnimationStyleParser.INFINITE, AnimationStyleParser.ALTERNATE,
                        AnimationStyleParser.NORMAL,
                        AnimationStyleParser.FORWARDS, AnimationStyleParser.NO_FILL,
                        Easing.LINEAR, Easing.EASE_IN, Easing.EASE_OUT, Easing.EASE_IN_OUT }),

                Define(StyleIdentifier.BACKGROUND, v => new BackgroundStyleParser(v),
                    p => ((BackgroundStyleParser)p).Descriptor),

                Define(StyleIdentifier.BOX_SHADOW, v => new BoxShadowStyleParser(v),
                    p => ((BoxShadowStyleParser)p).Descriptor),

                Define(StyleIdentifier.TEXT_SHADOW, v => new TextShadowStyleParser(v),
                    p => ((TextShadowStyleParser)p).Descriptor),

                Define(StyleIdentifier.BORDER, v => new BorderStyleParser(v),
                    p => ((BorderStyleParser)p).Descriptor,
                    new[] { "#CCCCCC 1px", "#CCCCCC 1px 2px", "#CCCCCC 1px 2px 3px",
                        "#CCCCCC 0px 0px 1px 0px inner" },
                    new[] { "inner", "center", "outer" }),

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
                    new[] { "default", "arrow", "hand", "pointer", "text", "caret", "wait",
                        "crosshair", "ew-resize", "ns-resize", "nesw-resize", "nwse-resize",
                        "move", "not-allowed", "help", "progress", "none" }),

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

                Define(StyleIdentifier.FILTER, v => new FilterStyleParser(v),
                    p => ((FilterStyleParser)p).Descriptor,
                    new[] { FilterStyleParser.NONE },
                    new[] { FilterStyleParser.BLUR, FilterStyleParser.DROP_SHADOW }),

                Define(StyleIdentifier.FONT_FAMILY, v => new FontFamilyStyleParser(v),
                    p => ((FontFamilyStyleParser)p).Descriptor),

                Define(StyleIdentifier.FONT_SIZE, v => new FontSizeStyleParser(v),
                    p => ((FontSizeStyleParser)p).Descriptor),

                Define(StyleIdentifier.FONT_STYLE, v => new FontStyleStyleParser(v),
                    p => ((FontStyleStyleParser)p).Descriptor, new[] { "normal", "italic" }),

                Define(StyleIdentifier.FONT_WEIGHT, v => new FontWeightStyleParser(v),
                    p => ((FontWeightStyleParser)p).Descriptor, new[] { "normal", "bold" }),

                Define(StyleIdentifier.CONTENT_ALIGN, v => new ContentAlignStyleParser(v),
                    p => ((ContentAlignStyleParser)p).Descriptor,
                    new[] { "left", "center", "right", "top", "middle", "bottom" }),

                Define(StyleIdentifier.VISIBILITY, v => new VisibilityStyleParser(v),
                    p => ((VisibilityStyleParser)p).Descriptor,
                    new[] { VisibilityStyleParser.VISIBLE, VisibilityStyleParser.HIDDEN }),

                Define(StyleIdentifier.SCROLL_BEHAVIOR, v => new ScrollBehaviorStyleParser(v),
                    p => ((ScrollBehaviorStyleParser)p).Descriptor,
                    new[] { ScrollBehaviorStyleParser.AUTO, ScrollBehaviorStyleParser.SMOOTH }),

                Define(StyleIdentifier.ASPECT_RATIO, v => new AspectRatioStyleParser(v),
                    p => ((AspectRatioStyleParser)p).Descriptor),

                Define(StyleIdentifier.GAP, v => new GapStyleParser(v),
                    p => ((GapStyleParser)p).Descriptor),

                Define(StyleIdentifier.HEIGHT, v => new HeightStyleParser(v),
                    p => ((HeightStyleParser)p).Descriptor),

                Define(StyleIdentifier.LAYOUT, v => new LayoutStyleParser(v),
                    p => ((LayoutStyleParser)p).Descriptor,
                    new[] { "row", "column", "grid", "absolute", "fixed", "dock" }),

                Define(StyleIdentifier.LEFT, v => new LeftStyleParser(v),
                    p => ((LeftStyleParser)p).Descriptor),

                Define(StyleIdentifier.LETTER_SPACING, v => new LetterSpacingStyleParser(v),
                    p => ((LetterSpacingStyleParser)p).Descriptor,
                    new[] { LetterSpacingStyleParser.NORMAL, "1px", "-0.5px" }),

                Define(StyleIdentifier.LINE_HEIGHT, v => new LineHeightStyleParser(v),
                    p => ((LineHeightStyleParser)p).Descriptor,
                    new[] { LineHeightStyleParser.NORMAL, "1.5", "24px", "150%" }),

                Define(StyleIdentifier.MARGIN, v => new MarginStyleParser(v),
                    p => ((MarginStyleParser)p).Descriptor),

                Define(StyleIdentifier.OBJECT_FIT, v => new ObjectFitStyleParser(v),
                    p => ((ObjectFitStyleParser)p).Descriptor,
                    new[] { "fill", "contain", "cover", "none", "scale-down" }),

                Define(StyleIdentifier.MAX_HEIGHT, v => new MaxHeightStyleParser(v),
                    p => ((MaxHeightStyleParser)p).Descriptor),

                Define(StyleIdentifier.MAX_WIDTH, v => new MaxWidthStyleParser(v),
                    p => ((MaxWidthStyleParser)p).Descriptor),

                Define(StyleIdentifier.MIN_HEIGHT, v => new MinHeightStyleParser(v),
                    p => ((MinHeightStyleParser)p).Descriptor),

                Define(StyleIdentifier.MIN_WIDTH, v => new MinWidthStyleParser(v),
                    p => ((MinWidthStyleParser)p).Descriptor),

                Define(StyleIdentifier.OPACITY, v => new OpacityStyleParser(v),
                    p => ((OpacityStyleParser)p).Descriptor),

                Define(StyleIdentifier.OBJECT_POSITION, v => new ObjectPositionStyleParser(v),
                    p => ((ObjectPositionStyleParser)p).Descriptor,
                    new[] { ObjectPositionStyleParser.LEFT, ObjectPositionStyleParser.CENTER,
                        ObjectPositionStyleParser.RIGHT, ObjectPositionStyleParser.TOP,
                        ObjectPositionStyleParser.MIDDLE, ObjectPositionStyleParser.BOTTOM }),

                Define(StyleIdentifier.OVERFLOW, v => new OverflowStyleParser(v),
                    p => ((OverflowStyleParser)p).Descriptor,
                    new[] { OverflowStyleParser.SCROLL, OverflowStyleParser.HIDDEN,
                        OverflowStyleParser.AUTO }),

                Define(StyleIdentifier.OVERSCROLL_BEHAVIOR, v => new OverscrollStyleParser(v),
                    p => ((OverscrollStyleParser)p).Descriptor,
                    new[] { OverscrollStyleParser.AUTO, OverscrollStyleParser.CONTAIN,
                        OverscrollStyleParser.NONE }),

                Define(StyleIdentifier.BACKDROP_FILTER, v => new BackdropFilterStyleParser(v),
                    p => ((BackdropFilterStyleParser)p).Descriptor,
                    new[] { FilterStyleParser.NONE },
                    new[] { FilterStyleParser.BLUR, FilterStyleParser.DROP_SHADOW }),

                Define(StyleIdentifier.POINTER_EVENTS, v => new PointerEventsStyleParser(v),
                    p => ((PointerEventsStyleParser)p).Descriptor,
                    new[] { PointerEventsStyleParser.AUTO, PointerEventsStyleParser.NONE }),

                Define(StyleIdentifier.PADDING, v => new PaddingStyleParser(v),
                    p => ((PaddingStyleParser)p).Descriptor),

                Define(StyleIdentifier.RIGHT, v => new RightStyleParser(v),
                    p => ((RightStyleParser)p).Descriptor),

                Define(StyleIdentifier.ROW_TEMPLATE, v => new RowTemplateStyleParser(v),
                    p => ((RowTemplateStyleParser)p).Descriptor),

                Define(StyleIdentifier.TEXT_ALIGN, v => new TextAlignStyleParser(v),
                    p => ((TextAlignStyleParser)p).Descriptor,
                    new[] { "left", "center", "right", "top", "middle", "bottom" }),

                Define(StyleIdentifier.TEXT_DECORATION, v => new TextDecorationStyleParser(v),
                    p => ((TextDecorationStyleParser)p).Descriptor,
                    new[] { TextDecorationStyleParser.NONE, TextDecorationStyleParser.UNDERLINE,
                        TextDecorationStyleParser.LINE_THROUGH, TextDecorationStyleParser.OVERLINE }),

                Define(StyleIdentifier.TEXT_OVERFLOW, v => new TextOverflowStyleParser(v),
                    p => ((TextOverflowStyleParser)p).Descriptor, new[] { "clip", "ellipsis" }),

                Define(StyleIdentifier.TEXT_WRAP, v => new TextWrapStyleParser(v),
                    p => ((TextWrapStyleParser)p).Descriptor, new[] { "wrap", "nowrap" }),

                Define(StyleIdentifier.TOP, v => new TopStyleParser(v),
                    p => ((TopStyleParser)p).Descriptor),

                Define(StyleIdentifier.TRANSFORM, v => new TransformStyleParser(v),
                    p => ((TransformStyleParser)p).Descriptor,
                    new[] { TransformStyleParser.NONE },
                    new[] { TransformStyleParser.TRANSLATE, "translateX", "translateY",
                        TransformStyleParser.SCALE, "scaleX", "scaleY",
                        TransformStyleParser.ROTATE,
                        TransformStyleParser.SKEW, "skewX", "skewY" }),

                Define(StyleIdentifier.TRANSFORM_ORIGIN, v => new TransformOriginStyleParser(v),
                    p => ((TransformOriginStyleParser)p).Descriptor,
                    new[] { "center middle", "left top", "right bottom" },
                    new[] { TransformOriginStyleParser.LEFT, TransformOriginStyleParser.CENTER,
                        TransformOriginStyleParser.RIGHT, TransformOriginStyleParser.TOP,
                        TransformOriginStyleParser.MIDDLE, TransformOriginStyleParser.BOTTOM }),

                Define(StyleIdentifier.TRANSITION, v => new TransitionStyleParser(v),
                    p => ((TransitionStyleParser)p).Descriptor,
                    null, new[] { "all", "background", "color", "border",
                        "width", "height", "left", "top", "right", "bottom", "transform",
                        Easing.LINEAR, Easing.EASE_IN, Easing.EASE_OUT, Easing.EASE_IN_OUT }),

                Define(StyleIdentifier.WIDTH, v => new WidthStyleParser(v),
                    p => ((WidthStyleParser)p).Descriptor),

                Define(StyleIdentifier.Z_INDEX, v => new ZIndexStyleParser(v),
                    p => ((ZIndexStyleParser)p).Descriptor)
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
