namespace APCGS.LexMachina.Tokens
{
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
}
