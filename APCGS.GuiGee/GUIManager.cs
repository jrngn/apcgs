using System.Collections.Generic;
using System.Linq;
using APCGS.GuiGee.Measurements;
using APCGS.GuiGee.Nodes;
using APCGS.Utils.Refactor;
using APCGS.Utils.Registry;

namespace APCGS.GuiGee
{
  [NeedsDocumentation]
  [Cleanup]
  [WorkInProgress(Reason = "multiphase registry is in redesign - I really don't like the magically numbered phases (also shouldn't phase be exposed directly on the manager itself?)")]
  [Redesign (Reason = "separating node tree from the manager could be usefull for component resolution - the only issue is that global events should be defineable by both the extensions AND the user code")]
  public class GUIManager
  {
    public GUIManager() { }
    public MeasurementUnitStore MUStore { get; private set; } = new MeasurementUnitStore();

    public Registry<GUIEvent> Events { get; private set; } = new Registry<GUIEvent>();
    public Registry<GUITarget> Targets { get; private set; } = new Registry<GUITarget>();

    // so, why are callbacks separate from their respective governing objects?
    // technically events/targets COULD be checked by reference, while callbacks would be dependent only on specific manager instance...
    // it's again instance/definition issue... I need to set this thing in stone once and for all
    // but then - why do we have a manager holding both static and dynamic data?
    public List<EventCallback> EventCallbacks { get; private set; } = new List<EventCallback>();
    public List<TargetCallback> TargetCallbacks { get; private set; } = new List<TargetCallback>();

    public List<Node> Nodes { get; private set; } = new List<Node>();
    // NOTE: sealing should be done at the end of phase 0 (AKA the registration phase) - in fact, IsSealed => Extensions.CurrentPhase > 0
    //public bool Sealed { get; private set; }
    /*public Seal(){
      // screw sealing & use lists?
      EventCallbacks = new EventCallback[Events.Count];
      TargetCallbacks = new TargetCallback[Targets.Count];
      Sealed = true;
    }*/
    protected DependencyRegistry<GUIExtension> Extensions { get; set; } = new DependencyRegistry<GUIExtension>();
    public void RegisterExtension(GUIExtension extension)
    {
      if (Extensions.CurrentPhase != 0) return;
      Extensions.Register(extension);
      extension.Register(this, Extensions.CurrentPhase);
    }
    public void FinalizeExtensions()
    {
      if (Extensions.CurrentPhase != 0) return;
      Extensions.Advance();
      foreach (var ext in Extensions)
        ext.Register(this, Extensions.CurrentPhase);
    }
    public GUIExtension GetExtension(string key) => Extensions[key];
    public GUIExtension GetExtension(int key) => Extensions[key];
    public void Update()
    {
      if (Extensions.CurrentPhase < 1) return;
      foreach (var ext in Extensions)
      {
        ext.Update(this);
      }
      Recalculate();
    }


    public GUIEvent RegisterEvent(string eventName)
    {
      var ret = Events[eventName];
      if (ret != null) return ret;
      //if (Sealed) return null;
      ret = new GUIEvent(eventName, Events.Count);
      Events.Register(ret);
      EventCallbacks.Add(null);
      return ret;
    }

    public void RegisterEventCallback(string eventName, EventCallback cb) => RegisterEventCallback(Events[eventName], cb);
    public void RegisterEventCallback(int eventId, EventCallback cb) => RegisterEventCallback(Events[eventId], cb);
    public void RegisterEventCallback(GUIEvent @event, EventCallback cb){
      if (@event == null) return;
      if (EventCallbacks[@event.Id] != null) EventCallbacks [@event.Id] += cb;
      else EventCallbacks [@event.Id] = cb;
    }

    public void UnregisterEventCallback(string eventName, EventCallback cb) => UnregisterEventCallback(Events[eventName], cb);
    public void UnregisterEventCallback(int eventId, EventCallback cb) => UnregisterEventCallback(Events[eventId], cb);
    public void UnregisterEventCallback(GUIEvent @event, EventCallback cb){
      if (EventCallbacks[@event.Id] != null) EventCallbacks[@event.Id] -= cb;
    }

    public GUITarget RegisterTarget(string targetName)
    {
      var ret = Targets[targetName];
      if (ret != null) return ret;
      //if (Sealed) return null;
      ret = new GUITarget(targetName, Targets.Count);
      Targets.Register(ret);
      TargetCallbacks.Add(null);
      return ret;
    }

    public void RegisterTargetCallback(string targetName, TargetCallback cb) => RegisterTargetCallback(Targets[targetName], cb);
    public void RegisterTargetCallback(int targetId, TargetCallback cb) => RegisterTargetCallback(Targets[targetId], cb);
    public void RegisterTargetCallback(GUITarget target, TargetCallback cb)
    {
      if (TargetCallbacks[target.Id] != null) TargetCallbacks[target.Id] += cb;
      else TargetCallbacks[target.Id] = cb;
    }

    public void UnregisterTargetCallback(string targetName, TargetCallback cb) => UnregisterTargetCallback(Targets[targetName], cb);
    public void UnregisterTargetCallback(int targetId, TargetCallback cb) => UnregisterTargetCallback(Targets[targetId], cb);
    public void UnregisterTargetCallback(GUITarget target, TargetCallback cb)
    {
      if (TargetCallbacks[target.Id] != null) TargetCallbacks[target.Id] -= cb;
    }


    public void Trigger(string eventName, GUIEventState data) => Trigger(Events[eventName], data);
    public void Trigger(int eventId, GUIEventState data) => Trigger(Events[eventId], data);
    public void Trigger(GUIEvent @event, GUIEventState data) {
      //if(!Sealed) return;
      if (@event == null) return;
      if (EventCallbacks[@event.Id] == null) return;
      data.Event = @event;
      data.Manager = this;
      EventCallbacks[@event.Id](data);
      //foreach (EventCallback cb in EventCallbacks[@event.Id].GetInvocationList()) if(!cb(data)) return;
      //foreach(var node in Nodes)if(!node.Trigger(this,@event, data)) return;
    }

    public void SetTarget(string targetName, Node node) => SetTarget(Targets[targetName], node);
    public void SetTarget(int targetId, Node node) => SetTarget(Targets[targetId], node);
    public void SetTarget(GUITarget target, Node node)
    {
      if (target == null) return;
      var oldTarget = target.Target;
      target.Target = node;
      if (TargetCallbacks[target.Id] == null) return;
      TargetCallbacks[target.Id](oldTarget, node);
      //foreach (TargetCallback cb in TargetCallbacks[target.Id].GetInvocationList()) if (!cb(oldTarget, node)) return;
    }

    public void Recalculate()
    {
      //TODO: get screen size & set it as boundaries for every node
      foreach (var node in Nodes) node.Recalculate(this,MUStore);
    }
  }
}
