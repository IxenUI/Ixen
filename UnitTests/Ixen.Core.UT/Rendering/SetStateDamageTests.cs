using Ixen.Core.UT.Components.Fixtures;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class SetStateDamageTests
    {
        private const int WIDTH = 400;
        private const int HEIGHT = 300;

        private static readonly SKColor Scribble = new SKColor(255, 0, 255);

        private TwoWayComponent _component;
        private IxenSurface _surface;
        private SKBitmap _bitmap;
        private SKCanvas _canvas;

        [TestInitialize]
        public void Setup()
        {
            _component = new TwoWayComponent { Name = "start" };
            _surface = new IxenSurface(_component) { Styles = new StyleRegistry() };
            _bitmap = new SKBitmap(WIDTH, HEIGHT);
            _canvas = new SKCanvas(_bitmap);

            Frame();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _canvas.Dispose();
            _bitmap.Dispose();
        }

        private void Frame()
        {
            _surface.ComputeLayout(WIDTH, HEIGHT);
            _surface.Render(_canvas);
        }

        private void Mark(int x, int y) => _bitmap.SetPixel(x, y, Scribble);

        private bool Survived(int x, int y) => _bitmap.GetPixel(x, y) == Scribble;

        [TestMethod]
        public void ASetStateThatChangesNothingRepaintsNothing()
        {
            Mark(390, 290);
            Mark(200, 150);

            _component.Touch();
            Frame();

            Assert.IsTrue(Survived(390, 290) && Survived(200, 150),
                "a SetState asks for the bindings to be replayed, not for the component's whole view to be "
                + "painted again: a player's clock ticking every second repainted the whole window under a "
                + "grid of covers, 12 ms a second for a label of four characters");
        }

        [TestMethod]
        public void ASetStateThatChangesABoundTextRepaintsThatTextAndNothingElse()
        {
            VisualElement echo = _component.Initialize().FindByName("two_way_echo");

            int insideX = (int)(echo.X + 2);
            int insideY = (int)(echo.Y + echo.ActualHeight / 2);

            Mark(insideX, insideY);
            Mark(390, 290);

            _component.Name = "changed";
            _component.Touch();
            Frame();

            Assert.IsFalse(Survived(insideX, insideY), "the text that changed is painted again");
            Assert.IsTrue(Survived(390, 290), "and the rest of the view is left as it was");
        }
    }
}
