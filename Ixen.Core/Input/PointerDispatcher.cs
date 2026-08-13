using Ixen.Core.Visual;
using System.Collections.Generic;

namespace Ixen.Core.Input
{
    internal class PointerDispatcher
    {
        internal const string HOVER_STATE = "hover";
        internal const string PRESSED_STATE = "pressed";

        private const float WHEEL_STEP = 48f;

        private readonly List<VisualElement> _leftChain = new();
        private readonly List<VisualElement> _enteredChain = new();

        private bool _trackStates;

        private VisualElement _hovered;
        private VisualElement _pressed;
        private VisualElement _captured;

        internal VisualElement Hovered => _hovered;
        internal VisualElement Pressed => _pressed;
        internal VisualElement Captured => _captured;

        internal void Move(VisualElement root, float x, float y, bool trackStates)
        {
            _trackStates = trackStates;

            VisualElement hit = HitTester.HitTest(root, x, y);

            if (_captured != null)
            {
                UpdateHover(CaptureHoverTarget(hit), x, y);
                Bubble(_captured, new PointerEventArgs(x, y, PointerButton.None, _captured), PointerEventKind.Move);
                return;
            }

            UpdateHover(hit, x, y);
            Bubble(hit, new PointerEventArgs(x, y, PointerButton.None, hit), PointerEventKind.Move);
        }

        internal void Wheel(VisualElement root, float x, float y, float deltaX, float deltaY)
        {
            VisualElement hit = _captured ?? HitTester.HitTest(root, x, y);

            if (hit == null)
            {
                return;
            }

            var args = new WheelEventArgs(x, y, deltaX, deltaY, hit);

            for (VisualElement element = hit; element != null; element = element.Parent)
            {
                element.RaisePointerWheel(args);

                if (args.Handled)
                {
                    return;
                }
            }

            Scroll(hit, deltaX, deltaY);
        }

        private static void Scroll(VisualElement hit, float deltaX, float deltaY)
        {
            float offsetX = deltaX * WHEEL_STEP;
            float offsetY = -deltaY * WHEEL_STEP;

            for (VisualElement element = hit; element != null; element = element.Parent)
            {
                if (element.Scrollable && CanScroll(element, offsetX, offsetY))
                {
                    element.ScrollBy(offsetX, offsetY);
                    return;
                }
            }
        }

        private static bool CanScroll(VisualElement element, float offsetX, float offsetY)
            => CanScrollAxis(element.ScrollX, element.MaxScrollX, offsetX)
                || CanScrollAxis(element.ScrollY, element.MaxScrollY, offsetY);

        private static bool CanScrollAxis(float offset, float max, float delta)
        {
            if (delta < 0)
            {
                return offset > 0;
            }

            return delta > 0 && offset < max;
        }

        internal void LeaveSurface(bool trackStates)
        {
            if (_captured != null)
            {
                return;
            }

            _trackStates = trackStates;

            UpdateHover(null, 0, 0);
            _pressed = null;
        }

        internal void ReleaseCapture()
        {
            _captured = null;
            SetState(_pressed, PRESSED_STATE, false);
            _pressed = null;
        }

        internal void Down(VisualElement root, float x, float y, PointerButton button, bool trackStates)
        {
            _trackStates = trackStates;

            VisualElement hit = HitTester.HitTest(root, x, y);

            UpdateHover(hit, x, y);

            _pressed = hit;
            _captured = hit;

            SetState(hit, PRESSED_STATE, true);

            Bubble(hit, new PointerEventArgs(x, y, button, hit), PointerEventKind.Down);
        }

        internal void Up(VisualElement root, float x, float y, PointerButton button, bool trackStates)
        {
            _trackStates = trackStates;

            VisualElement hit = HitTester.HitTest(root, x, y);
            VisualElement target = _captured ?? hit;

            _captured = null;

            SetState(_pressed, PRESSED_STATE, false);

            UpdateHover(hit, x, y);
            Bubble(target, new PointerEventArgs(x, y, button, target), PointerEventKind.Up);

            bool isClick = hit != null && hit == _pressed;

            _pressed = null;

            if (isClick)
            {
                Bubble(hit, new PointerEventArgs(x, y, button, hit), PointerEventKind.Click);
            }
        }

        private VisualElement CaptureHoverTarget(VisualElement hit)
        {
            if (hit == null)
            {
                return null;
            }

            if (IsWithinCapture(hit))
            {
                return hit;
            }

            for (VisualElement candidate = hit; candidate != null; candidate = candidate.Parent)
            {
                if (IsOnCaptureChain(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private void SetState(VisualElement element, string state, bool present)
        {
            if (!_trackStates || element == null)
            {
                return;
            }

            element.ToggleState(state, present);
        }

        private bool IsWithinCapture(VisualElement element)
        {
            for (VisualElement current = element; current != null; current = current.Parent)
            {
                if (current == _captured)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsOnCaptureChain(VisualElement element)
        {
            for (VisualElement current = _captured; current != null; current = current.Parent)
            {
                if (current == element)
                {
                    return true;
                }
            }

            return false;
        }

        private static void Bubble(VisualElement hit, PointerEventArgs args, PointerEventKind kind)
        {
            for (VisualElement element = hit; element != null; element = element.Parent)
            {
                switch (kind)
                {
                    case PointerEventKind.Down:
                        element.RaisePointerDown(args);
                        break;

                    case PointerEventKind.Up:
                        element.RaisePointerUp(args);
                        break;

                    case PointerEventKind.Move:
                        element.RaisePointerMove(args);
                        break;

                    case PointerEventKind.Click:
                        element.RaisePointerClick(args);
                        break;
                }

                if (args.Handled)
                {
                    return;
                }
            }
        }

        private void UpdateHover(VisualElement hit, float x, float y)
        {
            if (hit == _hovered)
            {
                return;
            }

            VisualElement left = _hovered;

            _leftChain.Clear();
            _enteredChain.Clear();

            for (VisualElement element = left; element != null; element = element.Parent)
            {
                _leftChain.Add(element);
            }

            for (VisualElement element = hit; element != null; element = element.Parent)
            {
                _enteredChain.Add(element);
            }

            int shared = 0;

            while (shared < _leftChain.Count
                && shared < _enteredChain.Count
                && _leftChain[_leftChain.Count - 1 - shared] == _enteredChain[_enteredChain.Count - 1 - shared])
            {
                shared++;
            }

            _hovered = hit;

            if (_leftChain.Count > shared)
            {
                var leaveArgs = new PointerEventArgs(x, y, PointerButton.None, left);

                for (int i = 0; i < _leftChain.Count - shared; i++)
                {
                    SetState(_leftChain[i], HOVER_STATE, false);
                    _leftChain[i].RaisePointerLeave(leaveArgs);
                }
            }

            if (_enteredChain.Count > shared)
            {
                var enterArgs = new PointerEventArgs(x, y, PointerButton.None, hit);

                for (int i = _enteredChain.Count - shared - 1; i >= 0; i--)
                {
                    SetState(_enteredChain[i], HOVER_STATE, true);
                    _enteredChain[i].RaisePointerEnter(enterArgs);
                }
            }
        }
    }
}
