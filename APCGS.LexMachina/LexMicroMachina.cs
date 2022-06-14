using APCGS.LexMachina.Lexer;
using APCGS.LexMachina.Tokens;
using APCGS.Utils.Refactor;
using System.Collections.Generic;
using System.Text;

namespace APCGS.LexMachina
{
  /// <summary>
  /// Also known as stateless <see cref="LexMachina"/>, this class provides decoupled functionality of the full <see cref="LexMachina"/> to perform common tasks without the burden of boilerplate necessary for other functions.<br/>
  /// Check source/documentation of its methods to learn how <see cref="LexMachina"/> works under the hood with just a limited set of functionality active at a time.
  /// </summary>
  [WorkInProgress(Reason = "add method utilizing lexer")]
  [Redesign(Reason = "later this class can become bloated from all of the capabilities - change to partial?")]
  public static class LexMicroMachina
  {
    /// <summary>
    /// Part of <see cref="LexTokenizer"/> - Extracts given token from the start of the source. <br/>
    /// All unicode glyphs will get converted to single ASCII substitute character, however the positions in returned lexeme will point to absolute character positions; <br/>
    /// ie. a surrogate pair in UTF16 source will be counted as two separate characters, but resolved inside token as a single character with value 0x1A
    /// </summary>
    /// <param name="token">Token to extract</param>
    /// <param name="source">Source on which the extraction will take place</param>
    /// <param name="globalState">Global state used by certain tokens; in most cases can be <see langword="null"/></param>
    /// <param name="encoding">Encoding of the source; if ommited will default to <see cref="Encoding.Unicode"/></param>
    /// <returns>
    /// Lexeme containing all processed characters. <br/>
    /// Characters are counted as processed only when <see cref="ILexToken.Advance(object, string, char, int)"/> returns <see langword="true"/>. <br/>
    /// If token matched, it will be set inside <see cref="LexLexeme.token"/> with corresponding value (if applicable).
    /// </returns>
    public static LexLexeme ExtractToken(ILexToken token, IEnumerable<char> source, object globalState = null, Encoding encoding = null)
    {
      encoding = encoding ?? Encoding.Unicode;
      object tokenState = token.GetTokenState(globalState);
      var sb = new StringBuilder();
      int p = -1;
      string cm = null;
      foreach (var _c in source)
      {
        var c = _c;
        p++; // treat every UTF glyph as single character for tokenization, or increase every character?
             // NOTE: lexemes MUST point to source positions - so if they include multichar glyphs their size > ascii representation
        if (LexUtils.IsContinuation(c, encoding)) continue; // ignore continuation characters - all unicode glyphs should be represented as single ASCII substitute character
        if (c > '\u007f') c = '\u001a'; // swap with substitute char

        cm = sb.ToString();
        sb.Append(c);
        if (token.Advance(tokenState, cm, c, p))
        {
          continue;
        }
        if (token.IsValid(tokenState, cm))
          return new LexLexeme(token, 0, p, cm, token.GetValue(tokenState, cm));
        else 
          return new LexLexeme(null, 0, p, cm); // duplicate to avoid returning with the last character
      }
      cm = sb.ToString();
      if (token.IsValid(tokenState, cm))
        return new LexLexeme(token, 0, p, cm, token.GetValue(tokenState, cm));
      return new LexLexeme(null, 0, p, cm);
    }

    public static LexLexeme GenerateToken(ILexTokenGenerator token, object globalState = null, int minRange = 0, int maxRange = 10)
    {
      var charCo = LexUtils.Random.Next(minRange, maxRange);
      var state = token.GetTokenState(globalState);
      var sb = new StringBuilder();
      char? c;
      while((--charCo)>=0)
      {
        if (!(c = token.Generate(state, sb.ToString(), sb.Length)).HasValue) break;
        sb.Append(c.Value);
      }
      while(!token.IsValid(state,sb.ToString()))
      {
        if (!(c = token.Generate(state, sb.ToString(), sb.Length)).HasValue) break;
        sb.Append(c.Value);
      }
      var str = sb.ToString();
      var valid = token.IsValid(state, str);
      var ret = new LexLexeme(valid ? token : null, 0, str.Length, str, valid ? token.GetValue(state, str) : null);
      return ret;
    }

  }
}
