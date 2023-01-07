using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;

namespace Loft
{
  public class MtNode
  {
    //BSP tree node
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
  public class MtTex
  {
    //A sub-region of a texture atlas
    public string Name { get; private set; } = Lib.UnsetName;
    public MtNode? Node { get; set; } = null;  //mega texture node
    public int ShrinkPixels { get; private set; } = 0;
    public vec2 uv0 { get; set; } = vec2.Zero;
    public vec2 uv1 { get; set; } = vec2.Zero;
    public UInt64 ImageHash { get; private set; } = 0;

    private Image? _pImg = null;
    private int _iWidth = 0;
    private int _iHeight = 0;
    private float _fSizeRatio = 0;
    private int _iPatchImg = 0;  //0-8 for 9p, or 0-2 for 3p //Basically this is if we split an image up into "patches". Probably not being used.

    public MtTex() { }
    public MtTex(string name, int iPatch, int shrinkPixelBorder)
    {
      Name = name;
      _iPatchImg = iPatch;
      ShrinkPixels = shrinkPixelBorder;
    }
    public int GetWidth() { return _iWidth; }
    public int GetHeight() { return _iHeight; }
    public float GetSizeRatio() { return _fSizeRatio; }
    public Image? Img() { return _pImg; }
    public void SetImg(Image img)
    {
      Gu.Assert(img != null);
      _pImg = img;
      //We don't save the img data on the CPU, so store what we need to generate texture coords.
      _iHeight = img.Height;
      _iWidth = img.Width;
      _fSizeRatio = (float)_iWidth / (float)_iHeight;
    }
    public void ClearImg()
    {
      //Note: we keep the image metadata around to compute megatexture information.
      _pImg = null;
    }
    public void FreeTmp()
    {
      _pImg = null;
      Node = null;
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
    public void ComputeImageHash()
    {
      ImageHash = 0;

      //Gets a hashcode of all the loaded images by FNV-ing the bytes.
      //Used to check if the generated images (or, even images, but use mod dates for those pls) hvae changed
      Gu.Assert(_pImg != null);
      Gu.Assert(_pImg.Data != null);
      ImageHash = Hash.HashByteArray(new List<byte[]> { _pImg.Data });
    }
    public void Serialize(BinaryWriter bw)
    {
      bw.Write((string)Name);
      bw.Write((Int32)ShrinkPixels);
      bw.Write((vec2)uv0);
      bw.Write((vec2)uv1);
      bw.Write((UInt64)ImageHash);
      bw.Write((Int32)_iWidth);
      bw.Write((Int32)_iHeight);
      bw.Write((Int32)_fSizeRatio);
      bw.Write((Int32)_iPatchImg);
    }
    public void Deserialize(BinaryReader br, SerializedFileVersion version)
    {
      Name = br.ReadString();
      ShrinkPixels = br.ReadInt32();
      uv0 = br.ReadVec2();
      uv1 = br.ReadVec2();
      ImageHash = br.ReadUInt64();
      _iWidth = br.ReadInt32();
      _iHeight = br.ReadInt32();
      _fSizeRatio = br.ReadInt32();
      _iPatchImg = br.ReadInt32();
    }
    public MtTex Clone()
    {
      MtTex ret = new MtTex();
      ret.Name = this.Name;
      ret.Node = null;
      ret.ShrinkPixels = this.ShrinkPixels;
      ret.uv0 = this.uv0;
      ret.uv1 = this.uv1;
      ret.ImageHash = this.ImageHash;
      ret._iWidth = this._iWidth;
      ret._iHeight = this._iHeight;
      ret._fSizeRatio = this._fSizeRatio;
      ret._iPatchImg = this._iPatchImg;
      return ret;
    }
  }//cls
  public class MtFile
  {
    //file that will be packed into texture, 
    //some files (such as fonts) can generate multiple textures
    public List<MtTex> Texs { get; private set; } = new List<MtTex>();
    public FileLoc FileLoc { get; private set; } = null;
    public DateTime LastWriteTime { get; protected set; } = DateTime.MinValue;
    public int Patches { get; private set; } = 1;
    public int ShrinkPixelBorder { get; private set; } = 0;

    public MtLoader Loader = null;

    public static Dictionary<Type, HashSet<string>> TypeAssociations = new Dictionary<Type, HashSet<string>>() {
      {typeof(MtImageLoader), new HashSet<string>(){".png",".tga",".bmp",".exr",".hdr",".hdri",".gif",".jpeg",".jpg"}},
      {typeof(MtFontLoader), new HashSet<string>(){".ttf",".otf"}},
      {typeof(MtGenLoader), new HashSet<string>(){MegaTex.GenExtension}},
    };

    public MtFile() { }
    public MtFile(FileLoc fileLoc, int patches, int shrinkPixelBorder)
    {
      FileLoc = fileLoc;
      LastWriteTime = FileLoc.GetLastWriteTime();
      Loader = null;
    }
    private MtLoader CreateLoaderFromType(Type loaderTYpe)
    {
      try
      {
        return (MtLoader)Activator.CreateInstance(loaderTYpe, this);//new Loader
      }
      catch (Exception ex)
      {
        Gu.Log.Error(ex.ToString());
        Gu.DebugBreak();
      }
      return null;
    }
    public void Serialize(BinaryWriter bw)
    {
      FileLoc.Serialize(bw);
      var writetime = FileLoc.GetLastWriteTime(true);
      bw.Write((DateTime)writetime);
      bw.Write((Int32)Texs.Count);
      bw.Write(LastWriteTime);
      foreach (var tx in Texs)
      {
        tx.Serialize(bw);
      }
      bw.Write((string)Loader.GetType().FullName);
      Loader.Serialize(bw);
    }
    public void Deserialize(BinaryReader br, SerializedFileVersion version)
    {
      //Deserialize the existing Mtcache data into the input resources.
      //Throw an exception if there was a noticable change in the resources:
      // **Generated files have different hashes
      // **Disk/Embedded files have later modification stamps.

      var tmp_fileloc = new FileLoc();
      tmp_fileloc.Deserialize(br, version);

      if (FileLoc != null)
      {
        if (!tmp_fileloc.Equals(FileLoc))
        {
          Gu.BRThrowException("File location mismatch.");
        }
        if (FileLoc.FileStorage == FileStorage.Disk || FileLoc.FileStorage == FileStorage.Embedded)
        {
          if (tmp_fileloc.GetLastWriteTime() < FileLoc.GetLastWriteTime())
          {
            Gu.BRThrowException($"File {tmp_fileloc.RawPath} was newer");
          }
        }
      }

      var writetime = br.ReadDateTime();
      if (tmp_fileloc.ExistsOnDisk())
      {
        if (tmp_fileloc.GetLastWriteTime(true) > writetime)
        {
          Gu.BRThrowException($"File {tmp_fileloc.RawPath} is newer.");
        }
      }

      FileLoc = tmp_fileloc;

      var texcount = br.ReadInt32();
      LastWriteTime = br.ReadDateTime();

      if (tmp_fileloc.FileStorage == FileStorage.Generated)
      {
        //Generated images must be set before checking megatex.
        if (this.Texs != null && this.Texs.Count != texcount)
        {
          Gu.BRThrowException($"{tmp_fileloc.RawPath}: Tex count mismatch");
        }
      }


      var tmp_texs = new List<MtTex>();
      for (int ti = 0; ti < texcount; ti++)
      {
        MtTex t = new MtTex();
        t.Deserialize(br, version);

        if (tmp_fileloc.FileStorage == FileStorage.Generated)
        {
          if (Texs[ti].ImageHash != t.ImageHash)
          {
            Gu.BRThrowException($"{tmp_fileloc.RawPath}: Generated file was not the same");
          }
        }

        tmp_texs.Add(t);
      }

      this.Texs = tmp_texs;//Set the whole fn' thing

      string loaderName = br.ReadString();

      Type typ = Type.GetType(loaderName);

      Loader = CreateLoaderFromType(typ);

      Gu.Assert(Loader != null);

      Loader.Deserialize(br, version);
    }
    public MtFile Clone()
    {
      MtFile ret = new MtFile();
      foreach (var t in this.Texs)
      {
        MtTex ttt = t.Clone();
        ret.Texs.Add(ttt);
      }
      ret.FileLoc = this.FileLoc.Clone();
      ret.LastWriteTime = this.LastWriteTime;
      ret.Patches = this.Patches;
      ret.ShrinkPixelBorder = this.ShrinkPixelBorder;
      //**Loader must not be set when we clone an MTFile - thisclone is currently only
      //for the changed filemod additions.
      Gu.Assert(Loader == null);
      return ret;
    }
    public void AfterCompile()
    {
      if (Loader != null)
      {
        Loader.AfterCompile();
      }
      else
      {
        Gu.Log.Error("Loader was not set for " + this.FileLoc.QualifiedPath);
      }
    }
    public void LoadData()
    {
      var ext = System.IO.Path.GetExtension(FileLoc.QualifiedPath);

      foreach (var p in TypeAssociations)
      {
        if (p.Value.Contains(ext))
        {
          Loader = CreateLoaderFromType(p.Key);
          break;
        }
      }

      if (Loader == null)
      {
        Gu.Log.Error("Failed to identify file type " + FileLoc.QualifiedPath);
        Gu.DebugBreak();
      }
      else
      {
        Loader.LoadData();
      }
    }
    public void UnloadData()
    {
      foreach (var t in Texs)
      {
        t.ClearImg();
      }
    }

  }//cls
  public abstract class MtLoader
  {
    //Loader class
    public MtFile MtFile { get { return _mtFile; } }
    protected MtFile _mtFile;
    public MtLoader(MtFile f)
    {
      _mtFile = f;
    }
    public abstract void LoadData();
    public abstract void AfterCompile();
    public virtual void Serialize(BinaryWriter bw)
    {
    }
    public virtual void Deserialize(BinaryReader br, SerializedFileVersion version)
    {
    }
  }//MtLoader
  public class MtImageLoader : MtLoader
  {
    //Load image files.
    public MtImageLoader(MtFile file) : base(file)
    {
    }
    public override void LoadData()
    {
      if (!MtFile.FileLoc.Exists)
      {
        Gu.Log.Error($"MTex: File '{MtFile.FileLoc.ToString()}' not found.");
        Gu.DebugBreak();
      }
      else
      {
        Image? img = Gu.Lib.GetOrLoadImage(MtFile.FileLoc);
        Gu.Assert(img != null);
        var tx = new MtTex(img.Name, 0, MtFile.ShrinkPixelBorder);
        MtFile.Texs.Add(tx);
        tx.SetImg(img);
      }
    }
    public override void AfterCompile()
    {
      //nop
    }
    public override void Serialize(BinaryWriter bw)
    {
      base.Serialize(bw);
    }
    public override void Deserialize(BinaryReader br, SerializedFileVersion version)
    {
      base.Deserialize(br, version);
    }
  }//cls
  public class MtGenLoader : MtLoader
  {
    //Pre-gen images.. or we could generate the image here.
    public MtGenLoader(MtFile file) : base(file)
    {
    }
    public override void LoadData()
    {
      //Gen image already loaded..
      //compute gen image hash to see if it changed..
      // foreach (var tx in MtFile.Texs)
      // {
      //   if (tx.Img != null)
      //   {
      //   }
      //   else
      //   {
      //     Gu.DebugBreak();//girls bothering me again
      //   }
      // }
    }
    public override void AfterCompile()
    {
      //nop
    }
    public override void Serialize(BinaryWriter bw)
    {
      base.Serialize(bw);
    }
    public override void Deserialize(BinaryReader br, SerializedFileVersion version)
    {
      base.Deserialize(br, version);
    }
  }//cls
  public class MtFontChar
  {
    //caches the result of stbtt_GetPackedChar
    //This is a cached char that has all its information pre-computed from stb
    public int patchTexture_Width;
    public int patchTexture_Height;
    public int code;
    public vec2 uv0;
    public vec2 uv1;
    public float ch_advance;
    public float ch_leftbearing;
    public float ch_top;
    public float ch_bot;
    public float ch_left;
    public float ch_right;

    public void Serialize(BinaryWriter bw)
    {
      bw.Write((Int32)patchTexture_Width);
      bw.Write((Int32)patchTexture_Height);
      bw.Write((Int32)code);
      bw.Write((vec2)uv0);
      bw.Write((vec2)uv1);
      bw.Write((Single)ch_advance);
      bw.Write((Single)ch_top);
      bw.Write((Single)ch_bot);
      bw.Write((Single)ch_left);
      bw.Write((Single)ch_right);
    }
    public void Deserialize(BinaryReader br, SerializedFileVersion version)
    {
      patchTexture_Width = br.ReadInt32();
      patchTexture_Height = br.ReadInt32();
      code = br.ReadInt32();
      uv0 = br.ReadVec2();
      uv1 = br.ReadVec2();
      ch_advance = br.ReadSingle();
      ch_top = br.ReadSingle();
      ch_bot = br.ReadSingle();
      ch_left = br.ReadSingle();
      ch_right = br.ReadSingle();
    }
  }//MtCachedCharData
  public class MtFontPatch
  {
    //A font patch is a mipmap of a given font (maximum BakedCharSize)
    //This is needed because automatic mipmapping does not work correctly for the UI. (scaling a single MaxBakedChar also causes artifacts)
    public float BakedCharSize { get; set; } = 0; //Pixel height of the characters
    public int TextureWidth { get; set; } = 0;
    public int TextureHeight { get; set; } = 0;
    public int TextureIndex { get; set; } = 0; //The patch index in the MtTexPatch
    public int FirstChar { get; set; } = ' ';
    public int CharCount { get; set; } = 0;
    public float ScaleForPixelHeight { get; set; } = 0;               //return value of stbtt_ScaleForPixelHeight
    public StbTrueTypeSharp.StbTrueType.stbtt_packedchar[] CharInfo = null;
    public Dictionary<int, MtFontChar> CachedChars = new Dictionary<int, MtFontChar>();

    public bool GetChar(int unicode_point, float fontSize, out MtFontChar ccd)
    {
      //Transform quad by STB scale.
      //The STB quad is in STB scaled units to the given BakedChar size, i.e. it is not in "raw" units
      //We must then transform it into EM 

      if (unicode_point < FirstChar || unicode_point >= (FirstChar + CharCount))
      {
        //char is an invalid character such as a newline.
        //Process it as a space
        unicode_point = ' ';
      }

      if (CachedChars.TryGetValue(unicode_point, out ccd))
      {
        return true;
      }
      else
      {
        return false;
      }
    }
    public float GetScaleForPixelSize(float ps)
    {
      float t = ps / (float)BakedCharSize;
      return t;
    }
    public void Serialize(BinaryWriter bw)
    {
      bw.Write((Single)BakedCharSize);
      bw.Write((Int32)TextureWidth);
      bw.Write((Int32)TextureHeight);
      bw.Write((Int32)TextureIndex);
      bw.Write((Int32)FirstChar);
      bw.Write((Int32)CharCount);

      bw.Write((Single)ScaleForPixelHeight);

      bw.Write<StbTrueTypeSharp.StbTrueType.stbtt_packedchar>(CharInfo);

      bw.Write((Int32)CachedChars.Count);
      foreach (var kvp in this.CachedChars)
      {
        bw.Write((Int32)kvp.Key);
        kvp.Value.Serialize(bw);
      }
    }
    public void Deserialize(BinaryReader br, SerializedFileVersion version)
    {
      BakedCharSize = br.ReadSingle();
      TextureWidth = br.ReadInt32();
      TextureHeight = br.ReadInt32();
      TextureIndex = br.ReadInt32();
      FirstChar = br.ReadInt32();
      CharCount = br.ReadInt32();

      ScaleForPixelHeight = br.ReadSingle();

      CharInfo = br.Read<StbTrueTypeSharp.StbTrueType.stbtt_packedchar>();
      var cc = br.ReadInt32();
      for (int ci = 0; ci < cc; ++ci)
      {
        Int32 k = br.ReadInt32();
        MtFontChar c = new MtFontChar();
        c.Deserialize(br, version);
        CachedChars.Add(k, c);
      }
    }
  }//MtFontPatch
  public class CharacterRangeUTF8
  {
    //Character ranges correspond to the chars we 'try' to fit on a single texture.
    //Han, for instance is massive, so we would split it among CharacterRanges. 
    //Also, most of UTF8 is pointless characters.
    public int FirstChar { get; private set; } = ' ';//en_US
    public int CharCount { get; private set; } = 512;//en_US

    public List<MtFontPatch> FontPatches = new List<MtFontPatch>();

    public CharacterRangeUTF8(int first, int count)
    {
      FirstChar = first;
      CharCount = count;
    }

  }
  public class LanguagePackUTF8
  {
    //по-ру́сски ISO 15924 https://www.compart.com/en/unicode/scripts/Cyrl 
    public LanguageCode LanguageCode { get; private set; }
    public SortedDictionary<int, CharacterRangeUTF8> CharacterRanges { get; private set; } = new SortedDictionary<int, CharacterRangeUTF8>();//Int maps to the First UTF-8 Char
    public LanguagePackUTF8(LanguageCode c, List<CharacterRangeUTF8> ranges)
    {
      LanguageCode = c;
      foreach (var x in ranges)
      {
        CharacterRanges.Add(x.FirstChar, x);
      }
    }
  }
  public class MtFontLoader : MtLoader
  {
    //Load font
    #region Private: Members

    private uint _oversampleX = 2;
    private uint _oversampleY = 2;
    private int _firstChar = ' ';
    private int _charCount = 512;
    private int _ascent = 0;
    private int _descent = 0;
    private int _lineGap = 0;
    private int _padding = 1; //STB - "normally you want 1 for bilinear filtering"

    public float LineHeight { get { return ((float)_ascent - (float)_descent + (float)_lineGap); } }
    public float Ascent { get { return (float)_ascent; } }
    public float Descent { get { return (float)_descent; } }

    private List<MtFontPatch> _fontPatches = new List<MtFontPatch>();

    //TODO:
    //TODO:
    //TODO:
    //TODO:
    private Dictionary<LanguageCode, LanguagePackUTF8> _loadedLanguagePacks = new Dictionary<LanguageCode, LanguagePackUTF8>();
    //TODO:
    //TODO:
    //TODO:
    //TODO:
    //TODO:

    #endregion
    #region Private: Unserialized Members

    private StbTrueTypeSharp.StbTrueType.stbtt_fontinfo _fontInfo;
    private byte[] _pFontBuffer = null;  // This must stay loaded. STB:"Load" a font file from a memory buffer (you have to keep the buffer loaded)
    private bool _bInitialized = false;

    #endregion
    #region Public:Methods

    public MtFontLoader(MtFile file) : base(file)
    {
    }
    public override void LoadData()
    {
      Gu.Log.Info("Creating New font Images '" + MtFile.FileLoc.FileName);
      try
      {
        LoadLanguagePacks();

        LoadSTBFont();

        CreateFontImages();

        _bInitialized = true;
      }
      catch (System.Exception ex)
      {
        Gu.Log.Error("Error loading font " + this.MtFile.FileLoc, ex);
      }
    }
    public float GetKerning(MtFontPatch patchInfo, int cCode, int cCodeNext)
    {
      //Get an additional width to add or subtract for kerning.
      float fKern = 0.0f;
      if (cCodeNext >= 0)
      {
        //issue with audio somewhere
        int kern = StbTrueTypeSharp.StbTrueType.stbtt_GetCodepointKernAdvance(_fontInfo, cCode, cCodeNext);
      }
      if (fKern > 0)
      {
        Gu.Trap();
      }
      return fKern;
    }
    public MtFontPatch SelectFontPatch(LanguageCode lc, float fontSize)
    {
      //Gets the closest font patch given the input size.
      //This is similar to mipmapping, but it works in case we can't filter the texture.
      Gu.Assert(_fontPatches != null);
      if (_bInitialized == false)
      {
        Gu.Log.Error("Font was not initialized.");
        Gu.DebugBreak();
        return null;
      }
      if (_fontPatches.Count == 0)
      {
        Gu.Log.Error("No patch infos.");
        Gu.DebugBreak();
        return null;
      }
      //Find the font patch info (MipMap)that is closest to the requested Font Size
      MtFontPatch last = _fontPatches[0];
      if (fontSize >= last.BakedCharSize)
      {
        return last;
      }
      //**TODO: Optimize (there is a binary search or something in animation data)
      foreach (var inf in _fontPatches)
      {
        if (fontSize <= last.BakedCharSize && fontSize >= inf.BakedCharSize)
        {
          return inf;
        }
      }
      return _fontPatches[_fontPatches.Count - 1];
    }
    public override void AfterCompile()
    {
      foreach (var fontpatch in _fontPatches)
      {
        CachePatchChars(fontpatch);
      }
    }
    public override void Serialize(BinaryWriter bw)
    {
      base.Serialize(bw);
      bw.Write((UInt32)_oversampleX);
      bw.Write((UInt32)_oversampleY);
      bw.Write((Int32)_firstChar);
      bw.Write((Int32)_charCount);
      bw.Write((Int32)_ascent);
      bw.Write((Int32)_descent);
      bw.Write((Int32)_lineGap);
      bw.Write((Int32)_padding);

      bw.Write((Int32)_fontPatches.Count);
      for (int pi = 0; pi < _fontPatches.Count; pi++)
      {
        _fontPatches[pi].Serialize(bw);
      }
    }
    public override void Deserialize(BinaryReader br, SerializedFileVersion version)
    {
      base.Deserialize(br, version);
      _oversampleX = br.ReadUInt32();
      _oversampleY = br.ReadUInt32();
      _firstChar = br.ReadInt32();
      _charCount = br.ReadInt32();
      _ascent = br.ReadInt32();
      _descent = br.ReadInt32();
      _lineGap = br.ReadInt32();
      _padding = br.ReadInt32();

      Int32 infCount = br.ReadInt32();
      _fontPatches = new List<MtFontPatch>();
      for (int pi = 0; pi < infCount; pi++)
      {
        var inf = new MtFontPatch();
        inf.Deserialize(br, version);
        _fontPatches.Add(inf);
      }

      //*The STB font must be loaded in 
      //Could we just write it out? IDK maybe.. pointers..
      try
      {
        LoadSTBFont();

        _bInitialized = true;
      }
      catch (Exception ex)
      {
        Gu.Log.Error("Error loading cached font. Font was likely not loaded correctly prior to caching.");
      }
    }

    #endregion
    #region Private:Methods

    private void LoadSTBFont()
    {
      Gu.Assert(MtFile.FileLoc.Exists);

      using (var ss = MtFile.FileLoc.OpenRead())
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
    private void LoadLanguagePacks()
    {
      //This may not be implemented.
      //TODO: config file here..
      var addPack = (LanguageCode c, List<CharacterRangeUTF8> l) =>
      {
        _loadedLanguagePacks.Add(c, new LanguagePackUTF8(c, l));
      };

      addPack(LanguageCode.en, new List<CharacterRangeUTF8>() { new CharacterRangeUTF8(' ', 512) });
      addPack(LanguageCode.es, new List<CharacterRangeUTF8>() { new CharacterRangeUTF8(' ', 512) });//Same as EN

      if (Gu.EngineConfig.Font_Lang_RU)
      {
        addPack(LanguageCode.ru, new List<CharacterRangeUTF8>() { new CharacterRangeUTF8(0x400, 0xFE2F - 0x400) });
      }
      if (Gu.EngineConfig.Font_Lang_ZH)
      {
        addPack(LanguageCode.zh, new List<CharacterRangeUTF8>() { new CharacterRangeUTF8(0x4E00, 0x62FF - 0x4E00) });//0x9FFF is the whole range, that's a lot
      }
    }
    private void CreateFontImages()
    {
      //BakedChar is the MAXIMUM size of a glyph.
      //Image Width/Height maximum is computed automatically, however
      //it is almost ALWAYS too much space. So we must trim the image.

      Gu.Assert(Gu.EngineConfig.Font_MaxBakedCharSize >= Gu.EngineConfig.Font_MinBakedCharSize);
      Gu.Assert(Gu.EngineConfig.Font_MaxBitmapSize >= Gu.EngineConfig.Font_MinBitmapSize);
      Gu.Assert(Gu.EngineConfig.Font_MinBitmapSize > 0);
      Gu.Assert(Gu.EngineConfig.Font_MinBakedCharSize > 0);

      //TODO: more optimal square image size = use widths of characters (BitmapUsedHeight)
      int xchar = (int)Math.Ceiling(Math.Sqrt(_charCount));
      int charPadding = _padding;
      int levels = Gu.EngineConfig.Font_Mipmaps;
      float charHeight = (float)Gu.EngineConfig.Font_MaxBakedCharSize;

      for (int iTex = levels; iTex > 0; iTex--)// iTex  < Gu.c_intMaxWhileTrueLoopSmall; iTex++)
      {
        int charheight_i = (int)Math.Ceiling(charHeight);

        int imageWidth = xchar * (charheight_i + charPadding * 2);
        int imageHeight = xchar * (charheight_i + charPadding * 2);
        Gu.Assert(imageWidth < Gu.EngineConfig.Font_MaxBitmapSize);
        Gu.Assert(imageHeight < Gu.EngineConfig.Font_MaxBitmapSize);

        var charInfo = new StbTrueTypeSharp.StbTrueType.stbtt_packedchar[_charCount];
        byte[] atlasData = ConstructBitmapForSize(imageWidth, imageHeight, charPadding, charHeight, charInfo);

        //Trim the font atlas, because it's friggin huge
        //TODO: Given the BitmapUsedHeight routine, we COULD pre-compute the OPTIMAL SQUARE texture size if we wanted to
        int usedHeight = GetBitmapUsedHeight(charInfo, imageWidth, imageHeight);
        atlasData = TrimAtlasBitmapToUsedHeight(imageWidth, usedHeight, atlasData);

        if (atlasData != null)
        {
          //Set the megatex image.
          Image img = CreateFontImage(atlasData, imageWidth, usedHeight, charInfo);
          if (Gu.EngineConfig.Debug_Font_SaveImage)
          {
            Gu.Log.Debug("Saving font image...");
            string nmapname_dbg = System.IO.Path.Combine(Gu.LocalTmpPath, Gu.Context.Name + " mt_" + MtFile.FileLoc.FileName + "_font_" + iTex + ".png");
            Lib.SaveImage(nmapname_dbg, img, false);
          }
          MtTex mt = new MtTex();
          mt.SetImg(img);
          MtFile.Texs.Add(mt);

          MtFontPatch f = new MtFontPatch();
          f.ScaleForPixelHeight = StbTrueTypeSharp.StbTrueType.stbtt_ScaleForPixelHeight(_fontInfo, charHeight);
          if (f.ScaleForPixelHeight == 0)
          {
            Gu.Log.Error("font scale was zero");
            Gu.DebugBreak();
          }
          f.BakedCharSize = charHeight;
          f.TextureWidth = imageWidth;
          f.TextureHeight = usedHeight;
          f.CharInfo = charInfo;
          f.TextureIndex = levels - iTex;
          f.FirstChar = _firstChar;
          f.CharCount = _charCount;
          _fontPatches.Add(f);
        }
        else
        {
          Gu.Log.Error("Failed to create font " + MtFile.FileLoc);
          break;
        }
        //charHeight /= 2;
        charHeight -= (float)Gu.EngineConfig.Font_MaxBakedCharSize / (float)levels;

        if (imageWidth < Gu.EngineConfig.Font_MinBitmapSize ||
            imageHeight < Gu.EngineConfig.Font_MinBitmapSize ||
            charHeight < Gu.EngineConfig.Font_MinBakedCharSize)
        {
          break;
        }
      }
    }

    private int GetBitmapUsedHeight(StbTrueTypeSharp.StbTrueType.stbtt_packedchar[] charinfos, int image_Width, int image_height)
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
    private byte[] TrimAtlasBitmapToUsedHeight(int imageWidth, int usedHeight, byte[] atlasData)
    {
      byte[] newatlas = new byte[usedHeight * imageWidth];
      System.Buffer.BlockCopy(atlasData, 0, newatlas, 0, newatlas.Length);
      return newatlas;
    }
    private Image CreateFontImage(byte[] pData, int image_width, int image_height, StbTrueTypeSharp.StbTrueType.stbtt_packedchar[] charInfo)
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

      Image img = new Image("FontImage", image_width, image_height, imgData, Image.ImagePixelFormat.RGBA32ub);

      return img;
    }

    private void CachePatchChars(MtFontPatch patchInfo)
    {
      //This routine caches this, basically avoiding unsafe{} calls to STB functions.
      //The speedup is: TODO: post FPS gains

      //Details / Notes:
      //Our goal is to create a label where all glyphs have the same outer box
      //       bearing
      //      <----|--------->advance
      //             y1
      //      |----A------- |                  +y
      //      |    A  pad   |                  | ascent
      //      |   -A---     |                  |
      //  x0  |   | C |pad  | x1    -x-------------------- +x
      //      |   ---D-     |                  |
      //      |      D  pad |                  |descent
      //      |------D-------y0                -y
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
        if (MtFile.Texs.Count == 0 || MtFile.Texs.Count <= patchInfo.TextureIndex)
        {
          //You didn't save the image
          Gu.Log.Error("Failure to save font image somewhere.");
          Gu.DebugBreak();
          return;
        }

        var imagePatch = MtFile.Texs[patchInfo.TextureIndex];

        //Scale the returned texcoodrs from [0,1] to the width of the baked texture
        float tw = imagePatch.uv1.x - imagePatch.uv0.x;  //top left, origin
        float th = imagePatch.uv1.y - imagePatch.uv0.y;  //This is flipped; We are in OpenGL tcoords, however our origin is at the top left

        MtFontChar ccd = new MtFontChar();

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
        int advWidth, leftbearing;
        unsafe
        {
          StbTrueTypeSharp.StbTrueType.stbtt_GetCodepointHMetrics(_fontInfo, cCode, &advWidth, &leftbearing);
        }

        // advanceWidth is the offset from the current horizontal position to the next horizontal position
        // leftSideBearing is the offset from the current horizontal position to the left edge of the character
        // ascent is the coordinate above the baseline the font extends; descent
        // is the coordinate below the baseline the font extends (i.e. it is typically negative)
        // lineGap is the spacing between one row's descent and the next row's ascent...
        // so you should advance the vertical position by "*ascent - *descent + *lineGap"
        //   these are expressed in unscaled coordinates, so you must multiply by
        //   the scale factor for a given size

        ccd.ch_leftbearing = (float)leftbearing;
        ccd.ch_advance = (float)advWidth;
        ccd.ch_left = stbQuad.x0;
        ccd.ch_top = stbQuad.y0;
        ccd.ch_right = stbQuad.x1;
        ccd.ch_bot = stbQuad.y1;

        ccd.patchTexture_Height = patchInfo.TextureHeight;
        ccd.patchTexture_Width = patchInfo.TextureWidth;

        patchInfo.CachedChars.Add(cCode, ccd);
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
    #endregion

  }//MtFontLoader
  public class MegaTex
  {
    //MegaTex
    //how it works: supply list of files (MtFile)
    // a file generates 1+ images (MtTex) and packs into this MegaTex
    // this handles images (MtImageLoader), fonts (MtFontLoader) and generated textures (MtGenLoader)
    #region Enums

    public enum MegaTexCompileState
    {
      NotCompiled,
      Dirty,
      Compiling,
      Compiled
    }
    public enum MtClearColor
    {
      White,
      BlackNoAlpha,
      DebugRainbow//create a rainbow of colors, for debugging
    }

    #endregion
    #region Public: Members

    public MtTex DefaultPixel = null;
    public string Name { get; private set; } = "";
    public static string GenExtension = ".mtgen";
    public List<MtFile> Files { get; private set; } = new List<MtFile>();//We need an ordered list for the disk cache. Keep a separate LUT to FileLoc, this is bc we call GetFont() a BOO BOO number of times which requires a fast LUT

    #endregion
    #region Private Members

    private SerializedFileVersion Version = new SerializedFileVersion(100000);
    private const string c_strDefaultPixelName = "MegaTexDefaultPixel";

    private int _iStartWH = 256;//Image start size (minimum size) pixels
    private int _iGrowWH = 128;//Amount to grow the image as we keep plopping (pixels)
    private MtNode _pRoot = null;
    private MegaTexCompileState _eState = MegaTexCompileState.NotCompiled;
    private UInt64 _genId = 0;
    private string _albedoLocStr = "";
    private string _normLocStr = "";
    private Dictionary<string, MtFile> _locToFileCache = new Dictionary<string, MtFile>();//Faster LUT than Files<>, however we need Files<> for maintaining sequence.
    private MtClearColor _clearColor = MtClearColor.BlackNoAlpha;
    private bool _generateMipmaps = false;
    private TexFilter _texFilter = TexFilter.Nearest;
    private bool _hasNormalMap = false;
    private float _normalMapStrength = 0.0f;
    private int _defaultPixelSize = 0;

    public static MtClearColor GetSystemDefaultClearColor()
    {
      if (Gu.EngineConfig.Debug_RainbowMegatexture) { return MtClearColor.DebugRainbow; }
      else { return MtClearColor.BlackNoAlpha; }
    }

    #endregion
    #region Public: Methods

    public MegaTex(string name, bool bCache, MtClearColor clearColor = MtClearColor.BlackNoAlpha, bool mipmaps = false, TexFilter filter = TexFilter.Nearest, float normalMapStrength = 0.0f, int defaultPixelSize = 3)
    {
      this.Name = name;
      this._hasNormalMap = normalMapStrength > 0.0f;
      this._normalMapStrength = normalMapStrength;
      this._clearColor = clearColor;
      this._generateMipmaps = mipmaps;
      this._texFilter = filter;
      this._albedoLocStr = MakeSaveTextureName(Gu.LocalCachePath, "albedo");
      this._normLocStr = MakeSaveTextureName(Gu.LocalCachePath, "normal");

      //@param defaultPixelSize - Add a default white for rendering solid colors. 0=disable.
      // _bCache = bCache;
      _defaultPixelSize = defaultPixelSize;
      if (defaultPixelSize > 0)
      {
        //Note: Default  will get skewed if texture filtering is enabled.
        var pixelBytes = Enumerable.Repeat((byte)255, defaultPixelSize * defaultPixelSize * 4).ToArray();
        var dpImage = new Image(Gu.Lib.GetUniqueName(ResourceType.Image, c_strDefaultPixelName), defaultPixelSize, defaultPixelSize, pixelBytes, Image.ImagePixelFormat.RGBA32ub);
        var tp = AddResource(dpImage, 1);
      }
    }
    public MtFile? GetResource(FileLoc path)
    {
      Gu.Assert(_locToFileCache != null);

      _locToFileCache.TryGetValue(path.QualifiedPath, out var file);
      return file;
    }
    public MtFile AddResource(Image tx, int shrinkPixelBorder = 0)
    {
      //Add a generated, or custom resource (not from a file)
      string genName = $"|{tx.Name}-gen-{_genId++}{MegaTex.GenExtension}";
      MtFile p = AddResource(new FileLoc(genName, FileStorage.Generated), 1, shrinkPixelBorder);
      MtTex tt = new MtTex(genName, 0, shrinkPixelBorder);
      tt.SetImg(tx);
      //Criticla - compute hash for generated images. 
      tt.ComputeImageHash();

      p.Texs.Add(tt);
      return p;
    }
    public MtFile AddResource(FileLoc loc, int nPatches = 1, int shrinkPixelBorder = 0)
    {
      //Add a file resource
      Gu.Assert(nPatches > 0);
      Gu.Assert(loc != null);

      MtFile? ret = null;
      _locToFileCache.TryGetValue(loc.QualifiedPath, out ret);
      if (ret == null)
      {
        ret = new MtFile(loc, nPatches, shrinkPixelBorder);
        Files.Add(ret);
        _locToFileCache.Add(ret.FileLoc.QualifiedPath, ret);
      }
      return ret;
    }
    public void AddResources(List<FileLoc> resources)
    {
      Gu.Assert(resources != null);
      foreach (var fl in resources)
      {
        AddResource(fl);
      }
      _eState = MegaTexCompileState.Dirty;
    }
    public MtFontLoader GetFont(FileLoc fontLoc)
    {
      Gu.Assert(fontLoc != null);
      if (_locToFileCache.TryGetValue(fontLoc.QualifiedPath, out var ret))
      {
        if (ret.Loader is MtFontLoader)
        {
          return (MtFontLoader)ret.Loader;
        }
      }
      return null;
    }
    public PBRTextureArray Compile()
    {
      _eState = MegaTexCompileState.Compiling;

      Gu.Log.Debug($"MegaTex: Compiling {this.Name}");
      //Cached image names

      //Adding cache because this MFER takes forever to do
      PBRTextureArray output = null;

      //Compiling megatex can be a long process, and it's going to get much longer as this (or any) app gets developed.
      //Caching is really necessary to make debugging faster.
      var cacheFile = this.GetCacheFileName();
      bool changed = CheckCacheFile() != "";
      if (changed)
      {
        output = RedoCompile();
        //[CONTEXT]_mt_[MEGATEX]_[albedo|normal].png
        Lib.SaveImage(_albedoLocStr, output.AlbedoImage, false);
        //Save to /tmp to see it upright
        Lib.SaveImage(MakeSaveTextureName(Gu.LocalTmpPath, "albedo"), output.AlbedoImage, false);
        if (this._hasNormalMap)
        {
          Lib.SaveImage(_normLocStr, output.NormalImage, false);
          Lib.SaveImage(MakeSaveTextureName(Gu.LocalTmpPath, "normal"), output.NormalImage, false);
        }
        SaveCacheFile();
      }
      else
      {
        //Load the cached and compiled megatexture Images
        output = new PBRTextureArray(this.Name + "-pbrtexturearray");
        var alb = new FileLoc(_albedoLocStr, FileStorage.Disk);
        var norm = new FileLoc(_normLocStr, FileStorage.Disk);

        //These MUST exist, or else CheckCacheFile has a bug.
        Gu.Assert(alb.Exists);
        Gu.Assert(!_hasNormalMap || (_hasNormalMap && norm.Exists));

        var albedo = Gu.Lib.GetOrLoadImage(alb);
        output.CreateTexture(PBRTextureInput.Albedo, albedo, this._generateMipmaps, this._texFilter, true);
        if (this._hasNormalMap)
        {
          if (!norm.Exists)
          {
            output.CreateNormalMap(_generateMipmaps, _texFilter, _normalMapStrength, false);
          }
          else
          {
            var normal = Gu.Lib.GetOrLoadImage(norm);
            output.CreateTexture(PBRTextureInput.Normal, normal, this._generateMipmaps, this._texFilter, true);
          }
        }
      }
      //Find the default pixel. We dot his because.. well, we cache it, load it, etc.
      FindNeededTexturesByNameAfterLoad();

      //Delete the loaded images...
      foreach (var f in Files)
      {
        f.UnloadData();
      }
      //Clean up the massive amount of unloaded image data.
      GC.Collect();

      _eState = MegaTexCompileState.Compiled;

      return output;
    }

    #endregion
    #region Private:Methods

    private List<MtTex> GetAllTexturesSortedBySize()
    {
      List<MtTex> vecTexs = new List<MtTex>();
      foreach (var f in Files)
      {
        foreach (var texx in f.Texs)
        {
          vecTexs.Add(texx);
        }
      }

      //Sort by wh - speeds up + saves room
      vecTexs.Sort((a, b) =>
      {
        float f1 = a.GetWidth() * a.GetHeight();
        float f2 = b.GetWidth() * b.GetHeight();
        return f2.CompareTo(f1);
      });

      return vecTexs;
    }
    private PBRTextureArray RedoCompile()
    {
      PBRTextureArray output = new PBRTextureArray(this.Name + "-pbrtexturearray");

      Gu.Log.Debug("Compiling Mega Tex,'" + this.Name + "', " + Files.Count + " images.");

      //Load all texture data. Compute hashes. LoadImages
      foreach (var f in Files)
      {
        f.LoadData();
        //texPair.Value.AfterLoad();
      }

      //Flatten Patches into individual images
      List<MtTex> vecTexs = GetAllTexturesSortedBySize();

      //Tex size
      int iMaxTexSize = 0; //GPU maximum texture size .. TODO: we will likely need to split the texture in the future if we exceed limits.
      GL.GetInteger(GetPName.MaxTextureSize, out iMaxTexSize);// glGetIntegerv(GL_MAX_TEXTURE_SIZE, (GLint*)&_iMaxTexSize);
      Gpu.CheckGpuErrorsRt();

      int iImageSize = _iStartWH;
      int nFailures = 0;
      Image master_albedo = null;

      //Expand rect and grow by 128 if we fail,  this is a pretty quick procedure, so we
      //don't have to worry about sizes.
      while (iImageSize <= iMaxTexSize)
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
              texx.Node = found;
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

      if (iImageSize > iMaxTexSize)
      {
        //Failure
        Gu.Log.Error("..Failed to compose mega texture, too many textures and not enough texture space.");
      }
      else
      {
        Gu.Log.Debug("..Successful. Tex size=" + iImageSize + ".. Creating Bitmap..");

        //Compose Master Image
        int datSiz = iImageSize * iImageSize * 4;

        Byte[] pData = null;
        if (this._clearColor == MtClearColor.BlackNoAlpha)
        {
          pData = Enumerable.Repeat((byte)0, datSiz).ToArray();//memset,0
        }
        else if (this._clearColor == MtClearColor.White)
        {
          pData = Enumerable.Repeat((byte)255, datSiz).ToArray();//memset,0
        }
        else if (this._clearColor == MtClearColor.DebugRainbow)
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

        master_albedo = new Image(this.Name + "-master", iImageSize, iImageSize, pData, Image.ImagePixelFormat.RGBA32ub);

        //delete[] pData;

        float imgW = (float)iImageSize;
        float imgH = (float)iImageSize;

        Gu.Log.Debug("..Copying Sub-Images..and calculating tex coords");
        foreach (MtTex texx in vecTexs)
        {
          if (texx.Node != null)
          {
            master_albedo.copySubImageFrom(texx.Node._b2Rect._min, new ivec2(0, 0), new ivec2(texx.GetWidth(), texx.GetHeight()), texx.Img());
            Gpu.CheckGpuErrorsDbg();

            //Tex coords
            float minx = (float)texx.Node._b2Rect._min.x + texx.ShrinkPixels;
            float miny = (float)texx.Node._b2Rect._min.y + texx.ShrinkPixels;
            float maxx = (float)texx.Node._b2Rect._max.x - texx.ShrinkPixels;
            float maxy = (float)texx.Node._b2Rect._max.y - texx.ShrinkPixels;
            texx.uv0 = new vec2(minx / imgW, miny / imgH);
            texx.uv1 = new vec2(maxx / imgW, maxy / imgH);
          }
          else
          {
            Gu.BRThrowException("Failed to get MTNode for texture.");
          }


          //Free the image and node, we don't need it
          texx.FreeTmp();
        }
      }

      if (master_albedo != null)
      {
        Gu.Log.Debug("..Creating Albedo Map.");

        output.CreateTexture(PBRTextureInput.Albedo, master_albedo, this._generateMipmaps, this._texFilter, true);
        if (_hasNormalMap)
        {
          output.CreateNormalMap(this._generateMipmaps, this._texFilter, _normalMapStrength, false);
        }
      }

      //Cache chars &c
      foreach (var file in Files)
      {
        file.AfterCompile();
      }

      return output;
    }
    private FileLoc GetTmpFileName()
    {
      return new FileLoc(Gu.LocalTmpPath, this.Name + ".mtcache", FileStorage.Disk);
    }
    private FileLoc GetCacheFileName()
    {
      return new FileLoc(Gu.LocalCachePath, this.Name + ".mtcache", FileStorage.Disk);
    }
    private void SaveCacheFile()
    {
      //Save some metadata for the images in this file, so we can check for changes
      //in that case, compile the megatex again
      var fn = GetCacheFileName();

      using (var stream = fn.OpenWrite())
      {
        if (stream != null)
        {
          using (var bw = new BinaryWriter(stream))
          {

            Serialize(bw);

          }
        }
      }
    }
    private string YesItChanged(string res)
    {
      res = "MegaTex " + Name + " Changed (" + res + ") recompiling.";
      //stub to help debug
      Gu.Log.Debug(res);
      return res;
    }
    private string CheckCacheFile()
    {
      //Return empty string if there were no changes to our packed file.
      //or, REturn a string telling what changed.
      try
      {
        // return true if we must compile.
        var fn = GetCacheFileName();
        Gu.Log.Debug("Loading MT cache file " + fn.QualifiedPath);
        if (!fn.Exists)
        {
          return YesItChanged($"{Name}: File {fn.QualifiedPath} does not exist.");
        }
        else
        {
          using (var stream = fn.OpenRead())
          using (var br = new BinaryReader(stream))
          {
            Deserialize(br, Version);
          }
        }
      }
      catch (Exception ex)
      {
        return YesItChanged(ex.ToString());
      }
      return "";
    }
    public void Serialize(BinaryWriter bw)
    {
      bw.Write((string)Name);
      bw.Write((Int32)_clearColor);
      bw.Write((bool)_generateMipmaps);
      bw.Write((Int32)_texFilter);
      bw.Write((bool)_hasNormalMap);
      bw.Write((float)_normalMapStrength);
      bw.Write((Int32)_defaultPixelSize);
      bw.Write((Int32)_iStartWH);
      bw.Write((Int32)_iGrowWH);
      bw.Write((UInt64)_genId);

      bw.Write((Int32)Files.Count);
      foreach (var f in Files)
      {
        f.Serialize(bw);
      }
      bw.Write((string)_albedoLocStr);
      bw.Write((string)_normLocStr);
    }
    public void Deserialize(BinaryReader br, SerializedFileVersion version)
    {
      Name = br.ReadString();
      _clearColor = (MtClearColor)br.ReadInt32();
      _generateMipmaps = br.ReadBoolean();
      _texFilter = (TexFilter)br.ReadInt32();
      _hasNormalMap = br.ReadBoolean();
      var normalMapStrength = br.ReadSingle();
      if (_normalMapStrength != normalMapStrength)
      {
        Gu.BRThrowException("bnormal map strength");
      }

      _defaultPixelSize = br.ReadInt32();
      _iStartWH = br.ReadInt32();
      _iGrowWH = br.ReadInt32();
      _genId = br.ReadUInt64();

      int count = br.ReadInt32();
      if (Files.Count != count)
      {
        Gu.BRThrowException("New texture(s)");
      }

      //Clone files, then, set them back if it all worked.
      List<MtFile> deserializedfiles = new List<MtFile>();

      foreach (var f in Files)
      {
        //Deserialize the CACHED file, INTO a constructed (empty) file object
        MtFile fnew = f.Clone();
        fnew.Deserialize(br, version);
        deserializedfiles.Add(fnew);
      }
      _albedoLocStr = br.ReadString();
      _normLocStr = br.ReadString();

      if (!System.IO.File.Exists(_albedoLocStr))
      {
        Gu.BRThrowException($"{Name} Albedo file {_albedoLocStr} does not exist.");
      }
      if (_hasNormalMap && !System.IO.File.Exists(_normLocStr))
      {
        Gu.BRThrowException($"{Name} Norm file {_normLocStr} does not exist.");
      }

      //All good. No exceptions.. Set our files to be the deserialized.
      Files = deserializedfiles;
      _locToFileCache.Clear();
      foreach (var f in Files)
      {
        _locToFileCache.Add(f.FileLoc.QualifiedPath, f);
      }
    }
    private string MakeSaveTextureName(string path, string texture_name)
    {
      string s = System.IO.Path.Combine(path, $"{Gu.Context.Name}-mt-{Name}-{texture_name}.png");
      return s;
    }
    private void FindNeededTexturesByNameAfterLoad()
    {
      //Find Default Pixel.
      if (_defaultPixelSize > 0)
      {
        DefaultPixel = null;
        foreach (var f in this.Files)
        {
          foreach (var tx in f.Texs)
          {
            if (tx.Name.Contains(c_strDefaultPixelName))
            {
              this.DefaultPixel = tx;
              return;
            }
          }
        }
        Gu.Assert(DefaultPixel != null);
      }

    }
    #endregion

  }//cls


}//ns
