using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ESystem.Asserting
{
  public class EAssert
  {
    public class Argument
    {
      public static void IsNotNull(object value, string argumentName = null)
      {
        if (argumentName == null)
          EAssert.IsNotNull(value, "Argument is null.");
        else
          EAssert.IsNotNull(value, $"Argument '{argumentName}' is null.");
      }

      public static void IsTrue(bool value, string argumentName = null)
      {
        if (argumentName == null)
          EAssert.IsTrue(value, "Argument condition check to true failed.");
        else
          EAssert.IsTrue(value, $"Argument '{argumentName}' condition check to true failed.");
      }

      public static void IsNonEmptyString( string value, string argumentName = null)
      {
        if (argumentName == null)
          EAssert.IsNonEmptyString(value, "Argument condition check to true failed.");
        else
          EAssert.IsNonEmptyString(value, $"Argument '{argumentName}' condition check to true failed.");
      }
    }

    public static void IsNonEmptyString( string value, string message = "String is empty or null.")
    {
      if (string.IsNullOrEmpty(value))
        throw new EAssertException(message);
    }

    public static void IsNotNull( object value, string message = "Value is null.")
    {
      if (value == null)
        throw new EAssertException(message);
    }

    public static void IsTrue(bool value, string message = "Value must be true.")
    {
      if (value != true)
        throw new EAssertException(message);
    }

    public static void IsFalse(bool value, string message = "Value must be false.")
    {
      if (value != false)
        throw new EAssertException(message);
    }
  }

}
