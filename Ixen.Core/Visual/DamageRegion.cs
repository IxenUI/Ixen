namespace Ixen.Core.Visual
{
    internal sealed class DamageRegion
    {
        internal const int MAX_RECTS = 4;

        private readonly float[] _lefts = new float[MAX_RECTS];
        private readonly float[] _tops = new float[MAX_RECTS];
        private readonly float[] _rights = new float[MAX_RECTS];
        private readonly float[] _bottoms = new float[MAX_RECTS];

        private int _count;
        private bool _whole;

        internal bool IsEmpty => _count == 0 && !_whole;
        internal bool IsWhole => _whole;
        internal int Count => _whole ? 0 : _count;

        internal float Left => Min(_lefts);
        internal float Top => Min(_tops);
        internal float Right => Max(_rights);
        internal float Bottom => Max(_bottoms);

        internal void RectAt(int index, out float left, out float top, out float right, out float bottom)
        {
            left = _lefts[index];
            top = _tops[index];
            right = _rights[index];
            bottom = _bottoms[index];
        }

        internal void Reset()
        {
            _count = 0;
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

            float left = x;
            float top = y;
            float right = x + width;
            float bottom = y + height;

            for (int index = 0; index < _count; index++)
            {
                if (Touches(index, left, top, right, bottom))
                {
                    Grow(index, left, top, right, bottom);
                    Settle(index);

                    return;
                }
            }

            if (_count < MAX_RECTS)
            {
                Set(_count++, left, top, right, bottom);

                return;
            }

            int cheapest = 0;
            float growth = float.MaxValue;

            for (int index = 0; index < _count; index++)
            {
                float added = UnionArea(index, left, top, right, bottom) - Area(index);

                if (added < growth)
                {
                    growth = added;
                    cheapest = index;
                }
            }

            Grow(cheapest, left, top, right, bottom);
            Settle(cheapest);
        }

        private void Settle(int grown)
        {
            for (int index = 0; index < _count; index++)
            {
                if (index == grown || !Touches(index, _lefts[grown], _tops[grown], _rights[grown], _bottoms[grown]))
                {
                    continue;
                }

                Grow(grown, _lefts[index], _tops[index], _rights[index], _bottoms[index]);
                RemoveAt(index);

                if (index < grown)
                {
                    grown--;
                }

                index = -1;
            }
        }

        private void RemoveAt(int index)
        {
            _count--;

            for (int moved = index; moved < _count; moved++)
            {
                Set(moved, _lefts[moved + 1], _tops[moved + 1], _rights[moved + 1], _bottoms[moved + 1]);
            }
        }

        private bool Touches(int index, float left, float top, float right, float bottom)
            => left <= _rights[index] && right >= _lefts[index] && top <= _bottoms[index] && bottom >= _tops[index];

        private void Grow(int index, float left, float top, float right, float bottom)
        {
            if (left < _lefts[index])
            {
                _lefts[index] = left;
            }

            if (top < _tops[index])
            {
                _tops[index] = top;
            }

            if (right > _rights[index])
            {
                _rights[index] = right;
            }

            if (bottom > _bottoms[index])
            {
                _bottoms[index] = bottom;
            }
        }

        private void Set(int index, float left, float top, float right, float bottom)
        {
            _lefts[index] = left;
            _tops[index] = top;
            _rights[index] = right;
            _bottoms[index] = bottom;
        }

        private float Area(int index) => (_rights[index] - _lefts[index]) * (_bottoms[index] - _tops[index]);

        private float UnionArea(int index, float left, float top, float right, float bottom)
        {
            float l = left < _lefts[index] ? left : _lefts[index];
            float t = top < _tops[index] ? top : _tops[index];
            float r = right > _rights[index] ? right : _rights[index];
            float b = bottom > _bottoms[index] ? bottom : _bottoms[index];

            return (r - l) * (b - t);
        }

        private float Min(float[] values)
        {
            float found = float.MaxValue;

            for (int index = 0; index < _count; index++)
            {
                if (values[index] < found)
                {
                    found = values[index];
                }
            }

            return found;
        }

        private float Max(float[] values)
        {
            float found = float.MinValue;

            for (int index = 0; index < _count; index++)
            {
                if (values[index] > found)
                {
                    found = values[index];
                }
            }

            return found;
        }
    }
}
