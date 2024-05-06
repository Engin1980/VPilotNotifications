using ESystem.Asserting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace ESystem
{
  public static class Extensions
  {
    public static StringBuilder AppendIf(this StringBuilder sb, Func<bool> condition, string text)
    {
      if (condition())
        sb.Append(text);
      return sb;
    }

    public static StringBuilder AppendIf(this StringBuilder sb, bool condition, string text)
    {
      if (condition)
        sb.Append(text);
      return sb;
    }

    public static bool None<T>(this IEnumerable<T> items, Func<T, bool> predicate)
    {
      return items.Any(predicate) == false;
    }

    [Obsolete("Use Select() instead.")]
    public static Tout Pipe<Tin, Tout>(this Tin obj, Func<Tin, Tout> selector)
    {
      return selector(obj);
    }

    public static Tout Select<Tin, Tout>(this Tin obj, Func<Tin, Tout> selector)
    {
      return selector(obj);
    }

    public static List<Tin> FlattenRecursively<Tin, Tcol>(this IEnumerable<Tin> lst, Func<Tcol, List<Tin>> subListSelector)
    {
      List<Tin> ret = new List<Tin>();

      foreach (var item in lst)
      {
        if (item is Tcol subListSource)
        {
          List<Tin> subList = subListSelector(subListSource);
          List<Tin> flatted = subList.FlattenRecursively(subListSelector);
          ret.AddRange(flatted);
        }
        else
          ret.Add(item);
      }

      return ret;
    }

    private static Random rnd;
    public static T GetRandom<T>(this List<T> lst)
    {
      EAssert.Argument.IsNotNull(lst, nameof(lst));
      EAssert.Argument.IsTrue(lst.Count > 0, nameof(lst));

      if (rnd == null) rnd = new Random();
      int index = rnd.Next(0, lst.Count);
      T ret = lst[index];
      return ret;
    }

    public static T GetRandomOrDefault<T>(this List<T> lst)
    {
      EAssert.Argument.IsNotNull(lst, nameof(lst));
      if (lst.Count == 0) return default;

      T ret = GetRandom(lst);
      return ret;
    }

    public static IEnumerable<T> TapEach<T>(this IEnumerable<T> lst, Action<T> action)
    {
      foreach (var item in lst)
      {
        action(item);
      }
      return lst;
    }

    public static T Tap<T>(this T obj, Action<T> action)
    {
      action(obj);
      return obj;
    }

    public static BindingList<T> ToBindingList<T>(this IEnumerable<T> items)
    {
      BindingList<T> ret;
      if (items is List<T> lst)
        ret = new BindingList<T>(lst);
      else
        ret = new BindingList<T>(items.ToList());
      return ret;
    }

    public static string GetFullMessage(this Exception ex, string delimiter = " <== ")
    {
      List<string> tmp = new List<string>();
      while (ex != null)
      {
        tmp.Add(ex.Message);
        ex = ex.InnerException;
      }
      string ret = string.Join(delimiter, tmp);
      return ret;
    }
  }
}
