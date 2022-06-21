using APCGS.Utils.Refactor;

namespace APCGS.GuiGee.Measurements
{
  public interface IArea : IOffset
  {
    int? XPos { get; set; }
    int? YPos { get; set; }
    int? Width { get; set; }
    int? Height { get; set; }
  }
  [NeedsDocumentation]
  public class Area : IArea
  {
    public Area(int? xpos = null, int? ypos = null, int? width = null, int? height = null)
    {
      XPos = xpos;
      YPos = ypos;
      Width = width;
      Height = height;
    }

    public int? XPos { get; set; }
    public int? YPos { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }

    public int? Left { get { return XPos; } set { XPos = value; } }
    public int? Top { get { return YPos; } set { YPos = value; } }
    public int? Right { get { return Width + XPos; } set { if (Width.HasValue && value.HasValue) XPos = Width - value; } }
    public int? Bottom { get { return Height + YPos; } set { if (Width.HasValue && value.HasValue) YPos = Height - value; } }

    public void Reset() { XPos = YPos = Width = Height = null; }

    public Area Offset(IOffset by)
    {
      return new Area (
        (XPos??0) + (by?.Left??0),
        (YPos??0) + (by?.Top??0),
        Width,
        Height
      );
    }
  }
  public class MeasuredArea : IArea
  {
    public Measurement XPos { get; set; } = new Measurement(MeasurementUnit.Units.Id);
    public Measurement YPos { get; set; } = new Measurement(MeasurementUnit.Units.Id);

    public Measurement Width { get; set; } = new Measurement(MeasurementUnit.Units.Id);
    public Measurement Height { get; set; } = new Measurement(MeasurementUnit.Units.Id);

    int? IArea.XPos { get { return XPos.pxCount; } set { XPos.pxCount = value; } }
    int? IArea.YPos { get { return YPos.pxCount; } set { YPos.pxCount = value; } }
    int? IArea.Width { get { return Width.pxCount; } set { Width.pxCount = value; } }
    int? IArea.Height { get { return Height.pxCount; } set { Height.pxCount = value; } }

    public int? Left { get { return XPos; } set { XPos.pxCount = value; } }
    public int? Top { get { return YPos; } set { YPos.pxCount = value; } }
    public int? Right { get { return Width + XPos; } set { if (Width.pxCount.HasValue) XPos.pxCount = Width - value; } }
    public int? Bottom { get { return Height + YPos; } set { if (Height.pxCount.HasValue) YPos.pxCount = Height - value; } }
  }
}
