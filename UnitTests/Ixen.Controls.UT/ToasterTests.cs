using Ixen.Core;
using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class ToasterTests
    {
        private const int WIDTH = 600;
        private const int HEIGHT = 400;

        private VisualElement _root;
        private Toaster _toaster;
        private IxenSurface _surface;
        private FakeScheduler _scheduler;
        private int _dismissed;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            var page = new VisualElement { Name = "page" };
            page.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = HEIGHT };

            _toaster = new Toaster { Name = "toaster" };
            _toaster.Dismissed += (sender, e) => _dismissed++;

            _root.AddChildren(page, _toaster);

            var styles = new StyleRegistry();
            styles.AddLoadedDefaults();

            _scheduler = new FakeScheduler();
            _surface = new IxenSurface(_root) { Styles = styles, Scheduler = _scheduler, ReducedMotion = true };

            Layout();
        }

        private void Layout() => _surface.ComputeLayout(WIDTH, HEIGHT);

        private VisualElement Toast => _toaster.Toast;

        private static void Drawn(VisualElement element, float x, float y, out float drawnX, out float drawnY)
        {
            drawnX = x;
            drawnY = y;

            for (VisualElement e = element; e != null; e = e.Parent)
            {
                if (e.HasTransform)
                {
                    Transforms.Of(e).Map(drawnX, drawnY, out drawnX, out drawnY);
                }
            }
        }

        private float Top
        {
            get
            {
                Drawn(Toast, Toast.X, Toast.Y, out _, out float y);
                return y;
            }
        }

        private float CentreX
        {
            get
            {
                Drawn(Toast, Toast.X + Toast.ActualWidth / 2, Toast.Y, out float x, out _);
                return x;
            }
        }

        private float CentreY => Top + Toast.ActualHeight / 2;

        private bool Visible => Top >= 0 && Top + Toast.ActualHeight <= HEIGHT;

        private static IEnumerable<AccessibleNode> Walk(AccessibleNode node)
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

        private bool Announced(string text)
            => Walk(_surface.BuildAccessibilityTree()).Any(node => node.Name == text || node.Value == text);

        [TestMethod]
        public void AToastAppearsAtTheBottomCentreAndIsAnnounced()
        {
            _toaster.Show("Added to Road trip");
            Layout();

            Assert.IsTrue(_toaster.IsShowing);
            Assert.IsTrue(Toast.HasState(Toaster.SHOWN));
            Assert.IsTrue(Visible, "on screen once shown: y = " + Top);
            Assert.AreEqual(WIDTH / 2f, CentreX, 1f, "centred");
            Assert.AreEqual(HEIGHT - 24, Top + Toast.ActualHeight, 1f, "24 units above the bottom");
            Assert.AreEqual(LiveRegionKind.Polite, _toaster.Toast.ChildElements[0].LiveRegion,
                "a screen reader hears it without the focus moving");
            Assert.IsTrue(Announced("Added to Road trip"));
        }

        [TestMethod]
        public void BeforeAndAfterItIsNeitherSeenHitNorRead()
        {
            Assert.IsFalse(Visible, "parked below the window");
            Assert.IsFalse(Announced("Added to Road trip"));

            _toaster.Show("Added to Road trip");
            Layout();
            _toaster.Dismiss();
            Layout();

            Assert.IsFalse(Visible);
            Assert.IsFalse(Announced("Added to Road trip"), "a dismissed toast is not left for a screen reader to find");
            Assert.AreNotSame(Toast, _surface.HitTest(CentreX, CentreY));
        }

        [TestMethod]
        public void ItGoesAwayByItselfAfterItsDuration()
        {
            _toaster.Show("Saved");
            Layout();

            _scheduler.FireAll();
            Layout();

            Assert.IsFalse(_toaster.IsShowing);
            Assert.AreEqual(1, _dismissed);
        }

        [TestMethod]
        public void ToastsWaitTheirTurn()
        {
            _toaster.Show("first");
            _toaster.Show("second");
            _toaster.Show("third");

            Assert.AreEqual("first", _toaster.Message);
            Assert.AreEqual(2, _toaster.Pending);

            _scheduler.FireAll();

            Assert.AreEqual("second", _toaster.Message, "the next one comes up as the first goes");

            _scheduler.FireAll();
            _scheduler.FireAll();

            Assert.IsFalse(_toaster.IsShowing);
            Assert.AreEqual(3, _dismissed);
        }

        [TestMethod]
        public void APointerRestingOnItHoldsIt()
        {
            _toaster.Show("Removed from library", "Undo");
            Layout();

            _surface.PointerMove(CentreX, CentreY);
            Layout();
            _scheduler.FireAll();

            Assert.IsTrue(_toaster.IsShowing, "nobody reads a toast that leaves from under the pointer");

            _surface.PointerMove(10, 10);
            Layout();
            _scheduler.FireAll();

            Assert.IsFalse(_toaster.IsShowing, "and it leaves once the pointer has gone");
        }

        [TestMethod]
        public void TheActionRunsOnceAndTakesTheToastAway()
        {
            int undone = 0;
            _toaster.Show("Removed from library", "Undo", () => undone++);
            Layout();

            VisualElement action = Toast.ChildElements[1];

            Assert.AreEqual("Undo", action.Text);

            Drawn(action, action.X + action.ActualWidth / 2, action.Y + action.ActualHeight / 2, out float x, out float y);

            _surface.PointerDown(x, y, PointerButton.Left);
            _surface.PointerUp(x, y, PointerButton.Left);
            Layout();

            Assert.AreEqual(1, undone);
            Assert.IsFalse(_toaster.IsShowing);
        }

        [TestMethod]
        public void AToastWithoutAnActionOffersNone()
        {
            _toaster.Show("Saved");
            Layout();

            VisualElement action = Toast.ChildElements[1];

            Assert.IsFalse(action.Focusable, "no tab stop to an invisible button");
            Assert.AreEqual(Visibility.Hidden, action.Styles.Visibility.Value);
        }

        [TestMethod]
        public void AFocusedActionIsNotTakenAway()
        {
            _toaster.Show("Removed from library", "Undo");
            Layout();

            _surface.Focus(Toast.ChildElements[1]);
            _scheduler.FireAll();

            Assert.IsTrue(_toaster.IsShowing, "the keyboard user reaching for Undo keeps it");
        }

        [TestMethod]
        public void WithoutASchedulerItStaysUntilDismissed()
        {
            _surface.Scheduler = null;

            _toaster.Show("Saved");
            Layout();

            Assert.IsTrue(_toaster.IsShowing);

            _toaster.Dismiss();

            Assert.IsFalse(_toaster.IsShowing);
        }
    }
}
