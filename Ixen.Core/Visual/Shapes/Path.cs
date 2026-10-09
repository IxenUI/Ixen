using System;
using System.Globalization;

namespace Ixen.Core.Visual.Shapes
{
    public class Path : VisualElement
    {
        internal const string NON_ZERO = "nonzero";
        internal const string EVEN_ODD = "evenodd";

        private string _data;
        private string _viewBox;
        private string _fillRule = NON_ZERO;
        private float _strokeWidth;

        private PathGeometry _geometry;
        private bool _parsed;

        private bool _hasViewBox;
        private float _viewLeft;
        private float _viewTop;
        private float _viewWidth;
        private float _viewHeight;

        public string Data
        {
            get => _data;
            set
            {
                if (_data == value)
                {
                    return;
                }

                _data = value;
                Forget();
            }
        }

        public string ViewBox
        {
            get => _viewBox;
            set
            {
                if (_viewBox == value)
                {
                    return;
                }

                SetViewBox(value);
                _viewBox = value;
                Forget();
            }
        }

        public string FillRule
        {
            get => _fillRule;
            set
            {
                string rule = string.IsNullOrEmpty(value) ? NON_ZERO : value.Trim().ToLowerInvariant();

                if (rule != NON_ZERO && rule != EVEN_ODD)
                {
                    throw new ArgumentException(
                        $"'{value}' is not a fill rule. Write '{NON_ZERO}' or '{EVEN_ODD}'.",
                        nameof(FillRule));
                }

                if (_fillRule == rule)
                {
                    return;
                }

                _fillRule = rule;
                Forget();
            }
        }

        public float StrokeWidth
        {
            get => _strokeWidth;
            set
            {
                float width = value < 0 ? 0 : value;

                if (_strokeWidth == width)
                {
                    return;
                }

                _strokeWidth = width;
                InvalidateLayout();
            }
        }

        internal bool IsStroked => _strokeWidth > 0;

        internal bool EvenOdd => _fillRule == EVEN_ODD;

        internal PathGeometry Geometry
        {
            get
            {
                if (_parsed)
                {
                    return _geometry;
                }

                _parsed = true;
                _geometry = PathData.Parse(_data, EvenOdd);

                return _geometry;
            }
        }

        internal float ViewLeft => _hasViewBox ? _viewLeft : DerivedLeft;

        internal float ViewTop => _hasViewBox ? _viewTop : DerivedTop;

        internal float ViewWidth => _hasViewBox ? _viewWidth : DerivedWidth;

        internal float ViewHeight => _hasViewBox ? _viewHeight : DerivedHeight;

        private float DerivedLeft => Geometry == null ? 0 : Geometry.Left - _strokeWidth / 2;

        private float DerivedTop => Geometry == null ? 0 : Geometry.Top - _strokeWidth / 2;

        private float DerivedWidth => Geometry == null ? 0 : Geometry.Width + _strokeWidth;

        private float DerivedHeight => Geometry == null ? 0 : Geometry.Height + _strokeWidth;

        private void Forget()
        {
            _parsed = false;
            _geometry = null;

            InvalidateLayout();
        }

        private void SetViewBox(string value)
        {
            _hasViewBox = false;

            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            string[] parts = value.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 4
                || !Read(parts[0], out _viewLeft)
                || !Read(parts[1], out _viewTop)
                || !Read(parts[2], out _viewWidth)
                || !Read(parts[3], out _viewHeight)
                || _viewWidth <= 0 || _viewHeight <= 0)
            {
                throw new ArgumentException(
                    $"'{value}' is not a view box. Write four numbers: x, y, width and height.",
                    nameof(ViewBox));
            }

            _hasViewBox = true;
        }

        private static bool Read(string text, out float value)
            => float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
