using Ixen.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Ixen.Core.UT.Hosts
{
    [TestClass]
    public class SingleInstanceTests
    {
        private static string Name() => "test-" + Guid.NewGuid().ToString("N");

        private static SingleInstance ClaimElsewhere(string name, params string[] args)
            => Task.Run(() => SingleInstance.Claim(name, args, null)).Result;

        private static string[] Next(BlockingCollection<string[]> received)
        {
            Assert.IsTrue(received.TryTake(out string[] args, 5000), "the first instance never heard of the second launch");
            return args;
        }

        [TestMethod]
        public void TheFirstLaunchHoldsItAndASecondHandsItsArgumentsOver()
        {
            string name = Name();
            var received = new BlockingCollection<string[]>();

            using (SingleInstance first = SingleInstance.Claim(name, Array.Empty<string>(), received.Add))
            {
                Assert.IsNotNull(first);

                SingleInstance second = ClaimElsewhere(name, "--open", @"C:\Music\Zoé — live.flac", "two\nlines", "");

                Assert.IsNull(second, "the second launch is told to step aside");
                CollectionAssert.AreEqual(new[] { "--open", @"C:\Music\Zoé — live.flac", "two\nlines", "" }, Next(received),
                    "every argument arrives as it was, spaces, accents, line breaks and empty ones included");
            }
        }

        [TestMethod]
        public void ItKeepsListeningAfterTheFirstHandOver()
        {
            string name = Name();
            var received = new BlockingCollection<string[]>();

            using (SingleInstance first = SingleInstance.Claim(name, Array.Empty<string>(), received.Add))
            {
                ClaimElsewhere(name, "a");
                ClaimElsewhere(name, "b");

                Assert.AreEqual("a", Next(received)[0]);
                Assert.AreEqual("b", Next(received)[0]);
            }
        }

        [TestMethod]
        public void LaunchesAtTheSameMomentAllArrive()
        {
            string name = Name();
            var received = new BlockingCollection<string[]>();

            using (SingleInstance first = SingleInstance.Claim(name, Array.Empty<string>(), received.Add))
            {
                Task[] launches = new Task[5];

                for (int i = 0; i < launches.Length; i++)
                {
                    string argument = "file" + i;
                    launches[i] = Task.Run(() => SingleInstance.Claim(name, new[] { argument }, null));
                }

                Task.WaitAll(launches);

                var arrived = new System.Collections.Generic.HashSet<string>();

                for (int i = 0; i < launches.Length; i++)
                {
                    arrived.Add(Next(received)[0]);
                }

                Assert.AreEqual(5, arrived.Count, "five files opened at once from Explorer are five arrivals, none lost");
            }
        }

        [TestMethod]
        public void OnceTheFirstIsGoneTheNextLaunchHoldsIt()
        {
            string name = Name();

            SingleInstance first = SingleInstance.Claim(name, Array.Empty<string>(), null);
            first.Dispose();

            using (SingleInstance next = ClaimElsewhere(name))
            {
                Assert.IsNotNull(next);
            }
        }

        [TestMethod]
        public void DifferentNamesDoNotMeet()
        {
            using (SingleInstance store = SingleInstance.Claim(Name(), Array.Empty<string>(), null))
            using (SingleInstance other = ClaimElsewhere(Name()))
            {
                Assert.IsNotNull(store);
                Assert.IsNotNull(other);
            }
        }

        [TestMethod]
        public void AFaultingHandlerDoesNotStopTheListener()
        {
            string name = Name();
            var received = new BlockingCollection<string[]>();
            int calls = 0;

            using (SingleInstance first = SingleInstance.Claim(name, Array.Empty<string>(), args =>
            {
                if (calls++ == 0)
                {
                    throw new InvalidOperationException("the application's handler failed");
                }

                received.Add(args);
            }))
            {
                ClaimElsewhere(name, "first");
                ClaimElsewhere(name, "second");

                Assert.AreEqual("second", Next(received)[0]);
            }
        }
    }
}
