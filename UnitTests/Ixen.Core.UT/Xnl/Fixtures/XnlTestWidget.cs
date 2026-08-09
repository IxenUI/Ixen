using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual
{
    public class XnlTestWidget : VisualElement
    {
        public string Label { get; set; }
        public string Path { get; set; }
        public int Count { get; set; }
        public bool Enabled { get; set; }
        public float Ratio { get; set; }
        public double Precision { get; set; }
        public decimal Amount { get; set; }
        public char Initial { get; set; }
        public LayoutType Direction { get; set; }
        public int? Optional { get; set; }
        public string ReadOnlyThing { get; }
    }
}
