using APCGS.Utils.Refactor;

namespace APCGS.GuiGee.Measurements
{
  [NeedsDocumentation]
  [Redesign(Reason = "shouldn't there be a relation between MeasuredArea and MeasuredPosition? add IPosition interface")]
  public class MeasuredPosition
  {
    public Measurement XPos { get; set; }
    public Measurement YPos { get; set; }

    public MeasuredPosition(MeasurementUnit Unit, int? XPos = null, int? YPos = null)
    {
      this.XPos = new Measurement(Unit.Id, XPos);
      this.YPos = new Measurement(Unit.Id, YPos);
    }

    public MeasuredPosition(int UnitId, int? XPos = null, int? YPos = null)
    {
      this.XPos = new Measurement(UnitId, XPos);
      this.YPos = new Measurement(UnitId, YPos);
    }
  }
}