using Ixen.Core;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Shapes;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.StyleSheets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class ShapeMarkTests
    {
        private const int VIEWPORT = 300;

        private VisualElement _root;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _root.Styles.Background = new BackgroundStyleDescriptor { Color = "#FFFFFF" };

            var registry = new StyleRegistry();
            registry.AddDefaults(new DefaultTheme_StyleSheet());

            _surface = new IxenSurface(_root) { Styles = registry };
        }

        private void Layout()
        {
            _root.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private static IEnumerable<VisualElement> Walk(VisualElement element)
        {
            yield return element;

            foreach (VisualElement child in element.ChildElements)
            {
                foreach (VisualElement found in Walk(child))
                {
                    yield return found;
                }
            }
        }

        private static IEnumerable<Type> Controls()
            => typeof(Button).Assembly.GetTypes()
                .Where(type => type.IsPublic && !type.IsAbstract
                    && typeof(VisualElement).IsAssignableFrom(type)
                    && type.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(type => type.Name);

        [TestMethod]
        public void NoControlDrawsAShapeWithACharacter()
        {
            var seen = 0;

            foreach (Type type in Controls())
            {
                var control = (VisualElement)Activator.CreateInstance(type);

                foreach (VisualElement element in Walk(control))
                {
                    seen++;

                    string text = element.Text;

                    if (string.IsNullOrEmpty(text))
                    {
                        continue;
                    }

                    foreach (char character in text)
                    {
                        Assert.IsTrue(character < 0x80,
                            $"{type.Name} puts U+{(int)character:X4} in the Text of a "
                            + $"{element.TypeName ?? "part"}. A font either has that character or "
                            + "draws nothing, which is exactly how the horizontal scrollbar arrows "
                            + "were invisible on Windows for years. Draw it with a <Path>.");
                    }
                }
            }

            Assert.IsTrue(seen > 30,
                $"the walk only saw {seen} elements, so it is not proving anything");
        }

        [TestMethod]
        public void TheScrollbarDrawsItsArrowsWithAShapeToo()
        {
            var bar = new Scrollbar(true);

            foreach (VisualElement element in Walk(bar))
            {
                Assert.IsTrue(string.IsNullOrEmpty(element.Text),
                    "the scrollbar is in Ixen.Core rather than in the control library, and it is "
                    + "where the invisible-arrow bug actually happened");
            }

            Assert.IsInstanceOfType<Path>(bar.Start);
            Assert.IsInstanceOfType<Path>(bar.End);
        }

        private static bool Paints(VisualElement element, SKBitmap frame)
            => Ink(element, frame, out _, out _, out _, out _);

        private static bool Ink(VisualElement element, SKBitmap frame,
            out int left, out int top, out int right, out int bottom)
        {
            left = int.MaxValue;
            top = int.MaxValue;
            right = int.MinValue;
            bottom = int.MinValue;

            for (int y = (int)element.Y; y < (int)(element.Y + element.ActualHeight); y++)
            {
                for (int x = (int)element.X; x < (int)(element.X + element.ActualWidth); x++)
                {
                    if (x < 0 || y < 0 || x >= frame.Width || y >= frame.Height)
                    {
                        continue;
                    }

                    SKColor pixel = frame.GetPixel(x, y);

                    if (pixel.Red >= 240 && pixel.Green >= 240 && pixel.Blue >= 240)
                    {
                        continue;
                    }

                    left = Math.Min(left, x);
                    top = Math.Min(top, y);
                    right = Math.Max(right, x);
                    bottom = Math.Max(bottom, y);
                }
            }

            return right >= left;
        }

        [TestMethod]
        public void AComboBoxPaintsItsChevron()
        {
            var combo = new ComboBox { Name = "pick" };

            combo.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 120 };
            combo.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 30 };
            combo.AddChildren(new MenuItem { Text = "Small" }, new MenuItem { Text = "Large" });

            _root.AddChild(combo);

            Layout();

            VisualElement chevron = Walk(combo)
                .First(element => element.TypeName == ComboBox.CHEVRON);

            Assert.IsInstanceOfType<Path>(chevron);

            using (SKBitmap frame = _surface.RenderToBitmap())
            {
                Assert.IsTrue(Paints(chevron, frame),
                    "the chevron reaches the screen, which a missing glyph never did");
            }
        }

        private TreeView Tree()
        {
            var roots = new List<string> { "Documents", "Pictures" };

            var tree = new TreeView { Name = "tree", ItemHeight = 22 };

            tree.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            tree.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 120 };
            tree.Rows.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            tree.Rows.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 120 };

            tree.SetRoots(roots,
                item => (string)item == "Documents" ? new List<string> { "Letters" } : null,
                () => new VisualElement(),
                (content, node) => content.Text = (string)node.Item);

            _root.AddChild(tree);

            return tree;
        }

        [TestMethod]
        public void ABranchPaintsItsExpander()
        {
            TreeView tree = Tree();

            Layout();

            TreeRow branch = Walk(tree).OfType<TreeRow>()
                .First(row => !row.Expander.IsHidden);

            Assert.IsInstanceOfType<Path>(branch.Expander);

            using (SKBitmap frame = _surface.RenderToBitmap())
            {
                Assert.IsTrue(
                    Ink(branch.Expander, frame, out int left, out int top, out int right, out int bottom),
                    "a collapsed branch shows a twisty, and Kevin found this one as a tofu on Android");

                Assert.IsTrue(right - left < branch.Expander.ActualWidth - 1,
                    "the theme's padding and object-fit: contain keep the twisty smaller than "
                    + "its box on both axes - a shape stretched to fill one is the thing a font "
                    + "never did to a glyph");

                Assert.IsTrue(bottom - top < branch.Expander.ActualHeight - 1);
            }
        }

        [TestMethod]
        public void ALeafPaintsNothing()
        {
            TreeView tree = Tree();

            Layout();

            TreeRow leaf = Walk(tree).OfType<TreeRow>()
                .First(row => row.Expander.IsHidden);

            using (SKBitmap frame = _surface.RenderToBitmap())
            {
                Assert.IsFalse(
                    Paints(leaf.Expander, frame),
                    "visibility: hidden covers a shape like anything else");
            }
        }
    }
}
