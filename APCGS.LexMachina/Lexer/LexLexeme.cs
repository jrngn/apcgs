using APCGS.LexMachina.Tokens;

namespace APCGS.LexMachina.Lexer
{
  /// <summary>
  /// Snippet of the source code behaving as an instance of a specific token. Should only be created once its governing token fully matched. <br/>
  /// This class is usually just created by <see cref="LexTokenizer"/> as an output of tokenization.
  /// </summary>
  public class LexLexeme
  {
    /// <summary>
    /// Character-wise starting position in source code
    /// </summary>
    public long srcBegin;
    /// <summary>
    /// Character-wise end position in source code
    /// </summary>
    public long srcEnd;
    /// <summary>
    /// Line number in source code at which this lexeme is present
    /// </summary>
    public int line;
    /// <summary>
    /// Character-wise starting position from the beginning of the line containing this lexeme in source code
    /// </summary>
    public long linePos;
    /// <summary>
    /// Extracted ASCII string that matched token of this lexeme <br/>
    /// NOTE: <see cref="LexTokenizer"/> converts all characters > 0x7F and multicharacter glyphs to a single ASCII substitute character (0x1A) <br/>
    /// which also means that contents.Length &lt;= srcEnd - srcBegin
    /// </summary>
    public string contents;
    /// <summary>
    /// Token-dependent stored value of this lexeme
    /// </summary>
    public object value;
    /// <summary>
    /// Token that matched this lexeme (if any) <br/>
    /// Tokenless lexemes can be regarded as simple snippets of code, and should not contain any value
    /// </summary>
    public ILexToken token;
    public LexLexeme(ILexToken token, long srcBegin, long srcEnd, string contents = null, object value = null, int line = 0, long linePos = 0)
    {
      Initialize(token, srcBegin, srcEnd, contents, value, line, linePos);
    }
    public void Initialize(ILexToken token, long srcBegin, long srcEnd, string contents, object value, int line = 0, long linePos = 0)
    {
      this.token = token;
      this.srcBegin = srcBegin;
      this.srcEnd = srcEnd;
      this.line = line;
      this.linePos = linePos;
      this.contents = contents;
      this.value = value;
      this.token = token;
      if (srcBegin >= 0 && srcEnd == 0) this.srcEnd = srcBegin + (contents != null ? contents.Length : 0);
      //SyncLinePos(parser);
    }
    // TODO: as extension method
    //public void SyncLinePos(ILexParser parser)
    //{
    //  line = parser.Lines.Count + 1;
    //  linePos = srcBegin - (parser.Lines.Count == 0 ? 0 : parser.Lines[parser.Lines.Count - 1]);
    //}
  }
}
