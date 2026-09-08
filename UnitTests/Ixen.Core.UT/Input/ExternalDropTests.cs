using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class ExternalDropTests
    {
        private const int VIEWPORT = 200;

        private static readonly string[] ONE_FILE = { @"C:\poems\swans.txt" };

        private VisualElement _root;
        private VisualElement _zone;
        private VisualElement _label;
        private VisualElement _plain;
        private IxenSurface _surface;
        private List<string> _log;

        [TestInitialize]
        public void Setup()
        {
            _log = new List<string>();

            _root = Box("root", 0, 0, VIEWPORT, VIEWPORT);
            _zone = Box("zone", 100, 0, 60, 60);
            _label = Box("label", 0, 0, 40, 40);
            _plain = Box("plain", 0, 0, 60, 60);

            _zone.AllowDrop = true;
            _zone.AddChild(_label);
            _root.AddChildren(_plain, _zone);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Watch(_zone, "zone");
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

        private void Watch(VisualElement element, string tag)
        {
            element.DragEnter += (s, e) => _log.Add($"enter:{tag}");
            element.DragOver += (s, e) => _log.Add($"over:{tag}");
            element.DragLeave += (s, e) => _log.Add($"leave:{tag}");
            element.Drop += (s, e) => _log.Add($"drop:{tag}");
        }

        private string Log => string.Join(" ", _log);

        private void Drop(float x, float y, object data) => _surface.PointerDrop(x, y, data);

        [TestMethod]
        public void ADropFromOutsideReachesTheZoneUnderIt()
        {
            Drop(120, 20, ONE_FILE);

            Assert.AreEqual("enter:zone over:zone drop:zone", Log,
                "the whole refusal protocol runs at the drop point, in one go");
        }

        [TestMethod]
        public void ThePayloadIsWhateverTheHostHandedOver()
        {
            string[] dropped = null;

            _zone.Drop += (s, e) => dropped = e.Data as string[];

            Drop(120, 20, ONE_FILE);

            Assert.IsNotNull(dropped, "DragEventArgs.Data is an object, so a file list needs no new API");
            Assert.AreEqual(1, dropped.Length);
            Assert.AreEqual(@"C:\poems\swans.txt", dropped[0]);
        }

        [TestMethod]
        public void ItWalksUpForTheNearestZoneJustLikeADragDoes()
        {
            Drop(110, 10, ONE_FILE);

            Assert.AreEqual("enter:zone over:zone drop:zone", Log,
                "the point is over the label, and the label is not the target");
        }

        [TestMethod]
        public void ADropOnSomethingThatRefusesReachesNobody()
        {
            Drop(20, 20, ONE_FILE);

            Assert.AreEqual(string.Empty, Log, "a plain element is not a drop zone");
        }

        [TestMethod]
        public void ADropOnNothingAtAllReachesNobody()
        {
            Drop(VIEWPORT + 40, VIEWPORT + 40, ONE_FILE);

            Assert.AreEqual(string.Empty, Log);
        }

        [TestMethod]
        public void AZoneMayStillRefuseThePayload()
        {
            _zone.DragEnter += (s, e) => e.Accepted = false;

            Drop(120, 20, ONE_FILE);

            Assert.AreEqual("enter:zone leave:zone", Log,
                "a refused target is left rather than dropped on, exactly as in a drag");
        }

        [TestMethod]
        public void RefusingOnOverStillRefuses()
        {
            _zone.DragOver += (s, e) => e.Accepted = false;

            Drop(120, 20, ONE_FILE);

            Assert.AreEqual("enter:zone over:zone leave:zone", Log);
        }

        [TestMethod]
        public void NothingIsCarriedIntoTheNextDrop()
        {
            _zone.DragEnter += (s, e) => e.Accepted = false;

            Drop(120, 20, ONE_FILE);

            _log.Clear();

            Drop(120, 20, ONE_FILE);

            Assert.AreEqual("enter:zone leave:zone", Log,
                "the refusal belonged to that drop and not to the zone");
        }

        [TestMethod]
        public void TheDragOverStateIsCleanedUpAfterwards()
        {
            Drop(120, 20, ONE_FILE);

            Assert.IsFalse(_zone.HasState("dragover"),
                "a drop that has already happened must not leave the zone lit");
        }

        [TestMethod]
        public void NothingInTheTreeOfferedThePayload()
        {
            VisualElement source = _zone;
            VisualElement target = null;

            _zone.Drop += (s, e) =>
            {
                source = e.DragSource;
                target = e.Source;
            };

            Drop(120, 20, ONE_FILE);

            Assert.IsNull(source,
                "the file came from the shell, so there is no drag source");
            Assert.AreSame(_zone, target,
                "and Source is the target, which is what every other pointer event means by it");
        }

        [TestMethod]
        public void AnEmptyPayloadIsNotADrop()
        {
            Drop(120, 20, null);

            Assert.AreEqual(string.Empty, Log);
        }

        [TestMethod]
        public void ADropIsRefusedWhileAGestureOfOurOwnIsRunning()
        {
            _surface.PointerDown(20, 20, PointerButton.Left);

            Drop(120, 20, ONE_FILE);

            Assert.AreEqual(string.Empty, Log,
                "the shell owns the mouse during its own drag, so this cannot happen - "
                    + "and refusing is cheaper than saving and restoring the gesture");
        }

        [TestMethod]
        public void TheDeviceCoordinatesAreDividedByTheScale()
        {
            _surface.Scale = 2;
            _surface.ComputeLayout(VIEWPORT * 2, VIEWPORT * 2);

            Drop(240, 40, ONE_FILE);

            Assert.AreEqual("enter:zone over:zone drop:zone", Log,
                "device 240,40 is logical 120,20, which is inside the zone");
        }

        [TestMethod]
        public void AWindowWithNoZoneSaysSoToItsHost()
        {
            Assert.IsTrue(_surface.AcceptsDrops, "this tree has one");

            _zone.AllowDrop = false;
            _root.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsFalse(_surface.AcceptsDrops,
                "which is what lets Win32 stop advertising a drop cursor it cannot honour");
        }

        [TestMethod]
        public void AZoneInsideAHiddenSectionIsNotFound()
        {
            _zone.Styles.Visibility = new VisibilityStyleDescriptor { Value = Visibility.Hidden };
            _zone.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Drop(120, 20, ONE_FILE);

            Assert.AreEqual(string.Empty, Log, "the hit test already refuses what nobody can see");
        }

        [TestMethod]
        public void AZoneNobodyCanSeeIsNotAdvertisedEither()
        {
            _zone.Styles.Visibility = new VisibilityStyleDescriptor { Value = Visibility.Hidden };
            _zone.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsFalse(_surface.AcceptsDrops,
                "the demo hides seven sections at a time, so the only zone is usually unreachable");
        }

        [TestMethod]
        public void AZoneUnderAHiddenSectionIsNotAdvertisedEither()
        {
            _zone.Styles.Visibility = new VisibilityStyleDescriptor { Value = Visibility.Hidden };
            _zone.AllowDrop = false;
            _label.AllowDrop = true;
            _root.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsFalse(_surface.AcceptsDrops,
                "visibility is per element, so the walk up the ancestors is what makes this true");
        }
    }
}
