using System.Collections.Generic;
using APCGS.Utils.Refactor;
using APCGS.Utils.Registry;

namespace APCGS.GuiGee
{
  [NeedsDocumentation]
  [WorkInProgress(Reason = "multiphase registry is in redesign - I really don't like the magically numbered phases")]
  public abstract class GUIExtension : IDependentEntry<GUIExtension>
  {
    public GUIManager Manager { get; set; }
    public virtual Dictionary<string, GUIExtension> ReqDependencies { get; } = new Dictionary<string, GUIExtension>();
    public virtual Dictionary<string, GUIExtension> OptDependencies { get; } = new Dictionary<string, GUIExtension>();
    public abstract string Key { get; }
    public int Id { get; set; }
    public virtual void OnRegister(IMultiphaseRegistry<GUIExtension> registry, int phaseid) { }
    public virtual void Register(GUIManager manager, int phaseid) { if (phaseid == 0) Manager = manager; }
    public virtual void Update(GUIManager manager) { }
  }
}
