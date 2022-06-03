using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APCGS.LexMachina
{
  // some refactoring attributes
  // yes, I know I could just TODO everything (or even create custom tags for that matter)
  // but its easier to Shift+F12 on these attributes to jump directly to specific areas of the code that require more work
  // plus I get the intellisense autocomplete and error marking without me remembering exactly what the tags were - which reduces the probability of mistyping and letting the issue go into oblivion
  // after all, in the final product all uses of these attributes should be eventually removed
  public class WorkInProgressAttribute : Attribute { }
  public class NeedsDocumentationAttribute : Attribute { }

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
  /// <summary>
  /// <see cref="ILexToken"/> with additional capability of generating characters that would fit this token (for procedural generation of tokens). <br/>
  /// Unnecessary for lexing itself, this interface can be implemented in order to create test case generators, or even fully featured text generators.
  /// </summary>
  public interface ILexTokenGenerator : ILexToken
  {
    /// <summary>
    /// Generates new character that will match this token. Basically a reverse of Advance() method, where instead of consuming a character we generate a new one. <br/>
    /// NOTE: Constructed string still needs to be validated with <see cref="ILexToken.IsValid(object, string)"/>.
    /// </summary>
    /// <returns>New character that would pass Advance() with given state. Should return <see langword="null"/> if no further characters can be generated at all.</returns>
    /// <inheritdoc cref="ILexToken.Advance(object, string, char, int)"/>
    char? Generate(object state, string str, int p);
    /// <summary>
    /// Serializes value object that can be reconstructed later using this token.
    /// </summary>
    /// <param name="value">Value for serialization. Only types supported by this token (if any) will be serialized. </param>
    /// <returns>Serialized value of the object. Should return <see langword="null"/> if this token doesn't serialize this value.</returns>
    string Serialize(object value);
  }

  /// <summary>
  /// Stateless single character token
  /// </summary>
  public class CharLexToken : ILexTokenGenerator
  {
    public CharLexToken(char c) { this.c = c; }
    /// <summary>
    /// Character defining the only valid form of this token
    /// </summary>
    public char c;
    public bool IsValid(object state, string str) => true;
    public bool Advance(object state, string str, char c, int p) => p == 0 && c == this.c;
    public object GetTokenState(object globalState = null) => null;
    public object GetValue(object state, string str) => null;

    public char? Generate(object state, string str, int p) => p == 0 ? c : (char?)null;
    public string Serialize(object value) => null;
  }
  /// <summary>
  /// Stateless string matching token
  /// </summary>
  public class StringLexToken : ILexTokenGenerator
  {
    public StringLexToken(string str) { this.str = str; }
    /// <summary>
    /// String defining the only valid form of this token
    /// </summary>
    public string str;
    public bool IsValid(object state, string str) => str == this.str;
    public bool Advance(object state, string str, char c, int p) => p < this.str.Length && c == this.str[p];
    public object GetTokenState(object globalState = null) => null;
    public object GetValue(object state, string str) => null;

    public char? Generate(object state, string str, int p) => p < this.str.Length ? this.str[p] : (char?)null;
    public string Serialize(object value) => null;
  }
  /// <summary>
  /// Semi-stateless token used for extracting keynames <br/>
  /// same as <see cref="System.Text.RegularExpressions.Regex(string)"/> with "[_a-zA-Z]\\w*" as pattern
  /// </summary>
  public class KeynameLexToken : ILexTokenGenerator
  {
    public static KeynameLexToken Instance { get; } = new KeynameLexToken();
    public bool IsValid(object state, string str) => true;
    public bool Advance(object state, string str, char c, int p) => c == '_' || char.IsLetter(c) || (p > 0 && char.IsDigit(c));
    public object GetTokenState(object globalState = null) => null;
    public object GetValue(object state, string str) => null;

    public char? Generate(object state, string str, int p)
    {
      var ldiff = 'z' - 'a' + 1;
      var r = ldiff * 2 + 1;
      if (p > 0) r += 10;
      var v = LexUtils.Random.Next(r);
      if (v <= 0) return '_';
      v--; if (v < ldiff) return (char)('a' + v);
      v -= ldiff; if (v < ldiff) return (char)('A' + v);
      v -= ldiff; return (char)('0' + v);
    }

    public string Serialize(object value) => null;
  }

  /// <summary>
  /// Base state interface of the state machine based tokens. Contains id of currently active state and validity of the whole passed string.
  /// </summary>
  public interface ISMLexTokenState
  {
    /// <summary>
    /// Id of currently active state; used to determine state transition table.
    /// </summary>
    int Id { get; set; }
    /// <summary>
    /// Whether lexeme in construction is currently valid
    /// </summary>
    bool Valid { get; set; }
  }
  /// <summary>
  /// Isolated behaviour of the state machine based token. Only one such class will be active at a time.
  /// </summary>
  /// <typeparam name="TState">State object used by the state machine</typeparam>
  public interface ISMLexTokenBehaviour<TState>
    where TState: ISMLexTokenState
  {
    /// <summary>
    /// Id of this state (NOTE: possibly redundant)
    /// </summary>
    int Id { get; set; }

    /// <summary>
    /// Returns all state ids valid for transition given the current state. Can return it's own id should the state persist.
    /// </summary>
    /// <param name="state">State of tokenization, including current state id of the state machine</param>
    /// <returns>Ids of valid transition states.</returns>
    IEnumerable<int> GetTransitions(TState state);

    /// <summary>
    /// Returns all characters that are valid for entering this state from the current one.<br/>
    /// Used primarily for token generation.
    /// </summary>
    /// <param name="state">State of tokenization, including current state id of the state machine</param>
    /// <returns>Valid characters for this state.</returns>
    IEnumerable<char> GetValidCharacters(TState state);

    // TODO: (optional) split to CanEnter() and OnEnter() function calls, for better verbosity
    /// <summary>
    /// Behaviour of this state, called once when entering this state; can alter the state. Keeping the same state still causes reentry.
    /// </summary>
    /// <returns></returns>
    /// <inheritdoc cref="SMLexToken{TState}.Advance(TState, string, char, int)"/>
    void OnEnter(TState state, string str, char c, int p);
    /// <summary>
    /// Determines wheter the state can be entered in the first place, cannot alter the state.
    /// </summary>
    /// <returns><see langword="true"/> if state can be entered with provided character and state. <br/>
    /// In most cases should be the same as calling <see cref="Enumerable.Contains{TSource}(IEnumerable{TSource}, TSource)"/> on <see cref="GetValidCharacters(TState)"/> with provided character.
    /// </returns>
    /// <inheritdoc cref="SMLexToken{TState}.Advance(TState, string, char, int)"/>
    bool CanEnter(TState state, string str, char c, int p);
  }

  [NeedsDocumentation]
  public class SMLexTokenYBehaviour<TState> : ISMLexTokenBehaviour<TState>
    where TState : ISMLexTokenState
  {
    public int Id { get; set; }
    public Func<TState, string, char, int, bool> YCanEnter { get; set; }
    public Action<TState, string, char, int> YOnEnter { get; set; }
    public Func<TState, IEnumerable<int>> YGetTransitions { get; set; }
    public Func<TState, IEnumerable<char>> YGetValidCharacters { get; set; }

    public bool IsCharacterValid(TState state, char c) => YGetValidCharacters?.Invoke(state)?.Contains(c) ?? false;
    public bool CanEnter(TState state, string str, char c, int p) => YCanEnter?.Invoke(state, str, c, p) ?? IsCharacterValid(state, c);
    public void OnEnter(TState state, string str, char c, int p) => YOnEnter?.Invoke(state, str, c, p);

    public IEnumerable<int> GetTransitions(TState state) => YGetTransitions?.Invoke(state) ?? null;
    public IEnumerable<char> GetValidCharacters(TState state) => YGetValidCharacters?.Invoke(state) ?? null;
  }

  /// <summary>
  /// Base class for state machine based token
  /// </summary>
  /// <typeparam name="TState">State object used by the state machine</typeparam>
  public abstract class SMLexToken<TState>: ILexTokenGenerator
    where TState: ISMLexTokenState
  {
    /// <summary>
    /// Array of all states utilized by this token
    /// </summary>
    public ISMLexTokenBehaviour<TState>[] States { get; set; }

    public bool IsValid(object state, string str) => IsValid(state is TState tstate ? tstate : default, str);
    public bool Advance(object state, string str, char c, int p) => Advance(state is TState tstate ? tstate : default, str,c,p);
    public object GetTokenState(object globalState = null) => GetTTokenState(globalState);
    public object GetValue(object state, string str) => GetValue(state is TState tstate ? tstate : default, str);
    public char? Generate(object state, string str, int p) => Generate(state is TState tstate ? tstate : default, str, p);

    public abstract string Serialize(object value);


    /// <param name="state">State of tokenization, including current state id of the state machine</param>
    /// <inheritdoc cref="IsValid(object, string)"/>
    public virtual bool IsValid(TState state, string str) => state.Valid;

    /// <param name="state">State of tokenization, including current state id of the state machine</param>
    /// <inheritdoc cref="Advance(object, string, char, int)"/>
    public virtual bool Advance(TState state, string str, char c, int p) {
      var cstate = States[state.Id];
      var transitions = cstate.GetTransitions(state);
      if(transitions != null)
        foreach (var tid in transitions)
        {
          var tstate = States[tid];
          if (tstate.CanEnter(state, str, c, p))
          {
            tstate.OnEnter(state, str, c, p);
            state.Id = tid;
            return true;
          }
        }
      return false;
    }

    /// <returns>Object instance storing current state id of the state machine, along with additional data for use during tokenization.</returns>
    /// <inheritdoc cref="GetTokenState(object)"/>
    public abstract TState GetTTokenState(object globalState = null);
    /// <param name="state">State of tokenization, including current state id of the state machine</param>
    /// <inheritdoc cref="GetValue(object,string)"/>
    public abstract object GetValue(TState state, string str);
    public char? Generate(TState state, string str, int p)
    {
      var cstate = States[state.Id];
      var transitions = cstate.GetTransitions(state)?.ToArray();
      if (transitions == null) return null;
      var tid = transitions[LexUtils.Random.Next(transitions.Length)];
      var tstate = States[tid];
      var characters = tstate.GetValidCharacters(state)?.ToArray();
      if (characters == null) return null;
      var c = characters[LexUtils.Random.Next(characters.Length)];
      // shouldn't the condition below throw an error? technically GetValidCharacters() is only state dependent...
      // do we even need str & p in SM-based tokens?
      if (!tstate.CanEnter(state, str, c, p)) return null;
      tstate.OnEnter(state, str, c, p);
      state.Id = tid;
      return c;
    }
  }


  
  public enum ENumberBase : byte
  {
    Invalid = 0,
    Binary = 2,
    Octal = 8,
    Decimal = 10,
    Hex = 16,
  }
  public enum ESign : byte
  {
    Unknown = 0, // defaults to positive
    Positive = 1,
    Negative = 2
  }
  public enum ENumberType : byte
  {
    Unknown = 0,
    UnknownFloat,
    Byte, // 's', or ? - cannot use <'f' due to hex notation, cannot use 'b' due to binary specifier
    Short, // 'i', or 's'
    Int, // 'l', or 'i'
    Long, // 'll', or 'l'
    Float, // 'f'
    Double // 'd'
  }
  public class NumericLexTokenState : ISMLexTokenState
  {
    public enum EParts : byte
    {
      Integral = 0,
      Fraction,
      Exponent
    }
    public NumericLexToken.EState EId { get; set; } = NumericLexToken.EState.BEGIN;
    public int Id { get => (int)EId; set => EId = Enum.IsDefined(typeof(NumericLexToken.EState), value) ? (NumericLexToken.EState)value : NumericLexToken.EState.INVALID; }
    public bool Valid { get; set; } = false;
    public ENumberBase Base { get; set; } = ENumberBase.Decimal;
    public EParts Part { get; set; } = EParts.Integral;
    public long[] Parts { get; set; } = new long[] { 0, 0, 0 };
    public int FractionDigits { get; set; } = 0;
    public ESign Sign { get; set; } = ESign.Unknown;
    public ESign ExpSign { get; set; } = ESign.Unknown;
    public bool Unsigned { get; set; } = false;
    public ENumberType Type { get; set; } = ENumberType.Unknown;
  }
  [WorkInProgress]
  public class NumericLexToken : SMLexToken<NumericLexTokenState>
  {
    public enum EState
    {
      INVALID = -1,
      BEGIN,
      LEAD_ZERO,
      LEAD_DIGIT,
      BASE_SPECIFIER,
      DIGIT,
      SIGN,
      FRACTION,
      EXPONENT,
      SIGN_SPECIFIER,
      IPREC_SPECIFIER,
      LLPREC_SPECIFIER,
      FPREC_SPECIFIER
    }
    public NumericLexToken() {
      States = new[] {
        new SMLexTokenYBehaviour<NumericLexTokenState>(){ Id = (int)EState.BEGIN, YGetTransitions = BEGIN_Transitions },
        new SMLexTokenYBehaviour<NumericLexTokenState>(){ Id = (int)EState.LEAD_ZERO, YGetTransitions = LEAD_ZERO_Transitions, YOnEnter = LEAD_ZERO_Enter, YGetValidCharacters = LEAD_ZERO_ValidCharacters },
        new SMLexTokenYBehaviour<NumericLexTokenState>(){ Id = (int)EState.LEAD_DIGIT, YGetTransitions = LEAD_DIGIT_Transitions, YOnEnter = LEAD_DIGIT_Enter, YGetValidCharacters = LEAD_DIGIT_ValidCharacters },
        new SMLexTokenYBehaviour<NumericLexTokenState>(){ Id = (int)EState.BASE_SPECIFIER, YGetTransitions = BASE_SPECIFIER_Transitions, YOnEnter = BASE_SPECIFIER_Enter, YGetValidCharacters = BASE_SPECIFIER_ValidCharacters },
        new SMLexTokenYBehaviour<NumericLexTokenState>(){ Id = (int)EState.DIGIT, YGetTransitions = DIGIT_Transitions, YOnEnter = DIGIT_Enter, YGetValidCharacters = DIGIT_ValidCharacters },
        new SMLexTokenYBehaviour<NumericLexTokenState>(){ Id = (int)EState.SIGN, YGetTransitions = SIGN_Transitions, YOnEnter = SIGN_Enter, YGetValidCharacters = SIGN_ValidCharacters },
        new SMLexTokenYBehaviour<NumericLexTokenState>(){ Id = (int)EState.FRACTION, YGetTransitions = FRACTION_Transitions, YOnEnter = FRACTION_Enter, YGetValidCharacters = FRACTION_ValidCharacters },
        new SMLexTokenYBehaviour<NumericLexTokenState>(){ Id = (int)EState.EXPONENT, YGetTransitions = EXPONENT_Transitions, YOnEnter = EXPONENT_Enter, YGetValidCharacters = EXPONENT_ValidCharacters },
        new SMLexTokenYBehaviour<NumericLexTokenState>(){ Id = (int)EState.SIGN_SPECIFIER, YGetTransitions = SIGN_SPECIFIER_Transitions, YOnEnter = SIGN_SPECIFIER_Enter, YGetValidCharacters = SIGN_SPECIFIER_ValidCharacters },
        new SMLexTokenYBehaviour<NumericLexTokenState>(){ Id = (int)EState.IPREC_SPECIFIER, YGetTransitions = IPREC_SPECIFIER_Transitions, YOnEnter = IPREC_SPECIFIER_Enter, YGetValidCharacters = IPREC_SPECIFIER_ValidCharacters },
        new SMLexTokenYBehaviour<NumericLexTokenState>(){ Id = (int)EState.LLPREC_SPECIFIER, YGetTransitions = LLPREC_SPECIFIER_Transitions, YOnEnter = LLPREC_SPECIFIER_Enter, YGetValidCharacters = LLPREC_SPECIFIER_ValidCharacters },
        new SMLexTokenYBehaviour<NumericLexTokenState>(){ Id = (int)EState.FPREC_SPECIFIER, YGetTransitions = FPREC_SPECIFIER_Transitions, YOnEnter = FPREC_SPECIFIER_Enter, YGetValidCharacters = FPREC_SPECIFIER_ValidCharacters },
      };
    }
    public override NumericLexTokenState GetTTokenState(object globalState = null) => new NumericLexTokenState();

    public override object GetValue(NumericLexTokenState state, string str)
    {
      var ipart = (int)NumericLexTokenState.EParts.Integral;
      var fpart = (int)NumericLexTokenState.EParts.Fraction;
      var epart = (int)NumericLexTokenState.EParts.Exponent;

      if (state.Type != ENumberType.UnknownFloat && state.Type <= ENumberType.Long)
      {
        long v = state.Parts[ipart];
        if (state.Part >= NumericLexTokenState.EParts.Exponent) v *= (long)Math.Pow(10,state.Parts[epart]);
        if (state.Type == ENumberType.Unknown)
        {
          state.Type = ENumberType.Byte;
          if (v > ((state.Unsigned) ? (int)byte.MaxValue : sbyte.MaxValue)) state.Type = ENumberType.Short;
          if (v > ((state.Unsigned) ? (int)ushort.MaxValue : short.MaxValue)) state.Type = ENumberType.Int;
          // disallow unspecified 64-bit integers
          //if (v > ((state.Unsigned) ? uint.MaxValue : int.MaxValue)) state.Type = ENumberType.Long;
        }
        if (state.Sign == ESign.Negative) v = -v;
        switch (state.Type)
        {
          case ENumberType.Short:
            if (state.Unsigned) return (ushort)v;
            return (short)v;
          case ENumberType.Byte:
            if (state.Unsigned) return (byte)v;
            return (sbyte)v;
          case ENumberType.Int:
            if (state.Unsigned) return (uint)v;
            return (int)v;
          case ENumberType.Long:
            if (state.Unsigned) return (ulong)v;
            return v;
        }
      }
      else {
        double v = state.Parts[ipart];
        if (state.FractionDigits > 0) v += state.Parts[fpart] / Math.Pow(10, state.FractionDigits);
        if (state.Part >= NumericLexTokenState.EParts.Exponent)
        {
          var exp = Math.Pow(10, state.Parts[epart]);
          if (state.ExpSign == ESign.Negative && exp > 0) v = v / exp;
          else v = v * exp;
        }
        if (state.Sign == ESign.Negative) v = -v;
        switch (state.Type) {
          case ENumberType.UnknownFloat:
          case ENumberType.Float:
            return (float)v;
          case ENumberType.Double:
            return v;
        }
      }
      return null;
    }

    public override string Serialize(object value)
    {
      throw new NotImplementedException();
    }

    public char SeparatorChar { get; set; } = '\'';
    public char FloatingPointChar { get; set; } = '.';

    private long IncrementValue(long source, long addition, ENumberBase @base) => source * (int)@base + addition;
    public static int GetCharValue(char c) {
      if (c >= '0' && c <= '9') return c - '0';
      if (c >= 'a' && c <= 'z') return c - 'a' + 10;
      if (c >= 'A' && c <= 'Z') return c - 'A' + 10;
      return 0;
    }
    private char Lowercase(char c) => (c >= 'A' && c <= 'Z') ? (char)('a' + c - 'A') : c;
    private IEnumerable<char> GetBaseDigits(ENumberBase @base)
    {
      if (@base == ENumberBase.Invalid) return null;
      var bnum = (int)@base;
      var ret = "0123456789abcdef".Substring(0,bnum).ToList();
      for (var i = 10; i < bnum; i++)
        ret.Add((char)(ret[i] - 'a' + 'A'));
      ret.Add(SeparatorChar);
      return ret;
    }
    private void LEAD_ZERO_Enter(NumericLexTokenState state, string str, char c, int p) { state.Valid = true; state.Base = ENumberBase.Octal; }
    private void LEAD_DIGIT_Enter(NumericLexTokenState state, string str, char c, int p) { state.Valid = true; state.Parts[0] = GetCharValue(c); }
    private void BASE_SPECIFIER_Enter(NumericLexTokenState state, string str, char c, int p) { state.Valid = false; state.Base = (Lowercase(c) == 'b') ? ENumberBase.Binary : (Lowercase(c) == 'x') ? ENumberBase.Hex : ENumberBase.Invalid; }
    private void DIGIT_Enter(NumericLexTokenState state, string str, char c, int p) {
      if (c == SeparatorChar) return;
      state.Valid = true;
      state.Parts[(int)state.Part] = IncrementValue(state.Parts[(int)state.Part], GetCharValue(c), state.Base);
      if (state.Part == NumericLexTokenState.EParts.Fraction) state.FractionDigits++;
    }
    // technically sign at the start of the number can be parsed as an unary operator - however exponent still would need it so screw this
    // and making exponent (e/E) as a binary operator is weird
    private void SIGN_Enter(NumericLexTokenState state, string str, char c, int p) {
      var sign = (c == '+') ? ESign.Positive : (c == '-') ? ESign.Negative : ESign.Unknown;
      if (state.Part == NumericLexTokenState.EParts.Exponent) { state.ExpSign = sign; if (sign == ESign.Negative) state.Type = ENumberType.UnknownFloat; }
      else state.Sign = sign;
    }
    private void FRACTION_Enter(NumericLexTokenState state, string str, char c, int p) {
      state.Type = ENumberType.UnknownFloat;
      state.Base = ENumberBase.Decimal; // fixes '0.'
      state.Part = NumericLexTokenState.EParts.Fraction;
    }
    private void EXPONENT_Enter(NumericLexTokenState state, string str, char c, int p) { state.Valid = false; state.Part = NumericLexTokenState.EParts.Exponent; }
    private void SIGN_SPECIFIER_Enter(NumericLexTokenState state, string str, char c, int p) { state.Unsigned = true; }
    // could, instead of s/i/l/ll, use c/s/i/l; wouldn't use b for byte tho, for it would make parsing 0b difficult
    private void IPREC_SPECIFIER_Enter(NumericLexTokenState state, string str, char c, int p) { state.Type = (Lowercase(c) == 'i') ? ENumberType.Short : (Lowercase(c) == 's') ? ENumberType.Byte : (Lowercase(c) == 'l') ? ENumberType.Int : ENumberType.Unknown; }
    private void LLPREC_SPECIFIER_Enter(NumericLexTokenState state, string str, char c, int p) { state.Type = ENumberType.Long; }
    private void FPREC_SPECIFIER_Enter(NumericLexTokenState state, string str, char c, int p) { state.Type = (Lowercase(c) == 'f') ? ENumberType.Float : (Lowercase(c) == 'd') ? ENumberType.Double : ENumberType.UnknownFloat; }

    private IEnumerable<int> BEGIN_Transitions(NumericLexTokenState s) => new[] { (int)EState.LEAD_ZERO, (int)EState.LEAD_DIGIT, (int)EState.SIGN, (int)EState.FRACTION };
    private IEnumerable<int> LEAD_ZERO_Transitions(NumericLexTokenState s) => new[] { (int)EState.BASE_SPECIFIER, (int)EState.DIGIT, (int)EState.FRACTION, (int)EState.EXPONENT, (int)EState.SIGN_SPECIFIER, (int)EState.IPREC_SPECIFIER, (int)EState.FPREC_SPECIFIER };
    private IEnumerable<int> LEAD_DIGIT_Transitions(NumericLexTokenState s) => new[] { (int)EState.DIGIT, (int)EState.FRACTION, (int)EState.EXPONENT, (int)EState.SIGN_SPECIFIER, (int)EState.IPREC_SPECIFIER, (int)EState.FPREC_SPECIFIER };
    private IEnumerable<int> BASE_SPECIFIER_Transitions(NumericLexTokenState s) => new[] { (int)EState.DIGIT };
    private IEnumerable<int> DIGIT_Transitions(NumericLexTokenState s) {
      var ret = new List<int> { (int)EState.DIGIT };
      if (!s.Valid) return ret;
      if (s.Base == ENumberBase.Decimal)
      {
        if (s.Part < NumericLexTokenState.EParts.Exponent) ret.Add((int)EState.EXPONENT);
        if (s.Part < NumericLexTokenState.EParts.Fraction) ret.Add((int)EState.FRACTION);
        ret.Add((int)EState.FPREC_SPECIFIER);
      } if (s.Type == ENumberType.Unknown)
      {
        ret.Add((int)EState.SIGN_SPECIFIER);
        ret.Add((int)EState.IPREC_SPECIFIER);
      }
      return ret;
    }
    private IEnumerable<int> SIGN_Transitions(NumericLexTokenState s) {
      if (s.Part == NumericLexTokenState.EParts.Integral)
      {
        return new[] { (int)EState.LEAD_ZERO, (int)EState.LEAD_DIGIT, (int)EState.FRACTION };
      }
      if (s.Part == NumericLexTokenState.EParts.Exponent)
      {
        return new[] { (int)EState.DIGIT };
      }
      return null;
    }
    private IEnumerable<int> FRACTION_Transitions(NumericLexTokenState s) {
      var ret = new List<int> { (int)EState.DIGIT };
      if (s.Part < NumericLexTokenState.EParts.Exponent && s.Valid) ret.Add((int)EState.EXPONENT);
      if (s.Valid) ret.Add((int)EState.FPREC_SPECIFIER);
      return ret;
    }
    private IEnumerable<int> EXPONENT_Transitions(NumericLexTokenState s) => new[] { (int)EState.SIGN, (int)EState.DIGIT };
    private IEnumerable<int> SIGN_SPECIFIER_Transitions(NumericLexTokenState s) => s.Type <= ENumberType.UnknownFloat ? new[] { (int)EState.IPREC_SPECIFIER } : null;
    private IEnumerable<int> IPREC_SPECIFIER_Transitions(NumericLexTokenState s)
    {
      var ret = new List<int>();
      if (s.Type == ENumberType.Int) ret.Add((int)EState.LLPREC_SPECIFIER);
      if (!s.Unsigned) ret.Add((int)EState.SIGN_SPECIFIER);
      return ret.Count == 0 ? null : ret;
    }
    private IEnumerable<int> LLPREC_SPECIFIER_Transitions(NumericLexTokenState s) => !s.Unsigned ? new[] { (int)EState.SIGN_SPECIFIER } : null;
    private IEnumerable<int> FPREC_SPECIFIER_Transitions(NumericLexTokenState s) => null;

    private IEnumerable<char> LEAD_ZERO_ValidCharacters(NumericLexTokenState s) => "0".ToArray();
    private IEnumerable<char> LEAD_DIGIT_ValidCharacters(NumericLexTokenState s) => "123456789".ToArray();
    private IEnumerable<char> BASE_SPECIFIER_ValidCharacters(NumericLexTokenState s) => "BXbx".ToArray();
    private IEnumerable<char> DIGIT_ValidCharacters(NumericLexTokenState s) => GetBaseDigits(s.Base);
    private IEnumerable<char> SIGN_ValidCharacters(NumericLexTokenState s) => "+-".ToArray();
    private IEnumerable<char> FRACTION_ValidCharacters(NumericLexTokenState s) => new[] { FloatingPointChar };
    private IEnumerable<char> EXPONENT_ValidCharacters(NumericLexTokenState s) => "Ee".ToArray();
    private IEnumerable<char> SIGN_SPECIFIER_ValidCharacters(NumericLexTokenState s) => "Uu".ToArray();
    private IEnumerable<char> IPREC_SPECIFIER_ValidCharacters(NumericLexTokenState s) => "ILSils".ToArray();
    private IEnumerable<char> LLPREC_SPECIFIER_ValidCharacters(NumericLexTokenState s) => "Ll".ToArray();
    private IEnumerable<char> FPREC_SPECIFIER_ValidCharacters(NumericLexTokenState s) => "DFdf".ToArray();
  }


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
  public static class LexUtils
  {
    public static Random Random { get; set; } = new Random();
    public static int GetGlyphBytes(UInt32 g, Encoding enc)
    {
      if (enc == Encoding.UTF32) return 4;
      if (enc == Encoding.ASCII) return 1;
      if (enc == Encoding.UTF8) {
        if (g < 0x80) return 1; // ASCII characters
        if (g < 0xC0) return 0; // continuation characters
        if (g < 0xE0) return 2; // technically, 0xC0 & 0xC1 will never form valid UTF8
        if (g < 0xF0) return 3; // certain 0xE0 and 0xED sequences can form invalid UTF8 sequence
        if (g < 0xF8) return 4; // certain 0xF0 and 0xF4 sequences can form invalid UTF8 sequence, also 0xF5 and above technically will never form valid UTF8
        return 0;
      }
      if (enc == Encoding.Unicode) {
        if (g < 0xD800) return 2; // BMP
        if (g < 0xDC00) return 4; // High Surrogate
        if (g < 0xE000) return 0; // Low Surrogate
        return 2; // BMP
      }
      return 0;
    }
    public static int GetGlyphCharacters(UInt32 g, Encoding enc)
    {
      var bpg = GetGlyphBytes(g, enc);
      if (enc == Encoding.UTF32) return 1;
      if (enc == Encoding.Unicode) return bpg / 2;
      return bpg;
    }
    public static bool IsContinuation(UInt32 g, Encoding enc) => GetGlyphBytes(g, enc) == 0;
  }
  /// <summary>
  /// Also known as stateless <see cref="LexMachina"/>, this class provides decoupled functionality of the full <see cref="LexMachina"/> to perform common tasks without the burden of boilerplate necessary for other functions.<br/>
  /// Check source/documentation of its methods to learn how <see cref="LexMachina"/> works under the hood with just a limited set of functionality active at a time.
  /// </summary>
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
        if (token.Advance(tokenState, cm, c, p))
        {
          sb.Append(c);
          continue;
        }
        if (token.IsValid(tokenState, cm))
          return new LexLexeme(token, 0, p, cm, token.GetValue(tokenState, cm));
        break;
      }
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
    public bool IsInterleaved { get => (Mode & EMode.Interleaved) != EMode.None; set => Mode = (value ? (Mode & ~EMode.Interleaved) : (Mode | EMode.Interleaved)); }
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
      if ((h & EHandler.Disallow) != EHandler.None) { ResultLexemes.AddLast(new LexLexeme(null,Offset, Position, Signature.ToString() + c, null)); return true; } // having these character creates an invalid lexeme
      if ((h & EHandler.Ignore) != EHandler.None) return false; // NOTE: it is rather weird to have a globally ignored character
      if ((h & EHandler.Terminate) != EHandler.None && ActiveTokens.Count != 0) terminate = true; 

      // check current tokens
      foreach (var kv in ActiveTokens.ToArray())
      {
        var sig = kv.Key == 0 ? Signature.ToString() : Signature.ToString(kv.Key,Signature.Length - kv.Key);
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
      if (ResultLexemes.Count > 0 && (Mode == EMode.ReturnSet || (Mode == EMode.ReturnAll && ActiveTokens.Count == 0))) return true;
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

  /// <summary>
  /// Root class, contains everything necessary for parsing a specific language <br/>
  /// Pretty much the same as <see cref="LexMicroMachina"/> with all the boilerplate preassembled for given language <br/>
  /// Only one instance per language should be present <br/>
  /// NOTE: this class currently is a stub, left just for documentation purposes
  /// </summary>
  [WorkInProgress]
  public class LexMachina
  {
  }
}
