﻿using System;
using System.Web;
using System.IO;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository;
using SenseNet.Portal.Virtualization;

namespace SenseNet.Portal.Handlers
{
    /// <summary>
    /// Base class for serving binary properties of Sense/Net content items.
    /// </summary>
    public class BinaryHandlerBase : IHttpHandler
    {
        /// <summary>
        /// URL part used for accessing this handler by default
        /// </summary>
        public static string UrlPart { get { return "sn-binary"; } }

        /// <summary>
        /// Use this if the binary is an image. If not null, the image will be resized to the specified width.
        /// </summary>
        protected virtual int? Width { get; set; }

        /// <summary>
        /// Use this if the binary is an image. If not null, the image will be resized to the specified height.
        /// </summary>
        protected virtual int? Height { get; set; }

        /// <summary>
        /// The node whose binary property should be served.
        /// </summary>
        protected virtual Node RequestedNode { get; set; }

        /// <summary>
        /// Name of the binary property to be served.
        /// </summary>
        protected virtual string PropertyName { get; set; }

        /// <summary>
        /// If not null, the HTTP Max-Age header will be set to this value.
        /// </summary>
        protected virtual TimeSpan? MaxAge { get; set; }

        /// <summary>
        /// Creates a new instance of BinaryHandlerBase
        /// </summary>
        public BinaryHandlerBase()
        {
        }

        /// <summary>
        /// Creates a new instance of BinaryHandlerBase
        /// </summary>
        public BinaryHandlerBase(Node requestedNode, string propertyName, TimeSpan? maxAge = null, int? width = null, int? height = null)
        {
            RequestedNode = requestedNode;
            PropertyName = propertyName;
            Width = width;
            Height = height;
            MaxAge = maxAge;
        }

        /// <summary>
        /// Processes the request.
        /// </summary>
        public virtual void ProcessRequestCore(HttpContextBase context)
        {
            var propertyName = PropertyName;
            var requestedNode = RequestedNode;

            if (string.IsNullOrEmpty(propertyName) || requestedNode == null)
            {
                context.Response.StatusCode = 404;
                return;
            }

            // Get the stream through our provider to let 3rd party developers serve custom data
            string contentType;
            BinaryFileName fileName;
            var binaryStream = DocumentBinaryProvider.Current.GetStream(requestedNode, propertyName, out contentType, out fileName);
            if (binaryStream == null)
                return;

            Stream resizedStream;

            // If this is an image and we need to resize it
            if (Width.HasValue && Height.HasValue)
                resizedStream = Image.CreateResizedImageFile(binaryStream, string.Empty, Width.Value, Height.Value, 0, contentType);
            else
                resizedStream = binaryStream;

            // We need to Flush the headers before
            // We start to stream the actual binary.
            
            context.Response.ContentType = contentType;
            context.Response.AppendHeader("Content-Length", resizedStream.Length.ToString());
            HttpHeaderTools.SetCacheControlHeaders(lastModified: requestedNode.ModificationDate);
            if (this.MaxAge.HasValue)
                HttpHeaderTools.SetCacheControlHeaders(maxAge: this.MaxAge);
            context.Response.StatusCode = 200;
            context.Response.Flush();

            resizedStream.Position = 0;

            var buffer = new byte[Math.Min(resizedStream.Length, RepositoryConfiguration.BinaryChunkSize)];
            int bytesRead;

            //while (bytesRead > 0)
            while ((bytesRead = resizedStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                context.Response.OutputStream.Write(buffer, 0, bytesRead);
                context.Response.Flush();
            }

            // Let the client code log file downloads
            var file = requestedNode as ContentRepository.File;
            if (file != null)
                ContentRepository.File.Downloaded(file.Id);
        }

        #region HttpHandler implementation

        bool IHttpHandler.IsReusable
        {
            get { return false; }
        }

        void IHttpHandler.ProcessRequest(HttpContext context)
        {
            ProcessRequestCore(new HttpContextWrapper(context));
        }

        #endregion
    }
}
