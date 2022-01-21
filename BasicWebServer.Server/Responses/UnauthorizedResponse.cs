using BasicWebServer.Server.HTTP;

namespace BasicWebServer.Server.Responses
{
    public class UnauthorizedResponse : Response
    {
        public UnauthorizedResponse()
             : base(StatusCode.Unauthorized)
        {
        }
    }
}
