using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Ixen.Controls
{
    public class Splitter : VisualElement, IAccessibleRange
    {
        public const string LINE = "SplitterLine";
        public const string VERTICAL = "vertical";
        public const string HORIZONTAL = "horizontal";
        public const string DRAGGING = "dragging";

        public event EventHandler<EventArgs> SizeChanged;

        private readonly VisualElement _line;

        private float _size = float.NaN;
        private float _initial = float.NaN;
        private float _minimum;
        private float _maximum = float.PositiveInfinity;
        private float _step = 10;
        private bool _vertical;
        private bool _resizesNext;

        private VisualElement _target;
        private float _applied = float.NaN;
        private bool _appliedVertical;

        private bool _dragging;
        private float _dragFrom;

        public Splitter()
        {
            TypeName = nameof(Splitter);
            Focusable = true;
            Role = AccessibleRole.Slider;

            _line = new VisualElement { TypeName = LINE, Role = AccessibleRole.Presentation };

            AddChild(_line);

            Orient();

            PointerDown += OnPointerDown;
            PointerDragStart += OnDragStart;
            PointerDrag += OnDrag;
            PointerDragEnd += OnDragEnd;
            PointerDoubleClick += OnDoubleClick;
            KeyDown += OnKeyDown;
        }

        public bool Vertical
        {
            get => _vertical;
            set
            {
                if (_vertical == value)
                {
                    return;
                }

                _vertical = value;

                Orient();
                Reapply();
            }
        }

        public bool ResizesNext
        {
            get => _resizesNext;
            set
            {
                if (_resizesNext == value)
                {
                    return;
                }

                _resizesNext = value;
                _target = null;

                InvalidateLayout();
            }
        }

        public float Size
        {
            get => _size;
            set => Set(value, false);
        }

        public float Minimum
        {
            get => _minimum;
            set
            {
                _minimum = value < 0 ? 0 : value;

                Set(_size, false);
            }
        }

        public float Maximum
        {
            get => _maximum;
            set
            {
                _maximum = value;

                Set(_size, false);
            }
        }

        public float Step
        {
            get => _step;
            set => _step = value <= 0 ? 1 : value;
        }

        public VisualElement Target => _target ?? FindTarget();

        public bool IsDragging => _dragging;

        private void Orient()
        {
            ToggleState(VERTICAL, _vertical);
            ToggleState(HORIZONTAL, !_vertical);
            _line.ToggleState(VERTICAL, _vertical);
            _line.ToggleState(HORIZONTAL, !_vertical);

            Styles.Layout = new LayoutStyleDescriptor { Type = _vertical ? LayoutType.Column : LayoutType.Row };
            Styles.ContentAlign = new ContentAlignStyleDescriptor
            {
                Horizontal = _vertical ? ContentAlign.Unset : ContentAlign.Center,
                Vertical = _vertical ? ContentVAlign.Middle : ContentVAlign.Unset
            };

            _line.Styles.Width = _vertical
                ? new WidthStyleDescriptor()
                : new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 1 };
            _line.Styles.Height = _vertical
                ? new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 1 }
                : new HeightStyleDescriptor();

            Invalidate();
            _line.Invalidate();
        }

        private VisualElement FindTarget()
        {
            VisualElement parent = Parent;

            if (parent == null)
            {
                return null;
            }

            IReadOnlyList<VisualElement> siblings = parent.ChildElements;

            for (int index = 0; index < siblings.Count; index++)
            {
                if (siblings[index] != this)
                {
                    continue;
                }

                int neighbour = _resizesNext ? index + 1 : index - 1;

                return neighbour >= 0 && neighbour < siblings.Count ? siblings[neighbour] : null;
            }

            return null;
        }

        protected override void OnPrepass(float viewportWidth, float viewportHeight)
        {
            base.OnPrepass(viewportWidth, viewportHeight);

            VisualElement target = FindTarget();

            if (target != _target)
            {
                _target = target;
                _applied = float.NaN;
            }

            Reapply();
        }

        private void Reapply()
        {
            if (_target == null || float.IsNaN(_size))
            {
                return;
            }

            if (_applied == _size && _appliedVertical == _vertical)
            {
                return;
            }

            if (!float.IsNaN(_applied) && _appliedVertical != _vertical)
            {
                Constrain(_target, _appliedVertical, null);
            }

            Constrain(_target, _vertical, _size);

            _applied = _size;
            _appliedVertical = _vertical;

            _target.Invalidate();
        }

        private static void Constrain(VisualElement target, bool vertical, float? size)
        {
            if (vertical)
            {
                target.Styles.Height = size.HasValue
                    ? new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = size.Value }
                    : new HeightStyleDescriptor();
                target.Styles.MinHeight = size.HasValue
                    ? new MinHeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = size.Value }
                    : new MinHeightStyleDescriptor();
                target.Styles.MaxHeight = size.HasValue
                    ? new MaxHeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = size.Value }
                    : new MaxHeightStyleDescriptor();

                return;
            }

            target.Styles.Width = size.HasValue
                ? new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = size.Value }
                : new WidthStyleDescriptor();
            target.Styles.MinWidth = size.HasValue
                ? new MinWidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = size.Value }
                : new MinWidthStyleDescriptor();
            target.Styles.MaxWidth = size.HasValue
                ? new MaxWidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = size.Value }
                : new MaxWidthStyleDescriptor();
        }

        private float Available
        {
            get
            {
                VisualElement parent = Parent;

                if (parent == null)
                {
                    return float.PositiveInfinity;
                }

                float room = _vertical
                    ? parent.ContentHeight - ActualHeight
                    : parent.ContentWidth - ActualWidth;

                return room > 0 ? room : float.PositiveInfinity;
            }
        }

        private float Upper => Math.Min(_maximum, Available);

        private float Clamp(float value)
        {
            float upper = Upper;

            if (value > upper)
            {
                value = upper;
            }

            return value < _minimum ? _minimum : value;
        }

        private float Current
        {
            get
            {
                if (!float.IsNaN(_size))
                {
                    return _size;
                }

                VisualElement target = Target;

                if (target == null)
                {
                    return _minimum;
                }

                return _vertical ? target.ActualHeight : target.ActualWidth;
            }
        }

        private void Set(float value, bool interactive)
        {
            if (float.IsNaN(value))
            {
                return;
            }

            float clamped = (float)Math.Round(interactive ? Clamp(value) : Math.Max(_minimum, Math.Min(_maximum, value)));

            if (float.IsNaN(_initial))
            {
                _initial = clamped;
            }

            if (clamped == _size)
            {
                return;
            }

            _size = clamped;

            AccessibleValue = _size.ToString(CultureInfo.InvariantCulture);

            if (_target != null)
            {
                Reapply();
            }
            else
            {
                InvalidateLayout();
            }

            if (interactive)
            {
                SizeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void Interact(float value)
        {
            if (!IsEnabled || Target == null)
            {
                return;
            }

            _target = Target;

            Set(value, true);
        }

        private float Sign => _resizesNext ? -1 : 1;

        private void OnPointerDown(object sender, PointerEventArgs args)
        {
            if (args.Button == PointerButton.Left)
            {
                args.Handled = true;
            }
        }

        private void OnDragStart(object sender, DragEventArgs args)
        {
            if (!IsEnabled || Target == null)
            {
                return;
            }

            args.Handled = true;

            _dragging = true;
            _dragFrom = Current;

            ToggleState(DRAGGING, true);
            _line.ToggleState(DRAGGING, true);
        }

        private void OnDrag(object sender, DragEventArgs args)
        {
            if (!_dragging)
            {
                return;
            }

            args.Handled = true;

            Interact(_dragFrom + (Sign * (_vertical ? args.TotalY : args.TotalX)));
        }

        private void OnDragEnd(object sender, DragEventArgs args)
        {
            if (!_dragging)
            {
                return;
            }

            _dragging = false;

            ToggleState(DRAGGING, false);
            _line.ToggleState(DRAGGING, false);
        }

        private void OnDoubleClick(object sender, PointerEventArgs args)
        {
            if (float.IsNaN(_initial))
            {
                return;
            }

            args.Handled = true;

            Interact(_initial);
        }

        private void OnKeyDown(object sender, KeyEventArgs args)
        {
            if (args.Modifiers != KeyModifiers.None && args.Modifiers != KeyModifiers.Shift)
            {
                return;
            }

            float amount = args.Modifiers == KeyModifiers.Shift ? 1 : _step;
            Key back = _vertical ? Key.Up : Key.Left;
            Key forth = _vertical ? Key.Down : Key.Right;

            if (args.Key == back || args.Key == forth)
            {
                args.Handled = true;
                Interact(Current + (Sign * (args.Key == forth ? amount : -amount)));

                return;
            }

            if (args.Key == Key.Home)
            {
                args.Handled = true;
                Interact(_minimum);

                return;
            }

            if (args.Key == Key.End)
            {
                args.Handled = true;
                Interact(Upper);
            }
        }

        bool IAccessibleRange.HasRange => Target != null;

        float IAccessibleRange.Minimum => _minimum;

        float IAccessibleRange.Maximum
        {
            get
            {
                float upper = Upper;

                return float.IsInfinity(upper) ? Math.Max(Current, _minimum) : upper;
            }
        }

        float IAccessibleRange.Value => Current;

        float IAccessibleRange.Step => _step;

        bool IAccessibleRange.CanSetValue => IsEnabled && Target != null;

        void IAccessibleRange.SetValue(float value) => Interact(value);
    }
}
