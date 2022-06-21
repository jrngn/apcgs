using APCGS.GuiGee.Measurements;
using APCGS.Utils.Refactor;

namespace APCGS.GuiGee.Nodes
{
  [NeedsDocumentation]
  [Questionable]
  public abstract class WrapperNode<T> : ContainerNode<T>
        where T: class, new ()
  {
    public abstract MeasuredArea WrapperBoundaries(int key);
  }
}
