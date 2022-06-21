using APCGS.GuiGee;
using APCGS.GuiGee.Measurements;
using APCGS.GuiGee.Nodes;
using APCGS.Utils;
using APCGS.Utils.Refactor;
using System.Collections.Generic;
using System.Linq;
namespace APCGS.GuiGee.Extensions
{
  public interface IHoverableNode : INode
  {
    // pointer xy is moving while hovering over
    //void Hover(GUIPointerEventState eventState);
    GUIPointerExtension.PointerDelegate Hover { get; set; }
    // pointer xy just entered node area
    //void HoverOver(GUIPointerEventState eventState);
    GUIPointerExtension.PointerDelegate HoverOver { get; set; }
    // pointer xy just left node area
    //void HoverOut(GUIPointerEventState eventState);
    GUIPointerExtension.PointerDelegate HoverOut { get; set; }
    // mouse wheel
    GUIPointerExtension.PointerDelegate HoverWheel { get; set; }
    bool Hoverable { get; set; }
    bool IsHovering { get; set; }
  }

  [NeedsDocumentation]
  [Cleanup]
  [WorkInProgress]
  public class GUIHoverExtension : GUIExtension
  {
    public override Dictionary<string, GUIExtension> ReqDependencies { get; } = new Dictionary<string, GUIExtension> { { GUIPointerExtension.StaticKey, null } };
    public GUIPointerExtension pointerExtension;

    public GUIEvent EventHover { get; set; }
    public GUIEvent EventHoverOver { get; set; }
    public GUIEvent EventHoverOut { get; set; }
    public GUITarget TargetHover { get; set; }
    public static string StaticKey => "hover";
    public override string Key => StaticKey;
    public override void Register(GUIManager manager, int phaseid)
    {
      base.Register(manager, phaseid);
      if (phaseid == 1)
      {
        EventHover = manager.RegisterEvent("hover");
        EventHoverOver = manager.RegisterEvent("hoverOver");
        EventHoverOut = manager.RegisterEvent("hoverOut");
        TargetHover = manager.RegisterTarget("hover");

        pointerExtension = manager.GetExtension(GUIPointerExtension.StaticKey) as GUIPointerExtension;

        manager.RegisterEventCallback(pointerExtension.EventPointer, OnPointerEvent);
        manager.RegisterEventCallback(pointerExtension.EventPointerHold, OnPointerEvent);
        manager.RegisterEventCallback(pointerExtension.EventPointerWheel, OnPointerEvent);

        manager.RegisterEventCallback(pointerExtension.EventPointerDrag, OnPointerMove);
        manager.RegisterEventCallback(pointerExtension.EventPointerMove, OnPointerMove);

        manager.RegisterTargetCallback(TargetHover, HoverChange);
      }
    }
    public bool IsNodeHoverable(Node node)
    {
      return node is IHoverableNode;
      // TODO: more complex logic for event nodes
      // Q: what about nodes with hovering disabled?
    }
    private void HoverChange(Node oldNode, Node newNode)
    {
      var data = new GUIPointerEventState
      {
        Manager = Manager,
        pointer = pointerExtension,
        device = pointerExtension.LastDevice
      };
      if (oldNode is IHoverableNode iOldNode)
      {
        data.Event = EventHoverOut;
        data.target = newNode;
        Manager.Trigger(EventHoverOut, data);
        iOldNode.HoverOut?.Invoke(data);
      }
      if (newNode is IHoverableNode iNewNode)
      {
        data.Event = EventHoverOver;
        data.target = oldNode;
        Manager.Trigger(EventHoverOver, data);
        iNewNode.HoverOver?.Invoke(data);
      }
    }
    public void OnPointerEvent(GUIEventState _data)
    {
      var data = _data as GUIPointerEventState;
      if (data == null) return;
      data.InvokationTarget = GUIEventState.EInvokationTarget.Target;
      if (TargetHover.Target != null)
      {
        if (TargetHover.Target is EventNode eNode)
          eNode.Trigger(data);
        if (TargetHover.Target is IHoverableNode iNode)
        {
          if(data.Event == pointerExtension.EventPointer || data.Event == pointerExtension.EventPointerHold)
            iNode.Hover?.Invoke(data);
          if (data.Event == pointerExtension.EventPointerWheel)
            iNode.HoverWheel?.Invoke(data);
        }
      }
    }
    public void OnPointerMove(GUIEventState _data)
    {
      var data = _data as GUIPointerEventState;
      if (data == null) return;
      data.InvokationTarget = GUIEventState.EInvokationTarget.Target;
      if (TargetHover.Target != null)
      {
        if (TargetHover.Target is IHoverableNode iNode && iNode.GlobalDimensions.Contains(data.device.PointerX, data.device.PointerY))
          iNode.Hover?.Invoke(data);
        if (TargetHover.Target is EventNode eNode && eNode.GlobalDimensions.Contains(data.device.PointerX, data.device.PointerY))
          eNode.Trigger(data);
      }
      data.InvokationTarget = GUIEventState.EInvokationTarget.Propagate;
      LinkedListStack<Node> pending = new LinkedListStack<Node>();
      LinkedList<bool> foundTarget = new LinkedList<bool>();
      LinkedList<Node> parents = new LinkedList<Node>();
      foundTarget.Push(false);
      Node parent = null;
      bool skipSpread = false;
      pending.Current.Push(data.Manager.Nodes.OrderBy(e => e.Order));
      Node newTarget = null;
      while (!pending.IsEmpty)
      {
        var node = pending.Current.Pop();
        if (node.GlobalDimensions.Contains(data.device.PointerX, data.device.PointerY))
        {
          var skip = false;
          if (node is IHoverableNode hNode)
          {
            if (hNode.Hoverable)
            {
              newTarget = node;
              foundTarget.Last.Value = true;
              skipSpread = true;
            }
            else skip = true;
          }
          // TODO: check if eventnode is hoverable
          // TODO: disable hovering
          // TODO: disable propagation
          if (!skip && node is IContainerNode cNode)
          {
            parents.Push(node);
            foundTarget.Push(false);
            pending.Push(cNode.GetNodes().OrderBy(e => e.Order));
            skipSpread = false;
          }
        }
        if (pending.Current.Count == 0 && skipSpread)
        {
          while (!pending.IsEmpty) pending.Pop();// skip rest (possible TODO: event bubbling)
        }
        while (pending.Current.Count == 0 && !pending.IsEmpty)
        {
          pending.Pop();
          skipSpread = skipSpread || foundTarget.Pop(); // do not spread this event if already found a suitable target on current or higher level
          parent = parents.Pop(); // for event bubbling purposes, I guess...
        }
      }
      if (TargetHover.Target != newTarget)
      {
        Manager.SetTarget(TargetHover, newTarget);
      }
    }
  }
}  
