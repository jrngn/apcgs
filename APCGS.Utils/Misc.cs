using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace APCGS.Utils
{
  public static class Sizeof<T> where T: struct
  {
    private static int? size;
    public static int Get()
    {
      size = size ?? Marshal.SizeOf(default(T));
      return size.Value;
    }
  }
}
