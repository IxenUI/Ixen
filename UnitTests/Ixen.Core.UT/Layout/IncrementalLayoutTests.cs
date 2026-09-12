using Ixen.Core;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Core.UT.Layout
{
    [TestClass]
    public class IncrementalLayoutTests
    {
        private const int ROWS = 40;

        private static VisualElement Row(string name)
        {
            var row = new VisualElement { Name = name };

            row.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 10 };

            return row;
        }

        private static IxenSurface Build(out VisualElement root, out List<VisualElement> rows)
        {
            root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            rows = new List<VisualElement>();

            for (int index = 0; index < ROWS; index++)
            {
                rows.Add(Row("row" + index));
            }

            root.AddChildren(rows.ToArray());

            var surface = new IxenSurface(root)
            {
                Styles = new StyleRegistry()
            };

            surface.ComputeLayout(400, 800);

            return surface;
        }

        [TestMethod]
        public void TheFirstLayoutMeasuresEverything()
        {
            IxenSurface surface = Build(out _, out _);

            Assert.AreEqual(ROWS + 1, surface.LastMeasuredElements,
                "the root and every row");
        }

        [TestMethod]
        public void ARootInvalidationDoesNotReMeasureACleanSubtree()
        {
            IxenSurface surface = Build(out VisualElement root, out _);

            root.InvalidateLayout();
            surface.ComputeLayout(400, 800);

            Assert.IsTrue(surface.LastLayoutRan, "the pass runs");
            Assert.AreEqual(1, surface.LastMeasuredElements,
                "the root, and not one of its forty rows");
        }

        [TestMethod]
        public void ADirtyRowReMeasuresItAndItsAncestorsAndNothingElse()
        {
            IxenSurface surface = Build(out _, out List<VisualElement> rows);

            rows[ROWS / 2].InvalidateLayout();
            surface.ComputeLayout(400, 800);

            Assert.AreEqual(2, surface.LastMeasuredElements,
                "the root and the one row that moved");
        }

        [TestMethod]
        public void AChangeOfViewportReMeasuresEverything()
        {
            IxenSurface surface = Build(out _, out _);

            surface.ComputeLayout(300, 800);

            Assert.AreEqual(ROWS + 1, surface.LastMeasuredElements,
                "a resize reaches every element, including one measured against the viewport");
        }

        [TestMethod]
        public void AStyleChangeReMeasuresTheElementItResolved()
        {
            IxenSurface surface = Build(out _, out List<VisualElement> rows);

            rows[3].Invalidate();
            surface.ComputeLayout(400, 800);

            Assert.AreEqual(2, surface.LastMeasuredElements,
                "the style pass resolved it, so its size may have moved");
        }

        [TestMethod]
        public void AStyleChangeOnAContainerReachesItsChildren()
        {
            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            var panel = new VisualElement { Name = "panel" };
            panel.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            panel.AddChild(Row("inner"));
            root.AddChild(panel);

            var surface = new IxenSurface(root)
            {
                Styles = new StyleRegistry()
            };

            surface.ComputeLayout(400, 800);

            panel.Invalidate();
            surface.ComputeLayout(400, 800);

            Assert.AreEqual(3, surface.LastMeasuredElements,
                "Invalidate marks the whole subtree's styles, so the whole subtree is measured again");
        }

        [TestMethod]
        public void AnElementThatGrowsStillGrowsWhenOnlyItIsDirty()
        {
            IxenSurface surface = Build(out _, out List<VisualElement> rows);

            VisualElement row = rows[1];

            Assert.AreEqual(10f, row.Height);

            row.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
            row.Invalidate();

            surface.ComputeLayout(400, 800);

            Assert.AreEqual(40f, row.Height, "the skip must never hold a stale size");
            Assert.AreEqual(50f, rows[2].Y, "and its siblings must move with it");
        }

        [TestMethod]
        public void AWeightSiblingFollowsAChangeItWasNeverToldAbout()
        {
            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };

            var left = new VisualElement { Name = "left" };
            left.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };

            var right = new VisualElement { Name = "right" };
            right.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Weight, Value = 1 };

            root.AddChildren(left, right);

            var surface = new IxenSurface(root)
            {
                Styles = new StyleRegistry()
            };

            surface.ComputeLayout(400, 200);

            Assert.AreEqual(300f, right.Width);

            left.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            left.Invalidate();

            surface.ComputeLayout(400, 200);

            Assert.AreEqual(200f, right.Width,
                "nothing marked the weight sibling, so only its offered share says it must move");
        }

        [TestMethod]
        public void NothingDirtyMeasuresNothingAtAll()
        {
            IxenSurface surface = Build(out _, out _);

            surface.ComputeLayout(400, 800);

            Assert.IsFalse(surface.LastLayoutRan);
        }
    }
}
