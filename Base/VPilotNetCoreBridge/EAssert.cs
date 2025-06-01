using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPilotNetCoreBridge
{
  internal class EAssert
  {
    public static void IsTrue(bool condition, string message)
    {
      if (!condition)
      {
        throw new InvalidOperationException($"Assertion failed: {message}");
      }
    }
  }
}
