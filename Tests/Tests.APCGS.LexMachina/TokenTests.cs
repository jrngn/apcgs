using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using APCGS.LexMachina;
using System.Collections.Generic;

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
  }
}
