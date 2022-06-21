using APCGS.GuiGee.Measurements;
using APCGS.GuiGee.Nodes;
using APCGS.Utils.Refactor;
using System.Collections.Generic;
using System.Linq;
namespace APCGS.GuiGee.Extensions
{
  public interface IDrawableNode
  {
    void Draw(GUIDrawerExtension drawer, Area area);
  }
  public interface IDrawableExtension
  {
    void Draw(GUIDrawerExtension drawer, Area area);
  }
  // abstract, since contains no drawing logic
  [NeedsDocumentation]
  public abstract class GUIDrawerExtension : GUIExtension
  {
    public static string StaticKey => "drawer";
    public override string Key => StaticKey;
    public List<IDrawableExtension> Extensions = new List<IDrawableExtension>();
    public Area RenderArea;
    public virtual void Draw()
    {
      foreach (var node in Manager.Nodes) DrawNode(node, RenderArea);
      foreach (var extension in Extensions) extension.Draw(this, RenderArea);
    }
    public virtual void DrawNode(Node node, Area area)
    {
      var newArea = new Area((node.Dimensions.XPos ?? 0) + (area.XPos ?? 0), (node.Dimensions.YPos ?? 0) + (area.YPos ?? 0), node.Dimensions.Width ?? 0, node.Dimensions.Height ?? 0);
      if (node is IDrawableNode dNode)
      {
        dNode.Draw(this, newArea);
      }
      if (node is IContainerNode pNode)
      {
        foreach (var cNode in pNode.GetNodes().OrderBy(e => e.Order)) DrawNode(cNode, newArea);
      }
    }
  }
}  
