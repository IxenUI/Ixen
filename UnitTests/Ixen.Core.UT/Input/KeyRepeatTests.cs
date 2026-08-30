using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class KeyRepeatTests
    {
        private const int VIEWPORT = 200;

        private List<string> _log;
        private VisualElement _root;
        private VisualElement _input;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _log = new List<string>();

            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _input = new VisualElement { Name = "input", Focusable = true };
            _root.AddChild(_input);

            _input.KeyDown += (s, e) => _log.Add(e.IsRepeat ? $"repeat:{e.Key}" : $"down:{e.Key}");
            _input.KeyUp += (s, e) => _log.Add(e.IsRepeat ? $"UPREPEAT:{e.Key}" : $"up:{e.Key}");

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            _surface.Focus(_input);
        }

        private string Log => string.Join(" ", _log);

        private void Down(Key key) => _surface.KeyDown(key, KeyModifiers.None);

        private void Up(Key key) => _surface.KeyUp(key, KeyModifiers.None);

        [TestMethod]
        public void HoldingAKeyRepeatsItAfterTheFirstPress()
        {
            Down(Key.A);
            Down(Key.A);
            Down(Key.A);
            Up(Key.A);

            Assert.AreEqual("down:A repeat:A repeat:A up:A", Log,
                "the platform sends a KeyDown per repeat and said nothing about it, so a shortcut "
                + "could not tell one press from a held key");
        }

        [TestMethod]
        public void ReleasingResetsIt()
        {
            Down(Key.A);
            Up(Key.A);
            Down(Key.A);

            Assert.AreEqual("down:A up:A down:A", Log, "a second real press is not a repeat");
        }

        [TestMethod]
        public void EachKeyIsTrackedOnItsOwn()
        {
            Down(Key.A);
            Down(Key.B);
            Down(Key.A);
            Down(Key.B);

            Assert.AreEqual("down:A down:B repeat:A repeat:B", Log,
                "two keys held together each keep their own state, which a single flag could not do");
        }

        [TestMethod]
        public void ReleasingOneKeyLeavesTheOtherHeld()
        {
            Down(Key.A);
            Down(Key.B);
            Up(Key.A);
            Down(Key.B);
            Down(Key.A);

            Assert.AreEqual("down:A down:B up:A repeat:B down:A", Log);
        }

        [TestMethod]
        public void AKeyUpIsNeverARepeat()
        {
            Down(Key.A);
            Down(Key.A);
            Up(Key.A);
            Up(Key.A);

            Assert.IsFalse(Log.Contains("UPREPEAT"),
                "a release is a release; the flag describes the press that is being held");
        }

        [TestMethod]
        public void AHandlerCanAcceptTheFirstPressAndIgnoreTheRest()
        {
            int fired = 0;

            _input.KeyDown += (s, e) =>
            {
                if (!e.IsRepeat)
                {
                    fired++;
                }
            };

            Down(Key.Enter);
            Down(Key.Enter);
            Down(Key.Enter);

            Assert.AreEqual(1, fired, "which is the whole point: a shortcut fires once for a held key");
        }

        [TestMethod]
        public void ARepeatStillMovesTheFocus()
        {
            var second = new VisualElement { Name = "second", Focusable = true };
            _root.AddChild(second);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            _surface.Focus(_input);

            _surface.KeyDown(Key.Tab, KeyModifiers.None);

            Assert.AreSame(second, _surface.FocusedElement);

            _surface.KeyDown(Key.Tab, KeyModifiers.None);

            Assert.AreSame(_input, _surface.FocusedElement,
                "holding Tab keeps moving, so marking a repeat must not suppress what the key does");
        }

        [TestMethod]
        public void AnUnknownKeyIsNeverARepeat()
        {
            Down(Key.None);
            Down(Key.None);

            Assert.AreEqual("down:None down:None", Log,
                "Key.None is not a key anybody holds, and the table is indexed by the enum value, "
                + "so it must not be tracked as one");
        }
    }
}
