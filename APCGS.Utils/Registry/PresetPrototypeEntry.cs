using APCGS.Utils.Refactor;
using System;

namespace APCGS.Utils.Registry
{
  public interface IPresetInstanceEntry : IInstanceEntry, IRegistryEntry
  {
  }
  public interface IPresetPrototypeEntry<TI> : IPrototypeEntry<TI>, IRegistry<TI>
      where TI : IPresetInstanceEntry
  {
    TI Create(string presetName);
    TI Create(int presetId);
  }

  [Obsolete]
  [SubjectForRemoval(Reason = "better to make multiple copies of the same prototype entry, while altering their prototypes & reregistering them with different keys/ids")]
  public abstract class PresetPrototypeEntry<TI> : Registry<TI>, IPresetPrototypeEntry<TI>
    where TI : IPresetInstanceEntry
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

    public TI Create(string presetName)
    {
      throw new NotImplementedException();
    }

    public TI Create(int presetId)
    {
      throw new NotImplementedException();
    }
  }
}
