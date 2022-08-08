using System;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;

namespace PirateCraft
{
  //Material, input to a shader & gpu state for material FBO (blending, etc)
  public class Material
  {
    private static Material _defaultDiffuse = null; //Default color material / shader.
    private static Material _defaultFlatColor = null; //Default color material / shader.
    public string Name { get;set;} = "material-unnamd";

    //Clonable members
    public Dictionary<Shader.TextureInput, Texture2D> Textures { get; private set; } = new Dictionary<Shader.TextureInput, Texture2D>();
    public Shader Shader { get; private set; } = null;
    public GpuRenderState GpuRenderState { get; set; } = new GpuRenderState(); //The rendering state of the material: clipping, depth, alpha, culling, etc

    public Material(string name, Shader s) :
       this(name, s, null, Shader.TextureInput.Albedo)
    {
    }
    public Material(string name, Shader s, Texture2D albedo) :
       this(name, s, albedo, Shader.TextureInput.Albedo)
    {
    }
    public Material(string name, Shader s, Texture2D single_tex, Shader.TextureInput single_tex_input) :
 this(name, s, new Dictionary<Shader.TextureInput, Texture2D>() { { single_tex_input, single_tex } })
    {
    }
    public Material(string name, Shader s, Texture2D albedo, Texture2D normal) :
       this(name, s, new Dictionary<Shader.TextureInput, Texture2D>() { { Shader.TextureInput.Albedo, albedo }, { Shader.TextureInput.Normal, normal } })
    {
    }
    private Material() { }
    public Material(string name, Shader s, Dictionary<Shader.TextureInput, Texture2D> textures = null)
    {
      this.Name = name;
      Textures = new Dictionary<Shader.TextureInput, Texture2D>();
      if (textures != null)
      {
        //Add textures that aren't null.
        bool added = false;
        foreach (var tex in textures)
        {
          if (tex.Value != null)
          {
            Textures.Add(tex.Key, tex.Value);
            added = true;
          }
        }
      }

      Shader = s;
    }
    public Material Clone(bool shallow = true)
    {
      Gu.Assert(shallow == true);//Not supported to clone the shader or textures

      Material other = new Material();

      other.GpuRenderState = this.GpuRenderState.Clone();
      other.Shader = this.Shader;
      other.Name = this.Name + "-copy";
      if (this.Textures != null)
      {
        other.Textures = new Dictionary<Shader.TextureInput, Texture2D>(this.Textures);
      }
      return other;
    }
    public static Material DefaultFlatColor()
    {
      //TODO: - the input shader should also be default.
      if (_defaultFlatColor == null)
      {
        _defaultFlatColor = new Material("DefaultFlatColor",Shader.DefaultFlatColorShader());
      }
      return _defaultFlatColor;
    }
    public static Material DefaultDiffuse()
    {
      //TODO: - the input shader should also be default.
      if (_defaultDiffuse == null)
      {
        _defaultDiffuse = new Material("DefaultDiffuse",Shader.DefaultDiffuse());
      }
      return _defaultDiffuse;
    }
    public void Draw(MeshData mesh, DrawCall_UniformData dat)
    {
      GpuRenderState.SetState();
      dat.m = this;
      Shader.BeginRender(dat);
      mesh.Draw(dat.instanceData);
      Shader.EndRender();
    }
    public void Draw(MeshData[] meshes, DrawCall_UniformData dat)
    {
      GpuRenderState.SetState();
      dat.m = this;
      Shader.BeginRender(dat);
      foreach (var m in meshes)
      {
        m.Draw(dat.instanceData);
      }
      Shader.EndRender();
    }
    public Texture2D GetTextureOrDefault(Shader.TextureInput texture_input)
    {
      Texture2D tex = null;
      if (Textures == null)
      {
        tex = Texture2D.Default(texture_input);
      }
      else
      {
        Textures.TryGetValue(texture_input, out tex);

        if (tex == null)
        {
          tex = Texture2D.Default(texture_input);
        }
      }

      return tex;
    }

  }
}
