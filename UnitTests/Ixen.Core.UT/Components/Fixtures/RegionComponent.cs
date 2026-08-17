using Ixen.Core.Components;
using Ixen.Views;

namespace Ixen.Core.UT.Components.Fixtures
{
    public class RegionComponent : Component<RegionView>
    {
        public bool ShowTitle { get; set; }
        public bool ShowFooter { get; set; }
        public string Title { get; set; } = "the title";
        public int Bumps { get; set; }
        public int Level { get; set; }

        public void Bump() => Bumps++;

        public void Toggle(bool title, bool footer)
            => SetState(() =>
            {
                ShowTitle = title;
                ShowFooter = footer;
            });
    }
}
