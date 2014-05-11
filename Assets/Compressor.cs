using System.Collections;
using System.IO;
using System.IO.Compression;

public class Compressor {
  const int BUFFER_SIZE = 1024;

  /* Deflate
  public static byte[] Decompress(byte[] aBytes)
  {
    MemoryStream lms = new MemoryStream(aBytes);
    DeflateStream lgzs = new DeflateStream(lms, CompressionMode.Decompress);
    MemoryStream lmso = new MemoryStream();
    byte[] lbuffer = new byte[BUFFER_SIZE];
    int lcount;
    while ((lcount = lgzs.Read(lbuffer, 0, lbuffer.Length)) > 0) {
      lmso.Write(lbuffer, 0, lcount);
    }
    return lmso.ToArray();
  }
  
  public static byte[] Compress(byte[] aBytes)
  {
    MemoryStream lms = new MemoryStream(aBytes);
    DeflateStream lgzs = new DeflateStream(lms, CompressionMode.Compress);
    lgzs.Write(aBytes,0,aBytes.Length);
    lgzs.Flush();
    return lms.ToArray();
  }
  */

  /* GZip
  public static byte[] Decompress(byte[] aBytes)
  {
    MemoryStream lms = new MemoryStream(aBytes);
    GZipStream lgzs = new GZipStream(lms, CompressionMode.Decompress);
    MemoryStream lmso = new MemoryStream();
    byte[] lbuffer = new byte[BUFFER_SIZE];
    int lcount;
    while ((lcount = lgzs.Read(lbuffer, 0, lbuffer.Length)) > 0) {
      lmso.Write(lbuffer, 0, lcount);
    }
    return lmso.ToArray();
  }

  public static byte[] Compress(byte[] aBytes)
  {
    MemoryStream lms = new MemoryStream(aBytes);
    GZipStream lgzs = new GZipStream(lms, CompressionMode.Compress);
    lgzs.Write(aBytes,0,aBytes.Length);
    lgzs.Flush();
    return lms.ToArray();
  }
  */

  public static byte[] Decompress(byte[] aBytes)
  {
    return aBytes;
  }
  
  public static byte[] Compress(byte[] aBytes)
  {
    return aBytes;
  }
}
