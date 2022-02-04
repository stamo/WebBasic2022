using BasicWebServer.Server.HTTP;

namespace BasicWebServer.Server.Attributes
{
    public class HttpPostAttribute : HttpMethodAttribute
    {
        public HttpPostAttribute() : base(Method.Post)
        {
        }
    }
}
