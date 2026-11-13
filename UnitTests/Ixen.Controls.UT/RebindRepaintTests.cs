using Ixen.Core;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class RebindRepaintTests
    {
        private const int WIDTH = 400;
        private const int HEIGHT = 300;

        private static readonly SKColor Scribble = new SKColor(255, 0, 255);

        private VisualElement _root;
        private VirtualList _list;
        private Slider _slider;
        private List<string> _items;
        private IxenSurface _surface;
        private SKBitmap _bitmap;
        private SKCanvas _canvas;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _root.Styles.Background = new BackgroundStyleDescriptor { Color = "#101010" };

            _items = Enumerable.Range(0, 200).Select(i => "track " + i).ToList();

            _list = new VirtualList { Name = "tracks", ItemHeight = 20 };
            _list.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            _list.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            Bind();

            _slider = new Slider { Name = "seek", Maximum = 100, Value = 40 };
            _slider.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            _slider.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 };

            _root.AddChildren(_list, _slider);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry(), Scheduler = new FakeScheduler() };
            _bitmap = new SKBitmap(WIDTH, HEIGHT);
            _canvas = new SKCanvas(_bitmap);

            Frame();
            Frame();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _canvas.Dispose();
            _bitmap.Dispose();
        }

        private void Bind() => _list.SetItems(_items, () => new VisualElement(), (row, index) => row.Text = _items[index]);

        private void Frame()
        {
            _surface.ComputeLayout(WIDTH, HEIGHT);
            _surface.Render(_canvas);
        }

        private bool Survived(int x, int y) => _bitmap.GetPixel(x, y) == Scribble;

        [TestMethod]
        public void HandingTheListTheSameRowsAgainRepaintsNone()
        {
            _bitmap.SetPixel(150, 50, Scribble);

            Bind();
            Frame();

            Assert.IsTrue(Survived(150, 50),
                "a SetState hands a @foreach back to its list on every replay; the rows are bound again "
                + "in case an item changed, but a row showing the same item in the same place is not "
                + "painted again - under a cover grid that was the whole window every second");
        }

        [TestMethod]
        public void ARowWhoseItemChangedIsRepaintedAndOnlyIt()
        {
            int y = (int)(_list.RealisedRows.First().Y + 10);

            _bitmap.SetPixel(20, y, Scribble);
            _bitmap.SetPixel(150, (int)(y + 60), Scribble);

            _items[0] = "a different first track";
            Bind();
            Frame();

            Assert.IsFalse(Survived(20, y), "the first row shows another text");
            Assert.IsTrue(Survived(150, (int)(y + 60)), "the rows below it did not change");
        }

        [TestMethod]
        public void ASliderGivenTheValueItHasRepaintsNothing()
        {
            int y = (int)(_slider.Y + 10);

            _bitmap.SetPixel(60, y, Scribble);

            _slider.Value = 40;
            _slider.Maximum = 100;
            Frame();

            Assert.IsTrue(Survived(60, y), "a binding replays the position of a seek bar every second");

            _slider.Value = 70;
            Frame();

            Assert.IsFalse(Survived(60, y), "and a value that changed still moves it");
        }
    }
}
