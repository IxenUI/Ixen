using Ixen.Core.Input;
using Ixen.Core.Visual.Styles.Descriptors;
using System;

namespace Ixen.Core.Visual
{
    public class ScrollbarThumb : VisualElement
    {
        public ScrollbarThumb()
        {
            TypeName = nameof(ScrollbarThumb);

            Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            Styles.Background = new BackgroundStyleDescriptor { Color = "#A0606060" };
            Styles.CornerRadius = new CornerRadiusStyleDescriptor
            {
                TopLeft = 4,
                TopRight = 4,
                BottomRight = 4,
                BottomLeft = 4
            };
        }
    }

    public class ScrollbarButton : VisualElement
    {
        internal ScrollbarButton(string glyph)
        {
            TypeName = nameof(ScrollbarButton);
            Text = glyph;

            Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            Styles.Color = new ColorStyleDescriptor { Value = "#90707070" };
            Styles.FontSize = new FontSizeStyleDescriptor { Value = 7 };
            Styles.TextAlign = new TextAlignStyleDescriptor
            {
                Horizontal = TextAlign.Center,
                Vertical = TextVAlign.Middle
            };
        }
    }

    public class Scrollbar : VisualElement
    {
        internal const float THICKNESS = 14;
        internal const float MIN_THUMB = 20;
        internal const float STEP = 48;

        private const float THUMB_INSET = 3.5f;

        private const string UP = "\u25B2";
        private const string DOWN = "\u25BC";
        private const string LEFT = "\u25C0";
        private const string RIGHT = "\u25B6";

        internal bool IsVertical { get; private set; }
        internal ScrollbarThumb Thumb { get; private set; }
        internal ScrollbarButton Start { get; private set; }
        internal ScrollbarButton End { get; private set; }

        internal Scrollbar(bool vertical)
        {
            IsVertical = vertical;
            TypeName = nameof(Scrollbar);

            Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };
            Styles.Background = new BackgroundStyleDescriptor { Color = "#08000000" };
            Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = THICKNESS };
            Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = THICKNESS };

            Start = new ScrollbarButton(vertical ? UP : LEFT);
            End = new ScrollbarButton(vertical ? DOWN : RIGHT);
            Thumb = new ScrollbarThumb();

            AddChildren(Start, End, Thumb);

            Start.PointerDown += (sender, args) => Press(Start, -1, args);
            End.PointerDown += (sender, args) => Press(End, 1, args);

            Thumb.PointerDragStart += OnThumbDrag;
            Thumb.PointerDrag += OnThumbDrag;
        }

        private const int REPEAT_DELAY = 400;
        private const int REPEAT_INTERVAL = 60;

        private IDisposable _repeat;

        private void Press(ScrollbarButton button, float direction, PointerEventArgs args)
        {
            Step(direction);
            args.Handled = true;

            _repeat?.Dispose();
            _repeat = Host?.Scheduler?.Schedule(REPEAT_DELAY, false, () => StartRepeat(button, direction));
        }

        private void StartRepeat(ScrollbarButton button, float direction)
        {
            if (!IsHeld(button))
            {
                return;
            }

            Step(direction);

            _repeat = Host?.Scheduler?.Schedule(REPEAT_INTERVAL, true, () =>
            {
                if (!IsHeld(button))
                {
                    StopRepeat();
                    return;
                }

                Step(direction);
            });
        }

        private bool IsHeld(ScrollbarButton button)
            => Host != null && Host.PressedElement == button;

        private void StopRepeat()
        {
            _repeat?.Dispose();
            _repeat = null;
        }

        internal override void OnHostChanged()
        {
            if (Host == null)
            {
                StopRepeat();
            }
        }

        private void Step(float direction)
        {
            if (Parent == null)
            {
                return;
            }

            if (IsVertical)
            {
                Parent.ScrollBy(0, direction * STEP);
            }
            else
            {
                Parent.ScrollBy(direction * STEP, 0);
            }
        }

        private void OnThumbDrag(object sender, DragEventArgs args)
        {
            VisualElement target = Parent;

            if (target == null)
            {
                return;
            }

            float free = TrackLength() - (IsVertical ? Thumb.ActualHeight : Thumb.ActualWidth);

            if (free <= 0)
            {
                return;
            }

            float max = IsVertical ? target.MaxScrollY : target.MaxScrollX;
            float delta = (IsVertical ? args.DeltaY : args.DeltaX) / free * max;

            if (IsVertical)
            {
                target.ScrollBy(0, delta);
            }
            else
            {
                target.ScrollBy(delta, 0);
            }

            args.Handled = true;
        }

        private float TrackLength()
        {
            float length = IsVertical ? ActualHeight : ActualWidth;

            return HasButtons(length) ? length - 2 * THICKNESS : length;
        }

        private static bool HasButtons(float length) => length >= 3 * THICKNESS;

        internal void Layout(VisualElement target, float x, float y, float length, float thickness)
        {
            Styles.Width.Value = IsVertical ? thickness : length;
            Styles.Height.Value = IsVertical ? length : thickness;
            LayoutOffsetX = x;
            LayoutOffsetY = y;

            bool buttons = HasButtons(length);
            float trackStart = buttons ? thickness : 0;
            float track = buttons ? length - 2 * thickness : length;

            LayoutButton(Start, buttons, 0, thickness);
            LayoutButton(End, buttons, length - thickness, thickness);

            float extent = IsVertical ? target.ScrollExtentHeight : target.ScrollExtentWidth;
            float viewport = IsVertical ? target.ContentHeight : target.ContentWidth;
            float offset = IsVertical ? target.ScrollY : target.ScrollX;
            float max = IsVertical ? target.MaxScrollY : target.MaxScrollX;

            float thumbLength = extent <= 0 ? track : track * (viewport / extent);

            if (thumbLength < MIN_THUMB)
            {
                thumbLength = MIN_THUMB;
            }

            if (thumbLength > track)
            {
                thumbLength = track;
            }

            float thumbOffset = trackStart + (max <= 0 ? 0 : (track - thumbLength) * (offset / max));

            float breadth = thickness - 2 * THUMB_INSET;

            Place(Thumb,
                IsVertical ? breadth : thumbLength,
                IsVertical ? thumbLength : breadth,
                IsVertical ? THUMB_INSET : thumbOffset,
                IsVertical ? thumbOffset : THUMB_INSET);
        }

        private void LayoutButton(ScrollbarButton button, bool visible, float offset, float thickness)
        {
            if (!visible)
            {
                Place(button, 0, 0, 0, 0);
                return;
            }

            Place(button, thickness, thickness, IsVertical ? 0 : offset, IsVertical ? offset : 0);
        }

        private static void Place(VisualElement element, float width, float height, float left, float top)
        {
            element.Styles.Width.Value = width;
            element.Styles.Height.Value = height;
            element.Styles.Left.Value = left;
            element.Styles.Top.Value = top;
        }

        internal void Hide()
        {
            Styles.Width.Value = 0;
            Styles.Height.Value = 0;

            Place(Thumb, 0, 0, 0, 0);
            Place(Start, 0, 0, 0, 0);
            Place(End, 0, 0, 0, 0);
        }
    }
}
