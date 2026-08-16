using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class ScrollTests
    {
        private const int VIEWPORT = 200;
        private const float NOTCH = 48f;

        private static VisualElement Element(string name)
        {
            var element = new VisualElement { Name = name };
            element.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            return element;
        }

        private static VisualElement Box(string name, float width, float height)
        {
            VisualElement element = Element(name);
            element.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = width };
            element.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };
            return element;
        }

        private static IxenSurface Laid(VisualElement root)
        {
            var surface = new IxenSurface(root)
            {
                Styles = new StyleRegistry()
            };

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return surface;
        }

        private static VisualElement Viewport(out VisualElement first, out VisualElement last, int childCount = 5)
        {
            VisualElement root = Element("root");
            VisualElement viewport = Box("viewport", 100, 100);
            viewport.Scrollable = true;

            first = null;
            last = null;

            for (int i = 0; i < childCount; i++)
            {
                VisualElement item = Element($"item{i}");
                item.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
                viewport.AddChild(item);

                first = first ?? item;
                last = item;
            }

            root.AddChild(viewport);
            return viewport;
        }

        [TestMethod]
        public void TheExtentIsTheAggregatedChildren()
        {
            VisualElement viewport = Viewport(out _, out _);
            Laid(viewport.Parent);

            Assert.AreEqual(200, viewport.ScrollExtentHeight, "five 40px items");
            Assert.AreEqual(100, viewport.MaxScrollY, "200 of content in a 100 box");
            Assert.AreEqual(0, viewport.MaxScrollX);
        }

        [TestMethod]
        public void AWheelScrollsTheNearestScrollableAncestor()
        {
            VisualElement viewport = Viewport(out VisualElement first, out _);
            IxenSurface surface = Laid(viewport.Parent);

            float before = first.Y;
            surface.PointerWheel(50, 50, 0, -1);

            Assert.AreEqual(NOTCH, viewport.ScrollY, "one notch down");

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(before - NOTCH, first.Y, "the content moved up by the offset");
        }

        [TestMethod]
        public void ScrollingIsClampedAtBothEnds()
        {
            VisualElement viewport = Viewport(out _, out _);
            IxenSurface surface = Laid(viewport.Parent);

            for (int i = 0; i < 10; i++)
            {
                surface.PointerWheel(50, 50, 0, -1);
                surface.ComputeLayout(VIEWPORT, VIEWPORT);
            }

            Assert.AreEqual(100, viewport.ScrollY, "cannot go past the end of the content");

            for (int i = 0; i < 10; i++)
            {
                surface.PointerWheel(50, 50, 0, 1);
                surface.ComputeLayout(VIEWPORT, VIEWPORT);
            }

            Assert.AreEqual(0, viewport.ScrollY, "cannot go above the top");
        }

        [TestMethod]
        public void AnElementWhoseContentFitsDoesNotScroll()
        {
            VisualElement viewport = Viewport(out _, out _, 1);
            IxenSurface surface = Laid(viewport.Parent);

            surface.PointerWheel(50, 50, 0, -1);

            Assert.AreEqual(0, viewport.ScrollY);
            Assert.IsFalse(surface.IsDirty, "nothing moved, so nothing to repaint");
        }

        [TestMethod]
        public void ANonScrollableElementIsNeverScrolled()
        {
            VisualElement viewport = Viewport(out _, out _);
            viewport.Scrollable = false;
            IxenSurface surface = Laid(viewport.Parent);

            surface.PointerWheel(50, 50, 0, -1);

            Assert.AreEqual(0, viewport.ScrollY);
            Assert.AreEqual(0, viewport.ScrollExtentHeight, "the extent is only measured when it is needed");
        }

        [TestMethod]
        public void TheWheelBubblesAndAHandlerCanTakeIt()
        {
            VisualElement viewport = Viewport(out VisualElement first, out _);
            IxenSurface surface = Laid(viewport.Parent);

            var log = "";
            first.PointerWheel += (s, e) => log += $"item({e.DeltaY}) ";
            viewport.PointerWheel += (s, e) =>
            {
                log += $"viewport({e.Source.Name}) ";
                e.Handled = true;
            };
            viewport.Parent.PointerWheel += (s, e) => log += "root ";

            surface.PointerWheel(50, 20, 0, -1);

            Assert.AreEqual("item(-1) viewport(item0) ", log,
                "it bubbles from the hit element and stops on Handled");
            Assert.AreEqual(0, viewport.ScrollY, "a handled wheel does not scroll");
        }

        [TestMethod]
        public void ScrollingChainsToTheAncestorThatCanStillMove()
        {
            VisualElement root = Element("root");
            VisualElement outer = Box("outer", 100, 100);
            outer.Scrollable = true;

            VisualElement inner = Element("inner");
            inner.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 60 };
            inner.Scrollable = true;

            VisualElement innerContent = Element("innerContent");
            innerContent.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
            inner.AddChild(innerContent);

            VisualElement filler = Element("filler");
            filler.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };

            outer.AddChildren(inner, filler);
            root.AddChild(outer);

            IxenSurface surface = Laid(root);

            Assert.AreEqual(0, inner.MaxScrollY, "the inner content fits, so it cannot move");

            surface.PointerWheel(50, 20, 0, -1);

            Assert.AreEqual(0, inner.ScrollY);
            Assert.AreEqual(NOTCH, outer.ScrollY, "the wheel went to the ancestor that can move");
        }

        [TestMethod]
        public void AHorizontalWheelScrollsTheHorizontalAxis()
        {
            VisualElement root = Element("root");
            VisualElement viewport = Box("viewport", 100, 100);
            viewport.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };
            viewport.Scrollable = true;
            VisualElement a = Element("a");
            a.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 80 };

            VisualElement b = Element("b");
            b.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 80 };

            viewport.AddChildren(a, b);
            root.AddChild(viewport);

            IxenSurface surface = Laid(root);

            Assert.AreEqual(60, viewport.MaxScrollX);

            surface.PointerWheel(50, 50, 1, 0);

            Assert.AreEqual(48, viewport.ScrollX, "a positive horizontal delta means to the right");
            Assert.AreEqual(0, viewport.ScrollY);

            surface.ComputeLayout(VIEWPORT, VIEWPORT);
            surface.PointerWheel(50, 50, -1, 0);

            Assert.AreEqual(0, viewport.ScrollX, "and a negative one comes back");
        }

        [TestMethod]
        public void HitTestingFollowsTheScrolledContent()
        {
            VisualElement viewport = Viewport(out VisualElement first, out VisualElement last);
            IxenSurface surface = Laid(viewport.Parent);

            Assert.AreSame(first, surface.HitTest(50, 20), "the first item is at the top");
            Assert.AreSame(viewport.Children[2], surface.HitTest(50, 95),
                "the third item is the last one still inside the viewport");

            viewport.ScrollY = 100;
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreSame(viewport.Children[2], surface.HitTest(50, 10),
                "after scrolling by 100 the third item straddles the top edge");
            Assert.AreSame(last, surface.HitTest(50, 95),
                "the last item was unreachable before and is hittable now");
        }

        [TestMethod]
        public void ScrollingIsClampedWhenTheContentShrinks()
        {
            VisualElement viewport = Viewport(out _, out VisualElement last);
            IxenSurface surface = Laid(viewport.Parent);

            viewport.ScrollY = 100;
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            viewport.RemoveChild(last);
            viewport.RemoveChild(viewport.Children[viewport.Children.Count - 1]);
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(20, viewport.ScrollY,
                "three 40px items in a 100px box leaves 20 of scroll, and the offset follows");
        }

        [TestMethod]
        public void SettingTheOffsetBeforeALayoutIsNotLost()
        {
            VisualElement viewport = Viewport(out _, out _);
            viewport.ScrollY = 60;

            Laid(viewport.Parent);

            Assert.AreEqual(60, viewport.ScrollY,
                "the extent is unknown until measure, so the value must survive until then");
        }

        [TestMethod]
        public void ScrollingRequestsALayoutAndNothingElse()
        {
            VisualElement viewport = Viewport(out _, out _);
            IxenSurface surface = Laid(viewport.Parent);

            viewport.ScrollBy(0, 40);

            Assert.IsTrue(surface.IsDirty);
            Assert.IsFalse(viewport.MustRefreshStyles, "geometry moved, no style changed");
        }
    }
}
