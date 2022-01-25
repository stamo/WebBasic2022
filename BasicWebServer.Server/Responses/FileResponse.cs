using System.IO;
using BasicWebServer.Server.HTTP;

namespace BasicWebServer.Server.Responses
{
    public class FileResponse : Response
    {
        public string FileName { get; init; }

        public FileResponse(string fileName)
             : base(StatusCode.OK)
        {
            this.FileName = fileName;

            this.Headers.Add(Header.ContentType, ContentType.FileContent);
        }

        public override string ToString()
        {
            if (File.Exists(this.FileName))
            {
                this.Body = string.Empty;
                FileContent = File.ReadAllBytes(this.FileName);

                var fileBytesCount = new FileInfo(this.FileName).Length;
                this.Headers.Add(Header.ContentLength, fileBytesCount.ToString());

                this.Headers.Add(Header.ContentDisposition,
                    $"attachment; filename=\"{this.FileName}\"");
            }

            return base.ToString();
        }
    }
}
