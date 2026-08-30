using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles;
using System.Collections.Generic;

namespace Ixen.Core.Input
{
    internal class KeyboardDispatcher
    {

        private readonly List<VisualElement> _focusables = new();

        private bool _trackStates;
        private VisualElement _focused;

        internal VisualElement Focused => _focused;

        internal void Focus(VisualElement element, bool trackStates)
        {
            _trackStates = trackStates;

            if (element != null && (!element.Focusable || !element.IsEnabled))
            {
                return;
            }

            if (element == _focused)
            {
                return;
            }

            VisualElement previous = _focused;
            _focused = element;

            if (previous != null)
            {
                SetState(previous, false);
                previous.RaiseLostFocus();
            }

            if (element != null)
            {
                SetState(element, true);
                element.RaiseGotFocus();
            }
        }

        internal void ElementDetached(VisualElement element)
        {
            if (element == null || _focused != element)
            {
                return;
            }

            SetState(element, false);
            _focused = null;
        }

        internal void FocusFromPointer(VisualElement hit, bool trackStates)
        {
            for (VisualElement candidate = hit; candidate != null; candidate = candidate.Parent)
            {
                if (candidate.Focusable)
                {
                    Focus(candidate, trackStates);
                    return;
                }
            }

            Focus(null, trackStates);
        }

        private readonly bool[] _held = new bool[HELD_KEYS];

        private const int HELD_KEYS = 256;

        private bool PressAndTellIfRepeat(Key key)
        {
            int index = (int)key;

            if (key == Key.None || index < 0 || index >= HELD_KEYS)
            {
                return false;
            }

            bool repeat = _held[index];

            _held[index] = true;

            return repeat;
        }

        private void Release(Key key)
        {
            int index = (int)key;

            if (index >= 0 && index < HELD_KEYS)
            {
                _held[index] = false;
            }
        }

        internal void KeyDown(VisualElement root, Key key, KeyModifiers modifiers, bool trackStates)
        {
            _trackStates = trackStates;

            VisualElement target = _focused ?? root;

            if (target == null)
            {
                return;
            }

            var args = new KeyEventArgs(key, modifiers, target, PressAndTellIfRepeat(key));

            for (VisualElement element = target; element != null; element = element.Parent)
            {
                element.RaiseKeyDown(args);

                if (args.Handled)
                {
                    return;
                }
            }

            if (key == Key.Tab)
            {
                MoveFocus(root, args.HasModifier(KeyModifiers.Shift), trackStates);
                return;
            }

            ScrollBy(target, root, key);
        }

        private static void ScrollBy(VisualElement from, VisualElement root, Key key)
        {
            VisualElement page = Target(from, root, PageStep(key, true), PageStep(key, false));

            float offsetX = 0;
            float offsetY = 0;

            switch (key)
            {
                case Key.Up:
                    offsetY = -ScrollNavigator.STEP;
                    break;

                case Key.Down:
                    offsetY = ScrollNavigator.STEP;
                    break;

                case Key.Left:
                    offsetX = -ScrollNavigator.STEP;
                    break;

                case Key.Right:
                    offsetX = ScrollNavigator.STEP;
                    break;

                case Key.PageUp:
                    offsetY = page == null ? -ScrollNavigator.STEP : -page.ContentHeight;
                    break;

                case Key.PageDown:
                    offsetY = page == null ? ScrollNavigator.STEP : page.ContentHeight;
                    break;

                case Key.Home:
                    ScrollToEnd(from, root, false);
                    return;

                case Key.End:
                    ScrollToEnd(from, root, true);
                    return;

                default:
                    return;
            }

            Target(from, root, offsetX, offsetY)?.ScrollBy(offsetX, offsetY);
        }

        private static VisualElement Target(VisualElement from, VisualElement root,
            float offsetX, float offsetY)
        {
            VisualElement found = ScrollNavigator.Find(from, offsetX, offsetY, out bool contained);

            if (found != null || contained)
            {
                return found;
            }

            return ScrollNavigator.FindDefault(root, offsetX, offsetY);
        }

        private static float PageStep(Key key, bool horizontal)
        {
            if (horizontal)
            {
                return 0;
            }

            return key == Key.PageUp ? -1 : key == Key.PageDown ? 1 : 0;
        }

        private static void ScrollToEnd(VisualElement from, VisualElement root, bool end)
        {
            VisualElement target = Target(from, root, 0, end ? 1 : -1);

            if (target != null)
            {
                target.ScrollY = end ? target.MaxScrollY : 0;
            }
        }

        internal void KeyUp(VisualElement root, Key key, KeyModifiers modifiers, bool trackStates)
        {
            _trackStates = trackStates;

            Release(key);

            VisualElement target = _focused ?? root;

            if (target == null)
            {
                return;
            }

            var args = new KeyEventArgs(key, modifiers, target);

            for (VisualElement element = target; element != null; element = element.Parent)
            {
                element.RaiseKeyUp(args);

                if (args.Handled)
                {
                    return;
                }
            }
        }

        internal void TextInput(VisualElement root, string text, bool trackStates)
        {
            _trackStates = trackStates;

            VisualElement target = _focused ?? root;

            if (target == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            var args = new TextInputEventArgs(text, target);

            for (VisualElement element = target; element != null; element = element.Parent)
            {
                element.RaiseTextInput(args);

                if (args.Handled)
                {
                    return;
                }
            }
        }

        internal void MoveFocus(VisualElement root, bool backwards, bool trackStates)
        {
            _focusables.Clear();
            Collect(ModalScope(root) ?? root, _focusables);

            if (_focusables.Count == 0)
            {
                return;
            }

            int index = _focused == null ? -1 : _focusables.IndexOf(_focused);
            int next;

            if (index < 0)
            {
                next = backwards ? _focusables.Count - 1 : 0;
            }
            else
            {
                next = backwards ? index - 1 : index + 1;

                if (next < 0)
                {
                    next = _focusables.Count - 1;
                }
                else if (next >= _focusables.Count)
                {
                    next = 0;
                }
            }

            Focus(_focusables[next], trackStates);
        }

        private static VisualElement ModalScope(VisualElement root)
        {
            if (root == null || !root.HasOverlays)
            {
                return null;
            }

            for (int index = root.Overlays.Count - 1; index >= 0; index--)
            {
                if (root.Overlays[index].Modal)
                {
                    return root.Overlays[index];
                }
            }

            return null;
        }

        private static void Collect(VisualElement element, List<VisualElement> result)
        {
            if (element == null)
            {
                return;
            }

            if (element.Focusable && element.IsEnabled)
            {
                result.Add(element);
            }

            foreach (VisualElement child in element.Children)
            {
                Collect(child, result);
            }
        }

        private void SetState(VisualElement element, bool present)
        {
            if (!_trackStates || element == null)
            {
                return;
            }

            element.ToggleState(StyleStates.FOCUS, present);
        }
    }
}
