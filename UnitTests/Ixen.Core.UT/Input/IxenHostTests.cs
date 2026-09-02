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
        public void KeysAndTextAreRoutedThroughToTheFocusedElement()
        {
            _box.Focusable = true;
            _host.Focus(_box);

            Key seenKey = Key.None;
            string seenText = null;
            _box.KeyDown += (s, e) => seenKey = e.Key;
            _box.TextInput += (s, e) => seenText = e.Text;

            _host.KeyDown(Key.A, KeyModifiers.Control);
            _host.TextInput("a");

            Assert.AreSame(_box, _host.FocusedElement);
            Assert.AreEqual(Key.A, seenKey);
            Assert.AreEqual("a", seenText);
        }

        [TestMethod]
        public void ControlCharactersAreNotTextInput()
        {
            _box.Focusable = true;
            _host.Focus(_box);

            string seen = null;
            _box.TextInput += (s, e) => seen = e.Text;

            _host.TextInput("\b");
            _host.TextInput("\r");

            Assert.IsNull(seen, "Backspace and Enter reach handlers through KeyDown, not as text");

            _host.TextInput("a\tb");

            Assert.AreEqual("ab", seen, "a mixed string keeps only its printable characters");
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

        private TextField Field()
        {
            var field = new TextField { Name = "field", Focusable = true };
            field.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 80 };
            field.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 24 };

            _host.Root.AddChild(field);
            Paint();

            _host.Focus(field);

            return field;
        }

        [TestMethod]
        public void ACompositionRunReachesTheFocusedField()
        {
            TextField field = Field();

            _host.Composition("nihon", 5);

            Assert.IsTrue(string.IsNullOrEmpty(field.Text), "a run is not an edit");
            Assert.IsTrue(field.IsComposing);
        }

        [TestMethod]
        public void FinishingTheCompositionCommitsWhatTheFieldHolds()
        {
            TextField field = Field();

            _host.Composition("nihon", 5);

            _repaints = 0;

            _host.FinishComposition();

            Assert.AreEqual("nihon", field.Text,
                "Android says finish composing without saying what the run was, so this is the "
                + "route that has to ask the field");

            Assert.IsFalse(field.IsComposing);
            Assert.AreEqual(1, _repaints);
        }

        [TestMethod]
        public void FinishingWithNothingFocusedIsANoOp()
        {
            _host.Focus(null);

            _host.FinishComposition();

            Assert.AreEqual(string.Empty, _box.Text ?? string.Empty);
        }
    }
}
