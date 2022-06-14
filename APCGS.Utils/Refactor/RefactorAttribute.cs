using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APCGS.Utils.Refactor
{
  // some refactoring attributes
  // yes, I know I could just TODO everything (or even create custom tags for that matter)
  // but its easier to Shift+F12 on these attributes to jump directly to specific areas of the code that require more work
  // plus I get the intellisense autocomplete and error marking without me remembering exactly what the tags were - which reduces the probability of mistyping and letting the issue go into oblivion
  // after all, in the final product all uses of these attributes should be eventually removed in code, and by extension also omitted due to compiler optimizations (maybe? possibly? idk)

  [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
  public abstract class RefactorAttribute : Attribute
  {
    public string Reason { get; set; }
  }

  /// <summary>
  /// Work in progress - contains method stubs, needs fixes, or generally is incomplete.
  /// </summary>
  public class WorkInProgressAttribute : RefactorAttribute { }
  /// <summary>
  /// Needs documentation - certain fields/methods could use some additional informations, especially regarding their unique data-driven behaviour.
  /// </summary>
  public class NeedsDocumentationAttribute : RefactorAttribute { }
  /// <summary>
  /// Subject for removal - this portion of the code does not provide immediate benefits (eiter as a result of overdesigning or simplification); find its purpose, remove or redesign. <br/>
  /// For better readability use in conjunction with <see cref="ObsoleteAttribute"/>.
  /// </summary>
  public class SubjectForRemovalAttribute : RefactorAttribute { }
  /// <summary>
  /// Redesign - this portion of the code does not fit requirements or simply could be just better for various reasons.
  /// </summary>
  public class RedesignAttribute : RefactorAttribute { }
}
