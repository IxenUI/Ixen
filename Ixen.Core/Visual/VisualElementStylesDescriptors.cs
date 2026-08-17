using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual
{
    public class VisualElementStylesDescriptors
    {
        public BackgroundStyleDescriptor Background { get; set; } = new();
        public BorderStyleDescriptor Border { get; set; } = new();
        public BottomStyleDescriptor Bottom { get; set; } = new();
        public ColorStyleDescriptor Color { get; set; } = new();
        public ColumnIndexStyleDescriptor ColumnIndex { get; set; } = new();
        public ColumnSpanStyleDescriptor ColumnSpan { get; set; } = new();
        public ColumnTemplateStyleDescriptor ColumnTemplate { get; set; } = new();
        public CornerRadiusStyleDescriptor CornerRadius { get; set; } = new();
        public CursorStyleDescriptor Cursor { get; set; } = new();
        public DockStyleDescriptor Dock { get; set; } = new();
        public FontFamilyStyleDescriptor FontFamily { get; set; } = new();
        public FontSizeStyleDescriptor FontSize { get; set; } = new();
        public FontStyleStyleDescriptor FontStyle { get; set; } = new();
        public FontWeightStyleDescriptor FontWeight { get; set; } = new();
        public HeightStyleDescriptor Height { get; set; } = new();
        public LayoutStyleDescriptor Layout { get; set; } = new();
        public LeftStyleDescriptor Left { get; set; } = new();
        public MarginStyleDescriptor Margin { get; set; } = new();
        public PaddingStyleDescriptor Padding { get; set; } = new();
        public RightStyleDescriptor Right { get; set; } = new();
        public RowIndexStyleDescriptor RowIndex { get; set; } = new();
        public RowSpanStyleDescriptor RowSpan { get; set; } = new();
        public RowTemplateStyleDescriptor RowTemplate { get; set; } = new();
        public TextAlignStyleDescriptor TextAlign { get; set; } = new();
        public TextOverflowStyleDescriptor TextOverflow { get; set; } = new();
        public TextWrapStyleDescriptor TextWrap { get; set; } = new();
        public TopStyleDescriptor Top { get; set; } = new();
        public TransitionStyleDescriptor Transition { get; set; } = new();
        public WidthStyleDescriptor Width { get; set; } = new();
    }
}
