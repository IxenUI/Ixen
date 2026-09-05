using Ixen.Core.Components;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ixen.Core.UT.Threading
{
    [TestClass]
    public class AsyncValueTests
    {
        private const int VIEWPORT = 200;

        private IxenSurface _surface;
        private IxenHost _host;
        private List<IxenErrorEventArgs> _seen;
        private SynchronizationContext _previous;
        private PoemsComponent _component;

        private class PoemsComponent : Component<VisualElement>
        {
            internal TaskCompletionSource<string> Gate;
            internal int Renders;

            public AsyncValue<string> Poems { get; } = new AsyncValue<string>();

            internal void Start()
            {
                Gate = new TaskCompletionSource<string>();
                Load(Poems, () => Gate.Task);
            }

            internal void StartWith(Func<Task<string>> work)
            {
                Load(Poems, work);
            }

            internal void StartWithNoSlot()
            {
                Load((AsyncValue<string>)null, () => Task.FromResult("x"));
            }

            protected override void Render()
            {
                Renders++;

                View.Text = Poems.IsLoading
                    ? "loading"
                    : Poems.IsFailed
                        ? "failed: " + Poems.Message
                        : Poems.HasValue
                            ? Poems.Value
                            : "idle";
            }
        }

        [TestInitialize]
        public void Setup()
        {
            _seen = new List<IxenErrorEventArgs>();
            _previous = SynchronizationContext.Current;

            _surface = new IxenSurface { Styles = new StyleRegistry() };
            _host = new IxenHost(_surface, () => { });

            _host.UnhandledError += (sender, args) =>
            {
                _seen.Add(args);
                args.Handled = true;
            };

            IxenSynchronizationContext.Install(_surface);

            _component = new PoemsComponent();

            var root = new VisualElement { Name = "root" };
            root.AddChild(_component.Initialize());

            _host.Root = root;
            Paint();
        }

        [TestCleanup]
        public void Restore()
        {
            SynchronizationContext.SetSynchronizationContext(_previous);
        }

        private void Paint()
        {
            using (var bitmap = new SKBitmap(VIEWPORT, VIEWPORT))
            using (var canvas = new SKCanvas(bitmap))
            {
                _host.Paint(canvas, VIEWPORT, VIEWPORT);
            }
        }

        private AsyncValue<string> Poems => _component.Poems;

        [TestMethod]
        public void NothingAskedForIsIdleRatherThanEmpty()
        {
            Assert.IsTrue(Poems.IsIdle, "a view has to be able to tell 'never asked' from 'asked and got nothing'");
            Assert.IsFalse(Poems.HasValue);
            Assert.IsNull(Poems.Error);
            Assert.AreEqual("idle", _component.View.Text);
        }

        [TestMethod]
        public void AskingIsLoadingBeforeAnythingArrives()
        {
            _component.Start();

            Assert.IsTrue(Poems.IsLoading);
            Assert.IsFalse(Poems.HasValue, "there is nothing to show yet");

            Paint();

            Assert.AreEqual("loading", _component.View.Text,
                "the loading state has to reach the screen without waiting for the load");
        }

        [TestMethod]
        public void AValueThatArrivesIsReady()
        {
            _component.Start();
            _component.Gate.SetResult("eight poems");

            Paint();

            Assert.IsTrue(Poems.IsReady);
            Assert.IsTrue(Poems.HasValue);
            Assert.AreEqual("eight poems", Poems.Value);
            Assert.AreEqual("eight poems", _component.View.Text);
        }

        [TestMethod]
        public void AFailureIsAStateRatherThanACrash()
        {
            _component.Start();
            _component.Gate.SetException(new InvalidOperationException("the shelf is empty"));

            Paint();

            Assert.IsTrue(Poems.IsFailed);
            Assert.AreEqual("the shelf is empty", Poems.Message);
            Assert.AreEqual("failed: the shelf is empty", _component.View.Text);
        }

        [TestMethod]
        public void AFailureCapturedIntoTheValueDoesNotAlsoReachTheErrorBoundary()
        {
            _component.Start();
            _component.Gate.SetException(new InvalidOperationException("the shelf is empty"));

            Paint();

            Assert.AreEqual(0, _seen.Count,
                "the view holds the failure and can show it, which is what observing it means - "
                + "Run is the route for work nobody is watching");
        }

        [TestMethod]
        public void AThrowBeforeAnyTaskExistsIsCapturedToo()
        {
            _component.StartWith(() => throw new InvalidOperationException("no task at all"));

            Paint();

            Assert.IsTrue(Poems.IsFailed);
            Assert.AreEqual("no task at all", Poems.Message);
        }

        [TestMethod]
        public void WorkThatHandsBackNoTaskFailsRatherThanStayingLoadingForEver()
        {
            _component.StartWith(() => null);

            Paint();

            Assert.IsTrue(Poems.IsFailed);
            Assert.IsNotNull(Poems.Error);
        }

        [TestMethod]
        public void ACancelledTaskIsAFailureAndStillCarriesAnError()
        {
            var source = new CancellationTokenSource();
            source.Cancel();

            _component.StartWith(() => Task.FromCanceled<string>(source.Token));

            Paint();

            Assert.IsTrue(Poems.IsFailed, "there is no cancelled state, so a cancellation is a failure");
            Assert.IsNotNull(Poems.Error, "and a failure a view cannot describe would be worse than none");
        }

        [TestMethod]
        public void ReloadingKeepsWhatIsAlreadyOnScreen()
        {
            _component.Start();
            _component.Gate.SetResult("eight poems");
            Paint();

            _component.Start();
            Paint();

            Assert.IsTrue(Poems.IsLoading);
            Assert.IsTrue(Poems.HasValue, "a refresh must be able to show the old list while it runs");
            Assert.AreEqual("eight poems", Poems.Value);
        }

        [TestMethod]
        public void ReloadingClearsThePreviousFailure()
        {
            _component.Start();
            _component.Gate.SetException(new InvalidOperationException("the shelf is empty"));
            Paint();

            _component.Start();

            Assert.IsTrue(Poems.IsLoading);
            Assert.IsNull(Poems.Error, "showing a stale error beside a running load would be a lie");
        }

        [TestMethod]
        public void ASupersededLoadCannotLandOnTopOfTheOneThatReplacedIt()
        {
            _component.Start();
            TaskCompletionSource<string> first = _component.Gate;

            _component.Start();
            TaskCompletionSource<string> second = _component.Gate;

            second.SetResult("the new list");
            Paint();

            first.SetResult("the old list");
            Paint();

            Assert.AreEqual("the new list", Poems.Value,
                "a slow first request answering after a second one must not win");
            Assert.IsTrue(Poems.IsReady);
        }

        [TestMethod]
        public void ASupersededFailureIsDroppedToo()
        {
            _component.Start();
            TaskCompletionSource<string> first = _component.Gate;

            _component.Start();
            TaskCompletionSource<string> second = _component.Gate;

            second.SetResult("the new list");
            Paint();

            first.SetException(new InvalidOperationException("the shelf is empty"));
            Paint();

            Assert.IsTrue(Poems.IsReady, "an abandoned request cannot report an error either");
            Assert.IsNull(Poems.Error);
        }

        [TestMethod]
        public void ResetGoesBackToIdleAndAbandonsWhatIsInFlight()
        {
            _component.Start();
            TaskCompletionSource<string> gate = _component.Gate;

            Poems.Reset();

            Assert.IsTrue(Poems.IsIdle);

            gate.SetResult("too late");
            Paint();

            Assert.IsTrue(Poems.IsIdle, "what was abandoned must not come back");
            Assert.IsFalse(Poems.HasValue);
        }

        [TestMethod]
        public void AskingMarksTheComponentDirtyStraightAway()
        {
            int before = _component.Renders;

            _component.Start();
            Paint();

            Assert.AreEqual(before + 1, _component.Renders,
                "without that the loading state would only appear when something else happened to repaint");
        }

        [TestMethod]
        public void LandingMarksItDirtyAgain()
        {
            _component.Start();
            Paint();

            int before = _component.Renders;

            _component.Gate.SetResult("eight poems");
            Paint();

            Assert.AreEqual(before + 1, _component.Renders);
        }

        [TestMethod]
        public void AFailureMarksItDirtyToo()
        {
            _component.Start();
            Paint();

            int before = _component.Renders;

            _component.Gate.SetException(new InvalidOperationException("the shelf is empty"));
            Paint();

            Assert.AreEqual(before + 1, _component.Renders,
                "a failure that never reaches the screen is the silence this feature exists to break");
            Assert.AreEqual("failed: the shelf is empty", _component.View.Text);
        }

        [TestMethod]
        public void ASupersededLandingCostsNoRender()
        {
            _component.Start();
            TaskCompletionSource<string> first = _component.Gate;

            _component.Start();
            _component.Gate.SetResult("the new list");
            Paint();

            int before = _component.Renders;

            first.SetResult("the old list");
            Paint();

            Assert.AreEqual(before, _component.Renders,
                "a dropped answer changes nothing, so it must not ask for a frame either");
        }

        [TestMethod]
        public void TheValueChangesAtAFrameBoundaryRatherThanWheneverTheAnswerArrives()
        {
            _component.Start();
            Paint();

            _component.Gate.SetResult("eight poems");

            Assert.IsTrue(Poems.IsLoading,
                "the answer is posted, so a render pass can never watch the value change under it");
            Assert.AreEqual("loading", _component.View.Text);

            Paint();

            Assert.IsTrue(Poems.IsReady);
            Assert.AreEqual("eight poems", _component.View.Text);
        }

        [TestMethod]
        public void AnAnswerFromAnotherThreadStillComesBack()
        {
            _component.Start();
            Paint();

            var worker = new Thread(() => _component.Gate.SetResult("from elsewhere"));
            worker.Start();
            worker.Join();

            for (int frame = 0; frame < 400 && !Poems.IsReady; frame++)
            {
                Paint();
                Thread.Sleep(1);
            }

            Assert.AreEqual("from elsewhere", Poems.Value);
            Assert.AreEqual("from elsewhere", _component.View.Text);
        }

        [TestMethod]
        public void AComponentThatLoadsBeforeItJoinsATreeStillSettles()
        {
            var early = new EarlyComponent();

            early.Initialize();

            Assert.IsTrue(early.Poems.IsReady, "there is no host to post to, so it settles where it is");
            Assert.AreEqual("read before attaching", early.Poems.Value);
        }

        private class EarlyComponent : Component<VisualElement>
        {
            public AsyncValue<string> Poems { get; } = new AsyncValue<string>();

            protected override void OnInitialized()
            {
                Load(Poems, () => Task.FromResult("read before attaching"));
            }
        }

        [TestMethod]
        public void ThereHasToBeSomewhereToSettle()
        {
            Assert.Throws<ArgumentNullException>(() => _component.StartWithNoSlot());
        }

        [TestMethod]
        public void NoWorkLeavesItAlone()
        {
            _component.StartWith(null);

            Assert.IsTrue(Poems.IsIdle, "nothing was asked, so nothing is loading");
        }
    }
}
