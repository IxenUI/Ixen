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

        private class WriteBackComponent : Component<VisualElement>
        {
            internal int Renders;
            internal bool Rendering = true;
            internal bool Reenter;

            protected override void Render()
            {
                Renders++;

                if (Reenter)
                {
                    SetState();
                    return;
                }

                if (Rendering)
                {
                    ((IBoundModel)this).SetState();
                }
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
        public void AWriteBackDuringARenderSaysItWasAWriteBack()
        {
            var component = new WriteBackComponent();

            try
            {
                component.Initialize();
                Assert.Fail("the write-back should have been refused");
            }
            catch (InvalidOperationException error)
            {
                StringAssert.Contains(error.Message, "two-way binding wrote back");
                StringAssert.Contains(error.Message, "WriteBackComponent");
            }
        }

        [TestMethod]
        public void AnOrdinarySetStateStillSaysItWasASetState()
        {
            var component = new LoopingComponent();

            try
            {
                component.Initialize();
                Assert.Fail("the re-entrant SetState should have been refused");
            }
            catch (InvalidOperationException error)
            {
                StringAssert.Contains(error.Message, "SetState() was called while");
            }
        }

        [TestMethod]
        public void AWriteBackOutsideARenderIsOrdinary()
        {
            var component = new WriteBackComponent { Rendering = false };

            component.Initialize();
            ((IBoundModel)component).SetState();

            component.RenderIfDirty();

            Assert.AreEqual(2, component.Renders);
        }

        [TestMethod]
        public void AnEarlierWriteBackDoesNotColourALaterMistake()
        {
            var component = new WriteBackComponent { Rendering = false };

            component.Initialize();
            ((IBoundModel)component).SetState();
            component.RenderIfDirty();

            component.Reenter = true;
            ((IBoundModel)component).SetState();

            try
            {
                component.RenderIfDirty();
                Assert.Fail("the re-entrant SetState should have been refused");
            }
            catch (InvalidOperationException error)
            {
                StringAssert.Contains(error.Message, "SetState() was called while");
            }
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
