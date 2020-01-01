using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace SiS.Communication.Http
{
    internal class DataZip
    {
        public static byte[] CompressToGZIPBytes(byte[] dataToCompress)
        {
            MemoryStream memStream = new MemoryStream();
            GZipStream zipStream = new GZipStream(memStream, CompressionLevel.Fastest);
            zipStream.Write(dataToCompress, 0, dataToCompress.Length);
            zipStream.Close();
            return memStream.ToArray();
        }

        public static byte[] CompressToGZIPBytes(Stream stream)
        {
            MemoryStream memStream = new MemoryStream();
            GZipStream zipStream = new GZipStream(memStream, CompressionLevel.Fastest);
            stream.CopyTo(zipStream);
            zipStream.Close();
            return memStream.ToArray();
        }

        public static byte[] UnzipToGZIPBytes(byte[] dataToUnzip)
        {
            MemoryStream streamToUnzip = new MemoryStream(dataToUnzip, 0, dataToUnzip.Length, false, true);
            GZipStream inStream = new GZipStream(streamToUnzip, CompressionMode.Decompress);
            MemoryStream outStream = new MemoryStream(60 * 1024);
            int readOnce = 0;
            byte[] buffer = new byte[60 * 1024];
            do
            {
                readOnce = inStream.Read(buffer, 0, buffer.Length);
                if (readOnce > 0)
                {
                    outStream.Write(buffer, 0, readOnce);
                }
            } while (readOnce > 0);
            inStream.Close();
            outStream.Close();
            return outStream.ToArray();
        }

    }
}
