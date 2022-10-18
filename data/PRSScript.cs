using System;
using System.Collections.Generic;
using PirateCraft;

namespace PirateCraft
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