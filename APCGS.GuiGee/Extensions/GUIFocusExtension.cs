using APCGS.GuiGee;
using APCGS.GuiGee.Nodes;
using APCGS.Utils.Refactor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APCGS.GuiGee.Extensions
{
  public interface IFocusableNode : INode {
    GUIPointerExtension.PointerDelegate Focus { get;set;}
    GUIPointerExtension.PointerDelegate Blur { get;set;}
    // press
    GUIPointerExtension.PointerDelegate Click { get; set; }
    // release
    GUIPointerExtension.PointerDelegate Release { get; set; }
    // press & release shortly after click
    GUIPointerExtension.PointerDelegate DoubleClick { get; set; }
    bool Focusable { get; set; }
    bool IsFocused { get; set; }
  }
  [NeedsDocumentation]
  [Cleanup]
  [WorkInProgress]
  public class GUIFocusExtension : GUIExtension
  {
    public override Dictionary<string, GUIExtension> ReqDependencies { get; } = new Dictionary<string, GUIExtension> { { "pointer", null }, { "hover", null } };
    public GUIPointerExtension pointerExtension;
    public GUIHoverExtension hoverExtension;
    public GUIEvent EventFocus { get; set; }
    public GUIEvent EventBlur { get; set; }
    public GUITarget TargetFocus { get; set; }
    public static string StaticKey = "focus";
    public override string Key => StaticKey;

    public override void Register(GUIManager manager, int phaseid)
    {
      base.Register(manager, phaseid);
      if(phaseid == 1)
      {
        pointerExtension = manager.GetExtension(GUIPointerExtension.StaticKey) as GUIPointerExtension;
        hoverExtension = manager.GetExtension(GUIHoverExtension.StaticKey) as GUIHoverExtension;

        EventFocus = manager.RegisterEvent("focus");
        EventBlur = manager.RegisterEvent("blur");

        TargetFocus = manager.RegisterTarget("focus");

        manager.RegisterEventCallback(pointerExtension.EventPointerClick, OnPointerClick);
        manager.RegisterEventCallback(pointerExtension.EventPointerDoubleClick, OnPointerEvent);
        manager.RegisterEventCallback(pointerExtension.EventPointerRelease, OnPointerEvent);

        manager.RegisterTargetCallback(TargetFocus, FocusChange);
      }
    }
    private void FocusChange(Node oldNode, Node newNode)
    {
      var data = new GUIPointerEventState
      {
        Manager = Manager,
        pointer = pointerExtension,
        device = pointerExtension.LastDevice
      };
      if (oldNode is IFocusableNode iOldNode)
      {
        data.Event = EventBlur;
        data.target = newNode; // aux target
        Manager.Trigger(EventBlur, data);
        iOldNode.Blur?.Invoke(data);
      }
      if (newNode is IFocusableNode iNewNode)
      {
        data.Event = EventFocus;
        data.target = oldNode; // aux target
        Manager.Trigger(EventFocus, data);
        iNewNode.Focus?.Invoke(data);
      }
      //TODO: differentiate between event & aux target
      //  for bubbling support we need to be able to determine whether this event is targetted or bubbling (simply via ev.target == this)
      //TODO: bubble up focus & blur events (necessary for ie. panel reordering)
    }

    public void OnPointerClick(GUIEventState _data)
    {
      var data = _data as GUIPointerEventState;
      var hoverTarget = hoverExtension.TargetHover.Target;// as IFocusableNode;
      var focusTarget = hoverTarget as IFocusableNode;
      // bubble up to nearest focusable
      while (focusTarget?.Focusable != true && hoverTarget != null)
      {
        hoverTarget = hoverTarget.Parent;
        focusTarget = hoverTarget as IFocusableNode;
      }
      if (focusTarget?.Focusable == true)
      {
        if(TargetFocus.Target != hoverTarget)
          Manager.SetTarget(TargetFocus, focusTarget as Node);
        focusTarget.Click(data);
      }
    }
    public void OnPointerEvent(GUIEventState _data)
    {
      var data = _data as GUIPointerEventState;
      if (data == null) return;
      data.InvokationTarget = GUIEventState.EInvokationTarget.Target;
      // possible TODO: use nearest focusable parent|self instead
      data.target = hoverExtension.TargetHover.Target;
      if (TargetFocus.Target != null /*&& TargetFocus.Target == hoverExtension.TargetHover.Target*/)
      {
        if (TargetFocus.Target is EventNode eNode)
          eNode.Trigger(data);
        if (TargetFocus.Target is IFocusableNode iNode)
        {
          if (data.Event == pointerExtension.EventPointerDoubleClick)
            iNode.DoubleClick?.Invoke(data);
          if (data.Event == pointerExtension.EventPointerRelease)
            iNode.Release?.Invoke(data);
        }
      }
    }
  }
}
