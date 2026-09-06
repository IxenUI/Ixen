using Ixen.Core.Components;
using Ixen.Views;

namespace Ixen.Core.UT.Components.Fixtures
{
    public class NavigationComponent : Component<NavigationView>
    {
        public Navigator Navigation { get; } = new Navigator();

        public Navigator Late { get; } = new Navigator();

        public int Renders { get; private set; }

        public string Caption => Navigation.Path;

        protected override void OnInitialized()
        {
            Follow(Navigation);
        }

        protected override void Render()
        {
            Renders++;
        }

        public void FollowAgain()
        {
            Follow(Navigation);
        }

        public void FollowNothing()
        {
            Follow(null);
        }

        public void FollowLate()
        {
            Follow(Late);
        }
    }
}
