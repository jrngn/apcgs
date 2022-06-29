using APCGS.LexMachina.Tokens;
using APCGS.Utils.Refactor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace APCGS.LexMachina.Lexer
{
  /// <summary>
  /// Performs tokenization of source code, token at a time
  /// </summary>
  [WorkInProgress]
  [NeedsDocumentation]
  [NeedsTests(Reason = "Positioning in UTF-based streams must be checked, especially with token rewinding")]
  public class LexTokenizer
  {
    public struct TokenMatch
    {
      /// <summary>
      /// Currently matching token
      /// </summary>
      public ILexToken token;
      /// <summary>
      /// Additional token state used during tokenization
      /// </summary>
      public object state;
    }
    public struct SignaturePosition
    {
      public long sourcePos;
      public int stringPos;
      public SignaturePosition(long sourcePos, int stringPos)
      {
        this.sourcePos = sourcePos;
        this.stringPos = stringPos;
      }
      public override int GetHashCode() => stringPos.GetHashCode();
      public override string ToString() => stringPos.ToString();
      public override bool Equals(object obj) => stringPos.Equals(obj);
      public static implicit operator int(SignaturePosition pos) => pos.stringPos;
      public static implicit operator long(SignaturePosition pos) => pos.sourcePos;
    }
    [Flags]
    public enum EHandler
    {
      None = 0,

      Pass = 0,
      Trim = 1,
      Disallow = 2,
      Ignore = 4,
      Terminate = 8
    }

    [Flags]
    public enum EMode
    {
      None = 0x0,

      ReturnFirst = 0x0,
      ReturnSet = 0x1,
      ReturnAll = 0x2, // NOTE: this flag is kinda useless

      Interleaved = 0x4
    }
    // NOTE: at current point in time, tokenizer is best created once per source file, so line tracking could be handled exclusively by the tokenizer itself
    public LexTokenizer(EMode mode, long offset = 0, long lineStart = -1, int lineOffset = 0)
    {
      Mode = mode;
      Position = Offset = offset;
      CurrentLine = LineOffset = lineOffset;
      CurrentLineStart = lineStart < 0 ? Position : lineStart;
      Lines = new List<long> { CurrentLineStart };
    }
    public long Offset { get; protected set; }
    public long Position { get; protected set; }
    public int LineOffset { get; protected set; }
    public int CurrentLine { get; protected set; }
    public long CurrentLineStart { get; protected set; }
    public List<long> Lines { get; protected set; }

    public StringBuilder Signature { get; protected set; } = new StringBuilder();

    public IEnumerable<ILexToken> Tokens { get; set; }
    public Dictionary<SignaturePosition, List<TokenMatch>> ActiveTokens { get; protected set; } = new Dictionary<SignaturePosition, List<TokenMatch>>();
    public LinkedList<LexLexeme> ResultLexemes { get; protected set; } = new LinkedList<LexLexeme>();
    public EMode Mode { get; set; }
    public bool IsInterleaved { get => (Mode & EMode.Interleaved) != EMode.None; set => Mode = value ? Mode & ~EMode.Interleaved : Mode | EMode.Interleaved; }
    public Encoding Encoding { get; set; } = Encoding.Unicode;

    private EHandler[] handlerTable = new EHandler[128];
    private bool newline = false;

    public void SetCategoryHandlers(LexUtils.EASCIICategory category, EHandler handler)
    {
      foreach(var c in LexUtils.GetASCIICategoryCharacters(category))
        handlerTable[c] = handler;
    }

    public bool Tokenize(char c)
    {
      Position++;
      // clear signature if no active tokens are present
      if (ActiveTokens.Count == 0) Signature.Clear();

      // ignore continuation characters - all unicode glyphs should be represented as single ASCII substitute character
      if (LexUtils.IsContinuation(c, Encoding)) return false;
      var cat = LexUtils.GetASCIICategory(c);
      // swap non-ASCII chars with substitute char
      if (cat == LexUtils.EASCIICategory.NotASCII) {
        c = '\u001a';
        cat = LexUtils.EASCIICategory.Substitute;
      }
      // handle newline
      if (cat == LexUtils.EASCIICategory.Newline) newline = true;
      else if (newline)
      {
        newline = false;
        CurrentLine++;
        CurrentLineStart = Position;
        Lines.Add(Position);
      }

      // get & resolve handler for this character
      var h = handlerTable[c];
      var terminate = false; // termination mode - will skip ILexToken.Advance() and immediately go for .IsValid()
      if ((h & EHandler.Trim) != EHandler.None && ActiveTokens.Count == 0) return false; // trim unnecessary characters, default behaviour of whitespace & newlines
      if ((h & EHandler.Disallow) != EHandler.None) { ResultLexemes.AddLast(new LexLexeme(null, Offset, Position, Signature.ToString() + c, null)); return true; } // having these character creates an invalid lexeme
      if ((h & EHandler.Ignore) != EHandler.None) return false; // NOTE: it is rather weird to have a globally ignored character
      if ((h & EHandler.Terminate) != EHandler.None && ActiveTokens.Count != 0) terminate = true;

      // check current tokens
      foreach (var kv in ActiveTokens.ToArray())
      {
        var sig = kv.Key == 0 ? Signature.ToString() : Signature.ToString(kv.Key, Signature.Length - kv.Key);
        for (int i = kv.Value.Count - 1; i >= 0; i--)
        {
          var tm = kv.Value[i];
          if (!terminate && !tm.token.Advance(tm.state, sig, c, sig.Length))
          {
            kv.Value.Remove(tm);
            if (tm.token.IsValid(tm.state, sig))
            {
              // NOTE: longer tokens will be at the start of the list
              ResultLexemes.AddFirst(new LexLexeme(tm.token, kv.Key, Position, sig, tm.token.GetValue(tm.state, sig), CurrentLine, Position - CurrentLineStart));
              if (Mode == EMode.ReturnFirst) return true;
            }
          }
        }
        if (kv.Value.Count == 0) ActiveTokens.Remove(kv.Key);
      }
      if (ResultLexemes.Count > 0 && (Mode == EMode.ReturnSet || Mode == EMode.ReturnAll && ActiveTokens.Count == 0)) return true;
      // if tokenization occured in non-interleaved, but ultimately failed to produce any token, return the invalid snippet
      // POSSIBLE TODO: extend modes, so this functionality is a flag
      if (ActiveTokens.Count == 0 && ResultLexemes.Count == 0 && Signature.Length > 0 && !IsInterleaved)
      {
        ResultLexemes.AddLast(new LexLexeme(null, Position - Signature.Length, Position, Signature.ToString()));
        return true;
      }
      // check for new tokens
      if (IsInterleaved || ActiveTokens.Count == 0)
      {
        List<TokenMatch> newPossibleTokens = new List<TokenMatch>();
        foreach (var token in Tokens)
        {
          // TODO: add global state
          var match = new TokenMatch { state = token.GetTokenState(), token = token };
          if (token.Advance(match.state, "", c, 0))
            newPossibleTokens.Add(match);
        }
        if (newPossibleTokens.Count > 0)
          ActiveTokens.Add(new SignaturePosition(Position,Signature.Length), newPossibleTokens);
        
      }

      Signature.Append(c);
      return false;
    }
    public void SetPosition(long position)
    {
      Position = position;
      int i = -1;
      for (i = 0; i < Lines.Count; i++)
        if (position < Lines[i]) break;
      i = i - 1;
      if (i > 0)
      {
        CurrentLine = i + LineOffset;
        CurrentLineStart = Lines[CurrentLine];
      }
      else
      {
        CurrentLine = -1;
        CurrentLineStart = -1;
      }
    }
    public void Reset()
    {
      ActiveTokens.Clear();
      ResultLexemes.Clear();
    }
  }
}
