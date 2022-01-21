using BasicWebServer.Server.HTTP;

namespace BasicWebServer.Server.Responses
{
    public class BadRequestResponse : Response
    {
        public BadRequestResponse()
             : base(StatusCode.BadRequest)
        {
        }
    }
}
