namespace Ixen.Core.Accessibility
{
    public interface IAccessibleRange
    {
        bool HasRange { get; }

        float Minimum { get; }

        float Maximum { get; }

        float Value { get; }

        float Step { get; }

        bool CanSetValue { get; }

        void SetValue(float value);
    }
}
