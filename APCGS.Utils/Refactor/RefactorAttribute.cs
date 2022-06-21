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
  // note: not all of them indicate a refactor needs, however they are used DURING refactor pass to annotate additional work
  // after all, refactoring refers to all actions taken to make the code more maintainable without modifying its core functionality - and these attributes do nothing on their own

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
  /// <summary>
  /// Questionable - not necessarily a bad thing, but neither plainly good; used to denote functionality that is either misleading, problematic or cryptic - or simply does not have a specific known use.
  /// </summary>
  public class QuestionableAttribute : RefactorAttribute { }
  /// <summary>
  /// Needs fix - this portion of the code does not behave as it supposed to, debug/write tests and fix any issues
  /// </summary>
  public class NeedsFixAttribute : RefactorAttribute { }
  /// <summary>
  /// Needs tests - test coverage is not sufficient for this portion of the code, esp. if the code consists of complex algorithms
  /// </summary>
  public class NeedsTestsAttribute : RefactorAttribute { }
  /// <summary>
  /// Cleanup - this portion of the code could be made more comprehensible through simple refactoring (ie. more descriptive variable naming, reordering instructions, removing useless comments etc.)
  /// </summary>
  public class CleanupAttribute : RefactorAttribute { }
}
