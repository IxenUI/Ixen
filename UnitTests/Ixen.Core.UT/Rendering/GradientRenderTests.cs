using Ixen.Core.Language.Xns;
using Ixen.Core.UT.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.Linq;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class GradientRenderTests
    {
        private const int VIEWPORT = 100;

        private VisualElement _root;
        private VisualElement _box;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _box = new VisualElement { Name = "box" };
            _root.AddChild(_box);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
        }

        private static BackgroundStyleDescriptor Background(string value)
        {
            var xnsSource = new XnsSource($"box {{ background: {value} }}");
            ClassesSet set = xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));

            return (BackgroundStyleDescriptor)set.Classes.Single().Styles.Single();
        }

        private SKBitmap Render(string value)
        {
            _box.Styles.Background = Background(value);
            _box.Invalidate();

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return _surface.RenderToBitmap();
        }

        private static SKColor At(SKBitmap bitmap, int x, int y) => bitmap.GetPixel(x, y);

        private static T Parsed<T>(string style, string value) where T : StyleDescriptor
        {
            var xnsSource = new XnsSource($"box {{ {style}: {value} }}");
            ClassesSet set = xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors);

            return (T)set.Classes.Single().Styles.Single();
        }

        [TestMethod]
        public void AVerticalGradientRunsFromTopToBottom()
        {
            using (SKBitmap rendered = Render("linear-gradient(to bottom #000000 #FFFFFF)"))
            {
                Assert.IsTrue(At(rendered, 50, 2).Red < 40, "the top is the first colour");
                Assert.IsTrue(At(rendered, 50, VIEWPORT - 3).Red > 215, "and the bottom the last");
                Assert.IsTrue(At(rendered, 50, VIEWPORT / 2).Red > 100 && At(rendered, 50, VIEWPORT / 2).Red < 155,
                    "with the middle halfway between them");
            }
        }

        [TestMethod]
        public void ToTopReversesIt()
        {
            using (SKBitmap rendered = Render("linear-gradient(to top #000000 #FFFFFF)"))
            {
                Assert.IsTrue(At(rendered, 50, 2).Red > 215);
                Assert.IsTrue(At(rendered, 50, VIEWPORT - 3).Red < 40);
            }
        }

        [TestMethod]
        public void ToRightRunsSideways()
        {
            using (SKBitmap rendered = Render("linear-gradient(to right #000000 #FFFFFF)"))
            {
                Assert.IsTrue(At(rendered, 2, 50).Red < 40, "dark on the left");
                Assert.IsTrue(At(rendered, VIEWPORT - 3, 50).Red > 215, "light on the right");
                Assert.AreEqual(At(rendered, 50, 5).Red, At(rendered, 50, VIEWPORT - 6).Red,
                    "and no variation down a column");
            }
        }

        [TestMethod]
        public void AnAngleOfNinetyIsTheSameAsToRight()
        {
            byte[] keyword;
            byte[] angle;

            using (SKBitmap rendered = Render("linear-gradient(to right #000000 #FFFFFF)"))
            {
                keyword = new[] { At(rendered, 10, 50).Red, At(rendered, 50, 50).Red, At(rendered, 90, 50).Red };
            }

            using (SKBitmap rendered = Render("linear-gradient(90deg #000000 #FFFFFF)"))
            {
                angle = new[] { At(rendered, 10, 50).Red, At(rendered, 50, 50).Red, At(rendered, 90, 50).Red };
            }

            CollectionAssert.AreEqual(keyword, angle, "0deg is up and 90deg is right, as in CSS");
        }

        [TestMethod]
        public void ADiagonalVariesOnBothAxes()
        {
            using (SKBitmap rendered = Render("linear-gradient(to bottom right #000000 #FFFFFF)"))
            {
                Assert.IsTrue(At(rendered, 5, 5).Red < 60, "the top left corner is the first colour");
                Assert.IsTrue(At(rendered, VIEWPORT - 6, VIEWPORT - 6).Red > 195,
                    "and the bottom right the last");
                Assert.IsTrue(System.Math.Abs(At(rendered, 90, 10).Red - At(rendered, 10, 90).Red) < 12,
                    "the two other corners sit on the same band");
            }
        }

        [TestMethod]
        public void AStopMovesTheColourEarlier()
        {
            using (SKBitmap rendered = Render("linear-gradient(to right #000000 #FFFFFF 25%)"))
            {
                Assert.IsTrue(At(rendered, 30, 50).Red > 215,
                    "past the 25% stop everything is the last colour");
            }
        }

        [TestMethod]
        public void AThirdColourShowsInTheMiddle()
        {
            using (SKBitmap rendered = Render("linear-gradient(to right #000000 #FF0000 #000000)"))
            {
                SKColor middle = At(rendered, 50, 50);

                Assert.IsTrue(middle.Red > 180, "the middle stop is red");
                Assert.IsTrue(middle.Green < 40 && middle.Blue < 40);
                Assert.IsTrue(At(rendered, 2, 50).Red < 40, "and both ends are black");
                Assert.IsTrue(At(rendered, VIEWPORT - 3, 50).Red < 40);
            }
        }

        [TestMethod]
        public void ARadialGradientRunsFromTheCentreOutwards()
        {
            using (SKBitmap rendered = Render("radial-gradient(#FFFFFF #000000)"))
            {
                Assert.IsTrue(At(rendered, 50, 50).Red > 215, "white in the middle");
                Assert.IsTrue(At(rendered, 3, 50).Red < 90, "dark at the edge");
                Assert.IsTrue(At(rendered, 50, 3).Red < 90);
            }
        }

        [TestMethod]
        public void AGradientReplacesTheFlatColour()
        {
            using (SKBitmap rendered = Render("#FF0000 linear-gradient(to bottom #000000 #000000)"))
            {
                Assert.IsTrue(At(rendered, 50, 50).Red < 40,
                    "the gradient is drawn instead of the colour, not under it");
            }
        }

        [TestMethod]
        public void ARadiusClipsTheGradient()
        {
            _box.Styles.CornerRadius = new CornerRadiusStyleDescriptor
            {
                TopLeft = 40,
                TopRight = 40,
                BottomRight = 40,
                BottomLeft = 40
            };

            using (SKBitmap rendered = Render("linear-gradient(to bottom #000000 #000000)"))
            {
                Assert.AreEqual(0, At(rendered, 1, 1).Alpha,
                    "the cut corner is left untouched, so the surface stays transparent there");
                Assert.IsTrue(At(rendered, 50, 50).Alpha > 200, "while the middle is painted");
            }
        }

        [TestMethod]
        public void TheShaderFollowsAViewportResizeWithoutRestyling()
        {
            using (SKBitmap rendered = Render("linear-gradient(to bottom #000000 #FFFFFF)"))
            {
                Assert.IsTrue(At(rendered, 50, VIEWPORT - 3).Red > 215, "the last colour reaches the bottom");
            }

            _surface.ComputeLayout(VIEWPORT, VIEWPORT * 2);

            using (SKBitmap rendered = _surface.RenderToBitmap())
            {
                Assert.IsTrue(At(rendered, 50, VIEWPORT).Red > 100 && At(rendered, 50, VIEWPORT).Red < 155,
                    "a viewport resize does not restyle, so the same handler had to rebuild its shader");

                Assert.IsTrue(At(rendered, 50, VIEWPORT * 2 - 3).Red > 215,
                    "and the last colour now reaches the new bottom");
            }
        }

        [TestMethod]
        public void AnElementWithNoGradientStillPaintsItsColour()
        {
            using (SKBitmap rendered = Render("#FF0000"))
            {
                SKColor pixel = At(rendered, 50, 50);

                Assert.IsTrue(pixel.Red > 200 && pixel.Green < 40);
            }
        }

        [TestMethod]
        public void AnAnimationOnAnotherPropertyLeavesTheGradientAlone()
        {
            var scheduler = new FakeScheduler();
            var registry = new StyleRegistry();

            registry.Add(new KeyframesSet("spin", new System.Collections.Generic.List<Keyframe>
            {
                new Keyframe(0f, new System.Collections.Generic.List<StyleDescriptor>
                {
                    Parsed<TransformStyleDescriptor>("transform", "rotate(0deg)")
                }),
                new Keyframe(1f, new System.Collections.Generic.List<StyleDescriptor>
                {
                    Parsed<TransformStyleDescriptor>("transform", "rotate(20deg)")
                })
            }));

            _surface.Styles = registry;
            _surface.Scheduler = scheduler;

            _box.Styles.Background = Background("linear-gradient(to bottom #000000 #FFFFFF)");

            _box.Styles.Animation = new AnimationStyleDescriptor
            {
                Name = "spin",
                Duration = 160,
                Iterations = 1
            };

            _box.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            using (SKBitmap rendered = _surface.RenderToBitmap())
            {
                Assert.AreNotEqual(At(rendered, 50, 4).Red, At(rendered, 50, VIEWPORT - 5).Red,
                    "nothing animates the background, so declaring an animation at all must not "
                    + "hand the element a baseline brush that swallows its gradient");
            }
        }

        [TestMethod]
        public void ATransitionOnAnotherPropertyLeavesTheGradientAlone()
        {
            _box.Styles.Transition = new TransitionStyleDescriptor
            {
                Specs =
                {
                    {
                        Visual.Styles.StyleIdentifier.WIDTH,
                        new TransitionSpec { Duration = 120 }
                    }
                }
            };

            using (SKBitmap rendered = Render("linear-gradient(to bottom #000000 #FFFFFF)"))
            {
                Assert.AreNotEqual(At(rendered, 50, 4).Red, At(rendered, 50, VIEWPORT - 5).Red,
                    "and a transition on some other property must not either");
            }
        }

        [TestMethod]
        public void ARunningKeyframeAnimationBeatsTheGradient()
        {
            var scheduler = new FakeScheduler();
            var registry = new StyleRegistry();

            registry.Add(new KeyframesSet("fade", new System.Collections.Generic.List<Keyframe>
            {
                new Keyframe(0f, new System.Collections.Generic.List<StyleDescriptor>
                {
                    new BackgroundStyleDescriptor { Color = "#FF0000" }
                }),
                new Keyframe(1f, new System.Collections.Generic.List<StyleDescriptor>
                {
                    new BackgroundStyleDescriptor { Color = "#00FF00" }
                })
            }));

            _surface.Styles = registry;
            _surface.Scheduler = scheduler;

            _box.Styles.Background = Background("linear-gradient(to bottom #000000 #FFFFFF)");
            _box.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            using (SKBitmap rendered = _surface.RenderToBitmap())
            {
                Assert.AreNotEqual(At(rendered, 50, 2).Red, At(rendered, 50, VIEWPORT - 3).Red,
                    "with no animation the gradient paints");
            }

            _box.Styles.Animation = new AnimationStyleDescriptor
            {
                Name = "fade",
                Duration = 160,
                Iterations = 1
            };

            _box.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            scheduler.FireAll();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            using (SKBitmap rendered = _surface.RenderToBitmap())
            {
                SKColor top = At(rendered, 50, 2);
                SKColor bottom = At(rendered, 50, VIEWPORT - 3);

                Assert.AreEqual(top.Red, bottom.Red,
                    "the animation owns background while it runs, so the flat animated colour wins");
                Assert.IsTrue(top.Red > 40, "and it really is the animated red, not the gradient's black");
            }
        }
    }
}
