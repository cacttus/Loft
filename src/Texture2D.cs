using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing.Imaging;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace PirateCraft
{
   public enum NormalMapFormat
   {
      Zup, Yup
   }
   public enum TexFilter
   {
      Nearest, //If mipmaps enabled, then this will also choose nearest mipmap (mipmapping should be specified if possible)
      Linear, //Simple linear filtering of texels (no mipmapping, regardless if it is set)
      Bilinear, //Linear filter texture, but use nearest mipmap (smooth with some noticable mipmap jumps). 
      Trilinear //Linear filter texture, linear filter between mipmaps. (very smooth / blurry).
   }
   public enum TextureChannel
   {
      Channel0,
      Channel1,
      Channel2,
      Channel3,
      Channel4,
      Channel5,
      Channel6,
      Channel7,
      Channel8,
      Channel9,
      Channel10,
      Channel11,
      Channel12,
      Channel13,
      Channel14,
      Channel15,
      Channel16,
   }
   public enum TexWrap
   {
      Clamp,
      Repeat
   }
   public class Texture2D : OpenGLResource
   {
      //public int _intTextureId;
      public float Width;
      public float Height;

      public static NormalMapFormat NormalMapFormat { get; private set; } = NormalMapFormat.Yup;

      private static Dictionary<Shader.TextureInput, Texture2D> _defaults = new Dictionary<Shader.TextureInput, Texture2D>();


      public System.Drawing.Imaging.PixelFormat WindowsPixelFormat
      {
         get
         {
            return System.Drawing.Imaging.PixelFormat.Format32bppArgb;
         }
         private set
         {
         }
      }
      public OpenTK.Graphics.OpenGL.PixelFormat GlPixelFormat
      {
         get
         {
            return OpenTK.Graphics.OpenGL.PixelFormat.Bgra;
         }
         private set
         {
         }
      }
      public Texture2D(Bitmap b, bool mipmaps, TexFilter filter, TextureWrapMode wrap = TextureWrapMode.Repeat)
      {
         Img32 img = new Img32(b);
         LoadToGpu(img, mipmaps, filter, wrap);
      }
      public Texture2D(Img32 img, bool mipmaps, TexFilter filter, TextureWrapMode wrap = TextureWrapMode.Repeat)
      {
         LoadToGpu(img, mipmaps, filter, wrap);
      }
      public Texture2D(FileLoc loc, bool mipmaps, TexFilter filter, TextureWrapMode wrap = TextureWrapMode.Repeat)
      {
         var bmp = Gu.LoadImage(loc);
         LoadToGpu(bmp, mipmaps, filter, wrap);
      }
      private int GetNumMipmaps(int w, int h)
      {
         int numMipMaps = 0;
         int x = System.Math.Max(w, h);
         for (; x > 0; x = x >> 1)
         {
            numMipMaps++;
         }
         return numMipMaps;
      }
      private void LoadToGpu(Img32 bmp, bool mipmaps, TexFilter filter, TextureWrapMode wrap)
      {
         Width = bmp.Width;
         Height = bmp.Height;

         int ts = Gu.Context.Gpu.GetMaxTextureSize();
         if (Width >= ts)
         {
            Gu.BRThrowException("Texture is too large");
         }
         if (Height >= ts)
         {
            Gu.BRThrowException("Texture is too large");
         }
         TextureMinFilter min = TextureMinFilter.Nearest;
         TextureMagFilter mag = TextureMagFilter.Nearest;
         if (filter == TexFilter.Linear)
         {
            min = TextureMinFilter.Linear;
            mag = TextureMagFilter.Linear;
         }
         else if (filter == TexFilter.Nearest)
         {
            if (mipmaps)
            {
               //Mipmap nearest is smoother than simply nearest, it looks better
               min = TextureMinFilter.NearestMipmapNearest;
            }
            else
            {
               min = TextureMinFilter.Nearest;
            }
            mag = TextureMagFilter.Nearest;
         }
         else if (filter == TexFilter.Bilinear)
         {
            if (mipmaps)
            {
               min = TextureMinFilter.LinearMipmapNearest;
               mag = TextureMagFilter.Linear;
            }
            else
            {
               Gu.Log.Warn("No mipmaps specified for texure but bilinear / trilinear filtering was specified.");
            }
         }
         else if (filter == TexFilter.Trilinear)
         {
            if (mipmaps)
            {
               min = TextureMinFilter.LinearMipmapLinear;
               mag = TextureMagFilter.Linear;
            }
            else
            {
               Gu.Log.Warn("No mipmaps specified for texure but bilinear / trilinear filtering was specified.");
            }
         }
         else
         {
            Gu.BRThrowNotImplementedException();
         }



         _glId = GL.GenTexture();
         GL.BindTexture(TextureTarget.Texture2D, GetGlId());
         GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)min);//LinearMipmapLinear
         GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)mag);
         GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)wrap);
         GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)wrap);

         int numMipmaps = 1;
         if (mipmaps)
         {
            numMipmaps = GetNumMipmaps((int)Width, (int)Height);
         }

         GL.TexStorage2D(TextureTarget2d.Texture2D, numMipmaps, SizedInternalFormat.Rgba8, (int)Width, (int)Height);

         //var raw = Gpu.SerializeGPUData(bmp.Data);
         var raw = Gpu.GetGpuDataPtr(bmp.Data);

         GL.TexSubImage2D(TextureTarget.Texture2D,
             0, //mipmap level
             0, 0, //x.y
             bmp.Width,
             bmp.Height,
             GlPixelFormat,
             PixelType.UnsignedByte,
             raw.Lock());
         raw.Unlock();

         if (mipmaps)
         {
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
         }
      }
      public static Texture2D Default(Shader.TextureInput input)
      {
         if (!_defaults.TryGetValue(input, out Texture2D texture))
         {
            Texture2D tex = null;
            if (input == Shader.TextureInput.Albedo)
            {
               //White albedo
               Bitmap b = Img32.CreateBitmapARGB(1, 1, new byte[] { 255, 255, 255, 255 });
               tex = new Texture2D(b, false, TexFilter.Nearest);
            }
            else if (input == Shader.TextureInput.Normal)
            {
               //Normal texture pointing up from surface (default) 
               byte[] dat = null;
               if (NormalMapFormat == NormalMapFormat.Yup)
               {
                  dat = new byte[] { 0, 255, 0, 255 };
               }
               else if (NormalMapFormat == NormalMapFormat.Zup)
               {
                  dat = new byte[] { 0, 0, 255, 255 };
               }
               else
               {
                  Gu.BRThrowNotImplementedException();
               }

               Bitmap b = Img32.CreateBitmapARGB(1, 1, dat);//Note this must match the Img32 normalizeImage format
               tex = new Texture2D(b, false, TexFilter.Nearest);
            }
            else
            {
               Gu.Log.WarnCycle("Default texture not handled for Texture2D::BindDefault");
            }
            if (tex != null)
            {
               _defaults.Add(input, tex);
            }
         }
         return texture;
      }
      public void Bind(TextureUnit unit)
      {
         if (GetGlId() == 0)
         {
            throw new System.Exception("Texture ID was 0 when binding texture.");
         }

         GL.ActiveTexture(unit);
         GL.BindTexture(TextureTarget.Texture2D, GetGlId());
      }
      public void Unbind(TextureUnit unit)
      {
         GL.ActiveTexture(unit);
         GL.BindTexture(TextureTarget.Texture2D, 0);
      }
      public override void Dispose()
      {
         //If delete is called on a non-texture it is ignored by the GL
         if (GL.IsTexture(GetGlId()))
         {
            GL.DeleteTexture(GetGlId());
         }
         base.Dispose();
      }

   }
}
