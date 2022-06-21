using System;
using System.Linq;
using APCGS.GuiGee;
using APCGS.GuiGee.Measurements;
using APCGS.GuiGee.Nodes;
using APCGS.Utils.Refactor;

namespace APCGS.GuiGee.Containers
{
  public enum EPositioningPass
  {
    First = 1,
    Second = 2
  }
  public class FloaterData
  {
    public MeasuredOffset Position { get; set; } = new MeasuredOffset(MeasurementUnit.Pixels.Id);
    public EPositioningPass Pass { get; set; } = EPositioningPass.First;
  }

  // TODO: Offset(AKA border AKA minimal space around content - since it's floater, you can totally assign something at x = 0, even negatives & outside of boundary) support
  [NeedsDocumentation]
  [NeedsTests]
  [Cleanup]
  [WorkInProgress]
  public class Floater : ContainerNode<FloaterData>
  {
    public void RecalculateChildren(GUIManager manager, MeasurementUnitStore muStore, bool secondPass)
    {
      var pass = !secondPass ? EPositioningPass.First : EPositioningPass.Second;
      // get boundary for children
      var maxWidthBoundary = Math.Min(Width.Max.pxCount ?? Boundaries.Width ?? 0, Boundaries.Width ?? Width.Max.pxCount ?? 0);
      var maxHeightBoundary = Math.Min(Height.Max.pxCount ?? Boundaries.Height ?? 0, Boundaries.Height ?? Height.Max.pxCount ?? 0);
      Area ChildBoundaries = secondPass ? new Area
      {
        Width = Dimensions.Width,
        Height = Dimensions.Height
      } : new Area
      {
        Width = Boundaries.Width,//Math.Min(Width.Max.pxCount ?? Boundaries.Width ?? 0, Boundaries.Width ?? Width.Max.pxCount ?? 0),
        Height = Boundaries.Height,//Math.Min(Height.Max.pxCount ?? Boundaries.Height ?? 0, Boundaries.Height ?? Height.Max.pxCount ?? 0),
      };
      foreach (var child in Children)
      {
        // multipass children - affect size & react to relative sizes
        // except they'd need to get nullified boundary... so currently not really supported? or should we just remove boundary from 1st pass?
        // CURRENT: removed boundary for first pass - even tho you should REALLY avoid 2-pass nodes
        //  SIDEEFFECTS: LR/TB cannot be used to control width/height of 1st pass only nodes (previously was calculated based on boundaries)
        if ((child.Value.Pass & pass) == 0) continue;
        var node = child.Key;
        var position = child.Value.Position;
        this.ComputeOffset(muStore, position); // calculate pixel counts
        node.Boundaries = ChildBoundaries; // assign new boundaries
        node.Dimensions.Reset(); // set everything to null
        node.Dimensions.XPos = position.Left.Value;
        node.Dimensions.YPos = position.Top.Value;
        if (secondPass)
        {
          // right & bottom are calculated from boundary endpoints
          if (position.Left.HasValue && position.Right.HasValue && position.Left > 0 && position.Right > 0)
            node.Dimensions.Width = ChildBoundaries.Width - position.Left - position.Right;
          if (position.Top.HasValue && position.Bottom.HasValue && position.Top > 0 && position.Bottom > 0)
            node.Dimensions.Height = ChildBoundaries.Height - position.Top - position.Bottom;
        }
        //if (node.Dimensions.Width < 0) node.Dimensions.Width = null; // null on negative - moved to Clamp() method
        node.Recalculate(manager, muStore);

        // non-floaters: do smth with child height/width, change it's position (resize further? flexlist shrink/expand)
        if (!secondPass)
        {
          //Dimensions.Width = Math.Min(Math.Max((node.Dimensions.XPos ?? 0) + (node.Dimensions.Width ?? 0), Dimensions.Width ?? 0), maxWidthBoundary);
          //Dimensions.Height = Math.Min(Math.Max((node.Dimensions.YPos ?? 0) + (node.Dimensions.Height ?? 0), Dimensions.Height ?? 0), maxHeightBoundary);

          Dimensions.Width = Math.Min(Math.Max((node.Dimensions.XPos ?? 0) + (node.Dimensions.Width ?? 0) + (position.Right.Value), Dimensions.Width ?? 0), maxWidthBoundary);
          Dimensions.Height = Math.Min(Math.Max((node.Dimensions.YPos ?? 0) + (node.Dimensions.Height ?? 0) + (position.Bottom.Value), Dimensions.Height ?? 0), maxHeightBoundary);
        }
      }
    }
    protected void CenterChildren()
    {
      foreach (var child in Children)
      {
        var node = child.Key;
        var position = child.Value.Position;
        // set x-coord if Right position has ben specified
        // in case of width mismatch, center
        if (position.Right.HasValue)
        {
          if (position.Left.HasValue)
          {
            if (position.Left == 0)
            {
              if (position.Right == 0) node.Dimensions.XPos = (Dimensions.Width - node.Dimensions.Width) / 2;
              else node.Dimensions.XPos = 0;
            }
            else if (position.Right == 0) node.Dimensions.XPos = Dimensions.Width - node.Dimensions.Width;
            else
            {
              var ratio = ((float)Math.Abs(position.Left.Value)) / (Math.Abs(position.Left.Value) + Math.Abs(position.Right.Value));
              node.Dimensions.XPos = (int)((Dimensions.Width - node.Dimensions.Width) * ratio);
            }
          }
          else node.Dimensions.XPos = Dimensions.Width - node.Dimensions.Width - position.Right;
        }

        if (position.Bottom.HasValue)
        {
          if (position.Top.HasValue)
          {
            if (position.Top == 0)
            {
              if (position.Bottom == 0) node.Dimensions.YPos = (Dimensions.Height - node.Dimensions.Height) / 2;
              else node.Dimensions.YPos = 0;
            }
            else if (position.Bottom == 0) node.Dimensions.YPos = Dimensions.Height - node.Dimensions.Height;
            else
            {
              var ratio = ((float)Math.Abs(position.Top.Value)) / (Math.Abs(position.Top.Value) + Math.Abs(position.Bottom.Value));
              node.Dimensions.YPos = (int)((Dimensions.Height - node.Dimensions.Height) * ratio);
            }
          }
          else node.Dimensions.YPos = Dimensions.Height - node.Dimensions.Height - position.Bottom;
        }

      }
    }
    public override void Recalculate(GUIManager manager, MeasurementUnitStore muStore)
    {
      // calculate pixel counts with measurement units
      this.ComputeRange(muStore, Width);
      this.ComputeRange(muStore, Height);
      //if (!Width.Default.HasValue && Dimensions.Width == null) Dimensions.Width = Boundaries.Width;
      //if (!Height.Default.HasValue && Dimensions.Height == null) Dimensions.Height = Boundaries.Height;
      // set startup dimensions
      //Dimensions.XPos = 0;
      //Dimensions.YPos = 0;
      Dimensions.Width = Width.Clamp(Dimensions.Width);
      Dimensions.Height = Height.Clamp(Dimensions.Height);

      // 1st pass - children assigned to this pass work on boundaries, their size affects floater size
      RecalculateChildren(manager, muStore, false);
      Dimensions.Width = Width.Clamp(Dimensions.Width);
      Dimensions.Height = Height.Clamp(Dimensions.Height);
      // 2nd pass - children assigned to this pass work on floater size without affecting it
      RecalculateChildren(manager, muStore, true);
      // center all children that had both of their LR/TB components specified
      CenterChildren();
    }
  }
}
