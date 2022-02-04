using BasicWebServer.Demo.Controllers;
using BasicWebServer.Server;
using BasicWebServer.Server.Routing;
using System.Threading.Tasks;

namespace BasicWebServer.Demo
{
    public class Startup
    {
        public static async Task Main()
        {
            await new HttpServer(routes => routes
               .MapControllers())
               .Start();
        }
    }
}
