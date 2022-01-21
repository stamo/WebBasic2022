using BasicWebServer.Server.HTTP;

namespace BasicWebServer.Server.Responses
{
    public class NotFoundResponse : Response
    {
        public NotFoundResponse()
             : base(StatusCode.NotFound)
        {
        }
    }
}
