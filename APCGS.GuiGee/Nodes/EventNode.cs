using APCGS.Utils.Refactor;
using System.Collections.Generic;

namespace APCGS.GuiGee.Nodes
{
  [NeedsDocumentation]
  [Cleanup]
  [Questionable(Reason = "how useful is it? have it not became obsoleted with the extensions?")]
  public abstract class EventNode: Node
  {
    // changed to dict 'cause the amount of events could get up to triple digits...
    public Dictionary<int, EventCallback> EventCallbacks { get; private set; } = new Dictionary<int, EventCallback>();

    /*private void SyncEventList(GUIManager manager)
    {
      if (EventCallbacks == null) EventCallbacks = new List<EventCallback>();
      while (EventCallbacks.Count < manager.Events.Count) EventCallbacks.Add(null);
    }*/
    public /*override*/ void Trigger(GUIEventState data)
    {
      //if(!Sealed) return;
      if (data?.Event == null) return;
      //if (@event.Id >= EventCallbacks.Count) SyncEventList(manager);
      if (EventCallbacks[data.Event.Id] == null) return; // nothing to do here
      // sanity question: why tf would you ever want to stop single node event processing midprocess???
      //foreach (EventCallback cb in EventCallbacks[@event.Id].GetInvocationList()) if (!cb(data)) return false;
      EventCallbacks[data.Event.Id](data);
      //foreach (var node in Nodes) if (!node.Trigger(@event, data)) return;
    }
  }
}
