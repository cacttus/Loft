using System;
using System.Runtime.InteropServices;

namespace PirateCraft
{
  //What to do when we are outside the bounds of the grid
  public enum IndexMode
  {
    Clamp,  //clamp to the edge if x>=width return x=width-1
    Wrap,   // if x>=width return 0 if x<0 return width-1
    Throw,  //throw an exception (error)
    Default,//return default val
  }
  public class Grid3D<T> where T : struct
  {
    public int SizeX { get; private set; } = 0;
    public int SizeY { get; private set; } = 0;
    public int SizeZ { get; private set; } = 0;
    public int Count { get { return SizeX * SizeY * SizeZ; } }

    public T[] Grid { get; set; } = null;

    public Grid3D(int size_x, int size_y, int size_z)
    {
      SizeX = size_x;
      SizeY = size_y;
      SizeZ = size_z;
    }
    public void Allocate(T initialValue)
    {
      Grid = new T[SizeX * SizeY * SizeZ];
      for (int xx = 0; xx < Count; xx++)
      {
        Grid[xx] = initialValue;
      }
    }
    public void Set(ivec3 v, T val, IndexMode m = IndexMode.Clamp)
    {
      Set(v.x, v.y, v.z, val);
    }
    public void Set(int x, int y, int z, T val, IndexMode m = IndexMode.Clamp)
    {
      ClampOrWrap(ref x, ref y, ref z, m);
      Grid[z * SizeX * SizeY + y * SizeX + x] = val;
    }
    public T Get(ivec3 v, IndexMode m = IndexMode.Clamp)
    {
      return Get(v.x, v.y, v.z, m);
    }
    public T Get(int x, int y, int z, IndexMode m = IndexMode.Clamp)
    {
      ClampOrWrap(ref x, ref y, ref z, m);
      return Grid[z * SizeX * SizeY + y * SizeX + x];
    }
    public T Get(int x, int y, int z, T defaultval)
    {
      if (ClampOrWrap(ref x, ref y, ref z, IndexMode.Default))
      {
        return defaultval;
      }
      return Grid[z * SizeX * SizeY + y * SizeX + x];
    }
    public T Get_Direct_Unsafe_But_Fast(int x, int y, int z)
    {
      //Unsafe get - no bounds checking.
      return Grid[z * SizeX * SizeY + y * SizeX + x];
    }
    public void Serialize(BinaryWriter br)
    {
      if (Grid == null)
      {
        br.Write((Int32)0);
      }
      else
      {
        var byteArr = new byte[Marshal.SizeOf(typeof(T)) * Grid.Length];
        var pinnedHandle = GCHandle.Alloc(Grid, GCHandleType.Pinned);
        Marshal.Copy(pinnedHandle.AddrOfPinnedObject(), byteArr, 0, byteArr.Length);
        pinnedHandle.Free();
        byte[] compressed = Gu.Compress(byteArr);
        br.Write((Int32)compressed.Length);
        br.Write(compressed);
      }
    }
    public void Deserialize(BinaryReader br)
    {
      int compressed_count = br.ReadInt32();
      if (compressed_count == 0)
      {
        Grid = null;
      }
      else
      {
        var compressed = br.ReadBytes(compressed_count);

        byte[] decompressed = Gu.Decompress(compressed);
        var numStructs = decompressed.Length / Marshal.SizeOf(typeof(ushort));

      //  Gu.Assert(numStructs == Drome.DromeBlockCount);

        Grid = new T[numStructs];
        var pinnedHandle = GCHandle.Alloc(Grid, GCHandleType.Pinned);
        Marshal.Copy(decompressed, 0, pinnedHandle.AddrOfPinnedObject(), decompressed.Length);
        pinnedHandle.Free();
      }
    }
    private bool ClampOrWrap(ref int x, ref int y, ref int z, IndexMode m)
    {
      if (m == IndexMode.Clamp)
      {
        if (x < 0)
        {
          x = 0;
        }
        else if (x >= SizeX)
        {
          x = SizeX - 1;
        }
        if (y < 0)
        {
          y = 0;
        }
        else if (y >= SizeY)
        {
          y = SizeY - 1;
        }
        if (z < 0)
        {
          z = 0;
        }
        else if (z >= SizeZ)
        {
          z = SizeZ - 1;
        }
      }
      else if (m == IndexMode.Wrap)
      {
        if (x < 0)
        {
          x = (x % SizeX) + SizeX;
        }
        else if (x >= SizeX)
        {
          x = (x % SizeX);
        }
        if (y < 0)
        {
          y = (y % SizeY) + SizeY;
        }
        else if (y >= SizeY)
        {
          y = (y % SizeY);
        }
        if (z < 0)
        {
          z = (z % SizeZ) + SizeZ;
        }
        else if (z >= SizeZ)
        {
          z = (z % SizeZ);
        }
      }
      else if (m == IndexMode.Throw)
      {
        if ((x < 0) || (x >= SizeX) || (y < 0) || (y >= SizeY) || (z < 0) || (z >= SizeZ))
        {
          Gu.BRThrowException("Index outisde bounds accessing " + this.GetType().Name);
        }
      }
      else if (m == IndexMode.Default)
      {
        if (x < 0)
        {
          return true;
        }
        else if (x >= SizeX)
        {
          return true;
        }
        if (y < 0)
        {
          return true;
        }
        else if (y >= SizeY)
        {
          return true;
        }
        if (z < 0)
        {
          return true;
        }
        else if (z >= SizeZ)
        {
          return true;
        }
      }
      else
      {
        Gu.BRThrowException("Invalid index mode accessing " + this.GetType().Name);
      }

      return false;

    }//ClampOrwra

  }//Grid3D

  public class Grid2D<T> 
  {
    public int SizeX { get; private set; } = 0;
    public int SizeY { get; private set; } = 0;
    public int Count { get { return SizeX * SizeY; } }

    public T[] Grid { get; set; } = null;

    public Grid2D(int size_x, int size_y, int size_z)
    {
      SizeX = size_x;
      SizeY = size_y;
    }
    public void Allocate(T initialValue)
    {
      Grid = new T[SizeX * SizeY];
      for (int xx = 0; xx < Count; xx++)
      {
        Grid[xx] = initialValue;
      }
    }
    public void Set(ivec2 v, T val, IndexMode m = IndexMode.Clamp)
    {
      Set(v.x, v.y, val);
    }
    public void Set(int x, int y, T val, IndexMode m = IndexMode.Clamp)
    {
      ClampOrWrap(ref x, ref y, m);
      Grid[y * SizeX + x] = val;
    }
    public T Get(ivec2 v, IndexMode m = IndexMode.Clamp)
    {
      return Get(v.x, v.y, m);
    }
    public T Get(int x, int y, IndexMode m = IndexMode.Clamp)
    {
      ClampOrWrap(ref x, ref y, m);
      return Grid[y * SizeX + x];
    }
    public T Get(int x, int y, T defaultval)
    {
      if (ClampOrWrap(ref x, ref y, IndexMode.Default))
      {
        return defaultval;
      }
      return Grid[y * SizeX + x];
    }
    public T Get_Direct_Unsafe_But_Fast(int x, int y, int z)
    {
      //Unsafe get - no bounds checking.
      return Grid[y * SizeX + x];
    }
    public void Serialize(BinaryWriter br)
    {
      if (Grid == null)
      {
        br.Write((Int32)0);
      }
      else
      {
        var byteArr = new byte[Marshal.SizeOf(typeof(T)) * Grid.Length];
        var pinnedHandle = GCHandle.Alloc(Grid, GCHandleType.Pinned);
        Marshal.Copy(pinnedHandle.AddrOfPinnedObject(), byteArr, 0, byteArr.Length);
        pinnedHandle.Free();
        byte[] compressed = Gu.Compress(byteArr);
        br.Write((Int32)compressed.Length);
        br.Write(compressed);
      }
    }
    public void Deserialize(BinaryReader br)
    {
      int compressed_count = br.ReadInt32();
      if (compressed_count == 0)
      {
        Grid = null;
      }
      else
      {
        var compressed = br.ReadBytes(compressed_count);

        byte[] decompressed = Gu.Decompress(compressed);
        var numStructs = decompressed.Length / Marshal.SizeOf(typeof(ushort));

       // Gu.Assert(numStructs == Drome.DromeBlockCount);

        Grid = new T[numStructs];
        var pinnedHandle = GCHandle.Alloc(Grid, GCHandleType.Pinned);
        Marshal.Copy(decompressed, 0, pinnedHandle.AddrOfPinnedObject(), decompressed.Length);
        pinnedHandle.Free();
      }
    }
    private bool ClampOrWrap(ref int x, ref int y, IndexMode m)
    {
      if (m == IndexMode.Clamp)
      {
        if (x < 0)
        {
          x = 0;
        }
        else if (x >= SizeX)
        {
          x = SizeX - 1;
        }
        if (y < 0)
        {
          y = 0;
        }
        else if (y >= SizeY)
        {
          y = SizeY - 1;
        }

      }
      else if (m == IndexMode.Wrap)
      {
        if (x < 0)
        {
          x = (x % SizeX) + SizeX;
        }
        else if (x >= SizeX)
        {
          x = (x % SizeX);
        }
        if (y < 0)
        {
          y = (y % SizeY) + SizeY;
        }
        else if (y >= SizeY)
        {
          y = (y % SizeY);
        }

      }
      else if (m == IndexMode.Throw)
      {
        if ((x < 0) || (x >= SizeX) || (y < 0) || (y >= SizeY))
        {
          Gu.BRThrowException("Index outisde bounds accessing " + this.GetType().Name);
        }
      }
      else if (m == IndexMode.Default)
      {
        if (x < 0)
        {
          return true;
        }
        else if (x >= SizeX)
        {
          return true;
        }
        if (y < 0)
        {
          return true;
        }
        else if (y >= SizeY)
        {
          return true;
        }
      }
      else
      {
        Gu.BRThrowException("Invalid index mode accessing " + this.GetType().Name);
      }

      return false;

    }//ClampOrwra

  }//Grid3D

}
