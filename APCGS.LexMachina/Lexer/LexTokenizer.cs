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
    public enum ECharacterCategory
    {
      // 0x00 - 0x21 & 0x7F
      Whitespace, // space(0x21), VT(0x0B), HT(0x09) 
      Newline, // CR(0x0D), LF(0x0A), FF(0x0C)
      Substitute, // SUB(0x1A)
      Control, // all others in the range & DEL(0x7F)
      // 0x22+
      LowercaseLetter, // [a-z]
      UppercaseLetter, // [A-Z]
      Letter, // [a-zA-Z]
      Digit, // [0-9]
      Alphanumeric, // Letter & Digit
      Word, // Alphanumeric & [_]
      Symbol, // !Word >=0x22 && < 0x7F
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

    private EHandler[] handlerTable = new EHandler[127];
    private bool newline = false;

    public bool Tokenize(char c)
    {
      Position++;
      // clear signature if no active tokens are present
      if (ActiveTokens.Count == 0) Signature.Clear();

      // ignore continuation characters - all unicode glyphs should be represented as single ASCII substitute character
      if (LexUtils.IsContinuation(c, Encoding)) return false;
      // swap non-ASCII chars with substitute char
      if (c > '\u007f') c = '\u001a';
      // handle newline
      if (c == '\n' || c == '\r') { if (!newline) newline = true; }
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
      // POSSIBLE TODO: merge interleaved into EMode
      // POSSIBLE TODO: extend modes, so this functionality is a flag
      if (ActiveTokens.Count == 0 && ResultLexemes.Count == 0 && Signature.Length > 0 && !IsInterleaved)
      {
        // FIXME
        //ResultLexemes.AddLast(new TokenMatch { token = null, lexeme = new LexLexeme(Position - Signature.Length, Signature/*, parser*/) });
        return true;
      }
      // check for new tokens
      if (IsInterleaved || ActiveTokens.Count == 0)
      {
        // FIXME
        /*
        List<ILexToken> newPossibleTokens = new List<ILexToken>();
        foreach (var token in Tokens)
          if (token.Advance("", c, 0))
            newPossibleTokens.Add(token);
        if (newPossibleTokens.Count > 0)
          ActiveTokens.Add(Signature.Length, newPossibleTokens);
        */
      }

      Signature.Append(c);
      return false;
    }
    public void Reset()
    {
      ActiveTokens.Clear();
      ResultLexemes.Clear();
    }
  }
}
