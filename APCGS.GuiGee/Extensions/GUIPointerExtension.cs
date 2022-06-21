using APCGS.GuiGee;
using APCGS.GuiGee.Nodes;
using APCGS.Utils.Refactor;
using System.Collections.Generic;
namespace APCGS.GuiGee.Extensions
{
  public class GUIPointerEventState : GUIEventState
  {
    public GUIPointerExtension pointer;
    public int buttonId;
    public int wheelDelta; // for mouse wheel
    public IPointerDevice device;
    public Node target; // current for hover, old for hoverover, new for hoverout 
  }
  public interface IPointerDevice
  {
    int PointerX { get; set; }
    int PointerY { get; set; }
    bool IsButtonPressed(int buttonId);
  }
  // pointer extension - ability to hover, click & drag, device agnostic

  [NeedsDocumentation]
  public class GUIPointerExtension : GUIExtension
  {
    public delegate void PointerDelegate(GUIPointerEventState eventState);
    public GUIEvent EventPointer { get; set; }
    public GUIEvent EventPointerMove { get; set; }
    public GUIEvent EventPointerHold { get; set; }
    public GUIEvent EventPointerDrag { get; set; }
    public GUIEvent EventPointerClick { get; set; }
    public GUIEvent EventPointerRelease { get; set; }
    public GUIEvent EventPointerDoubleClick { get; set; }
    public GUIEvent EventPointerWheel { get; set; }
    public static string StaticKey => "pointer";
    public override string Key => StaticKey;
    public List<IPointerDevice> ActiveDevices;
    public IPointerDevice LastDevice;
    public override void Register(GUIManager manager, int phaseid)
    {
      base.Register(manager, phaseid);
      if (phaseid == 1)
      {
        EventPointer = manager.RegisterEvent("pointer");
        EventPointerMove = manager.RegisterEvent("pointerMove");
        EventPointerHold = manager.RegisterEvent("pointerHold");
        EventPointerDrag = manager.RegisterEvent("pointerDrag");
        EventPointerClick = manager.RegisterEvent("pointerClick");
        EventPointerRelease = manager.RegisterEvent("pointerRelease");
        EventPointerDoubleClick = manager.RegisterEvent("pointerDoubleClick");
        EventPointerWheel = manager.RegisterEvent("pointerWheel");

        manager.RegisterEventCallback(EventPointer, OnPointerEvent);
        manager.RegisterEventCallback(EventPointerMove, OnPointerEvent);
        manager.RegisterEventCallback(EventPointerHold, OnPointerEvent);
        manager.RegisterEventCallback(EventPointerDrag, OnPointerEvent);
        manager.RegisterEventCallback(EventPointerClick, OnPointerEvent);
        manager.RegisterEventCallback(EventPointerRelease, OnPointerEvent);
        manager.RegisterEventCallback(EventPointerDoubleClick, OnPointerEvent);
        manager.RegisterEventCallback(EventPointerWheel, OnPointerEvent);
      }
    }
    private void OnPointerEvent(GUIEventState _state)
    {
      var state = _state as GUIPointerEventState;
      if (state.device != null) LastDevice = state.device;
    }
  }
}  
