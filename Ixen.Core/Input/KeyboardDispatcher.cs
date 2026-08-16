using Ixen.Core.Visual;
using System.Collections.Generic;

namespace Ixen.Core.Input
{
    internal class KeyboardDispatcher
    {
        internal const string FOCUS_STATE = "focus";

        private readonly List<VisualElement> _focusables = new();

        private bool _trackStates;
        private VisualElement _focused;

        internal VisualElement Focused => _focused;

        internal void Focus(VisualElement element, bool trackStates)
        {
            _trackStates = trackStates;

            if (element != null && !element.Focusable)
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

        internal void KeyDown(VisualElement root, Key key, KeyModifiers modifiers, bool trackStates)
        {
            _trackStates = trackStates;

            VisualElement target = _focused ?? root;

            if (target == null)
            {
                return;
            }

            var args = new KeyEventArgs(key, modifiers, target);

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
            }
        }

        internal void KeyUp(VisualElement root, Key key, KeyModifiers modifiers, bool trackStates)
        {
            _trackStates = trackStates;

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
            Collect(root, _focusables);

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

        private static void Collect(VisualElement element, List<VisualElement> result)
        {
            if (element == null)
            {
                return;
            }

            if (element.Focusable)
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

            element.ToggleState(FOCUS_STATE, present);
        }
    }
}
