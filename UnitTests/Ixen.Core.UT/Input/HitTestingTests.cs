using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class HitTestingTests
    {
        private const int VIEWPORT = 200;

        private static VisualElement Element(string name, LayoutType layout = LayoutType.Column,
            SizeUnit widthUnit = SizeUnit.Unset, float widthValue = 1,
            SizeUnit heightUnit = SizeUnit.Unset, float heightValue = 1)
        {
            var element = new VisualElement { Name = name };
            element.Styles.Layout = new LayoutStyleDescriptor { Type = layout };
            element.Styles.Width = new WidthStyleDescriptor { Unit = widthUnit, Value = widthValue };
            element.Styles.Height = new HeightStyleDescriptor { Unit = heightUnit, Value = heightValue };
            return element;
        }

        private static VisualElement Box(string name, float width, float height)
            => Element(name, LayoutType.Column, SizeUnit.Pixels, width, SizeUnit.Pixels, height);

        private static IxenSurface Laid(VisualElement root)
        {
            var surface = new IxenSurface(root);
            surface.ComputeLayout(VIEWPORT, VIEWPORT);
            return surface;
        }

        private static string HitName(IxenSurface surface, float x, float y)
            => surface.HitTest(x, y)?.Name;

        [TestMethod]
        public void APointInsideTheRootHitsIt()
        {
            IxenSurface surface = Laid(Element("root"));

            Assert.AreEqual("root", HitName(surface, 100, 100));
        }

        [TestMethod]
        public void APointOutsideTheRootHitsNothing()
        {
            IxenSurface surface = Laid(Element("root"));

            Assert.IsNull(HitName(surface, -1, 100), "left of the root");
            Assert.IsNull(HitName(surface, 100, -1), "above the root");
            Assert.IsNull(HitName(surface, VIEWPORT, 100), "the right edge is exclusive");
            Assert.IsNull(HitName(surface, 100, VIEWPORT), "the bottom edge is exclusive");
        }

        [TestMethod]
        public void TheDeepestElementWins()
        {
            VisualElement root = Element("root");
            VisualElement middle = Box("middle", 100, 100);
            VisualElement leaf = Box("leaf", 40, 40);
            middle.AddChild(leaf);
            root.AddChild(middle);

            IxenSurface surface = Laid(root);

            Assert.AreEqual("leaf", HitName(surface, 20, 20));
            Assert.AreEqual("middle", HitName(surface, 60, 60));
            Assert.AreEqual("root", HitName(surface, 150, 150));
        }

        [TestMethod]
        public void APointInThePaddingHitsTheParent()
        {
            VisualElement root = Element("root");
            VisualElement panel = Box("panel", 100, 100);
            var padding = new PaddingStyleDescriptor();
            padding.Set(new SpaceStyleDescriptor
            {
                Top = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 },
                Right = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 },
                Bottom = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 },
                Left = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 }
            });
            panel.Styles.Padding = padding;
            panel.AddChild(Element("filler"));
            root.AddChild(panel);

            IxenSurface surface = Laid(root);

            Assert.AreEqual("panel", HitName(surface, 5, 50), "inside the left padding");
            Assert.AreEqual("filler", HitName(surface, 50, 50), "inside the content");
        }

        [TestMethod]
        public void TheLastChildIsOnTopWhereTheyOverlap()
        {
            VisualElement root = Element("root", LayoutType.Row);
            VisualElement first = Box("first", 180, 60);
            VisualElement second = Box("second", 60, 60);
            root.AddChildren(first, second);

            IxenSurface surface = Laid(root);

            Assert.AreEqual(180, second.X, "the second child starts after the first");
            Assert.AreEqual("second", HitName(surface, 190, 30),
                "the later sibling is painted on top, so it wins where they overlap");
        }

        [TestMethod]
        public void AChildIsNotHittableOutsideItsParentBounds()
        {
            VisualElement root = Element("root");
            VisualElement frame = Box("frame", 80, 80);
            frame.AddChild(Box("big", 160, 160));
            root.AddChild(frame);

            IxenSurface surface = Laid(root);

            Assert.AreEqual("big", HitName(surface, 40, 40), "inside the frame it is hittable");
            Assert.AreEqual("root", HitName(surface, 120, 120),
                "past the frame it is clipped away, so the root is hit instead");
        }

        [TestMethod]
        public void ARoundedCornerIsNotHit()
        {
            VisualElement root = Element("root");
            VisualElement card = Box("card", 100, 100);
            card.Styles.CornerRadius = new CornerRadiusStyleDescriptor
            {
                TopLeft = 30,
                TopRight = 30,
                BottomRight = 30,
                BottomLeft = 30
            };
            root.AddChild(card);

            IxenSurface surface = Laid(root);

            Assert.AreEqual("root", HitName(surface, 2, 2), "the top-left corner is cut away");
            Assert.AreEqual("root", HitName(surface, 98, 2), "the top-right corner too");
            Assert.AreEqual("root", HitName(surface, 98, 98), "and the bottom-right");
            Assert.AreEqual("root", HitName(surface, 2, 98), "and the bottom-left");
            Assert.AreEqual("card", HitName(surface, 50, 50), "the middle is still hit");
            Assert.AreEqual("card", HitName(surface, 50, 2), "the straight top edge is still hit");
        }

        [TestMethod]
        public void OnlyTheRoundedCornersAreCutNotTheWholeQuadrant()
        {
            VisualElement root = Element("root");
            VisualElement card = Box("card", 100, 100);
            card.Styles.CornerRadius = new CornerRadiusStyleDescriptor { TopLeft = 40 };
            root.AddChild(card);

            IxenSurface surface = Laid(root);

            Assert.AreEqual("root", HitName(surface, 3, 3), "outside the arc");
            Assert.AreEqual("card", HitName(surface, 30, 30), "inside the arc but within the corner box");
            Assert.AreEqual("card", HitName(surface, 1, 99), "the other corners are square");
        }

        [TestMethod]
        public void ARoundedParentCutsItsChildrenToo()
        {
            VisualElement root = Element("root");
            VisualElement card = Box("card", 100, 100);
            card.Styles.CornerRadius = new CornerRadiusStyleDescriptor { TopLeft = 30 };
            card.AddChild(Element("fill"));
            root.AddChild(card);

            IxenSurface surface = Laid(root);

            Assert.AreEqual("root", HitName(surface, 2, 2), "the child cannot be hit in the cut corner");
            Assert.AreEqual("fill", HitName(surface, 50, 50));
        }

        [TestMethod]
        public void AZeroSizedElementIsNotHittable()
        {
            VisualElement root = Element("root");
            root.AddChild(Box("empty", 0, 0));

            IxenSurface surface = Laid(root);

            Assert.AreEqual("root", HitName(surface, 0, 0));
        }

        [TestMethod]
        public void AnElementWithoutABackgroundIsStillHittable()
        {
            VisualElement root = Element("root");
            VisualElement invisible = Box("invisible", 60, 60);
            root.AddChild(invisible);

            IxenSurface surface = Laid(root);

            Assert.AreEqual("invisible", HitName(surface, 30, 30),
                "hit testing is geometric: no background is required");
        }

        [TestMethod]
        public void AMarginIsNotPartOfTheElement()
        {
            VisualElement root = Element("root");
            VisualElement spaced = Box("spaced", 60, 60);
            var margin = new MarginStyleDescriptor();
            margin.Set(new SpaceStyleDescriptor
            {
                Top = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 },
                Right = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 },
                Bottom = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 },
                Left = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 }
            });
            spaced.Styles.Margin = margin;
            root.AddChild(spaced);

            IxenSurface surface = Laid(root);

            Assert.AreEqual(20, spaced.X);
            Assert.AreEqual("root", HitName(surface, 5, 5), "the margin belongs to nobody");
            Assert.AreEqual("spaced", HitName(surface, 25, 25));
        }

        [TestMethod]
        public void HitTestingFollowsAReLayout()
        {
            VisualElement root = Element("root");
            VisualElement box = Box("box", 40, 40);
            root.AddChild(box);

            IxenSurface surface = Laid(root);

            Assert.AreEqual("root", HitName(surface, 80, 20));

            box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 120 };
            box.Invalidate();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("box", HitName(surface, 80, 20), "the new geometry is used");
        }

        [TestMethod]
        public void AGridCellIsHitByItsCoordinates()
        {
            VisualElement grid = Element("grid", LayoutType.Grid, SizeUnit.Pixels, 200, SizeUnit.Pixels, 200);
            var columns = new RowTemplateStyleDescriptor();
            columns.Value.Add(new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 });
            columns.Value.Add(new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 });
            grid.Styles.RowTemplate = columns;

            var rows = new ColumnTemplateStyleDescriptor();
            rows.Value.Add(new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 });
            grid.Styles.ColumnTemplate = rows;

            grid.AddChildren(Element("a"), Element("b"), Element("c"), Element("d"));

            IxenSurface surface = Laid(grid);

            Assert.AreEqual("a", HitName(surface, 50, 50));
            Assert.AreEqual("b", HitName(surface, 150, 50));
            Assert.AreEqual("c", HitName(surface, 50, 150));
            Assert.AreEqual("d", HitName(surface, 150, 150));
        }
    }
}
