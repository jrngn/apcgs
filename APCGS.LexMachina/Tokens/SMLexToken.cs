using APCGS.Utils.Refactor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace APCGS.LexMachina.Tokens
{

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
    where TState : ISMLexTokenState
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
  public abstract class SMLexToken<TState> : ILexTokenGenerator
    where TState : ISMLexTokenState
  {
    /// <summary>
    /// Array of all states utilized by this token
    /// </summary>
    public ISMLexTokenBehaviour<TState>[] States { get; set; }

    public bool IsValid(object state, string str) => IsValid(state is TState tstate ? tstate : default, str);
    public bool Advance(object state, string str, char c, int p) => Advance(state is TState tstate ? tstate : default, str, c, p);
    public object GetTokenState(object globalState = null) => GetTTokenState(globalState);
    public object GetValue(object state, string str) => GetValue(state is TState tstate ? tstate : default, str);
    public char? Generate(object state, string str, int p) => Generate(state is TState tstate ? tstate : default, str, p);

    public abstract string Serialize(object value);


    /// <param name="state">State of tokenization, including current state id of the state machine</param>
    /// <inheritdoc cref="IsValid(object, string)"/>
    public virtual bool IsValid(TState state, string str) => state.Valid;

    /// <param name="state">State of tokenization, including current state id of the state machine</param>
    /// <inheritdoc cref="Advance(object, string, char, int)"/>
    public virtual bool Advance(TState state, string str, char c, int p)
    {
      var cstate = States[state.Id];
      var transitions = cstate.GetTransitions(state);
      if (transitions != null)
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
}
