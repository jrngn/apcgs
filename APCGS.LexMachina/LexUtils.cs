using APCGS.Utils.Refactor;
using System;
using System.Text;

namespace APCGS.LexMachina
{
  [Redesign(Reason = "could move to APCGS.Utils, as UTF utility class")]
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
}
