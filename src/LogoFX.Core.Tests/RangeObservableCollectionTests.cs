using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace LogoFX.Core.Tests
{
    [TestFixture]    
    public class RangeObservableCollectionTests
    {
        [Test]
        public void Ctor_DoesntThrow()
        {
            Assert.DoesNotThrow(() => new RangeObservableCollection<int>());
        }

#if NET45
[Test]
        public void Performance_MonitorNumberOfAllocatedThreads()
        {
            int maxNumberOfThreads = 0;

            int currentNumberOfThreads = Process.GetCurrentProcess().Threads.Count;

            Console.WriteLine("Number of threads before run {0}", currentNumberOfThreads);
            for (int j = 0; j < 100; j++)
            {
                var threadSafe = new RangeObservableCollection<int>();
                for (int i = 0; i < 100; i++)
                {
                    threadSafe.Add(i);

                    if (i % 10 == 0)
                    {
                        int tmp = Process.GetCurrentProcess().Threads.Count;
                        if (tmp > maxNumberOfThreads)
                        {
                            maxNumberOfThreads = tmp;
                        }
                    }
                }
            }
            Console.WriteLine("Max number of threads  {0}", maxNumberOfThreads);
            Assert.That(maxNumberOfThreads - currentNumberOfThreads, Is.LessThan(10), "too many threads created");
        }
#endif


        [Test]
        public void Read_CheckLengthAfterAdd_LengthIsUpdated()
        {
            var col = new RangeObservableCollection<int>();
            col.Add(1);

            Assert.That(col.Count, Is.EqualTo(1));
        }

        [Test]
        public void Read_ExceptionDuringEnumeration_LockReleased()
        {
            var array = new int[100];
            for (int i = 0; i < 100; i++)
            {
                array[i] = i;
            }
            var col = new RangeObservableCollection<int>(array);

            try
            {
                int x = 0;
                col.ForEach(c =>
                {
                    if (x++ > 50)
                    {
                        throw new Exception();
                    }
                });
            }
            catch (Exception)
            {
                Console.WriteLine("Exception was fired");
            }

            col.Add(3);

            Assert.That(col.Count, Is.EqualTo(101));
        }

        [Test]
        public void Write_AddElement_ElementAdded()
        {
            var col = new RangeObservableCollection<string>();
            col.Add("a");
            Assert.That(col.First(), Is.EqualTo("a"));
        }

        [Test]
        public void Write_AddNull_ElementAdded()
        {
            var col = new RangeObservableCollection<string>();
            var expected = new[] { "a", null };
            col.AddRange(expected);
            CollectionAssert.AreEquivalent(expected, col);
        }

        [Test]
        public void Write_AddRange_ElementsAdded()
        {
            var col = new RangeObservableCollection<string>();
            var expected = new[] { "a", "b" };
            col.AddRange(expected);
            CollectionAssert.AreEquivalent(expected, col);
        }

        [Test]
        public void Write_ComplexOperation_CollectionUpdatedProperly()
        {
            var col = new RangeObservableCollection<string>(new[] { "a", "b", "c" });

            col.Add("d");
            col.Remove("b");
            col.Insert(0, "x");
            col.AddRange(new[] { "z", "f", "y" });
            col.RemoveAt(4);
            col.RemoveRange(new[] { "y", "c" });
            col[2] = "p";
            CollectionAssert.AreEquivalent(new[] { "x", "a", "p", "f" }, col);
        }

        [Test]
        public void AddRange_5SequentialAdds_CollectionChangeEventsAreReported()
        {
            var col = new RangeObservableCollection<string>(new[] { "a" });

            var argsList = new List<NotifyCollectionChangedEventArgs>();
            col.CollectionChanged += (sender, args) =>
            {
                argsList.Add(args);
            };
            col.AddRange(new[] { "z1", "f1", "y1" });
            col.AddRange(new[] { "z2", "f2", "y2" });
            col.AddRange(new[] { "z3", "f3", "y3" });
            col.AddRange(new[] { "z4", "f4", "y4" });
            col.AddRange(new[] { "z5", "f5", "y5" });

            Assert.That(argsList.Count(x => x.Action == NotifyCollectionChangedAction.Add), Is.EqualTo(5));
            foreach (var args in argsList)
            {
                CollectionAssert.AreEquivalent(args.NewItems, col.Skip(args.NewStartingIndex).Take(args.NewItems.Count));
            }
            CollectionAssert.AreEquivalent(new[] { "a", "z1", "f1", "y1", "z2", "f2", "y2", "z3", "f3", "y3", "z4", "f4", "y4", "z5", "f5", "y5" }, col);
        }

        [Test]
        public void Write_FiresAddEvent()
        {
            var col = new RangeObservableCollection<string>();
            string received = string.Empty;
            col.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                {
                    received = args.NewItems.OfType<string>().First();
                }
            };
            col.Add("a");
            Assert.That(received, Is.EqualTo("a"));
        }

        [Test]
        public void AddRange_FiresAddEvent()
        {
            var col = new RangeObservableCollection<string>();
            string received = string.Empty;
            col.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                {
                    received = args.NewItems.OfType<string>().First();
                }
            };
            col.AddRange(new[] { "a", "b", "c" });
            Assert.That(received, Is.EqualTo("a"));
        }

        [Test]
        public void RemoveRange_FiresRemoveEvent()
        {
            var col = new RangeObservableCollection<string>(new[] { "a", "b", "c" });
            string received = string.Empty;
            col.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Remove)
                {
                    received = args.OldItems.OfType<string>().First();
                }
            };
            col.RemoveRange(new[] { "a", "b"});
            Assert.That(received, Is.EqualTo("a"));
        }

        [Test]
        public void Write_FiresRemoveEvent()
        {
            var col = new RangeObservableCollection<string>(new[] { "b", "c" });
            string received = string.Empty;
            col.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Remove)
                {
                    received = args.OldItems.OfType<string>().First();
                }
            };
            col.Remove("c");

            Assert.That(received, Is.EqualTo("c"));
        }

        [Test]
        public void Write_FiresResetEvent()
        {
            var col = new RangeObservableCollection<string>(new[] { "b", "c" });
            bool fired = false;
            col.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Reset)
                {
                    fired = true;
                }
            };
            col.Clear();

            Assert.True(fired);
        }

        [Test]
        public void Write_InsertElement_ElementInserted()
        {
            var col = new RangeObservableCollection<string>(new[] { "a", "b", "c" });
            col.Insert(1, "x");
            CollectionAssert.AreEqual(col, new[] { "a", "x", "b", "c" });
        }

        [Test]
        public void Write_InsertElement_FiresAddEvent()
        {
            var col = new RangeObservableCollection<string>(new[] { "a", "b", "c" });
            NotifyCollectionChangedEventArgs receivedArgs = null;
            col.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                {
                    receivedArgs = args;
                }
            };
            col.Insert(1, "x");
            Assert.That(receivedArgs.NewStartingIndex, Is.EqualTo(1), "Index is wrong");
            CollectionAssert.AreEquivalent(receivedArgs.NewItems, new[] { "x" }, "New items collection wrong");
        }

        [Test]
        public void Write_RemoveElementAtIndex_ElementRemoved()
        {
            var col = new RangeObservableCollection<string>(new[] { "a", "b", "c" });
            col.RemoveAt(1);
            CollectionAssert.AreEqual(new[] { "a", "c" }, col);
        }

        [Test]
        public void Write_RemoveElement_ElementRemoved()
        {
            var col = new RangeObservableCollection<string>(new[] { "b", "c", "d" });
            col.Remove("b");
            CollectionAssert.AreEquivalent(col, new[] { "c", "d" });
        }

        [Test]
        public void Write_RemoveNotExisting_DoesntFail()
        {
            var col = new RangeObservableCollection<string>(new[] { "b", "c", "d" });

            col.RemoveRange(new[] { "b", "X" });
            CollectionAssert.AreEquivalent(new[] { "c", "d" }, col);
        }

        [Test]
        public void Write_RemoveRange_ElementsRemoved()
        {
            var col = new RangeObservableCollection<string>(new[] { "b", "c", "d" });

            col.RemoveRange(new[] { "b", "c" });
            CollectionAssert.AreEquivalent(new[] { "d" }, col);
        }

        [Test]
        public void Write_ReplaceElement_ElementReplaced()
        {
            var col = new RangeObservableCollection<string>(new[] { "a", "b", "c" });
            col[2] = "z";
            CollectionAssert.AreEqual(new[] { "a", "b", "z" }, col);
        }

        [Test]
        public void Write_ReplaceElement_FiresReplaceEvent()
        {
            var col = new RangeObservableCollection<string>(new[] { "a", "b", "c" });
            NotifyCollectionChangedEventArgs receivedArgs = null;
            col.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Replace)
                {
                    receivedArgs = args;
                }
            };
            col[2] = "z";
            Assert.That(receivedArgs.NewStartingIndex, Is.EqualTo(2), "Index is wrong");
            CollectionAssert.AreEquivalent(receivedArgs.NewItems, new[] { "z" }, "New items collection wrong");
            CollectionAssert.AreEquivalent(receivedArgs.OldItems, new[] { "c" }, "Old items collection wrong");
        }

        [Test]
        public void RemoveRange_AcquireRangeToRemoveUsingLinq_RangeRemovedWithoutExceptions()
        {

            var col = new RangeObservableCollection<string>(new[] { "a", "b", "c" });

            var select = col.Where(c => c.Equals("c"));

            col.RemoveRange(select);

            CollectionAssert.AreEquivalent(col, new[] { "a", "b" });
        }

        [Test]
        public void RemoveRange_SequentialRemove_StartFromFirstElement_FiresRemoveEvent()
        {
            var col = new RangeObservableCollection<string>(new[] {"a", "b", "c", "d", "e", "f", "g"});

            var received = new List<string>();
            int oldIndex = -1;
            col.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Remove)
                {
                    received.AddRange(args.OldItems.OfType<string>());
                    oldIndex = args.OldStartingIndex;
                }
            };
            col.RemoveRange(new[] { "a", "b", "c", "d", "e" });
            Assert.That(oldIndex, Is.EqualTo(0));
            CollectionAssert.AreEquivalent(received, new[] { "a", "b", "c", "d", "e" });
            CollectionAssert.AreEquivalent(col, new[] {"f", "g"});
        }

        [Test]
        public void RemoveRange_SequentialRemove_StartFromSecondElement_FiresRemoveEvent()
        {
            var col = new RangeObservableCollection<string>(new[] {"a", "b", "c", "d", "e", "f", "g"});

            var received = new List<string>();
            int oldIndex = -1;
            col.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Remove)
                {
                    received.AddRange(args.OldItems.OfType<string>());
                    oldIndex = args.OldStartingIndex;
                }
            };
            col.RemoveRange(new[] {"b", "c", "d", "e"});
            Assert.That(oldIndex, Is.EqualTo(1));
            CollectionAssert.AreEquivalent(received, new[] {"b", "c", "d", "e"});
            CollectionAssert.AreEquivalent(col, new[] {"a", "f", "g"});
        }

        [Test]
        public void RemoveRange_AllRemove_FiresResetEvent()
        {
            var col = new RangeObservableCollection<string>(new[] {"a", "b", "c", "d", "e", "f"});

            int received = 0;
            col.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Reset)
                {
                    ++received;
                }
            };

            col.RemoveRange(new[] {"a", "b", "c", "d", "e", "f"});
            
            Assert.That(received, Is.EqualTo(1));
            CollectionAssert.IsEmpty(col);
        }

        [Test]
        public void RemoveRange_RemoveOneElement_FiresRemoveEvent()
        {
            var col = new RangeObservableCollection<string>(new[] {"a", "b", "c", "d", "e", "f", "g"});

            var received = new List<string>();
            int oldIndex = -1;
            col.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Remove)
                {
                    received.AddRange(args.OldItems.OfType<string>());
                    oldIndex = args.OldStartingIndex;
                }
            };
            col.RemoveRange(new[] {"d"});
            Assert.That(oldIndex, Is.EqualTo(3));
            CollectionAssert.AreEquivalent(received, new[] {"d"});
            CollectionAssert.AreEquivalent(col, new[] {"a", "b", "c", "e", "f", "g"});
        }

        [Test]
        public void RemoveRange_NotSequentialRemove1_FiresRemoveEvent()
        {
            var col = new RangeObservableCollection<string>(new[] {"a", "b", "c", "d", "e", "f", "g"});

            var received = new List<NotifyCollectionChangedEventArgs>();
            col.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Remove)
                {
                    received.Add(args);
                }
            };
            col.RemoveRange(new[] {"b", "c", "a", "d"});

            Assert.That(received.Count, Is.EqualTo(2));

            CollectionAssert.AreEquivalent(received[0].OldItems, new[] {"b", "c"});
            Assert.That(received[0].OldStartingIndex, Is.EqualTo(1));
            CollectionAssert.AreEquivalent(received[1].OldItems, new[] {"a", "d"});
            Assert.That(received[1].OldStartingIndex, Is.EqualTo(0));
            CollectionAssert.AreEquivalent(col, new[] {"e", "f", "g"});
        }

        [Test]
        public void RemoveRange_NotSequentialRemove2_FiresRemoveEvent()
        {
            var col = new RangeObservableCollection<string>(new[] {"a", "b", "c", "d", "e", "f", "g"});

            var received = new List<NotifyCollectionChangedEventArgs>();
            col.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Remove)
                {
                    received.Add(args);
                }
            };
            col.RemoveRange(new[] {"b", "c", "f", "g"});

            Assert.That(received.Count, Is.EqualTo(2));

            CollectionAssert.AreEquivalent(received[0].OldItems, new[] {"b", "c"});
            Assert.That(received[0].OldStartingIndex, Is.EqualTo(1));
            CollectionAssert.AreEquivalent(received[1].OldItems, new[] {"f", "g"});
            Assert.That(received[1].OldStartingIndex, Is.EqualTo(3));
            CollectionAssert.AreEquivalent(col, new[] {"a", "d", "e"});
        }

        [Test]
        public void RemoveRange_NotSequentialRemove3_FiresRemoveEvent()
        {
            var col = new RangeObservableCollection<string>(new[] {"a", "b", "c", "d", "e", "f", "g"});

            var received = new List<NotifyCollectionChangedEventArgs>();
            col.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Remove)
                {
                    received.Add(args);
                }
            };
            col.RemoveRange(new[] {"b", "c", "a", "g"});

            Assert.That(received.Count, Is.EqualTo(3));

            CollectionAssert.AreEquivalent(received[0].OldItems, new[] {"b", "c"});
            Assert.That(received[0].OldStartingIndex, Is.EqualTo(1));
            CollectionAssert.AreEquivalent(received[1].OldItems, new[] {"a"});
            Assert.That(received[1].OldStartingIndex, Is.EqualTo(0));
            CollectionAssert.AreEquivalent(received[2].OldItems, new[] {"g"});
            Assert.That(received[2].OldStartingIndex, Is.EqualTo(3));
            CollectionAssert.AreEquivalent(col, new[] {"d", "e", "f"});
        }
    }
}
