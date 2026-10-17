using Ixen.Core;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Shapes;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.StyleSheets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class SubmenuMarkerTests
    {
        private const int VIEWPORT = 400;

        private VisualElement _root;
        private Menu _menu;
        private MenuItem _plain;
        private MenuItem _short;
        private MenuItem _long;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _menu = new Menu { Name = "menu" };

            _plain = new MenuItem { Name = "open", Text = "Open" };
            _short = WithSubmenu("recent", "Recent");
            _long = WithSubmenu("export", "Export the whole document as");

            _menu.AddChildren(_plain, _short, _long);
            _root.AddChild(_menu);

            var registry = new StyleRegistry();
            registry.AddDefaults(new DefaultTheme_StyleSheet());

            _surface = new IxenSurface(_root) { Styles = registry };
            _menu.Open = true;

            Layout();
        }

        private static MenuItem WithSubmenu(string name, string text)
        {
            var item = new MenuItem { Name = name, Text = text };
            var submenu = new Menu { Name = name + "_menu" };

            submenu.AddChild(new MenuItem { Name = name + "_first", Text = "First" });
            item.AddChild(submenu);

            return item;
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private static Path Marker(MenuItem item)
        {
            foreach (VisualElement child in item.ChildElements)
            {
                if (child is Path path && path.TypeName == MenuItem.MARKER)
                {
                    return path;
                }
            }

            return null;
        }

        [TestMethod]
        public void OnlyAnItemWithASubmenuCarriesAMarker()
        {
            Assert.IsNull(Marker(_plain));
            Assert.IsNotNull(Marker(_short));
            Assert.IsNotNull(Marker(_long));
        }

        [TestMethod]
        public void TheMarkerSitsAtTheFarEndOfTheItem()
        {
            Path marker = Marker(_short);

            Assert.AreEqual(_short.ContentX + _short.ContentWidth, marker.X + marker.ActualWidth, 0.01f,
                "the text slot absorbs the slack, which is what pushes the marker to the end");
        }

        [TestMethod]
        public void TheMarkersOfOnePanelLineUpWhateverTheLabelIs()
        {
            Path first = Marker(_short);
            Path second = Marker(_long);

            Assert.AreEqual(second.X, first.X, 0.01f,
                "the panel hugs its widest item and then stretches the others, so every slot "
                + "ends at the same place");
        }

        [TestMethod]
        public void TheMarkerDoesNotSitOnTheLabel()
        {
            Path marker = Marker(_long);

            Assert.IsTrue(marker.X > _long.ContentX + _long.TextExtent,
                "an element's own text and its children no longer share one content box");
        }

        [TestMethod]
        public void ABarItemCarriesNoMarker()
        {
            var bar = new MenuBar { Name = "bar" };
            MenuItem file = WithSubmenu("file", "File");

            bar.AddChild(file);
            _root.AddChild(bar);

            Layout();

            Assert.IsTrue(file.HasSubmenu);
            Assert.IsNull(Marker(file), "a top level bar item drops its menu below, so it needs no arrow");
        }

        [TestMethod]
        public void TheMarkerIsInked()
        {
            Path marker = Marker(_short);

            var bitmap = new SKBitmap(VIEWPORT, VIEWPORT);

            using (var canvas = new SKCanvas(bitmap))
            {
                _surface.Render(canvas);
            }

            int inked = 0;

            for (int x = (int)marker.X; x < marker.X + marker.ActualWidth; x++)
            {
                for (int y = (int)marker.Y; y < marker.Y + marker.ActualHeight; y++)
                {
                    SKColor color = bitmap.GetPixel(x, y);

                    if (color.Alpha > 0 && color.Red < 200)
                    {
                        inked++;
                    }
                }
            }

            bitmap.Dispose();

            Assert.IsTrue(inked > 4, $"only {inked} pixels of the marker were drawn");
        }
    }
}
