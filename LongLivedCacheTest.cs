using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Papirus.Cache.LongLived;
using Papirus.Tests.Base;

namespace Papirus.Cache.Tests.LongLived
{
	[TestClass]
	public class LongLivedCacheTest
	{
		private struct DataItem
		{
			public int x;
			public int y;

			public override string ToString() => $"DataItem(x={x}, y={y})";
		}

		[TestMethod, TestCategory(Categories.Unit), Owner(Owners.DmitryGrigoryev)]
		public void Insert()
		{
			var cache = new LongLivedStructCache<int, DataItem>();
			cache.AddOrUpdate(3, new DataItem { x = 8, y = 9 });
			Assert.IsTrue(cache.TryGet(3, out var result));
			Assert.AreEqual(result.x, 8);
			Assert.AreEqual(result.y, 9);
			Assert.IsFalse(cache.TryGet(5, out var result1));
			cache.CheckStateAndThrowIfCorrupted();
		}

		[TestMethod, TestCategory(Categories.Unit), Owner(Owners.DmitryGrigoryev)]
		public void Remove()
		{
			var cache = new LongLivedStructCache<int, DataItem>();
			cache.AddOrUpdate(3, new DataItem { x = 8, y = 9 });
			cache.Remove(3);
			Assert.IsFalse(cache.TryGet(3, out var result1));
			cache.CheckStateAndThrowIfCorrupted();
		}

		[TestMethod, TestCategory(Categories.Unit), Owner(Owners.DmitryGrigoryev)]
		public void RemoveAdd()
		{
			var cache = new LongLivedStructCache<int, DataItem>();
			cache.AddOrUpdate(3, new DataItem { x = 3, y = 3 });
			cache.Remove(3);
			cache.AddOrUpdate(5, new DataItem { x = 5, y = 5 });
			cache.AddOrUpdate(6, new DataItem { x = 6, y = 6 });
			Assert.IsFalse(cache.TryGet(3, out var result1));
			Assert.IsTrue(cache.TryGet(5, out var result2));
			Assert.AreEqual(result2.x, 5);
			Assert.AreEqual(result2.y, 5);
			Assert.IsTrue(cache.TryGet(6, out var result3));
			Assert.AreEqual(result3.x, 6);
			Assert.AreEqual(result3.y, 6);
			cache.CheckStateAndThrowIfCorrupted();
		}

		[TestMethod, TestCategory(Categories.Unit), Owner(Owners.DmitryGrigoryev)]
		public void MassAdd()
		{
			const int maxI = 1000;
			var cache = new LongLivedStructCache<int, DataItem>();
			for (int i = 0; i < maxI; i++)
			{
				cache.AddOrUpdate(i, new DataItem { x = i, y = 2*i });
			}
			for (int i = 0; i < maxI; i++)
			{
				Assert.IsTrue(cache.TryGet(i, out var result));
				Assert.AreEqual(result.x, i);
				Assert.AreEqual(result.y, 2*i);
			}
			Assert.IsTrue(cache.Capacity >= maxI);
			cache.CheckStateAndThrowIfCorrupted();
		}

		[TestMethod, TestCategory(Categories.Unit), Owner(Owners.DmitryGrigoryev)]
		public void MassAddRemove()
		{
			const int maxI = 1000;
			var cache = new LongLivedStructCache<int, DataItem>();
			var capacity = cache.Capacity;
			for (int i = 0; i < maxI; i++)
			{
				cache.AddOrUpdate(i, new DataItem { x = i, y = 2 * i });
				cache.AddOrUpdate(i + maxI, new DataItem { x = 3 * i, y = 4 * i });
				cache.Remove(i);
				cache.Remove(i + maxI);
			}
			for (int i = 0; i < 2 * maxI; i++)
			{
				Assert.IsFalse(cache.TryGet(i, out var result));
			}
			Assert.AreEqual(cache.Capacity, capacity);
			cache.CheckStateAndThrowIfCorrupted();
		}

		[TestMethod, TestCategory(Categories.Unit), Owner(Owners.DmitryGrigoryev)]
		public void Enumerate()
		{
			var cache = new LongLivedStructCache<int, DataItem>();
			cache.AddOrUpdate(5, new DataItem { x = 1, y = 2 });
			cache.AddOrUpdate(8, new DataItem { x = 3, y = 4 });
			cache.AddOrUpdate(9, new DataItem { x = 5, y = 6 });
			cache.Remove(8);

			CollectionAssert.AreEquivalent(
				cache.ToArray(),
				new []
				{
					new KeyValuePair<int, DataItem>(5, new DataItem { x = 1, y = 2, }),
					new KeyValuePair<int, DataItem>(9, new DataItem { x = 5, y = 6, }),
				});

			cache.CheckStateAndThrowIfCorrupted();
		}
	}
}
