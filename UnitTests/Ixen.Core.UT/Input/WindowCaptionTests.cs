using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class WindowCaptionTests
    {
        private IxenSurface _surface;
        private VisualElement _header;
        private VisualElement _title;
        private VisualElement _button;
        private VisualElement _clickable;
        private VisualElement _icon;

        private static VisualElement Box(string name, float left, float top, float width, float height)
        {
            var element = new VisualElement { Name = name };

            element.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = left };
            element.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = top };
            element.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = width };
            element.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };

            return element;
        }

        [TestInitialize]
        public void Setup()
        {
            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };

            _header = Box("header", 0, 0, 400, 40);
            _header.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };
            _header.WindowDrag = true;

            _title = Box("title", 10, 10, 100, 20);
            _button = Box("close", 360, 5, 30, 30);
            _button.Focusable = true;
            _clickable = Box("search", 200, 5, 100, 30);
            _clickable.PointerClick += (sender, e) => { };
            _icon = Box("icon", 5, 5, 20, 20);
            _clickable.AddChild(_icon);

            _header.AddChild(_title);
            _header.AddChild(_button);
            _header.AddChild(_clickable);
            root.AddChild(_header);
            root.AddChild(Box("page", 0, 40, 400, 260));

            _surface = new IxenSurface(root) { Styles = new StyleRegistry() };
            _surface.ComputeLayout(400, 300);
        }

        [TestMethod]
        public void TheEmptyPartOfADragAreaAndWhatMerelyDecoratesItMoveTheWindow()
        {
            Assert.IsTrue(_surface.IsWindowCaptionAt(150, 20), "the bare header");
            Assert.IsTrue(_surface.IsWindowCaptionAt(50, 20), "a title is decoration, so a drag on it moves the window");
        }

        [TestMethod]
        public void SomethingInteractiveInsideItStaysClickable()
        {
            Assert.IsFalse(_surface.IsWindowCaptionAt(370, 20), "a focusable button");
            Assert.IsFalse(_surface.IsWindowCaptionAt(250, 20), "an element that listens for clicks");
            Assert.IsFalse(_surface.IsWindowCaptionAt(212, 20), "and a plain child of that element");
        }

        [TestMethod]
        public void OutsideADragAreaIsTheApplicationsOwn()
        {
            Assert.IsFalse(_surface.IsWindowCaptionAt(150, 150));
        }

        [TestMethod]
        public void TheDensityIsTakenIntoAccount()
        {
            _surface.Scale = 2;
            _surface.ComputeLayout(800, 600);

            Assert.IsTrue(_surface.IsWindowCaptionAt(300, 40), "the host hands device pixels in");
            Assert.IsFalse(_surface.IsWindowCaptionAt(740, 40));
        }

        [TestMethod]
        public void TheMaximizeButtonIsReportedAsSuchSoWindowsOffersItsSnapLayouts()
        {
            _button.MaximizeButton = true;

            Assert.AreEqual(WindowArea.Maximize, _surface.WindowAreaAt(370, 20),
                "a focusable button would otherwise be plain client area");
            Assert.AreEqual(WindowArea.Caption, _surface.WindowAreaAt(150, 20));
            Assert.AreEqual(WindowArea.Client, _surface.WindowAreaAt(250, 20));
        }

        [TestMethod]
        public void AChildOfTheMaximizeButtonIsPartOfIt()
        {
            _clickable.MaximizeButton = true;

            Assert.AreEqual(WindowArea.Maximize, _surface.WindowAreaAt(212, 20),
                "the glyph inside the button, which the pointer usually rests on");
        }

        [TestMethod]
        public void ADisabledMaximizeButtonOffersNoSnapLayouts()
        {
            _button.MaximizeButton = true;
            _button.Enabled = false;
            _surface.ComputeLayout(400, 300);

            Assert.AreNotEqual(WindowArea.Maximize, _surface.WindowAreaAt(370, 20));
        }

        [TestMethod]
        public void WithoutADragAreaNothingMovesTheWindow()
        {
            _header.WindowDrag = false;

            Assert.IsFalse(_surface.IsWindowCaptionAt(150, 20));
        }
    }
}
