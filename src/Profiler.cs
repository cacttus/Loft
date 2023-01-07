using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Loft
{
  //class MethodProf

  public class Profiler
  {
    //using var __MP = new MethodProf(); 
    //Gu.Prof()
    //Gu.Prof()

    //we wont need to get StackTrace if we know we only create this one time
    // we currently delete it..
    public class FrameProf
    {
      public string _label = "";
      public long _elapsed = 0;
      public long _delta = 0;
      public string _method = ""; //MethodBase? _method;
      public ulong _key = 0;
      public int _depth;
    }

    public bool Enabled { get { return _enabled && _rv_visible; } set { _enabled = value; } }
    private List<FrameProf> _last = new List<FrameProf>();
    private List<FrameProf> _cur = new List<FrameProf>();
    private bool _rv_visible = false;
    private bool _enabled = true;
    private long _time = 0;
    private long _msLast = 0;
    private long _prevMS = 0;
    public Profiler()
    {
    }
    public void Section(string label, [CallerMemberName] string memberName = "")// int skiptop = 2)
    {
      if (!Enabled) { return; }

      //The getting file/line in stacktrace is
      int skiptop = 3;
      var st = new StackTrace();//this will only work if we have file/line
      //var frames = st.GetFrames();
      //Gu.Assert(skiptop >= 0 && skiptop < frames.Length);

      //ulong key = GetFrameKey(frames, skiptop);

      var elapsed = Gu.Microseconds() - _time;
      _cur.Add(new FrameProf()
      {
        _label = label,
        _elapsed = elapsed,
        _delta = elapsed - _prevMS,
        _depth = st.FrameCount - skiptop,
        _method = memberName// frames[frames.Length-skiptop].GetMethod()
        //_key = key,
      });

      _prevMS = elapsed;
    }
    public void Reset(bool clearTree = false)
    {
      CheckInfoVisible();
      if (!Enabled) { return; }

      _time = Gu.Microseconds();
      _msLast = _prevMS;
      _prevMS = 0;
      _last = new List<FrameProf>(_cur);
      _cur.Clear();
    }
    public string ToString(string indent = "  ")
    {
      StringBuilder sb = new StringBuilder();
      double lastms = ((double)_msLast / 1000.0);
      double pcttot = 0;

      foreach (var pt in _last)
      {
        var indt = StringUtil.Indent(indent, pt._depth);

        var methodname = pt._method;// pt._method.ReflectedType.Name + "." + pt._method.Name;
        var label = StringUtil.IsEmpty(pt._label) ? methodname : pt._label;

        double delms = ((double)pt._delta / 1000.0);
        var dms = StringUtil.FormatPrec(delms, 2);
        var pct = delms / lastms * 100;
        pcttot += pct;
        var spct = StringUtil.FormatPrec(pct, 1);
        if (pct >= 10)
        {
          sb.AppendLine($"{indt}{UiTextColor.TextRed}[{label}]:{dms}ms ({spct}%){UiTextColor.TextReset}");
        }
         else       if (pct >= 5)
        {
          sb.AppendLine($"{indt}{UiTextColor.TextDarkYellow}[{label}]:{dms}ms ({spct}%){UiTextColor.TextReset}");
        }
        else
        {
          sb.AppendLine($"{indt}[{label}]:{dms}ms ({spct}%)");
        }
      }
      sb.AppendLine($"Frame:({StringUtil.FormatPrec(lastms, 2)}ms) (tot={StringUtil.FormatPrec(pcttot, 1)})");
      return sb.ToString();
    }
    private void CheckInfoVisible()
    {
      //optimization to not do this junk when its not showing
      _rv_visible = false;
      foreach (var rv in Gu.Context.GameWindow.RenderViews)
      {
        if (rv != null && rv.ProfInfo != null && rv.ProfInfo.Visible == true)
        {
          _rv_visible = true;
          break;
        }
      }
    }
    private ulong GetFrameKey(StackFrame[]? frames, int skiptop)
    {
      //make unique key based on function call frame
      ulong prime = 1099511628211;//4401858765275463419;
      ulong basis = 14695981039346656037;//1729532795367666019;
      ulong key = basis;
      unchecked
      {
        List<int> fl = new List<int>();
        for (int fi = frames.Length - 1; fi >= skiptop; fi--)
        {
          ulong l = (ulong)frames[fi].GetFileLineNumber();
          ulong c = (ulong)frames[fi].GetFileColumnNumber();
          ulong f = (ulong)frames[fi].GetFileName().GetHashCode();

          key = (key * prime) ^ l;
          key = (key * prime) ^ c;
          key = (key * prime) ^ f;
        }
      }
      return key;
    }


  }//cls
  
}