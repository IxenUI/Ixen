using Ixen.Core.Input;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Core.Visual.Styles.Parsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class OverscrollTests
    {
        private const int VIEWPORT = 200;
        private const float NOTCH = 48f;
        private const long PAUSE = 200;

        private FakeTimeSource _time;
        private VisualElement _root;
        private VisualElement _outer;
        private VisualElement _inner;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _time = new FakeTimeSource();

            _root = Element("root");
            _outer = Box("outer", 100, 100);
            _outer.Scrollable = true;

            _inner = Box("inner", 100, 60);
            _inner.Scrollable = true;
            _inner.AddChild(Box("innerContent", 100, 200));

            _outer.AddChildren(_inner, Box("filler", 100, 200));
            _root.AddChild(_outer);

            _surface = new IxenSurface(_root)
            {
                Styles = new StyleRegistry(),
                TimeSource = _time
            };

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

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

        private void Contain(VisualElement element, OverscrollKind kind)
        {
            element.Styles.Overscroll = new OverscrollStyleDescriptor { X = kind, Y = kind };
            element.Invalidate();

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private void Notch(float x, float y, float deltaY)
        {
            _time.Now += 16;

            _surface.PointerWheel(x, y, 0, deltaY);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private void Exhaust()
        {
            for (int i = 0; i < 20; i++)
            {
                Notch(50, 20, -1);
            }

            _time.Now += PAUSE;
        }

        [TestMethod]
        public void WithoutItTheWheelChainsToThePage()
        {
            Exhaust();
            Notch(50, 20, -1);

            Assert.AreEqual(NOTCH, _outer.ScrollY, "the default is auto, which is the old rule");
        }

        [TestMethod]
        public void ContainStopsTheWheelAtTheList()
        {
            Contain(_inner, OverscrollKind.Contain);

            Exhaust();
            Notch(50, 20, -1);

            Assert.AreEqual(_inner.MaxScrollY, _inner.ScrollY);
            Assert.AreEqual(0, _outer.ScrollY,
                "the walk stops at a contained scroll container instead of climbing past it, "
                + "which is what lets a dialog or a map keep the wheel to itself");
        }

        [TestMethod]
        public void AndItStillScrollsItselfWhileItCan()
        {
            Contain(_inner, OverscrollKind.Contain);

            Notch(50, 20, -1);

            Assert.AreEqual(NOTCH, _inner.ScrollY,
                "containment is about the ancestors, not about the element itself");
            Assert.AreEqual(0, _outer.ScrollY);
        }

        [TestMethod]
        public void ContainmentIsAboutWhatIsAboveNotWhatIsBelow()
        {
            Contain(_outer, OverscrollKind.Contain);

            Exhaust();
            Notch(50, 20, -1);

            Assert.AreEqual(NOTCH, _outer.ScrollY,
                "the page itself is contained, but the list inside it is not - a container "
                + "refuses to hand the wheel UP, never to take it");
        }

        [TestMethod]
        public void AutoSaysItExplicitlyAndChainsLikeNothingAtAll()
        {
            Contain(_inner, OverscrollKind.Auto);

            Exhaust();
            Notch(50, 20, -1);

            Assert.AreEqual(NOTCH, _outer.ScrollY);
        }

        [TestMethod]
        public void ANonScrollableElementCannotContain()
        {
            VisualElement root = Element("root");
            VisualElement page = Box("page", 100, 100);

            page.Scrollable = true;

            VisualElement middle = Box("middle", 100, 60);

            middle.Styles.Overscroll = new OverscrollStyleDescriptor
            {
                X = OverscrollKind.Contain,
                Y = OverscrollKind.Contain
            };

            middle.AddChild(Box("leaf", 100, 40));
            page.AddChildren(middle, Box("filler", 100, 200));
            root.AddChild(page);

            _surface = new IxenSurface(root)
            {
                Styles = new StyleRegistry(),
                TimeSource = _time
            };

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Notch(50, 20, -1);

            Assert.AreEqual(NOTCH, page.ScrollY,
                "overscroll-behavior belongs to a scroll container, exactly as in CSS - a plain "
                + "box that happens to declare it stops nothing");
        }

        [TestMethod]
        public void TheKeyboardIsContainedTheSameWay()
        {
            Contain(_inner, OverscrollKind.Contain);

            _inner.Focusable = true;
            _surface.Focus(_inner);

            for (int i = 0; i < 20; i++)
            {
                _surface.KeyDown(Key.Down, KeyModifiers.None);
                _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            }

            Assert.AreEqual(_inner.MaxScrollY, _inner.ScrollY);
            Assert.AreEqual(0, _outer.ScrollY,
                "both dispatchers go through the same walk, so the arrows honour it for free");
        }

        [TestMethod]
        public void ItComesFromXnsEndToEnd()
        {
            var source = new XnsSource(@"inner {
    overflow: scroll
    overscroll-behavior: contain
}");

            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors);

            var registry = new StyleRegistry();

            registry.Add(set);

            _surface.Styles = registry;
            _root.Invalidate();

            Exhaust();
            Notch(50, 20, -1);

            Assert.AreEqual(0, _outer.ScrollY,
                "the ApplyStyle arm is only ever reached by a rule from a stylesheet, which is "
                + "the one path an inline descriptor never exercises");
        }

        private static OverscrollStyleDescriptor Parsed(string value)
        {
            var parser = new OverscrollStyleParser(value);

            return parser.IsValid ? parser.Descriptor : null;
        }

        [TestMethod]
        public void OneValueSetsBothAxes()
        {
            OverscrollStyleDescriptor parsed = Parsed("contain");

            Assert.AreEqual(OverscrollKind.Contain, parsed.X);
            Assert.AreEqual(OverscrollKind.Contain, parsed.Y);
        }

        [TestMethod]
        public void TwoValuesAreHorizontalThenVertical()
        {
            OverscrollStyleDescriptor parsed = Parsed("none auto");

            Assert.AreEqual(OverscrollKind.None, parsed.X,
                "the order is the one CSS uses for overflow: the horizontal axis first");

            Assert.AreEqual(OverscrollKind.Auto, parsed.Y);
        }

        [TestMethod]
        public void NoneIsItsOwnValueRatherThanASynonym()
        {
            Assert.AreEqual(OverscrollKind.None, Parsed("none").X,
                "this used to fold into Contain, because there was no band for none to "
                + "suppress that contain did not");
        }

        [TestMethod]
        public void AnythingElseIsRefused()
        {
            Assert.IsNull(Parsed("rubber"));
            Assert.IsNull(Parsed("contain rubber"), "on either axis");
            Assert.IsNull(Parsed("auto auto auto"), "and there is no third axis");
            Assert.IsNull(Parsed(""));
        }

        [TestMethod]
        public void TheTwoValueFormComesFromXnsEndToEnd()
        {
            var source = new XnsSource("inner { overscroll-behavior: none contain }");

            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors);

            var registry = new StyleRegistry();

            registry.Add(set);

            _surface.Styles = registry;
            _root.Invalidate();

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            OverscrollStyleDescriptor resolved = _inner.StylesHandlers.Overscroll.Descriptor;

            Assert.AreEqual(OverscrollKind.None, resolved.X);
            Assert.AreEqual(OverscrollKind.Contain, resolved.Y);
        }
    }
}
