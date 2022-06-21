using APCGS.Utils.Refactor;

namespace APCGS.GuiGee.Measurements
{
  [NeedsDocumentation]
  [Cleanup]
  public interface IPositioning
  {
    //MeasurementRatios Ratios { get; set; }

    MeasuredRange Width { get; set; }
    MeasuredRange Height { get; set; }

    //MeasuredOffset Postition { get; set; }
    //MeasuredOffset Margin { get; set; } // shouldn't be a part of container child data?

    //Area GetContentSize();
    Area GlobalDimensions { get; }
    Area Dimensions { get; set; }
    Area Boundaries { get; set; }

    void Recalculate(GUIManager manager, MeasurementUnitStore muStore);
  }
  public static class IPositioningExtensions
  {
    public static void ComputeMeasurement(this IPositioning positionable, MeasurementUnitStore muStore, Measurement measurement)
    {
      if (measurement == null) return;
      measurement.pxCount = (int?)(measurement.Count * muStore[measurement.UnitId].Converter(positionable));
    }
    public static void ComputeArea(this IPositioning positionable, MeasurementUnitStore muStore, MeasuredArea area)
    {
      if (area == null) return;
      positionable.ComputeMeasurement(muStore,area.XPos);
      positionable.ComputeMeasurement(muStore,area.YPos);
      positionable.ComputeMeasurement(muStore,area.Width);
      positionable.ComputeMeasurement(muStore,area.Height);
    }
    public static void ComputeOffset(this IPositioning positionable, MeasurementUnitStore muStore, MeasuredOffset offset)
    {
      if (offset == null) return;
      positionable.ComputeMeasurement(muStore,offset.Top);
      positionable.ComputeMeasurement(muStore,offset.Bottom);
      positionable.ComputeMeasurement(muStore,offset.Left);
      positionable.ComputeMeasurement(muStore,offset.Right);
    }
    public static void ComputePosition(this IPositioning positionable, MeasurementUnitStore muStore, MeasuredPosition position)
    {
      if (position == null) return;
      positionable.ComputeMeasurement(muStore,position.XPos);
      positionable.ComputeMeasurement(muStore,position.YPos);
    }
    public static void ComputeRange(this IPositioning positionable, MeasurementUnitStore muStore, MeasuredRange range)
    {
      if (range == null) return;
      positionable.ComputeMeasurement(muStore,range.Default);
      positionable.ComputeMeasurement(muStore,range.Min);
      positionable.ComputeMeasurement(muStore,range.Max);
    }
    public static bool Contains(this IArea area, int xpos, int ypos)
    {
      if (area.Width == null || area.Height == null) return false;
      return xpos >= (area.XPos??0) && xpos < (area.XPos??0) + area.Width && ypos >= (area.YPos??0) && ypos < (area.YPos??0) + area.Height;
    }
  }
}
