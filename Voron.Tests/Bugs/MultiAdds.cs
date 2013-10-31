﻿using System.Text;

namespace Voron.Tests.Bugs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;

	using Voron.Debugging;
	using Voron.Impl;
	using Voron.Trees;

	using Xunit;

	public class MultiAdds
	{
		readonly Random _random = new Random(1234);
		private int _incrementedIndex = 0;
        private string RandomString(int size)
        {
            var builder = new StringBuilder();

// ReSharper disable once SpecifyACultureInStringConversionExplicitly
	        var indexString = _incrementedIndex.ToString().PadLeft(4, '0');

            for (int i = 0; i < size - 5; i++)
            {
                builder.Append(Convert.ToChar(Convert.ToInt32(Math.Floor(26 * _random.NextDouble() + 65))));
            }
			_incrementedIndex++;
			return builder.Append("-").Append(indexString).ToString();
        }

		[Fact]
		public void MultiAdds_And_MultiDeletes_After_Causing_PageSplit_DoNot_Fail()
		{
			using (var Env = new StorageEnvironment(StorageEnvironmentOptions.GetInMemory()))
			{
				var inputData = new List<byte[]>();
				for (int i = 0; i < 3000; i++)
				{
                    inputData.Add(Encoding.UTF8.GetBytes(RandomString(1024)));
				}

				using (var tx = Env.NewTransaction(TransactionFlags.ReadWrite))
				{
					Env.CreateTree(tx, "foo");
					tx.Commit();
				}

				using (var tx = Env.NewTransaction(TransactionFlags.ReadWrite))
				{
					var tree = tx.GetTree("foo");
					foreach (var buffer in inputData)
					{						
						Assert.DoesNotThrow(() => tree.MultiAdd(tx, "ChildTreeKey", new Slice(buffer)));
					}
					tx.Commit();
				}

				using (var tx = Env.NewTransaction(TransactionFlags.ReadWrite))
				{
					var tree = tx.GetTree("foo");
					for (int i = 0; i < inputData.Count; i++)
					{
						var buffer = inputData[i];
//						if (i >= 1113)
//						{
//							using(var treeIterator = (TreeIterator)tree.MultiRead(tx, "ChildTreeKey"))
//								DebugStuff.RenderAndShow(tx, treeIterator.TreeRootPage,1);
//						}
						Assert.DoesNotThrow(() => tree.MultiDelete(tx, "ChildTreeKey", new Slice(buffer)));
					}

					tx.Commit();
				}
			}
		}

		[Fact]
		public void SplitterIssue()
		{
			const int DocumentCount = 10;

			using (var env = new StorageEnvironment(StorageEnvironmentOptions.GetInMemory()))
			{
				var rand = new Random();
				var testBuffer = new byte[168];
				rand.NextBytes(testBuffer);

				var multiTrees = CreateTrees(env, 1, "multitree");

				for (var i = 0; i < 50; i++)
				{
					AddMultiRecords(env, multiTrees, DocumentCount, true);

					ValidateMultiRecords(env, multiTrees, DocumentCount, i + 1);
				}
			}
		}

		private void ValidateRecords(StorageEnvironment env, IEnumerable<Tree> trees, int documentCount, int i)
		{
			using (var tx = env.NewTransaction(TransactionFlags.Read))
			{
				foreach (var tree in trees)
				{
					using (var iterator = tree.Iterate(tx))
					{
						Assert.True(iterator.Seek(Slice.BeforeAllKeys));

						var count = 0;
						do
						{
							count++;
						}
						while (iterator.MoveNext());

						Assert.Equal(i * documentCount, tree.State.EntriesCount);
						Assert.Equal(i * documentCount, count);
					}
				}
			}
		}

		private void ValidateMultiRecords(StorageEnvironment env, IEnumerable<string> trees, int documentCount, int i)
		{
			using (var tx = env.NewTransaction(TransactionFlags.Read))
			{
				for (var j = 0; j < 10; j++)
				{
					foreach (var treeName in trees)
					{
					    var tree = tx.GetTree(treeName);
						using (var iterator = tree.MultiRead(tx, (j % 10).ToString()))
						{
							Assert.True(iterator.Seek(Slice.BeforeAllKeys));

							var count = 0;
							do
							{
								count++;
							}
							while (iterator.MoveNext());

							Assert.Equal((i * documentCount) / 10, count);
						}
					}
				}
			}
		}

		private void AddRecords(StorageEnvironment env, IList<Tree> trees, int documentCount, byte[] testBuffer, bool sequential)
		{
			var key = Guid.NewGuid().ToString();
			var batch = new WriteBatch();

			for (int i = 0; i < documentCount; i++)
			{
				foreach (var tree in trees)
				{
					var id = sequential ? string.Format("tree_{0}_record_{1}_key_{2}", tree.Name, i, key) : Guid.NewGuid().ToString();

					batch.Add(id, new MemoryStream(testBuffer), tree.Name);
				}
			}

			env.Writer.Write(batch);
		}

		private void AddMultiRecords(StorageEnvironment env, IList<string> trees, int documentCount, bool sequential)
		{
			var key = Guid.NewGuid().ToString();
			var batch = new WriteBatch();

			for (int i = 0; i < documentCount; i++)
			{
				foreach (var tree in trees)
				{
					var value = sequential ? string.Format("tree_{0}_record_{1}_key_{2}", tree, i, key) : Guid.NewGuid().ToString();

					batch.MultiAdd((i % 10).ToString(), value, tree);
				}
			}

			env.Writer.Write(batch);
		}

		private IList<string> CreateTrees(StorageEnvironment env, int number, string prefix)
		{
			var results = new List<string>();

			using (var tx = env.NewTransaction(TransactionFlags.ReadWrite))
			{
				for (var i = 0; i < number; i++)
				{
					results.Add(env.CreateTree(tx, prefix + i).Name);
				}

				tx.Commit();
			}

			return results;
		}
	}
}