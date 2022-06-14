using System;

namespace APCGS.LexMachina.Tokens
{
  // question: why not make all tokens stateful, where token state always contains string constructed from characters that passed Advance()?
  // issue: currently lexer extracts this string from previously analyzed source only when needed - ie. when interleaved or when constructing a lexeme(= token finished with valid string)
  // in other words, doing so in most cases we would turn single string to many, many, MANY redundant duplicates
  // on top of that, this string is being constructed using StringBuilder, so there

  // TODO: design altering/creation of lexemes through tokens (REASON: tokens should have the ability to set value of the lexemes)
  // probably just a method with object GetValue(object state,string str) signature, invoked by lexer during lexeme creation
  /// <summary>
  /// Base interface used in tokenization. Contains behaviour describing how to extract snippets of code (aka lexemes). <br/>
  /// Only one token describing a particular class of lexemes should exist at a time, so that tokens could be compared by reference. <br/>
  /// As such, token data cannot contain runtime-altered behavioural switches - use global and/or token state for this purpose <br/>
  /// Check <see cref="GetTokenState"/> for more information
  /// </summary>
  public interface ILexToken
  {
    /// <summary>
    /// Advance tokenization by a single character. Can alter the state.
    /// </summary>
    /// <param name="state">State of tokenization. Always <see langword="null"/> for stateless tokens.</param>
    /// <param name="str">Previously matched string.</param>
    /// <param name="c">Current character.</param>
    /// <param name="p">Current position.</param>
    /// <returns><see langword="true"/> if character has been consumed (ie. was valid and altered the state)</returns>
    bool Advance(object state, string str, char c, int p);
    /// <summary>
    /// Performs additional validation on final string which have not been performed during <see cref="Advance"/> method invokation.<br/>
    /// Should be called right after <see cref="Advance"/> call returned false.
    /// </summary>
    /// <param name="state">State of tokenization. Always <see langword="null"/> for stateless tokens.</param>
    /// <param name="str">String containing all characters that have previously passed through the <see cref="Advance"/> method.</param>
    /// <returns></returns>
    bool IsValid(object state, string str);
    /// <summary>
    /// Returns clean tokenization state necessary for finding a new match.
    /// </summary>
    /// <param name="globalState">
    /// Global state, containing unique language dependent metadata. <br/>
    /// Used primarily for synchronizing data between unrelated passes (ie. altering lexer behaviour during interleaved parsing without both of them knowing each other). <br/>
    /// Any data from this state that is utilized during tokenization should be copied or referenced in the returned state. <br/>
    /// NOTE: this state is used to resovle a set of very specific problems, in most cases its existence can be safely ignored.
    /// </param>
    /// <returns>Object instance storing additional data for use during tokenization. Should return <see langword="null"/> if token is stateless.</returns>
    object GetTokenState(object globalState = null);
    /// <summary>
    /// Computes the value of the lexeme created using this token. <br/>
    /// Assumes that passed string was fully validated (ie. all characters passed <see cref="Advance(object, string, char, int)"/> and string itself passed <see cref="IsValid(object, string)"/>)
    /// </summary>
    /// <param name="state">State of tokenization. Always <see langword="null"/> for stateless tokens.</param>
    /// <param name="str">Fully validated string maching this token</param>
    /// <returns>
    /// Value of the lexeme. Should return <see langword="null"/> for value-less tokens. <br/>
    /// NOTE: lexeme already contains string that matched this token - returning the string itself as value is redundant
    /// </returns>
    object GetValue(object state, string str);
  }
}
