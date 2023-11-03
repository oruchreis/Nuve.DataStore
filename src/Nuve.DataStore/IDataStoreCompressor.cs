using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Nuve.DataStore;

public interface IDataStoreCompressor
{
    public byte[] Signature { get; }
    public void Compress(Stream outputStream, byte[] uncompressed);
    public void Decompress(Stream outputStream, byte[] compressed);
}
