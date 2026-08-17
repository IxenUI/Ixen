namespace Ixen.Core.Visual
{
    public interface IRegionRow
    {
        int ElementCount { get; }

        VisualElement ElementAt(int index);
    }
}
