using Ixen.Core.Components;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ixen.Core.UT.Threading
{
    [TestClass]
    public class ConnectivityTests
    {
        private const int VIEWPORT = 100;

        private IxenSurface _surface;
        private IxenHost _host;
        private SynchronizationContext _previous;
        private StoreComponent _component;
        private VisualElement _root;
        private Connectivity _connectivity;
        private bool _online;
        private int _calls;
        private bool _reachable;

        private class StoreComponent : Component<VisualElement>
        {
            public AsyncValue<string> Albums { get; } = new AsyncValue<string>();

            internal void Start(Func<CancellationToken, Task<string>> work, LoadOptions options)
                => Load(Albums, work, options);

            protected override void Render()
            {
            }
        }

        [TestInitialize]
        public void Setup()
        {
            _previous = SynchronizationContext.Current;

            _surface = new IxenSurface { Styles = new StyleRegistry() };
            _host = new IxenHost(_surface, () => { });

            IxenSynchronizationContext.Install(_surface);

            _online = false;
            _connectivity = new Connectivity(_surface.Post, () => _online);
            _surface.Connectivity = _connectivity;

            _component = new StoreComponent();

            _root = new VisualElement { Name = "root" };
            _root.AddChild(_component.Initialize());

            _host.Root = _root;

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

        private void Until(Func<bool> condition)
        {
            for (int round = 0; round < 500 && !condition(); round++)
            {
                Paint();
                Thread.Sleep(1);
            }

            Paint();
        }

        private void Settle()
        {
            for (int round = 0; round < 20; round++)
            {
                Paint();
            }
        }

        private Task<string> Fetch(CancellationToken token)
        {
            _calls++;

            return _reachable
                ? Task.FromResult("albums")
                : Task.FromException<string>(new InvalidOperationException("no route to the store"));
        }

        private void StartOffline(bool retryWhenOnline)
        {
            _component.Start(Fetch, new LoadOptions { RetryWhenOnline = retryWhenOnline });
            Until(() => _component.Albums.IsFailed);

            Assert.IsTrue(_component.Albums.IsFailed);
            Assert.AreEqual(1, _calls);
        }

        private void ComeBack()
        {
            _online = true;
            _reachable = true;
            _connectivity.Signal();
        }

        [TestMethod]
        public void AFailedLoadIsRetriedWhenTheNetworkComesBack()
        {
            StartOffline(true);

            ComeBack();
            Until(() => _component.Albums.IsReady);

            Assert.AreEqual("albums", _component.Albums.Value);
            Assert.AreEqual(2, _calls);
        }

        [TestMethod]
        public void OnlyWhenTheLoadAskedForIt()
        {
            StartOffline(false);

            ComeBack();
            Settle();

            Assert.IsTrue(_component.Albums.IsFailed);
            Assert.AreEqual(1, _calls);
        }

        [TestMethod]
        public void AChangeWhileStillOfflineRetriesNothing()
        {
            StartOffline(true);

            _connectivity.Signal();
            Settle();

            Assert.AreEqual(1, _calls, "a network change that leaves the device offline is no reason to try");
        }

        [TestMethod]
        public void AValueThatLoadedIsNotLoadedAgain()
        {
            _reachable = true;
            _online = true;
            _component.Start(Fetch, new LoadOptions { RetryWhenOnline = true });
            Until(() => _component.Albums.IsReady);

            _connectivity.Signal();
            Settle();

            Assert.AreEqual(1, _calls);
        }

        [TestMethod]
        public void ADetachedComponentNoLongerListens()
        {
            StartOffline(true);

            _root.RemoveChild(_root.ChildElements[0]);
            Paint();

            ComeBack();
            Settle();

            Assert.AreEqual(1, _calls);
        }

        [TestMethod]
        public void AChangeReportedFromAnotherThreadIsRaisedOnTheSurfacesThread()
        {
            int raisedOn = -1;

            _connectivity.Changed += (sender, args) => raisedOn = Thread.CurrentThread.ManagedThreadId;

            _online = true;
            Task.Run(() => _connectivity.Signal()).Wait();

            Assert.AreEqual(-1, raisedOn, "the network thread only posts");

            Until(() => raisedOn != -1);

            Assert.AreEqual(Thread.CurrentThread.ManagedThreadId, raisedOn);
            Assert.IsTrue(_connectivity.IsOnline);
        }
    }
}
