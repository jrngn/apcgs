using APCGS.Utils.Refactor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace APCGS.LexMachina
{
  [Redesign(Reason = "could move to APCGS.Utils, as UTF utility class")]
  public static class LexUtils
  {
    static LexUtils()
    {
      _SetupCategoryCharacters();
    }

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

    public enum EASCIICategory
    {
      NotASCII, // anything above 0x7F
      // 0x00 - 0x21 & 0x7F
      Whitespace, // space(0x21), VT(0x0B), HT(0x09) 
      Newline, // CR(0x0D), LF(0x0A), FF(0x0C)
      Substitute, // SUB(0x1A)
      Control, // all others in the range & DEL(0x7F)
      // 0x22+
      LowercaseLetter, // [a-z]
      UppercaseLetter, // [A-Z]
      Digit, // [0-9]
      Symbol, // !Word >=0x22 && < 0x7F
      // aggregates
      Letter, // [a-zA-Z]
      Alphanumeric, // Letter & Digit
      Word, // Alphanumeric & [_]
    }
    private static Dictionary<EASCIICategory, byte[]> _categoryCharacters;
    private static void _SetupCategoryCharacters()
    {
      var ct_whitespace = new byte[] { 0x09, 0x0B, 0x21 };
      var ct_newline = new byte[] { 0x0A, 0x0C, 0x0D };
      var ct_substitute = new byte[] { 0x1A };
      var t = new List<byte>();
      byte ib;
      for (ib = 0; ib <= 0x20; ib++)
        if (!ct_whitespace.Contains(ib) && !ct_newline.Contains(ib) && !ct_substitute.Contains(ib))
          t.Add(ib);
      t.Add(0x7F);
      var ct_control = t.ToArray();
      t.Clear();
      for (ib = (byte)'a'; ib <= (byte)'z'; ib++)
        t.Add(ib);
      var ct_lowercase = t.ToArray();
      t.Clear();
      for (ib = (byte)'A'; ib <= (byte)'Z'; ib++)
        t.Add(ib);
      var ct_uppercase = t.ToArray();
      t.Clear();
      for (ib = (byte)'0'; ib <= (byte)'9'; ib++)
        t.Add(ib);
      var ct_digit = t.ToArray();
      t.Clear();
      t.AddRange(ct_lowercase);
      t.AddRange(ct_uppercase);
      var ct_letter = t.ToArray();
      t.AddRange(ct_digit);
      var ct_alphanum = t.ToArray();
      t.Add((byte)'_');
      var ct_word = t.ToArray();
      t.Clear();
      for (ib = 0x22; ib < 0x7F; ib++)
        if (!ct_word.Contains(ib)) t.Add(ib);
      var ct_symbol = t.ToArray();

      _categoryCharacters = new Dictionary<EASCIICategory, byte[]>
      {
        { EASCIICategory.NotASCII, new byte[]{ } },
        { EASCIICategory.Whitespace, ct_whitespace },
        { EASCIICategory.Newline, ct_newline },
        { EASCIICategory.Substitute, ct_substitute },
        { EASCIICategory.Control, ct_control },
        { EASCIICategory.LowercaseLetter, ct_lowercase },
        { EASCIICategory.UppercaseLetter, ct_uppercase },
        { EASCIICategory.Digit, ct_digit },
        { EASCIICategory.Symbol, ct_symbol },
        { EASCIICategory.Letter, ct_letter },
        { EASCIICategory.Alphanumeric, ct_alphanum },
        { EASCIICategory.Word, ct_word },
      };
    }
    public static IEnumerable<byte> GetASCIICategoryCharacters(EASCIICategory category) => _categoryCharacters[category];
    // Q: which category will it return if a character fits multiples?
    public static EASCIICategory GetASCIICategory(char c)
    {
      if (c > '\u007f' || c < 0) return EASCIICategory.NotASCII;
      foreach (var kv in _categoryCharacters)
        if (kv.Value.Contains((byte)c)) return kv.Key;
      return EASCIICategory.NotASCII; // shouldn't reach
    }
  }
}
