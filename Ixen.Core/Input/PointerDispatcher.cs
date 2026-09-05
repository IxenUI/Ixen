using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles;
using System;
using System.Collections.Generic;

namespace Ixen.Core.Input
{
    internal class PointerDispatcher
    {

        private const float WHEEL_STEP = ScrollNavigator.STEP;
        private const float DRAG_THRESHOLD = 4f;
        private const float DOUBLE_CLICK_DISTANCE = 4f;
        private const long DOUBLE_CLICK_DELAY = 500;
        private const int LONG_PRESS_DELAY = 500;
        private const long WHEEL_LATCH_DELAY = 150;

        private const int FLING_TICK = 16;
        private const float FLING_MIN_VELOCITY = 0.15f;
        private const float FLING_STOP_VELOCITY = 0.02f;
        private const float FLING_FRICTION = 0.94f;
        private const float FLING_MAX_VELOCITY = 4f;
        private const float OVERSCROLL_LIMIT = 0.4f;
        private const float OVERSCROLL_FRICTION = 0.5f;
        private const float OVERSCROLL_RETURN = 0.82f;
        private const float OVERSCROLL_MIN = 0.5f;
        private const float VELOCITY_WEIGHT = 0.6f;

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
        private object _dragData;
        private VisualElement _dropTarget;
        private bool _dropAccepted;
        private bool _dropped;
        private float _lastX;
        private float _lastY;
        private bool _inside;
        private VisualElement _panning;
        private VisualElement _flinging;
        private IDisposable _fling;
        private float _velocityX;
        private float _velocityY;
        private float _overscrollX;
        private float _overscrollY;
        private bool _panHorizontal;
        private long _panTime;

        private VisualElement _scrollLatch;
        private long _lastWheelTime;

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

            _lastX = x;
            _lastY = y;
            _inside = true;

            if (_panning != null)
            {
                Pan(x, y);
                return;
            }

            VisualElement hit = Enabled(HitTester.HitTest(root, x, y));

            if (_captured != null)
            {
                UpdateHover(CaptureHoverTarget(hit), x, y);
                Bubble(_captured, new PointerEventArgs(x, y, PointerButton.None, _captured), PointerEventKind.Move);
                UpdateDrag(hit, x, y);
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

        private void Scroll(VisualElement hit, float deltaX, float deltaY, KeyModifiers modifiers)
        {
            bool sideways = (modifiers & KeyModifiers.Shift) == KeyModifiers.Shift;

            float offsetX = (deltaX - (sideways ? deltaY : 0)) * WHEEL_STEP;
            float offsetY = (sideways ? 0 : -deltaY) * WHEEL_STEP;

            long now = TimeSource.Milliseconds;
            VisualElement target = Latched(now) ?? ScrollNavigator.Find(hit, offsetX, offsetY);

            _lastWheelTime = now;

            if (target == null)
            {
                return;
            }

            _scrollLatch = target;

            target.ScrollBy(offsetX, offsetY);
        }

        private VisualElement Latched(long now)
        {
            if (_scrollLatch == null)
            {
                return null;
            }

            if (!_scrollLatch.Scrollable || now - _lastWheelTime > WHEEL_LATCH_DELAY)
            {
                _scrollLatch = null;
            }

            return _scrollLatch;
        }

        internal void Refresh(VisualElement root, bool trackStates)
        {
            if (!_inside || _panning != null)
            {
                return;
            }

            _trackStates = trackStates;

            VisualElement hit = Enabled(HitTester.HitTest(root, _lastX, _lastY));

            UpdateHover(_captured != null ? CaptureHoverTarget(hit) : hit, _lastX, _lastY);
        }

        internal void LeaveSurface(bool trackStates)
        {
            if (_captured != null)
            {
                return;
            }

            _trackStates = trackStates;
            _inside = false;

            UpdateHover(null, 0, 0);
            _pressed = null;
        }

        internal void ReleaseCapture()
        {
            _captured = null;
            _panning = null;
            StopFling();
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

            if (_dropTarget == element)
            {
                SetState(element, StyleStates.DRAG_OVER, false);
                _dropTarget = null;
                _dropAccepted = false;
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

            if (_flinging == element)
            {
                StopFling();
            }

            if (_scrollLatch == element)
            {
                _scrollLatch = null;
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

            _lastX = x;
            _lastY = y;
            _inside = true;

            StopFling();

            if (_captured != null)
            {
                Bubble(_captured, new PointerEventArgs(x, y, button, _captured), PointerEventKind.Down);
                return;
            }

            _panning = null;

            VisualElement hit = Enabled(HitTester.HitTest(root, x, y));

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

        private void UpdateDrag(VisualElement hit, float x, float y)
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

                bool claimed = RaiseDrag(x, y, PointerEventKind.DragStart);

                if (!claimed && _dragData == null)
                {
                    TryStartPan(x, y);
                }

                SyncDropTarget(hit, x, y);

                return;
            }

            RaiseDrag(x, y, PointerEventKind.Drag);
            SyncDropTarget(hit, x, y);
        }

        private void EndDrag(float x, float y)
        {
            if (!_dragging)
            {
                return;
            }

            _dragging = false;

            LeaveDropTarget(x, y);
            RaiseDrag(x, y, PointerEventKind.DragEnd);

            _dragData = null;
            _dropped = false;
        }

        private bool RaiseDrag(float x, float y, PointerEventKind kind)
        {
            var args = new DragEventArgs(x, y, _pressedButton, _pressed,
                x - _lastDragX, y - _lastDragY, x - _pressX, y - _pressY, _kind)
            {
                Data = _dragData
            };

            if (kind == PointerEventKind.DragEnd)
            {
                args.Accepted = _dropped;
            }

            _lastDragX = x;
            _lastDragY = y;

            Bubble(_pressed, args, kind);

            if (kind == PointerEventKind.DragStart)
            {
                _dragData = args.Data;
            }

            return args.Handled;
        }

        private void SyncDropTarget(VisualElement hit, float x, float y)
        {
            if (_dragData == null)
            {
                return;
            }

            VisualElement target = DropTarget(hit);

            if (target != _dropTarget)
            {
                LeaveDropTarget(x, y);

                _dropTarget = target;

                if (target == null)
                {
                    return;
                }

                DragEventArgs entered = DropArgs(x, y, target, true);

                target.RaiseDragEnter(entered);

                _dropAccepted = entered.Accepted;
                SetState(target, StyleStates.DRAG_OVER, _dropAccepted);

                return;
            }

            if (target == null)
            {
                return;
            }

            DragEventArgs moved = DropArgs(x, y, target, _dropAccepted);

            target.RaiseDragOver(moved);

            if (moved.Accepted != _dropAccepted)
            {
                _dropAccepted = moved.Accepted;
                SetState(target, StyleStates.DRAG_OVER, _dropAccepted);
            }
        }

        private void LeaveDropTarget(float x, float y)
        {
            if (_dropTarget == null)
            {
                return;
            }

            VisualElement target = _dropTarget;

            _dropTarget = null;

            SetState(target, StyleStates.DRAG_OVER, false);
            target.RaiseDragLeave(DropArgs(x, y, target, _dropAccepted));

            _dropAccepted = false;
        }

        private void PerformDrop(float x, float y)
        {
            if (_dropTarget == null)
            {
                return;
            }

            if (!_dropAccepted)
            {
                LeaveDropTarget(x, y);
                return;
            }

            VisualElement target = _dropTarget;

            _dropTarget = null;
            _dropped = true;

            SetState(target, StyleStates.DRAG_OVER, false);
            target.RaiseDrop(DropArgs(x, y, target, true));
        }

        private static VisualElement DropTarget(VisualElement hit)
        {
            for (VisualElement element = hit; element != null; element = element.Parent)
            {
                if (element.AllowDrop)
                {
                    return element;
                }
            }

            return null;
        }

        private DragEventArgs DropArgs(float x, float y, VisualElement target, bool accepted)
            => new DragEventArgs(x, y, _pressedButton, target, _pressed, _dragData, accepted, _kind);

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

            _velocityX = 0f;
            _velocityY = 0f;
            _overscrollX = 0f;
            _overscrollY = 0f;
            _panHorizontal = ScrollNavigator.Horizontal(offsetX, offsetY);
            _panTime = TimeSource.Milliseconds;

            Pan(x, y);

            return true;
        }

        private static VisualElement PanTarget(VisualElement from, float offsetX, float offsetY)
            => ScrollNavigator.Find(from, offsetX, offsetY)
                ?? ScrollNavigator.Bouncer(from, offsetX, offsetY);

        private void Pan(float x, float y)
        {
            float offsetX = _lastDragX - x;
            float offsetY = _lastDragY - y;

            Push(_panning, offsetX, offsetY);

            TrackVelocity(offsetX, offsetY);

            _lastDragX = x;
            _lastDragY = y;
        }

        private void TrackVelocity(float offsetX, float offsetY)
        {
            long now = TimeSource.Milliseconds;
            long elapsed = now - _panTime;

            _panTime = now;

            if (elapsed <= 0)
            {
                return;
            }

            _velocityX = Blend(_velocityX, offsetX / elapsed);
            _velocityY = Blend(_velocityY, offsetY / elapsed);
        }

        private static float Blend(float carried, float sample)
        {
            float blended = carried * (1f - VELOCITY_WEIGHT) + sample * VELOCITY_WEIGHT;

            if (blended > FLING_MAX_VELOCITY)
            {
                return FLING_MAX_VELOCITY;
            }

            return blended < -FLING_MAX_VELOCITY ? -FLING_MAX_VELOCITY : blended;
        }

        private bool Overscrolled => _overscrollX != 0f || _overscrollY != 0f;

        private void Push(VisualElement target, float offsetX, float offsetY)
        {
            bool bounces = ScrollNavigator.CanBounce(target, _panHorizontal);

            float scrollX = Absorb(ref _overscrollX, offsetX, target.ScrollX, target.MaxScrollX,
                bounces && _panHorizontal);

            float scrollY = Absorb(ref _overscrollY, offsetY, target.ScrollY, target.MaxScrollY,
                bounces && !_panHorizontal);

            target.ScrollBy(scrollX, scrollY);

            ApplyOverscroll(target);
        }

        private static float Absorb(ref float raw, float offset, float scroll, float max, bool bounces)
        {
            offset += raw;
            raw = 0f;

            float take;

            if (offset > 0f)
            {
                float room = max - scroll;
                take = offset < room ? offset : room;
            }
            else
            {
                float room = -scroll;
                take = offset > room ? offset : room;
            }

            if (bounces)
            {
                raw = offset - take;
            }

            return take;
        }

        private void ApplyOverscroll(VisualElement target)
            => target.SetOverscroll(Rubber(_overscrollX, target.ContentWidth),
                Rubber(_overscrollY, target.ContentHeight));

        private static float Rubber(float distance, float dimension)
        {
            if (distance == 0f || dimension <= 0f)
            {
                return 0f;
            }

            float limit = dimension * OVERSCROLL_LIMIT;
            float magnitude = distance < 0f ? -distance : distance;
            float mapped = magnitude / (magnitude + limit) * limit;

            return distance < 0f ? -mapped : mapped;
        }

        private static float Recoil(float raw)
        {
            float next = raw * OVERSCROLL_RETURN;

            return next < OVERSCROLL_MIN && next > -OVERSCROLL_MIN ? 0f : next;
        }

        private void StartFling(VisualElement target)
        {
            if (target == null)
            {
                return;
            }

            bool flinging = Math.Abs(_velocityX) >= FLING_MIN_VELOCITY
                || Math.Abs(_velocityY) >= FLING_MIN_VELOCITY;

            if (Scheduler == null || (!flinging && !Overscrolled))
            {
                _overscrollX = 0f;
                _overscrollY = 0f;
                target.SetOverscroll(0f, 0f);

                return;
            }

            if (!flinging)
            {
                _velocityX = 0f;
                _velocityY = 0f;
            }

            _flinging = target;
            _fling = Scheduler.Schedule(FLING_TICK, true, AdvanceFling);
        }

        private void AdvanceFling()
        {
            VisualElement target = _flinging;

            if (target == null)
            {
                StopFling();
                return;
            }

            float offsetX = _velocityX * FLING_TICK;
            float offsetY = _velocityY * FLING_TICK;

            if (Math.Abs(_velocityX) >= FLING_STOP_VELOCITY
                || Math.Abs(_velocityY) >= FLING_STOP_VELOCITY)
            {
                if (!ScrollNavigator.CanBounce(target, _panHorizontal)
                    && !ScrollNavigator.CanScroll(target, offsetX, offsetY))
                {
                    StopFling();
                    return;
                }

                Push(target, offsetX, offsetY);

                float friction = Overscrolled ? OVERSCROLL_FRICTION : FLING_FRICTION;

                _velocityX *= friction;
                _velocityY *= friction;

                return;
            }

            _velocityX = 0f;
            _velocityY = 0f;

            if (!Overscrolled)
            {
                StopFling();
                return;
            }

            _overscrollX = Recoil(_overscrollX);
            _overscrollY = Recoil(_overscrollY);

            ApplyOverscroll(target);
        }

        internal void StopFling()
        {
            VisualElement target = _flinging;

            if (target != null)
            {
                _overscrollX = 0f;
                _overscrollY = 0f;
                target.SetOverscroll(0f, 0f);
            }

            _flinging = null;

            if (_fling == null)
            {
                return;
            }

            IDisposable running = _fling;
            _fling = null;
            running.Dispose();
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

            _lastX = x;
            _lastY = y;
            _inside = true;

            if (_captured != null && button != _pressedButton)
            {
                Bubble(_captured, new PointerEventArgs(x, y, button, _captured), PointerEventKind.Up);
                return;
            }

            if (_panning != null)
            {
                Pan(x, y);
                StartFling(_panning);
                _panning = null;

                return;
            }

            VisualElement hit = Enabled(HitTester.HitTest(root, x, y));
            VisualElement target = _captured ?? hit;

            _captured = null;

            SetState(_pressed, StyleStates.PRESSED, false);

            UpdateHover(hit, x, y);
            Bubble(target, new PointerEventArgs(x, y, button, target), PointerEventKind.Up);

            CancelLongPress();
            PerformDrop(x, y);
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

        internal static void Invoke(VisualElement element)
        {
            var args = new PointerEventArgs(
                element.X + element.ActualWidth / 2,
                element.Y + element.ActualHeight / 2,
                PointerButton.Left,
                element);

            Bubble(element, args, PointerEventKind.Click);
        }

        private static VisualElement Enabled(VisualElement hit)
            => hit == null || hit.IsEnabled ? hit : null;

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
