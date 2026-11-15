using Ixen.Core;
using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class PopupTests
    {
        private const int VIEWPORT = 400;

        private VisualElement _root;
        private VisualElement _page;
        private Button _volume;
        private Popup _popup;
        private Slider _slider;
        private IxenSurface _surface;
        private int _changes;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _volume = new Button { Name = "volume", Text = "Volume" };
            _volume.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 80 };
            _volume.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 30 };

            _page = new VisualElement { Name = "page" };
            _page.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };

            _popup = new Popup { Name = "popup", Label = "Volume", AnchorElement = _volume };
            _slider = new Slider { Name = "level" };
            _slider.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 120 };
            _slider.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 24 };
            _popup.AddChild(_slider);
            _popup.OpenChanged += (sender, e) => _changes++;

            _volume.PointerClick += (sender, e) => _popup.Toggle();

            _root.AddChildren(_volume, _page, _popup);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            Layout();
        }

        private void Layout()
        {
            for (int pass = 0; pass < 2; pass++)
            {
                _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            }
        }

        private void Click(VisualElement element)
        {
            float x = element.X + element.ActualWidth / 2;
            float y = element.Y + element.ActualHeight / 2;

            _surface.PointerDown(x, y, PointerButton.Left);
            _surface.PointerUp(x, y, PointerButton.Left);
            Layout();
        }

        [TestMethod]
        public void ItStartsClosedAndOutOfTheTree()
        {
            Assert.IsFalse(_popup.Open);
            Assert.IsFalse(_surface.BuildAccessibilityTree().Children.Any(c => c.Role == AccessibleRole.Dialog));
        }

        [TestMethod]
        public void TheAnchorOpensItBelowAndItsContentIsReachable()
        {
            Click(_volume);

            Assert.IsTrue(_popup.Open);
            Assert.AreEqual(1, _changes, "a user's toggle is reported, for a two-way binding");
            Assert.IsTrue(_popup.Panel.Y >= _volume.Y + _volume.ActualHeight - 0.01f, "it is anchored below the button");

            AccessibleNode dialog = _surface.BuildAccessibilityTree().Children.Single(c => c.Role == AccessibleRole.Dialog);

            Assert.AreEqual("Volume", dialog.Name);
            Assert.IsTrue(dialog.Children.Any(c => c.Role == AccessibleRole.Slider), "its content is in the tree");
        }

        [TestMethod]
        public void OpeningFocusesItsFirstFocusableAndClosingGivesTheFocusBack()
        {
            _surface.Focus(_volume);
            _popup.Toggle();
            Layout();

            Assert.AreSame(_slider, _surface.FocusedElement);

            _surface.KeyDown(Key.Escape, KeyModifiers.None);
            Layout();

            Assert.IsFalse(_popup.Open);
            Assert.AreSame(_volume, _surface.FocusedElement, "the focus goes back to what opened it");
        }

        [TestMethod]
        public void APressOutsideClosesItButNotAPressInsideOrOnTheAnchor()
        {
            Click(_volume);
            Click(_slider);

            Assert.IsTrue(_popup.Open, "a press inside keeps it open");

            Click(_page);

            Assert.IsFalse(_popup.Open, "a press outside closes it");
            Assert.AreEqual(2, _changes);

            Click(_volume);
            Click(_volume);

            Assert.IsFalse(_popup.Open, "the anchor toggles it rather than closing then reopening it");
        }

        [TestMethod]
        public void EscapeClosesItWhateverHasTheFocus()
        {
            _popup.FocusOnOpen = false;
            _surface.Focus(_volume);
            _popup.Toggle();
            Layout();

            _surface.KeyDown(Key.Escape, KeyModifiers.None);
            Layout();

            Assert.IsFalse(_popup.Open);
            Assert.AreSame(_volume, _surface.FocusedElement, "the focus was never inside, so it is left alone");
        }

        [TestMethod]
        public void WithoutLightDismissOnlyTheAppClosesIt()
        {
            _popup.LightDismiss = false;
            Click(_volume);
            Click(_page);

            Assert.IsTrue(_popup.Open);

            _popup.Close();

            Assert.IsFalse(_popup.Open);
        }

        [TestMethod]
        public void WithTheThemeAVolumePopupIsTheSizeOfItsContent()
        {
            var registry = new StyleRegistry();
            registry.AddLoadedDefaults();

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            var button = new Button { Name = "volume", Text = "Volume" };
            var popup = new Popup { Name = "popup", AnchorElement = button };
            var slider = new Slider { Name = "level" };
            slider.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 120 };
            popup.AddChild(slider);
            root.AddChildren(button, popup);

            var surface = new IxenSurface(root) { Styles = registry };
            popup.Open = true;

            for (int pass = 0; pass < 2; pass++)
            {
                surface.ComputeLayout(VIEWPORT, VIEWPORT);
            }

            Assert.IsTrue(popup.Panel.ActualHeight < 80, "the panel wraps its slider: " + popup.Panel.ActualHeight);
            Assert.AreEqual(button.Y + button.ActualHeight, popup.Panel.Y, 0.5f, "right under its button");
        }

        [TestMethod]
        public void SettingOpenFromCodeIsNotReported()
        {
            _popup.Open = true;
            _popup.Open = false;

            Assert.AreEqual(0, _changes);
        }
    }
}
