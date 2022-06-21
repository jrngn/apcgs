using APCGS.Utils.Refactor;
using APCGS.Utils.Registry;
using System.Collections.Generic;

namespace APCGS.GuiGee.Measurements
{
  // NOTE: what about var(), scoping and MUL|DIV support?
  // might as well just use LexMachina lexer and implement fully-featured arithmetic parser, eh?
  // if only we had LexMachina refactored... nyeeeeehh

  [NeedsDocumentation]
  [Cleanup]
  [Redesign(Reason = "replace with LexMachina parser")]
  public static class MeasurementParser
  {
    enum ELexemeType
    {
      Sign,
      Number,
      Unit
    }
    class Lexeme { public ELexemeType type; }
    class SignLexeme : Lexeme { public bool positive; }
    class NumberLexeme : Lexeme { public float value; }
    class UnitLexeme : Lexeme { public MeasurementUnit unit; }
    class MUPair { public MeasurementUnit unit; public float value; }

    static IEnumerable<Lexeme> Lex(string source)
    {
      throw new System.NotImplementedException();
      if (source == null) return null;
      int pos = -1;
      char c;
      var ret = new List<Lexeme>();
      while(pos < source.Length)
      {
        pos++;
        c = source[pos];
        if (c <= ' ') continue; // this also includes tabs and other garbage characters
        if (c == '+' || c == '-') continue; // this is a sign
        if (c >= '0' && c <= '9') continue; // this must be a number
        // this must be a unit
      }
      return ret;
    }

  }

  [WorkInProgress]
  /// <summary>
  /// Singleton class, storing all defined measurement units (working as dynamic enum)
  /// </summary>
  public class RMeasurementUnits : Registry<MeasurementUnit>
  {
    private RMeasurementUnits() { _initialize(); }
    internal static RMeasurementUnits _instance = new RMeasurementUnits(); // any static access will initialize its data

    internal static MeasurementUnit _pixel = new MeasurementUnit("Pixel", (p) => 1, "px");

    internal static MeasurementUnit _em;
    internal static MeasurementUnit _rootEm;

    internal static MeasurementUnit _inch;
    internal static MeasurementUnit _cm;
    internal static MeasurementUnit _mm;
    internal static MeasurementUnit _point;
    internal static MeasurementUnit _pica;

    internal static MeasurementUnit _row;
    internal static MeasurementUnit _column;
    internal static MeasurementUnit _viewportWidth;
    internal static MeasurementUnit _viewportHeight;
    internal static MeasurementUnit _parentWidth;
    internal static MeasurementUnit _parentHeight;
    private void _initialize() {
      //Register(_pixel);
      //Register(_em);
      //Register(_rootEm);
      //Register(_inch);
      //Register(_cm);
      //Register(_mm);
      //Register(_point);
      //Register(_pica);
      //Register(_row);
      //Register(_column);
      //Register(_viewportWidth);
      //Register(_viewportHeight);
      //Register(_parentWidth);
      //Register(_parentHeight);
    }
    public static RMeasurementUnits Instance { get { if (_instance == null) _instance = new RMeasurementUnits(); return _instance; } }
  }

  public delegate double MeasurementConverter(IPositioning positionable);
  [Redesign(Reason = "currently uses hax, see above for a bit more manageable code (since it utilizes static behavior already, a singleton/static class is more suited for the task)")]
  [Redesign(Reason = "MeasurementConverter doesn't have a standarized access to a MUStore/MUConfig")]
  [Cleanup]
  public class MeasurementUnit : IRegistryEntry
  {
    private static int _id = -1;
    public static MeasurementUnit Units { get; internal set; } = new MeasurementUnit("Units", (p) => 1, "u", ++_id);
    public static MeasurementUnit Pixels { get; internal set; } = new MeasurementUnit("Pixels", (p) => 1, "px", ++_id);
    public static MeasurementUnit Rows { get; internal set; } = new MeasurementUnit("Rows", (p) => 1, " rows", ++_id);
    public static MeasurementUnit Columns { get; internal set; } = new MeasurementUnit("Columns", (p) => 1, " cols", ++_id);
    public static MeasurementUnit Percentage { get; internal set; } = new MeasurementUnit("Percentage", (p) => 1, "%", ++_id);
    public static MeasurementUnit WidthPercentage { get; internal set; } = new MeasurementUnit("WidthPercentage", (p) => (p.Boundaries.Width ?? 0) / 100d, "w%", ++_id);
    public static MeasurementUnit HeightPercentage { get; internal set; } = new MeasurementUnit("HeightPercentage", (p) => (p.Boundaries.Height ?? 0) / 100d, "h%", ++_id);
    // static hax -> as soon as this class is referenced in any shape or form, all of its static properties will be initialized, causing the above fields to receive IDs
    // a tad jank tbh, so instead extended Registry<> to allow registration based on already defined IDs
    // public static Registry<string, MeasurementUnit> Defaults { get; internal set; } = RegisterDefaults(new Registry<string, MeasurementUnit>());
    public static Registry<MeasurementUnit> RegisterDefaults(Registry<MeasurementUnit> reg)
    {
      reg.Register(Units);
      reg.Register(Pixels);
      reg.Register(Rows);
      reg.Register(Columns);
      reg.Register(Percentage);
      reg.Register(WidthPercentage);
      reg.Register(HeightPercentage);
      return reg;
    }

    public string Key { get; protected set; }
    public string Suffix { get; protected set; }
    public int Id { get; set; }
    public MeasurementConverter Converter { get; protected set; }
    public MeasurementUnit(string key, MeasurementConverter converter, string suffix = null, int id = -1)
    {
      Key = key;
      Converter = converter;
      Suffix = suffix;
      Id = id;
    }
    public override string ToString() => $"{Suffix??Key}";
  }
  [Redesign(Reason = "there should only be one store, but multiple configs")]
  public class MeasurementUnitStore : Registry<MeasurementUnit>
  {
    public MeasurementUnitStore Parent { get; set; }
    public MeasurementUnitStore(): base()
    {
      Default = new MeasurementUnit(null,(p) => 1);
      MeasurementUnit.RegisterDefaults(this);
    }

    public new MeasurementUnit this[string key] { get { return RegisteredByKey.ContainsKey(key) ? RegisteredByKey[key] : Parent?[key] ?? Default; } }
    public new MeasurementUnit this[int id] { get { return RegisteredById.ContainsKey(id) ? RegisteredById[id] : Parent?[id] ?? Default; } }

  }
}
