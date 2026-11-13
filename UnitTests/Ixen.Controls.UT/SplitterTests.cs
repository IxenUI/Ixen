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
    public class SplitterTests
    {
        private const int WIDTH = 600;
        private const int HEIGHT = 300;
        private const float BAR = 8;

        private VisualElement _root;
        private VisualElement _body;
        private VisualElement _first;
        private VisualElement _second;
        private Splitter _splitter;
        private IxenSurface _surface;
        private int _changes;

        private void Build(bool vertical = false, bool resizesNext = false, bool theme = false)
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _body = new VisualElement { Name = "body" };
            _body.Styles.Layout = new LayoutStyleDescriptor { Type = vertical ? LayoutType.Column : LayoutType.Row };
            _body.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = WIDTH };
            _body.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = HEIGHT };

            _first = new VisualElement { Name = "first" };
            _second = new VisualElement { Name = "second" };
            _splitter = new Splitter { Name = "splitter", Vertical = vertical, ResizesNext = resizesNext };

            if (!theme)
            {
                if (vertical)
                {
                    _splitter.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = BAR };
                }
                else
                {
                    _splitter.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = BAR };
                }
            }

            VisualElement filling = resizesNext ? _first : _second;

            if (vertical)
            {
                filling.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Weight, Value = 1 };
            }
            else
            {
                filling.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Weight, Value = 1 };
            }

            _body.AddChildren(_first, _splitter, _second);
            _root.AddChild(_body);

            var registry = new StyleRegistry();

            if (theme)
            {
                registry.AddLoadedDefaults();
            }

            _surface = new IxenSurface(_root) { Styles = registry };
            _splitter.SizeChanged += (sender, e) => _changes++;

            Layout();
        }

        private void Layout() => _surface.ComputeLayout(WIDTH + 40, HEIGHT + 40);

        private float MidX => _splitter.X + (_splitter.ActualWidth / 2);

        private float MidY => _splitter.Y + (_splitter.ActualHeight / 2);

        private void Drag(float dx, float dy)
        {
            _surface.PointerDown(MidX, MidY, PointerButton.Left);
            _surface.PointerMove(MidX + (dx / 2), MidY + (dy / 2));
            _surface.PointerMove(MidX + dx, MidY + dy);
            _surface.PointerUp(MidX + dx, MidY + dy, PointerButton.Left);
            Layout();
        }

        [TestMethod]
        public void TheSizeIsWrittenOnThePaneBeforeIt()
        {
            Build();

            _splitter.Size = 220;
            Layout();

            Assert.AreEqual(220f, _first.ActualWidth,
                "the splitter owns the pane's size and writes it as an inline width - which is "
                + "why the pane must not declare one in a stylesheet, where the cascade would win");
            Assert.AreEqual(WIDTH - 220f - BAR, _second.ActualWidth, "and the filling pane takes the rest");
            Assert.AreEqual(0, _changes, "an assignment is not an interaction");
        }

        [TestMethod]
        public void ASizeGivenBeforeTheSplitterHasNeighboursIsAppliedOnceItHasThem()
        {
            _splitter = new Splitter { Size = 180 };
            _splitter.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = BAR };

            var pane = new VisualElement();
            var rest = new VisualElement();
            rest.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Weight, Value = 1 };

            var body = new VisualElement();
            body.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };
            body.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = WIDTH };
            body.AddChildren(pane, _splitter, rest);

            var root = new VisualElement();
            root.AddChild(body);

            _surface = new IxenSurface(root) { Styles = new StyleRegistry() };
            _surface.ComputeLayout(WIDTH, HEIGHT);

            Assert.AreEqual(180f, pane.ActualWidth,
                "generated code assigns properties before the element joins its parent, so the "
                + "size waits for the first pass that finds a neighbour");
        }

        [TestMethod]
        public void DraggingMovesTheEdgeAndSaysSo()
        {
            Build();
            _splitter.Size = 220;
            Layout();

            Drag(50, 0);

            Assert.AreEqual(270f, _splitter.Size);
            Assert.AreEqual(270f, _first.ActualWidth);
            Assert.AreEqual(WIDTH - 270f - BAR, _second.ActualWidth);
            Assert.IsTrue(_changes > 0, "a drag is an interaction, so a two-way [Size] hears it");
            Assert.IsFalse(_splitter.IsDragging);
            Assert.IsFalse(_splitter.HasState(Splitter.DRAGGING), "the state goes with the drag");
        }

        [TestMethod]
        public void ADragStaysWithinTheBoundsAndTheRoomThereIs()
        {
            Build();
            _splitter.Size = 220;
            _splitter.Minimum = 150;
            _splitter.Maximum = 400;
            Layout();

            Drag(-500, 0);
            Assert.AreEqual(150f, _splitter.Size, "not below the minimum");

            Drag(900, 0);
            Assert.AreEqual(400f, _splitter.Size, "not past the maximum");

            _splitter.Maximum = float.PositiveInfinity;
            Drag(900, 0);
            Assert.AreEqual(WIDTH - BAR, _splitter.Size,
                "with no maximum, not past the room the parent has - the splitter itself stays "
                + "on screen");
        }

        [TestMethod]
        public void ASplitterCanSizeThePaneAfterIt()
        {
            Build(resizesNext: true);
            _splitter.Size = 200;
            Layout();

            Assert.AreEqual(200f, _second.ActualWidth);
            Assert.AreEqual(WIDTH - 200f - BAR, _first.ActualWidth);

            Drag(50, 0);

            Assert.AreEqual(150f, _second.ActualWidth,
                "moving the bar right shrinks a pane on its right - a panel docked to the right "
                + "edge, which is the other half of the layouts that use one");
        }

        [TestMethod]
        public void AVerticalSplitterSizesTheHeight()
        {
            Build(vertical: true);
            _splitter.Size = 100;
            Layout();

            Assert.AreEqual(100f, _first.ActualHeight);
            Assert.IsTrue(_splitter.HasState(Splitter.VERTICAL));

            Drag(0, 30);

            Assert.AreEqual(130f, _first.ActualHeight);
            Assert.AreEqual(HEIGHT - 130f - BAR, _second.ActualHeight);
        }

        [TestMethod]
        public void TheArrowsOfItsAxisMoveIt()
        {
            Build();
            _splitter.Size = 220;
            _splitter.Minimum = 100;
            Layout();
            _surface.Focus(_splitter);

            _surface.KeyDown(Key.Right, KeyModifiers.None);
            Assert.AreEqual(230f, _splitter.Size, "a step is 10 units");

            _surface.KeyDown(Key.Left, KeyModifiers.Shift);
            Assert.AreEqual(229f, _splitter.Size, "and 1 with Shift");

            _surface.KeyDown(Key.Down, KeyModifiers.None);
            Assert.AreEqual(229f, _splitter.Size, "the other axis is not its business");

            _surface.KeyDown(Key.Home, KeyModifiers.None);
            Assert.AreEqual(100f, _splitter.Size);

            _surface.KeyDown(Key.End, KeyModifiers.None);
            Layout();
            Assert.AreEqual(WIDTH - BAR, _splitter.Size);
            Assert.IsTrue(_changes > 0);
        }

        [TestMethod]
        public void ADoubleClickPutsBackTheFirstSize()
        {
            Build();
            _splitter.Size = 220;
            Layout();

            Drag(80, 0);
            Assert.AreEqual(300f, _splitter.Size);

            _surface.PointerDown(MidX, MidY, PointerButton.Left);
            _surface.PointerUp(MidX, MidY, PointerButton.Left);
            _surface.PointerDown(MidX, MidY, PointerButton.Left);
            _surface.PointerUp(MidX, MidY, PointerButton.Left);
            Layout();

            Assert.AreEqual(220f, _splitter.Size);
        }

        [TestMethod]
        public void AScreenReaderSeesAndSetsTheSize()
        {
            Build();
            _splitter.Size = 220;
            _splitter.Minimum = 120;
            _splitter.Label = "sidebar width";
            Layout();

            AccessibleNode node = Find(_surface.BuildAccessibilityTree(), "sidebar width");

            Assert.IsNotNull(node);
            Assert.AreEqual(AccessibleRole.Slider, node.Role,
                "a focusable bar with a position is ARIA's window splitter, which every bridge "
                + "already speaks as a slider with a range");
            Assert.IsTrue(node.HasRange);
            Assert.AreEqual(120f, node.RangeMinimum);
            Assert.AreEqual(WIDTH - BAR, node.RangeMaximum);
            Assert.AreEqual(220f, node.RangeValue);

            Assert.IsTrue(_surface.Perform(node, AccessibleActions.SetRangeValue, "250"));
            Layout();

            Assert.AreEqual(250f, _first.ActualWidth);
        }

        [TestMethod]
        public void TheDefaultThemeGivesTheBarItsThicknessAndCursor()
        {
            Build(theme: true);
            _splitter.Size = 220;
            Layout();

            Assert.AreEqual(7f, _splitter.ActualWidth);
            Assert.AreEqual(220f, _first.ActualWidth);
            Assert.AreEqual("ew-resize", _surface.ExplainStyles(_splitter).ValueOf("cursor"));
            Assert.AreEqual("#Splitter:horizontal", _surface.ExplainStyles(_splitter).WinnerOf("width").Selector);
        }

        [TestMethod]
        public void APaneWhoseThemeDeclaresAWidthIsSizedAllTheSame()
        {
            var tree = new TreeView();
            _splitter = new Splitter { Size = 180 };

            var rest = new VisualElement();
            rest.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Weight, Value = 1 };

            var body = new VisualElement();
            body.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };
            body.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = WIDTH };
            body.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = HEIGHT };
            body.AddChildren(tree, _splitter, rest);

            var root = new VisualElement();
            root.AddChild(body);

            var registry = new StyleRegistry();
            registry.AddLoadedDefaults();

            _surface = new IxenSurface(root) { Styles = registry };
            _surface.ComputeLayout(WIDTH, HEIGHT);

            Assert.AreEqual("#TreeView", _surface.ExplainStyles(tree).WinnerOf("width").Selector,
                "the premise: the theme's width: 1* is the rule that wins the pane's width, and a "
                + "rule beats an inline style - found in the store, where the sidebar took half "
                + "the window");
            Assert.AreEqual(180f, tree.ActualWidth,
                "so the splitter also bounds the pane with min-width and max-width, which no "
                + "theme declares");
        }

        [TestMethod]
        public void AFingerDragMovesTheBarRatherThanScrolling()
        {
            Build();
            _body.Scrollable = true;
            _second.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 900 };
            _second.Invalidate();
            _splitter.Size = 220;
            Layout();

            Assert.IsTrue(_body.MaxScrollX > 0, "the row overflows, so an unclaimed finger would pan it");

            _surface.PointerDown(MidX, MidY, PointerButton.Left, PointerKind.Touch);
            _surface.PointerMove(MidX + 20, MidY, PointerKind.Touch);
            _surface.PointerMove(MidX + 40, MidY, PointerKind.Touch);
            _surface.PointerUp(MidX + 40, MidY, PointerButton.Left, PointerKind.Touch);
            Layout();

            Assert.AreEqual(260f, _splitter.Size, "the splitter claims the drag");
            Assert.AreEqual(0f, _body.ScrollX, "and the row behind it did not move");
        }

        private static AccessibleNode Find(AccessibleNode node, string name)
        {
            if (node.Name == name)
            {
                return node;
            }

            return node.Children.Select(child => Find(child, name)).FirstOrDefault(found => found != null);
        }
    }
}
