using Ixen.Core.Components;
using Ixen.Views;

namespace Ixen.Core.UT.Components.Fixtures
{
    public class ObservableComponent : Component<ObservableView>
    {
        public ObservableComponent()
            : this(new ObservableList<string>(), new AsyncValue<string>(), true)
        { }

        public ObservableComponent(ObservableList<string> entries, AsyncValue<string> poem, bool follow)
        {
            Entries = entries;
            Poem = poem;
            Follows = follow;
        }

        public ObservableList<string> Entries { get; }

        public AsyncValue<string> Poem { get; }

        public bool Follows { get; }

        public bool MutatesInRender { get; set; }

        public int Renders { get; private set; }

        protected override void OnInitialized()
        {
            if (!Follows)
            {
                return;
            }

            Follow(Entries);
        }

        protected override void Render()
        {
            Renders++;

            if (!MutatesInRender)
            {
                return;
            }

            Entries.Add("from render");
        }

        public void FollowThePoem()
        {
            Follow(Poem);
        }

        public void FollowAgain()
        {
            Follow(Entries);
        }
    }
}
