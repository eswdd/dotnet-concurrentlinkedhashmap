using System;
using NUnit.Framework;
using ConcurrentLinkedDictionary;

namespace ConcurrentLinkedDictionary.Test
{
    [TestFixture]
    public class SimpleMapTest
    {
        [Test]
        public void PutGet() {
            var map = new Builder<string,string> ().MaximumWeightedCapacity(100).Weigher(new StringLengthWeigher()).Build ();
            map ["a"] = "a";
            Assert.AreEqual ("a", map ["a"]);
        }

        public class StringLengthWeigher : IWeigher<string>
        {
            public int weightOf (string value)
            {
                return value.Length;
            }
        }
    }
}

