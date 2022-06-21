using APCGS.Utils.Refactor;
using System;

namespace APCGS.GuiGee.Measurements
{
  [NeedsDocumentation]
  public class MeasuredRange
  {
    public MeasuredRange(MeasurementUnit Unit, int? Default = null, int? Min = null, int? Max = null)
    {
      this.Default = new Measurement(Unit.Id, Default);
      this.Min = new Measurement(Unit.Id, Min);
      this.Max = new Measurement(Unit.Id, Max);
    }
    public MeasuredRange(int UnitId, int? Default = null, int? Min = null, int? Max = null)
    {
      this.Default = new Measurement(UnitId, Default);
      this.Min = new Measurement(UnitId, Min);
      this.Max = new Measurement(UnitId, Max);
    }
    public Measurement Min { get; set; }
    public Measurement Max { get; set; }
    public Measurement Default { get; set; }
    public int? Clamp(int? value) {
      if (value < 0) value = null;
      value = value ?? Default ?? Min ?? Max;
      if (!value.HasValue) return null;
      if (Max.HasValue) value = Math.Min(value.Value, Max.Value);
      if (Min.HasValue) value = Math.Max(value.Value, Min.Value);
      return value;
    }
    public override string ToString() => $"{Min}/{Default}/{Max}";
  }
}
