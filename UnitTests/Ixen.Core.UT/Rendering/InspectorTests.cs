using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.Globalization;
using System.Security.Cryptography;
using System.Threading;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class InspectorTests
    {
        private const int WIDTH = 400;
        private const int HEIGHT = 300;

        private static readonly SKColor Scribble = new SKColor(255, 0, 255);

        private VisualElement _root;
        private VisualElement _card;
        private IxenSurface _surface;
        private SKBitmap _bitmap;
        private SKCanvas _canvas;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };
            _root.Styles.Background = new BackgroundStyleDescriptor { Color = "#101010" };

            _card = new VisualElement { Name = "card" };
            _card.Classes.Add("wide");
            _card.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
            _card.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 60 };
            _card.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 120 };
            _card.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 80 };
            _card.Styles.Padding = new PaddingStyleDescriptor
            {
                Left = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 },
                Top = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 },
                Right = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 },
                Bottom = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 }
            };
            _card.Styles.Background = new BackgroundStyleDescriptor { Color = "#4C6EF5" };

            _root.AddChild(_card);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };

            _bitmap = new SKBitmap(WIDTH, HEIGHT);
            _canvas = new SKCanvas(_bitmap);

            Frame();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _canvas?.Dispose();
            _bitmap?.Dispose();
        }

        private void Frame()
        {
            _surface.ComputeLayout(WIDTH, HEIGHT);
            _surface.Render(_canvas);
        }

        private void Hover(int x, int y) => _surface.PointerMove(x, y);

        private SKColor At(int x, int y) => _bitmap.GetPixel(x, y);

        private int Light(int x, int y, int width, int height)
        {
            int found = 0;

            for (int row = y; row < y + height && row < HEIGHT; row++)
            {
                for (int column = x; column < x + width && column < WIDTH; column++)
                {
                    SKColor pixel = _bitmap.GetPixel(column, row);

                    if (pixel.Red > 200 && pixel.Green > 200 && pixel.Blue > 200)
                    {
                        found++;
                    }
                }
            }

            return found;
        }

        private string Hash()
        {
            using (var md5 = MD5.Create())
            {
                return string.Concat(System.Array.ConvertAll(
                    md5.ComputeHash(_bitmap.Bytes), b => b.ToString("x2")));
            }
        }

        [TestMethod]
        public void ItPaintsNothingUntilItIsAskedFor()
        {
            Hover(80, 100);
            Frame();

            string plain = Hash();

            _surface.Inspector = true;
            Frame();

            string inspecting = Hash();

            Assert.AreNotEqual(plain, inspecting, "the overlay has to be visible for anything else "
                + "here to mean something");

            _surface.Inspector = false;
            Frame();

            Assert.AreEqual(plain, Hash(), "turning it off gives the frame back exactly");
        }

        [TestMethod]
        public void TheHoveredContentBoxIsTinted()
        {
            SKColor before = At(100, 100);

            _surface.Inspector = true;
            Hover(100, 100);
            Frame();

            SKColor after = At(100, 100);

            Assert.AreNotEqual(before, after, "the content box is covered by the overlay");
        }

        [TestMethod]
        public void ThePaddingBandIsPaintedAndIsNotTheContent()
        {
            SKColor bare = At(100, 70);

            _surface.Inspector = true;
            Hover(100, 100);
            Frame();

            SKColor padding = At(100, 70);

            Assert.AreNotEqual(bare, padding,
                "the ring between the bounds and the content box is painted too - and comparing it "
                + "against the element's own colour is what discriminates, since comparing it "
                + "against the CONTENT passes whether the band is drawn or not");
            Assert.AreNotEqual(At(100, 100), padding, "and the two bands are different colours");
        }

        [TestMethod]
        public void WithNothingHoveredNothingIsPainted()
        {
            string plain = Hash();

            _surface.Inspector = true;
            Frame();

            Assert.AreEqual(plain, Hash(), "no pointer has been anywhere near the surface");
        }

        [TestMethod]
        public void AFingerNeverHoversSoItHighlightsNothing()
        {
            string plain = Hash();

            _surface.Inspector = true;
            _surface.PointerMove(100, 100, PointerKind.Touch);
            Frame();

            Assert.AreEqual(plain, Hash(),
                "a finger has no position between taps, so there is nothing to inspect");
        }

        [TestMethod]
        public void LeavingTheSurfaceTakesTheOverlayWithIt()
        {
            _surface.Inspector = true;
            Hover(100, 100);
            Frame();

            string highlighted = Hash();

            _surface.PointerLeaveSurface();
            Frame();

            Assert.AreNotEqual(highlighted, Hash(), "the overlay went with the pointer");
        }

        [TestMethod]
        public void ItIsPaintedOverALayer()
        {
            var layer = new VisualElement { Name = "layer" };

            layer.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Fixed };
            layer.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            layer.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };

            var sheet = new VisualElement { Name = "sheet" };

            sheet.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            sheet.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 60 };
            sheet.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 60 };
            sheet.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 80 };
            sheet.Styles.Background = new BackgroundStyleDescriptor { Color = "#FF000000" };

            layer.AddChild(sheet);
            _root.AddChild(layer);
            Frame();

            SKColor covered = At(130, 100);

            _surface.Inspector = true;
            Hover(60, 100);
            Frame();

            Assert.AreSame(_card, _surface.Inspect(60, 100).Element,
                "the uncovered half of the card is what is hovered");
            Assert.AreNotEqual(covered, At(130, 100),
                "the overlay is painted after the tree, layers included, so a highlight is never "
                + "hidden behind a dropdown");
        }

        [TestMethod]
        public void TurningItOnAsksForAFrame()
        {
            Assert.IsFalse(_surface.IsDirty);

            _surface.Inspector = true;

            Assert.IsTrue(_surface.IsDirty);
        }

        [TestMethod]
        public void TurningItOnTwiceAsksOnce()
        {
            _surface.Inspector = true;
            Frame();

            Assert.IsFalse(_surface.IsDirty);

            _surface.Inspector = true;

            Assert.IsFalse(_surface.IsDirty, "nothing changed, so nothing is asked for");
        }

        [TestMethod]
        public void MovingBetweenElementsRepaintsTheWholeSurface()
        {
            _surface.Inspector = true;
            Hover(100, 100);
            Frame();

            _bitmap.SetPixel(380, 280, Scribble);

            _surface.InvalidateVisual(_card);

            Hover(300, 250);
            Frame();

            Assert.AreNotEqual(Scribble, At(380, 280),
                "the label plate is measured at paint time and can land anywhere, so the honest "
                + "damage for a hover change is the whole surface. The card is invalidated first "
                + "on purpose: an EMPTY damage region already means repaint everything, so without "
                + "a small region in play this test would pass whatever the hover did");
        }

        [TestMethod]
        public void WithTheOverlayOffAMoveAsksForNothing()
        {
            Hover(100, 100);

            Assert.IsFalse(_surface.IsDirty,
                "an application that never asks for the inspector pays nothing for it");
        }

        [TestMethod]
        public void ThePlateCarriesTheLabelAboveTheBox()
        {
            Assert.AreEqual(0, Light(40, 30, 200, 28), "nothing is written there yet");

            _surface.Inspector = true;
            Hover(100, 100);
            Frame();

            Assert.IsTrue(Light(40, 30, 200, 28) > 0,
                "the label is written on a plate just above the bounds");
        }

        [TestMethod]
        public void ThePlateFlipsBelowWhenThereIsNoRoomAbove()
        {
            var top = new VisualElement { Name = "top" };

            top.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            top.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            top.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 60 };
            top.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 };

            _root.AddChild(top);
            Frame();

            _surface.Inspector = true;
            Hover(230, 10);
            Frame();

            Assert.IsTrue(Light(200, 22, 200, 28) > 0,
                "there is nothing above y 0, so the plate goes under the box instead");
        }

        [TestMethod]
        public void MovingWithinOneElementAsksForNothing()
        {
            _surface.Inspector = true;
            Hover(100, 100);
            Frame();

            Assert.IsFalse(_surface.IsDirty);

            Hover(102, 102);

            Assert.IsFalse(_surface.IsDirty, "the same element is still hovered");
        }

        [TestMethod]
        public void AskingReportsTheElementAndItsBox()
        {
            InspectorHit hit = _surface.Inspect(100, 100);

            Assert.IsNotNull(hit);
            Assert.AreSame(_card, hit.Element);
            Assert.AreEqual(40f, hit.X);
            Assert.AreEqual(60f, hit.Y);
            Assert.AreEqual(120f, hit.Width);
            Assert.AreEqual(80f, hit.Height);
            Assert.AreEqual(60f, hit.ContentX);
            Assert.AreEqual(80f, hit.ContentY);
            Assert.AreEqual(80f, hit.ContentWidth);
            Assert.AreEqual(40f, hit.ContentHeight);
            Assert.AreEqual(1, hit.Depth);
        }

        [TestMethod]
        public void AskingReportsTheLabelAndTheTrace()
        {
            InspectorHit hit = _surface.Inspect(100, 100);

            Assert.AreEqual("card.wide  120 x 80", hit.Label);
            Assert.IsNotNull(hit.Styles);
            Assert.AreEqual("card", hit.Styles.Name);
        }

        [TestMethod]
        public void EverySpotInsideTheSurfaceHasAnAnswer()
        {
            InspectorHit empty = _surface.Inspect(300, 250);

            Assert.IsNotNull(empty, "an element is hit on its geometry whether it paints or not, so "
                + "a bare spot answers with whatever contains it");
            Assert.AreSame(_root, empty.Element);
            Assert.AreEqual(0, empty.Depth);

            Assert.IsNull(_surface.Inspect(500, 500), "outside the surface there is nothing");
        }

        [TestMethod]
        public void AskingDoesNotNeedTheOverlay()
        {
            Assert.IsFalse(_surface.Inspector);
            Assert.IsNotNull(_surface.Inspect(100, 100),
                "the report is an API, the overlay is a switch, and neither needs the other");
        }

        [TestMethod]
        public void AskingChangesNothingOnScreen()
        {
            string before = Hash();

            _surface.Inspect(100, 100);
            Frame();

            Assert.AreEqual(before, Hash(),
                "explaining the styles re-runs the style pass, and the passes are idempotent");
        }

        [TestMethod]
        public void TheLabelDoesNotDependOnTheCulture()
        {
            CultureInfo previous = Thread.CurrentThread.CurrentCulture;

            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");

                _card.Styles.Width = new WidthStyleDescriptor
                {
                    Unit = SizeUnit.Pixels,
                    Value = 120.5f
                };

                _card.Invalidate();
                Frame();

                Assert.AreEqual("card.wide  120.5 x 80", _surface.Inspect(100, 100).Label,
                    "a comma there would sit next to a size and read as two numbers");
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }

        [TestMethod]
        public void AnUnnamedElementSaysSo()
        {
            var plain = new VisualElement();

            plain.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 240 };
            plain.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
            plain.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 30 };
            plain.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 30 };

            _root.AddChild(plain);
            Frame();

            Assert.AreEqual("(unnamed)  30 x 30", _surface.Inspect(250, 50).Label);

            plain.TypeName = "Button";
            _surface.Inspect(250, 50);

            Assert.AreEqual("#Button  30 x 30", _surface.Inspect(250, 50).Label,
                "a type is the next best identity after a name");
        }
    }
}
