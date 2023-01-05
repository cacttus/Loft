
using System.Collections;
using System.Collections.Generic;

namespace Loft
{
  public class BaseGuiScript : IUIScript
  {
    //Thunk class for test/or main guio
    private IUIScript? _script = null;
    private IUIScript Script
    {
      get
      { 
        if (_script == null)
        { 
          // _script = new EditGuiScript();
          _script = new TestGuiScript();
        }
        return _script;
      }
    }

    public BaseGuiScript() { }
    public string GetName() { return Script.GetName(); }
    public List<FileLoc> GetResources() { return Script.GetResources(); }
    public void OnUpdate(RenderView rv) { Script.OnUpdate(rv); }
    public void OnCreate(Gui2d g) { Script.OnCreate(g); }

  }


}//ns