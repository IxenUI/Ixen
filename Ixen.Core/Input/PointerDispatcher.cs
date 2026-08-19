using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles;
using System;
using System.Collections.Generic;

namespace Ixen.Core.Input
{
    internal class PointerDispatcher
    {

        private const float WHEEL_STEP = 48f;
        private const float DRAG_THRESHOLD = 4f;
        private const float DOUBLE_CLICK_DISTANCE = 4f;
        private const long DOUBLE_CLICK_DELAY = 500;
        private const int LONG_PRESS_DELAY = 500;

        private readonly List<VisualElement> _leftChain = new();
        private readonly List<VisualElement> _enteredChain = new();

        private bool _trackStates;

        private VisualElement _hovered;
        private VisualElement _pressed;
        private VisualElement _captured;

        private PointerButton _pressedButton;
        private PointerKind _kind;
        private float _pressX;
        private float _pressY;
        private float _lastDragX;
        private float _lastDragY;
        private bool _dragging;
        private VisualElement _panning;

        private VisualElement _lastClicked;
        private long _lastClickTime;
        private float _lastClickX;
        private float _lastClickY;

        private IDisposable _longPress;
        private bool _longPressHandled;

        internal ITimeSource TimeSource { get; set; } = SystemTimeSource.Instance;
        internal IScheduler Scheduler { get; set; }

        internal VisualElement Hovered => _hovered;
        internal VisualElement Pressed => _pressed;
        internal VisualElement Captured => _captured;

        internal void Move(VisualElement root, float x, float y, bool trackStates,
            PointerKind kind = PointerKind.Mouse)
        {
            _trackStates = trackStates;
            _kind = kind;

            if (_panning != null)
            {
                Pan(x, y);
                return;
            }

            VisualElement hit = HitTester.HitTest(root, x, y);

            if (_captured != null)
            {
                UpdateHover(CaptureHoverTarget(hit), x, y);
                Bubble(_captured, new PointerEventArgs(x, y, PointerButton.None, _captured), PointerEventKind.Move);
                UpdateDrag(x, y);
                return;
            }

            UpdateHover(hit, x, y);
            Bubble(hit, new PointerEventArgs(x, y, PointerButton.None, hit), PointerEventKind.Move);
        }

        internal void Wheel(VisualElement root, float x, float y, float deltaX, float deltaY,
            KeyModifiers modifiers)
        {
            VisualElement hit = _captured ?? HitTester.HitTest(root, x, y);

            if (hit == null)
            {
                return;
            }

            var args = new WheelEventArgs(x, y, deltaX, deltaY, modifiers, hit);

            for (VisualElement element = hit; element != null; element = element.Parent)
            {
                element.RaisePointerWheel(args);

                if (args.Handled)
                {
                    return;
                }
            }

            Scroll(hit, deltaX, deltaY, modifiers);
        }

        private static void Scroll(VisualElement hit, float deltaX, float deltaY, KeyModifiers modifiers)
        {
            bool sideways = (modifiers & KeyModifiers.Shift) == KeyModifiers.Shift;

            float offsetX = (deltaX - (sideways ? deltaY : 0)) * WHEEL_STEP;
            float offsetY = (sideways ? 0 : -deltaY) * WHEEL_STEP;

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
            _panning = null;
            CancelLongPress();
            EndDrag(_lastDragX, _lastDragY);
            SetState(_pressed, StyleStates.PRESSED, false);
            _pressed = null;
        }

        internal void ElementDetached(VisualElement element)
        {
            if (element == null)
            {
                return;
            }

            if (_hovered == element)
            {
                SetState(element, StyleStates.HOVER, false);
                _hovered = null;
            }

            if (_pressed == element)
            {
                SetState(element, StyleStates.PRESSED, false);
                _pressed = null;
                CancelLongPress();
            }

            if (_captured == element)
            {
                _captured = null;
                EndDrag(_lastDragX, _lastDragY);
            }

            if (_panning == element)
            {
                _panning = null;
            }

            if (_lastClicked == element)
            {
                _lastClicked = null;
            }
        }

        internal void Down(VisualElement root, float x, float y, PointerButton button, bool trackStates,
            PointerKind kind = PointerKind.Mouse)
        {
            _trackStates = trackStates;
            _kind = kind;
            _panning = null;

            VisualElement hit = HitTester.HitTest(root, x, y);

            UpdateHover(hit, x, y);

            _pressed = hit;
            _captured = hit;

            _pressedButton = button;
            _pressX = x;
            _pressY = y;
            _lastDragX = x;
            _lastDragY = y;
            _dragging = false;

            SetState(hit, StyleStates.PRESSED, true);

            Bubble(hit, new PointerEventArgs(x, y, button, hit), PointerEventKind.Down);

            StartLongPress(x, y, button);
        }

        private void StartLongPress(float x, float y, PointerButton button)
        {
            _longPressHandled = false;

            if (Scheduler == null || _pressed == null)
            {
                return;
            }

            VisualElement pressed = _pressed;

            _longPress = Scheduler.Schedule(LONG_PRESS_DELAY, false, () =>
            {
                _longPress = null;

                if (_pressed != pressed)
                {
                    return;
                }

                var args = new PointerEventArgs(x, y, button, pressed);
                Bubble(pressed, args, PointerEventKind.LongPress);
                _longPressHandled = args.Handled;
            });
        }

        private void CancelLongPress()
        {
            _longPress?.Dispose();
            _longPress = null;
        }

        private void UpdateDrag(float x, float y)
        {
            if (_pressed == null)
            {
                return;
            }

            if (!_dragging)
            {
                if (Math.Abs(x - _pressX) < DRAG_THRESHOLD && Math.Abs(y - _pressY) < DRAG_THRESHOLD)
                {
                    return;
                }

                _dragging = true;
                CancelLongPress();

                if (!RaiseDrag(x, y, PointerEventKind.DragStart))
                {
                    TryStartPan(x, y);
                }

                return;
            }

            RaiseDrag(x, y, PointerEventKind.Drag);
        }

        private void EndDrag(float x, float y)
        {
            if (!_dragging)
            {
                return;
            }

            _dragging = false;
            RaiseDrag(x, y, PointerEventKind.DragEnd);
        }

        private bool RaiseDrag(float x, float y, PointerEventKind kind)
        {
            var args = new DragEventArgs(x, y, _pressedButton, _pressed,
                x - _lastDragX, y - _lastDragY, x - _pressX, y - _pressY, _kind);

            _lastDragX = x;
            _lastDragY = y;

            Bubble(_pressed, args, kind);

            return args.Handled;
        }

        private bool TryStartPan(float x, float y)
        {
            if (_kind != PointerKind.Touch)
            {
                return false;
            }

            float offsetX = _pressX - x;
            float offsetY = _pressY - y;

            VisualElement target = PanTarget(_pressed, offsetX, offsetY);

            if (target == null)
            {
                return false;
            }

            _panning = target;
            _dragging = false;

            _captured = null;
            SetState(_pressed, StyleStates.PRESSED, false);
            _pressed = null;

            _lastDragX = _pressX;
            _lastDragY = _pressY;

            Pan(x, y);

            return true;
        }

        private static VisualElement PanTarget(VisualElement from, float offsetX, float offsetY)
        {
            for (VisualElement element = from; element != null; element = element.Parent)
            {
                if (element.Scrollable && CanScroll(element, offsetX, offsetY))
                {
                    return element;
                }
            }

            return null;
        }

        private void Pan(float x, float y)
        {
            _panning.ScrollBy(_lastDragX - x, _lastDragY - y);

            _lastDragX = x;
            _lastDragY = y;
        }

        private bool IsDoubleClick(VisualElement hit, float x, float y)
        {
            long now = TimeSource.Milliseconds;

            bool doubled = hit == _lastClicked
                && now - _lastClickTime <= DOUBLE_CLICK_DELAY
                && Math.Abs(x - _lastClickX) <= DOUBLE_CLICK_DISTANCE
                && Math.Abs(y - _lastClickY) <= DOUBLE_CLICK_DISTANCE;

            _lastClicked = doubled ? null : hit;
            _lastClickTime = now;
            _lastClickX = x;
            _lastClickY = y;

            return doubled;
        }

        internal void Up(VisualElement root, float x, float y, PointerButton button, bool trackStates,
            PointerKind kind = PointerKind.Mouse)
        {
            _trackStates = trackStates;
            _kind = kind;

            if (_panning != null)
            {
                Pan(x, y);
                _panning = null;

                return;
            }

            VisualElement hit = HitTester.HitTest(root, x, y);
            VisualElement target = _captured ?? hit;

            _captured = null;

            SetState(_pressed, StyleStates.PRESSED, false);

            UpdateHover(hit, x, y);
            Bubble(target, new PointerEventArgs(x, y, button, target), PointerEventKind.Up);

            CancelLongPress();
            EndDrag(x, y);

            bool isClick = hit != null && hit == _pressed && !_longPressHandled;

            _pressed = null;

            if (!isClick)
            {
                return;
            }

            Bubble(hit, new PointerEventArgs(x, y, button, hit), PointerEventKind.Click);

            if (IsDoubleClick(hit, x, y))
            {
                Bubble(hit, new PointerEventArgs(x, y, button, hit), PointerEventKind.DoubleClick);
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

                    case PointerEventKind.DoubleClick:
                        element.RaisePointerDoubleClick(args);
                        break;

                    case PointerEventKind.LongPress:
                        element.RaisePointerLongPress(args);
                        break;

                    case PointerEventKind.DragStart:
                        element.RaisePointerDragStart((DragEventArgs)args);
                        break;

                    case PointerEventKind.Drag:
                        element.RaisePointerDrag((DragEventArgs)args);
                        break;

                    case PointerEventKind.DragEnd:
                        element.RaisePointerDragEnd((DragEventArgs)args);
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
                    SetState(_leftChain[i], StyleStates.HOVER, false);
                    _leftChain[i].RaisePointerLeave(leaveArgs);
                }
            }

            if (_enteredChain.Count > shared)
            {
                var enterArgs = new PointerEventArgs(x, y, PointerButton.None, hit);

                for (int i = _enteredChain.Count - shared - 1; i >= 0; i--)
                {
                    SetState(_enteredChain[i], StyleStates.HOVER, true);
                    _enteredChain[i].RaisePointerEnter(enterArgs);
                }
            }
        }
    }
}
