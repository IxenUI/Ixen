using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using System;

namespace Ixen.Controls
{
    public class MenuItem : VisualElement
    {
        public event EventHandler<EventArgs> Invoked;

        public MenuItem()
        {
            TypeName = nameof(MenuItem);
            Focusable = true;
            Role = AccessibleRole.MenuItem;

            PointerClick += OnPointerClick;
            KeyDown += OnKeyDown;
        }

        public void Activate()
        {
            if (!IsEnabled)
            {
                return;
            }

            Invoked?.Invoke(this, EventArgs.Empty);

            Menu owner = Owner();

            if (owner == null)
            {
                return;
            }

            owner.RaiseItemInvoked(this);
            owner.Close();
        }

        private Menu Owner()
        {
            for (VisualElement element = Parent; element != null; element = element.Parent)
            {
                if (element is Menu menu)
                {
                    return menu;
                }
            }

            return null;
        }

        private void OnPointerClick(object sender, PointerEventArgs args)
        {
            Activate();
        }

        private void OnKeyDown(object sender, KeyEventArgs args)
        {
            if (args.Key != Key.Enter && args.Key != Key.Space)
            {
                return;
            }

            args.Handled = true;

            Activate();
        }
    }
}
