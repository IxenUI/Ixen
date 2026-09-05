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
        private const float TOUCH_DRAG_THRESHOLD = 8f;
        private const float DOUBLE_CLICK_DISTANCE = 4f;
        private const float TOUCH_DOUBLE_CLICK_DISTANCE = 32f;
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

        private const int NO_POINTER = -1;
        private const float PINCH_MIN_SPAN = 1f;

        private readonly List<VisualElement> _leftChain = new();
        private readonly List<VisualElement> _enteredChain = new();
        private readonly List<ActivePointer> _pointers = new();

        private int _primary = NO_POINTER;

        private VisualElement _pinchTarget;
        private bool _pinching;
        private bool _pinchRefused;
        private float _baseSpan;
        private float _baseCentroidX;
        private float _baseCentroidY;
        private float _lastAngle;
        private float _pinchScale = 1f;
        private float _pinchRotation;
        private float _pinchTotalX;
        private float _pinchTotalY;
        private float _pinchX;
        private float _pinchY;

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

        internal int PointerCount => _pointers.Count;
        internal VisualElement PinchTarget => _pinching ? _pinchTarget : null;

        private class ActivePointer
        {
            internal int Id;
            internal float X;
            internal float Y;
            internal float BaseX;
            internal float BaseY;
            internal VisualElement Down;
        }

        private ActivePointer Find(int id)
        {
            for (int index = 0; index < _pointers.Count; index++)
            {
                if (_pointers[index].Id == id)
                {
                    return _pointers[index];
                }
            }

            return null;
        }

        private void AddPointer(VisualElement root, int id, float x, float y, VisualElement hit)
        {
            if (Find(id) != null)
            {
                return;
            }

            Fold();

            _pointers.Add(new ActivePointer { Id = id, X = x, Y = y, Down = hit });

            Rebase();
        }

        private void RemovePointer(int id)
        {
            ActivePointer pointer = Find(id);

            if (pointer == null)
            {
                return;
            }

            Fold();

            _pointers.Remove(pointer);

            if (_pointers.Count < 2)
            {
                EndPinch();
            }

            if (_pointers.Count > 0)
            {
                Rebase();
            }
        }

        private void ClearPointers()
        {
            Fold();
            EndPinch();

            _pointers.Clear();
            _primary = NO_POINTER;
        }

        private void Fold()
        {
            if (!_pinching)
            {
                return;
            }

            _pinchScale = CurrentScale();
            _pinchTotalX = CurrentTotalX();
            _pinchTotalY = CurrentTotalY();
        }

        private void Rebase()
        {
            _baseSpan = Span();
            _baseCentroidX = CentroidX();
            _baseCentroidY = CentroidY();
            _lastAngle = Angle();

            for (int index = 0; index < _pointers.Count; index++)
            {
                _pointers[index].BaseX = _pointers[index].X;
                _pointers[index].BaseY = _pointers[index].Y;
            }

            SyncPan();
        }

        private void SyncPan()
        {
            if (_panning == null || _pointers.Count == 0)
            {
                return;
            }

            _lastDragX = CentroidX();
            _lastDragY = CentroidY();
        }

        private float CentroidX()
        {
            float sum = 0f;

            for (int index = 0; index < _pointers.Count; index++)
            {
                sum += _pointers[index].X;
            }

            return sum / _pointers.Count;
        }

        private float CentroidY()
        {
            float sum = 0f;

            for (int index = 0; index < _pointers.Count; index++)
            {
                sum += _pointers[index].Y;
            }

            return sum / _pointers.Count;
        }

        private float Span()
        {
            if (_pointers.Count < 2)
            {
                return 0f;
            }

            float centroidX = CentroidX();
            float centroidY = CentroidY();
            float sum = 0f;

            for (int index = 0; index < _pointers.Count; index++)
            {
                float offsetX = _pointers[index].X - centroidX;
                float offsetY = _pointers[index].Y - centroidY;

                sum += (float)Math.Sqrt(offsetX * offsetX + offsetY * offsetY);
            }

            return sum / _pointers.Count * 2f;
        }

        private float Angle()
        {
            if (_pointers.Count < 2)
            {
                return 0f;
            }

            ActivePointer first = _pointers[0];
            ActivePointer second = _pointers[1];

            return (float)(Math.Atan2(second.Y - first.Y, second.X - first.X) * 180 / Math.PI);
        }

        private static float Turn(float degrees)
        {
            while (degrees > 180f)
            {
                degrees -= 360f;
            }

            while (degrees <= -180f)
            {
                degrees += 360f;
            }

            return degrees;
        }

        private float CurrentScale()
            => _baseSpan < PINCH_MIN_SPAN ? _pinchScale : _pinchScale * (Span() / _baseSpan);

        private float DragThreshold
            => _kind == PointerKind.Touch ? TOUCH_DRAG_THRESHOLD : DRAG_THRESHOLD;

        private float PanX(float x) => _pointers.Count > 1 ? CentroidX() : x;

        private float PanY(float y) => _pointers.Count > 1 ? CentroidY() : y;

        private float CurrentTotalX() => _pinchTotalX + CentroidX() - _baseCentroidX;

        private float CurrentTotalY() => _pinchTotalY + CentroidY() - _baseCentroidY;

        private bool MovedPastThreshold()
        {
            for (int index = 0; index < _pointers.Count; index++)
            {
                ActivePointer pointer = _pointers[index];

                if (Math.Abs(pointer.X - pointer.BaseX) >= DragThreshold
                    || Math.Abs(pointer.Y - pointer.BaseY) >= DragThreshold)
                {
                    return true;
                }
            }

            return false;
        }

        private VisualElement CommonDownElement()
        {
            VisualElement common = _pointers[0].Down;

            for (int index = 1; index < _pointers.Count && common != null; index++)
            {
                common = CommonAncestor(common, _pointers[index].Down);
            }

            return common;
        }

        private static VisualElement CommonAncestor(VisualElement first, VisualElement second)
        {
            for (VisualElement mine = first; mine != null; mine = mine.Parent)
            {
                for (VisualElement theirs = second; theirs != null; theirs = theirs.Parent)
                {
                    if (mine == theirs)
                    {
                        return mine;
                    }
                }
            }

            return null;
        }

        private void UpdatePinch()
        {
            if (_pinchRefused)
            {
                return;
            }

            float angle = Angle();

            _pinchRotation += Turn(angle - _lastAngle);
            _lastAngle = angle;

            if (_pinching)
            {
                RaisePinch(PointerEventKind.Pinch);
                return;
            }

            if (!MovedPastThreshold())
            {
                return;
            }

            _pinchTarget = CommonDownElement();

            if (_pinchTarget == null)
            {
                _pinchRefused = true;
                return;
            }

            _pinching = true;
            _pinchX = CentroidX();
            _pinchY = CentroidY();

            if (RaisePinch(PointerEventKind.PinchStart).Handled)
            {
                ClaimPinch();
                return;
            }

            _pinching = false;
            _pinchRefused = true;
            _pinchTarget = null;
        }

        private PinchEventArgs RaisePinch(PointerEventKind kind)
        {
            float centroidX = CentroidX();
            float centroidY = CentroidY();

            var args = new PinchEventArgs(centroidX, centroidY, _pinchTarget, CurrentScale(),
                _pinchRotation, centroidX - _pinchX, centroidY - _pinchY,
                CurrentTotalX(), CurrentTotalY(), _pointers.Count, _kind);

            _pinchX = centroidX;
            _pinchY = centroidY;

            Bubble(_pinchTarget, args, kind);

            return args;
        }

        private void ClaimPinch()
        {
            CancelLongPress();
            EndDrag(_lastDragX, _lastDragY);

            SetState(_pressed, StyleStates.PRESSED, false);

            _pressed = null;
            _captured = null;

            if (_panning == null)
            {
                return;
            }

            VisualElement panning = _panning;

            _panning = null;
            _velocityX = 0f;
            _velocityY = 0f;

            StartFling(panning);
        }

        private void EndPinch()
        {
            if (_pinching)
            {
                var args = new PinchEventArgs(_pinchX, _pinchY, _pinchTarget, _pinchScale,
                    _pinchRotation, 0f, 0f, _pinchTotalX, _pinchTotalY, _pointers.Count, _kind);

                Bubble(_pinchTarget, args, PointerEventKind.PinchEnd);
            }

            _pinching = false;
            _pinchRefused = false;
            _pinchTarget = null;
            _pinchScale = 1f;
            _pinchRotation = 0f;
            _pinchTotalX = 0f;
            _pinchTotalY = 0f;
        }

        internal void Move(VisualElement root, float x, float y, bool trackStates,
            PointerKind kind = PointerKind.Mouse, int pointerId = 0)
        {
            ActivePointer pointer = Find(pointerId);

            if (pointer != null)
            {
                pointer.X = x;
                pointer.Y = y;
            }

            if (_pointers.Count > 1)
            {
                UpdatePinch();
            }

            if (_pointers.Count > 0 && pointerId != _primary)
            {
                if (_panning != null)
                {
                    Pan(CentroidX(), CentroidY());
                }

                return;
            }

            _trackStates = trackStates;
            _kind = kind;

            _lastX = x;
            _lastY = y;
            _inside = true;

            if (_panning != null)
            {
                Pan(PanX(x), PanY(y));
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

            ClearPointers();
            UpdateHover(null, 0, 0);
            _pressed = null;
        }

        internal void ReleaseCapture()
        {
            ClearPointers();

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

            if (_pinchTarget == element)
            {
                _pinching = false;
                _pinchTarget = null;
            }

            for (int index = 0; index < _pointers.Count; index++)
            {
                if (_pointers[index].Down == element)
                {
                    _pointers[index].Down = null;
                }
            }
        }

        internal void Down(VisualElement root, float x, float y, PointerButton button, bool trackStates,
            PointerKind kind = PointerKind.Mouse, int pointerId = 0)
        {
            if (_pointers.Count > 0 && Find(pointerId) == null)
            {
                AddPointer(root, pointerId, x, y, Enabled(HitTester.HitTest(root, x, y)));
                return;
            }

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

            _primary = pointerId;
            AddPointer(root, pointerId, x, y, hit);

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
                if (Math.Abs(x - _pressX) < DragThreshold && Math.Abs(y - _pressY) < DragThreshold)
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

            float centroidX = PanX(x);
            float centroidY = PanY(y);

            float offsetX = _baseCentroidX - centroidX;
            float offsetY = _baseCentroidY - centroidY;

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

            _lastDragX = _baseCentroidX;
            _lastDragY = _baseCentroidY;

            _velocityX = 0f;
            _velocityY = 0f;
            _overscrollX = 0f;
            _overscrollY = 0f;
            _panHorizontal = ScrollNavigator.Horizontal(offsetX, offsetY);
            _panTime = TimeSource.Milliseconds;

            Pan(centroidX, centroidY);

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

            float distance = _kind == PointerKind.Touch
                ? TOUCH_DOUBLE_CLICK_DISTANCE
                : DOUBLE_CLICK_DISTANCE;

            bool doubled = hit == _lastClicked
                && now - _lastClickTime <= DOUBLE_CLICK_DELAY
                && Math.Abs(x - _lastClickX) <= distance
                && Math.Abs(y - _lastClickY) <= distance;

            _lastClicked = doubled ? null : hit;
            _lastClickTime = now;
            _lastClickX = x;
            _lastClickY = y;

            return doubled;
        }

        internal void Up(VisualElement root, float x, float y, PointerButton button, bool trackStates,
            PointerKind kind = PointerKind.Mouse, int pointerId = 0)
        {
            ActivePointer pointer = Find(pointerId);

            if (pointer != null)
            {
                pointer.X = x;
                pointer.Y = y;
            }

            if (_pointers.Count > 0 && pointerId != _primary)
            {
                RemovePointer(pointerId);
                return;
            }

            bool visitor = _captured != null && button != _pressedButton;

            UpPrimary(root, x, y, button, trackStates, kind);

            if (visitor)
            {
                return;
            }

            _primary = NO_POINTER;
            RemovePointer(pointerId);
        }

        private void UpPrimary(VisualElement root, float x, float y, PointerButton button,
            bool trackStates, PointerKind kind)
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
                Pan(PanX(x), PanY(y));
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

                    case PointerEventKind.PinchStart:
                        element.RaisePointerPinchStart((PinchEventArgs)args);
                        break;

                    case PointerEventKind.Pinch:
                        element.RaisePointerPinch((PinchEventArgs)args);
                        break;

                    case PointerEventKind.PinchEnd:
                        element.RaisePointerPinchEnd((PinchEventArgs)args);
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
