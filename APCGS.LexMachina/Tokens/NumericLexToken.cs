using APCGS.Utils.Refactor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace APCGS.LexMachina.Tokens
{
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

  [Redesign(Reason = "that's a biiig token - can it be simplified? could also separate each state into unique SMLexTokenBehaviour")]
  [Redesign(Reason = "NaN and infinity are currently not supported")]
  [Questionable(Reason = "usigned negative behaviour can be misleading - see GetValue()")]
  [Questionable(Reason = "64-bit integer precision is excluded from automatic precision inferrence - see GetValue()")]
  [WorkInProgress(Reason = "serialization is missing")]
  [NeedsDocumentation(Reason = "every state could have a short description of their behavior - also docs just for which forms are supported")]
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
    public NumericLexToken()
    {
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
        if (state.Part >= NumericLexTokenState.EParts.Exponent) v *= (long)Math.Pow(10, state.Parts[epart]);
        if (state.Type == ENumberType.Unknown)
        {
          state.Type = ENumberType.Byte;
          if (v > (state.Unsigned ? (int)byte.MaxValue : sbyte.MaxValue)) state.Type = ENumberType.Short;
          if (v > (state.Unsigned ? (int)ushort.MaxValue : short.MaxValue)) state.Type = ENumberType.Int;
          // disallow unspecified 64-bit integers?
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
      else
      {
        double v = state.Parts[ipart];
        if (state.FractionDigits > 0) v += state.Parts[fpart] / Math.Pow(10, state.FractionDigits);
        if (state.Part >= NumericLexTokenState.EParts.Exponent)
        {
          var exp = Math.Pow(10, state.Parts[epart]);
          if (state.ExpSign == ESign.Negative && exp > 0) v = v / exp;
          else v = v * exp;
        }
        if (state.Sign == ESign.Negative) v = -v;
        switch (state.Type)
        {
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
    public static int GetCharValue(char c)
    {
      if (c >= '0' && c <= '9') return c - '0';
      if (c >= 'a' && c <= 'z') return c - 'a' + 10;
      if (c >= 'A' && c <= 'Z') return c - 'A' + 10;
      return 0;
    }
    private char Lowercase(char c) => c >= 'A' && c <= 'Z' ? (char)('a' + c - 'A') : c;
    private IEnumerable<char> GetBaseDigits(ENumberBase @base)
    {
      if (@base == ENumberBase.Invalid) return null;
      var bnum = (int)@base;
      var ret = "0123456789abcdef".Substring(0, bnum).ToList();
      for (var i = 10; i < bnum; i++)
        ret.Add((char)(ret[i] - 'a' + 'A'));
      ret.Add(SeparatorChar);
      return ret;
    }
    private void LEAD_ZERO_Enter(NumericLexTokenState state, string str, char c, int p) { state.Valid = true; state.Base = ENumberBase.Octal; }
    private void LEAD_DIGIT_Enter(NumericLexTokenState state, string str, char c, int p) { state.Valid = true; state.Parts[0] = GetCharValue(c); }
    private void BASE_SPECIFIER_Enter(NumericLexTokenState state, string str, char c, int p) { state.Valid = false; state.Base = Lowercase(c) == 'b' ? ENumberBase.Binary : Lowercase(c) == 'x' ? ENumberBase.Hex : ENumberBase.Invalid; }
    private void DIGIT_Enter(NumericLexTokenState state, string str, char c, int p)
    {
      if (c == SeparatorChar) return;
      state.Valid = true;
      state.Parts[(int)state.Part] = IncrementValue(state.Parts[(int)state.Part], GetCharValue(c), state.Base);
      if (state.Part == NumericLexTokenState.EParts.Fraction) state.FractionDigits++;
    }
    // technically sign at the start of the number can be parsed as an unary operator - however exponent still would need it so screw this
    // and making exponent (e/E) as a binary operator is weird
    private void SIGN_Enter(NumericLexTokenState state, string str, char c, int p)
    {
      var sign = c == '+' ? ESign.Positive : c == '-' ? ESign.Negative : ESign.Unknown;
      if (state.Part == NumericLexTokenState.EParts.Exponent) { state.ExpSign = sign; if (sign == ESign.Negative) state.Type = ENumberType.UnknownFloat; }
      else state.Sign = sign;
    }
    private void FRACTION_Enter(NumericLexTokenState state, string str, char c, int p)
    {
      state.Type = ENumberType.UnknownFloat;
      state.Base = ENumberBase.Decimal; // fixes '0.'
      state.Part = NumericLexTokenState.EParts.Fraction;
    }
    private void EXPONENT_Enter(NumericLexTokenState state, string str, char c, int p) { state.Valid = false; state.Part = NumericLexTokenState.EParts.Exponent; }
    private void SIGN_SPECIFIER_Enter(NumericLexTokenState state, string str, char c, int p) { state.Unsigned = true; }
    // could, instead of s/i/l/ll, use c/s/i/l; wouldn't use b for byte tho, for it would make parsing 0b difficult
    private void IPREC_SPECIFIER_Enter(NumericLexTokenState state, string str, char c, int p) { state.Type = Lowercase(c) == 'i' ? ENumberType.Short : Lowercase(c) == 's' ? ENumberType.Byte : Lowercase(c) == 'l' ? ENumberType.Int : ENumberType.Unknown; }
    private void LLPREC_SPECIFIER_Enter(NumericLexTokenState state, string str, char c, int p) { state.Type = ENumberType.Long; }
    private void FPREC_SPECIFIER_Enter(NumericLexTokenState state, string str, char c, int p) { state.Type = Lowercase(c) == 'f' ? ENumberType.Float : Lowercase(c) == 'd' ? ENumberType.Double : ENumberType.UnknownFloat; }

    private IEnumerable<int> BEGIN_Transitions(NumericLexTokenState s) => new[] { (int)EState.LEAD_ZERO, (int)EState.LEAD_DIGIT, (int)EState.SIGN, (int)EState.FRACTION };
    private IEnumerable<int> LEAD_ZERO_Transitions(NumericLexTokenState s) => new[] { (int)EState.BASE_SPECIFIER, (int)EState.DIGIT, (int)EState.FRACTION, (int)EState.EXPONENT, (int)EState.SIGN_SPECIFIER, (int)EState.IPREC_SPECIFIER, (int)EState.FPREC_SPECIFIER };
    private IEnumerable<int> LEAD_DIGIT_Transitions(NumericLexTokenState s) => new[] { (int)EState.DIGIT, (int)EState.FRACTION, (int)EState.EXPONENT, (int)EState.SIGN_SPECIFIER, (int)EState.IPREC_SPECIFIER, (int)EState.FPREC_SPECIFIER };
    private IEnumerable<int> BASE_SPECIFIER_Transitions(NumericLexTokenState s) => new[] { (int)EState.DIGIT };
    private IEnumerable<int> DIGIT_Transitions(NumericLexTokenState s)
    {
      var ret = new List<int> { (int)EState.DIGIT };
      if (!s.Valid) return ret;
      if (s.Base == ENumberBase.Decimal)
      {
        if (s.Part < NumericLexTokenState.EParts.Exponent) ret.Add((int)EState.EXPONENT);
        if (s.Part < NumericLexTokenState.EParts.Fraction) ret.Add((int)EState.FRACTION);
        ret.Add((int)EState.FPREC_SPECIFIER);
      }
      if (s.Type == ENumberType.Unknown)
      {
        ret.Add((int)EState.SIGN_SPECIFIER);
        ret.Add((int)EState.IPREC_SPECIFIER);
      }
      return ret;
    }
    private IEnumerable<int> SIGN_Transitions(NumericLexTokenState s)
    {
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
    private IEnumerable<int> FRACTION_Transitions(NumericLexTokenState s)
    {
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
}
