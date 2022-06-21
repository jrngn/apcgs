using APCGS.Utils.Refactor;

namespace APCGS.GuiGee.Measurements
{
  [NeedsDocumentation]
  public class MeasuredOffset : IOffset
  {
    public MeasuredOffset(MeasurementUnit Unit, int? Left = null, int? Top = null, int? Right = null, int? Bottom = null, int? Vertical = null, int? Horizontal = null, int? All = null)
    {
      this.Left = new Measurement(Unit.Id, Left ?? Horizontal ?? All);
      this.Top = new Measurement(Unit.Id, Top ?? Vertical ?? All);
      this.Right = new Measurement(Unit.Id, Right ?? Horizontal ?? All);
      this.Bottom = new Measurement(Unit.Id, Bottom ?? Vertical ?? All);
    }
    public MeasuredOffset(int UnitId, int? Left = null, int? Top = null, int? Right = null, int? Bottom = null, int? Vertical = null, int? Horizontal = null, int? All = null) {
      this.Left    = new Measurement(UnitId, Left ?? Horizontal ?? All);
      this.Top     = new Measurement(UnitId, Top ?? Vertical ?? All);
      this.Right   = new Measurement(UnitId, Right ?? Horizontal ?? All);
      this.Bottom  = new Measurement(UnitId, Bottom ?? Vertical ?? All);
    }
    public Measurement Left { get; set; }
    public Measurement Top { get; set; }
    public Measurement Right { get; set; }
    public Measurement Bottom { get; set; }

    int? IOffset.Left { get { return Left; } set { Left.pxCount = value; } }
    int? IOffset.Top { get { return Top; } set { Top.pxCount = value; } }
    int? IOffset.Right { get { return Right; } set { Right.pxCount = value; } }
    int? IOffset.Bottom { get { return Bottom; } set { Bottom.pxCount = value; } }
  }

  public interface IOffset
  {
    int? Left { get; set; }
    int? Top { get; set; }
    int? Right { get; set; }
    int? Bottom { get; set; }
  }
  public class Offset : IOffset
  {
    public int? Left { get; set; }
    public int? Top { get; set; }
    public int? Right { get; set; }
    public int? Bottom { get; set; }
  }
}
