namespace APCGS.LexMachina.Tokens
{
  /// <summary>
  /// Stateless token used for extracting keynames <br/>
  /// same as <see cref="System.Text.RegularExpressions.Regex(string)"/> with "[_a-zA-Z]\\w*" as pattern
  /// </summary>
  public class KeynameLexToken : ILexTokenGenerator
  {
    public static KeynameLexToken Instance { get; } = new KeynameLexToken();
    public bool IsValid(object state, string str) => true;
    public bool Advance(object state, string str, char c, int p) => c == '_' || char.IsLetter(c) || p > 0 && char.IsDigit(c);
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
}
