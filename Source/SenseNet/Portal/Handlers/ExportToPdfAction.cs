using System;
using System.Web;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Preview;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Portal.Handlers
{
    class ExportToPdfAction : UrlAction, IHttpHandler
    {
        //================================================================== Action overrides

        public override bool IsHtmlOperation { get { return true; } }
        public override bool IsODataOperation { get { return false; } }
        public override bool CausesStateChange { get { return false; } }

        public override void Initialize(Content context, string backUri, Application application, object parameters)
        {
            base.Initialize(context, backUri, application, parameters);

            //real pdf files should be downloaded
            if (context.Name.ToLower().EndsWith(".pdf"))
                this.Forbidden = true;

            var dpp = DocumentPreviewProvider.Current;
            if (dpp != null && !dpp.HasPreviewImages(context.ContentHandler))
                this.Forbidden = true;
        }

        //================================================================== IHttpHandler members

        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            context.Response.Clear();

            var file = this.Content.ContentHandler as File;
            var dpp = DocumentPreviewProvider.Current;
            var head = file == null ? null : NodeHead.Get(file.Id);

            if (file == null || dpp == null || !dpp.HasPreviewPermission(head))
            {
                context.Response.End();
                return;
            }

            context.Response.ContentType = "application/pdf";
            context.Response.AddHeader("content-disposition", "attachment; filename=\"" + this.Content.Name + ".pdf" + "\"");

            // We store the restriction type for the current user to use it later inside the elevated block.
            var rt = dpp.GetRestrictionType(head);

            // We need to elevate here because otherwise preview images would 
            // not be accessible for a user that has only Preview permissions.)
            using (new SystemAccount())
            {
                using (var pdfStream = dpp.GetPreviewImagesDocumentStream(this.Content, DocumentFormat.Pdf, rt))
                {
                    if (pdfStream != null)
                    {
                        context.Response.AppendHeader("Content-Length", pdfStream.Length.ToString());

                        //We need to Flush the headers before
                        //we start to stream the actual binary.
                        context.Response.Flush();

                        var buffer = new byte[Math.Min(pdfStream.Length, RepositoryConfiguration.BinaryChunkSize)];
                        int readed;

                        while ((readed = pdfStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            context.Response.OutputStream.Write(buffer, 0, readed);
                            context.Response.Flush();
                        }
                    }
                } 
            }

            context.Response.End();
        }
    }
}
