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

        private readonly List<VisualElement> _chain = new List<VisualElement>();
        private readonly List<int> _chainAt = new List<int>();

        private VisualElement _resumeParent;
        private int _resumeIndex;

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

            Remember(element);
        }

        private void Remember(VisualElement element)
        {
            _resumeParent = null;
            _chain.Clear();
            _chainAt.Clear();

            for (VisualElement step = element; step != null; step = step.Parent)
            {
                _chain.Add(step);
                _chainAt.Add(step.ChildIndex);
            }
        }

        internal static bool CanHoldFocus(VisualElement element)
            => element != null
                && element.Focusable
                && element.IsEnabled
                && !element.IsHiddenInTree;

        internal void Refresh(bool trackStates)
        {
            if (_focused == null || CanHoldFocus(_focused))
            {
                return;
            }

            Focus(null, trackStates);
        }

        internal void ElementDetached(VisualElement element)
        {
            if (element == null || _focused != element)
            {
                return;
            }

            SetState(element, false);
            _focused = null;

            for (int index = 1; index < _chain.Count; index++)
            {
                if (_chain[index].Host == null)
                {
                    continue;
                }

                _resumeParent = _chain[index];
                _resumeIndex = _chainAt[index - 1];
                break;
            }

            _chain.Clear();
            _chainAt.Clear();
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

        internal void KeyDown(VisualElement root, Key key, KeyModifiers modifiers, bool trackStates,
            bool? isRepeat = null)
        {
            _trackStates = trackStates;

            VisualElement target = _focused ?? root;

            if (target == null)
            {
                return;
            }

            bool derived = PressAndTellIfRepeat(key);

            var args = new KeyEventArgs(key, modifiers, target, isRepeat ?? derived);

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
                int resume = ResumeAt(ModalScope(root) ?? root);

                if (resume < 0)
                {
                    next = backwards ? _focusables.Count - 1 : 0;
                }
                else if (backwards)
                {
                    next = resume - 1 < 0 ? _focusables.Count - 1 : resume - 1;
                }
                else
                {
                    next = resume >= _focusables.Count ? 0 : resume;
                }
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

        private int ResumeAt(VisualElement scope)
        {
            if (_resumeParent == null)
            {
                return -1;
            }

            int before = 0;

            return CountBefore(scope, _resumeParent, _resumeIndex, ref before)
                ? before
                : -1;
        }

        private static bool CountBefore(VisualElement element, VisualElement parent, int at,
            ref int before)
        {
            if (element == null || element.IsHidden)
            {
                return false;
            }

            if (element.Focusable && element.IsEnabled)
            {
                before++;
            }

            int index = 0;

            foreach (VisualElement child in element.Children)
            {
                if (element == parent && index >= at)
                {
                    return true;
                }

                if (CountBefore(child, parent, at, ref before))
                {
                    return true;
                }

                index++;
            }

            return element == parent;
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

            if (element.IsHidden)
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
