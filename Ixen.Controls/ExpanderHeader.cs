using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;

namespace Ixen.Controls
{
    internal sealed class ExpanderHeader : VisualElement, IExpandable
    {
        private readonly Expander _owner;

        public ExpanderHeader(Expander owner)
        {
            _owner = owner;

            Focusable = true;
            Role = AccessibleRole.Button;

            PointerClick += OnClick;
            KeyDown += OnKeyDown;
        }

        public bool CanExpand => true;

        public bool IsExpanded => _owner.Expanded;

        public void SetExpanded(bool expanded) => _owner.Interact(expanded);

        private void OnClick(object sender, PointerEventArgs args)
        {
            args.Handled = true;

            _owner.Interact(!_owner.Expanded);
        }

        private void OnKeyDown(object sender, KeyEventArgs args)
        {
            if (args.Modifiers != KeyModifiers.None || (args.Key != Key.Space && args.Key != Key.Enter))
            {
                return;
            }

            args.Handled = true;

            PerformClick();
        }
    }
}
