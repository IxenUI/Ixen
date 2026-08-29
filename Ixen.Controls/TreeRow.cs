using Ixen.Core.Accessibility;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Controls
{
    public class TreeRow : VisualElement
    {
        public const string EXPANDER = "TreeExpander";
        public const string EXPANDED = "expanded";
        public const string SELECTED = "selected";
        public const string LEAF = "leaf";

        private const string GLYPH = "\u25BC";

        private readonly VisualElement _expander;

        private VisualElement _content;

        public TreeRow()
        {
            TypeName = nameof(TreeRow);
            Role = AccessibleRole.TreeItem;

            Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };

            _expander = new VisualElement
            {
                TypeName = EXPANDER,
                Role = AccessibleRole.Presentation,
                Text = GLYPH
            };

            AddChild(_expander);
        }

        public VisualElement Expander => _expander;

        public VisualElement Content => _content;

        internal void SetContent(VisualElement content)
        {
            _content = content;

            AddChild(content);
        }

        internal void SetDepth(float indent)
        {
            Styles.Padding = new PaddingStyleDescriptor
            {
                Left = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = indent }
            };
        }

        internal void SetBranch(bool hasChildren, bool expanded)
        {
            ToggleState(LEAF, !hasChildren);
            ToggleState(EXPANDED, expanded);

            _expander.ToggleState(LEAF, !hasChildren);
            _expander.ToggleState(EXPANDED, expanded);
        }
    }
}
