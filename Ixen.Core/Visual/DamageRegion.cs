namespace Ixen.Core.Visual
{
    internal struct DamageRegion
    {
        private float _left;
        private float _top;
        private float _right;
        private float _bottom;
        private bool _any;
        private bool _whole;

        internal bool IsEmpty => !_any && !_whole;
        internal bool IsWhole => _whole;

        internal float Left => _left;
        internal float Top => _top;
        internal float Right => _right;
        internal float Bottom => _bottom;

        internal void Reset()
        {
            _any = false;
            _whole = false;
        }

        internal void SetWhole()
        {
            _whole = true;
        }

        internal void Add(float x, float y, float width, float height)
        {
            if (_whole || width <= 0 || height <= 0)
            {
                return;
            }

            if (!_any)
            {
                _left = x;
                _top = y;
                _right = x + width;
                _bottom = y + height;
                _any = true;

                return;
            }

            if (x < _left)
            {
                _left = x;
            }

            if (y < _top)
            {
                _top = y;
            }

            if (x + width > _right)
            {
                _right = x + width;
            }

            if (y + height > _bottom)
            {
                _bottom = y + height;
            }
        }
    }
}
