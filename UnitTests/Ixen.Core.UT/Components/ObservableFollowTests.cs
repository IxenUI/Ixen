using Ixen.Core.Components;
using Ixen.Core.UT.Components.Fixtures;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Ixen.Core.UT.Components
{
    [TestClass]
    public class ObservableFollowTests
    {
        private const int VIEWPORT = 200;

        private VisualElement _root;
        private IxenSurface _surface;
        private ObservableList<string> _entries;
        private AsyncValue<string> _poem;
        private ObservableComponent _component;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement();
            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };

            _entries = new ObservableList<string>();
            _poem = new AsyncValue<string>();
            _component = Mount(true);

            Layout();
        }

        private ObservableComponent Mount(bool follow)
        {
            var component = new ObservableComponent(_entries, _poem, follow);

            _root.AddChild(component.Initialize());

            return component;
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private static VisualElement Body(ObservableComponent component)
            => component.View.ChildElements[0];

        private static int Rows(ObservableComponent component)
            => Body(component).ChildElements.Count - 1;

        private static string Caption(ObservableComponent component)
            => Body(component).ChildElements[0].Text;

        [TestMethod]
        public void AddingToAFollowedListRendersTheComponentAgain()
        {
            int before = _component.Renders;

            _entries.Add("the wild swans");
            Layout();

            Assert.AreEqual(before + 1, _component.Renders);
        }

        [TestMethod]
        public void TheLoopFollowsTheList()
        {
            Assert.AreEqual(0, Rows(_component));
            Assert.AreEqual("0 entries", Caption(_component));

            _entries.AddRange(new[] { "one", "two", "three" });
            Layout();

            Assert.AreEqual(3, Rows(_component));
            Assert.AreEqual("3 entries", Caption(_component));
            Assert.AreEqual("two", Body(_component).ChildElements[2].Text);

            _entries.RemoveAt(0);
            Layout();

            Assert.AreEqual(2, Rows(_component));
            Assert.AreEqual("two", Body(_component).ChildElements[1].Text);

            _entries.Clear();
            Layout();

            Assert.AreEqual(0, Rows(_component));
            Assert.AreEqual("0 entries", Caption(_component));
        }

        [TestMethod]
        public void TwoComponentsFollowingOneListBothRender()
        {
            ObservableComponent other = Mount(true);

            Layout();

            int first = _component.Renders;
            int second = other.Renders;

            _entries.Add("the wild swans");
            Layout();

            Assert.AreEqual(first + 1, _component.Renders, "the one that does not own the list");
            Assert.AreEqual(second + 1, other.Renders);
            Assert.AreEqual(1, Rows(_component));
            Assert.AreEqual(1, Rows(other));
        }

        [TestMethod]
        public void AComponentThatDoesNotFollowSeesNothing()
        {
            ObservableComponent quiet = Mount(false);

            Layout();

            int before = quiet.Renders;

            _entries.Add("the wild swans");
            Layout();

            Assert.AreEqual(before, quiet.Renders, "following is opt-in");
            Assert.AreEqual(0, quiet.FollowedCount);
        }

        [TestMethod]
        public void AChangeMissedWhileAwayIsCaughtUpOnTheWayBack()
        {
            _root.RemoveChild(_component.View);
            Layout();

            _entries.Add("the wild swans");

            _root.AddChild(_component.View);
            Layout();

            Assert.AreEqual(1, Rows(_component), "the follower came back showing what it had missed");
            Assert.AreEqual("1 entries", Caption(_component));
        }

        [TestMethod]
        public void ADetachedFollowerIsNotRendered()
        {
            _root.RemoveChild(_component.View);
            Layout();

            int before = _component.Renders;

            _entries.Add("the wild swans");
            Layout();

            Assert.AreEqual(before, _component.Renders);
        }

        [TestMethod]
        public void FollowingTheSameListTwiceKeepsOneSubscription()
        {
            Assert.AreEqual(1, _component.FollowedCount);

            _component.FollowAgain();
            _component.FollowAgain();

            Assert.AreEqual(1, _component.FollowedCount);

            int before = _component.Renders;

            _entries.Add("the wild swans");
            Layout();

            Assert.AreEqual(before + 1, _component.Renders, "one change, one render");
        }

        [TestMethod]
        public void FollowingAnAsyncValueRendersOnEachState()
        {
            _component.FollowThePoem();

            Assert.AreEqual(2, _component.FollowedCount);

            int before = _component.Renders;

            int generation = _poem.Begin();
            Layout();

            Assert.AreEqual(before + 1, _component.Renders);
            Assert.IsTrue(_poem.IsLoading);

            _poem.Succeed(generation, "the wild swans");
            Layout();

            Assert.AreEqual(before + 2, _component.Renders);
            Assert.AreEqual("the wild swans", _poem.Value);
        }

        [TestMethod]
        public void ASupersededAnswerAnnouncesNothing()
        {
            _component.FollowThePoem();

            int stale = _poem.Begin();

            _poem.Begin();
            Layout();

            int before = _component.Renders;

            Assert.IsFalse(_poem.Succeed(stale, "too late"));
            Assert.IsFalse(_poem.Fail(stale, new InvalidOperationException("too late")));

            Layout();

            Assert.AreEqual(before, _component.Renders, "a dropped answer is not a change");
        }

        [TestMethod]
        public void AFailureIsAChangeLikeAnyOther()
        {
            _component.FollowThePoem();

            int generation = _poem.Begin();

            Layout();

            int before = _component.Renders;

            _poem.Fail(generation, new InvalidOperationException("no"));
            Layout();

            Assert.AreEqual(before + 1, _component.Renders);
            Assert.AreEqual("no", _poem.Message);
        }

        [TestMethod]
        public void ResettingAnAsyncValueIsAChangeToo()
        {
            _component.FollowThePoem();

            _poem.Succeed(_poem.Begin(), "the wild swans");
            Layout();

            int before = _component.Renders;

            _poem.Reset();
            Layout();

            Assert.AreEqual(before + 1, _component.Renders);
            Assert.IsTrue(_poem.IsIdle);
        }

        [TestMethod]
        public void MutatingAFollowedListInsideRenderIsRefused()
        {
            _component.MutatesInRender = true;

            _entries.Add("the wild swans");

            Assert.Throws<InvalidOperationException>(() => Layout());
        }
    }
}
