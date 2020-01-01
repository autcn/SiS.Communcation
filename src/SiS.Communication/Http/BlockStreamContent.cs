using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SiS.Communication.Http
{
    /// <summary>
    /// Represents a http content with block stream inside.
    /// </summary>
    public class BlockStreamContent : HttpContent
    {
        /// <summary>
        /// Create an instance of BlockStreamContent based on BlockStream.
        /// </summary>
        /// <param name="stream"></param>
        public BlockStreamContent(BlockStream stream)
        {
            Stream = stream;
        }

        /// <summary>
        /// Gets the block stream of the http content.
        /// </summary>
        public BlockStream Stream { get; }

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Determines whether the HTTP content has a valid length in bytes.
        /// </summary>
        /// <param name="length">The length of the content, in bytes.</param>
        /// <returns></returns>
        protected override bool TryComputeLength(out long length)
        {
            length = Stream.Length;
            return true;
        }
    }
}
