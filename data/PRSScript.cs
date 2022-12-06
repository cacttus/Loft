using System;
using System.Collections.Generic;
using Loft;

namespace Loft
{
  public class DoTheThing : IFunctionScript
  {
    public object? DoThing(object? param)
    {
      if (param != null)
      {
        return (object?)mat4.Identity;
      }
      return null;
    }
  }
}