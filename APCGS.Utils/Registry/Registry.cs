using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APCGS.Utils.Registry
{
  public interface IRegistry<TI>
  {
    bool Register(TI instance, bool @override);
    TI this[string key] { get; }
    TI this[int id] { get; }
  }
  public interface IRegistryEntry
  {
    string Key { get; }
    int Id { get; set; }
  }
  
  /// <summary>
  /// Simple class handling data association with unique string key and autoincrementing numeric identifier.
  /// </summary>
  /// <typeparam name="TI">Entry type to keep in the registry. Key of each added entry should be a costant value; the id will be issued by the registry itself.</typeparam>
  public class Registry<TI> : IRegistry<TI>, IEnumerable<TI>, IEnumerable
      where TI : IRegistryEntry
  {
    /// <summary>
    /// Autoincrementing id counter.
    /// </summary>
    public int NextRegistryId { get; protected set; } = 0;
    protected Dictionary<int, TI> RegisteredById;
    protected Dictionary<string, TI> RegisteredByKey;
    /// <summary>
    /// The value returned on get and index calls, if the entry requested didn't exists.
    /// </summary>
    public TI Default { get; set; } = default;
    /// <summary>
    /// Count of currently registered entries. Not the same as <see cref="NextRegistryId"/>, since id fragmentation can occur.
    /// </summary>
    public int Count { get => RegisteredByKey.Count; }

    public Registry(IEqualityComparer<string> comparer = null)
    {
      RegisteredById = new Dictionary<int, TI>();
      RegisteredByKey = comparer == null ? new Dictionary<string, TI>() : new Dictionary<string, TI>(comparer);
    }
    /// <summary>
    /// Removes entry by their id. Will also rewind the id counter should the removed entry be the last entry currently registered.
    /// </summary>
    /// <param name="byId">Id of the entry to remove</param>
    /// <returns><see langword="false"/> if no entry with this id exists, <see langword="true"/> otherwise</returns>
    public bool Remove(int byId)
    {
      if (!RegisteredById.ContainsKey(byId)) return false;
      var existing = RegisteredById[byId];
      if (RegisteredByKey.ContainsKey(existing.Key)) RegisteredByKey.Remove(existing.Key);
      RegisteredById.Remove(existing.Id);
      if (existing.Id == NextRegistryId - 1) NextRegistryId = RegisteredById.Select(kv => kv.Key).DefaultIfEmpty(0).Max();
      return true;
    }
    /// <summary>
    /// Removes entry by their key. Will also rewind the id counter should the removed entry be the last entry currently registered.
    /// </summary>
    /// <param name="byKey">Key of the entry to remove</param>
    /// <returns><see langword="false"/> if no entry with this key exists, <see langword="true"/> otherwise</returns>
    public bool Remove(string byKey)
    {
      if (!RegisteredByKey.ContainsKey(byKey)) return false;
      var existing = RegisteredByKey[byKey];
      RegisteredByKey.Remove(existing.Key);
      if (RegisteredById.ContainsKey(existing.Id))
      {
        RegisteredById.Remove(existing.Id);
        if (existing.Id == NextRegistryId - 1) NextRegistryId = RegisteredById.Select(kv => kv.Key).DefaultIfEmpty(0).Max();
      }
      return true;
    }
    /// <summary>
    /// Registers a new entry based on its key and optionally its id.
    /// </summary>
    /// <param name="instance">Entry to register. Will try to register at given instance id, unless the id is &lt;0, in which case the next id in the counter will be assigned.</param>
    /// <param name="override">If <see langword="true"/>, will remove entries with the same key and/or id as the new instance.</param>
    /// <returns></returns>
    public virtual bool Register(TI instance, bool @override = false)
    {
      bool hasKey = RegisteredByKey.ContainsKey(instance.Key);
      bool hasId = RegisteredById.ContainsKey(instance.Id);
      if (@override)
      {
        if (hasId)
        {
          Remove(instance.Id);
        }
        if (hasKey)
        {
          Remove(instance.Key);
        }
      }
      else if (hasKey || hasId) return false;
      if (instance.Id < 0) instance.Id = NextRegistryId;
      RegisteredByKey.Add(instance.Key, instance);
      RegisteredById.Add(instance.Id, instance);
      NextRegistryId = Math.Max(NextRegistryId, instance.Id + 1);
      return true;
    }
    public IEnumerator<TI> GetEnumerator()
    {
      foreach (var kv in RegisteredById)
      {
        yield return kv.Value;
      }
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public TI this[string key] { get { return RegisteredByKey.ContainsKey(key) ? RegisteredByKey[key] : Default; } }
    public TI this[int id] { get { return RegisteredById.ContainsKey(id) ? RegisteredById[id] : Default; } }
    /// <summary>
    /// Checks the entries in id order agianst the given type. Returns the first found, or default value.
    /// </summary>
    /// <param name="default">Default value to use if an entry with given type did not exist</param>
    /// <typeparam name="TIx">Subtype of the entry to find.</typeparam>
    /// <returns>First entry with given type.</returns>
    public TIx Get<TIx>(TIx @default = default) where TIx : TI
    {
      foreach (var kv in RegisteredById)
        if (kv.Value is TIx x) return x;
      return @default;
    }
    /// <summary>
    /// Casts the entry with the id specified to the given subtype.
    /// </summary>
    /// <param name="id">Id of the entry to cast.</param>
    /// <returns>The entry casted to the subtype if successful, default value otherwise</returns>
    /// <inheritdoc cref="Get{TIx}(TIx)"/>
    public TIx Get<TIx>(int id, TIx @default = default) where TIx : TI => this[id] is TIx x ? x : @default;
    /// <summary>
    /// Casts the entry with the key specified to the given subtype.
    /// </summary>
    /// <param name="key">Key of the entry to cast.</param>
    /// <returns>The entry casted to the subtype if successful, default value otherwise</returns>
    /// <inheritdoc cref="Get{TIx}(TIx)"/>
    public TIx Get<TIx>(string key, TIx @default = default) where TIx : TI => this[key] is TIx x ? x : @default;
  }
}
