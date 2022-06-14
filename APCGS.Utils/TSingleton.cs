using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APCGS.Utils
{
  public static class TSingletonFactory<T>
  {
    private static T _instance;
    private static Func<T> _factory;
    public static void SetFactory(Func<T> factory) { _factory = factory; }
    public static T Instance { get { if (Equals(_instance,default(T)) && _factory != null) _instance = _factory(); return _instance; } }
  }
  public static class TSingleton<T>
    where T:new()
  {
    private static T _instance;
    public static T Instance { get { if (Equals(_instance, default(T))) _instance = new T(); return _instance; } }
  }
}
