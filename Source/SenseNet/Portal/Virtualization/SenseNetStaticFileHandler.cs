using System;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.Portal.Virtualization
{
    public class SenseNetStaticFileHandler : IHttpHandler
    {
        #region IHttpHandler Members

        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            var request = context.Request;
            var response = context.Response;

            VirtualFile vf = null;
            var filePath = request.FilePath;

            if (HostingEnvironment.VirtualPathProvider.FileExists(filePath))
                vf = HostingEnvironment.VirtualPathProvider.GetFile(filePath);

            if (vf == null)
                throw new HttpException(404, "File does not exist");
            
            response.ClearContent();

            //Set content type only if this is not a RepositoryFile, because the
            //Open method of RepositoryFile will set the content type itself.
            if (!(vf is RepositoryFile))
            {
                var extension = System.IO.Path.GetExtension(filePath);
                context.Response.ContentType = MimeTable.GetMimeType(extension);

                //add the necessary header for the css font-face rule
                if (MimeTable.IsFontType(extension))
                    HttpContext.Current.Response.AppendHeader("Access-Control-Allow-Origin", HttpContext.Current.Request.Url.GetComponents(UriComponents.SchemeAndServer, UriFormat.SafeUnescaped));
            }

            using (var stream = vf.Open())
            {
                response.AppendHeader("Content-Length", stream.Length.ToString());

                //We need to Flush the headers before
                //we start to stream the actual binary.
                response.Flush();

                var buffer = new byte[Math.Min(stream.Length, RepositoryConfiguration.BinaryChunkSize)];
                int readed;

                while ((readed = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    response.OutputStream.Write(buffer, 0, readed);
                    response.Flush();
                }
            }
        }

        #endregion
    }
}
