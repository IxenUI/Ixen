using Ixen.Core.UT.Components.Fixtures;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Components
{
    [TestClass]
    public class FollowTests
    {
        private const int VIEWPORT = 200;

        private VisualElement _root;
        private IxenSurface _surface;
        private NavigationComponent _component;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement();
            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };

            _component = new NavigationComponent();
            _root.AddChild(_component.Initialize());

            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private string Caption => _component.View.FindByName("navigation_label")?.Text;

        [TestMethod]
        public void FollowingANavigatorRendersTheComponentAgain()
        {
            int before = _component.Renders;

            _component.Navigation.Navigate("/poems");
            Layout();

            Assert.AreEqual(before + 1, _component.Renders);
        }

        [TestMethod]
        public void TheBoundValueFollowsTheCurrentPath()
        {
            Assert.AreEqual("/", Caption);

            _component.Navigation.Navigate("/poems/12");
            Layout();

            Assert.AreEqual("/poems/12", Caption);

            _component.Navigation.Back();
            Layout();

            Assert.AreEqual("/", Caption);
        }

        [TestMethod]
        public void ADetachedComponentIsNotRenderedAtAll()
        {
            _root.RemoveChild(_component.View);
            Layout();

            int before = _component.Renders;

            _component.Navigation.Navigate("/poems");
            Layout();

            Assert.AreEqual(before, _component.Renders);
        }

        [TestMethod]
        public void ANavigationMissedWhileAwayIsCaughtUpOnTheWayBack()
        {
            _root.RemoveChild(_component.View);
            Layout();

            _component.Navigation.Navigate("/poems");

            _root.AddChild(_component.View);
            Layout();

            Assert.AreEqual("/poems", Caption, "the follower came back showing where it used to be");
        }

        [TestMethod]
        public void FollowingWhileAlreadyAttachedSubscribesStraightAway()
        {
            _component.FollowLate();

            int before = _component.Renders;

            _component.Late.Navigate("/late");
            Layout();

            Assert.AreEqual(before + 1, _component.Renders);
            Assert.AreEqual(2, _component.FollowedCount);
        }

        [TestMethod]
        public void AComponentThatFollowsNothingRendersNoMoreThanBefore()
        {
            var plain = new LifecycleComponent();

            _root.AddChild(plain.Initialize());
            Layout();

            Assert.AreEqual(0, plain.FollowedCount);
        }

        [TestMethod]
        public void ReattachingResumesFollowing()
        {
            _root.RemoveChild(_component.View);
            Layout();

            _component.Navigation.Navigate("/poems");

            _root.AddChild(_component.View);
            Layout();

            int before = _component.Renders;

            _component.Navigation.Navigate("/authors");
            Layout();

            Assert.AreEqual(before + 1, _component.Renders);
            Assert.AreEqual("/authors", Caption);
        }

        [TestMethod]
        public void FollowingTheSameNavigatorTwiceKeepsOneSubscription()
        {
            Assert.AreEqual(1, _component.FollowedCount);

            _component.FollowAgain();
            _component.FollowAgain();

            Assert.AreEqual(1, _component.FollowedCount);
        }

        [TestMethod]
        public void FollowingNothingIsANoOp()
        {
            _component.FollowNothing();

            Assert.AreEqual(1, _component.FollowedCount);
        }

    }
}
