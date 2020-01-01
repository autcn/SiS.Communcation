using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace SiS.Communication.Http
{
    /// <summary>
    /// Provide methods to create response message.
    /// </summary>
    public static class ResponseMsgHelper
    {
        /// <summary>
        /// Create a simple response message that contains Date and Server header.
        /// </summary>
        /// <returns></returns>

        public static HttpResponseMessage CreateSimpleRepMsg()
        {
            HttpResponseMessage msg = new HttpResponseMessage();
            msg.Headers.Date = DateTimeOffset.Now;
            msg.Headers.Server.Add(new ProductInfoHeaderValue("SiS.Communication.HttpServer", "1.0.0"));
            return msg;
        }
    }
}
