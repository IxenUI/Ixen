using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Core.UT.Layout
{
    [TestClass]
    public class ChromeTests
    {
        private const int VIEWPORT = 300;

        private VisualElement _root;
        private VisualElement _field;
        private VisualElement _first;
        private VisualElement _second;
        private VisualElement _clear;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _root.Styles.Background = new BackgroundStyleDescriptor { Color = "#FFFFFF" };

            _field = Sized(new VisualElement { Name = "field" }, 200, 40);
            _field.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };

            _first = Sized(new VisualElement { Name = "first" }, 50, 40);
            _second = Sized(new VisualElement { Name = "second" }, 50, 40);
            _field.AddChildren(_first, _second);

            _clear = Sized(new VisualElement { Name = "clear" }, 16, 16);
            _clear.Styles.Right = new RightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 4 };
            _clear.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 12 };
            _clear.Styles.Background = new BackgroundStyleDescriptor { Color = "#FF0000" };

            _root.AddChild(_field);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
        }

        private static VisualElement Sized(VisualElement element, float width, float height)
        {
            element.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = width };
            element.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };
            return element;
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        [TestMethod]
        public void ChromeIsPlacedOnTheBoxByItsAnchors()
        {
            _field.AddChrome(_clear);
            Layout();

            Assert.AreEqual(_field.X + 200 - 4 - 16, _clear.X, 0.01f, "right: 4px from the element's own edge");
            Assert.AreEqual(_field.Y + 12, _clear.Y, 0.01f);
            Assert.AreEqual(16f, _clear.ActualWidth);
        }

        [TestMethod]
        public void ChromeTakesNoPlaceInTheContent()
        {
            Layout();
            float before = _second.X;

            _field.AddChrome(_clear);
            Layout();

            Assert.AreEqual(before, _second.X, "the row lays its children out as if the chrome were not there");
            CollectionAssert.AreEqual(new[] { _first, _second }, _field.ChildElements.ToArray());
            CollectionAssert.AreEqual(new[] { _clear }, _field.ChromeElements.ToArray());
            Assert.AreSame(_field, _clear.Parent);
        }

        [TestMethod]
        public void ChromeIsPaintedOverTheChildrenAndHitFirst()
        {
            _second.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 150 };
            _second.Styles.Background = new BackgroundStyleDescriptor { Color = "#0000FF" };
            _field.AddChrome(_clear);
            Layout();

            int x = (int)(_clear.X + 8);
            int y = (int)(_clear.Y + 8);

            using (SKBitmap frame = _surface.RenderToBitmap())
            {
                SKColor pixel = frame.GetPixel(x, y);

                Assert.IsTrue(pixel.Red > 200 && pixel.Blue < 60, $"the chrome sits on top of the child under it: {pixel}");
            }

            var hits = new List<string>();
            _clear.PointerClick += (sender, e) => hits.Add("clear");
            _second.PointerClick += (sender, e) => hits.Add("second");

            _surface.PointerDown(x, y, PointerButton.Left);
            _surface.PointerUp(x, y, PointerButton.Left);

            CollectionAssert.AreEqual(new[] { "clear" }, hits, "and it is what the pointer reaches");
        }

        [TestMethod]
        public void ChromeIsNotAChildForStructuralSelectors()
        {
            var registry = new StyleRegistry();
            var source = new XnsSource("second:last-child { background: #00FF00 }");
            registry.Add(source.Compile());
            _surface.Styles = registry;

            _field.AddChrome(_clear);
            Layout();

            Assert.AreEqual("#00FF00", _second.StylesHandlers.Background.Descriptor?.Color,
                "the last child is still the last child: chrome is a second list");
        }

        [TestMethod]
        public void AStylesheetReachesChrome()
        {
            var registry = new StyleRegistry();
            var source = new XnsSource("clear { width: 24px }");
            registry.Add(source.Compile());
            _surface.Styles = registry;

            _clear.Styles.Width = new WidthStyleDescriptor();
            _field.AddChrome(_clear);
            Layout();

            Assert.AreEqual(24f, _clear.ActualWidth);
        }

        [TestMethod]
        public void ExposedChromeIsInTheAccessibilityTreeButAScrollbarIsNot()
        {
            _clear.Role = AccessibleRole.Button;
            _clear.Label = "clear";
            _field.AddChrome(_clear);
            _field.Scrollable = true;
            Layout();

            AccessibleNode tree = _surface.BuildAccessibilityTree();
            var all = new List<AccessibleNode>();
            Walk(tree, all);

            Assert.IsTrue(all.Any(node => node.Element == _clear), "a clear button a field draws over itself is reachable");
            Assert.IsFalse(all.Any(node => node.Element is Scrollbar), "a scrollbar stays out of it");
        }

        private static void Walk(AccessibleNode node, List<AccessibleNode> into)
        {
            into.Add(node);

            foreach (AccessibleNode child in node.ChildList)
            {
                Walk(child, into);
            }
        }

        [TestMethod]
        public void RemovingChromeDetachesItAndTheBoxRelaysOut()
        {
            _field.AddChrome(_clear);
            Layout();

            Assert.IsTrue(_field.RemoveChrome(_clear));
            Assert.IsNull(_clear.Parent);
            Assert.IsNull(_clear.Host);
            Assert.AreEqual(0, _field.ChromeElements.Count);
            Assert.IsFalse(_field.RemoveChrome(_clear), "removing it twice is a no-op");
        }

        [TestMethod]
        public void ChromeThatAlreadyBelongsSomewhereIsRefused()
        {
            Assert.Throws<InvalidOperationException>(() => _field.AddChrome(_first), "it is a child already");
            Assert.Throws<InvalidOperationException>(() => _first.AddChrome(_field), "and this would be a cycle");
        }
    }
}
