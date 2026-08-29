using Ixen.Core.Accessibility;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Controls
{
    public class TabItem : VisualElement
    {
        public const string SELECTED = "selected";

        public TabItem()
        {
            TypeName = nameof(TabItem);
            Role = AccessibleRole.Group;

            Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            Styles.Right = new RightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
        }

        public string Header { get; set; }

        internal void Select(bool selected) => ToggleState(SELECTED, selected);
    }
}
