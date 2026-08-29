using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;

namespace Ixen.Controls
{
    public class Button : VisualElement
    {
        public Button()
        {
            Focusable = true;
            Role = AccessibleRole.Button;

            KeyDown += OnKeyDown;
        }

        private void OnKeyDown(object sender, KeyEventArgs args)
        {
            if (args.Key != Key.Space && args.Key != Key.Enter)
            {
                return;
            }

            if (!IsEnabled)
            {
                return;
            }

            args.Handled = true;

            Activate();
        }

        public void Activate() => PerformClick();
    }
}
