using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class TransformTests
    {
        private const int VIEWPORT = 200;
        private const int SIDE = 40;

        private VisualElement _root;
        private VisualElement _box;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _root.Styles.Background = new BackgroundStyleDescriptor { Color = "#FFFFFF" };

            _box = Black("box", SIDE, SIDE);

            _root.AddChild(_box);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
        }

        private static VisualElement Black(string name, float width, float height)
        {
            var element = new VisualElement { Name = name };

            element.Styles.Background = new BackgroundStyleDescriptor { Color = "#000000" };
            element.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = width };
            element.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };

            return element;
        }

        private static void Transform(VisualElement element, string value)
        {
            var parser = new XnsSource($"probe {{ transform: {value} }}");
            ClassesSet set = parser.Compile();

            Assert.IsFalse(parser.HasErrors, string.Join(" | ", parser.Diagnostics.Select(d => d.Message)));

            element.Styles.Transform = (TransformStyleDescriptor)set.Classes.Single().Styles.Single();
            element.Invalidate();
        }

        private static void Origin(VisualElement element, string value)
        {
            var parser = new XnsSource($"probe {{ transform-origin: {value} }}");
            ClassesSet set = parser.Compile();

            Assert.IsFalse(parser.HasErrors, string.Join(" | ", parser.Diagnostics.Select(d => d.Message)));

            element.Styles.TransformOrigin =
                (TransformOriginStyleDescriptor)set.Classes.Single().Styles.Single();

            element.Invalidate();
        }

        private SKBitmap Render()
        {
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            return _surface.RenderToBitmap();
        }

        private static bool IsBlack(SKBitmap bitmap, int x, int y)
            => bitmap.GetPixel(x, y).Red < 128;

        [TestMethod]
        public void ATranslationMovesThePaintingAndNotTheBox()
        {
            Transform(_box, "translate(100px 100px)");

            using (SKBitmap rendered = Render())
            {
                Assert.IsFalse(IsBlack(rendered, 10, 10), "nothing is left where the box was laid out");
                Assert.IsTrue(IsBlack(rendered, 110, 110), "and it paints where the transform put it");
            }

            Assert.AreEqual(0f, _box.X, "layout is untouched, exactly as in CSS");
            Assert.AreEqual(0f, _box.Y);
            Assert.AreEqual((float)SIDE, _box.ActualWidth);
        }

        [TestMethod]
        public void HitTestingFollowsTheTransform()
        {
            Transform(_box, "translate(100px 100px)");

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(_box, _surface.HitTest(110, 110),
                "the pointer is mapped through the inverse matrix, so a click lands where the eye is");

            Assert.AreEqual(_root, _surface.HitTest(10, 10),
                "and the box no longer answers from where it was laid out");
        }

        [TestMethod]
        public void AScaleGrowsFromTheCentreByDefault()
        {
            Transform(_box, "scale(2)");

            using (SKBitmap rendered = Render())
            {
                Assert.IsTrue(IsBlack(rendered, 50, 50),
                    "the 40x40 box scaled about its centre reaches 60, so 50 is inside");
            }

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            Assert.AreEqual(_box, _surface.HitTest(50, 50));
        }

        [TestMethod]
        public void TheOriginDecidesWhereAScaleGrowsFrom()
        {
            Transform(_box, "scale(2)");

            using (SKBitmap centred = Render())
            {
                Assert.IsFalse(IsBlack(centred, 70, 70),
                    "about the centre the box stops at 60");
            }

            Origin(_box, "left top");

            using (SKBitmap corner = Render())
            {
                Assert.IsTrue(IsBlack(corner, 70, 70),
                    "about the top-left corner the same scale reaches 80");
            }
        }

        [TestMethod]
        public void ARotationTurnsTheShapeAndItsHitArea()
        {
            Transform(_box, "rotate(45deg)");

            using (SKBitmap rendered = Render())
            {
                Assert.IsTrue(IsBlack(rendered, 44, 20),
                    "a 40x40 square turned 45 degrees reaches past its own right edge");

                Assert.IsFalse(IsBlack(rendered, 4, 4),
                    "and its former top-left corner is now outside the shape");
            }

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(_box, _surface.HitTest(44, 20));
            Assert.AreEqual(_root, _surface.HitTest(4, 4));
        }

        [TestMethod]
        public void TheOrderOfTheFunctionsMatters()
        {
            Transform(_box, "translate(20px) scale(2)");

            using (SKBitmap first = Render())
            {
                Assert.IsTrue(IsBlack(first, 10, 10),
                    "translate then scale leaves the left edge at 0");
            }

            Transform(_box, "scale(2) translate(20px)");

            using (SKBitmap second = Render())
            {
                Assert.IsFalse(IsBlack(second, 10, 10),
                    "scale then translate doubles the translation, so the left edge is at 20");
            }
        }

        [TestMethod]
        public void APercentageTranslationIsRelativeToTheElement()
        {
            Transform(_box, "translate(100%)");

            using (SKBitmap rendered = Render())
            {
                Assert.IsTrue(IsBlack(rendered, SIDE + 10, 10),
                    "100% of a 40 wide box is 40, so it sits exactly beside where it was");

                Assert.IsFalse(IsBlack(rendered, 10, 10));
            }
        }

        [TestMethod]
        public void ChildrenMoveWithTheirParentAndInheritNothing()
        {
            VisualElement child = Black("child", 10, 10);

            _box.Styles.Background = new BackgroundStyleDescriptor();
            _box.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _box.AddChild(child);

            Transform(_box, "translate(100px 100px)");

            using (SKBitmap rendered = Render())
            {
                Assert.IsTrue(IsBlack(rendered, 105, 105), "the child travels with its parent");
                Assert.IsFalse(IsBlack(rendered, 5, 5));
            }

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(child, _surface.HitTest(105, 105));

            Assert.IsFalse(child.StylesHandlers.Transform.Descriptor.IsDeclared,
                "the child is moved by its ancestor's matrix, not by a transform of its own");
        }

        [TestMethod]
        public void ADegenerateMatrixPaintsNothingAndIsNotHittable()
        {
            Transform(_box, "scale(0)");

            using (SKBitmap rendered = Render())
            {
                for (int y = 0; y < 60; y += 10)
                {
                    for (int x = 0; x < 60; x += 10)
                    {
                        Assert.IsFalse(IsBlack(rendered, x, y), $"nothing at {x},{y}");
                    }
                }
            }

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(_root, _surface.HitTest(20, 20),
                "a collapsed matrix cannot be inverted, so the element is skipped");
        }

        [TestMethod]
        public void ATransformedSubtreeIsNotCulledByItsUntransformedBounds()
        {
            var panel = new VisualElement { Name = "panel" };
            panel.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };
            panel.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            panel.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };

            VisualElement far = Black("far", SIDE, SIDE);
            far.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };

            panel.AddChild(far);

            _root.RemoveChild(_box);
            _root.AddChild(panel);

            using (SKBitmap before = Render())
            {
                Assert.IsFalse(IsBlack(before, 10, 10), "laid out past its parent, it is clipped away");
            }

            Assert.IsTrue(far.Clip.IsVoidOrInvalid, "and its plain intersection is genuinely void");

            Transform(far, "translate(-200px)");

            using (SKBitmap after = Render())
            {
                Assert.IsTrue(IsBlack(after, 10, 10),
                    "the precomputed clip is only a culling bound, so a transformed subtree keeps "
                    + "its ancestor's clip instead of a rect the matrix has made meaningless");
            }
        }

        [TestMethod]
        public void ATransformTakesNoLayoutSpaceFromItsSiblings()
        {
            VisualElement sibling = Black("sibling", SIDE, SIDE);
            _root.AddChild(sibling);

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            float before = sibling.Y;

            Transform(_box, "scale(3) translate(50px)");
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(before, sibling.Y, "the sibling has not noticed");
            Assert.AreEqual((float)SIDE, _box.ActualHeight);
        }

        [TestMethod]
        public void ATransformCombinesWithAnOpacityLayerAndBalancesTheStack()
        {
            _box.Styles.Opacity = new OpacityStyleDescriptor { Value = 0.5f };
            Transform(_box, "translate(100px 100px)");

            VisualElement sibling = Black("sibling", SIDE, SIDE);
            _root.AddChild(sibling);

            using (SKBitmap rendered = Render())
            {
                int faded = rendered.GetPixel(110, 110).Red;

                Assert.IsTrue(faded > 110 && faded < 145, $"the moved box is half transparent, got {faded}");
                Assert.IsTrue(IsBlack(rendered, 10, 50), "and the sibling after it is fully opaque");
            }

            _root.Invalidate();

            using (SKBitmap again = Render())
            {
                Assert.IsTrue(IsBlack(again, 10, 50),
                    "a transform and a layer share one save stack, so EndFrame unwinds both");
            }
        }

        [TestMethod]
        public void AnOverlayCarriesItsOwnTransform()
        {
            var layer = new VisualElement { Name = "layer" };
            layer.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Fixed };
            layer.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            layer.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };

            VisualElement sheet = Black("sheet", SIDE, SIDE);
            sheet.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            sheet.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };

            layer.AddChild(sheet);
            _root.RemoveChild(_box);
            _root.AddChild(layer);

            Transform(layer, "translate(120px 120px)");

            using (SKBitmap rendered = Render())
            {
                Assert.IsTrue(IsBlack(rendered, 130, 130), "the layer's children move with it");
                Assert.IsFalse(IsBlack(rendered, 10, 10));
            }

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(sheet, _surface.HitTest(130, 130),
                "and the overlay maps the pointer once before walking its children");
        }

        [TestMethod]
        public void TheMatrixConcatenatesInTheOrderItIsRead()
        {
            Matrix2D moved = Matrix2D.Concat(Matrix2D.Translation(10, 0), Matrix2D.Scaling(2, 2));

            moved.Map(5, 0, out float x, out float y);

            Assert.AreEqual(20f, x, "translate then scale maps 5 to 5 * 2 + 10");
            Assert.AreEqual(0f, y);

            Matrix2D other = Matrix2D.Concat(Matrix2D.Scaling(2, 2), Matrix2D.Translation(10, 0));

            other.Map(5, 0, out float otherX, out _);

            Assert.AreEqual(30f, otherX, "the other way round the translation is scaled too");
        }

        [TestMethod]
        public void AnInvertedMatrixRoundTripsAndACollapsedOneFails()
        {
            Matrix2D matrix = Matrix2D.Concat(
                Matrix2D.Translation(17, -4),
                Matrix2D.Concat(Matrix2D.Rotation(31), Matrix2D.Scaling(1.5f, 0.8f)));

            Assert.IsTrue(matrix.TryInvert(out Matrix2D inverted));

            matrix.Map(12, 9, out float x, out float y);
            inverted.Map(x, y, out float backX, out float backY);

            Assert.AreEqual(12f, backX, 0.001f);
            Assert.AreEqual(9f, backY, 0.001f);

            Assert.IsFalse(Matrix2D.Scaling(0, 1).TryInvert(out _));
            Assert.IsTrue(Matrix2D.Identity.IsIdentity);
        }
    }
}
