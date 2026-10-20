using Ixen.Core.Accessibility;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Shapes;
using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Controls
{
    public class TreeRow : VisualElement, IExpandable
    {
        public const string EXPANDER = "TreeExpander";
        public const string EXPANDED = "expanded";
        public const string SELECTED = "selected";
        public const string LEAF = "leaf";


        private readonly VisualElement _expander;

        private VisualElement _content;
        private bool _hasChildren;
        private bool _expanded;

        public TreeRow()
        {
            TypeName = nameof(TreeRow);
            Role = AccessibleRole.TreeItem;

            Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };

            _expander = new Path
            {
                TypeName = EXPANDER,
                Role = AccessibleRole.Presentation,
                Data = Marks.DOWN
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

        internal TreeView Owner { get; set; }

        bool IExpandable.CanExpand => _hasChildren;

        bool IExpandable.IsExpanded => _hasChildren && _expanded;

        void IExpandable.SetExpanded(bool expanded)
        {
            Owner?.SetExpanded(this, expanded);
        }

        internal void SetBranch(bool hasChildren, bool expanded)
        {
            _hasChildren = hasChildren;
            _expanded = expanded;

            ToggleState(LEAF, !hasChildren);
            ToggleState(EXPANDED, expanded);

            _expander.ToggleState(LEAF, !hasChildren);
            _expander.ToggleState(EXPANDED, expanded);
        }
    }
}
