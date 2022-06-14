using System.Collections.Generic;

namespace APCGS.Utils
{
  public static class LinkedListExtensions
  {
    public static void Push<T>(this LinkedList<T> list, T item) => list.AddLast(item);
    public static void Shift<T>(this LinkedList<T> list, T item) => list.AddFirst(item);

    public static void Push<T>(this LinkedList<T> list, IEnumerable<T> items) { foreach (var item in items) list.AddLast(item); }
    public static void Shift<T>(this LinkedList<T> list, IEnumerable<T> items) { foreach (var item in items) list.AddFirst(item); }

    public static T Pop<T>(this LinkedList<T> list)
    {
      if (list.Count == 0) return default;
      var ret = list.Last.Value;
      list.RemoveLast();
      return ret;
    }
    public static T Unshift<T>(this LinkedList<T> list)
    {
      if (list.Count == 0) return default;
      var ret = list.First.Value;
      list.RemoveFirst();
      return ret;
    }
  }
}
