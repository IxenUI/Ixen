using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Shapes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class PathRenderTests
    {
        private const int SIZE = 40;
        private const int BOX = 20;

        private VisualElement _root;
        private Path _path;
        private IxenSurface _surface;
        private SKBitmap _bitmap;
        private SKCanvas _canvas;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };
            _root.Styles.Background = new BackgroundStyleDescriptor { Color = "#FFFFFF" };

            _path = new Path { Name = "shape", Data = "M 0 0 H 20 V 20 H 0 Z", ViewBox = "0 0 20 20" };
            _path.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _path.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _path.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = BOX };
            _path.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = BOX };

            _root.AddChild(_path);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };

            _bitmap = new SKBitmap(SIZE, SIZE);
            _canvas = new SKCanvas(_bitmap);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _canvas?.Dispose();
            _bitmap?.Dispose();
        }

        private void Frame()
        {
            _surface.ComputeLayout(SIZE, SIZE);
            _surface.Render(_canvas);
        }

        private SKColor At(int x, int y) => _bitmap.GetPixel(x, y);

        private static bool IsInk(SKColor color)
            => color.Red < 80 && color.Green < 80 && color.Blue < 80;

        private static bool IsBlue(SKColor color)
            => color.Blue > 180 && color.Red < 90;

        private static bool IsPaper(SKColor color)
            => color.Red > 200 && color.Green > 200 && color.Blue > 200;

        [TestMethod]
        public void APathIsFilledInTheForegroundColour()
        {
            Frame();

            Assert.IsTrue(IsInk(At(10, 10)), "the square is painted");
            Assert.IsTrue(IsPaper(At(30, 30)), "and nothing outside its box is");
        }

        [TestMethod]
        public void TheColourIsInheritedLikeTextIs()
        {
            _root.Styles.Color = new ColorStyleDescriptor { Value = "#0000FF" };
            _root.Invalidate();

            Frame();

            Assert.IsTrue(IsBlue(At(10, 10)),
                "colour is inherited, which is what makes an icon follow the row it sits in");
        }

        [TestMethod]
        public void ADeclaredColourOnThePathItselfWins()
        {
            _root.Styles.Color = new ColorStyleDescriptor { Value = "#0000FF" };
            _path.Styles.Color = new ColorStyleDescriptor { Value = "#000000" };
            _root.Invalidate();

            Frame();

            Assert.IsTrue(IsInk(At(10, 10)));
        }

        [TestMethod]
        public void AStrokeDrawsTheOutlineRatherThanTheInside()
        {
            _path.Data = "M 4 4 H 16 V 16 H 4 Z";
            _path.StrokeWidth = 2;

            Frame();

            Assert.IsTrue(IsInk(At(4, 10)), "the left edge is drawn");
            Assert.IsTrue(IsPaper(At(10, 10)), "and the middle is left alone");
        }

        [TestMethod]
        public void WithNoStrokeTheSameOutlineIsSolid()
        {
            _path.Data = "M 4 4 H 16 V 16 H 4 Z";

            Frame();

            Assert.IsTrue(IsInk(At(10, 10)),
                "which is what says the stroke is the thing that hollowed it out");
        }

        [TestMethod]
        public void NonZeroFillsASubpathInsideAnother()
        {
            _path.Data = "M 0 0 H 20 V 20 H 0 Z M 5 5 H 15 V 15 H 5 Z";

            Frame();

            Assert.IsTrue(IsInk(At(10, 10)),
                "two subpaths wound the same way count twice, so the inside stays filled");
        }

        [TestMethod]
        public void EvenOddPunchesTheHole()
        {
            _path.Data = "M 0 0 H 20 V 20 H 0 Z M 5 5 H 15 V 15 H 5 Z";
            _path.FillRule = Path.EVEN_ODD;

            Frame();

            Assert.IsTrue(IsPaper(At(10, 10)), "the inner subpath is a hole");
            Assert.IsTrue(IsInk(At(2, 10)), "while the ring around it is still painted");
        }

        [TestMethod]
        public void AWideBoxCentresTheShapeRatherThanStretchingIt()
        {
            _path.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
            _path.Styles.ObjectFit = new ObjectFitStyleDescriptor { Value = ObjectFit.Contain };
            _path.Invalidate();

            Frame();

            Assert.IsTrue(IsInk(At(20, 10)), "the square sits in the middle of the wide box");
            Assert.IsTrue(IsPaper(At(2, 10)), "with paper on its left");
            Assert.IsTrue(IsPaper(At(37, 10)), "and on its right");
        }

        [TestMethod]
        public void ObjectPositionMovesIt()
        {
            _path.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
            _path.Styles.ObjectFit = new ObjectFitStyleDescriptor { Value = ObjectFit.Contain };
            _path.Styles.ObjectPosition = new ObjectPositionStyleDescriptor { X = 0, Y = 0.5f };
            _path.Invalidate();

            Frame();

            Assert.IsTrue(IsInk(At(2, 10)), "anchored left, the square starts at the box edge");
            Assert.IsTrue(IsPaper(At(37, 10)));
        }

        [TestMethod]
        public void AViewBoxOriginShowsADifferentPartOfTheSameData()
        {
            _path.Data = "M 0 0 H 10 V 10 H 0 Z";
            _path.ViewBox = "10 10 10 10";

            Frame();

            Assert.IsTrue(IsPaper(At(10, 10)),
                "the ink is at 0..10 and the window is at 10..20, so nothing shows");

            _path.ViewBox = "0 0 10 10";

            Frame();

            Assert.IsTrue(IsInk(At(10, 10)), "and moving the window back brings it into view");
        }

        [TestMethod]
        public void AVectorHasNoNaturalPixelSizeSoNoneMeansContain()
        {
            _path.Data = "M 0 0 H 4 V 4 H 0 Z";
            _path.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 10 };
            _path.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 10 };
            _path.Styles.ObjectFit = new ObjectFitStyleDescriptor { Value = ObjectFit.None };
            _path.Invalidate();

            Frame();

            Assert.IsTrue(IsInk(At(1, 1)),
                "a 20 by 20 canvas shown at its own size in a box of ten would push its "
                + "top-left corner out of view, which is meaningless for a shape that scales");
        }

        [TestMethod]
        public void AndSoDoesScaleDown()
        {
            _path.Data = "M 0 0 H 4 V 4 H 0 Z";
            _path.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 10 };
            _path.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 10 };
            _path.Styles.ObjectFit = new ObjectFitStyleDescriptor { Value = ObjectFit.ScaleDown };
            _path.Invalidate();

            Frame();

            Assert.IsTrue(IsInk(At(1, 1)));
        }

        [TestMethod]
        public void ARadiusClipsTheShapeLikeItClipsAPicture()
        {
            _path.Styles.CornerRadius = new CornerRadiusStyleDescriptor
            {
                TopLeft = BOX,
                TopRight = BOX,
                BottomLeft = BOX,
                BottomRight = BOX
            };
            _path.Invalidate();

            Frame();

            Assert.IsTrue(IsInk(At(10, 10)), "the middle of the disc is painted");
            Assert.IsTrue(IsPaper(At(1, 1)), "and the square's corner is cut away");
        }

        [TestMethod]
        public void TheWholeBoxAnswersThePointerAndNotOnlyTheInk()
        {
            _path.Data = "M 0 0 H 4 V 4 H 0 Z";

            Frame();

            Assert.AreSame(_path, _surface.HitTest(18, 18),
                "a shape is hit on its bounds like every other element, which is what makes an "
                + "icon inside a button clickable at its corners");
        }

        [TestMethod]
        public void TheNativePathIsBuiltOnceAndReusedEveryFrame()
        {
            Frame();

            object first = _path.Geometry.Native;

            Assert.IsNotNull(first);

            Frame();

            Assert.AreSame(first, _path.Geometry.Native,
                "a shape rebuilt per frame would allocate on the paint path");
        }

        [TestMethod]
        public void NothingIsDrawnForDataThatIsNotAPath()
        {
            _path.Data = "wobble";

            Frame();

            Assert.IsTrue(IsPaper(At(10, 10)));
        }

        [TestMethod]
        public void ATransformCarriesThePathWithIt()
        {
            _path.Data = "M 0 0 H 10 V 20 H 0 Z";
            _path.Styles.Transform = new TransformStyleDescriptor();
            _path.Styles.Transform.Operations.Add(new TransformOperation
            {
                Kind = TransformKind.Translate,
                XUnit = SizeUnit.Pixels,
                X = 10
            });
            _path.Invalidate();

            Frame();

            Assert.IsTrue(IsPaper(At(2, 10)), "the half-width bar has left the left edge");
            Assert.IsTrue(IsInk(At(14, 10)), "and landed ten units to the right");
        }
    }
}
