using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class IxenHostTests
    {
        private const int VIEWPORT = 200;

        private int _repaints;
        private VisualElement _box;
        private IxenHost _host;

        [TestInitialize]
        public void Setup()
        {
            _repaints = 0;

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _box = new VisualElement { Name = "box" };
            _box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 80 };
            _box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 80 };
            root.AddChild(_box);

            var surface = new IxenSurface
            {
                Styles = new StyleRegistry()
            };

            _host = new IxenHost(surface, () => _repaints++);
            _host.Root = root;

            Paint();
        }

        private void Paint()
        {
            using (var bitmap = new SKBitmap(VIEWPORT, VIEWPORT))
            using (var canvas = new SKCanvas(bitmap))
            {
                _host.Paint(canvas, VIEWPORT, VIEWPORT);
            }
        }

        [TestMethod]
        public void PaintingLaysOutAndClearsTheDirtyFlag()
        {
            Assert.IsTrue(_box.Width > 0, "the layout ran");
            Assert.AreEqual(0, _repaints, "painting must not ask for another paint");
        }

        [TestMethod]
        public void AnEventThatChangesNothingDoesNotRequestARepaint()
        {
            _host.PointerMove(40, 40);
            _host.PointerMove(41, 41);
            _host.PointerDown(40, 40, PointerButton.Left);
            _host.PointerUp(40, 40, PointerButton.Left);

            Assert.AreEqual(0, _repaints,
                "invalidating unconditionally would repaint on every mouse move");
        }

        [TestMethod]
        public void AHandlerThatDirtiesTheTreeRequestsARepaint()
        {
            _box.PointerClick += (s, e) => _box.Text = "changed";

            _host.PointerDown(40, 40, PointerButton.Left);
            _host.PointerUp(40, 40, PointerButton.Left);

            Assert.AreEqual(1, _repaints);
        }

        [TestMethod]
        public void EachDirtyingEventRequestsExactlyOneRepaint()
        {
            int counter = 0;
            _box.PointerEnter += (s, e) => _box.Text = $"in {++counter}";
            _box.PointerLeave += (s, e) => _box.Text = $"out {++counter}";

            _host.PointerMove(40, 40);
            Paint();
            _host.PointerMove(150, 150);

            Assert.AreEqual(2, _repaints, "one for the enter, one for the leave");
        }

        [TestMethod]
        public void TheHostRoutesThroughToTheSurface()
        {
            string seen = null;
            _box.PointerClick += (s, e) => seen = ((VisualElement)s).Name;

            _host.PointerDown(40, 40, PointerButton.Left);
            _host.PointerUp(40, 40, PointerButton.Left);

            Assert.AreEqual("box", seen);
        }

        [TestMethod]
        public void PaintingIsIgnoredForADegenerateSize()
        {
            using (var bitmap = new SKBitmap(1, 1))
            using (var canvas = new SKCanvas(bitmap))
            {
                _host.Paint(canvas, 0, 0);
                _host.Paint(null, VIEWPORT, VIEWPORT);
            }

            Assert.AreEqual(0, _repaints, "no crash and nothing requested");
        }

        [TestMethod]
        public void ALeaveWithoutAHandlerRequestsNothing()
        {
            _host.PointerMove(40, 40);
            _host.PointerLeave();

            Assert.AreEqual(0, _repaints);
        }

        [TestMethod]
        public void ACaptureLostIsRoutedAndCancelsThePress()
        {
            _box.PointerClick += (s, e) => _box.Text = "clicked";

            _host.PointerDown(40, 40, PointerButton.Left);
            _host.PointerCaptureLost();
            _host.PointerUp(40, 40, PointerButton.Left);

            Assert.AreNotEqual("clicked", _box.Text, "the press died with the capture");
        }
    }
}
