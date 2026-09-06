using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class KeyHandledTests
    {
        private const int VIEWPORT = 200;

        private VisualElement _root;
        private VisualElement _input;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _input = new VisualElement { Name = "input", Focusable = true };
            _root.AddChild(_input);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private VisualElement Scroller()
        {
            var list = new VisualElement { Name = "list", Scrollable = true };
            list.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            list.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 60 };

            var tall = new VisualElement { Name = "tall" };
            tall.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 600 };

            list.AddChild(tall);
            _root.AddChild(list);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return list;
        }

        [TestMethod]
        public void AKeyNobodyWantsIsReportedAsUnhandled()
        {
            Assert.IsFalse(_surface.KeyDown(Key.A, KeyModifiers.None));
        }

        [TestMethod]
        public void AHandlerThatClaimsTheKeyIsReported()
        {
            _input.KeyDown += (sender, e) => e.Handled = true;
            _surface.Focus(_input);

            Assert.IsTrue(_surface.KeyDown(Key.A, KeyModifiers.None));
        }

        [TestMethod]
        public void AHandlerThatOnlyWatchesDoesNotClaimTheKey()
        {
            int seen = 0;

            _input.KeyDown += (sender, e) => seen++;
            _surface.Focus(_input);

            Assert.IsFalse(_surface.KeyDown(Key.A, KeyModifiers.None));
            Assert.AreEqual(1, seen);
        }

        private VisualElement Command(string shortcut)
        {
            var command = new VisualElement { Name = "command", Shortcut = shortcut };

            _root.AddChild(command);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return command;
        }

        [TestMethod]
        public void AShortcutClaimsTheKey()
        {
            Command("Ctrl+S");

            Assert.IsTrue(_surface.KeyDown(Key.S, KeyModifiers.Control));
            Assert.IsFalse(_surface.KeyDown(Key.S, KeyModifiers.None));
        }

        [TestMethod]
        public void MovingTheFocusClaimsTheKey()
        {
            Assert.IsTrue(_surface.KeyDown(Key.Tab, KeyModifiers.None));
        }

        [TestMethod]
        public void AnArrowThatScrollsSomethingClaimsTheKey()
        {
            Scroller();

            Assert.IsTrue(_surface.KeyDown(Key.Down, KeyModifiers.None));
        }

        [TestMethod]
        public void AnArrowWithNothingToScrollDoesNotClaimTheKey()
        {
            Assert.IsFalse(_surface.KeyDown(Key.Down, KeyModifiers.None));
            Assert.IsFalse(_surface.KeyDown(Key.PageDown, KeyModifiers.None));
        }

        [TestMethod]
        public void EndClaimsTheKeyOnlyWhenThereIsSomewhereToGo()
        {
            Assert.IsFalse(_surface.KeyDown(Key.End, KeyModifiers.None));

            Scroller();

            Assert.IsTrue(_surface.KeyDown(Key.End, KeyModifiers.None));
            Assert.IsTrue(_surface.KeyDown(Key.Home, KeyModifiers.None));
        }

        [TestMethod]
        public void TheBackKeyIsAnOrdinaryKeyTheTreeCanClaim()
        {
            Key seen = Key.None;

            _input.KeyDown += (sender, e) =>
            {
                seen = e.Key;
                e.Handled = true;
            };

            _surface.Focus(_input);

            Assert.IsTrue(_surface.KeyDown(Key.Back, KeyModifiers.None));
            Assert.AreEqual(Key.Back, seen);
        }

        [TestMethod]
        public void AnUnwantedBackKeyIsHandedBackToTheHost()
        {
            Assert.IsFalse(_surface.KeyDown(Key.Back, KeyModifiers.None));
        }

        [TestMethod]
        public void TheBackKeyMayBeBoundAsAShortcut()
        {
            int clicks = 0;

            Command("Back").PointerClick += (sender, e) => clicks++;

            Assert.IsTrue(_surface.KeyDown(Key.Back, KeyModifiers.None));
            Assert.AreEqual(1, clicks);
        }
    }
}
