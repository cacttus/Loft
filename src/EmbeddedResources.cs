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
      WorldObject o;

      //FAK now we are loading everything. 

      t = Gu.Lib.LoadTexture(RName.Tex2D_DefaultFailedTexture, Image.Default1x1_RGBA32ub(RName.Image_DefaultFailedPixel, Byte.MaxValue, 0, Byte.MaxValue, Byte.MaxValue), false, TexFilter.Nearest);
      t = Gu.Lib.LoadTexture(RName.Tex2D_DefaultWhitePixel, Image.Default1x1_RGBA32ub(RName.Image_DefaultWhitePixel, Byte.MaxValue, 0, Byte.MaxValue, Byte.MaxValue), false, TexFilter.Nearest);
      t = Gu.Lib.LoadTexture(RName.Tex2D_DefaultBlackPixelNoAlpha, Image.Default1x1_RGBA32ub(RName.Image_DefaultWhitePixel, 0, 0, 0, 0), false, TexFilter.Nearest);
      CreateNormalPixel();

      s = Gu.Lib.LoadShader(RName.Shader_GuiShader, "v_gui", FileStorage.Embedded, OpenTK.Graphics.OpenGL4.PrimitiveType.Points);
      s = Gu.Lib.LoadShader(RName.Shader_DefaultFlatColorShader, "v_v3", FileStorage.Embedded);
      s = Gu.Lib.LoadShader(RName.Shader_DefaultObjectShader, "v_DefaultObjectShader", FileStorage.Embedded);
      s = Gu.Lib.LoadShader(RName.Shader_DefaultBillboardPoints, "v_billboard_points", FileStorage.Embedded, OpenTK.Graphics.OpenGL4.PrimitiveType.Points);
      s = Gu.Lib.LoadShader(RName.Shader_DebugDraw, "v_v3c4_debugdraw", FileStorage.Embedded);//Dont override primtype

      s = Gu.Lib.LoadShader(RName.Shader_VertexFaceNormals,
      new List<FileLoc>(){
          new FileLoc("v_obj_dbg_shared.vs.glsl", FileStorage.Embedded),
          new FileLoc("v_normals.gs.glsl", FileStorage.Embedded),
          new FileLoc("v_obj_dbg_shared.fs.glsl", FileStorage.Embedded),
        }, OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles);

      s = Gu.Lib.LoadShader(RName.Shader_Wireframe,
      new List<FileLoc>(){
          new FileLoc("v_obj_dbg_shared.vs.glsl", FileStorage.Embedded),
          new FileLoc("v_wire.gs.glsl", FileStorage.Embedded),
          new FileLoc("v_obj_dbg_shared.fs.glsl", FileStorage.Embedded),
        }, OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles);

      m = Gu.Lib.LoadMaterial(RName.Material_DefaultFlatColorMaterial, Gu.Lib.LoadShader(RName.Shader_DefaultFlatColorShader));
      m.AlbedoSlot.Texture = Gu.Lib.LoadTexture(RName.Tex2D_DefaultWhitePixel);

      m = Gu.Lib.LoadMaterial(RName.Material_DefaultObjectMaterial, Gu.Lib.LoadShader(RName.Shader_DefaultObjectShader));
      m.AlbedoSlot.Texture = Gu.Lib.LoadTexture(RName.Tex2D_DefaultFailedTexture);

      m = Gu.Lib.LoadMaterial(RName.DebugDraw_Wireframe_FlatColor, Gu.Lib.LoadShader(RName.Shader_Wireframe));
      m.GpuRenderState.Blend = true;
      m.GpuRenderState.CullFace = false;
      m.GpuRenderState.DepthTest = true;
      m.Flat = true;

      m = Gu.Lib.LoadMaterial(RName.Material_DebugDraw_VertexNormals_FlatColor, Gu.Lib.LoadShader(RName.Shader_VertexFaceNormals));
      m.GpuRenderState.Blend = false;
      m.Flat = true;

      m = Gu.Lib.LoadMaterial(RName.Material_DebugDrawMaterial, Gu.Lib.LoadShader(RName.Shader_DebugDraw));
      m.GpuRenderState.Blend = true;
      m.GpuRenderState.CullFace = false;
      m.Flat = true;

      o = Gu.Lib.LoadModel(RName.WorldObject_Camera, new FileLoc("camera.glb", FileStorage.Embedded), true);
      o = Gu.Lib.LoadModel(RName.WorldObject_Gear, new FileLoc("gear.glb", FileStorage.Embedded), true);
      o = Gu.Lib.LoadModel(RName.WorldObject_Barrel, new FileLoc("barrel.glb", FileStorage.Embedded), true);

    }
    private static void CreateNormalPixel()
    {
      byte nmap_zero = Byte.MaxValue / 2;//zero in normal map is .5
      byte nmap_one = Byte.MaxValue;
      if (Texture.NormalMapFormat == NormalMapFormat.Yup)
      {
        var tex = Gu.Lib.LoadTexture(RName.Tex2D_DefaultNormalPixel,
        Image.Default1x1_RGBA32ub(RName.Image_DefaultNormalPixelZUp, nmap_zero, nmap_one, nmap_zero, Byte.MaxValue), false, TexFilter.Nearest);
      }
      else if (Texture.NormalMapFormat == NormalMapFormat.Zup)
      {
        var tex = Gu.Lib.LoadTexture(RName.Tex2D_DefaultNormalPixel,
        Image.Default1x1_RGBA32ub(RName.Image_DefaultNormalPixelZUp, nmap_zero, nmap_zero, nmap_one, Byte.MaxValue), false, TexFilter.Nearest);
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
    }

  }


}