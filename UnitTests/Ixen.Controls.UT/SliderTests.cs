using Ixen.Core;
using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class SliderTests
    {
        private const int VIEWPORT = 400;

        private VisualElement _root;
        private Slider _slider;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            var page = new VisualElement { Name = "page" };

            page.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            page.Styles.Padding = new PaddingStyleDescriptor
            {
                Left = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 },
                Top = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 12 }
            };

            _slider = new Slider { Name = "volume" };
            _slider.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _slider.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 };

            page.AddChild(_slider);
            _root.AddChild(page);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private VisualElement Part(string typeName)
            => _slider.ChildElements.Single(c => c.TypeName == typeName);

        [TestMethod]
        public void ItStartsAtItsMinimum()
        {
            Assert.AreEqual(0f, _slider.Value);
            Assert.AreEqual(0f, _slider.Fraction);
        }

        [TestMethod]
        public void PressingTheTrackJumpsToThatPoint()
        {
            int changes = 0;
            _slider.ValueChanged += (sender, e) => changes++;

            _surface.PointerDown(_slider.ContentX + 100, _slider.Y + 10, PointerButton.Left);

            Assert.AreEqual(50f, _slider.Value,
                "half way across a 200 wide slider on a 0..100 range - which needs the control "
                + "to know where its own content box is, and that was internal until now");
            Assert.AreEqual(1, changes);
        }

        [TestMethod]
        public void DraggingFollowsThePointer()
        {
            _surface.PointerDown(_slider.ContentX + 20, _slider.Y + 10, PointerButton.Left);
            _surface.PointerMove(_slider.ContentX + 150, _slider.Y + 10);

            Assert.AreEqual(75f, _slider.Value, "capture is implicit, so no drag plumbing is needed");
        }

        [TestMethod]
        public void ADragOutsideTheTrackClampsRatherThanOverruns()
        {
            _surface.PointerDown(_slider.ContentX + 20, _slider.Y + 10, PointerButton.Left);
            _surface.PointerMove(_slider.ContentX + 900, _slider.Y + 10);

            Assert.AreEqual(100f, _slider.Value);

            _surface.PointerMove(_slider.ContentX - 900, _slider.Y + 10);

            Assert.AreEqual(0f, _slider.Value);
        }

        [TestMethod]
        public void ItSnapsToTheStep()
        {
            _slider.Step = 25;

            _surface.PointerDown(_slider.ContentX + 118, _slider.Y + 10, PointerButton.Left);

            Assert.AreEqual(50f, _slider.Value, "59 rounds to the nearest 25");
        }

        [TestMethod]
        public void AZeroStepIsContinuous()
        {
            _slider.Step = 0;

            _surface.PointerDown(_slider.ContentX + 118, _slider.Y + 10, PointerButton.Left);

            Assert.AreEqual(59f, _slider.Value, 0.001f,
                "a continuous slider carries the pointer's own float, so it is compared with a "
                + "tolerance where a snapped one is exact");
        }

        [TestMethod]
        public void TheArrowsAndTheEndsMoveIt()
        {
            _surface.Focus(_slider);

            _surface.KeyDown(Key.Right, KeyModifiers.None);
            Assert.AreEqual(1f, _slider.Value);

            _surface.KeyDown(Key.Left, KeyModifiers.None);
            Assert.AreEqual(0f, _slider.Value, "and it stops at the minimum");

            _surface.KeyDown(Key.PageUp, KeyModifiers.None);
            Assert.AreEqual(10f, _slider.Value);

            _surface.KeyDown(Key.End, KeyModifiers.None);
            Assert.AreEqual(100f, _slider.Value);

            _surface.KeyDown(Key.Home, KeyModifiers.None);
            Assert.AreEqual(0f, _slider.Value);
        }

        [TestMethod]
        public void ThePartsMoveWithTheValue()
        {
            _slider.Value = 40;

            Assert.AreEqual(40f, Part(Slider.FILL).Styles.Width.Value,
                "the fill and the thumb are placed in PERCENT, so neither the control nor the "
                + "stylesheet has to know how wide the slider ended up");
            Assert.AreEqual(40f, Part(Slider.THUMB).Styles.Left.Value);
        }

        [TestMethod]
        public void AnAssignmentIsNotAnInteraction()
        {
            int changes = 0;
            _slider.ValueChanged += (sender, e) => changes++;

            _slider.Value = 30;

            Assert.AreEqual(30f, _slider.Value);
            Assert.AreEqual(0, changes, "the two-way contract, for the fourth control");
        }

        [TestMethod]
        public void ARangeThatMovesTakesTheValueWithIt()
        {
            _slider.Value = 80;
            _slider.Maximum = 50;

            Assert.AreEqual(50f, _slider.Value, "the value cannot be left outside its own range");
        }

        [TestMethod]
        public void ADisabledSliderDoesNotMove()
        {
            _slider.Enabled = false;

            _surface.PointerDown(_slider.ContentX + 100, _slider.Y + 10, PointerButton.Left);

            Assert.AreEqual(0f, _slider.Value);
        }

        [TestMethod]
        public void ItReportsItsValueAndNoneOfItsParts()
        {
            _slider.Value = 42;
            _slider.Label = "volume";
            Layout();

            AccessibleNode node = _surface.BuildAccessibilityTree()
                .Children.Single(c => c.Role == AccessibleRole.Slider);

            Assert.AreEqual("volume", node.Name);
            Assert.AreEqual("42", node.Value,
                "a slider has no text to put its value in, and putting one there would draw it "
                + "over the thumb - AccessibleValue is how a control states a value it does "
                + "not display");
            Assert.AreEqual(0, node.Children.Count, "the track, the fill and the thumb are decoration");
        }

        [TestMethod]
        public void ItIsNotAssumedToSitAtTheOriginOfTheWindow()
        {
            Assert.AreEqual(40f, _slider.ContentX,
                "the fixture offsets it on purpose - with the slider at x 0 every one of these "
                + "tests passes whether the pointer is made local to it or not");
        }
    }
}
