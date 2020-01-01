using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace SiS.Communication.Http
{
    /// <summary>
    /// The handle for web socket request.
    /// </summary>
    public sealed class WebsocketHandler : HttpHandler
    {
        /// <summary>
        /// Create an instance of WebsocketHandler with specific url.
        /// </summary>
        /// <param name="url">The relative url for web socket connection.</param>
        public WebsocketHandler(string url)
        {
            if (!url.StartsWith("/"))
            {
                url = "/" + url;
            }
            _url = url;
        }
        private string _url;
        /// <summary>
        /// Gets the relative url for web socket connection.
        /// </summary>
        public string RelativeUrl
        {
            get { return _url; }
        }
        private static string Sha1Signature(string str)
        {
            var buffer = Encoding.UTF8.GetBytes(str);
            byte[] data = SHA1.Create().ComputeHash(buffer);
            return Convert.ToBase64String(data);
        }

        private bool TryGetHeaderValue(HttpRequestHeaders headers, string name, out string value)
        {
            if (headers.TryGetValues(name, out IEnumerable<string> values) && values.Any())
            {
                value = values.First();
                return true;
            }
            value = null;
            return false;
        }

        /// <summary>
        /// The function used to process web socket request.
        /// </summary>
        /// <param name="context">The context of the http request.</param>
        public override void Process(HttpContext context)
        {
            HttpRequestMessage reqMsg = context.Request;
            if (reqMsg.Method != HttpMethod.Get)
            {
                return;
            }
            HttpResponseMessage repMsg = ResponseMsgHelper.CreateSimpleRepMsg();
            string url = reqMsg.RequestUri.ToString();
            int paramPos = url.IndexOf("?");
            if (paramPos >= 0)
            {
                url = url.Substring(0, paramPos);
            }
            if (url.ToLower() != _url)
            {
                return;
            }
            do
            {
                repMsg.StatusCode = HttpStatusCode.BadRequest;
                if (reqMsg.Method != HttpMethod.Get
                    || !reqMsg.Headers.Connection.Contains("Upgrade")
                    || reqMsg.Version < Version.Parse("1.1")
                    || !reqMsg.Headers.Upgrade.Contains(new ProductHeaderValue("websocket")))
                {
                    break;
                }
                if (!TryGetHeaderValue(reqMsg.Headers, "Sec-WebSocket-Key", out string wsKey))
                {
                    break;
                }

                if (!TryGetHeaderValue(reqMsg.Headers, "Sec-webSocket-Version", out string wsVersion) || wsVersion != "13")
                {
                    break;
                }

                string protocol = null;
                if (reqMsg.Headers.TryGetValues("Sec-WebSocket-Protocol", out IEnumerable<string> protocols))
                {
                    if (!protocols.Contains("chat"))
                    {
                        break;
                    }
                    protocol = "chat";
                }

                wsKey += "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                string acceptKey = Sha1Signature(wsKey);
                string wsUrl = $"ws://{reqMsg.Headers.Host}{_url}";
                repMsg.StatusCode = HttpStatusCode.SwitchingProtocols;
                repMsg.Headers.Upgrade.Add(new ProductHeaderValue("websocket"));
                repMsg.Headers.Connection.Add("Upgrade");
                repMsg.Headers.Add("Sec-WebSocket-Accept", acceptKey);
                if (protocol != null)
                {
                    repMsg.Headers.Add("Sec-WebSocket-Protocol", protocol);
                }
                repMsg.Headers.Add("Sec-WebSocket-Location", wsUrl);

            } while (false);
            context.Response = repMsg;
        }
    }
}
