using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;

namespace Ixen.Controls
{
    internal class RangeSliderThumb : VisualElement, IAccessibleRange
    {
        private readonly RangeSlider _owner;

        internal RangeSliderThumb(RangeSlider owner, bool lower)
        {
            _owner = owner;

            TypeName = Slider.THUMB;
            Name = lower ? "lower" : "upper";
            Focusable = true;
            Role = AccessibleRole.Slider;

            KeyDown += OnKeyDown;
        }

        bool IAccessibleRange.HasRange => true;

        float IAccessibleRange.Minimum => _owner.BoundBelow(this);

        float IAccessibleRange.Maximum => _owner.BoundAbove(this);

        float IAccessibleRange.Value => _owner.ValueOf(this);

        float IAccessibleRange.Step => _owner.KeyStep;

        bool IAccessibleRange.CanSetValue => IsEnabled;

        void IAccessibleRange.SetValue(float value) => _owner.Move(this, value);

        private void OnKeyDown(object sender, KeyEventArgs args)
        {
            float amount = _owner.KeyStep;
            float value = _owner.ValueOf(this);

            switch (args.Key)
            {
                case Key.Left:
                case Key.Down:
                    args.Handled = true;
                    _owner.Move(this, value - amount);
                    break;

                case Key.Right:
                case Key.Up:
                    args.Handled = true;
                    _owner.Move(this, value + amount);
                    break;

                case Key.PageDown:
                    args.Handled = true;
                    _owner.Move(this, value - (amount * 10));
                    break;

                case Key.PageUp:
                    args.Handled = true;
                    _owner.Move(this, value + (amount * 10));
                    break;

                case Key.Home:
                    args.Handled = true;
                    _owner.Move(this, _owner.BoundBelow(this));
                    break;

                case Key.End:
                    args.Handled = true;
                    _owner.Move(this, _owner.BoundAbove(this));
                    break;
            }
        }
    }
}
