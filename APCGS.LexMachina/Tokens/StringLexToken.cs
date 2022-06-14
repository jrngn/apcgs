namespace APCGS.LexMachina.Tokens
{
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
}
