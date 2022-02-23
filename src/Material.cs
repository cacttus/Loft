using System;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;

namespace PirateCraft
{
   //Material, input to a shader & gpu state for material FBO (blending, etc)
   public class Material
   {
      //Clonable members
      public Dictionary<Shader.TextureInput, Texture2D> Textures { get; private set; } = null;
      public Shader Shader { get; private set; } = null;
      public GpuRenderState GpuRenderState { get; set; } = new GpuRenderState(); //The rendering state of the material: clipping, depth, alpha, culling, etc

      private static Material _defaultDiffuse = null; //Default color material / shader.
      private static Material _defaultFlatColor = null; //Default color material / shader.

      public Material(Shader s) :
         this(s, null, Shader.TextureInput.Albedo)
      {
      }
      public Material(Shader s, Texture2D albedo) :
         this(s, albedo, Shader.TextureInput.Albedo)
      {
      }
      public Material(Shader s, Texture2D single_tex, Shader.TextureInput single_tex_input) :
   this(s, new Dictionary<Shader.TextureInput, Texture2D>() { { single_tex_input, single_tex } })
      {
      }
      public Material(Shader s, Texture2D albedo, Texture2D normal) :
         this(s, new Dictionary<Shader.TextureInput, Texture2D>() { { Shader.TextureInput.Albedo, albedo }, { Shader.TextureInput.Normal, normal } })
      {
      }
      public Material(Shader s, Dictionary<Shader.TextureInput, Texture2D> textures = null)
      {
         if (textures != null)
         {
            //Add textures that aren't null.
            bool added = false;
            Textures = new Dictionary<Shader.TextureInput, Texture2D>();
            foreach (var tex in textures)
            {
               if (tex.Value != null)
               {
                  Textures.Add(tex.Key, tex.Value);
                  added = true;
               }
            }
            if (added == false)
            {
               Textures = null;
            }
         }
         Shader = s;
      }
      public Material Clone()
      {
         Material m = new Material(this.Shader, this.Textures);
         m.GpuRenderState = this.GpuRenderState.Clone();
         return m;
      }
      public static Material DefaultFlatColor()
      {
         //TODO: - the input shader should also be default.
         if (_defaultFlatColor == null)
         {
            _defaultFlatColor = new Material(Shader.DefaultFlatColorShader());
         }
         return _defaultFlatColor;
      }
      public static Material DefaultDiffuse()
      {
         //TODO: - the input shader should also be default.
         if (_defaultDiffuse == null)
         {
            _defaultDiffuse = new Material(Shader.DefaultDiffuse());
         }
         return _defaultDiffuse;
      }
      public void BeginRender(double dt, Camera3D camera, WorldObject ob)
      {
         //Gu.CurrentWindowContext.Gpu.GpuRenderState.CullFace = GpuRenderState.CullFace;
         //Gu.CurrentWindowContext.Gpu.GpuRenderState.DepthTest = GpuRenderState.DepthTest;
         //Gu.CurrentWindowContext.Gpu.GpuRenderState.ScissorTest = GpuRenderState.ScissorTest;

         GpuRenderState.SetState();

         Shader.BeginRender(dt, camera, ob, this);
      }
      public void EndRender()
      {
         Shader.EndRender();
      }
      public Texture2D GetTextureOrDefault(Shader.TextureInput texture_input)
      {
         Textures.TryGetValue(texture_input, out var tex);

         if (tex == null)
         {
            tex = Texture2D.Default(texture_input);
         }

         return tex;
      }

   }
}
