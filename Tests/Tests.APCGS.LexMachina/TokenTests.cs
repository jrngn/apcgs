using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using APCGS.LexMachina;
using System.Collections.Generic;
using APCGS.LexMachina.Tokens;

namespace Tests.APCGS.LexMachina
{
  [TestClass]
  public class TokenTests
  {
    [TestMethod]
    public void Token_TokenGeneration()
    {
      LexUtils.Random = new Random(1337);
      var numToken = new NumericLexToken();
      //var ret = new List<string>();
      for (int i = 0; i < 100; i++)
      {
        //ret.Add(LexMicroMachina.GenerateToken(numToken));
        var lex = LexMicroMachina.GenerateToken(numToken);
        Console.WriteLine($"({(lex.token != null ? "valid" : "invalid")}) {lex.contents} : {lex.value} ({lex.value.GetType()})");
      }
    }
    struct Token_TokenExtraction_TestCase
    {
      public string src;
      public Type type;
      public double approx;
      public long value;
    };
    [TestMethod]
    public void Token_TokenExtraction()
    {
      var i_testCases = new Token_TokenExtraction_TestCase[]{
        new Token_TokenExtraction_TestCase { src = "0X2f6Su", type = typeof(byte), value = 246 },
        new Token_TokenExtraction_TestCase { src = "-06UI", type = typeof(UInt16), value = 65530 },
        new Token_TokenExtraction_TestCase { src = "-5u", type = typeof(byte), value = 251 },
        new Token_TokenExtraction_TestCase { src = "0Xe", type = typeof(sbyte), value = 14 },
        new Token_TokenExtraction_TestCase { src = "1ULl", type = typeof(UInt64), value = 1 },
        new Token_TokenExtraction_TestCase { src = "+0251Su", type = typeof(byte), value = 169 },
        new Token_TokenExtraction_TestCase { src = "6e+'9", type = typeof(Int32), value = 1705032704 },
        new Token_TokenExtraction_TestCase { src = "-0b''11", type = typeof(sbyte), value = -3 },
      };
      var f_testCases = new Token_TokenExtraction_TestCase[]{
        new Token_TokenExtraction_TestCase { src = ".3e+4d", type = typeof(double), approx = 3000 },
      };

      var numToken = new NumericLexToken();
      foreach (var tc in i_testCases)
      {
        var lex = LexMicroMachina.ExtractToken(numToken,tc.src);
        if (lex.value != null)
        {
          var value = Convert.ToInt64(lex.value);
          Assert.IsTrue(lex.contents == tc.src);
          Assert.IsTrue(value == tc.value);
          Assert.IsTrue(lex.value.GetType() == tc.type);
        }
        else Assert.Fail($"Failed to parse \"{tc.src}\"");
      }
      foreach (var tc in f_testCases)
      {
        var lex = LexMicroMachina.ExtractToken(numToken, tc.src);
        if (lex.value != null)
        {
          var value = Convert.ToDouble(lex.value);
          Assert.IsTrue(lex.contents == tc.src);
          Assert.IsTrue(Math.Abs(value - tc.approx) < 0.0001);
          Assert.IsTrue(lex.value.GetType() == tc.type);
        }
        else Assert.Fail($"Failed to parse \"{tc.src}\"");
      }
    }
  }
}
