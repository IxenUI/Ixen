using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class HoverAfterLayoutTests
    {
        private const int VIEWPORT = 100;
        private const int ROW = 40;

        private List<string> _log;
        private VisualElement _root;
        private VisualElement _list;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _log = new List<string>();

            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _list = new VisualElement { Name = "list" };
            _list.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _list.Scrollable = true;

            _root.AddChild(_list);

            for (int index = 0; index < 6; index++)
            {
                var row = new VisualElement { Name = $"row{index}" };
                row.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = ROW };

                string tag = $"row{index}";

                row.PointerEnter += (s, e) => _log.Add($"enter:{tag}");
                row.PointerLeave += (s, e) => _log.Add($"leave:{tag}");

                _list.AddChild(row);
            }

            _surface = new IxenSurface(_root);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private VisualElement Hovered => _surface.HoveredElement;

        private string Log => string.Join(" ", _log);

        [TestMethod]
        public void AWheelUnderAStillPointerMovesTheHover()
        {
            _surface.PointerMove(20, 20);

            Assert.AreEqual("row0", Hovered.Name, "the pointer sits on the first row");

            _log.Clear();

            _surface.PointerWheel(20, 20, 0, -1, KeyModifiers.None);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreNotEqual("row0", Hovered.Name,
                "the content moved under a pointer that did not: the row lit up is the one that "
                + "is now under the cursor, not the one that was");
            Assert.IsTrue(Log.Contains("leave:row0"), $"and row0 was told it lost the pointer, log was: {Log}");
        }

        [TestMethod]
        public void TheHoverIsRefreshedAfterTheLayoutRatherThanAtDispatch()
        {
            _surface.PointerMove(20, 20);
            _log.Clear();

            _surface.PointerWheel(20, 20, 0, -1, KeyModifiers.None);

            Assert.AreEqual("row0", Hovered.Name,
                "at dispatch time the new layout has not run, so nothing can be re-hit-tested yet");

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreNotEqual("row0", Hovered.Name, "the pass that moves the rows is what settles it");
        }

        [TestMethod]
        public void ARefreshDuringACaptureStillTargetsTheCommonAncestor()
        {
            _surface.PointerDown(20, 20, PointerButton.Left);
            _surface.PointerMove(20, 90);

            Assert.AreEqual("list", Hovered.Name,
                "dragging off the pressed row onto a sibling leaves the row but not the list");

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("list", Hovered.Name,
                "and a layout in the middle of the drag must not hand the hover to the row under "
                + "the cursor, which is the rule Move already follows");
        }

        [TestMethod]
        public void APointerThatLeftTheSurfaceIsNotBroughtBack()
        {
            _surface.PointerMove(20, 20);
            _surface.PointerLeaveSurface();

            Assert.IsNull(Hovered);

            _surface.PointerWheel(20, 20, 0, -1, KeyModifiers.None);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsNull(Hovered,
                "the last position is stale once the pointer is gone, so refreshing from it would "
                + "hover something nobody is pointing at");
        }

        [TestMethod]
        public void APointerThatNeverArrivedHoversNothing()
        {
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsNull(Hovered, "0,0 is a real coordinate, so an unknown position must not be used as one");
        }

        [TestMethod]
        public void AStillLayoutRaisesNothing()
        {
            _surface.PointerMove(20, 20);
            _log.Clear();

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(string.Empty, Log,
                "re-hit-testing an unchanged tree finds the same element, so the chains match and "
                + "nothing is raised");
        }

        [TestMethod]
        public void RemovingWhatWasHoveredMovesTheHoverToWhatTookItsPlace()
        {
            _surface.PointerMove(20, 20);

            Assert.AreEqual("row0", Hovered.Name);

            _log.Clear();

            _list.RemoveChild(_list.ChildElements[0]);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("row1", Hovered.Name,
                "detaching clears the hover, and the pass that follows finds whatever moved up");
        }
    }
}
