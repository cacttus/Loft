using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
//Chinese Test
//if (getName().QualifiedPath.Contains("simhei"))
//{
//  //https://stackoverflow.com/questions/1366068/whats-the-complete-range-for-chinese-characters-in-unicode
//  //Han Ideographs: 4E00 - 9FFF   Common
//  _firstChar = 0x4E00;
//  _charCount = 0x62FF - 0x4E00;  //0x9FFF is the whole range, that's a lot
//                                 //Compute size for a 20x20 pixel han character
//  _iBakedCharSizePixels = 20;
//  float ch_w = ceilf(sqrtf((float)_charCount));
//  _atlasWidth = _atlasHeight = (uint)((ch_w) * (_iBakedCharSizePixels + _oversampleX));

//  //Test "huan"
//  //_firstChar = 0x6B61;// 喜..喜欢 0x559C, 0x6B61.. correct.. seems to work..Note: 欢 prints, 歡.. the traditioanl character
//  //_charCount = 1;
//  Gu.DebugBreak();
//}

namespace PirateCraft
{
  public enum MtTexType
  {
    Image,
    Font
  }
  public class MtNode
  {
    public MtTex _pTex = null;
    public MtNode[] _pChild = new MtNode[2];
    public Box2i _b2Rect;
    public MtNode plop(MtTex tex)
    {
      //Copied from
      //http://blackpawn.com/texts/lightmaps/default.html
      if (_pChild[0] != null && _pChild[1] != null)
      {
        //leaf
        MtNode ret = _pChild[0].plop(tex);
        if (ret != null)
        {
          return ret;
        }
        return _pChild[1].plop(tex);
      }
      else
      {
        if (_pTex != null)
        {
          return null;
        }
        int bw = _b2Rect.Width();
        int bh = _b2Rect.Height();

        if (tex.GetHeight() > bh)
        {
          return null;
        }
        if (tex.GetWidth() > bw)
        {
          return null;
        }
        if (tex.GetWidth() == bw && tex.GetHeight() == bh)
        {
          //prefect fit
          _pTex = tex;
          return this;
        }

        _pChild[0] = new MtNode();
        _pChild[1] = new MtNode();

        int dw = bw - tex.GetWidth();
        int dh = bh - tex.GetHeight();

        if (dw > dh)
        {
          _pChild[0]._b2Rect.Construct(
              _b2Rect.left(),
              _b2Rect.top(),
              _b2Rect.left() + tex.GetWidth(),
              _b2Rect.bottom());
          _pChild[1]._b2Rect.Construct(
              _b2Rect.left() + tex.GetWidth(),
              _b2Rect.top(),
              _b2Rect.right(),
              _b2Rect.bottom());
        }
        else
        {
          _pChild[0]._b2Rect.Construct(
              _b2Rect.left(),
              _b2Rect.top(),
              _b2Rect.right(),
              _b2Rect.top() + tex.GetHeight());
          _pChild[1]._b2Rect.Construct(
              _b2Rect.left(),
              _b2Rect.top() + tex.GetHeight(),
              _b2Rect.right(),
              _b2Rect.bottom());
        }
        return _pChild[0].plop(tex);
      }
    }
  };
  #region MtTex

  public class MtTex
  {
    private MtNode _pMtNode = null;  //mega texture node
    private Img32 _pImg = null;
    private int _iWidth = 0;
    private int _iHeight = 0;
    private float _fSizeRatio = 0;
    private vec2 _uv_p0, _uv_p1;
    private FileLoc _strImgName;
    private int _iPatchImg = 0;  //0-8 for 9p, or 0-2 for 3p //Basically this is if we split an image up into "patches". Probably not being used.

    public void SetWH(int w, int h)
    {
      _iWidth = w;
      _iHeight = h;
    }
    public MtTex(FileLoc imgName, int iPatch)
    {
      _strImgName = imgName;
      _iPatchImg = iPatch;
    }
    public FileLoc ImgName() { return _strImgName; }
    public int GetWidth() { return _iWidth; }
    public int GetHeight() { return _iHeight; }
    public float GetSizeRatio() { return _fSizeRatio; }
    public vec2 uv0 { get { return _uv_p0; } set { _uv_p0 = value; } } // Bottom left,in OpenGL coordinates, from [0,1] 
    public vec2 uv1 { get { return _uv_p1; } set { _uv_p1 = value; } } // Top Right [0,1]
    public MtNode Node() { return _pMtNode; }        //mega texture node
    public void SetNode(MtNode n) { _pMtNode = n; }  //mega texture node
    public Img32 Img() { return _pImg; }
    public void SetImg(Img32 img)
    {
      _pImg = img;
      //We don't save the img data on the CPU, so just store what we need
      _iHeight = img.Height;
      _iWidth = img.Width;
      _fSizeRatio = (float)_iWidth / (float)_iHeight;
    }
    public void FreeTmp()
    {
      _pImg = null;
      _pMtNode = null;
    }
    public vec2[] GetQuadTexs()
    {
      //Get quad textures oriented via OpenGL with the image flipped upside down
      vec2[] texs = new vec2[4];
      texs[0] = new vec2(uv0.x, uv0.y);
      texs[1] = new vec2(uv1.x, uv0.y);
      texs[2] = new vec2(uv0.x, uv1.y);
      texs[3] = new vec2(uv1.x, uv1.y);
      return texs;
    }
  }

  #endregion
  #region MtTexPatch

  public class MtTexPatch
  {
    //List<Bitmap> parseImagePatch(string file);
    FileLoc _strName;  //Image Or Font name
    List<MtTex> _vecTexs = new List<MtTex>();
    MegaTex _pMegaTex = null;
    public FileLoc GetName() { return _strName; }
    public List<MtTex> GetTexs() { return _vecTexs; }

    public MtTexPatch(MegaTex mt, FileLoc imgName)
    {
      _strName = imgName;
      _pMegaTex = mt;
    }
    public void AddTexImage(FileLoc img, int iPatch)
    {
      MtTex mt = new MtTex(img, iPatch);
      Gpu.CheckGpuErrorsDbg();
      //_pMegaTex.getContext().chkErrDbg();
      _vecTexs.Add(mt);
    }
    public virtual void LoadData()
    {
      if (_vecTexs.Count() == 0)
      {
        Gu.Log.Error("Image patch was invalid for " + GetName() + ".");
      }
      else if (_vecTexs.Count() == 1)
      {
        if (_vecTexs[0].Img() == null)
        {
          //If image isn't null, then it was already provided and should be loaded.
          if (!GetName().Exists)
          {
            Gu.Log.Error("Failed to load, image file '" + GetName() + "' didn't exist");
            Gu.DebugBreak();
          }
          else
          {
            Img32 img = ResourceManager.LoadImage(GetName());
            _vecTexs[0].SetImg(img);
          }
        }
      }
      else
      {
        List<Img32> imgs = ParseImagePatch(GetName());
        if (imgs.Count != _vecTexs.Count)
        {
          Gu.Log.Error("Tex Count Mismatch, or texture not found for '" + GetName() + "'.");
          Gu.DebugBreak();
        }
        else
        {
          for (int i = 0; i < imgs.Count; ++i)
          {
            _vecTexs[i].SetImg(imgs[i]);
          }
        }
      }
    }
    private List<Img32> ParseImagePatch(FileLoc file)
    {
      List<Img32> ret = new List<Img32>();

      if (!file.Exists)
      {
        Gu.Log.Error("Failed to load, image file '" + file + "' didn't exist");
        return ret;
      }

      Img32 master = ResourceManager.LoadImage(file);// Gu::loadImage(file);

      //So we have to flip it because we load it into OpenGL space but we're in screen space.
      if (master == null)
      {
        Gu.Log.Error("Error parsing 9-tile. Invalid or missing master image file '" + file + "'");
        return ret;
      }

      //Not sure what "ijmage patch is"
      Gu.DebugBreak();
      //  if (Img32::parseImagePatch(master, ret) == false)
      {
        Gu.Log.Error("Error parsing image patch for file " + file);
      }

      //bool b = false;
      //if (b)
      //{
      //   //save images (and master
      //   Gu::saveImage("./data/cache/saved_9P_master.png", master);
      //   for (int n = 0; n < ret.size(); ++n)
      //   {
      //      std::shared_ptr<Texture2D> tex = std::make_shared<Texture2D>(getName(), TextureFormat::Image4ub, ret[n], _pMegaTex.getContext(), TexFilter::Nearest);
      //      _pMegaTex.getContext().getRenderUtils().saveTexture(std::move(Stz "./data/cache/saved_9P_" + n + ".png"), tex.getGlId(), GL_TEXTURE_2D);
      //   }
      //}

      return ret;
    }
    public virtual void AfterCompile()
    {
      // Perform calculations after the texture coordinates have been generated.
    }
  }

  #endregion
  #region MtFont

  public struct CachedCharData
  {
    public int code;
    public vec2 uv0;
    public vec2 uv1;
    public float width;
    public float height;
    public float margin_top;
    public float margin_bot;
    public float margin_left;
    public float margin_right;
  }
  public class FontPatchInfo
  {
    public int BakedCharSize { get; set; } = 0; //Pixel height of the characters
    public StbTrueTypeSharp.StbTrueType.stbtt_packedchar[] CharInfo = null;
    public float ScaleForPixelHeight = 0;               //return value of stbtt_ScaleForPixelHeight
    public int TextureWidth = 0;
    public int TextureHeight = 0;
    public int TextureIndex = 0; //The patch index in the MtTexPatch
    public int FirstChar = ' ';
    public int CharCount = 0;
    public Dictionary<int, CachedCharData> CachedChars = new Dictionary<int, CachedCharData>();

    public bool GetChar(int unicode_point, float fontSize, out CachedCharData ccd)
    {
      if (unicode_point < FirstChar || unicode_point >= (FirstChar + CharCount))
      {
        //char is an invalid character such as a newline.
        //Process it as a space
        unicode_point = ' ';
      }

      //Transform quad by STB scale.
      //The STB quad is in STB scaled units to the given BakedChar size, i.e. it is not in "raw" units
      //We must then transform it into EM 
      float fScale = GetScaleForPixelSize(fontSize);
      if (CachedChars.TryGetValue(unicode_point, out ccd))
      {
        ccd.margin_top *= fScale;
        ccd.margin_right *= fScale;
        ccd.margin_bot *= fScale;
        ccd.margin_left *= fScale;
        ccd.width *= fScale;
        ccd.height *= fScale;
        return true;
      }
      else
      {
        return false;
      }
    }
    public float GetScaleForPixelSize(float ps)
    {
      //float f = StbTrueTypeSharp.StbTrueType.stbtt_ScaleForMappingEmToPixels(_fontInfo, .5f);
      float t = ps / (float)BakedCharSize;
      return t;
    }
  }
  public class MtFont : MtTexPatch
  {
    private StbTrueTypeSharp.StbTrueType.stbtt_fontinfo _fontInfo;
    private uint _oversampleX = 2;
    private uint _oversampleY = 2;
    private int _firstChar = ' ';
    private int _charCount = 512;
    private int _ascent = 0;
    private int _descent = 0;
    private int _lineGap = 0;
    private byte[] _pFontBuffer = null;  // This must stay loaded. STB:"Load" a font file from a memory buffer (you have to keep the buffer loaded)
    private bool _bInitialized = false;
    private int _padding = 1; //STB - "normally you want 1 for bilinear filtering"
    private List<FontPatchInfo> _fontPatchInfos = new List<FontPatchInfo>();

    public MtFont(MegaTex mt, FileLoc loc, int padding = 1) : base(mt, loc)
    {
      _padding = padding;
    }
    public override void LoadData()
    {
      Gu.Log.Info("Creating font '" + GetName());

      CreateSTBFont();

      CreateFontImages();

      _bInitialized = true;
    }
    private void CreateSTBFont()
    {
      using (var ss = GetName().GetStream())
      using (var ms = new MemoryStream())
      {
        ss.CopyTo(ms);
        _pFontBuffer = ms.ToArray();
      }
      if (_pFontBuffer != null)
      {
        _fontInfo = StbTrueTypeSharp.StbTrueType.CreateFont(_pFontBuffer, 0);
      }
      unsafe
      {
        _fontInfo = new StbTrueTypeSharp.StbTrueType.stbtt_fontinfo();
        fixed (byte* fbdata = _pFontBuffer)
        {
          StbTrueTypeSharp.StbTrueType.stbtt_InitFont(_fontInfo, fbdata, 0);
        }
      }
      if (_fontInfo == null)
      {
        Gu.BRThrowException("Could not initialize font. stb returned null fontinfo");
      }
      unsafe
      {
        int ascent = 0, descent = 0, lineGap = 0;
        StbTrueTypeSharp.StbTrueType.stbtt_GetFontVMetrics(_fontInfo, &ascent, &descent, &lineGap);
        _ascent = ascent;
        _descent = descent;
        _lineGap = lineGap;
      }
    }
    private void CreateFontImages()
    {
      //BakedCHar is the MAXIMUm size of an glyph, therefore
      // the image width / height should be automatic.
      int xchar = (int)Math.Ceiling(Math.Sqrt(_charCount));
      int charsize = Gu.EngineConfig.MaxBakedCharSize;
      int charPadding = _padding;

      for (int i = 0; i < 96; i++)//96=arbitrary to prevent inf loop
      {
        int imageWidth = xchar * (charsize + charPadding * 2);
        int imageHeight = xchar * (charsize + charPadding * 2);
        Gu.Assert(imageWidth < Gu.EngineConfig.MaxFontBitmapSize);
        Gu.Assert(imageHeight < Gu.EngineConfig.MaxFontBitmapSize);

        var charInfo = new StbTrueTypeSharp.StbTrueType.stbtt_packedchar[_charCount];
        byte[] atlasData = ConstructBitmapForSize(imageWidth, imageHeight, charPadding, (float)charsize, charInfo);

        //Trim the atlas, because it's friggin huge
        int usedHeight = GetBitmapUsedHeight(charInfo, imageWidth, imageHeight);
        atlasData = TrimAtlasBitmapToUsedHeight(imageWidth, usedHeight, atlasData);

        if (atlasData != null)
        {
          //Set the megatex image.
          Img32 img = CreateFontImage(atlasData, imageWidth, usedHeight, charInfo);
          if (Gu.EngineConfig.SaveSTBFontImage)
          {
            Gu.Log.Debug("DEBG:Saving font...");
            string nmapname_dbg = System.IO.Path.Combine(Gu.LocalCachePath, "mt_" + GetName() + "_font_" + i + ".png");
            ResourceManager.SaveImage(nmapname_dbg, img);
          }
          MtTex mt = new MtTex(GetName(), 0);
          mt.SetImg(img);
          GetTexs().Add(mt);

          FontPatchInfo f = new FontPatchInfo();
          f.ScaleForPixelHeight = StbTrueTypeSharp.StbTrueType.stbtt_ScaleForPixelHeight(_fontInfo, (float)charsize);
          f.BakedCharSize = charsize;
          f.TextureWidth = imageWidth;
          f.TextureHeight = usedHeight;
          f.CharInfo = charInfo;
          f.TextureIndex = i;
          f.FirstChar = _firstChar;
          f.CharCount = _charCount;
          _fontPatchInfos.Add(f);
        }
        else
        {
          Gu.Log.Error("Failed to create font " + this.GetName());
          break;
        }
        charsize /= 2;

        if (imageWidth < 64 || imageHeight < 64 || charsize < 8)
        {
          break;
        }
      }
    }
    private byte[] ConstructBitmapForSize(int img_width, int img_height, int img_padding, float charsize, StbTrueTypeSharp.StbTrueType.stbtt_packedchar[] charInfo)
    {
      byte[] atlasData = new byte[img_width * img_height];
      unsafe
      {
        StbTrueTypeSharp.StbTrueType.stbtt_pack_context context = new StbTrueTypeSharp.StbTrueType.stbtt_pack_context();

        fixed (byte* atlasData_pinned = atlasData)
        {
          StbTrueTypeSharp.StbTrueType.stbtt_PackSetOversampling(context, _oversampleX, _oversampleY);
          int ret = StbTrueTypeSharp.StbTrueType.stbtt_PackBegin(context, atlasData_pinned, img_width, img_height, img_width, img_padding, null);

          if (ret == 0)
          {
            Gu.Log.Error("Failed to begin font pack.");
            Gu.DebugBreak();
            atlasData = null;
          }
          else
          {
            fixed (StbTrueTypeSharp.StbTrueType.stbtt_packedchar* charInfo_pinned = charInfo)
            {
              fixed (byte* _pFontBuffer_pinned = _pFontBuffer)
              {
                ret = StbTrueTypeSharp.StbTrueType.stbtt_PackFontRange(context, _pFontBuffer_pinned, 0, charsize, _firstChar, _charCount, charInfo_pinned);
              }
            }
            if (ret == 0)
            {
              Gu.Log.Error("Failed to pack font size:" + charsize);
              Gu.DebugBreak();
              atlasData = null;
            }
            StbTrueTypeSharp.StbTrueType.stbtt_PackEnd(context);
          }
        }
      }
      return atlasData;
    }
    public int GetBitmapUsedHeight(StbTrueTypeSharp.StbTrueType.stbtt_packedchar[] charinfos, int image_Width, int image_height)
    {
      int maxHeight = 1;
      StbTrueTypeSharp.StbTrueType.stbtt_aligned_quad stbQuad;

      for (int cCode = _firstChar; cCode <= this._charCount; cCode++)
      {
        unsafe
        {
          float curX = 0, curY = 0;
          fixed (StbTrueTypeSharp.StbTrueType.stbtt_packedchar* charinfo_pt = charinfos)
          {
            StbTrueTypeSharp.StbTrueType.stbtt_GetPackedQuad(charinfo_pt, image_Width, image_height, cCode, &curX, &curY, &stbQuad, 0);
          }
          float quadMinY = stbQuad.t0 * image_height;
          float quadMaxY = stbQuad.t1 * image_height;

          maxHeight = (int)Math.Max((float)maxHeight, Math.Max(quadMinY, quadMaxY));
        }
      }
      return maxHeight + 1;
    }
    public byte[] TrimAtlasBitmapToUsedHeight(int imageWidth, int usedHeight, byte[] atlasData){
      byte[] newatlas = new byte[usedHeight * imageWidth];
      System.Buffer.BlockCopy(atlasData, 0, newatlas, 0, newatlas.Length);
      return newatlas;
    }
    public Img32 CreateFontImage(byte[] pData, int image_width, int image_height, StbTrueTypeSharp.StbTrueType.stbtt_packedchar[] charInfo)
    {
      //Copied from fontspec
      byte[] imgData = new byte[image_width * image_height * 4];
      if (charInfo == null)
      {
        Gu.DebugBreak();
      }

      for (int iPix = 0; iPix < image_width * image_height * 4; iPix += 4)
      {
        byte dat = pData[iPix / 4];
        imgData[iPix + 0] = 255;  //r
        imgData[iPix + 1] = 255;  //g
        imgData[iPix + 2] = 255;  //b
        imgData[iPix + 3] = dat;  //a
      }

      Img32 img = new Img32();
      img.init(image_width, image_height, imgData, Img32.PixelFormat.RGBA);

      return img;
    }
    public FontPatchInfo SelectFontPatchInfo(float fontSize)
    {
      //Gets the closest font patch given the input size.
      //This is similar to mipmapping, but it works in case we can't filter the texture.
      Gu.Assert(_fontPatchInfos != null);
      if (_bInitialized == false)
      {
        Gu.Log.Error("Font was not initialized.");
        Gu.DebugBreak();
        return null;
      }
      //Given the pixel
      if (_fontPatchInfos.Count == 0)
      {
        return null;
      }
      FontPatchInfo last = _fontPatchInfos[0];
      if (fontSize >= last.BakedCharSize)
      {
        return last;
      }
      foreach (var inf in _fontPatchInfos)
      {
        if (fontSize <= last.BakedCharSize && fontSize >= inf.BakedCharSize)
        {
          return inf;
        }
      }
      return _fontPatchInfos[_fontPatchInfos.Count - 1];
    }
    public float GetKernAdvanceWidth(FontPatchInfo patchInfo, float fontSize, int cCodePrev, int cCode)
    {
      //Get an additional width to add or subtract for kerning.
      float fKern = 0.0f;
      if (cCodePrev >= 0)
      {
        int kern = StbTrueTypeSharp.StbTrueType.stbtt_GetCodepointKernAdvance(_fontInfo, cCode, cCodePrev);
        fKern = (float)kern * patchInfo.ScaleForPixelHeight;
        fKern *= patchInfo.GetScaleForPixelSize(fontSize);
      }
      return fKern;
    }

    public void CachePatchChars(FontPatchInfo patchInfo)
    {
      //This routine caches this, basically avoiding unsafe{} calls to STB functions.
      //The speedup is: TODO: post FPS gains

      //Details / Notes:
      //Our goal is to create a label where all glyphs have the same outer box
      //       bearing
      //      <----|--------->advance
      //             y1
      //      |----A------- |
      //      |    A  pad   |
      //      |   -A---     |  
      //  x0  |   | C |pad  | x1
      //      |   ---D-     |  
      //      |      D  pad |
      //      |------D-------y0
      //             y0
      //Taking the MAXIMUM ascent and descent (A, D) of the font (getfontVmetrics).
      //C is the center of the character. The origin of a character is the very center.
      //Then we add padding to the character based on the difference between the MAX ascent or descent, minus the coordinates of the returned quad.
      //Ideally, we'd use the maximum ascent or descent of a given STRING and not the WHOLE FONT, however
      //this is easier for now.

      if (patchInfo.CharInfo == null)
      {
        return;
      }

      StbTrueTypeSharp.StbTrueType.stbtt_aligned_quad stbQuad;

      for (int cCode = patchInfo.FirstChar; cCode < patchInfo.CharCount; cCode++)
      {
        float curX = 0, curY = 0;  //Dummies
        unsafe
        {
          fixed (StbTrueTypeSharp.StbTrueType.stbtt_packedchar* charinfo_pt = patchInfo.CharInfo)
          {
            StbTrueTypeSharp.StbTrueType.stbtt_GetPackedQuad(charinfo_pt, patchInfo.TextureWidth, patchInfo.TextureHeight, cCode - _firstChar, &curX, &curY, &stbQuad, 0);
          }
        }
        if (GetTexs().Count == 0 || GetTexs().Count <= patchInfo.TextureIndex)
        {
          //You didn't save the image
          Gu.Log.Error("Failure to save font image somewhere.");
          Gu.DebugBreak();
          return;
        }

        var imagePatch = GetTexs()[patchInfo.TextureIndex];

        //Scale the returned texcoodrs from [0,1] to the width of the baked texture
        float tw = imagePatch.uv1.x - imagePatch.uv0.x;  //top left, origin
        float th = imagePatch.uv1.y - imagePatch.uv0.y;  //This is flipped; We are in OpenGL tcoords, however our origin is at the top left

        CachedCharData ccd = new CachedCharData();

        ccd.code = cCode;

        //Scale
        float dv = stbQuad.t1 - stbQuad.t0;
        float du = stbQuad.s1 - stbQuad.s0;
        ccd.uv0.x = imagePatch.uv0.x + (stbQuad.s0) * tw;
        ccd.uv0.y = imagePatch.uv0.y + (stbQuad.t0) * th;  //Bottom-left = uv1
        ccd.uv1.x = imagePatch.uv0.x + (stbQuad.s1) * tw;
        ccd.uv1.y = imagePatch.uv0.y + (stbQuad.t1) * th;

        //Compute the padding between characters
        //Bearing and Width are in RAW units,
        //we must convert to BakedChar units, THEN into EM units.
        int advWidth, bearing;
        unsafe
        {
          StbTrueTypeSharp.StbTrueType.stbtt_GetCodepointHMetrics(_fontInfo, cCode, &advWidth, &bearing);
        }
        ccd.width = (stbQuad.x1 - stbQuad.x0); //the stb x0, x1 seem to be in direct pixel coordinates
        ccd.height = (stbQuad.y1 - stbQuad.y0);
        ccd.margin_right = (float)advWidth * patchInfo.ScaleForPixelHeight;// advanceWidth is the offset from the current horizontal position to the next horizontal position
        ccd.margin_left = (float)bearing * patchInfo.ScaleForPixelHeight;// leftSideBearing is the offset from the current horizontal position to the left edge of the character
        ccd.margin_bot = (float)_descent * patchInfo.ScaleForPixelHeight;
        ccd.margin_top = (float)_ascent * patchInfo.ScaleForPixelHeight;

        patchInfo.CachedChars.Add(cCode, ccd);
      }
    }
    public override void AfterCompile()
    {
      foreach (var fontpatch in _fontPatchInfos)
      {
        CachePatchChars(fontpatch);
      }
    }
  }//MtFont

  #endregion
  #region MegaTex

  public class MegaTex
  {
    public enum MegaTexCompileState
    {
      NotCompiled,
      Dirty,
      Compiling,
      Compiled
    }
    public class CompiledTextures
    {
      public Texture2D Albedo = null;
      public Texture2D Normal = null;
    }
    public enum MtClearColor
    {
      White,
      BlackNoAlpha,
      DebugRainbow//create a rainbow of colors, for debugging
    }

    private Dictionary<string, MtTexPatch> _mapTexs = new Dictionary<string, MtTexPatch>();
    private int _iStartWH = 256;
    private int _iGrowWH = 128;
    private int _iMaxTexSize = 0;
    private MtNode _pRoot = null;
    private MegaTexCompileState _eState = MegaTexCompileState.NotCompiled;
    private bool _bCache = false;
    private static UInt64 genId = 0;
    public MtTex DefaultPixel = null;
    public string Name { get; private set; } = "";

    public MegaTex(string name, bool bCache, int defaultRegionSize = 0) //: Texture2D(name, TextureFormat::Image4ub, ctx)
    {
      //@param defaultRegionSize - Add a default white region for rendering solid colors. 0=disable.
      _bCache = bCache;
      if (defaultRegionSize > 0)
      {
        //Note: Default region will get skewed if texture filtering is enabled.
        var tp = GetTex(new Img32(defaultRegionSize, defaultRegionSize, Enumerable.Repeat((byte)255, defaultRegionSize * defaultRegionSize * 4).ToArray(), Img32.PixelFormat.RGBA));
        DefaultPixel = tp.GetTexs()[0];
      }
      Name = name;
    }
    public MtFont GetFont(FileLoc img)
    {
      MtTexPatch ret = null;
      _mapTexs.TryGetValue(img.QualifiedPath, out ret);
      if (ret == null)
      {
        _eState = MegaTexCompileState.Dirty;
        MtFont mtf = new MtFont(this, img);
        _mapTexs.Add(img.QualifiedPath, mtf);// .insert(std::make_pair(h, mtf));
        _eState = MegaTexCompileState.Dirty;

        _mapTexs.TryGetValue(img.QualifiedPath, out ret);
      }

      MtFont ft = ret as MtFont;
      return ft;
    }
    public MtTexPatch GetTex(Img32 tx)
    {
      string genName = "|gen-" + genId++;
      MtTexPatch p = GetTex(new FileLoc(genName, FileStorage.Embedded), 1, true);
      if (p != null && p.GetTexs().Count > 0)
      {
        p.GetTexs()[0].SetImg(tx);
      }
      else
      {
        Gu.Log.Error("Failed to add texture 23458242");
        Gu.DebugBreak();
      }

      return p;
    }
    public MtTexPatch GetTex(FileLoc img, int nPatches = 1, bool bPreloaded = false, bool bLoadNow = false)
    {
      Gu.Assert(nPatches > 0);

      if (bPreloaded == false)
      {
        if (!img.Exists)
        {
          Gu.Log.Error("Image file " + img.QualifiedPath + " did not exist when compiling MegaTex.");
          return null;
        }
      }

      MtTexPatch? ret = null;
      _mapTexs.TryGetValue(img.QualifiedPath, out ret);
      if (ret == null)
      {
        ret = new MtTexPatch(this, img);
        for (int i = 0; i < nPatches; ++i)
        {
          ret.AddTexImage(img, i);  //we could do "preloaded' as a bool, but it's probably nto necessary
        }
        _mapTexs.Add(img.QualifiedPath, ret);
        _eState = MegaTexCompileState.Dirty;

        if (bLoadNow)
        {
          ret.LoadData();
        }
        _mapTexs.TryGetValue(img.QualifiedPath, out ret);
      }

      ////**MUST return nPatches number of textures, never return a different number
      if (ret == null)
      {
        Gu.Log.Error("Could not find MegaTex Texture " + img.QualifiedPath);
        Gu.DebugBreak();
      }
      else if (ret.GetTexs().Count != nPatches)
      {
        Gu.Log.Error("Failed to return an appropriate number of texture patches.");
        Gu.DebugBreak();
      }
      return ret;
    }
    public void LoadImages()
    {
      Gu.Log.Info("Mega Tex: Loading " + _mapTexs.Count + " images.");

      foreach (var p in _mapTexs)
      {
        MtTexPatch mtt = p.Value;
        mtt.LoadData();
      }

      //_bImagesLoaded = true;
    }
    public CompiledTextures Compile(MtClearColor clearColor = MtClearColor.BlackNoAlpha, bool mipmaps = false, TexFilter filter = TexFilter.Nearest)
    {
      Img32 master_albedo = null, master_normal = null;

      //Images should be loaded here with loadImages()
      //This is required because we use images sizes when constructing the gui
      _eState = MegaTexCompileState.Compiling;

      //Flatten Patches into individual images
      Gu.Log.Debug("Mega Tex: Flattening " + _mapTexs.Count + " images.");
      List<MtTex> vecTexs = new List<MtTex>();
      foreach (var texPair in _mapTexs)
      {
        foreach (var texx in texPair.Value.GetTexs())
        {
          vecTexs.Add(texx);
        }
      }

      //Sort by wh - speeds up + saves room
      Gu.Log.Debug("MegaTex - Mega Tex: Sorting " + vecTexs.Count + ".");
      //struct {
      //  bool operator()(MtTex a, MtTex b) const {
      //    float f1 = a.getWidth() * a.getHeight();
      //       float f2 = b.getWidth() * b.getHeight();
      //    return f1 > f2;
      //  }
      // }
      // customLess;

      //replaces std::sort(vecTexs.begin(), vecTexs.end(), customLess);
      vecTexs.Sort((a, b) =>
      {
        float f1 = a.GetWidth() * a.GetHeight();
        float f2 = b.GetWidth() * b.GetHeight();
        return f1.CompareTo(f2);
      });

      //Tex size

      GL.GetInteger(GetPName.MaxTextureSize, out _iMaxTexSize);// glGetIntegerv(GL_MAX_TEXTURE_SIZE, (GLint*)&_iMaxTexSize);
      Gpu.CheckGpuErrorsRt();

      int iImageSize = _iStartWH;
      int nFailures = 0;

      //Expand rect and grow by 128 if we fail,  this is a pretty quick procedure, so we
      //don't have to worry about sizes.
      Gu.Log.Debug("MegaTex - Making space for " + vecTexs.Count + " texs.");
      while (iImageSize <= _iMaxTexSize)
      {
        //Root
        _pRoot = new MtNode();
        _pRoot._b2Rect._min = new ivec2(0, 0);
        _pRoot._b2Rect._max = new ivec2(iImageSize, iImageSize);

        //Do your thing
        bool bSuccess = true;

        foreach (MtTex texx in vecTexs)
        {
          if (texx.Img() != null)
          {
            MtNode found = _pRoot.plop(texx);
            if (found != null)
            {
              texx.SetNode(found);
            }
            else
            {
              //Failure, try larger size
              iImageSize = iImageSize + _iGrowWH;
              nFailures++;
              bSuccess = false;
              break;
            }
          }
          else
          {
            //Error
            //loadImages() wasn't called
            Gu.DebugBreak();
          }
        }

        if (bSuccess == true)
        {
          break;
        }
      }

      if (iImageSize > _iMaxTexSize)
      {
        //Failure
        Gu.Log.Error("MegaTex - Failed to compose mega texture, too many textures and not enough texture space.");
      }
      else
      {
        Gu.Log.Debug("MegaTex - Successful. Tex size=" + iImageSize + ".. Creating Bitmap..");

        //Compose Master Image
        master_albedo = new Img32();
        int datSiz = iImageSize * iImageSize * 4;

        Byte[] pData = null;
        if (clearColor == MtClearColor.BlackNoAlpha)
        {
          pData = Enumerable.Repeat((byte)0, datSiz).ToArray();//memset,0
        }
        else if (clearColor == MtClearColor.White)
        {
          pData = Enumerable.Repeat((byte)255, datSiz).ToArray();//memset,0
        }
        else if (clearColor == MtClearColor.DebugRainbow)
        {
          vec3 blue = new vec3(0, 0, 1);
          vec3 red = new vec3(1, 0, 0);
          vec3 green = new vec3(0, 1, 0);
          vec3 yellow = new vec3(1, 1, 0);
          //biliear interp
          //blue-->red
          //green-->yellow

          pData = Enumerable.Repeat((byte)0, datSiz).ToArray();//memset,0
          for (var yi = 0; yi < iImageSize; yi++)
          {
            for (var xi = 0; xi < iImageSize; xi++)
            {
              float fx = (float)xi / (float)iImageSize;
              float fy = (float)yi / (float)iImageSize;
              vec3 br = blue + (red - blue) * fx;
              vec3 gy = green + (yellow - green) * fx;
              vec3 brgy = br + (gy - br) * fy;

              pData[(yi * iImageSize + xi) * 4 + 0] = (byte)(brgy.x * 255.0); //BRGA .. wtf
              pData[(yi * iImageSize + xi) * 4 + 1] = (byte)(brgy.y * 255.0);
              pData[(yi * iImageSize + xi) * 4 + 2] = (byte)(brgy.z * 255.0);
              pData[(yi * iImageSize + xi) * 4 + 3] = 255;
            }
          }
        }

        master_albedo.init(iImageSize, iImageSize, pData, Img32.PixelFormat.RGBA);

        //delete[] pData;

        float imgW = (float)iImageSize;
        float imgH = (float)iImageSize;

        Gu.Log.Debug("MegaTex - Copying Sub-Images..and calculating tex coords");
        foreach (MtTex texx in vecTexs)
        {
          master_albedo.copySubImageFrom(texx.Node()._b2Rect._min, new ivec2(0, 0), new ivec2(texx.GetWidth(), texx.GetHeight()), texx.Img());
          Gpu.CheckGpuErrorsDbg();

          texx.uv0 = new vec2(
            (float)texx.Node()._b2Rect._min.x / imgW,
           (float)texx.Node()._b2Rect._min.y / imgH);
          texx.uv1 = new vec2(
            (float)texx.Node()._b2Rect._max.x / imgW,
           (float)texx.Node()._b2Rect._max.y / imgH);

          //Free the image and node, we don't need it
          texx.FreeTmp();
        }
        if (_bCache)
        {
          string imgName = System.IO.Path.Combine(Gu.LocalCachePath, "mt_" + Name + "_albedo.png");
          ResourceManager.SaveImage(imgName, master_albedo);
        }
        else
        {
          Gu.Log.Debug("MegaTex caching is disabled for this texture '" + "no name bo9y" + "'.");
        }
      }

      CompiledTextures output = new CompiledTextures();
      if (master_albedo != null)
      {
        Gu.Log.Debug("MegaTex - Creating Albedo Map.");
        output.Albedo = new Texture2D(master_albedo, mipmaps, filter);

        Gu.Log.Debug("MegaTex - Creating Normal Map.");
        master_normal = master_albedo.createNormalMap();
        string nmapname_dbg = System.IO.Path.Combine(Gu.LocalCachePath, "mt_" + Name + "_normal.png");
        ResourceManager.SaveImage(nmapname_dbg, master_normal);
        output.Normal = new Texture2D(master_normal, mipmaps, filter);
      }

      _eState = MegaTexCompileState.Compiled;

      foreach (var patch in _mapTexs)
      {
        patch.Value.AfterCompile();
      }

      return output;
    }
    void Update()
    {
      if (_eState == MegaTexCompileState.Dirty || _eState == MegaTexCompileState.NotCompiled)
      {
        LoadImages();
        Compile();
      }
    }
  }

  #endregion

}
