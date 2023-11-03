using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Text;

namespace Nuve.DataStore;

internal class DeflateCompressor : IDataStoreCompressor
{
    private static readonly byte[] _compressSignature = Encoding.ASCII.GetBytes("__Compressed_D__");
    private const CompressionLevel _compressionLevel = CompressionLevel.Optimal;
    public byte[] Signature => _compressSignature;

    public void Compress(Stream outputStream, byte[] uncompressed)
    {
        using var ds = new DeflateStream(outputStream, _compressionLevel);
        ds.Write(uncompressed, 0, uncompressed.Length);
        ds.Close();
    }

    public void Decompress(Stream outputStream, byte[] compressed)
    {
        using var compressedStream = new MemoryStream(compressed);
        using var ds = new DeflateStream(compressedStream, CompressionMode.Decompress);
        ds.CopyTo(outputStream);
        ds.Close();        
    }
}
