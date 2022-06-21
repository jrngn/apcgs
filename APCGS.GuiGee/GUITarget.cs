using APCGS.GuiGee.Nodes;
using APCGS.Utils.Refactor;
using APCGS.Utils.Registry;

namespace APCGS.GuiGee
{
  public delegate void TargetCallback(Node oldTarget, Node newTarget);
  [NeedsDocumentation]
  public class GUITarget : IRegistryEntry
  {
    internal GUITarget(string staticName, int id)
    {
      StaticName = staticName;
      Id = id;
    }
    public string StaticName { get => Key; private set => Key = value; }
    public string Key { get; private set; }
    public int Id { get; set; }

    public Node Target { get; internal set; }
    public bool HasTarget() => Target != null;
  }
}
