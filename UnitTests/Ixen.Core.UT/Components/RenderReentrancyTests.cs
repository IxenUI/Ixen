using Ixen.Core.Components;
using Ixen.Core.Visual;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Ixen.Core.UT.Components
{
    [TestClass]
    public class RenderReentrancyTests
    {
        private class LoopingComponent : Component<VisualElement>
        {
            internal int Renders;

            protected override void Render()
            {
                Renders++;
                SetState();
            }
        }

        private class LoopingWithActionComponent : Component<VisualElement>
        {
            internal int Value;

            protected override void Render()
            {
                SetState(() => Value = 1);
            }
        }

        private class LateLoopComponent : Component<VisualElement>
        {
            internal bool Loop;

            internal void Poke() => SetState();

            protected override void Render()
            {
                if (Loop)
                {
                    SetState();
                }
            }
        }

        private class QuietComponent : Component<VisualElement>
        {
            internal int Renders;

            internal void Poke() => SetState();

            protected override void Render() => Renders++;
        }

        [TestMethod]
        public void SetStateInsideRenderThrowsInsteadOfRepaintingForever()
        {
            var component = new LoopingComponent();

            Assert.Throws<InvalidOperationException>(() => component.Initialize());
            Assert.AreEqual(1, component.Renders, "it must not have looped before reporting");
        }

        [TestMethod]
        public void TheMessageNamesTheComponent()
        {
            var exception = Assert.Throws<InvalidOperationException>(
                () => new LoopingComponent().Initialize());

            StringAssert.Contains(exception.Message, nameof(LoopingComponent));
        }

        [TestMethod]
        public void TheActionOverloadReportsBeforeApplyingTheChange()
        {
            var component = new LoopingWithActionComponent();

            Assert.Throws<InvalidOperationException>(() => component.Initialize());
            Assert.AreEqual(0, component.Value,
                "the guard runs first, so the state is not left half applied");
        }

        [TestMethod]
        public void ARenderTriggeredByAStateChangeIsGuardedToo()
        {
            var component = new LateLoopComponent();
            component.Initialize();

            component.Loop = true;
            component.Poke();

            Assert.Throws<InvalidOperationException>(() => component.RenderIfDirty());
        }

        [TestMethod]
        public void TheFlagIsReleasedAfterAThrowSoTheComponentStaysUsable()
        {
            var component = new LateLoopComponent();
            component.Initialize();

            component.Loop = true;
            component.Poke();

            try
            {
                component.RenderIfDirty();
            }
            catch (InvalidOperationException)
            {
            }

            component.Loop = false;
            component.Poke();
            component.RenderIfDirty();
        }

        [TestMethod]
        public void AnOrdinaryComponentIsUnaffected()
        {
            var component = new QuietComponent();
            component.Initialize();

            Assert.AreEqual(1, component.Renders);

            component.Poke();
            component.RenderIfDirty();

            Assert.AreEqual(2, component.Renders);
        }
    }
}
