using System;
using System.Collections.Generic;
using System.Linq;
using APCGS.GuiGee.Measurements;
using APCGS.GuiGee.Nodes;
using APCGS.Utils.Refactor;

namespace APCGS.GuiGee.Containers
{
  public enum EFlexAxialBehaviour
  {
    Horizontal = 0,
    Vertical = 1,
    CoaxisNormal = 0,
    CoaxisReverse = 2,
    ContraxisNormal = 0,
    ContraxisReverse = 4,
    NoWrap = 0,
    Wrap = 8,
    WrapStrech = 16,
    LTR = Horizontal | CoaxisNormal | ContraxisNormal | Wrap,
    RTL = Horizontal | CoaxisReverse | ContraxisNormal | Wrap,
    List = Vertical | CoaxisNormal | NoWrap
  }
  public enum EFlexLineAlignment
  {
    Start,
    End,
    Center,
    Stretch
  }
  public enum EFlexAxialAlignment
  {
    Start,
    End,
    Center,
    SpaceBetween,
    SpaceAround,
    SpaceEvenly
  }

  public class FlexListData
  {
    public MeasuredOffset Margin { get; set; } = new MeasuredOffset(MeasurementUnit.Pixels.Id);// note: contraxial margin is almost pointless - will only work with no stretch to offset items in the same line
    public Measurement Basis { get; set; } = new Measurement(MeasurementUnit.Pixels.Id); // make all measurable structs? but interfaces... ugh
    public int Grow { get; set; } = 0;
    public int Shrink { get; set; } = 0;
    public EFlexLineAlignment? Alignment { get; set; }
  }
  [NeedsDocumentation]
  [NeedsTests]
  [Cleanup]
  [Cleanup(Reason = "'contraxial' isn't really a word that exists in common use (plus shouldn't it be 'counteraxial'?) - maybe there is better naming for it? could use parallel/perpendicular instead - tho I'd prefer something shorter...")]
  [WorkInProgress(Reason = "check all of them TODOs scattered around")]
  public class FlexList : ContainerNode<FlexListData>
  {
    internal class FlexListLine
    {
      public FlexListLine(int fittingSize)
      {
        LeftoverSize = FittingSize = fittingSize;
      }
      internal class Data
      {
        public Node node;
        public FlexListData data;
        public bool recalc;
        public bool assigned;
      }
      public List<Data> Nodes { get; set; } = new List<Data>();
      public int LeftoverSize;
      public int FittingSize;
      public int MaxContraxialSize = 0;
      public int Grow = 0;
      public int Shrink = 0;
    }
    public EFlexAxialBehaviour AxialBehaviour { get; set; } = EFlexAxialBehaviour.List; // HTML:flex-flow
    public EFlexLineAlignment LineAlignment { get; set; } // HTML:align-items
    public EFlexAxialAlignment CoaxialAlignment { get; set; } // HTML:justify-content
    public EFlexAxialAlignment ContraxialAlignment { get; set; } // HTML:align-content
    public MeasuredOffset Padding { get; set; } = new MeasuredOffset(MeasurementUnit.Pixels.Id);
    public MeasuredPosition Spacing { get; set; } = new MeasuredPosition(MeasurementUnit.Pixels.Id);
    // flag extraction
    // TODO: add setters
    public bool Wrap => (AxialBehaviour & EFlexAxialBehaviour.Wrap) != 0;
    public bool WrapStretch => (AxialBehaviour & EFlexAxialBehaviour.WrapStrech) != 0;
    public bool Horizontal => (AxialBehaviour & EFlexAxialBehaviour.Vertical) == 0;
    public bool CoaxReverse => (AxialBehaviour & EFlexAxialBehaviour.CoaxisReverse) != 0;
    public bool ContraxReverse => (AxialBehaviour & EFlexAxialBehaviour.ContraxisReverse) != 0;

    protected void SetAxialPosition(Node node, bool contraxial, int? value)
    { if (Horizontal != contraxial) node.Dimensions.XPos = value; else node.Dimensions.YPos = value; }
    protected void SetAxialSize(Node node, bool contraxial, int? value)
    { if (Horizontal != contraxial) node.Dimensions.Width = value; else node.Dimensions.Height = value; }
    protected int? GetAxialPosition(Node node, bool contraxial) => (Horizontal != contraxial) ? node.Dimensions.XPos : node.Dimensions.YPos;
    protected int? GetAxialSize(Node node, bool contraxial) => (Horizontal != contraxial) ? node.Dimensions.Width : node.Dimensions.Height;

    public override void Recalculate(GUIManager manager, MeasurementUnitStore muStore)
    {
      // calculate pixel counts with measurement units
      this.ComputeRange(muStore, Width);
      this.ComputeRange(muStore, Height);
      this.ComputeOffset(muStore, Padding);
      this.ComputePosition(muStore, Spacing);
      // set startup dimensions
      Dimensions.Width = Width.Clamp(Dimensions.Width);
      Dimensions.Height = Height.Clamp(Dimensions.Height);

      // get boundary for children
      var maxWidth = Math.Min(Width.Max.pxCount ?? Boundaries.Width ?? 0, Boundaries.Width ?? Width.Max.pxCount ?? 0);
      var maxHeight = Math.Min(Height.Max.pxCount ?? Boundaries.Height ?? 0, Boundaries.Height ?? Height.Max.pxCount ?? 0);
      Area ChildBoundaries = new Area
      {
        //Width = Horizontal ? (maxWidth - Padding.Left.Value - Padding.Right.Value) : 0,
        //Height =  Horizontal ? 0 : (maxHeight - Padding.Top.Value - Padding.Bottom.Value),

        Width = (maxWidth - Padding.Left.Value - Padding.Right.Value),
        Height = (maxHeight - Padding.Top.Value - Padding.Bottom.Value),
      };
      // Q: set contraxial size to 0, so that % contraxial size gets only applied during second pass?

      // line management
      int maxLineLength = 0;
      int maxLineSize = 0;
      int coaxialSize = Horizontal ? ChildBoundaries.Width.Value : ChildBoundaries.Height.Value;
      int coaxialSpacing = Horizontal ? Spacing.XPos.Value : Spacing.YPos.Value;
      int contraxialSpacing = Horizontal ? Spacing.YPos.Value : Spacing.XPos.Value;
      int totalLineSize = -contraxialSpacing;

      List<FlexListLine> Lines = new List<FlexListLine>();
      var currentLine = new FlexListLine(coaxialSize + coaxialSpacing);
      // first pass: calculate size & assign into lines
      foreach (var child in Children)
      {
        var node = child.Key;
        var data = child.Value;
        var recalc = false;
        this.ComputeOffset(muStore, data.Margin); // calculate pixel counts
        this.ComputeMeasurement(muStore, data.Basis); // calculate pixel counts
                                                      // do not calculate at 1st pass if using basis
                                                      // TODO: subtract margin per node as well
        node.Boundaries = ChildBoundaries; // assign new boundaries
        node.Dimensions.Reset(); // set everything to null
                                 // basis doesn't include height - so it's not possible to do second-only pass with basis
        node.Recalculate(manager, muStore);
        //if (data.Basis.HasValue)
        //{
        //    if (Horizontal) node.Dimensions.Width = data.Basis.Value;
        //    else node.Dimensions.Height = data.Basis.Value;
        //    recalc = true;
        //}
        //else
        //{
        //    node.Recalculate();
        //}
        // append node to current line if not wrapping or can be fit - otherwise create new line
        var nodeSize = (Horizontal
            ? node.Dimensions.Width + data.Margin.Left.Value + data.Margin.Right.Value + Spacing.XPos.Value
            : node.Dimensions.Height + data.Margin.Top.Value + data.Margin.Bottom.Value + Spacing.YPos.Value) ?? 0;
        var nodeContraxialSize = GetAxialSize(node, true) ?? 0;
        if (Wrap && currentLine.Nodes.Count > 0 && currentLine.FittingSize < nodeSize)
        {
          Lines.Add(currentLine);
          maxLineLength = Math.Max(maxLineLength, coaxialSize - currentLine.FittingSize);
          maxLineSize = Math.Max(maxLineSize, currentLine.MaxContraxialSize);
          totalLineSize += currentLine.MaxContraxialSize + contraxialSpacing;
          currentLine = new FlexListLine(coaxialSize + coaxialSpacing); // first padding doesn't count
        }
        // Postponed basis
        // REASON: would affect line fitting, at basis 0 all would be added to the same line, no matter the min size
        // also added FittingSize - used instead of LeftoverSize for in-line fitting condition to remove artifacts with lower than min basis
        currentLine.FittingSize -= nodeSize;
        if (data.Basis.HasValue)
        {
          if (Horizontal)
          {
            nodeSize = nodeSize - (node.Dimensions.Width ?? 0) + data.Basis.Value;
            node.Dimensions.Width = data.Basis.Value;
          }
          else
          {
            nodeSize = nodeSize - (node.Dimensions.Height ?? 0) + data.Basis.Value;
            node.Dimensions.Height = data.Basis.Value;
          }
          recalc = true;
        }
        currentLine.LeftoverSize -= nodeSize;
        if (currentLine.MaxContraxialSize < nodeContraxialSize) currentLine.MaxContraxialSize = nodeContraxialSize;
        currentLine.Grow += data.Grow;
        currentLine.Shrink += data.Shrink;
        currentLine.Nodes.Add(new FlexListLine.Data { node = node, data = data, recalc = recalc });
      }
      Lines.Add(currentLine);
      maxLineLength = Math.Max(maxLineLength, coaxialSize - currentLine.FittingSize);
      maxLineSize = Math.Max(maxLineSize, currentLine.MaxContraxialSize);
      totalLineSize += currentLine.MaxContraxialSize + contraxialSpacing;
      if (coaxialSize > 0)
        maxLineLength = Math.Min(coaxialSize, maxLineLength);
      // line pass: calculate size
      var leftoverCorrection = 0;
      if (Horizontal)
      {
        Dimensions.Width = Math.Max(maxLineLength + Padding.Left.Value + Padding.Right.Value, Dimensions.Width ?? 0);
        Dimensions.Height = Math.Max(totalLineSize + Padding.Top.Value + Padding.Bottom.Value, Dimensions.Height ?? 0);
        leftoverCorrection = (Dimensions.Width ?? 0) - coaxialSize - (Padding.Left.Value) - (Padding.Right.Value);
      }
      else
      {
        Dimensions.Width = Math.Max(totalLineSize + Padding.Left.Value + Padding.Right.Value, Dimensions.Width ?? 0);
        Dimensions.Height = Math.Max(maxLineLength + Padding.Top.Value + Padding.Bottom.Value, Dimensions.Height ?? 0);
        leftoverCorrection = (Dimensions.Height ?? 0) - coaxialSize - (Padding.Top.Value) - (Padding.Bottom.Value);
      }
      Dimensions.Width = Width.Clamp(Dimensions.Width);
      Dimensions.Height = Height.Clamp(Dimensions.Height);

      // second pass: reallocate nodes, set their sizes (with respect to their min/max), recalculate
      var maxContraxialSize = maxLineSize;
      var totalContraxialSize = WrapStretch
          ? (maxContraxialSize + contraxialSpacing) * Lines.Count - contraxialSpacing
          : totalLineSize;
      var contraxialLeftovers = (Horizontal
          ? Dimensions.Height - totalContraxialSize - Padding.Top.Value - Padding.Bottom.Value
          : Dimensions.Width - totalContraxialSize - Padding.Left.Value - Padding.Right.Value) ?? 0;

      // contraxial alignment
      var alignmentContraxialPadding = 0;
      var alignmentContraxialSpacing = 0; // not doing decimal accumulation - waste of performance
      if (contraxialLeftovers > 0)
        switch (ContraxialAlignment)
        {
          // start is default
          case EFlexAxialAlignment.End:
            alignmentContraxialPadding = contraxialLeftovers; break;
          case EFlexAxialAlignment.Center:
            alignmentContraxialPadding = contraxialLeftovers / 2; break;
          case EFlexAxialAlignment.SpaceBetween:
            alignmentContraxialSpacing = (contraxialLeftovers) / (Lines.Count - 1); break;
          case EFlexAxialAlignment.SpaceAround:
            alignmentContraxialSpacing = (contraxialLeftovers) / (Lines.Count);
            alignmentContraxialPadding = (int)(alignmentContraxialSpacing / 2); break;
          case EFlexAxialAlignment.SpaceEvenly:
            alignmentContraxialSpacing = (contraxialLeftovers) / (Lines.Count + 1);
            alignmentContraxialPadding = (int)alignmentContraxialSpacing; break;
        }
      // current contraxial position, ie. for normal horizontal this is Y offset of current line
      var contraxialPosition = ((Horizontal
          ? !ContraxReverse ? Padding.Top : Dimensions.Height - Padding.Bottom
          : !ContraxReverse ? Padding.Left : Dimensions.Width - Padding.Right) ?? 0)
          + (!ContraxReverse ? alignmentContraxialPadding : -alignmentContraxialPadding);

      foreach (var line in Lines)
      {
        if (WrapStretch) line.MaxContraxialSize = maxContraxialSize;
        var lineBoundaries = new Area
        {
          Width = ChildBoundaries.Width,
          Height = ChildBoundaries.Height,
        };
        if (Horizontal) lineBoundaries.Height = line.MaxContraxialSize;
        else lineBoundaries.Width = line.MaxContraxialSize;
        foreach (var node in line.Nodes) node.node.Boundaries = lineBoundaries;
        //1st - reduce leftovers by applying growth/shrinkage
        //2nd - redistribute leftovers (if positive) according to selected mode (AKA assign each node an coaxial X/YPos)
        //3rd - apply coaxial position & contraxial position/size
        //------------------------------ LEFTOVER REDUCTION ------------------------------//
        line.LeftoverSize += leftoverCorrection;
        var leftovers = line.LeftoverSize;
        // GROWTH
        if (leftovers > 0 && line.Grow > 0)
        {
          // get nodes that can grow
          var nodes = line.Nodes.Where(e => e.data.Grow > 0).ToList();
          var grow = line.Grow;
          var done = false;
          var ratio = ((float)leftovers) / grow; // variable growth ratio
                                                 // grow to cover all leftovers - unless max of all nodes is not enough
          while (nodes.Count > 0 && !done)
          {
            done = true;
            for (int i = nodes.Count - 1; i >= 0; i--)
            {
              var node = nodes[i];
              var currentCoaxialSize = GetAxialSize(node.node, false) ?? 0;
              // node that need to be already recalculated here didn't compute their extremes yet - do it now
              // Q: use old or new boundaries?
              //if (node.recalc) { node.node.ComputeRange(node.node.Width); node.node.ComputeRange(node.node.Height); }
              // REMOVED: need to compute height even while using basis - so first pass will always be executed
              // ADDED: min coax size - if using basis lower than min would produce artifacts without it
              var maxCoaxialSize = Horizontal ? node.node.Width.Max.pxCount : node.node.Height.Max.pxCount;
              var minCoaxialSize = Horizontal ? node.node.Width.Min.pxCount : node.node.Height.Min.pxCount;
              // growth would exceed max size - apply max size, reduce growth
              if (maxCoaxialSize.HasValue && maxCoaxialSize < currentCoaxialSize + ratio * node.data.Grow)
              {
                done = false;
                if (currentCoaxialSize != maxCoaxialSize)
                {
                  leftovers -= maxCoaxialSize.Value - currentCoaxialSize;
                  SetAxialSize(node.node, false, maxCoaxialSize);
                  node.recalc = true;
                }
                grow -= node.data.Grow;
                nodes.RemoveAt(i);
              }
              else if (minCoaxialSize.HasValue && minCoaxialSize > currentCoaxialSize + ratio * node.data.Grow)
              {
                done = false;
                if (currentCoaxialSize != minCoaxialSize)
                {
                  leftovers -= minCoaxialSize.Value - currentCoaxialSize;
                  SetAxialSize(node.node, false, minCoaxialSize);
                  node.recalc = true;
                }
                grow -= node.data.Grow;
                nodes.RemoveAt(i);
              }
            }
            // calculate new ratio - might change due to max size restrictions
            ratio = grow > 0 ? ((float)leftovers) / grow : 0;
          }
          // apply growth with decimal accumulation (Q: is decimal accumulation that important? pixel here or there won't save anybody)
          if (nodes.Count > 0)
          {
            float decimalPart = 0;
            foreach (var node in nodes)
            {
              var size = decimalPart + ratio * node.data.Grow;
              var intPart = (int)size;
              decimalPart = size - intPart;

              var currentCoaxialSize = GetAxialSize(node.node, false) ?? 0;
              SetAxialSize(node.node, false, currentCoaxialSize + intPart);
              node.recalc = true;
            }
            leftovers = 0;
          }
        }
        // SHRINKAGE
        else if (leftovers < 0 && line.Shrink > 0)
        {
          // TODO: shrinkage
        }
        //------------------------------ LEFTOVER REDISTRIBUTION/ALIGNMENT ------------------------------//
        var alignmentCoaxialOffset = 0;
        var alignmentCoaxialPadding = 0;  // not doing decimal accumulation - waste of performance
        var nodecount = line.Nodes.Count;
        if (leftovers > 0)
          switch (CoaxialAlignment)
          {
            // start is default
            case EFlexAxialAlignment.End:
              alignmentCoaxialOffset = leftovers; break;
            case EFlexAxialAlignment.Center:
              alignmentCoaxialOffset = leftovers / 2; break;
            case EFlexAxialAlignment.SpaceBetween:
              alignmentCoaxialPadding = (leftovers) / (line.Nodes.Count - 1); break;
            case EFlexAxialAlignment.SpaceAround:
              alignmentCoaxialPadding = (leftovers) / (line.Nodes.Count);
              alignmentCoaxialOffset = (alignmentCoaxialPadding / 2); break;
            case EFlexAxialAlignment.SpaceEvenly:
              alignmentCoaxialPadding = (leftovers) / (line.Nodes.Count + 1);
              alignmentCoaxialOffset = alignmentCoaxialPadding; break;
          }
        //------------------------------ POSITION ASSIGNMENT (incl. contraxial size) ------------------------------//
        var coaxialPosition = Horizontal ? !CoaxReverse ? Padding.Left.Value : (Dimensions.Width ?? 0) - Padding.Right.Value : !CoaxReverse ? Padding.Top.Value : (Dimensions.Height ?? 0) - Padding.Bottom.Value;
        coaxialPosition += (!CoaxReverse ? alignmentCoaxialOffset : -alignmentCoaxialOffset);
        foreach (var node in line.Nodes)
        {
          // COUNTERAXIS
          // ISSUE: can overflow on counteraxis
          var align = node.data.Alignment ?? LineAlignment;
          // not sure about reversing the alignment?
          // there def was an issue with positioning - this would have to be the case, since it's the only place the position gets computed
          var nodeContraxialPosition = contraxialPosition; //!ContraxReverse ? contraxialPosition : contraxialPosition - GetAxialSize(node.node, true);
          var contraxMargin = (Horizontal ? node.data.Margin.Top.Value : node.data.Margin.Left.Value);
          var contraxRevMargin = (Horizontal ? node.data.Margin.Bottom.Value : node.data.Margin.Right.Value);
          switch (align)
          {
            //TODO: counteraxial margin
            case EFlexLineAlignment.Start:
              SetAxialPosition(node.node, true, nodeContraxialPosition + contraxMargin);
              break;
            case EFlexLineAlignment.End:
              //nodeContraxialPosition += (line.MaxContraxialSize - GetAxialSize(node.node, true)) * (!ContraxReverse ? 1 : -1);
              nodeContraxialPosition += (line.MaxContraxialSize - (GetAxialSize(node.node, true) ?? 0) - contraxRevMargin);
              SetAxialPosition(node.node, true, nodeContraxialPosition);
              break;
            case EFlexLineAlignment.Center:
              nodeContraxialPosition += ((line.MaxContraxialSize - (GetAxialSize(node.node, true) ?? 0) - contraxMargin - contraxRevMargin) / 2);
              SetAxialPosition(node.node, true, nodeContraxialPosition);
              break;
            case EFlexLineAlignment.Stretch:
              //nodeContraxialPosition = !ContraxReverse ? contraxialPosition : contraxialPosition - line.MaxContraxialSize;
              SetAxialPosition(node.node, true, nodeContraxialPosition);
              if (GetAxialSize(node.node, true) != line.MaxContraxialSize)
              {
                node.recalc = true;
                SetAxialSize(node.node, true, line.MaxContraxialSize);
              }
              break;
          }
          // COAXIS
          var fullNodeAxialSize = Horizontal
              ? (node.node.Dimensions.Width ?? 0) + node.data.Margin.Left.Value + node.data.Margin.Right.Value + Spacing.XPos.Value
              : (node.node.Dimensions.Height ?? 0) + node.data.Margin.Top.Value + node.data.Margin.Bottom.Value + Spacing.YPos.Value;
          var nodeAxialPosition = Horizontal
              ? !CoaxReverse ? coaxialPosition + node.data.Margin.Left.Value : coaxialPosition - (node.node.Dimensions.Width ?? 0) - node.data.Margin.Right.Value
              : !CoaxReverse ? coaxialPosition + node.data.Margin.Top.Value : coaxialPosition - (node.node.Dimensions.Height ?? 0) - node.data.Margin.Bottom.Value;
          SetAxialPosition(node.node, false, nodeAxialPosition);
          coaxialPosition += (fullNodeAxialSize + alignmentCoaxialPadding) * (!CoaxReverse ? 1 : -1);
        }
        contraxialPosition += (line.MaxContraxialSize + alignmentContraxialSpacing + contraxialSpacing) * (!ContraxReverse ? 1 : -1);
        foreach (var node in line.Nodes.Where(e => e.recalc))
          node.node.Recalculate(manager, muStore); // ISSUE: contraxial relative size requires recalc - WORKAROUND: make the node stretch - node sizing is more important, so it can override it 
      }

    }
  }

}
