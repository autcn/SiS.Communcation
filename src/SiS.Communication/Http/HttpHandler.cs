namespace SiS.Communication.Http
{
    /// <summary>
    /// The handler used to process http request.
    /// </summary>
    public abstract class HttpHandler
    {
        /// <summary>
        /// The function to process http request.
        /// </summary>
        /// <param name="context">The context of the http communication.</param>
        public abstract void Process(HttpContext context);
    }
}
