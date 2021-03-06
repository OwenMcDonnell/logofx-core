﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace LogoFX.Core
{
    /// <summary>
    /// Observable collection that allows performing addition and removal
    /// of collection of items as single operation.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="System.Collections.ObjectModel.ObservableCollection{T}" />
    public class RangeObservableCollection<T> : ObservableCollection<T>, IRangeCollection<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RangeObservableCollection{T}"/> class.
        /// </summary>
        public RangeObservableCollection()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RangeObservableCollection{T}"/> class.
        /// </summary>
        /// <param name="collection">The collection from which the elements are copied.</param>
        public RangeObservableCollection(IEnumerable<T> collection)
            : base(collection)
        {
        }

        private bool _suppressNotification;

        /// <summary>
        /// Raises the <see cref="E:System.Collections.ObjectModel.ObservableCollection`1.CollectionChanged"/> event with the provided arguments.
        /// </summary>
        /// <param name="e">Arguments of the event being raised.</param>
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_suppressNotification)
                base.OnCollectionChanged(e);
        }

        /// <summary>
        /// Adds range of items as single operation.
        /// </summary>
        /// <param name="range"></param>
        public void AddRange(IEnumerable<T> range)
        {
            if (range == null)
                return;

            int initialindex = Count;

            var enumerable = range as T[] ?? range.ToArray();
            if (!enumerable.Any())
                return;

            _suppressNotification = true;
           
            foreach (var item in enumerable)
            {
                Add(item);
            }
           
            _suppressNotification = false;
            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<T>(enumerable), initialindex));
        }

        /// <summary>
        /// Removes the range of items as single operation.
        /// </summary>
        /// <param name="range">The items to be removed.</param>
        /// <param name="beforeResetAction">The action tp be invoked before the underlying collection is reset.</param>
        public void RemoveRange(IEnumerable<T> range, Action<IEnumerable<T>> beforeResetAction)
        {
            RemoveRangeImpl(range, beforeResetAction);
        }

        /// <summary>
        /// Removes the range of items as single operation.
        /// </summary>
        /// <param name="range">The items to nbe removed.</param>
        public void RemoveRange(IEnumerable<T> range)
        {
            RemoveRangeImpl(range, null);
        }

        private void RemoveRangeImpl(IEnumerable<T> range, Action<IEnumerable<T>> beforeResetAction)
        {
            if (range == null)
            {
                throw new ArgumentNullException(nameof(range));
            }

            if (Count == 0)
            {
                return;
            }

            var enumerable = range as T[] ?? range.ToArray();

            if (enumerable.Length == 0)
            {
                return;
            }

            if (enumerable.Length == 1)
            {
                Remove(enumerable[0]);
                return;
            }

            if (beforeResetAction != null && enumerable.Length >= Count)
            {
                int count = Count;
                foreach (var item in enumerable)
                {
                    if (Contains(item))
                    {
                        --count;
                    }
                }

                if (count == 0)
                {
                    beforeResetAction(enumerable);
                }
            }

            _suppressNotification = true;

            var clusters = new Dictionary<int, List<T>>();
            var lastIndex = -1;
            List<T> lastCluster = null;
            foreach (T item in enumerable)
            {
                var index = IndexOf(item);
                if (index < 0)
                {
                    continue;
                }

                Items.RemoveAt(index);

                if (lastIndex == index && lastCluster != null)
                {
                    lastCluster.Add(item);
                }
                else
                {
                    clusters[lastIndex = index] = lastCluster = new List<T> { item };
                }
            }

            _suppressNotification = false;
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));

            if (Count == 0)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
            else
            {
                foreach (KeyValuePair<int, List<T>> cluster in clusters)
                {
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, cluster.Value, cluster.Key));
                }
            }
        }        

        /// <summary>
        /// Raises the <see cref="E:System.Collections.ObjectModel.ObservableCollection`1.PropertyChanged"/> event with the provided arguments.
        /// </summary>
        /// <param name="e">Arguments of the event being raised.</param>
        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (_suppressNotification == false)
            {
                base.OnPropertyChanged(e);
            }
        }

        /// <summary>
        /// Helper function to determine if a collection contains any elements.
        /// </summary>
        /// <param name="collection">The collection to evaluate.</param>
        /// <returns></returns>
        private static bool ContainsAny(IEnumerable<T> collection)
        {
            using (IEnumerator<T> enumerator = collection.GetEnumerator())
            {
                return enumerator.MoveNext();
            }
        }
    }
}
