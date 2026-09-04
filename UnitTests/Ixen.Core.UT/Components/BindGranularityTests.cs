using Ixen.Core.Components;
using Ixen.Core.UT.Components.Fixtures;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Components
{
    [TestClass]
    public class BindGranularityTests
    {
        private const int VIEWPORT = 400;

        private GranularityComponent _parent;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _parent = new GranularityComponent();

            var root = new VisualElement { Name = "root" };

            root.AddChild(_parent.Initialize());

            _surface = new IxenSurface(root) { Styles = new StyleRegistry() };
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private void Frame()
        {
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        [TestMethod]
        public void AChildIsReachableThroughItsElement()
        {
            Assert.IsNotNull(_parent.Child("first"));
            Assert.IsNotNull(_parent.Child("second"));
            Assert.AreNotSame(_parent.Child("first"), _parent.Child("second"));
        }

        [TestMethod]
        public void AStateChangeInAChildDoesNotReplayTheParentsBindings()
        {
            int before = _parent.CaptionReads;

            ((IBoundModel)_parent.Child("first")).SetState();

            Frame();

            Assert.AreEqual(before, _parent.CaptionReads,
                "this is the whole answer to a view with hundreds of bindings: a component binds "
                + "only its own view, so splitting a screen into components partitions Bind by "
                + "construction and no smarter replay is needed");
        }

        [TestMethod]
        public void NorTheOtherChildsRender()
        {
            CounterComponent first = _parent.Child("first");
            CounterComponent second = _parent.Child("second");

            int before = second.Renders;

            ((IBoundModel)first).SetState();

            Frame();

            Assert.AreEqual(before, second.Renders,
                "a sibling is as isolated as the parent - RenderIfDirty is asked of every Owner "
                + "and only the dirty one answers");
        }

        [TestMethod]
        public void TheChildThatChangedIsTheOneThatRenders()
        {
            CounterComponent first = _parent.Child("first");
            int before = first.Renders;

            ((IBoundModel)first).SetState();

            Frame();

            Assert.AreEqual(before + 1, first.Renders,
                "and it does render, or the isolation above would be indistinguishable from "
                + "nothing happening at all");
        }

        [TestMethod]
        public void TheParentsOwnStateChangeReplaysItsBindings()
        {
            int before = _parent.CaptionReads;

            ((IBoundModel)_parent).SetState();

            Frame();

            Assert.IsTrue(_parent.CaptionReads > before,
                "the counter-case: a parent that genuinely changed does reassign everything it "
                + "declares, which is what makes the replay correct without observing the model");
        }
    }
}
