using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;

namespace Ixen.Controls
{
    public class Button : VisualElement
    {
        public Button()
        {
            TypeName = nameof(Button);
            Focusable = true;
            Role = AccessibleRole.Button;

            KeyDown += OnKeyDown;
            PointerClick += OnPointerClick;
        }

        public string Result { get; set; }

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

        private void OnPointerClick(object sender, PointerEventArgs args)
        {
            if (Result == null || !IsEnabled)
            {
                return;
            }

            for (VisualElement element = Parent; element != null; element = element.Parent)
            {
                if (element is Dialog dialog)
                {
                    dialog.Close(Result);

                    return;
                }
            }
        }
    }
}
