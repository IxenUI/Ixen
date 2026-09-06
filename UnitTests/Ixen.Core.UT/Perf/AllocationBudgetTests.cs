using Ixen.Core.Input;
using Ixen.Core.Language.Xnl;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.Text;

namespace Ixen.Core.UT.Perf
{
    [TestClass]
    public class AllocationBudgetTests
    {
        private const int ROWS = 1000;
        private const int GRID = 30;
        private const int CELLS = GRID * GRID;
        private const int WIDTH = 1280;
        private const int HEIGHT = 800;
        private const int PASSES = 40;
        private const int EVENTS = 400;
        private const int TOKENS = 2000;

        private const long ONE_EVENT = 128;
        private const long RELAYOUT = 150 * 1024;
        private const long PER_TEXT_DRAW = 200;
        private const long PER_TOKEN = 100;
        private const long STYLE_SLACK = 1024;

        private static IxenSurface Rows(int rows, out VisualElement root)
        {
            var registry = new StyleRegistry();
            var sheet = new XnsSource(
                "row { background: #2E3138  border: #3C424E 1px inner  padding: 4px  "
                + "color: #E8ECF5  font-size: 13px  height: 20px }");

            registry.Add(sheet.Compile());

            Assert.IsFalse(sheet.HasErrors);

            root = new VisualElement { Name = "page" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            for (int index = 0; index < rows; index++)
            {
                root.AddChild(new VisualElement { Name = "row", Text = "a row of text" });
            }

            var surface = new IxenSurface(root) { Styles = registry };

            surface.ComputeLayout(WIDTH, HEIGHT);

            return surface;
        }

        private static IxenSurface Grid(string cell, string text)
        {
            var registry = new StyleRegistry();
            var sheet = new XnsSource(
                "band { layout: row  height: 40px }  cell { width: 40px  " + cell + " }");

            registry.Add(sheet.Compile());

            Assert.IsFalse(sheet.HasErrors);

            var root = new VisualElement { Name = "page" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            for (int down = 0; down < GRID; down++)
            {
                var band = new VisualElement { Name = "band" };

                for (int across = 0; across < GRID; across++)
                {
                    band.AddChild(new VisualElement { Name = "cell", Text = text });
                }

                root.AddChild(band);
            }

            var surface = new IxenSurface(root) { Styles = registry };

            surface.ComputeLayout(GRID * 40, GRID * 40);

            return surface;
        }

        private static long PaintOnce(IxenSurface surface)
        {
            using (var bitmap = new SKBitmap(GRID * 40, GRID * 40))
            using (var canvas = new SKCanvas(bitmap))
            {
                return Allocations.PerPass(PASSES, () => surface.Render(canvas));
            }
        }

        [TestMethod]
        public void AFullRestyleAllocatesNothingBeyondTheRelayoutItTriggers()
        {
            IxenSurface surface = Rows(ROWS, out VisualElement root);

            long styled = Allocations.PerPass(PASSES,
                () => { root.Invalidate(); surface.ComputeLayout(WIDTH, HEIGHT); });

            long laid = Allocations.PerPass(PASSES,
                () => { root.InvalidateLayout(); surface.ComputeLayout(WIDTH, HEIGHT); });

            Assert.IsTrue(styled - laid <= STYLE_SLACK,
                $"a restyle of {ROWS} rows allocated {styled} bytes against {laid} for the relayout "
                + $"alone, so the style pass cost {styled - laid} bytes. It should cost none: shared "
                + "Default handlers, one handler per descriptor cached on the descriptor itself, and "
                + "static lambdas are what make ApplyBaseStyle's fifty-five slots and ApplyClasses' "
                + "per-declaration walk free. It allocated 484 KB a pass before that. This test "
                + "compares the two passes rather than a number, so it says nothing about the "
                + "machine it runs on and everything about the pass.");
        }

        [TestMethod]
        public void AFrameWithNothingDirtyAllocatesNothingAtAll()
        {
            IxenSurface surface = Rows(ROWS, out _);

            long each = Allocations.PerPass(PASSES, () => surface.ComputeLayout(WIDTH, HEIGHT));

            Assert.AreEqual(0, each,
                $"a frame with nothing dirty allocated {each} bytes. ComputeLayout returns before "
                + "the four passes when the viewport is unchanged and the root is clean, and a host "
                + "that paints on demand asks for that on every event it decides not to act on.");
        }

        [TestMethod]
        public void ARelayoutOfAThousandTextRowsStaysWithinItsBudget()
        {
            IxenSurface surface = Rows(ROWS, out VisualElement root);

            Allocations.Under(RELAYOUT, PASSES,
                () => { root.InvalidateLayout(); surface.ComputeLayout(WIDTH, HEIGHT); },
                $"one relayout of {ROWS} rows each carrying a line of text, against a measured floor "
                + "of about a hundred bytes a row. What it catches is an allocation that became per "
                + "element - a list, a string or a descriptor minted per row per pass. Note what it "
                + "cannot catch: the text layout cache saves the TIME of re-measuring a line, not "
                + "bytes, because MeasureCharacters fills a reused buffer either way. Ignoring the "
                + "cache costs six times the wall clock and no allocation at all, so no budget here "
                + "can pin it.");
        }

        [TestMethod]
        public void PaintingAGridOfRoundedBoxesAllocatesNothingAtAll()
        {
            IxenSurface surface = Grid(
                "background: #2E3138  border: #3C424E 1px inner  corner-radius: 3px", null);

            long each = PaintOnce(surface);

            Assert.AreEqual(0, each,
                $"one frame over {CELLS} rounded cells allocated {each} bytes, and it should be none. "
                + "This is the uniform-border path: one rounded fill and one rounded stroke a cell, "
                + "both through the reused SKRoundRect and the reused radii array, with the clip "
                + "stack a list that is cleared rather than rebuilt and every handler a shared "
                + "instance. The grid is deliberately small enough to fit the viewport: a tall "
                + "column of a thousand rows would have all but twenty-six culled, so it would "
                + "measure twenty-six rows while claiming to measure a thousand.");
        }

        [TestMethod]
        public void PaintingAGridOfPerSideBordersAllocatesNothingEither()
        {
            IxenSurface surface = Grid(
                "background: #2E3138  border: #3C424E 1px 0px 2px 0px inner", null);

            long each = PaintOnce(surface);

            Assert.AreEqual(0, each,
                $"one frame over {CELLS} cells with differing side thicknesses allocated {each} "
                + "bytes, and it should be none. Differing sides take a second path entirely - four "
                + "filled bands through the reused scratch paint rather than one stroke - so the "
                + "rounded guard above cannot stand in for this one. Note what is deliberately not "
                + "pinned here: differing side COLOURS are mitred through FillQuad, which allocates "
                + "an SKPath a side a frame, measured at 352 bytes a cell. That is a documented "
                + "cost paid only by the elements that ask for it.");
        }

        [TestMethod]
        public void PaintingTextCostsOneSkiaCallPerElementAndNoMore()
        {
            IxenSurface surface = Grid("color: #E8ECF5  font-size: 11px", "x");

            long each = PaintOnce(surface);

            Assert.IsTrue(each / CELLS <= PER_TEXT_DRAW,
                $"{each / CELLS} bytes per text draw over {CELLS} cells, against a budget of "
                + $"{PER_TEXT_DRAW}. The floor is 160 and it is not ours: SKCanvas.DrawText's string "
                + "overload allocates per call, constant whatever the length - one character and "
                + "forty both cost 160, and a text-shadow doubles it because it is a second call. So "
                + "an element that paints a box costs nothing and an element that paints text costs "
                + "160 bytes a frame, which is the largest per-element allocation left on the paint "
                + "path. Removing it means caching an SKTextBlob per text and font, which is a real "
                + "feature with a real invalidation question; this budget is here so that the day it "
                + "is done the number moves, and so that nothing else creeps in beside it. The forty "
                + "bytes of headroom are what make FontCache the thing this also pins: a face and a "
                + "font minted per draw rather than per spec is what it catches.");
        }

        [TestMethod]
        public void APointerMoveCostsOneEventArgsAndNoMore()
        {
            IxenSurface surface = Rows(40, out _);

            float x = 10;

            Allocations.Under(ONE_EVENT, EVENTS,
                () => { x = x > 300 ? 10 : x + 1; surface.PointerMove(x, 12); },
                "a move inside one element raises one PointerMove and nothing else, so it should "
                + "cost one PointerEventArgs. This is the hottest input path in the framework and it "
                + "is the reason IxenHost catches per entry point rather than through a lambda.");
        }

        [TestMethod]
        public void AMoveThatCrossesElementsReusesItsChains()
        {
            IxenSurface surface = Rows(40, out _);

            float y = 5;

            Allocations.Under(ONE_EVENT, EVENTS,
                () => { y = y > 700 ? 5 : y + 7; surface.PointerMove(20, y); },
                "a move that leaves one row and enters the next raises the move plus one batch of "
                + "enter and leave, and nothing else - because the two ancestor chains it diffs them "
                + "from are reused buffers on the dispatcher rather than lists built per move. "
                + "Crossing is what exercises them: a move that stays inside one element never "
                + "touches them at all, which is why that test cannot stand in for this one.");
        }

        [TestMethod]
        public void AKeyPressCostsOneEventArgsAndNoMore()
        {
            IxenSurface surface = Rows(40, out _);

            Allocations.Under(ONE_EVENT, EVENTS,
                () => surface.KeyDown(Key.A, KeyModifiers.None),
                "a key nobody handles bubbles to the root and is then offered to the shortcut table, "
                + "which walks a list rather than building a candidate, so it should cost one "
                + "KeyEventArgs.");
        }

        [TestMethod]
        public void AWheelNotchCostsOneEventArgsAndNoMore()
        {
            IxenSurface surface = Rows(40, out _);

            Allocations.Under(ONE_EVENT, EVENTS,
                () => surface.PointerWheel(20, 30, 0, 1),
                "a notch raises one WheelEventArgs and resolves its target by walking ancestors, so "
                + "it should allocate nothing else - the latch is a field, not a lookup.");
        }

        [TestMethod]
        public void TokenizingAStylesheetDoesNotAllocatePerReadAttempt()
        {
            var text = new StringBuilder();

            for (int index = 0; index < 200; index++)
            {
                text.AppendLine($"rule_{index} {{ width: 200px  height: 100px  "
                    + $"background: #4C6EF5  color: #FFFFFF  font-size: 13px }}");
            }

            string source = text.ToString();
            var sheet = new XnsSource(source);
            int tokens = sheet.Tokenize().Count;

            Assert.IsFalse(sheet.HasErrors, "the fixture does not tokenize cleanly");
            Assert.IsTrue(tokens > TOKENS,
                $"the fixture produced {tokens} tokens, so it stopped early and the budget below "
                + "would be measuring something else");

            long each = Allocations.PerPass(PASSES,
                () => { sheet.UpdateSource(source); sheet.Tokenize(); });

            Assert.IsTrue(each / tokens <= PER_TOKEN,
                $"{each / tokens} bytes a token over {tokens} tokens. Every reader used to allocate "
                + "a StringBuilder, its char[] and a ToString before knowing whether the read would "
                + "succeed - and ReadClassName and ReadStyleName are both tried at every declaration "
                + "position, so one of the two always threw its buffer away: 197 bytes a token. A "
                + "token's content is a Slice taken only on success, so what is left is the token, "
                + "its slot in the list and its content string. The VS extension re-tokenises the "
                + "whole buffer on every keystroke, which is what makes this the path that matters.");
        }

        [TestMethod]
        public void TokenizingAViewDoesNotAllocatePerReadAttemptEither()
        {
            var text = new StringBuilder();

            text.AppendLine("root {} [");

            for (int index = 0; index < 200; index++)
            {
                text.AppendLine($"\tel_{index} {{ text: \"some text here\" class: \"a b\" }}");
            }

            text.AppendLine("]");

            string source = text.ToString();
            var view = new XnlSource(source);
            int tokens = view.Tokenize().Count;

            Assert.IsFalse(view.HasErrors, "the fixture does not tokenize cleanly");
            Assert.IsTrue(tokens > TOKENS,
                $"the fixture produced {tokens} tokens, so it stopped early and the budget below "
                + "would be measuring something else");

            long each = Allocations.PerPass(PASSES,
                () => { view.UpdateSource(source); view.Tokenize(); });

            Assert.IsTrue(each / tokens <= PER_TOKEN,
                $"{each / tokens} bytes a token over {tokens} tokens, against 150 before the "
                + "StringBuilders went. Both languages landing at about the same figure is the tell "
                + "that what is left is structural rather than per-reader waste.");
        }
    }
}
