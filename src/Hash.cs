namespace Loft
{
  public class Hash
  {
    //https://stackoverflow.com/questions/19250374/fastest-way-to-make-a-hashkey-of-multiple-strings
    //https://stackoverflow.com/questions/16340/how-do-i-generate-a-hashcode-from-a-byte-array-in-c

    public const int c_iByteArrayHashVersion = 0;//change this if this class changes
    private const UInt64 c_byteHashPrime = 16777619;
    private const UInt64 c_bytehashStart = 2166136261;
    private const UInt64 c_stringHashStart = 2166136261;



    private static uint[] Fnv_Basis_32 = new uint[] {
     2166136261
   , 50331653
   , 3331805713
   , 2324833853
    };
    private static uint[] Fnv_Prime_32 = new uint[] {
     16777619
   , 805306457
   , 124675729
   , 417013901
      };
    private static ulong[] Fnv_Basis_64 = new ulong[]  {
      16540412399126526479U
    , 1604091723691372229
    , 1729532795367666019
    , 15711276102652330523U
    };
    private static ulong[] Fnv_Prime_64 = new ulong[] {
      17577211953059058149U
    , 4401858765275463419
    , 8868681736133192851
    , 6500018318822434283
    };
    public static ulong FNV64(List<int> ints, int index)
    {
      //AssertOrThrow2(iAlgorithmIndex < FNV_MAX_ALGORITHMS_64);
      var n = Fnv_Basis_64[index]; ;
      for (int i = 0; i < ints.Count(); i++)
      {
        n *= Fnv_Prime_64[index];
        n ^= (ulong)ints[i];
      }
      return n;
    }


    public static ulong HashStringArray(List<string> strrings)
    {
      unchecked
      {
        ulong hash = c_bytehashStart;
        foreach (var s in strrings)
        {
          hash = HashString(s, hash);

          //prevet GetHashCode whichcolud (possibly/unlikely) change
          //hash = hash * 23 + s == null ? 0 : (ulong)s.GetHashCode();
        }
        return hash;
      }
    }
    public static UInt64 HashString(string s, UInt64? in_hash = null)
    {
      UInt64 hash = ByteHashStart(in_hash);

      byte[] b = System.Text.Encoding.ASCII.GetBytes(s);
      return HashBytes(b);
    }
    public static UInt64 HashBytes(byte[] data, UInt64? in_hash = null)
    {
      unchecked
      {
        UInt64 hash = ByteHashStart(in_hash);

        Gu.Assert(data != null);
        for (UInt64 i = 0; i < (UInt64)data.Length; i++)
        {
          hash = (hash ^ data[i]) * c_byteHashPrime;
        }
        hash = ByteHashFooter(hash);

        return hash;
      }
    }
    public static UInt64 HashByteArray(List<byte[]> datas)
    {
      unchecked
      {
        UInt64 hash = c_bytehashStart;

        foreach (var data in datas)
        {
          Gu.Assert(data != null);
          hash = HashBytes(data, hash);
        }
        hash = ByteHashFooter(hash);

        return hash;
      }
    }
    private static UInt64 ByteHashStart(UInt64? in_hash)
    {
      UInt64 hash = (in_hash == null) ? c_bytehashStart : in_hash.Value;
      return hash;
    }
    private static UInt64 ByteHashFooter(UInt64 hash)
    {
      hash += hash << 13;
      hash ^= hash >> 7;
      hash += hash << 3;
      hash ^= hash >> 17;
      hash += hash << 5;
      return hash;
    }

  }//cls


}//ns