using APCGS.Utils.Refactor;

namespace APCGS.Utils.Registry
{

  public interface IInstanceEntry
  {
    int PrototypeId { get; set; }
  }

  public interface IPrototypeEntry<TI> : IRegistryEntry
      where TI : IInstanceEntry
  {
    TI Create();
  }

  public abstract class PrototypeEntry<TI> : IPrototypeEntry<TI>
      where TI : IInstanceEntry
  {
    public abstract string Key { get; }
    public int Id { get; set; }
    public TI Create()
    {
      var inst = CreateInstance();
      if (inst == null) return default;
      inst.PrototypeId = Id;
      return inst;
    }
    public abstract TI CreateInstance();
  }
  /// <summary>
  /// Subclass of <see cref="Registry{TI}"/>, whose entries are also responsible for creating prototype-based instances. <br/>
  /// This kind of registry can be regarded as a store of instance definitions.
  /// </summary>
  /// <typeparam name="TPI">Type of the instance created by this registry's entries.</typeparam>
  /// <inheritdoc cref="Registry{TI}"/>
  [Redesign(Reason = "could benefit from Create(key|id) method")]
  public class PrototypeRegistry<TPI, TI> : Registry<TI>
      where TI : IPrototypeEntry<TPI>
      where TPI : IInstanceEntry
  {

  }
}
