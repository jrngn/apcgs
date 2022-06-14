namespace APCGS.LexMachina.Tokens
{
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
}
