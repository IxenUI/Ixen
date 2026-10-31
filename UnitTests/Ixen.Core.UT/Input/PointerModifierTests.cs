using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class PointerModifierTests
    {
        private VisualElement _root;
        private VisualElement _box;
        private IxenSurface _surface;
        private List<string> _seen;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _box = new VisualElement { Name = "box" };
            _box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 50 };
            _box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 50 };
            _root.AddChild(_box);

            _seen = new List<string>();
            _box.PointerDown += (sender, e) => _seen.Add("down:" + e.Modifiers);
            _box.PointerUp += (sender, e) => _seen.Add("up:" + e.Modifiers);
            _box.PointerClick += (sender, e) => _seen.Add("click:" + e.Modifiers);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            _surface.ComputeLayout(200, 200);
        }

        [TestMethod]
        public void TheModifiersHeldDuringAClickReachEveryEventOfIt()
        {
            KeyModifiers held = KeyModifiers.Control | KeyModifiers.Shift;

            _surface.PointerDown(10, 10, PointerButton.Left, PointerKind.Mouse, 0, held);
            _surface.PointerUp(10, 10, PointerButton.Left, PointerKind.Mouse, 0, held);

            CollectionAssert.AreEqual(new[] { "down:Shift, Control", "up:Shift, Control", "click:Shift, Control" }, _seen);
        }

        [TestMethod]
        public void ASynthesisedClickHoldsNothing()
        {
            _surface.PointerDown(10, 10, PointerButton.Left, PointerKind.Mouse, 0, KeyModifiers.Control);
            _surface.PointerUp(10, 10, PointerButton.Left, PointerKind.Mouse, 0, KeyModifiers.Control);
            _seen.Clear();

            _box.PerformClick();

            CollectionAssert.AreEqual(new[] { "click:None" }, _seen,
                "a shortcut or a screen reader's invoke is not a click with whatever was held last");
        }
    }
}
