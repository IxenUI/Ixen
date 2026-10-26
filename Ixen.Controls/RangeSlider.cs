using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Globalization;

namespace Ixen.Controls
{
    public class RangeSlider : VisualElement
    {
        public event EventHandler<EventArgs> LowerChanged;
        public event EventHandler<EventArgs> UpperChanged;

        private readonly VisualElement _track;
        private readonly VisualElement _fill;
        private readonly RangeSliderThumb _lowerThumb;
        private readonly RangeSliderThumb _upperThumb;

        private float _minimum;
        private float _maximum = 100;
        private float _step = 1;
        private float _lower = float.NegativeInfinity;
        private float _upper = float.PositiveInfinity;
        private bool _vertical;
        private RangeSliderThumb _active;

        public RangeSlider()
        {
            TypeName = nameof(RangeSlider);
            Role = AccessibleRole.Group;

            Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };

            _track = new VisualElement { TypeName = Slider.TRACK, Role = AccessibleRole.Presentation };
            _fill = new VisualElement { TypeName = Slider.FILL, Role = AccessibleRole.Presentation };
            _lowerThumb = new RangeSliderThumb(this, true) { Label = "minimum" };
            _upperThumb = new RangeSliderThumb(this, false) { Label = "maximum" };

            AddChildren(_track, _fill, _lowerThumb, _upperThumb);

            Orient();

            PointerDown += OnPointerDown;
            PointerDragStart += OnDrag;
            PointerDrag += OnDrag;
        }

        public VisualElement LowerThumb => _lowerThumb;

        public VisualElement UpperThumb => _upperThumb;

        public string LowerLabel
        {
            get => _lowerThumb.Label;
            set => _lowerThumb.Label = value;
        }

        public string UpperLabel
        {
            get => _upperThumb.Label;
            set => _upperThumb.Label = value;
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
            }
        }

        public float Minimum
        {
            get => _minimum;
            set
            {
                _minimum = value;

                Layout();
            }
        }

        public float Maximum
        {
            get => _maximum;
            set
            {
                _maximum = value;

                Layout();
            }
        }

        public float Step
        {
            get => _step;
            set
            {
                _step = value <= 0 ? 0 : value;

                Layout();
            }
        }

        public float Lower
        {
            get => Clamp(Snap(_lower), _minimum, _maximum);
            set
            {
                _lower = value;

                Layout();
            }
        }

        public float Upper
        {
            get
            {
                float upper = Clamp(Snap(_upper), _minimum, _maximum);
                float lower = Lower;

                return upper < lower ? lower : upper;
            }
            set
            {
                _upper = value;

                Layout();
            }
        }

        internal float KeyStep => _step > 0 ? _step : (_maximum - _minimum) / 100;

        internal float BoundBelow(RangeSliderThumb thumb) => thumb == _lowerThumb ? _minimum : Lower;

        internal float BoundAbove(RangeSliderThumb thumb) => thumb == _lowerThumb ? Upper : _maximum;

        internal float ValueOf(RangeSliderThumb thumb) => thumb == _lowerThumb ? Lower : Upper;

        internal void Move(RangeSliderThumb thumb, float value)
        {
            if (!IsEnabled)
            {
                return;
            }

            float moved = Clamp(Snap(value), BoundBelow(thumb), BoundAbove(thumb));

            if (moved == ValueOf(thumb))
            {
                return;
            }

            if (thumb == _lowerThumb)
            {
                _lower = moved;
                Layout();
                LowerChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            _upper = moved;
            Layout();
            UpperChanged?.Invoke(this, EventArgs.Empty);
        }

        private float Snap(float value)
        {
            if (_step <= 0 || float.IsInfinity(value))
            {
                return value;
            }

            float steps = (float)Math.Round((value - _minimum) / _step);

            return _minimum + (steps * _step);
        }

        private static float Clamp(float value, float low, float high)
        {
            if (value < low)
            {
                return low;
            }

            return value > high ? high : value;
        }

        private float FractionOf(float value)
        {
            float span = _maximum - _minimum;

            return span <= 0 ? 0 : (value - _minimum) / span;
        }

        private void Orient()
        {
            foreach (VisualElement element in new VisualElement[] { this, _track, _fill, _lowerThumb, _upperThumb })
            {
                element.ToggleState(Slider.VERTICAL, _vertical);
                element.ToggleState(Slider.HORIZONTAL, !_vertical);
            }

            _track.Styles.Left = _vertical ? new LeftStyleDescriptor() : new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _track.Styles.Right = _vertical ? new RightStyleDescriptor() : new RightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _track.Styles.Top = _vertical ? new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 } : new TopStyleDescriptor();
            _track.Styles.Bottom = _vertical ? new BottomStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 } : new BottomStyleDescriptor();

            _fill.Styles.Left = _vertical ? new LeftStyleDescriptor() : new LeftStyleDescriptor { Unit = SizeUnit.Percents, Value = 0 };
            _fill.Styles.Width = _vertical ? new WidthStyleDescriptor() : new WidthStyleDescriptor { Unit = SizeUnit.Percents, Value = 0 };
            _fill.Styles.Bottom = _vertical ? new BottomStyleDescriptor { Unit = SizeUnit.Percents, Value = 0 } : new BottomStyleDescriptor();
            _fill.Styles.Height = _vertical ? new HeightStyleDescriptor { Unit = SizeUnit.Percents, Value = 0 } : new HeightStyleDescriptor();

            foreach (RangeSliderThumb thumb in new[] { _lowerThumb, _upperThumb })
            {
                thumb.Styles.Left = _vertical ? new LeftStyleDescriptor() : new LeftStyleDescriptor { Unit = SizeUnit.Percents, Value = 0 };
                thumb.Styles.Top = _vertical ? new TopStyleDescriptor { Unit = SizeUnit.Percents, Value = 0 } : new TopStyleDescriptor();
                thumb.Styles.Transform = new TransformStyleDescriptor
                {
                    Operations =
                    {
                        new TransformOperation
                        {
                            Kind = TransformKind.Translate,
                            XUnit = SizeUnit.Percents,
                            X = _vertical ? 0 : -50,
                            YUnit = SizeUnit.Percents,
                            Y = _vertical ? -50 : 0
                        }
                    }
                };
            }

            foreach (VisualElement part in new VisualElement[] { _track, _fill, _lowerThumb, _upperThumb })
            {
                part.Invalidate();
            }

            Layout();
        }

        private void Layout()
        {
            float lower = Lower;
            float upper = Upper;
            float from = FractionOf(lower) * 100;
            float to = FractionOf(upper) * 100;

            if (_vertical)
            {
                _fill.Styles.Bottom.Value = from;
                _fill.Styles.Height.Value = to - from;
                _lowerThumb.Styles.Top.Value = 100 - from;
                _upperThumb.Styles.Top.Value = 100 - to;
            }
            else
            {
                _fill.Styles.Left.Value = from;
                _fill.Styles.Width.Value = to - from;
                _lowerThumb.Styles.Left.Value = from;
                _upperThumb.Styles.Left.Value = to;
            }

            _lowerThumb.AccessibleValue = lower.ToString(CultureInfo.InvariantCulture);
            _upperThumb.AccessibleValue = upper.ToString(CultureInfo.InvariantCulture);

            InvalidateLayout();
        }

        private float ValueAt(float x, float y)
        {
            float length = _vertical ? ContentHeight : ContentWidth;

            if (length <= 0)
            {
                return _minimum;
            }

            float fraction = _vertical ? 1 - ((y - ContentY) / length) : (x - ContentX) / length;

            return _minimum + (fraction * (_maximum - _minimum));
        }

        private RangeSliderThumb Nearest(float value)
        {
            float lower = Lower;
            float upper = Upper;

            if (value >= upper)
            {
                return _upperThumb;
            }

            if (value <= lower)
            {
                return _lowerThumb;
            }

            return value - lower <= upper - value ? _lowerThumb : _upperThumb;
        }

        private void OnPointerDown(object sender, PointerEventArgs args)
        {
            args.Handled = true;

            if (!IsEnabled)
            {
                return;
            }

            float value = ValueAt(args.X, args.Y);

            _active = args.Source == _lowerThumb || args.Source == _upperThumb
                ? (RangeSliderThumb)args.Source
                : Nearest(value);

            Host?.Focus(_active);
            Move(_active, value);
        }

        private void OnDrag(object sender, DragEventArgs args)
        {
            args.Handled = true;

            if (_active != null)
            {
                Move(_active, ValueAt(args.X, args.Y));
            }
        }
    }
}
