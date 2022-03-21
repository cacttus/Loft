using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PirateCraft
{
  /*
   * Generally speaking i think we can add the framework for the same element system as UIControls
   * I mean, we will, in the future need some kind of ui, however, it doesn't need to be nuts, like UIControls.
   *  -- i.e. -- no "DimUnit" or "DisplayMode" or "PositionMode" X X X done..
   *  fixed size . Fixed position . Easy easy easy.
   *  no "glyphs" man that was dumb. the text was so slow, in c++ no less
   *  
   * float pad_top
   * float pad_right
   * float pad_bot
   * float pad_left
   * float width
   * float height
   * Box2f texs
   * etc
   * 
   */

  //Oh brother. Please no. Don't do this. Don't do this. Don't do this.
  public class UIElement
  {
    float Pad_Top=0;
    float Pad_Right = 0;
    float Pad_Bot = 0;
    float Pad_Left = 0;
    float Width = 0;
    float Height = 0;
    Box2f Texs;
  }
  public class UILabel
  {

  }
  public class Font
  {
    public FileLoc FileLoc { get; private set; }
    //font size -> list of glyphs based on char code.
    public MtFont MtFont { get; } = null;
    private Dictionary<float, Dictionary<char, v_v4v4v4v2u2>> Glyphs = new Dictionary<float, Dictionary<char, v_v4v4v4v2u2>>();
    public Font(FileLoc loc, MtFont f)
    {
      FileLoc = loc;
      MtFont = f;
    }
  }
  public class Gui : WorldObject
  {
    private const int MaxGuiVerts = 2048;
    private v_v4v4v4v2u2[] _verts = new v_v4v4v4v2u2[MaxGuiVerts];
    private int _numVerts = 0;
    private List<Font> _fonts = new List<Font>();
    private Shader _shader = null;
    private MegaTex _megaTex = null;
    private Font _activeFont = null;

    public FileLoc Fancy = new FileLoc("Parisienne-Regular.ttf", FileStorage.Embedded);
    public FileLoc Pixel = new FileLoc("PressStart2P-Regular.ttf", FileStorage.Embedded);
    public FileLoc Pixel2 = new FileLoc("kenpixel.ttf", FileStorage.Embedded);
    public FileLoc Pixel3 = new FileLoc("visitor1.ttf", FileStorage.Embedded);
    public FileLoc Minecraft = new FileLoc("Minecraftia-Regular.ttf", FileStorage.Embedded);
    public FileLoc Entypo_Symbols = new FileLoc("Entypo.ttf", FileStorage.Embedded);
    public FileLoc FontAwesome_Symbols = new FileLoc("fontawesome.ttf", FileStorage.Embedded);

    public Gui() : base("gui")
    {
      _megaTex = new MegaTex("gui_megatex", true);
    }
    public void SetFont(Font f)
    {
      Gu.Assert(f != null);
      _activeFont = f;
    }
    public Font LoadFont(FileLoc loc)
    {
      var ret = new Font(loc, _megaTex.getFont(loc));
      _fonts.Add(ret);
      if (_activeFont == null)
      {
        _activeFont = ret;
      }
      return ret;
    }
    private void Build()
    {
      _megaTex.loadImages();
      MegaTex.CompiledTextures tx = _megaTex.compile(true);
      if (tx != null)
      {
        _shader = Gu.Resources.LoadShader("v_Gui", true, FileStorage.Embedded);
        Material = new Material(_shader);
        Material.GpuRenderState.DepthTest = false;
        Material.Textures[Shader.TextureInput.Albedo] = tx.Albedo;

        Mesh = new MeshData("gui_mesh", OpenTK.Graphics.OpenGL4.PrimitiveType.Points, Gpu.CreateVertexBuffer(_verts));
        Mesh.DrawOrder = DrawOrder.Last;
      }
    }
    public override void Update(World world, double dt, ref Box3f parentBoundBox)
    {
      base.Update(world, dt, ref parentBoundBox);
      if (world.Camera != null)
      {
        //TODO: 
        //  guiShader->addUniform (Frustum.TopLEft)
        //  **Camera.BasisXYZ -> This is already a shader uniform.
      }
      if (Mesh != null && Mesh.VertexBuffers != null && Mesh.VertexBuffers.Count>0)
      {
        var pt = Gpu.GetGpuDataPtr(_verts);
        Mesh.VertexBuffers[0].CopyDataToGPU(pt, 0, _numVerts);
      }
       
      _numVerts = 0;
    }
    public void DrawText(vec2 pos, string text)
    {
      Gu.Assert(_activeFont != null);
      Gu.Assert(_activeFont.MtFont != null);
      float w, h;
      foreach (char c in text)
      {
    //    _activeFont.MtFont.getCharQuad(c);
        //Glyph g = GetGlyphByChar(c) // glyphs cached by by getCharQuad
        //    g.x += w;
        //w += g.width;
        //if (_cur_vert >= MaxGuiVerts)
        //{
        //  Gu.Log.Error("Used up all our verts.");
        //}
        //else
        //{
        //  Verts[_numVerts] = g.GetVert();
        //}
      }
    }

  }//class Gui

}//Namespace Pri
