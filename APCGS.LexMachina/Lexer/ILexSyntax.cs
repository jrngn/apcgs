using APCGS.LexMachina.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APCGS.LexMachina.Lexer
{
  /// <summary>
  /// Interface governing syntax validation. <br/>
  /// Can be used to validate a list of lexemes, to independently control tokenizer or as a part of a parser context for interleaved lexing.
  /// </summary>
  public interface ILexSyntax
  {
    /// <summary>
    /// Creates new state utilized by this particular implementation of ILexSyntax.
    /// </summary>
    /// <returns>Newly created state decoupling state from functionality. Can return itself if no state split is utilized.</returns>
    object GetState();
    /// <summary>
    /// Returns list of tokens valid for current state.
    /// </summary>
    /// <param name="state">Current state of syntax validation.</param>
    /// <returns>List of tokens valid for current state.</returns>
    IEnumerable<ILexToken> GetTokens(object state);
    /// <summary>
    /// Selects the best fit from a list of lexemes. Can optionally perform additional validation. <br/>
    /// Note that returning <see langword="null"/> can lead to infinite loops if not handled correctly, <br/>
    /// so in most cases this function should strive to return something, even by creating new lexemes from the results.
    /// </summary>
    /// <param name="state">Current state of syntax validation.</param>
    /// <param name="lexemes">List of lexemes found by the tokenizer with the current config.</param>
    /// <returns>Best matching lexeme.</returns>
    LexLexeme SelectLexeme(object state, IEnumerable<LexLexeme> lexemes);
    /// <summary>
    /// Handles selected lexeme, primarily altering the syntax state. <br/>
    /// </summary>
    /// <param name="state">Current state of syntax validation.</param>
    /// <param name="lexeme">Lexeme used. This lexeme should be already valid for current state.</param>
    void HandleLexeme(object state, LexLexeme lexeme);
    /// <summary>
    /// Alters the tokenizer settings, in particular its mode and mapping table, but not the list of lexemes. <br/>
    /// Invoked once per token found, so further modifications are possible for more complex scenarios.
    /// </summary>
    /// <param name="state">Current state of syntax validation.</param>
    /// <param name="tokenizer">Tokenizer to alter.</param>
    void SetupTokenizer(object state, LexTokenizer tokenizer);

    /// <summary>
    /// Validates the token against current state. Primarily used for validating pretokenized streams.
    /// </summary>
    /// <param name="state">Current state of syntax validation.</param>
    /// <param name="lexeme">Lexeme to validate.</param>
    /// <returns><see langword="true"/> if passed lexeme is valid for current state.</returns>
    bool IsValid(object state, LexLexeme lexeme);
  }

  /// <inheritdoc cref="ILexSyntax"/>
  /// <typeparam name="TState">State type utilized</typeparam>
  public interface ILexSyntax<TState> : ILexSyntax
  {
    /// <inheritdoc cref="ILexSyntax.GetState"/>
    TState GetTState();
    /// <inheritdoc cref="ILexSyntax.GetTokens(object)"/>
    IEnumerable<ILexToken> GetTokens(TState state);
    /// <inheritdoc cref="ILexSyntax.SelectLexeme(object, IEnumerable{LexLexeme})"/>
    LexLexeme SelectLexeme(TState state, IEnumerable<LexLexeme> lexemes);
    /// <inheritdoc cref="ILexSyntax.HandleLexeme(object, LexLexeme)"/>
    void HandleLexeme(TState state, LexLexeme lexeme);
    /// <inheritdoc cref="ILexSyntax.SetupTokenizer(object, LexTokenizer)"/>
    void SetupTokenizer(TState state, LexTokenizer tokenizer);
    /// <inheritdoc cref="ILexSyntax.IsValid(object, LexLexeme)"/>
    bool IsValid(TState state, LexLexeme lexeme);
  }
}
