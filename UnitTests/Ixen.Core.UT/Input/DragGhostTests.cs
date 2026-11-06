using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.Linq;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class DragGhostTests
    {
        private const int VIEWPORT = 300;

        private VisualElement _root;
        private VisualElement _source;
        private VisualElement _target;
        private IxenSurface _surface;
        private object _dropped;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };

            _source = Box("source", 10, 10, "#FF0000");
            _target = Box("target", 150, 150, "#0000FF");
            _target.AllowDrop = true;
            _target.Drop += (sender, e) => _dropped = e.Data;

            _root.AddChildren(_source, _target);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            Layout();
        }

        private static VisualElement Box(string name, float left, float top, string color)
        {
            var box = new VisualElement { Name = name };
            box.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = left };
            box.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = top };
            box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
            box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
            box.Styles.Background = new BackgroundStyleDescriptor { Color = color };

            return box;
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private SKColor PixelAt(int x, int y)
        {
            using (SKBitmap bitmap = _surface.RenderToBitmap())
            {
                return bitmap.GetPixel(x, y);
            }
        }

        private void Start(float toX, float toY)
        {
            _surface.PointerDown(20, 20, PointerButton.Left);
            _surface.PointerMove(26, 20);
            Layout();
            _surface.PointerMove(toX, toY);
            Layout();
        }

        [TestMethod]
        public void AGhostFollowsThePointerAndLeavesWithTheDrag()
        {
            var ghost = new VisualElement { Name = "ghost" };
            ghost.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 30 };
            ghost.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 };

            _source.PointerDragStart += (sender, e) =>
            {
                e.Data = "song";
                e.Ghost = ghost;
                e.GhostOffsetX = 5;
                e.GhostOffsetY = 4;
            };

            Start(100, 80);

            Assert.IsNotNull(_surface.DragGhost);
            Assert.AreSame(_surface.DragGhost, ghost.Parent);
            Assert.AreEqual(95, ghost.X, 0.01f, "the pointer keeps its place on the ghost");
            Assert.AreEqual(76, ghost.Y, 0.01f);

            _surface.PointerMove(120, 90);
            Layout();

            Assert.AreEqual(115, ghost.X, 0.01f);
            Assert.AreEqual(86, ghost.Y, 0.01f);

            _surface.PointerUp(120, 90, PointerButton.Left);
            Layout();

            Assert.IsNull(_surface.DragGhost);
            Assert.IsNull(ghost.Parent, "the ghost is handed back, so it can be shown again");
        }

        [TestMethod]
        public void TheGhostIsNeverHitAndNeverAChild()
        {
            var ghost = new VisualElement { Name = "ghost" };
            ghost.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 60 };
            ghost.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 60 };

            _source.PointerDragStart += (sender, e) =>
            {
                e.Data = "song";
                e.Ghost = ghost;
                e.GhostOffsetX = 30;
                e.GhostOffsetY = 30;
            };

            bool entered = false;
            _target.DragEnter += (sender, e) => entered = true;

            Start(170, 170);

            Assert.AreEqual(2, _root.ChildElements.Count, "structural selectors still count two children");
            Assert.IsTrue(entered, "the target under the ghost is the one hovered");

            bool left = false;
            _target.DragLeave += (sender, e) => left = true;

            _surface.PointerMove(172, 172);
            Layout();

            Assert.IsFalse(left, "once the ghost sits under the pointer it must still not be what is hit");
            Assert.IsFalse(Walk(_surface.BuildAccessibilityTree()).Any(node => node.Element == ghost),
                "a screen reader is not shown the picture of what is being dragged");

            _surface.PointerUp(170, 170, PointerButton.Left);
            Layout();

            Assert.AreEqual("song", _dropped);
        }

        private static System.Collections.Generic.IEnumerable<AccessibleNode> Walk(AccessibleNode node)
        {
            yield return node;

            foreach (AccessibleNode child in node.Children)
            {
                foreach (AccessibleNode descendant in Walk(child))
                {
                    yield return descendant;
                }
            }
        }

        [TestMethod]
        public void GhostOfPaintsTheElementUnderThePointerAsItLookedWhenTheDragStarted()
        {
            _source.PointerDragStart += (sender, e) =>
            {
                e.Data = "song";
                e.GhostOf(_source);
            };

            Start(110, 210);

            Assert.IsNotNull(_surface.DragGhost);

            _source.Styles.Background = new BackgroundStyleDescriptor { Color = "#00FF00" };
            _source.Invalidate();
            Layout();

            SKColor under = PixelAt(110, 210);

            Assert.IsTrue(under.Red > 150 && under.Green < 100 && under.Blue < 100,
                "a red picture of the source sits under the pointer, though the source turned green: " + under);

            Assert.AreEqual(100, _surface.DragGhost.X, 0.01f,
                "pressed 10 units into the source, the picture keeps the pointer 10 units in");

            _surface.PointerUp(110, 210, PointerButton.Left);
            Layout();

            SKColor after = PixelAt(110, 210);

            Assert.IsTrue(after.Red < 100, "gone with the drag: " + after);
        }

        [TestMethod]
        public void ADragThatTurnsIntoAPanShowsNoGhost()
        {
            var scroller = new VisualElement { Name = "scroller", Scrollable = true };
            scroller.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            scroller.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            scroller.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            scroller.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            scroller.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };

            var tall = new VisualElement { Name = "tall" };
            tall.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 400 };
            tall.PointerDragStart += (sender, e) => e.GhostOf(tall);

            scroller.AddChild(tall);
            _root.AddChild(scroller);
            Layout();

            _surface.PointerDown(50, 80, PointerButton.Left, PointerKind.Touch);
            _surface.PointerMove(50, 60, PointerKind.Touch);
            Layout();

            Assert.IsNull(_surface.DragGhost, "nothing claimed the drag, so the finger scrolls instead");

            _surface.PointerUp(50, 60, PointerButton.Left, PointerKind.Touch);
        }

        [TestMethod]
        public void DetachingTheSourceMidDragStillTakesTheGhostAway()
        {
            _source.PointerDragStart += (sender, e) =>
            {
                e.Data = "song";
                e.GhostOf(_source);
            };

            Start(110, 210);

            _root.RemoveChild(_source);
            Layout();

            _surface.PointerUp(110, 210, PointerButton.Left);
            Layout();

            Assert.IsNull(_surface.DragGhost);
        }
    }
}
