using System;
using BasicWebServer.Server.HTTP;

namespace BasicWebServer.Server.Responses
{
    public class HtmlResponse : ContentResponse
    {
        public HtmlResponse(string text,
            Action<Request, Response> preRenderAction = null)
            : base(text, ContentType.Html, preRenderAction)
        {
        }
    }
}
