using APCGS.Utils.Refactor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace APCGS.Utils.Registry
{
  public interface IDependentEntry<TI> : IMultiphaseEntry<TI>
    where TI : IDependentEntry<TI>
  {
    /// <summary>
    /// Required dependencies of this entry, will be registered after all of them, will not get registered if anyone of them is missing
    /// </summary>
    Dictionary<string, TI> ReqDependencies { get; }
    /// <summary>
    /// Optional dependencies of this entry, will be registered after all of them
    /// </summary>
    Dictionary<string, TI> OptDependencies { get; }
  }
  /// <summary>
  /// Subclass of <see cref="Registry{TI}"/>, that handles its entry dependencies.<br/>
  /// Id registration of its entries is delayed until all dependency restrictions are solved.<br/>
  /// This is done primarily to reorder the entries to be registered after their dependencies.<br/>
  /// Entries whose dependency requirements are not satisfied are removed from the registry.
  /// </summary>
  /// <inheritdoc cref="Registry{TI}"/>
  [Redesign(Reason = "certain parts of this class might end up in MultiphaseRegistry<TI>")]
  [Redesign(Reason = "entries that fail dependency checks should be kept in a separate collection - error handling should be left to user prefs")]
  [NeedsDocumentation]
  public class DependencyRegistry<TI> : MultiphaseRegistry<TI>
    where TI : IDependentEntry<TI>
  {
    public enum EPhases : int
    {
      Register = 0,
      SolveDependencies
    }
    public DependencyRegistry(int maxPhase = 1, IEqualityComparer<string> comparer = null) : base(Math.Max(maxPhase, 1), comparer)
    {
      Finalizers[0] = SolveDependencies;
    }
    public override bool Register(TI instance, bool @override = false)
    {
      if (CurrentPhase != (int)EPhases.Register) return false;
      bool hasKey = RegisteredByKey.ContainsKey(instance.Key);
      if (@override)
      {
        if (hasKey)
        {
          Remove(instance.Key);
        }
      }
      else if (hasKey) return false;
      RegisteredByKey.Add(instance.Key, instance);
      return true;
    }
    public void SolveDependencies()
    {
      TI t = default;
      LinkedList<TI> unorderedEntries = new LinkedList<TI>();
      foreach (var kv in RegisteredByKey)
      {
        foreach (var key in kv.Value.ReqDependencies.Keys.ToArray())
          kv.Value.ReqDependencies[key] = RegisteredByKey.TryGetValue(key, out t) ? t : default;
        foreach (var key in kv.Value.OptDependencies.Keys.ToArray())
          kv.Value.OptDependencies[key] = RegisteredByKey.TryGetValue(key, out t) ? t : default;
        kv.Value.Id = -1;
        unorderedEntries.AddLast(kv.Value);
      }
      while (unorderedEntries.Count > 0)
      {
        TI entry = default;
        foreach (var _entry in unorderedEntries)
        {
          if (_entry.ReqDependencies.All(e => e.Value != null && e.Value.Id >= 0) && _entry.OptDependencies.All(e => e.Value == null || e.Value.Id >= 0))
          {
            entry = _entry;
            break;
          }
        }
        if (!Equals(entry, default(TI)))
        {
          unorderedEntries.Remove(entry);
          RegisteredById.Add(NextRegistryId, entry);
          entry.Id = NextRegistryId;
          NextRegistryId++;
        }
        else break; // not found valid entry = all remaining are invalid (missing req dep or having cyclical deps)
      }
      foreach (var entry in unorderedEntries)
      {
        Remove(entry.Key);
      }
      unorderedEntries = null;
    }
  }
}
