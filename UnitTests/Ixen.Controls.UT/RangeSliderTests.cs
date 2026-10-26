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
    public class RangeSliderTests
    {
        private const int VIEWPORT = 400;

        private VisualElement _root;
        private RangeSlider _range;
        private IxenSurface _surface;
        private int _lowerChanges;
        private int _upperChanges;

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

            _range = new RangeSlider { Name = "price", Label = "price" };
            _range.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _range.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 };
            _range.LowerChanged += (sender, e) => _lowerChanges++;
            _range.UpperChanged += (sender, e) => _upperChanges++;

            page.AddChild(_range);
            _root.AddChild(page);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private float At(float fraction) => _range.ContentX + (_range.ContentWidth * fraction);

        private void Press(float fraction) => _surface.PointerDown(At(fraction), _range.Y + 10, PointerButton.Left);

        private void Release(float fraction) => _surface.PointerUp(At(fraction), _range.Y + 10, PointerButton.Left);

        private AccessibleNode Node(VisualElement element)
            => AccessibleSearch.Find(_surface.BuildAccessibilityTree(), node => node.Element == element);

        [TestMethod]
        public void ItStartsAcrossTheWholeRange()
        {
            Assert.AreEqual(0f, _range.Lower);
            Assert.AreEqual(100f, _range.Upper);
        }

        [TestMethod]
        public void TheOrderOfAssignmentDoesNotMatter()
        {
            _range.Lower = 150;
            _range.Upper = 180;
            _range.Maximum = 200;

            Assert.AreEqual(150f, _range.Lower,
                "a generated view may assign lower before maximum, and the value must survive it");
            Assert.AreEqual(180f, _range.Upper);
        }

        [TestMethod]
        public void TheUpperEndNeverFallsBelowTheLower()
        {
            _range.Lower = 70;
            _range.Upper = 30;

            Assert.AreEqual(70f, _range.Lower);
            Assert.AreEqual(70f, _range.Upper);
        }

        [TestMethod]
        public void PressingTheTrackMovesTheNearerThumb()
        {
            _range.Lower = 20;
            _range.Upper = 80;
            Layout();

            Press(0.3f);
            Release(0.3f);

            Assert.AreEqual(30f, _range.Lower);
            Assert.AreEqual(80f, _range.Upper);
            Assert.AreSame(_range.LowerThumb, _surface.FocusedElement, "and that thumb takes the focus");

            Press(0.9f);
            Release(0.9f);

            Assert.AreEqual(90f, _range.Upper);
            Assert.AreSame(_range.UpperThumb, _surface.FocusedElement);
            Assert.AreEqual(1, _lowerChanges);
            Assert.AreEqual(1, _upperChanges);
        }

        [TestMethod]
        public void ADraggedThumbStopsAtTheOther()
        {
            _range.Lower = 20;
            _range.Upper = 60;
            Layout();

            Press(0.2f);
            _surface.PointerMove(At(0.9f), _range.Y + 10);

            Assert.AreEqual(60f, _range.Lower, "the lower thumb cannot pass the upper one");
            Assert.AreEqual(60f, _range.Upper, "and does not push it");
        }

        [TestMethod]
        public void TheArrowsMoveTheFocusedThumbWithinItsBounds()
        {
            _range.Lower = 20;
            _range.Upper = 60;
            _range.Step = 5;
            Layout();

            _surface.Focus(_range.UpperThumb);
            _surface.KeyDown(Key.Left, KeyModifiers.None);

            Assert.AreEqual(55f, _range.Upper);

            _surface.KeyDown(Key.Home, KeyModifiers.None);

            Assert.AreEqual(20f, _range.Upper, "Home takes the upper thumb down to the lower one, no further");

            _surface.Focus(_range.LowerThumb);
            _surface.KeyDown(Key.Home, KeyModifiers.None);
            _surface.KeyDown(Key.End, KeyModifiers.None);

            Assert.AreEqual(20f, _range.Lower, "End takes the lower thumb up to the upper one");
        }

        [TestMethod]
        public void TheFillSpansBetweenTheThumbs()
        {
            _range.Lower = 20;
            _range.Upper = 80;
            Layout();

            VisualElement fill = _range.ChildElements.Single(c => c.TypeName == Slider.FILL);

            Assert.AreEqual(At(0.2f), fill.X, 0.5f);
            Assert.AreEqual(_range.ContentWidth * 0.6f, fill.ActualWidth, 0.5f);
        }

        [TestMethod]
        public void AnAssignmentIsNotAnInteraction()
        {
            _range.Lower = 10;
            _range.Upper = 90;

            Assert.AreEqual(0, _lowerChanges + _upperChanges);
        }

        [TestMethod]
        public void ADisabledRangeDoesNotMove()
        {
            _range.Enabled = false;
            Layout();

            Press(0.4f);

            Assert.AreEqual(0f, _range.Lower);
        }

        [TestMethod]
        public void AScreenReaderSeesTwoSlidersBoundedByEachOther()
        {
            _range.Lower = 20;
            _range.Upper = 80;
            _range.LowerLabel = "from";
            _range.UpperLabel = "to";
            Layout();

            AccessibleNode group = Node(_range);
            AccessibleNode lower = Node(_range.LowerThumb);
            AccessibleNode upper = Node(_range.UpperThumb);

            Assert.AreEqual(AccessibleRole.Group, group.Role);
            Assert.AreEqual("price", group.Name);
            Assert.AreEqual("from", lower.Name);
            Assert.AreEqual(0f, lower.RangeMinimum);
            Assert.AreEqual(80f, lower.RangeMaximum, "the lower thumb can go up to the upper one");
            Assert.AreEqual(20f, lower.RangeValue);
            Assert.AreEqual(20f, upper.RangeMinimum);
            Assert.AreEqual(100f, upper.RangeMaximum);

            Assert.IsTrue(_surface.Perform(upper, AccessibleActions.SetRangeValue, "50"));

            Assert.AreEqual(50f, _range.Upper);
            Assert.AreEqual(1, _upperChanges, "a reader's change is a user's change");
        }

        [TestMethod]
        public void AVerticalRangeGrowsUpward()
        {
            _range.Vertical = true;
            _range.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 };
            _range.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _range.Lower = 25;
            _range.Upper = 75;
            _range.Invalidate();
            Layout();

            Assert.IsTrue(_range.LowerThumb.Y > _range.UpperThumb.Y, "the lower end is at the bottom");
        }

        [TestMethod]
        public void TheSlidersThemeDressesIt()
        {
            var registry = new StyleRegistry();

            registry.AddLoadedDefaults();
            _surface.Styles = registry;
            _range.Styles.Width = new WidthStyleDescriptor();
            _range.Styles.Height = new HeightStyleDescriptor();
            Layout();

            Assert.AreEqual(176f, _range.ActualWidth, "the RangeSlider rule sizes it like a slider");
            Assert.AreEqual(16f, _range.LowerThumb.ActualWidth, "and its parts wear the slider's own rules");
            Assert.AreEqual(16f, _range.UpperThumb.ActualWidth);
        }
    }
}
