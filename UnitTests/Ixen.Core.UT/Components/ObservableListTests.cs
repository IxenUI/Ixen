using Ixen.Core.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Collections.Generic;

namespace Ixen.Core.UT.Components
{
    [TestClass]
    public class ObservableListTests
    {
        private ObservableList<string> _list;
        private int _announced;

        [TestInitialize]
        public void Setup()
        {
            _list = new ObservableList<string>();
            _announced = 0;
            _list.Changed += (sender, e) => _announced++;
        }

        [TestMethod]
        public void AddingAnnouncesOnce()
        {
            _list.Add("one");

            Assert.AreEqual(1, _announced);
            Assert.AreEqual(1, _list.Count);
        }

        [TestMethod]
        public void ARangeAnnouncesOnceForTheWholeBatch()
        {
            _list.AddRange(new[] { "one", "two", "three" });

            Assert.AreEqual(1, _announced, "three items are one change");
            Assert.AreEqual(3, _list.Count);
        }

        [TestMethod]
        public void AnEmptyRangeAnnouncesNothing()
        {
            _list.AddRange(new string[0]);
            _list.AddRange(null);

            Assert.AreEqual(0, _announced);
        }

        [TestMethod]
        public void ClearingAnEmptyListAnnouncesNothing()
        {
            _list.Clear();

            Assert.AreEqual(0, _announced);

            _list.Add("one");
            _list.Clear();

            Assert.AreEqual(2, _announced);
            Assert.AreEqual(0, _list.Count);
        }

        [TestMethod]
        public void RemovingSomethingAbsentAnnouncesNothing()
        {
            _list.Add("one");

            Assert.IsFalse(_list.Remove("two"));
            Assert.AreEqual(1, _announced, "only the add");

            Assert.IsTrue(_list.Remove("one"));
            Assert.AreEqual(2, _announced);
        }

        [TestMethod]
        public void RemovingAtAnIndexAnnounces()
        {
            _list.AddRange(new[] { "one", "two" });
            _list.RemoveAt(0);

            Assert.AreEqual(2, _announced);
            Assert.AreEqual("two", _list[0]);
        }

        [TestMethod]
        public void InsertingAnnounces()
        {
            _list.Add("two");
            _list.Insert(0, "one");

            Assert.AreEqual(2, _announced);
            Assert.AreEqual("one", _list[0]);
        }

        [TestMethod]
        public void WritingTheSameValueBackAnnouncesNothing()
        {
            _list.Add("one");
            _list[0] = "one";

            Assert.AreEqual(1, _announced, "nothing observable moved");

            _list[0] = "two";

            Assert.AreEqual(2, _announced);
            Assert.AreEqual("two", _list[0]);
        }

        [TestMethod]
        public void ItIsAnOrdinaryIndexableSource()
        {
            var list = new ObservableList<string>(new[] { "one", "two" });

            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("two", list[1]);
            Assert.AreEqual(1, list.IndexOf("two"));
            Assert.IsTrue(list.Contains("one"));
            Assert.IsFalse(list.IsReadOnly);

            var seen = new List<string>();

            foreach (string entry in list)
            {
                seen.Add(entry);
            }

            Assert.AreEqual("one,two", string.Join(",", seen));

            var copy = new string[2];

            list.CopyTo(copy, 0);

            Assert.AreEqual("one,two", string.Join(",", copy));

            var boxed = new List<string>();

            foreach (string entry in (IEnumerable<string>)list)
            {
                boxed.Add(entry);
            }

            foreach (object entry in (IEnumerable)list)
            {
                boxed.Add((string)entry);
            }

            Assert.AreEqual("one,two,one,two", string.Join(",", boxed),
                "the two interface routes agree with the struct enumerator");
        }

        [TestMethod]
        public void ANullSeedIsAnEmptyList()
        {
            var list = new ObservableList<string>(null);

            Assert.AreEqual(0, list.Count);
        }

        [TestMethod]
        public void AListWithNoFollowerStillMutates()
        {
            var list = new ObservableList<string>();

            list.Add("one");
            list.Clear();

            Assert.AreEqual(0, list.Count, "announcing to nobody is not an error");
        }
    }
}
