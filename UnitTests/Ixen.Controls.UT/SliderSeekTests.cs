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
    public class SliderSeekTests
    {
        private const int VIEWPORT = 400;

        private VisualElement _root;
        private Slider _slider;
        private IxenSurface _surface;
        private int _changes;
        private int _commits;

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

            _slider = new Slider { Name = "seek" };
            _slider.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _slider.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 };
            _slider.ValueChanged += (sender, e) => _changes++;
            _slider.ValueCommitted += (sender, e) => _commits++;

            page.AddChild(_slider);
            _root.AddChild(page);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private float X(float value) => _slider.ContentX + value * 2;

        private float Y => _slider.Y + 10;

        private VisualElement Part(string typeName) => _slider.ChildElements.Single(c => c.TypeName == typeName);

        [TestMethod]
        public void DraggingChangesAsItGoesAndCommitsOnceWhenReleased()
        {
            _surface.PointerDown(X(20), Y, PointerButton.Left);
            _surface.PointerMove(X(30), Y);
            _surface.PointerMove(X(60), Y);

            Assert.IsTrue(_slider.IsInteracting);
            Assert.AreEqual(3, _changes, "the press and two moves");
            Assert.AreEqual(0, _commits, "nothing is committed while the thumb is held");

            _surface.PointerUp(X(60), Y, PointerButton.Left);

            Assert.IsFalse(_slider.IsInteracting);
            Assert.AreEqual(1, _commits, "a player seeks here, once");
            Assert.AreEqual(60f, _slider.Value);
        }

        [TestMethod]
        public void AClickCommitsToo()
        {
            _surface.PointerDown(X(40), Y, PointerButton.Left);
            _surface.PointerUp(X(40), Y, PointerButton.Left);

            Assert.AreEqual(1, _commits);
        }

        [TestMethod]
        public void PressingWhereTheThumbAlreadyIsCommitsNothing()
        {
            _slider.Value = 40;

            _surface.PointerDown(X(40), Y, PointerButton.Left);
            _surface.PointerUp(X(40), Y, PointerButton.Left);

            Assert.AreEqual(0, _commits, "the value did not move, so there is nothing to seek to");
        }

        [TestMethod]
        public void PlaybackCannotPullTheThumbFromTheUsersHand()
        {
            _surface.PointerDown(X(20), Y, PointerButton.Left);
            _surface.PointerMove(X(70), Y);

            _slider.Value = 21;
            _slider.Value = 22;

            Assert.AreEqual(70f, _slider.Value, "the position the timer reports is ignored while held");

            _surface.PointerUp(X(70), Y, PointerButton.Left);

            Assert.AreEqual(70f, _slider.Value, "and the user's choice is what is committed");
            Assert.AreEqual(1, _commits);

            _slider.Value = 71;

            Assert.AreEqual(71f, _slider.Value, "once released, the timer moves it again");
        }

        [TestMethod]
        public void LosingTheCaptureEndsTheHold()
        {
            _surface.PointerDown(X(20), Y, PointerButton.Left);
            _surface.PointerMove(X(30), Y);
            _surface.PointerMove(X(50), Y);

            _surface.PointerCaptureLost();

            Assert.IsFalse(_slider.IsInteracting, "a window that loses the mouse mid-drag lets the thumb go");
            Assert.AreEqual(1, _commits);

            _slider.Value = 10;

            Assert.AreEqual(10f, _slider.Value);
        }

        [TestMethod]
        public void KeysAndScreenReadersCommitEachStep()
        {
            _surface.Focus(_slider);
            _surface.KeyDown(Key.Right, KeyModifiers.None);
            _surface.KeyDown(Key.PageUp, KeyModifiers.None);

            Assert.AreEqual(2, _commits);

            AccessibleNode node = Find(_surface.BuildAccessibilityTree(), "seek");
            _surface.Perform(node, AccessibleActions.SetRangeValue, "80");

            Assert.AreEqual(3, _commits);
            Assert.AreEqual(80f, _slider.Value);
        }

        private static AccessibleNode Find(AccessibleNode node, string name)
        {
            if (node.Element?.Name == name)
            {
                return node;
            }

            foreach (AccessibleNode child in node.Children)
            {
                AccessibleNode found = Find(child, name);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        [TestMethod]
        public void TheWheelStepsOnlyWhenAsked()
        {
            _slider.Value = 50;

            _surface.PointerWheel(X(50), Y, 0, 1);

            Assert.AreEqual(50f, _slider.Value, "by default the wheel scrolls the page, not the volume");

            _slider.AcceptsWheel = true;

            _surface.PointerWheel(X(50), Y, 0, 1);

            Assert.AreEqual(51f, _slider.Value, "a notch up is a step up");

            _surface.PointerWheel(X(50), Y, 0, -3);

            Assert.AreEqual(48f, _slider.Value);
            Assert.AreEqual(2, _commits);
        }

        [TestMethod]
        public void TheBufferedPartIsDrawnUnderTheFill()
        {
            VisualElement buffer = Part(Slider.BUFFER);

            Assert.AreEqual(0, buffer.Styles.Width.Value, "nothing buffered by default");

            _slider.Buffered = 75;
            _slider.Value = 30;
            Layout();

            Assert.AreEqual(75, buffer.Styles.Width.Value, 0.01f);
            Assert.AreEqual(30, Part(Slider.FILL).Styles.Width.Value, 0.01f);

            int bufferIndex = _slider.ChildElements.ToList().IndexOf(buffer);
            int fillIndex = _slider.ChildElements.ToList().IndexOf(Part(Slider.FILL));

            Assert.IsTrue(bufferIndex < fillIndex, "painted before the fill, so the fill covers it");

            _slider.Buffered = 500;
            Layout();

            Assert.AreEqual(100, buffer.Styles.Width.Value, 0.01f, "clamped to the range");

            _slider.Vertical = true;
            _slider.Buffered = 40;
            Layout();

            Assert.AreEqual(40, buffer.Styles.Height.Value, 0.01f, "a vertical slider buffers upward");
        }

        [TestMethod]
        public void TheBufferIsNotRead()
        {
            Assert.IsFalse(Find(_surface.BuildAccessibilityTree(), "seek").Children.Any(),
                "the parts are presentation: a screen reader hears the value");
        }
    }
}
