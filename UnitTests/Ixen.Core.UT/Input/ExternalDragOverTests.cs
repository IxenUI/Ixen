using Ixen.Core.Input;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class ExternalDragOverTests
    {
        private const int VIEWPORT = 200;

        private static readonly string[] ONE_FILE = { "/poems/swans.txt" };

        private VisualElement _root;
        private VisualElement _zone;
        private VisualElement _plain;
        private IxenSurface _surface;
        private List<string> _log;

        [TestInitialize]
        public void Setup()
        {
            _log = new List<string>();

            _root = Box("root", 0, 0, VIEWPORT, VIEWPORT);
            _zone = Box("zone", 100, 0, 60, 60);
            _plain = Box("plain", 0, 0, 60, 60);

            _zone.AllowDrop = true;
            _root.AddChildren(_plain, _zone);

            var xnsSource = new XnsSource("zone {\r\n    background: #111111\r\n}\r\n"
                + "zone:dragover {\r\n    background: #222222\r\n}");
            var registry = new StyleRegistry();

            registry.Add(xnsSource.Compile());

            _surface = new IxenSurface(_root) { Styles = registry };
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            _zone.DragEnter += (s, e) => _log.Add("enter");
            _zone.DragOver += (s, e) => _log.Add("over");
            _zone.DragLeave += (s, e) => _log.Add("leave");
            _zone.Drop += (s, e) => _log.Add("drop");
        }

        private static VisualElement Box(string name, float x, float y, float width, float height)
        {
            var element = new VisualElement { Name = name };

            element.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };
            element.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = x };
            element.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = y };
            element.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = width };
            element.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };

            return element;
        }

        private string Log => string.Join(" ", _log);

        [TestMethod]
        public void HoveringTheZoneLightsItBeforeAnythingIsDropped()
        {
            bool accepted = _surface.PointerDragOver(120, 20, ONE_FILE);

            Assert.IsTrue(accepted, "the host is told the zone would take it");
            Assert.IsTrue(_zone.HasState("dragover"));
            Assert.AreEqual("enter", Log);
        }

        [TestMethod]
        public void MovingInsideTheZoneIsAnOver()
        {
            _surface.PointerDragOver(120, 20, ONE_FILE);
            _surface.PointerDragOver(130, 30, ONE_FILE);

            Assert.AreEqual("enter over", Log);
        }

        [TestMethod]
        public void NothingUnderThePointerIsARefusal()
        {
            Assert.IsFalse(_surface.PointerDragOver(20, 20, ONE_FILE));
            Assert.AreEqual("", Log);
        }

        [TestMethod]
        public void MovingOffTheZoneLeavesIt()
        {
            _surface.PointerDragOver(120, 20, ONE_FILE);

            Assert.IsFalse(_surface.PointerDragOver(20, 20, ONE_FILE));
            Assert.IsFalse(_zone.HasState("dragover"));
            Assert.AreEqual("enter leave", Log);
        }

        [TestMethod]
        public void LeavingTheWindowLeavesTheZone()
        {
            _surface.PointerDragOver(120, 20, ONE_FILE);
            _surface.PointerDragLeave();

            Assert.IsFalse(_zone.HasState("dragover"));
            Assert.AreEqual("enter leave", Log);
        }

        [TestMethod]
        public void ADropAfterAHoverIsOneOverAndTheDrop()
        {
            _surface.PointerDragOver(120, 20, ONE_FILE);
            _surface.PointerDrop(120, 20, ONE_FILE);

            Assert.AreEqual("enter over drop", Log, "no second enter, and no doubled over");
            Assert.IsFalse(_zone.HasState("dragover"));
        }

        [TestMethod]
        public void TheSessionEndsWithTheDrop()
        {
            _surface.PointerDragOver(120, 20, ONE_FILE);
            _surface.PointerDrop(120, 20, ONE_FILE);
            _log.Clear();

            _surface.PointerDragOver(120, 20, ONE_FILE);

            Assert.AreEqual("enter", Log, "a second drag starts afresh");
        }

        [TestMethod]
        public void ARefusingZoneIsNotLitAndNotReportedAsAccepting()
        {
            _zone.DragEnter += (s, e) => e.Accepted = false;

            Assert.IsFalse(_surface.PointerDragOver(120, 20, ONE_FILE));
            Assert.IsFalse(_zone.HasState("dragover"));
        }

        [TestMethod]
        public void ALeaveWithNoSessionDoesNothing()
        {
            _surface.PointerDragLeave();

            Assert.AreEqual("", Log);
        }

        [TestMethod]
        public void AnInternalDragIsNotTakenOverByAnExternalOne()
        {
            _surface.PointerDown(20, 20, PointerButton.Left);

            Assert.IsFalse(_surface.PointerDragOver(120, 20, ONE_FILE));
            Assert.AreEqual("", Log);
        }
    }
}
