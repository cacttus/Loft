using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;

namespace PirateCraft
{
  using Hash32 = Int32;
  public enum MtTexType
  {
    Image,
    Font
  }
  /**
  *  @class MtNode
  *  @brief Node in the MegaTexture class.
  */
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

        if (tex.getHeight() > bh)
        {
          return null;
        }
        if (tex.getWidth() > bw)
        {
          return null;
        }
        if (tex.getWidth() == bw && tex.getHeight() == bh)
        {
          //prefect fit
          _pTex = tex;
          return this;
        }

        _pChild[0] = new MtNode();
        _pChild[1] = new MtNode();

        int dw = bw - tex.getWidth();
        int dh = bh - tex.getHeight();

        if (dw > dh)
        {
          _pChild[0]._b2Rect.Construct(
              _b2Rect.left(),
              _b2Rect.top(),
              _b2Rect.left() + tex.getWidth(),
              _b2Rect.bottom());
          _pChild[1]._b2Rect.Construct(
              _b2Rect.left() + tex.getWidth(),
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
              _b2Rect.top() + tex.getHeight());
          _pChild[1]._b2Rect.Construct(
              _b2Rect.left(),
              _b2Rect.top() + tex.getHeight(),
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
    MtNode _pMtNode = null;  //mega texture node
    Img32 _pImg = null;
    int _iWidth = 0;
    int _iHeight = 0;
    float _fSizeRatio = 0;
    vec2 _uv_p0, _uv_p1;
    FileLoc _strImgName;
    int _iPatchImg = 0;  //0-8 for 9p, or 0-2 for 3p

    public MtTex(FileLoc imgName, int iPatch)
    {
      _strImgName = imgName;
      _iPatchImg = iPatch;
    }
    public FileLoc imgName() { return _strImgName; }
    public int getWidth() { return _iWidth; }
    public int getHeight() { return _iHeight; }
    public float getSizeRatio() { return _fSizeRatio; }
    public vec2 uv0 { get { return _uv_p0; } set { _uv_p0 = value; } } // Bottom left,in OpenGL coordinates, from [0,1] 
    public vec2 uv1 { get { return _uv_p1; } set { _uv_p1 = value; } } // Top Right [0,1]
    public MtNode node() { return _pMtNode; }        //mega texture node
    public void setNode(MtNode n) { _pMtNode = n; }  //mega texture node
    public Img32 img() { return _pImg; }

    public void setImg(Img32 img)
    {
      _pImg = img;
      //We don't save the img data on the CPU, so just store what we need
      _iHeight = img.Height;
      _iWidth = img.Width;
      _fSizeRatio = (float)_iWidth / (float)_iHeight;
    }
    public void freeTmp()
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
    public void addTexImage(FileLoc img, int iPatch)
    {
      MtTex mt = new MtTex(img, iPatch);
      Gpu.CheckGpuErrorsDbg();
      //_pMegaTex.getContext().chkErrDbg();
      _vecTexs.Add(mt);
    }
    public virtual void loadData()
    {
      if (_vecTexs.Count() == 0)
      {
        Gu.Log.Error("Image patch was invalid for " + GetName() + ".");
      }
      else if (_vecTexs.Count() == 1)
      {
        if (_vecTexs[0].img() == null)
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
            _vecTexs[0].setImg(img);
          }
        }
      }
      else
      {
        List<Img32> imgs = parseImagePatch(GetName());
        if (imgs.Count != _vecTexs.Count)
        {
          Gu.Log.Error("Tex Count Mismatch, or texture not found for '" + GetName() + "'.");
          Gu.DebugBreak();
        }
        else
        {
          for (int i = 0; i < imgs.Count; ++i)
          {
            _vecTexs[i].setImg(imgs[i]);
          }
        }
      }
    }
    private List<Img32> parseImagePatch(FileLoc file)
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
  }
  #endregion
  #region MtFont
  public class MtFont : MtTexPatch
  {
    int _iBakedCharSizePixels = 40;
    int _fontTextureWidth = 1024;
    int _fontTextureHeight = 1024;
    uint _oversampleX = 2;
    uint _oversampleY = 2;
    int _firstChar = ' ';
    int _charCount = '~' - ' ';
    float _fAscent = 0;
    float _fDescent = 0;
    float _fLineGap = 0;
    StbTrueTypeSharp.StbTrueType.stbtt_packedchar[] _charInfo;
    StbTrueTypeSharp.StbTrueType.stbtt_fontinfo _fontInfo;
    float _fScaleForPixelHeight;               //return value of stbtt_ScaleForPixelHeight
    byte[] _pFontBuffer = null;  // STB:  "Load" a font file from a memory buffer (you have to keep the buffer loaded)
    bool _bInitialized = false;

    public MtFont(MegaTex mt, FileLoc loc) : base(mt, loc)
    {
    }
    public override void loadData()
    {
      _iBakedCharSizePixels = Gu.EngineConfig.BakedCharSize;// Gu::getConfig().getBakedCharSize();
      _fontTextureWidth = Gu.EngineConfig.FontBitmapSize;// Gu::getConfig().getFontBitmapSize();
      _fontTextureHeight = Gu.EngineConfig.FontBitmapSize;

      Gu.Log.Info("Creating font '" + GetName() + "'. size=" + _fontTextureWidth + "x" + _fontTextureHeight + ".  Baked Char Size =" + _iBakedCharSizePixels);

      //_pFontBuffer = std::make_shared<BinaryFile>("<none>");
      //if (Gu::getPackage().getFile(getName(), _pFontBuffer) == false)
      //{
      //  Gu.Log.Error("Failed to get font file '" + getName() + "'");
      //  Gu.DebugBreak();
      //  return;
      //}

      //byte[] bytes = null;
      using (var s = GetName().GetStream())
      using (var ms = new MemoryStream())
      {
        s.CopyTo(ms);
        _pFontBuffer = ms.ToArray();
      }
      //StbTrueTypeSharp.StbTrueType.stbtt_fontinfo inf = null;
      if (_pFontBuffer != null)
      {
        _fontInfo = StbTrueTypeSharp.StbTrueType.CreateFont(_pFontBuffer, 0);
      }

      //The Api seems to recommend CreateFont instead of calling the native
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

      //Get soem font metrics
      //stbtt_InitFont(&_fontInfo, (const unsigned char*)_pFontBuffer.getData().ptr(), 0);
      _fScaleForPixelHeight = StbTrueTypeSharp.StbTrueType.stbtt_ScaleForPixelHeight(_fontInfo, (float)_iBakedCharSizePixels);

      int ascent, descent, lineGap;
      unsafe
      {
        StbTrueTypeSharp.StbTrueType.stbtt_GetFontVMetrics(_fontInfo, &ascent, &descent, &lineGap);
      }
      _fAscent = (float)ascent * _fScaleForPixelHeight;
      _fDescent = (float)descent * _fScaleForPixelHeight;
      _fLineGap = (float)lineGap * _fScaleForPixelHeight;

      if (false)
      {
        string x = "Hello ";
        foreach (int c in x)
        {
          int adv, be;
          unsafe
          {
            StbTrueTypeSharp.StbTrueType.stbtt_GetGlyphHMetrics(_fontInfo, c, &adv, &be);
          }
          float fa = (float)adv * _fScaleForPixelHeight;
          float fb = (float)be * _fScaleForPixelHeight;
          int nn = 0;
          nn++;
        }
      }

      //pack the image into a bitmap **nice version**
      //std::unique_ptr<uint8_t[]> atlasData = std::make_unique<uint8_t[]>(_atlasWidth * _atlasHeight);
      //_charInfo = std::make_unique<stbtt_packedchar[]>(_charCount);

      //Img32 atlasData = new Img32(_atlasWidth, _atlasHeight);
      byte[] atlasData = new byte[_fontTextureWidth * _fontTextureHeight];
      _charInfo = new StbTrueTypeSharp.StbTrueType.stbtt_packedchar[_charCount];

      StbTrueTypeSharp.StbTrueType.stbtt_pack_context context = new StbTrueTypeSharp.StbTrueType.stbtt_pack_context();
      int padding = 1;
      int ret = 0;
      unsafe
      {
        fixed (byte* atlasData_pinned = atlasData)
        {
          ret = StbTrueTypeSharp.StbTrueType.stbtt_PackBegin(context, atlasData_pinned, _fontTextureWidth, _fontTextureHeight, 0, padding, null);

          if (ret == 0)
          {
            Gu.Log.Error("Failed to initialize font");
            Gu.DebugBreak();
            return;
          }

          StbTrueTypeSharp.StbTrueType.stbtt_PackSetOversampling(context, _oversampleX, _oversampleY);
          fixed (StbTrueTypeSharp.StbTrueType.stbtt_packedchar* _charInfo_pinned = _charInfo)
          {
            fixed (byte* _pFontBuffer_pinned = _pFontBuffer)
            {
              ret = StbTrueTypeSharp.StbTrueType.stbtt_PackFontRange(context, _pFontBuffer_pinned, 0,
                (float)_iBakedCharSizePixels, _firstChar, _charCount, _charInfo_pinned);
            }
          }
          if (ret == 0)
          {
            Gu.Log.Error("Failed to pack font");
            Gu.DebugBreak();
            return;
          }

          StbTrueTypeSharp.StbTrueType.stbtt_PackEnd(context);
        }
      }

      //Set the megatex image.
      Img32 img = createFontImage(atlasData);
      if (false)
      {
        Gu.Log.Info("Saving " + System.IO.Path.GetFileName(GetName().QualifiedPath) + "...");
        string imgName = "./data/cache/dbg_font_" + System.IO.Path.GetFileName(GetName().QualifiedPath) + ".png";
        ResourceManager.SaveImage(imgName, img);
      }
      MtTex mt = new MtTex(GetName(), 0);
      mt.setImg(img);
      GetTexs().Add(mt);

      _bInitialized = true;
    }
    public Img32 createFontImage(byte[] pData)
    {
      //Copied from fontspec
      byte[] imgData = new byte[_fontTextureWidth * _fontTextureHeight * 4];
      if (_charInfo == null)
      {
        Gu.DebugBreak();
      }

      for (int iPix = 0; iPix < _fontTextureWidth * _fontTextureHeight * 4; iPix += 4)
      {
        byte dat = pData[iPix / 4];
        imgData[iPix + 0] = 255;  //r
        imgData[iPix + 1] = 255;  //g
        imgData[iPix + 2] = 255;  //b
        imgData[iPix + 3] = dat;  //a
      }

      Img32 img = new Img32();
      img.init(_fontTextureWidth, _fontTextureHeight, imgData);

      //Stb creates the image upside-down for OpenGL, h owever in our GUi
      //we create it right-side up, then store it upside down, so we need to flip it rightise up first.
      //this also helps us see it when we cache it
      //img.flipV();

      return img;
    }

    float getKernAdvanceWidth(float fontSize, int cCodePrev, int cCode)
    {
      //Get an additional width to add or subtract for kerning.
      float fKern = 0.0f;
      if (cCodePrev >= 0)
      {
        int kern = StbTrueTypeSharp.StbTrueType.stbtt_GetCodepointKernAdvance(_fontInfo, cCode, cCodePrev);
        fKern = (float)kern * _fScaleForPixelHeight;
        fKern *= fontSizeToFontScale(fontSize);
      }
      return fKern;
    }
    public void getCharQuad(int cCode, float fontSize, ref float outWidth, ref float outHeight, ref Box2f texs,
                             ref float padTop, ref float padRight, ref float padBot, ref float padLeft)
    {
      //The return of this function is the information needed to create a 3D quad

      StbTrueTypeSharp.StbTrueType.stbtt_aligned_quad stbQuad;
      Box2f worldQuad;
      if (_bInitialized == false)
      {
        Gu.Log.Error("Font was not initialized.");
        Gu.DebugBreak();
        return;
      }
      if (_charInfo == null)
      {
        return;
      }
      if (cCode < _firstChar || cCode >= (_firstChar + _charCount))
      {
        //char is an invalid character such as a newline.
        //Process it as a space
        cCode = ' ';
      }

      float curX = 0, curY = 0;  //Dummies
      unsafe
      {
        fixed (StbTrueTypeSharp.StbTrueType.stbtt_packedchar* charinfo_pt = _charInfo)
        {
          StbTrueTypeSharp.StbTrueType.stbtt_GetPackedQuad(charinfo_pt, _fontTextureWidth, _fontTextureHeight, cCode - _firstChar, &curX, &curY, &stbQuad, 0);
        }
      }
      if (GetTexs().Count == 0)
      {
        //You didn't save the image
        Gu.Log.Error("Failure to save font image somewhere.");
        Gu.DebugBreak();
        return;
      }

      //**TExs
      //Scale hte returned texcoodrs from [0,1] to the width of the baked texture
      float tw = GetTexs()[0].uv1.x - GetTexs()[0].uv0.x;  //top left, origin
      float th = GetTexs()[0].uv0.y - GetTexs()[0].uv1.y;  //This is flipped; We are in OpenGL tcoords, however our origin is at the top left

      //Scale
      float dv = stbQuad.t1 - stbQuad.t0;
      float du = stbQuad.s1 - stbQuad.s0;
      vec2 uv0, uv1;
      uv0.x = GetTexs()[0].uv0.x + stbQuad.s0 * tw;
      uv0.y = GetTexs()[0].uv1.y + stbQuad.t0 * th;  //Bottom-left = uv1
      uv1.x = GetTexs()[0].uv0.x + stbQuad.s1 * tw;
      uv1.y = GetTexs()[0].uv1.y + stbQuad.t1 * th;

      //Don't flip Y - we will do that in the regenmesh
      texs = new Box2f(uv0, uv1);
      //**End TExs

      //Debug - Save texture
      if (false)
      {
        //TODO: this is just some debug, but we can fix this in the future.
        //RenderUtils::saveTexture("./data/cache/saved_TEST.png", Gu::getActiveWindow().getGui().getTex().getGlId(), GL_TEXTURE_2D);
      }

      //**Pos
      //Transform quad by scale.  This is new - transorm the local quad only.  Not the whole text line.
      float fScale = fontSizeToFontScale(fontSize);
      outWidth = (stbQuad.x1 - stbQuad.x0) * fScale;
      outHeight = (stbQuad.y1 - stbQuad.y0) * fScale;

      //Position character horizontally
      //Compute the padding between characters
      int advWidth, bearing;
      float fAdvWidth, fBearing;
      unsafe
      {
        StbTrueTypeSharp.StbTrueType.stbtt_GetCodepointHMetrics(_fontInfo, cCode, &advWidth, &bearing);
      }
      fAdvWidth = (float)advWidth * _fScaleForPixelHeight;
      fBearing = (float)bearing * _fScaleForPixelHeight;
      fAdvWidth *= fScale;
      fBearing *= fScale;



      //Compute the glyph padding values, and spaceing
      //for some reason space has a negative x0
      padLeft = fBearing;               // leftSideBearing is the offset from the current horizontal position to the left edge of the character
      padRight = fAdvWidth - outWidth;  // advanceWidth is the offset from the current horizontal position to the next horizontal position

      //Position character vertically
      //The ascent + descent of the character is wherever the quad is above, or below zero (zero is the baseline, we pass it in with curY)
      //_fAscent adn _fDescent are the scaled MAXIMUM ascent + descent of the font.  So the math here is correct
      padBot = (Math.Abs(_fDescent) - Math.Abs(stbQuad.y1));  // usually negative
      padTop = (Math.Abs(_fAscent) - Math.Abs(stbQuad.y0));   //
      padBot *= fScale;
      padTop *= fScale;
    }
    public float fontSizeToFontScale(float fs)
    {
      //Incorrect but I'm just,, o
      //Dividing by _ascent gives us a larger font than the actual extent.
      return fs / (float)_iBakedCharSizePixels;
    }
  }
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

    private Dictionary<string, MtTexPatch> _mapTexs = new Dictionary<string, MtTexPatch>();
    private int _iStartWH = 256;
    private int _iGrowWH = 128;
    private int _iMaxTexSize = 0;
    private MtNode _pRoot = null;
    private MegaTexCompileState _eState = MegaTexCompileState.NotCompiled;
    private bool _bCache = false;
    private bool _bDefaultPixel = false;
    private static UInt64 genId = 0;
    public MtTex DefaultPixel = null;

    public MegaTex(string name, bool bCache, bool bAddDefaultPixel) //: Texture2D(name, TextureFormat::Image4ub, ctx)
    {
      //bAddDefaultPixel - add a 1x1 white pixel texture to this image.
      _bCache = bCache;
      if (bAddDefaultPixel)
      {
        var tp = GetTex(new Img32(1, 1, new byte[] { 255, 255, 255, 255 }));
        DefaultPixel = tp.GetTexs()[0];
      }
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
        p.GetTexs()[0].setImg(tx);
      }
      else
      {
        Gu.Log.Error("Failed to add texture 23458242");
        Gu.DebugBreak();
      }

      return p;
    }
    /**
    *  @fn getTex
    *  @brief Returns the given texture image by name, separated into patches / slices. Not case sensitive.
    *  @param nPatches - number of patches to expect - this is more of a debug thing to prevent invalid patches
    *  @param bPreloaded - if we already loaded the image (skips validation and texture coords)
    *  @param bLoadNow - Load the image immediately in this function (skips validation of texture coords)
    */
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
          ret.addTexImage(img, i);  //we could do "preloaded' as a bool, but it's probably nto necessary
        }
        _mapTexs.Add(img.QualifiedPath, ret);
        _eState = MegaTexCompileState.Dirty;

        if (bLoadNow)
        {
          ret.loadData();
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
        mtt.loadData();
      }

      //_bImagesLoaded = true;
    }
    public CompiledTextures Compile(bool flip_y_texture_coords = false)
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
        float f1 = a.getWidth() * a.getHeight();
        float f2 = b.getWidth() * b.getHeight();
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
          if (texx.img() != null)
          {
            MtNode found = _pRoot.plop(texx);
            if (found != null)
            {
              texx.setNode(found);
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
        Byte[] pData = Enumerable.Repeat((byte)0, datSiz).ToArray();//memset,0
        master_albedo.init(iImageSize, iImageSize, pData);
        //delete[] pData;

        float imgW = (float)iImageSize;
        float imgH = (float)iImageSize;

        Gu.Log.Debug("MegaTex - Copying Sub-Images..and calculating tex coords");
        foreach (MtTex texx in vecTexs)
        {
          master_albedo.copySubImageFrom(texx.node()._b2Rect._min, new ivec2(0, 0), new ivec2(texx.getWidth(), texx.getHeight()), texx.img());
          Gpu.CheckGpuErrorsDbg();

          //New Tex coords
          if (flip_y_texture_coords)
          {
            texx.uv0 = new vec2((float)texx.node()._b2Rect._min.x / imgW,
             (float)texx.node()._b2Rect._min.y / imgH);
            texx.uv1 = new vec2((float)texx.node()._b2Rect._max.x / imgW,
             (float)texx.node()._b2Rect._max.y / imgH);
          }
          else
          {
            texx.uv0 = new vec2((float)texx.node()._b2Rect._min.x / imgW,
               (float)texx.node()._b2Rect._max.y / imgH);
            texx.uv1 = new vec2((float)texx.node()._b2Rect._max.x / imgW,
             (float)texx.node()._b2Rect._min.y / imgH);  //*Note the Y flop - OpenGL
          }

          //Free the image and node, we don't need it
          texx.freeTmp();
        }
        if (_bCache)
        {
          string imgName = System.IO.Path.Combine(Gu.LocalCachePath, "ui_master.png");
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
        output.Albedo = new Texture2D(master_albedo, true, TexFilter.Nearest);

        Gu.Log.Debug("MegaTex - Creating Normal Map.");
        master_normal = master_albedo.createNormalMap();
        string nmapname_dbg = System.IO.Path.Combine(Gu.LocalCachePath, "ui_master_nm.png");
        ResourceManager.SaveImage(nmapname_dbg, master_normal);
        output.Normal = new Texture2D(master_normal, true, TexFilter.Nearest);
      }

      _eState = MegaTexCompileState.Compiled;

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
