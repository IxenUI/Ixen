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

        public int Visits { get; set; }

        public bool Saves { get; set; } = true;

        public int Restores { get; private set; }

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

        protected override void OnSaveState(ComponentState state)
        {
            if (!Saves)
            {
                return;
            }

            state.Set("path", Navigation.Path);
            state.Set("visits", Visits);
        }

        protected override void OnRestoreState(ComponentState state)
        {
            Restores++;
            Navigation.Reset(state.Get("path", Navigator.ROOT));
            Visits = state.Get("visits", 0);
        }
    }
}
