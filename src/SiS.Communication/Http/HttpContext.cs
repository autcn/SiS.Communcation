using System.Net;
using System.Net.Http;

namespace SiS.Communication.Http
{
    /// <summary>
    /// The context of one http communication session.
    /// </summary>
    public class HttpContext
    {
        /// <summary>
        /// Gets or sets the end point of the client.
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; set; }

        /// <summary>
        /// Gets or sets the http request object.
        /// </summary>
        public HttpRequestMessage Request { get; set; }

        /// <summary>
        /// Gets or sets the http response object.
        /// </summary>
        public HttpResponseMessage Response { get; set; }

    }
}
