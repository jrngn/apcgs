using APCGS.Utils.Refactor;
using System.Collections.Generic;

namespace APCGS.GuiGee.Nodes
{
  [NeedsDocumentation]
  [Redesign(Reason = "component inner node tree resolution is not set in stone yet")]
  public abstract class ComponentNode: Node, IContainerNode
  {
    public bool Resolved { get; private set; } = false;
    public virtual void ResolveTemplate() { Resolved = true; }
    protected List<Node> Nodes { get; set; }
    public IEnumerable<Node> GetNodes() { if (!Resolved) ResolveTemplate(); return Nodes; }
  }
}
