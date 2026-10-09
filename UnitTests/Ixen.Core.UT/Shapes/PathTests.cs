using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Shapes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Ixen.Core.UT.Shapes
{
    [TestClass]
    public class PathTests
    {
        private const int VIEWPORT = 400;
        private const string TRIANGLE = "M 0 0 L 60 0 L 60 40 Z";

        private VisualElement _root;
        private Path _path;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _path = new Path { Name = "shape", Data = TRIANGLE };
            _root.AddChild(_path);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private void Size(SizeUnit widthUnit, float width, SizeUnit heightUnit, float height)
        {
            _path.Styles.Width = new WidthStyleDescriptor { Unit = widthUnit, Value = width };
            _path.Styles.Height = new HeightStyleDescriptor { Unit = heightUnit, Value = height };
            _path.Invalidate();
        }

        [TestMethod]
        public void BothAxesAutoGiveTheViewBoxSize()
        {
            Size(SizeUnit.Content, 0, SizeUnit.Content, 0);
            Layout();

            Assert.AreEqual(60f, _path.ActualWidth, "with no view box the ink's own bounds decide");
            Assert.AreEqual(40f, _path.ActualHeight);
        }

        [TestMethod]
        public void ADeclaredViewBoxBeatsTheInksOwnBounds()
        {
            _path.ViewBox = "0 0 24 24";
            Size(SizeUnit.Content, 0, SizeUnit.Content, 0);
            Layout();

            Assert.AreEqual(24f, _path.ActualWidth,
                "an icon set gives a canvas, and the canvas is what sits beside its siblings");
            Assert.AreEqual(24f, _path.ActualHeight);
        }

        [TestMethod]
        public void AFixedWidthMakesTheHeightFollowTheAspectRatio()
        {
            Size(SizeUnit.Pixels, 120, SizeUnit.Content, 0);
            Layout();

            Assert.AreEqual(120f, _path.ActualWidth);
            Assert.AreEqual(80f, _path.ActualHeight, "60x40 at 120 wide is 80 tall");
        }

        [TestMethod]
        public void AFixedHeightMakesTheWidthFollowTheAspectRatio()
        {
            Size(SizeUnit.Content, 0, SizeUnit.Pixels, 20);
            Layout();

            Assert.AreEqual(30f, _path.ActualWidth, "60x40 at 20 tall is 30 wide");
            Assert.AreEqual(20f, _path.ActualHeight);
        }

        [TestMethod]
        public void TwoFixedAxesSayWhatTheyMean()
        {
            Size(SizeUnit.Pixels, 200, SizeUnit.Pixels, 10);
            Layout();

            Assert.AreEqual(200f, _path.ActualWidth);
            Assert.AreEqual(10f, _path.ActualHeight);
        }

        [TestMethod]
        public void AStrokeWidensTheBoundsItIsDrawnOn()
        {
            _path.StrokeWidth = 4;
            Size(SizeUnit.Content, 0, SizeUnit.Content, 0);
            Layout();

            Assert.AreEqual(64f, _path.ActualWidth,
                "a stroke is centred on the outline, so half of it falls outside the ink");
            Assert.AreEqual(44f, _path.ActualHeight);
        }

        [TestMethod]
        public void ADeclaredViewBoxIsNotWidenedByAStroke()
        {
            _path.ViewBox = "0 0 24 24";
            _path.StrokeWidth = 2;
            Size(SizeUnit.Content, 0, SizeUnit.Content, 0);
            Layout();

            Assert.AreEqual(24f, _path.ActualWidth,
                "an icon set already leaves room for its own stroke inside its canvas");
        }

        [TestMethod]
        public void NoDataIsNoSize()
        {
            _path.Data = null;
            Size(SizeUnit.Content, 0, SizeUnit.Content, 0);
            Layout();

            Assert.AreEqual(0f, _path.ActualWidth);
            Assert.AreEqual(0f, _path.ActualHeight);
        }

        [TestMethod]
        public void DataThatIsNotAPathIsNoSizeRatherThanAThrow()
        {
            _path.Data = "wobble";
            Size(SizeUnit.Content, 0, SizeUnit.Content, 0);
            Layout();

            Assert.AreEqual(0f, _path.ActualWidth,
                "a bad path draws nothing; it is not worth taking an application down for");
        }

        [TestMethod]
        public void TheGeometryIsParsedOnceAndKeptUntilTheDataMoves()
        {
            object first = _path.Geometry;

            Assert.IsNotNull(first);
            Assert.AreSame(first, _path.Geometry, "asking twice parses once");

            _path.Data = "M 0 0 L 1 1";

            Assert.AreNotSame(first, _path.Geometry, "new data is new geometry");
        }

        [TestMethod]
        public void TheFillRuleIsPartOfTheGeometry()
        {
            Assert.IsFalse(_path.Geometry.EvenOdd);

            _path.FillRule = Path.EVEN_ODD;

            Assert.IsTrue(_path.Geometry.EvenOdd, "changing the rule re-parses");
        }

        [TestMethod]
        public void AFillRuleThatIsNotOneThrowsWhereItIsWritten()
        {
            Assert.ThrowsExactly<ArgumentException>(() => _path.FillRule = "wobble");
        }

        [TestMethod]
        public void AViewBoxThatIsNotFourNumbersThrowsWhereItIsWritten()
        {
            Assert.ThrowsExactly<ArgumentException>(() => _path.ViewBox = "0 0 24");
            Assert.ThrowsExactly<ArgumentException>(() => _path.ViewBox = "0 0 24 wobble");
            Assert.ThrowsExactly<ArgumentException>(() => _path.ViewBox = "0 0 0 24");
        }

        [TestMethod]
        public void ClearingTheViewBoxGoesBackToTheInksBounds()
        {
            _path.ViewBox = "0 0 24 24";
            Size(SizeUnit.Content, 0, SizeUnit.Content, 0);
            Layout();

            Assert.AreEqual(24f, _path.ActualWidth);

            _path.ViewBox = null;
            Layout();

            Assert.AreEqual(60f, _path.ActualWidth);
        }

        [TestMethod]
        public void ANegativeStrokeIsNoStroke()
        {
            _path.StrokeWidth = -4;

            Assert.AreEqual(0f, _path.StrokeWidth);
            Assert.IsFalse(_path.IsStroked);
        }

        [TestMethod]
        public void AViewBoxWithAnOriginShiftsWhatIsShownRatherThanItsSize()
        {
            _path.ViewBox = "10 10 20 20";
            Size(SizeUnit.Content, 0, SizeUnit.Content, 0);
            Layout();

            Assert.AreEqual(20f, _path.ActualWidth);
            Assert.AreEqual(10f, _path.ViewLeft);
            Assert.AreEqual(10f, _path.ViewTop);
        }
    }
}
