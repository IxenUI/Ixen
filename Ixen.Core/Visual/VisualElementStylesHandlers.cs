using Ixen.Core.Visual.Styles.Handlers;

namespace Ixen.Core.Visual
{
    internal class VisualElementStylesHandlers
    {
        internal static readonly BackgroundStyleHandler DefaultBackground = new();
        internal static readonly BorderStyleHandler DefaultBorder = new();
        internal static readonly ColorStyleHandler DefaultColor = new();
        internal static readonly ColumnTemplateStyleHandler DefaultColumnTemplate = new();
        internal static readonly CornerRadiusStyleHandler DefaultCornerRadius = new();
        internal static readonly FontFamilyStyleHandler DefaultFontFamily = new();
        internal static readonly FontSizeStyleHandler DefaultFontSize = new();
        internal static readonly FontStyleStyleHandler DefaultFontStyle = new();
        internal static readonly FontWeightStyleHandler DefaultFontWeight = new();
        internal static readonly HeightStyleHandler DefaultHeight = new();
        internal static readonly LayoutStyleHandler DefaultLayout = new();
        internal static readonly MarginStyleHandler DefaultMargin = new();
        internal static readonly PaddingStyleHandler DefaultPadding = new();
        internal static readonly RowTemplateStyleHandler DefaultRowTemplate = new();
        internal static readonly TextAlignStyleHandler DefaultTextAlign = new();
        internal static readonly TextOverflowStyleHandler DefaultTextOverflow = new();
        internal static readonly TextWrapStyleHandler DefaultTextWrap = new();
        internal static readonly WidthStyleHandler DefaultWidth = new();

        public BackgroundStyleHandler Background { get; set; } = DefaultBackground;
        public BorderStyleHandler Border { get; set; } = DefaultBorder;
        public ColorStyleHandler Color { get; set; } = DefaultColor;
        public ColumnTemplateStyleHandler ColumnTemplate { get; set; } = DefaultColumnTemplate;
        public CornerRadiusStyleHandler CornerRadius { get; set; } = DefaultCornerRadius;
        public FontFamilyStyleHandler FontFamily { get; set; } = DefaultFontFamily;
        public FontSizeStyleHandler FontSize { get; set; } = DefaultFontSize;
        public FontStyleStyleHandler FontStyle { get; set; } = DefaultFontStyle;
        public FontWeightStyleHandler FontWeight { get; set; } = DefaultFontWeight;
        public HeightStyleHandler Height { get; set; } = DefaultHeight;
        public LayoutStyleHandler Layout { get; set; } = DefaultLayout;
        public MarginStyleHandler Margin { get; set; } = DefaultMargin;
        public PaddingStyleHandler Padding { get; set; } = DefaultPadding;
        public RowTemplateStyleHandler RowTemplate { get; set; } = DefaultRowTemplate;
        public TextAlignStyleHandler TextAlign { get; set; } = DefaultTextAlign;
        public TextOverflowStyleHandler TextOverflow { get; set; } = DefaultTextOverflow;
        public TextWrapStyleHandler TextWrap { get; set; } = DefaultTextWrap;
        public WidthStyleHandler Width { get; set; } = DefaultWidth;
    }
}
