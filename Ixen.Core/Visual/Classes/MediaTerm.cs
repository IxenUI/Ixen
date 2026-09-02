namespace Ixen.Core.Visual.Classes
{
    internal enum MediaFeatureKind
    {
        MinWidth,
        MaxWidth,
        MinHeight,
        MaxHeight,
        Portrait,
        Landscape
    }

    internal abstract class MediaTerm
    {
        internal abstract bool Matches(float width, float height);
    }

    internal sealed class MediaFeature : MediaTerm
    {
        private readonly MediaFeatureKind _kind;
        private readonly float _length;

        internal MediaFeature(MediaFeatureKind kind, float length)
        {
            _kind = kind;
            _length = length;
        }

        internal override bool Matches(float width, float height)
        {
            switch (_kind)
            {
                case MediaFeatureKind.MinWidth:
                    return width >= _length;

                case MediaFeatureKind.MaxWidth:
                    return width <= _length;

                case MediaFeatureKind.MinHeight:
                    return height >= _length;

                case MediaFeatureKind.MaxHeight:
                    return height <= _length;

                case MediaFeatureKind.Portrait:
                    return width <= height;

                default:
                    return height <= width;
            }
        }
    }

    internal sealed class MediaAnd : MediaTerm
    {
        private readonly MediaTerm _left;
        private readonly MediaTerm _right;

        internal MediaAnd(MediaTerm left, MediaTerm right)
        {
            _left = left;
            _right = right;
        }

        internal override bool Matches(float width, float height)
            => _left.Matches(width, height) && _right.Matches(width, height);
    }

    internal sealed class MediaOr : MediaTerm
    {
        private readonly MediaTerm _left;
        private readonly MediaTerm _right;

        internal MediaOr(MediaTerm left, MediaTerm right)
        {
            _left = left;
            _right = right;
        }

        internal override bool Matches(float width, float height)
            => _left.Matches(width, height) || _right.Matches(width, height);
    }

    internal sealed class MediaNot : MediaTerm
    {
        private readonly MediaTerm _inner;

        internal MediaNot(MediaTerm inner)
        {
            _inner = inner;
        }

        internal override bool Matches(float width, float height)
            => !_inner.Matches(width, height);
    }
}
