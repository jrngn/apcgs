using APCGS.Utils.Refactor;
using APCGS.Utils.Registry;
using System;

namespace APCGS.GuiGee
{
  public delegate void EventCallback(GUIEventState data);
  [Questionable(Reason = "spreading & bubbling is currently left to the implementation of the governing extension - keep as is or redesign?")]
  public class GUIEventState
  {
    public enum EInvokationTarget
    {
      Target,
      Propagate,
      Bubble
    }
    [Flags]
    public enum EFlags : byte
    {
      None = 0,
      Spreading = 1,
      Bubbling = 2,
      All = Spreading | Bubbling
    }
    public GUIEvent Event { get; set; }
    public GUIManager Manager { get; set; }
    public EInvokationTarget InvokationTarget { get; set; }
    public EFlags Flags = EFlags.All;
    public void StopPropagation() { InvokationTarget = EInvokationTarget.Bubble; }
    public void EnableSpreading() { Flags |= EFlags.Spreading; }
    public void EnableBubbling() { Flags |= EFlags.Bubbling; }
    public void DisableSpreading() { Flags &= ~EFlags.Spreading; }
    public void DisableBubbling() { Flags &= ~EFlags.Bubbling; }
    public bool IsSpreading() => (Flags & EFlags.Spreading) != EFlags.None;
    public bool IsBubbling() => (Flags & EFlags.Bubbling) != EFlags.None;
  }
  [NeedsDocumentation]
  [Cleanup(Reason = "move the comment block below somewhere else")]
  public class GUIEvent : IRegistryEntry
  {
    internal GUIEvent(string staticName, int id = -1) {
      StaticName = staticName;
      Id = id;
    }
    public string StaticName { get => Key; private set => Key = value; }
    public string Key { get; private set; }
    public int Id { get; set; }
  }

  /* Event list:
   *  Input events:
   *      event data:
   *        DeviceId -> for handling multiples, each device should be mapped to a flat list (ie. keyboard:0, mouse:1, gamepad:2, gamepad:3 etc.)
   *        (InputDevice)
   *        InvokationCount -> for continuous events, how many times this event has been triggered
   *    Keyboard:
   *        event data:
   *          Keyboard -> InputDevice cast (NOTE: this will NOT freeze event data when using delays)
   *          Modifiers -> Keyboard projection, contains info on keylocks(caps,scrl,num) and modifiers (shift,ctrl,alt,fn)
   *          Key -> key in question
   *      OnKeyPress -> key is pressed over time, triggered every (tPeriod) after (tDelay) has passed since OnKeyPressed (that is, OnKeyPressed and OnKeyPress are NOT triggered concurrently, unless tDelay = 0)
   *      OnKeyPressed -> key has been pressed
   *      OnKeyReleased -> key has been released
   *    Mouse:
   *        event data:
   *          Mouse -> InputDevice cast
   *          MouseCoords -> Mouse projection
   *          Button -> button in question
   *      OnMouseButtonPress -> same as for keyboard
   *      OnMouseButtonPressed -> same as for keyboard
   *      OnMouseButtonReleased -> same as for keyboard
   *      OnMouseMove -> xy coords changed
   *      OnMouseHover -> similar to keypress, but checks if mouse coords are over MouseTarget
   *      OnMouseEnter -> mouse coords change, use MouseTarget
   *      OnMouseLeave -> mouse coords change, use OldMouseTarget
   *      OnMouseScroll -> mouse scroll wheel
   *    Gamepad:
   *        event data:
   *          Gamepad -> InputDevice cast
   *          Axis -> axis in question, with its value? except we can trigger multiple axis at the same time?
   *          Button -> button in question
   *      OnGamepadButtonPress
   *      OnGamepadButtonPressed
   *      OnGamepadButtonReleased
   *      OnGamepadAxisActive -> kept outside deadzone
   *      OnGamepadAxisActivate -> just outside deadzone
   *      OnGamepadAxisDeactivate -> in deadzone
   *    Pen, Gesture, Gyroscope etc
   *  Target events:
   *    OnFocus
   *    OnChildFocus
   *    OnBlur
   *    OnChildBlur
   *    OnResize
   *    OnResizeStart
   *    OnResizeApply
   *    OnDrag
   *    OnDragStart
   *    OnDragDrop
   *    OnCut
   *    OnCopy
   *    OnPaste
   *    OnBeforeInput
   *    OnInput
   *    OnChange
   *
   *  how events are triggered:
   *    an external source (ie. InputDevice(like Keyboard) or GUIExtension(like DragNDrop)) monitors its internals, and triggers on change
   *    they also define how events propagate, how things can be targeted, etc
   */
}
