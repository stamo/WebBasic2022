namespace BasicWebServer.Demo.Services
{
    public class UserService
    {
        private const string Username = "user";

        private const string Password = "user123";

        public bool IsLoginCorrect(string username, string password)
            => username == Username && password == Password;
    }
}
