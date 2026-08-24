using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout.Geometry
{
    [TestClass]
    public class AnchorGeometryTests : BaseGeometryTests
    {
        private const float BUTTON = 60;
        private const float MENU_WIDTH = 100;
        private const float MENU_HEIGHT = 80;

        private static VisualElement Anchored(string anchorName, AnchorSide side, AnchorAlign align,
            bool noFlip = false)
        {
            VisualElement layer = Element("layer", LayoutType.Fixed,
                SizeUnit.Pixels, 0, SizeUnit.Pixels, 0);

            layer.Styles.Anchor = new AnchorStyleDescriptor { Name = anchorName };
            layer.Styles.AnchorPlacement = new AnchorPlacementStyleDescriptor
            {
                Side = side,
                Align = align,
                NoFlip = noFlip
            };

            VisualElement menu = Element("menu", LayoutType.Column,
                SizeUnit.Pixels, MENU_WIDTH, SizeUnit.Pixels, MENU_HEIGHT);

            layer.AddChild(menu);

            return layer;
        }

        private static VisualElement Button(string name, float left, float top)
        {
            VisualElement button = Element(name, LayoutType.Column,
                SizeUnit.Pixels, BUTTON, SizeUnit.Pixels, BUTTON);

            button.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = left };
            button.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = top };

            return button;
        }

        private static void Scene(VisualElement button, VisualElement layer)
        {
            VisualElement root = Element("root", LayoutType.Absolute);
            root.AddChild(button);
            root.AddChild(layer);

            Layout(root);
        }

        [TestMethod]
        public void BelowStartSitsUnderTheAnchorsLeadingEdge()
        {
            VisualElement button = Button("button", 50, 50);
            VisualElement layer = Anchored("button", AnchorSide.Below, AnchorAlign.Start);

            Scene(button, layer);

            AssertBox(layer.FindByName("menu"), 50, 110, MENU_WIDTH, MENU_HEIGHT);
        }

        [TestMethod]
        public void BelowCentreCentresOnTheAnchor()
        {
            VisualElement button = Button("button", 50, 50);
            VisualElement layer = Anchored("button", AnchorSide.Below, AnchorAlign.Center);

            Scene(button, layer);

            AssertBox(layer.FindByName("menu"), 30, 110, MENU_WIDTH, MENU_HEIGHT);
        }

        [TestMethod]
        public void BelowEndAlignsTheTrailingEdges()
        {
            VisualElement button = Button("button", 150, 50);
            VisualElement layer = Anchored("button", AnchorSide.Below, AnchorAlign.End);

            Scene(button, layer);

            AssertBox(layer.FindByName("menu"), 110, 110, MENU_WIDTH, MENU_HEIGHT);
        }

        [TestMethod]
        public void AboveSitsOnTopOfTheAnchor()
        {
            VisualElement button = Button("button", 50, 200);
            VisualElement layer = Anchored("button", AnchorSide.Above, AnchorAlign.Start);

            Scene(button, layer);

            AssertBox(layer.FindByName("menu"), 50, 120, MENU_WIDTH, MENU_HEIGHT);
        }

        [TestMethod]
        public void RightSitsBesideTheAnchor()
        {
            VisualElement button = Button("button", 50, 50);
            VisualElement layer = Anchored("button", AnchorSide.Right, AnchorAlign.Start);

            Scene(button, layer);

            AssertBox(layer.FindByName("menu"), 110, 50, MENU_WIDTH, MENU_HEIGHT);
        }

        [TestMethod]
        public void LeftSitsBesideTheAnchorOnTheOtherSide()
        {
            VisualElement button = Button("button", 150, 50);
            VisualElement layer = Anchored("button", AnchorSide.Left, AnchorAlign.Start);

            Scene(button, layer);

            AssertBox(layer.FindByName("menu"), 50, 50, MENU_WIDTH, MENU_HEIGHT);
        }

        [TestMethod]
        public void ABelowMenuWithNoRoomUnderneathFlipsAbove()
        {
            VisualElement button = Button("button", 50, 300);
            VisualElement layer = Anchored("button", AnchorSide.Below, AnchorAlign.Start);

            Scene(button, layer);

            AssertBox(layer.FindByName("menu"), 50, 220, MENU_WIDTH, MENU_HEIGHT);
        }

        [TestMethod]
        public void AnAboveMenuWithNoRoomOnTopFlipsBelow()
        {
            VisualElement button = Button("button", 50, 10);
            VisualElement layer = Anchored("button", AnchorSide.Above, AnchorAlign.Start);

            Scene(button, layer);

            AssertBox(layer.FindByName("menu"), 50, 70, MENU_WIDTH, MENU_HEIGHT);
        }

        [TestMethod]
        public void NoFlipKeepsTheRequestedSideEvenOffScreen()
        {
            VisualElement button = Button("button", 50, 300);
            VisualElement layer = Anchored("button", AnchorSide.Below, AnchorAlign.Start, noFlip: true);

            Scene(button, layer);

            AssertBox(layer.FindByName("menu"), 50, 360, MENU_WIDTH, MENU_HEIGHT);
        }

        [TestMethod]
        public void TheCrossAxisIsShiftedBackIntoTheViewport()
        {
            VisualElement button = Button("button", 340, 50);
            VisualElement layer = Anchored("button", AnchorSide.Below, AnchorAlign.Start);

            Scene(button, layer);

            AssertBox(layer.FindByName("menu"), VIEWPORT_WIDTH - MENU_WIDTH, 110, MENU_WIDTH, MENU_HEIGHT);
        }

        [TestMethod]
        public void NoFlipAlsoGivesUpTheCrossAxisShift()
        {
            VisualElement button = Button("button", 340, 50);
            VisualElement layer = Anchored("button", AnchorSide.Below, AnchorAlign.Start, noFlip: true);

            Scene(button, layer);

            AssertBox(layer.FindByName("menu"), 340, 110, MENU_WIDTH, MENU_HEIGHT);
        }

        [TestMethod]
        public void AnUnknownAnchorFallsBackToTheViewportOrigin()
        {
            VisualElement button = Button("button", 50, 50);
            VisualElement layer = Anchored("nobody", AnchorSide.Below, AnchorAlign.Start);

            Scene(button, layer);

            AssertBox(layer.FindByName("menu"), 0, 0, MENU_WIDTH, MENU_HEIGHT);
        }

        [TestMethod]
        public void AnAnchoredLayerIsStillClippedByTheViewportOnly()
        {
            VisualElement button = Button("button", 50, 50);
            VisualElement layer = Anchored("button", AnchorSide.Below, AnchorAlign.Start);

            Scene(button, layer);

            Assert.AreEqual(0f, layer.Clip.X);
            Assert.AreEqual(0f, layer.Clip.Y);
            Assert.AreEqual((float)VIEWPORT_WIDTH, layer.Clip.ActualWidth);
            Assert.AreEqual((float)VIEWPORT_HEIGHT, layer.Clip.ActualHeight);
        }

        [TestMethod]
        public void AnAnchorOnSomethingThatIsNotALayerDoesNothing()
        {
            VisualElement button = Button("button", 50, 50);

            VisualElement plain = Element("plain", LayoutType.Column,
                SizeUnit.Pixels, MENU_WIDTH, SizeUnit.Pixels, MENU_HEIGHT);

            plain.Styles.Anchor = new AnchorStyleDescriptor { Name = "button" };
            plain.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 5 };
            plain.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 5 };

            Scene(button, plain);

            AssertBox(plain, 5, 5, MENU_WIDTH, MENU_HEIGHT);
        }

        [TestMethod]
        public void AnUnanchoredLayerStillPlacesItsChildrenAgainstTheViewport()
        {
            VisualElement button = Button("button", 50, 50);

            VisualElement layer = Element("layer", LayoutType.Fixed,
                SizeUnit.Pixels, 0, SizeUnit.Pixels, 0);

            VisualElement menu = Element("menu", LayoutType.Column,
                SizeUnit.Pixels, MENU_WIDTH, SizeUnit.Pixels, MENU_HEIGHT);

            menu.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 7 };
            menu.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 9 };
            layer.AddChild(menu);

            Scene(button, layer);

            AssertBox(menu, 7, 9, MENU_WIDTH, MENU_HEIGHT);
        }

        [TestMethod]
        public void TwoLayersFollowTheirOwnAnchors()
        {
            VisualElement first = Button("first", 20, 20);
            VisualElement second = Button("second", 200, 200);

            VisualElement one = Anchored("first", AnchorSide.Below, AnchorAlign.Start);
            VisualElement two = Anchored("second", AnchorSide.Below, AnchorAlign.Start);
            two.FindByName("menu").Name = "menu2";

            VisualElement root = Element("root", LayoutType.Absolute);
            root.AddChildren(first, second, one, two);

            Layout(root);

            AssertBox(one.FindByName("menu"), 20, 80, MENU_WIDTH, MENU_HEIGHT);
            AssertBox(two.FindByName("menu2"), 200, 260, MENU_WIDTH, MENU_HEIGHT);
        }

        [TestMethod]
        public void ASubmenuCanAnchorToAnItemInsideAnEarlierLayer()
        {
            VisualElement button = Button("button", 40, 40);

            VisualElement menuLayer = Anchored("button", AnchorSide.Below, AnchorAlign.Start);
            VisualElement item = Element("item", LayoutType.Column,
                SizeUnit.Pixels, MENU_WIDTH, SizeUnit.Pixels, 20);
            menuLayer.FindByName("menu").AddChild(item);

            VisualElement subLayer = Anchored("item", AnchorSide.Right, AnchorAlign.Start);
            subLayer.FindByName("menu").Name = "submenu";

            VisualElement root = Element("root", LayoutType.Absolute);
            root.AddChildren(button, menuLayer, subLayer);

            Layout(root);

            AssertBox(item, 40, 100, MENU_WIDTH, 20);
            AssertBox(subLayer.FindByName("submenu"), 140, 100, MENU_WIDTH, MENU_HEIGHT);
        }
    }
}
