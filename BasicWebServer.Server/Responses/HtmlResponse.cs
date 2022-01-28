using System;
using BasicWebServer.Server.HTTP;

namespace BasicWebServer.Server.Responses
{
    public class HtmlResponse : ContentResponse
    {
        public HtmlResponse(string text)
            : base(text, ContentType.Html)
        {
        }
    }
}
