namespace BasicWebServer.Server.HTTP
{
    public class ContentType
    {
        public const string PlainText = "text/plain; charset=UTF-8";
        public const string Html = "text/html; charset=UTF-8";
        public const string FormUrlEncoded = "application/x-www-form-urlencoded";
        public const string FileContent = "application/octet-stream";

        public static string GetByFileExtension(string fileExtension)
            => fileExtension switch
            {
                "css" => "text/css",
                "js" => "application/javascript",
                "jpg" or "jpeg" => "image/jpeg",
                "png" => "image/png",
                _ => PlainText
            };
    }
}
