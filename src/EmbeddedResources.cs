using System.ComponentModel;

namespace PirateCraft
{
  public static class EmbeddedResources
  {
    //Build some manual resources.
    public static void BuildResources()
    {
      Image i;
      Texture t;
      Shader s;
      Material m;
      MeshData md;
      Model o;

      Lib g = Gu.Lib;

      //texs
      t = g.AddE(new Texture(Rs.Tex2D.DefaultFailedTexture, Image.Default1x1_RGBA32ub(Rs.Image.DefaultFailedPixel, Byte.MaxValue, 0, Byte.MaxValue, Byte.MaxValue), false, TexFilter.Nearest));
      t = g.AddE(new Texture(Rs.Tex2D.DefaultWhitePixel, Image.Default1x1_RGBA32ub(Rs.Image.DefaultWhitePixel, Byte.MaxValue, 0, Byte.MaxValue, Byte.MaxValue), false, TexFilter.Nearest));
      t = g.AddE(new Texture(Rs.Tex2D.DefaultBlackPixelNoAlpha, Image.Default1x1_RGBA32ub(Rs.Image.DefaultWhitePixel, 0, 0, 0, 0), false, TexFilter.Nearest));
      CreateNormalPixel(g);

      //shaders
      s = g.AddE(new Shader(Rs.Shader.GuiShader, "v_gui", FileStorage.Embedded, OpenTK.Graphics.OpenGL4.PrimitiveType.Points));
      s = g.AddE(new Shader(Rs.Shader.DefaultFlatColorShader, "v_v3", FileStorage.Embedded));
      s = g.AddE(new Shader(Rs.Shader.DefaultObjectShader, "v_DefaultObjectShader", FileStorage.Embedded));
      s = g.AddE(new Shader(Rs.Shader.DefaultBillboardPoints, "v_billboard_points", FileStorage.Embedded, OpenTK.Graphics.OpenGL4.PrimitiveType.Points));

      s = g.AddE(new Shader(Rs.Shader.DebugDraw_Lines, new List<FileLoc>(){
          new FileLoc("v_debugdraw_shared.vs.glsl", FileStorage.Embedded),
          new FileLoc("v_lines.gs.glsl", FileStorage.Embedded),
          new FileLoc("v_debugdraw_shared.fs.glsl", FileStorage.Embedded),
        }, OpenTK.Graphics.OpenGL4.PrimitiveType.Lines));
      s = g.AddE(new Shader(Rs.Shader.DebugDraw_Points,
      new List<FileLoc>(){
          new FileLoc("v_debugdraw_shared.vs.glsl", FileStorage.Embedded),
          new FileLoc("v_points.gs.glsl", FileStorage.Embedded),
          new FileLoc("v_debugdraw_shared.fs.glsl", FileStorage.Embedded),
        }, OpenTK.Graphics.OpenGL4.PrimitiveType.Points));
      s = g.AddE(new Shader(Rs.Shader.DebugDraw_Tris,
      new List<FileLoc>(){
          new FileLoc("v_debugdraw_shared.vs.glsl", FileStorage.Embedded),
          new FileLoc("v_tris.gs.glsl", FileStorage.Embedded),
          new FileLoc("v_debugdraw_shared.fs.glsl", FileStorage.Embedded),
        }, OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles));
      s = g.AddE(new Shader(Rs.Shader.VertexFaceNormals,
      new List<FileLoc>(){
          new FileLoc("v_obj_dbg_shared.vs.glsl", FileStorage.Embedded),
          new FileLoc("v_normals.gs.glsl", FileStorage.Embedded),
          new FileLoc("v_obj_dbg_shared.fs.glsl", FileStorage.Embedded),
        }, OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles));
      s = g.AddE(new Shader(Rs.Shader.Wireframe,
      new List<FileLoc>(){
          new FileLoc("v_obj_dbg_shared.vs.glsl", FileStorage.Embedded),
          new FileLoc("v_wire.gs.glsl", FileStorage.Embedded),
          new FileLoc("v_obj_dbg_shared.fs.glsl", FileStorage.Embedded), 
        }, OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles));
      s = g.AddE(new Shader(Rs.Shader.Solid,
      new List<FileLoc>(){
          new FileLoc("v_solid.vs.glsl", FileStorage.Embedded),
          new FileLoc("v_solid.fs.glsl", FileStorage.Embedded),
        }, OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles));

      //Obj material
      m = g.AddE(new Material(Rs.Material.DefaultObjectMaterial, g.GetShader(Rs.Shader.DefaultObjectShader)));
      m.AlbedoSlot.Texture = g.GetTexture(Rs.Tex2D.DefaultFailedTexture);
      m.DrawOrder = DrawOrder.Mid;
      m.DrawMode = DrawMode.Deferred;

      //Debug mateirals
      m = g.AddE(new Material(Rs.Material.DebugDraw_Wireframe_FlatColor, g.GetShader(Rs.Shader.Wireframe)));
      m.GpuRenderState.DepthTest = true;
      m.GpuRenderState.Blend = true;
      m.GpuRenderState.CullFace = false;
      m.DrawOrder = DrawOrder.Last;
      m.DrawMode = DrawMode.Debug;
      m.Flat = true;
      
      m = g.AddE(new Material(Rs.Material.DebugDraw_Solid_FlatColor, g.GetShader(Rs.Shader.Solid)));
      m.BaseColor = new vec4(.793f, .779f, .783f, 1);
      m.GpuRenderState.DepthTest = true;
      m.GpuRenderState.Blend = true;
      m.GpuRenderState.CullFace = false;
      m.DrawOrder = DrawOrder.Last;
      m.DrawMode = DrawMode.Deferred;
      m.Flat = false;

      m = g.AddE(new Material(Rs.Material.DebugDraw_VertexNormals_FlatColor, g.GetShader(Rs.Shader.VertexFaceNormals)));
      m.GpuRenderState.DepthTest = true;
      m.GpuRenderState.Blend = false;
      m.GpuRenderState.CullFace = false;
      m.DrawOrder = DrawOrder.Last;
      m.DrawMode = DrawMode.Debug;
      m.Flat = true;

      m = g.AddE(new Material(Rs.Material.DebugDrawMaterial_Lines, g.GetShader(Rs.Shader.DebugDraw_Lines)));
      m.GpuRenderState.DepthTest = true;
      m.GpuRenderState.Blend = true;
      m.GpuRenderState.CullFace = false;
      m.DrawOrder = DrawOrder.Last;
      m.DrawMode = DrawMode.Debug;
      m.Flat = true;

      m = g.AddE(new Material(Rs.Material.DebugDrawMaterial_Points, g.GetShader(Rs.Shader.DebugDraw_Points)));
      m.GpuRenderState.DepthTest = true;
      m.GpuRenderState.Blend = true;
      m.GpuRenderState.CullFace = false;
      m.DrawOrder = DrawOrder.Last;
      m.DrawMode = DrawMode.Debug;
      m.Flat = true;

      m = g.AddE(new Material(Rs.Material.DebugDrawMaterial_Tris, g.GetShader(Rs.Shader.DebugDraw_Tris)));
      m.GpuRenderState.DepthTest = true;
      m.GpuRenderState.Blend = true;
      m.GpuRenderState.CullFace = false;
      m.DrawOrder = DrawOrder.Last;
      m.DrawMode = DrawMode.Debug;
      m.Flat = true;

      //mesh
      md = g.AddE(MeshGen.GenBox(Rs.Mesh.DefaultBox, 1, 1, 1));


      //models
      o = g.AddE(g.LoadModel(Rs.Model.Camera, new FileLoc("camera.glb", FileStorage.Embedded), true));
      o = g.AddE(g.LoadModel(Rs.Model.Gear, new FileLoc("gear.glb", FileStorage.Embedded), true));
      o = g.AddE(g.LoadModel(Rs.Model.Barrel, new FileLoc("barrel.glb", FileStorage.Embedded), true));

    }

    private static void CreateNormalPixel(Lib lib)
    {
      byte nmap_zero = Byte.MaxValue / 2;//zero in normal map is .5
      byte nmap_one = Byte.MaxValue;
      if (Texture.NormalMapFormat == NormalMapFormat.Yup)
      {
        var tex = lib.AddE(new Texture(Rs.Tex2D.DefaultNormalPixel, Image.Default1x1_RGBA32ub(Rs.Image.DefaultNormalPixelZUp, nmap_zero, nmap_one, nmap_zero, Byte.MaxValue), false, TexFilter.Nearest));
      }
      else if (Texture.NormalMapFormat == NormalMapFormat.Zup)
      {
        var tex = lib.AddE(new Texture(Rs.Tex2D.DefaultNormalPixel, Image.Default1x1_RGBA32ub(Rs.Image.DefaultNormalPixelZUp, nmap_zero, nmap_zero, nmap_one, Byte.MaxValue), false, TexFilter.Nearest));
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
    }

  }


}