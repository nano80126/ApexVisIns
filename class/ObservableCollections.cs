using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;

namespace ObservableCollections
{

    //public delegate void NotifyCollectionChangedEventHandler<T>(in NotifyCollectionChangedEventArgs<T> e)

    public interface IObservableCollection<T> : IReadOnlyCollection<T>, IEnumerable<T>
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        object SyncRoot { get; }
    }


    public sealed partial class ObservableStack<T> : Stack<T>, INotifyCollectionChanged
    {
        private readonly Stack<T> stack;
        public object SyncRoot { get; } = new object();

        #region 建構子
        public ObservableStack()
        {
            stack = new Stack<T>();
        }

        public ObservableStack(int capatity)
        {
            stack = new Stack<T>(capatity);
        }

        public ObservableStack(IEnumerable<T> collection)
        {
            stack = new Stack<T>(collection);
        }
        #endregion

        public int Count
        {
            get
            {
                lock (SyncRoot)
                {
                    return stack.Count;
                }
            }
        }

        [Obsolete("待測試")]
        public void Push(T item)
        {
            lock (SyncRoot)
            {
                int index = stack.Count;
                stack.Push(item);
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
            }
        }

        [Obsolete("待測試")]
        public void PushRange(T[] items)
        {
            lock (SyncRoot)
            {
                int index = stack.Count;
                foreach (T item in items)
                {
                    stack.Push(item);
                }
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items, index));
            }
        }

        public T Pop()
        {
            lock (SyncRoot)
            {
                T t = stack.Pop();
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, 0));
                return t;
            }
        }


        public bool TryPop([MaybeNullWhen(false)] out T result)
        {
            lock (SyncRoot)
            {
                if (stack.Count > 0)
                {
                    result = stack.Pop();
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, 0));
                    return true;
                }
                else
                {
                    result = default;
                    return false;
                }
            }
        }


        [Obsolete("待測試，Error 處理")]
        public T[] PopRange(int count)
        {
            lock (SyncRoot)
            {
                T[] arr = new T[count];
                for (int i = 0; i < count; i++)
                {
                    arr[i] = stack.Pop();
                }
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, 0));
                return arr;
            }
        }

        public void Clear()
        {
            lock (SyncRoot)
            {
                stack.Clear();
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }


        public T Peek()
        {
            lock (SyncRoot)
            {
                return stack.Peek();
            }
        }


        public bool TryPeek([MaybeNullWhen(false)] out T result)
        {
            lock (SyncRoot)
            {
                if (stack.Count > 0)
                {
                    result = stack.Peek();
                    return true;
                }
                else
                {
                    result = default;
                    return false;
                }
            }
        }

        public T[] ToArray()
        {
            lock (SyncRoot)
            {
                return stack.ToArray();
            }
        }

        public void TrimExcess()
        {
            lock (SyncRoot)
            {
                stack.TrimExcess();
            }
        }

        public IEnumerable<T> GetEnumerator()
        {
            lock (SyncRoot)
            {
                foreach (T item in stack)
                {
                    yield return item;
                }
            }
        }

        #region Collection Changed
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void OnCollectionChanged(NotifyCollectionChangedAction action, T item)
        {
            // CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, item, ), T item);

        }

        //IEnumerator<T> IEnumerable<T>.GetEnumerator()
        //{

        //}
        #endregion
    }
}
