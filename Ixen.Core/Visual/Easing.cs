using Ixen.Core.Visual.Styles;

namespace Ixen.Core.Visual
{
    internal static class Easing
    {
        internal const string LINEAR = "linear";
        internal const string EASE_IN = "ease-in";
        internal const string EASE_OUT = "ease-out";
        internal const string EASE_IN_OUT = "ease-in-out";

        internal static bool TryParse(string value, out EasingKind kind)
        {
            switch (value)
            {
                case LINEAR:
                    kind = EasingKind.Linear;
                    return true;

                case EASE_IN:
                    kind = EasingKind.EaseIn;
                    return true;

                case EASE_OUT:
                    kind = EasingKind.EaseOut;
                    return true;

                case EASE_IN_OUT:
                    kind = EasingKind.EaseInOut;
                    return true;

                default:
                    kind = EasingKind.Linear;
                    return false;
            }
        }

        internal static float Apply(EasingKind kind, float progress)
        {
            if (progress <= 0f)
            {
                return 0f;
            }

            if (progress >= 1f)
            {
                return 1f;
            }

            switch (kind)
            {
                case EasingKind.EaseIn:
                    return progress * progress;

                case EasingKind.EaseOut:
                    return progress * (2f - progress);

                case EasingKind.EaseInOut:
                    return progress < 0.5f
                        ? 2f * progress * progress
                        : 1f - 2f * (1f - progress) * (1f - progress);

                default:
                    return progress;
            }
        }
    }
}
