using APCGS.Utils.Refactor;

namespace APCGS.GuiGee.Measurements
{
  [NeedsDocumentation]
  [WorkInProgress(Reason = "more measurement capabilities - split to many classes: RawMeasurement, Measurement, CompoundMeasurement/CalcMeasurement")]
  public class Measurement
  {
    public Measurement(MeasurementUnit Unit, float? Count = null) { this.Count = Count; this.UnitId = Unit.Id; pxCount = null; }
    public Measurement(int UnitId, float? Count = null) { this.Count = Count; this.UnitId = UnitId; pxCount = null; }
    public int UnitId { get; set; }
    
    public float? Count { get; set; }
    public int? pxCount { get; set; }
    public bool HasValue => pxCount.HasValue;
    public int Value => pxCount ?? 0;
    public string ToString(MeasurementUnitStore mus) => $"{Count}{mus[UnitId]?.ToString()??"?"}: {pxCount}px";
    public static implicit operator int? (Measurement instance) { return instance.pxCount; }
  }
}
