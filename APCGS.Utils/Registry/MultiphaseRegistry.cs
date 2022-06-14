using APCGS.Utils.Refactor;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace APCGS.Utils.Registry
{
  public interface IMultiphaseEntry<TI> : IRegistryEntry
    where TI : IMultiphaseEntry<TI>
  {
    void OnRegister(IMultiphaseRegistry<TI> registry, int phaseid);
  }
  
  public interface IMultiphaseRegistry<TI> : IRegistry<TI>
    where TI : IMultiphaseEntry<TI>
  {
    void Advance();
    List<Action> Finalizers { get; }
  }

  /// <summary>
  /// Subclass of <see cref="Registry{TI}"/>, that allows to trigger phase-dependent functionalities of its entries.<br/>
  /// Mainly used in more complex registration schemes, where entries must have access to eachother.<br/>
  /// In such case, first phase is usually only meant for generating ids,<br/>
  /// allowing entries to fetch eachother in later phases by their keys/ids.<br/><br/>
  /// NOTE: this implementation does not seal registration - in common case scenario, only first phase should allow modification of the registered entries.<br/>
  /// Furthermore, entry registration will not trigger its <see cref="IMultiphaseEntry{TI}.OnRegister(IMultiphaseRegistry{TI}, int)"/> method for previous phases.
  /// </summary>
  /// <inheritdoc cref="Registry{TI}"/>
  [Redesign(Reason = "see notes in its docs - registering past phase 0 makes little to no sense - add sealing behaviour")]
  [Redesign(Reason = "phases do work here a little bit like magic numbers - can it be improved? note that phases are designed here to be dynamically adjusted.")]
  [NeedsDocumentation]
  public abstract class MultiphaseRegistry<TI> : Registry<TI>, IMultiphaseRegistry<TI>
    where TI : IMultiphaseEntry<TI>
  {
    public int CurrentPhase { get; protected set; } = 0;
    public int MaxPhase { get; protected set; }
    public List<Action> Finalizers { get; protected set; }
    public MultiphaseRegistry(int maxPhase, IEqualityComparer<string> comparer = null) : base(comparer)
    {
      MaxPhase = maxPhase;
      Finalizers = new List<Action>(MaxPhase + 1);
      for (int i = 0; i < Finalizers.Capacity; i++) Finalizers.Add(null);
    }
    public void Advance()
    {
      if (CurrentPhase >= MaxPhase) return;
      Finalizers[CurrentPhase]();
      CurrentPhase++;
      foreach (var entry in this)
      {
        entry.OnRegister(this, CurrentPhase);
      }
    }
  }
}
