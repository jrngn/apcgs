using System;
using System.Collections;
using System.Collections.Generic;

namespace APCGS.Utils
{
  public class LinkedListStack<T> : ICollection<T>, IEnumerable<T>, IEnumerable, ICollection, IReadOnlyCollection<T>
  {
    public LinkedList<LinkedList<T>> Data;
    public int Count
    {
      get
      {
        var ret = 0;
        foreach (var bucket in Data) ret += bucket.Count;
        return ret;
      }
    }
    public LinkedListStack()
    {
      Data = new LinkedList<LinkedList<T>>();
      Push();
    }
    public bool IsEmpty => Data.Last == Data.First && Current.Count <= 0;
    public LinkedList<T> Current => Data.Last.Value;

    object ICollection.SyncRoot => this;

    bool ICollection.IsSynchronized => false;

    bool ICollection<T>.IsReadOnly => false;

    void ICollection<T>.Add(T item) => Data.Last.Value.AddLast(item);

    public void Clear()
    {
      foreach (var bucket in Data) bucket.Clear();
      Data.Clear();
      Data.AddLast(new LinkedList<T>());
    }

    public bool Contains(T item)
    {
      foreach (var bucket in Data) if (bucket.Contains(item)) return true;
      return false;
    }

    public void CopyTo(Array array, int index)
    {
      throw new NotImplementedException();
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
      throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<T> GetEnumerator()
    {
      foreach (var bucket in Data)
        foreach (var item in bucket)
          yield return item;
    }

    public bool Remove(T item)
    {
      throw new NotSupportedException();
    }


    public void Push() { Data.AddLast(new LinkedList<T>()); }
    public void Push(IEnumerable<T> data) { Data.AddLast(new LinkedList<T>(data)); }
    public LinkedList<T> Pop() { var ret = Data.Last.Value; Data.RemoveLast(); if (Data.Count == 0) Data.AddLast(new LinkedList<T>()); return ret; }

    public LinkedListNode<T> AddFirst(T value) => Current.AddFirst(value);
    public LinkedListNode<T> AddLast(T value) => Current.AddLast(value);

    public void RemoveFirst() => Current.RemoveFirst();
    public void RemoveLast() => Current.RemoveLast();

  }
  public class ListStack<T> : IList<T>
  {
    public LinkedList<List<T>> Data;
    public ListStack()
    {
      Data = new LinkedList<List<T>>();
      Data.AddLast(new List<T>());
    }
    public List<T> Current => Data.Last.Value;
    public bool IsEmpty => Data.Last == Data.First && Current.Count <= 0;
    public void Push() { Data.AddLast(new List<T>()); }
    public List<T> Pop() { var ret = Data.Last.Value; Data.RemoveLast(); if (Data.Count == 0) Data.AddLast(new List<T>()); return ret; }
    public T this[int index]
    {
      get
      {
        if (index < 0) throw new ArgumentOutOfRangeException();
        foreach (var bucket in Data)
        {
          if (index < bucket.Count) return bucket[index];
          index -= bucket.Count;
        }
        throw new ArgumentOutOfRangeException();
      }
      set
      {
        if (index < 0) throw new ArgumentOutOfRangeException();
        foreach (var bucket in Data)
        {
          if (index < bucket.Count)
          {
            bucket[index] = value;
            return;
          }
          index -= bucket.Count;
        }
        throw new ArgumentOutOfRangeException();
      }
    }

    public int Count
    {
      get
      {
        var ret = 0;
        foreach (var bucket in Data) ret += bucket.Count;
        return ret;
      }
    }

    public bool IsReadOnly => false;

    public void Add(T item) => Data.Last.Value.Add(item);
    public void AddRange(IEnumerable<T> items)
    {
      foreach (var item in items) Add(item);
    }

    public void Clear()
    {
      foreach (var bucket in Data) bucket.Clear();
      Data.Clear();
      Data.AddLast(new List<T>());
    }

    public bool Contains(T item)
    {
      foreach (var bucket in Data) if (bucket.Contains(item)) return true;
      return false;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
      throw new NotImplementedException();
    }

    public IEnumerator<T> GetEnumerator()
    {
      foreach (var bucket in Data)
        foreach (var item in bucket)
          yield return item;
    }

    public int IndexOf(T item)
    {
      var ret = -1;
      foreach (var bucket in Data)
      {
        ret = bucket.IndexOf(item);
        if (ret >= 0) return ret;
      }
      return ret;
    }

    public void Insert(int index, T item)
    {
      throw new NotSupportedException();
    }

    public bool Remove(T item)
    {
      throw new NotSupportedException();
    }

    public void RemoveAt(int index)
    {
      throw new NotSupportedException();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  }
}
