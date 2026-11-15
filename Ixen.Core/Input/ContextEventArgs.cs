using Ixen.Core.Visual;

namespace Ixen.Core.Input
{
    public class ContextEventArgs : PointerEventArgs
    {
        public bool FromKeyboard { get; private set; }

        internal ContextEventArgs(float x, float y, PointerButton button, VisualElement source, PointerKind kind,
            bool fromKeyboard)
            : base(x, y, button, source, kind)
        {
            FromKeyboard = fromKeyboard;
        }

        internal static bool Request(VisualElement element, float x, float y, PointerButton button, PointerKind kind,
            KeyModifiers modifiers, bool fromKeyboard)
        {
            if (element == null)
            {
                return false;
            }

            var args = new ContextEventArgs(x, y, button, element, kind, fromKeyboard) { Modifiers = modifiers };

            for (VisualElement current = element; current != null; current = current.Parent)
            {
                current.RaiseContextRequested(args);

                if (args.Handled)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool RequestAtCentre(VisualElement element)
        {
            if (element == null)
            {
                return false;
            }

            VisualElement anchor = element.ContextAnchor ?? element;

            return Request(element, anchor.X + anchor.ActualWidth / 2, anchor.Y + anchor.ActualHeight / 2,
                PointerButton.Right, PointerKind.Mouse, KeyModifiers.None, true);
        }
    }
}
