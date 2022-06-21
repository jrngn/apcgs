using APCGS.Utils.Refactor;
using System.Collections.Generic;
using System.Linq;

namespace APCGS.GuiGee.Nodes
{
  public interface IContainerNode
  {
    //void UpdateChildrenBoundary(Node child);
    //void RebuildConnections();
    IEnumerable<Node> GetNodes();
    //IOrderedEnumerable<Node> GetOrderedNodes();
  }

  [NeedsDocumentation]
  [Cleanup]
  public abstract class ContainerNode<T> : Node, IContainerNode
        where T: class,new()
  {
    public List<KeyValuePair<Node,T>> Children { get; set; } = new List<KeyValuePair<Node, T>>();
    public virtual ContainerNode<T> ChainAddNode(Node node ,T nodeData = null) { AddNode(node, nodeData); return this; }
    public virtual KeyValuePair<Node, T> AddNode(Node node ,T nodeData = null) { node.Parent = this; var ret = new KeyValuePair<Node, T>(node, nodeData ?? new T()); Children.Add(ret); return ret; }
    // TODO: replace LINQ
    public virtual IEnumerable<Node> GetNodes() => Children.Select(e => e.Key);
    //public virtual IOrderedEnumerable<Node> GetOrderedNodes() => Children.Select(e => e.Key).OrderBy(e => e.Order);
    //public override bool Trigger(GUIManager manager,GUIEvent @event, object data)
    //{
    //  if (!base.Trigger(manager,@event, data)) return false; // node doesn't exist or some callbacks stopped propagation
    //  foreach (var child in Children) if(!child.Key.Trigger(manager,@event, data)) return false;
    //  return true;
    //}
    //public virtual void UpdateChildrenBoundary(Node child) { child.Boundaries = new Rectangle(0, 0, Boundaries.Width - Padding.Left.pxCount - Padding.Right.pxCount, Boundaries.Height - Padding.Top.pxCount - Padding.Bottom.pxCount); }
    //public void RebuildConnections() { foreach (var child in Children) { child.Key.Parent = this; if (child is IContainerNode) (child as IContainerNode).RebuildConnections(); } }
  }
  public abstract class ContainerNode : ContainerNode<object> { }
}
