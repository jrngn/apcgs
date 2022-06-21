//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;
using APCGS.GuiGee.Measurements;
using APCGS.Utils.Refactor;
using System;

namespace APCGS.GuiGee.Nodes
{
  public struct NodeIdentifier
  {
    // better keep publicly available references to important nodes instead of relying on identifying nodes
    public Guid guid;
    public int id;
    public string key;
    public NodeIdentifier(int id, string key, Guid? guid) {
      this.guid = guid??Guid.NewGuid();
      this.id = id;
      this.key = key;
    }
  }
  public interface INode : IPositioning
  {
    int Order { get; set; }

    //bool Trigger(GUIManager manager, string eventName, object data);
    //bool Trigger(GUIManager manager, int eventId, object data);
    //bool Trigger(GUIManager manager, GUIEvent @event, object data);
  }
  [NeedsDocumentation]
  [Cleanup]
  public abstract class Node : INode
  {
    public Node Parent { get; set; }
    public NodeIdentifier? NodeIdentifier { get; set; } // identification is pretty much only needed for template load, optionally during extension phase (ie. plugins, mods etc.)
    public int Order { get; set; } = 0; // higher order - drawn later (on top), handled earlier (events), still calculated in register order
    //public Node Parent { get; set; } // do i need parent node for anything?
    // do we keep manager in each node, or just subset of nodes, or just pass it along if needed?
    //public GUIManager Manager { get; set; }

    //public MeasurementRatios Ratios { get; set; }

    public MeasuredRange Width { get; set; } = new MeasuredRange(MeasurementUnit.Pixels.Id);
    public MeasuredRange Height { get; set; } = new MeasuredRange(MeasurementUnit.Pixels.Id);

    //public MeasuredOffset Postition { get; set; } = new MeasuredOffset(); // only useful in floaters... hmmm
    //public MeasuredOffset Margin { get; set; } = new MeasuredOffset(); // margin have no purpose in floaters... does it?

    public Area GlobalDimensions => Dimensions.Offset(Parent?.GlobalDimensions);
    public Area Dimensions { get; set; } = new Area();
    public Area Boundaries { get; set; } = new Area();

    //public bool Trigger(GUIManager manager,string eventName, object data) => Trigger(manager, manager.Events[eventName], data);
    //public bool Trigger(GUIManager manager,int eventId, object data) => Trigger(manager, manager.Events[eventId], data);
    //public abstract bool Trigger(GUIManager manager, GUIEvent @event, object data);

    //public virtual Area GetContentSize() => new Area();
    public virtual void Recalculate(GUIManager manager, MeasurementUnitStore muStore) {
      // calculate pixel counts with measurement units
      this.ComputeRange(muStore, Width);
      this.ComputeRange(muStore, Height);
      // set startup dimensions
      Dimensions.Width = Width.Clamp(Dimensions.Width);
      Dimensions.Height = Height.Clamp(Dimensions.Height);
      Dimensions.XPos = 0;
      Dimensions.YPos = 0;
    }
    //public Node() { Ratios = new MeasurementRatios(Parent?.Ratios); }
  }

  /* GuiGee implementation is renderless
   * meaning it contains only logic necessary for building, managing and positioning of the GUI
   * the drawing needs to be implemented on consumer side
   * so that GuiGee is not dependent on any rendering library
   * the following interface could be used to mark nodes that can be rendered
   */
  //public interface IRenderable
  //{
  //    void Render(SpriteBatch spriteBatch, Rectangle offset);
  //}
}
