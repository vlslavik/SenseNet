using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Specialized;
using Aspose.Pdf.Devices;
using Aspose.Imaging;
using Aspose.Cells;
using Aspose.Cells.Rendering;
using System.Threading;

namespace AsposePreviewGenerator
{
    class Program
    {
        public static int ContentId { get; set; }
        public static string Version { get; set; }
        public static int StartIndex { get; set; }
        public static int MaxPreviewCount { get; set; }
        public static string SiteUrl { get; set; }
        public static string Username { get; set; }
        public static string Password { get; set; }

        private static int REQUEST_RETRY_COUNT = 3;

        public static readonly int PREVIEW_WIDTH = 1240;
        public static readonly int PREVIEW_HEIGHT = 1754;
        public static readonly int PREVIEW_POWERPOINT_WIDTH = 1280;
        public static readonly int PREVIEW_POWERPOINT_HEIGHT = 960;

        public static readonly int THUMBNAIL_WIDTH = 200;
        public static readonly int THUMBNAIL_HEIGHT = 200;

        public static readonly string PREVIEW_IMAGENAME = "preview{0}.png";
        public static readonly string THUMBNAIL_IMAGENAME = "thumbnail{0}.png";

        private static readonly string LICENSEPATH = "Aspose.Total.lic";


        // The duplicate of this list exists in the AsposePreviewProvider project! 
        // If you modify this list, please make sure you do the same there.

        public static readonly string[] WORD_EXTENSIONS = new[] { ".doc", ".docx", ".odt", ".rtf", ".txt", ".xml", ".csv" };
        public static readonly string[] DIAGRAM_EXTENSIONS = new[] { ".vdw", ".vdx", ".vsd", ".vss", ".vst", ".vsx", ".vtx" };
        public static readonly string[] IMAGE_EXTENSIONS = new[] { ".gif", ".jpg", ".jpeg", ".bmp", ".png", ".svg", ".exif", ".icon" };
        public static readonly string[] TIFF_EXTENSIONS = new[] { ".tif", ".tiff" };
        public static readonly string[] WORKBOOK_EXTENSIONS = new[] { ".ods", ".xls", ".xlsm", ".xlsx", ".xltm", ".xltx" };
        public static readonly string[] PDF_EXTENSIONS = new[] { ".pdf" };
        public static readonly string[] PRESENTATION_EXTENSIONS = new[] { ".pot", ".pps", ".ppt" };
        public static readonly string[] PRESENTATIONEX_EXTENSIONS = new[] { ".potx", ".ppsx", ".pptx", ".odp" };
        public static readonly string[] PROJECT_EXTENSIONS = new[] { ".mpp" };

        public static readonly ImageFormat PREVIEWIMAGEFORMAT = ImageFormat.Png;

        static void Main(string[] args)
        {
            if (!ParseParameters(args))
            {
                Logger.WriteWarning(ContentId, 0, "Aspose preview generator process arguments are not correct.");
                return;
            }

            ServicePointManager.DefaultConnectionLimit = 10;

            try
            {
                GenerateImages();
            }
            catch (Exception ex)
            {
                Logger.WriteError(ContentId, 0, ex: ex, startIndex: StartIndex, version: Version);

                SetPreviewStatus(-3); //PreviewStatus.Error
            }
        }
        
        //================================================================================================== Preview generation

        protected static void GenerateImages()
        {
            int previewsFolderId;
            string contentPath;

            try
            {
                previewsFolderId = GetPreviewsFolderId();

                if (previewsFolderId < 1)
                {
                    Logger.WriteWarning(ContentId, 0, "Previews folder not found, maybe the content is missing.");
                    return;
                }

                var fileInfo = GetFileInfo();

                if (fileInfo == null)
                {
                    Logger.WriteWarning(ContentId, 0, "Content not found.");
                    return;
                }

                Console.WriteLine("Progress: file info downloaded.");

                contentPath = fileInfo["Path"].Value<string>();

                CheckLicense(contentPath.Substring(contentPath.LastIndexOf('/') + 1));
            }
            catch (Exception ex)
            {
                Logger.WriteError(ContentId, message: "Error during initialization. The process will exit without generating images.", ex: ex, startIndex: StartIndex, version: Version);
                return;
            }

            using (var docStream = GetBinary())
            {
                Console.WriteLine("Progress: file downloaded.");
                if (docStream == null || docStream.Length == 0)
                {
                    SetPreviewStatus(0); //PreviewStatus.EmptyDocument
                    return;
                }

                var extension = contentPath.Substring(contentPath.LastIndexOf('.')).ToLower();

                if (WORD_EXTENSIONS.Contains(extension))
                    GenerateWordPreview(previewsFolderId, docStream);
                else if (TIFF_EXTENSIONS.Contains(extension))
                    GenerateTiffPreview(previewsFolderId, docStream);
                else if (IMAGE_EXTENSIONS.Contains(extension))
                    GenerateImagePreview(previewsFolderId, docStream);
                else if (DIAGRAM_EXTENSIONS.Contains(extension))
                    GenerateDiagramPreview(previewsFolderId, docStream);
                else if (WORKBOOK_EXTENSIONS.Contains(extension))
                    GenerateWorkBookPreview(previewsFolderId, docStream);
                else if (PDF_EXTENSIONS.Contains(extension))
                    GeneratePdfPreview(previewsFolderId, docStream);
                else if (PRESENTATION_EXTENSIONS.Contains(extension))
                    GeneratePresentationPreview(previewsFolderId, docStream);
                else if (PRESENTATIONEX_EXTENSIONS.Contains(extension))
                    GeneratePresentationPreview(previewsFolderId, docStream);
                else if (PROJECT_EXTENSIONS.Contains(extension))
                    GenerateProjectPreview(previewsFolderId, docStream);
            }
        }

        private static void GenerateWordPreview(int previewsFolderId, Stream docStream)
        {
            var document = new Aspose.Words.Document(docStream);
            var pc = document.BuiltInDocumentProperties.Pages;

            // double-check
            if (pc == 0)
                pc = document.PageCount;

            //save the document only if this is the first round
            if (StartIndex == 0 || pc < 1)
                SetPageCount(pc);

            var firstIndex = 0;
            var lastIndex = 0;
            if (pc > 0)
            {
                SetIndexes(StartIndex, pc, out firstIndex, out lastIndex, MaxPreviewCount);

                for (var i = firstIndex; i <= lastIndex; i++)
                {
                    //TODO: compare original file timestamp with current timestamp
                    //if (!CheckActuality(file))
                    //    break;

                    try
                    {
                        using (var imgStream = new MemoryStream())
                        {
                            var options = new Aspose.Words.Saving.ImageSaveOptions(Aspose.Words.SaveFormat.Png)
                            {
                                PageIndex = i,
                                Resolution = 300
                            };

                            document.Save(imgStream, options);
                            if (imgStream.Length == 0)
                                continue;

                            SavePreviewAndThumbnail(imgStream, i + 1, previewsFolderId);
                        }
                    }
                    catch (Exception ex)
                    {

                        Logger.WriteError(ContentId, i + 1, ex: ex, startIndex: StartIndex, version: Version);
                        SaveEmptyPreview(i + 1, previewsFolderId);
                    }
                }
            }
        }

        private static void GeneratePdfPreview(int previewsFolderId, Stream docStream)
        {
            var document = new Aspose.Pdf.Document(docStream);

            if (StartIndex == 0)
                SetPageCount(document.Pages.Count);

            var firstIndex = 0;
            var lastIndex = 0;
            SetIndexes(StartIndex, document.Pages.Count, out firstIndex, out lastIndex, MaxPreviewCount);

            for (var i = firstIndex; i <= lastIndex; i++)
            {
                //if (!CheckActuality(file))
                //    break;

                using (var imgStream = new MemoryStream())
                {
                    try
                    {
                        var pngDevice = new PngDevice();
                        pngDevice.Process(document.Pages[i + 1], imgStream);
                        if (imgStream.Length == 0)
                            continue;

                        SavePreviewAndThumbnail(imgStream, i + 1, previewsFolderId);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteError(ContentId, i + 1, ex: ex, startIndex: StartIndex, version: Version);
                        SaveEmptyPreview(i + 1, previewsFolderId);
                    }
                }
            }
        }

        private static void GenerateTiffPreview(int previewsFolderId, Stream docStream)
        {
            docStream.Seek(0, SeekOrigin.Begin);

            var document = (Aspose.Imaging.FileFormats.Tiff.TiffImage)Aspose.Imaging.Image.Load(docStream);

            if (StartIndex == 0)
                SetPageCount(document.Frames.Length);

            var firstIndex = 0;
            var lastIndex = 0;
            SetIndexes(StartIndex, document.Frames.Length, out firstIndex, out lastIndex, MaxPreviewCount);

            for (var i = firstIndex; i <= lastIndex; i++)
            {
                //if (!CheckActuality(file))
                //    break;

                try
                {
                    document.ActiveFrame = document.Frames[i];
                    using (var imgStream = new MemoryStream())
                    {
                        var options = new Aspose.Imaging.ImageOptions.PngOptions();
                        document.Save(imgStream, options);

                        SavePreviewAndThumbnail(imgStream, i + 1, previewsFolderId);
                    }
                }
                    catch (Exception ex)
                    {
                    Logger.WriteError(ContentId, i + 1, ex: ex, startIndex: StartIndex, version: Version);
                    SaveEmptyPreview(i + 1, previewsFolderId);
                    }
                }
            }

        private static void GenerateImagePreview(int previewsFolderId, Stream docStream)
        {
            docStream.Seek(0, SeekOrigin.Begin);

            var document = Aspose.Imaging.Image.Load(docStream);

            if (StartIndex == 0)
                SetPageCount(1);

            using (var imgStream = new MemoryStream())
            {
                var options = new Aspose.Imaging.ImageOptions.PngOptions();
                document.Save(imgStream, options);

                SavePreviewAndThumbnail(imgStream, 1, previewsFolderId);
            }
        }

        private static void GenerateDiagramPreview(int previewsFolderId, Stream docStream)
        {
            var document = new Aspose.Diagram.Diagram(docStream);

            if (StartIndex == 0)
                SetPageCount(document.Pages.Count);

            var firstIndex = 0;
            var lastIndex = 0;
            SetIndexes(StartIndex, document.Pages.Count, out firstIndex, out lastIndex, MaxPreviewCount);

            for (var i = firstIndex; i <= lastIndex; i++)
            {
                //if (!CheckActuality(file))
                //    break;

                try
                {
                    using (var imgStream = new MemoryStream())
                    {
                        var options = new Aspose.Diagram.Saving.ImageSaveOptions(Aspose.Diagram.SaveFileFormat.PNG)
                        {
                            PageIndex = i,
                            Resolution = 300
                        };

                        document.Save(imgStream, options);
                        if (imgStream.Length == 0)
                            continue;

                        SavePreviewAndThumbnail(imgStream, i + 1, previewsFolderId);
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteError(ContentId, i + 1, ex: ex, startIndex: StartIndex, version: Version);
                    SaveEmptyPreview(i + 1, previewsFolderId);
                }
            }
        }

        private static void GenerateWorkBookPreview(int previewsFolderId, Stream docStream)
        {
            var document = new Workbook(docStream);
            var printOptions = new ImageOrPrintOptions
            {
                ImageFormat = PREVIEWIMAGEFORMAT,
                OnePagePerSheet = false
            };

            // every worksheet may contain multiple pages (as set by Excel 
            // automatically, or by the user using the print layout)
            var estimatedPageCount = document.Worksheets.Cast<Worksheet>().Select(w => new SheetRender(w, printOptions).PageCount).Sum();

            if (StartIndex == 0)
                SetPageCount(estimatedPageCount);

            var firstIndex = 0;
            var lastIndex = 0;
            SetIndexes(StartIndex, estimatedPageCount, out firstIndex, out lastIndex, MaxPreviewCount);

            var workbookPageIndex = 0;
            var worksheetIndex = 0;

            // iterate through worksheets
            while (worksheetIndex < document.Worksheets.Count)
            {
                //if (!CheckActuality(file))
                //    break;

                try
                {
                    var worksheet = document.Worksheets[worksheetIndex];
                    var sheetRender = new SheetRender(worksheet, printOptions);

                    // if we need to start preview generation on a subsequent worksheet, skip the previous ones
                    if (workbookPageIndex + sheetRender.PageCount < StartIndex)
                    {
                        workbookPageIndex += sheetRender.PageCount;
                        worksheetIndex++;
                        continue;
                    }

                    // iterate through pages inside a worksheet
                    for (var worksheetPageIndex = 0; worksheetPageIndex < sheetRender.PageCount; worksheetPageIndex++)
                    {
                        // if the desired page interval contains this page, generate the image
                        if (workbookPageIndex >= firstIndex && workbookPageIndex <= lastIndex)
                        {
                            using (var imgStream = new MemoryStream())
                            {
                                sheetRender.ToImage(worksheetPageIndex, imgStream);

                                // handle empty sheets
                                if (imgStream.Length == 0)
                                    SaveEmptyPreview(workbookPageIndex + 1, previewsFolderId);
                                else
                                    SavePreviewAndThumbnail(imgStream, workbookPageIndex + 1, previewsFolderId);
                            }
                        }

                        workbookPageIndex++;
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteError(ContentId, workbookPageIndex + 1, "", ex, StartIndex, Version);

                    SaveEmptyPreview(workbookPageIndex + 1, previewsFolderId);
                    workbookPageIndex++;
                }

                worksheetIndex++;
            }

            //set the real count if some of the sheets turned out to be empty
            if (workbookPageIndex < estimatedPageCount)
                SetPageCount(workbookPageIndex);
        }

        private static void GeneratePresentationPreview(int previewsFolderId, Stream docStream)
        {
            docStream.Seek(0, SeekOrigin.Begin);

            var pres = new Aspose.Slides.Presentation(docStream);

            if (StartIndex == 0)
                SetPageCount(pres.Slides.Count);

            var firstIndex = 0;
            var lastIndex = 0;

            SetIndexes(StartIndex, pres.Slides.Count, out firstIndex, out lastIndex, MaxPreviewCount);


            for (var i = firstIndex; i <= lastIndex; i++)
            {
                //if (!CheckActuality(file))
                //    break;

                try
                {
                    var slide = pres.Slides[i];

                    // generate a 4:3 ratio image based on the ppt preview dimensions
                    using (var image = slide.GetThumbnail(new System.Drawing.Size(PREVIEW_POWERPOINT_WIDTH, PREVIEW_POWERPOINT_HEIGHT)))
                    {
                        SaveImage(image, i + 1, previewsFolderId);
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteError(ContentId, i + 1, ex: ex, startIndex: StartIndex, version: Version);
                    SaveEmptyPreview(i + 1, previewsFolderId);
                }
            }
        }

        private static void GenerateProjectPreview(int previewsFolderId, Stream docStream)
        {
            var reader = new Aspose.Tasks.ProjectReader();
            var document = reader.Read(docStream);

            // This is the simplest way to create a reasonably readable 
            // preview from a project file: convert it to a PDF first.
            using (var pdfStream = new MemoryStream())
            {
                // save project file in memory as a pdf document
                document.Save(pdfStream, Aspose.Tasks.Saving.SaveFileFormat.PDF);

                // generate previews from the pdf document
                GeneratePdfPreview(previewsFolderId, pdfStream);
            }
        }
        
        //================================================================================================== Communication with the portal

        private static int GetPreviewsFolderId()
        {
            var url = GetUrl(SiteUrl, "GetPreviewsFolder", ContentId, new Dictionary<string, string> { { "version", Version } });
            var json = GetResponseJson(url, "POST", string.Format("{{ empty: {0} }}", (StartIndex == 0).ToString().ToLower()));

            return json != null && json["Id"] != null 
                ? json["Id"].Value<int>()
                : 0;
        }

        private static Stream GetBinary()
        {
            var fileUrl = string.Format("{0}/binaryhandler.ashx?nodeid={1}&propertyname=Binary&version={2}", SiteUrl, ContentId, Version);

            var uri = new Uri(fileUrl);
            var myReq = WebRequest.Create(uri);
            var documentStream = new MemoryStream();

            SetBasicAuthForRequest(myReq, uri);

            try
            {
                using (var wr = myReq.GetResponse())
                {
                    using (var rs = wr.GetResponseStream())
                    {
                        rs.CopyTo(documentStream);
                    }
                }
            }
            catch (WebException ex)
            {
                Logger.WriteError(ContentId, 0, "Error during remote file access.", ex, StartIndex, Version);

                // We need to throw the error further to let the main catch block
                // log the error and set the preview status to 'Error'.
                throw;
            }

            return documentStream;
        }

        private static JToken GetFileInfo()
        {
            var url = GetUrl(SiteUrl, null, ContentId, new Dictionary<string, string> 
            { 
                { "version", Version }, 
                { "$select", "Name,DisplayName,Path,CreatedById" },
                { "metadata", "no" }
            });

            var json = GetResponseJson(url);

            return json != null ? json["d"] : null;
        }

        private static void SetPreviewStatus(int status)
        {
            // REVIEWSTATUS ENUM in document provider

            // NoProvider = -5,
            // Postponed = -4,
            // Error = -3,
            // NotSupported = -2,
            // InProgress = -1,
            // EmptyDocument = 0,
            // Ready = 1

            var url = GetUrl(SiteUrl, "SetPreviewStatus", ContentId);

            GetResponseContent(url, "POST", string.Format("{{ status: {0} }}", status));
        }

        private static void SetPageCount(int pageCount)
        {
            var url = GetUrl(SiteUrl, "SetPageCount", ContentId);

            GetResponseContent(url, "POST", string.Format("{{ pageCount: {0} }}", pageCount.ToString()));
        }

        private static void SavePreviewAndThumbnail(Stream imageStream, int page, int previewsFolderId)
        {
            //save main preview image
            SaveImageStream(imageStream, GetPreviewNameFromPageNumber(page), page, PREVIEW_WIDTH, PREVIEW_HEIGHT, previewsFolderId);
            Console.WriteLine("Progress: {0}%", ((page - StartIndex) * 2 - 1) * 100 / MaxPreviewCount / 2);

            //save smaller image for thumbnail
            SaveImageStream(imageStream, GetThumbnailNameFromPageNumber(page), page, THUMBNAIL_WIDTH, THUMBNAIL_HEIGHT, previewsFolderId);
            Console.WriteLine("Progress: {0}%", ((page - StartIndex) * 2) * 100 / MaxPreviewCount / 2);
        }

        private static void SaveImageStream(Stream imageStream, string name, int page, int width, int height, int previewsFolderId)
        {
            imageStream.Seek(0, SeekOrigin.Begin);
            using (var original = System.Drawing.Image.FromStream(imageStream))
            {
                width = Math.Min(width, original.Width);
                height = Math.Min(height, original.Height);

                using (var resized = ResizeImage(original, width, height))
                {
                    if (resized == null)
                        return;

                    using (var memStream = new MemoryStream())
                    {
                        resized.Save(memStream, PREVIEWIMAGEFORMAT);

                        SaveImageStream(memStream, name, page, previewsFolderId);
                    }
                }
            }
        }

        private static void SaveImageStream(Stream imageStream, string previewName, int page, int previewsFolderId)
        {
            imageStream.Seek(0, SeekOrigin.Begin);

            try
            {
                var imageId = UploadImage(imageStream, previewsFolderId, previewName);

                // set initial preview image properties (CreatedBy, Index, etc.)
                var url = GetUrl(SiteUrl, "SetInitialPreviewProperties", imageId);
                var result = GetResponseContent(url, "POST");
            }
            catch (WebException ex)
            {
                var logged = false;

                if (ex.Response != null)
                {
                    var stream = ex.Response.GetResponseStream();
                    string responseContent;
                    using (var reader = new StreamReader(stream))
                    {
                        responseContent = reader.ReadToEnd();
                    }

                    if (!string.IsNullOrEmpty(responseContent))
                    {
                        if (responseContent.IndexOf("NodeIsOutOfDateException") > 0)
                        {
                            return;
                        }
                        else
                        {
                            Logger.WriteError(ContentId, page, "ERROR RESPONSE: " + responseContent.Replace(Environment.NewLine, " * "), ex, StartIndex, Version);
                            logged = true;
                        }
                    }
                }

                if (!logged)
                    Logger.WriteError(ContentId, page, "Error during uploading a preview image.", ex, StartIndex, Version);
            }
        }

        private static void SaveEmptyPreview(int page, int previewsFolderId)
        {
            using (var emptyImage = new Bitmap(16, 16))
            {
                SaveImage(emptyImage, page, previewsFolderId);
            }
        }

        private static void SaveImage(System.Drawing.Image image, int page, int previewsFolderId)
        {
            using (var imgStream = new MemoryStream())
            {
                image.Save(imgStream, PREVIEWIMAGEFORMAT);
                SavePreviewAndThumbnail(imgStream, page, previewsFolderId);
            }
        }

        private static int UploadImage(Stream imageStream, int previewsFolderId, string imageName)
        {
            var imageStreamLength = imageStream.Length;
            var useChunk = imageStreamLength > Configuration.ChunkSizeInBytes;
            var url = GetUrl(SiteUrl, "Upload", previewsFolderId, new Dictionary<string, string> { { "create", "1" } });            
            var uploadedImageId = 0;
            var retryCount = 0;
            var token = string.Empty;

            // send initial request
            while (retryCount < REQUEST_RETRY_COUNT)
            {
                try
                {
                    var myReq = GetInitWebRequest(url, imageStreamLength, imageName);

                    using (var wr = myReq.GetResponse())
                    {
                        using (var stream = wr.GetResponseStream())
                        {
                            using (var reader = new StreamReader(stream))
                            {
                                token = reader.ReadToEnd();
                            }
                        }
                    }

                    // succesful request: skip out from retry loop
                    break;
                }
                catch (WebException)
                {
                    if (retryCount >= REQUEST_RETRY_COUNT - 1)
                        throw;
                    else
                        Thread.Sleep(50);
                }

                retryCount++;
            }

            var boundary = "---------------------------" + DateTime.UtcNow.Ticks.ToString("x");
            var trailer = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");

            // send subsequent requests
            var buffer = new byte[Configuration.ChunkSizeInBytes];
            int bytesRead;
            var start = 0;

            while ((bytesRead = imageStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                url = GetUrl(SiteUrl, "Upload", previewsFolderId);

                retryCount = 0;

                //get the request object for the actual chunk
                while (retryCount < REQUEST_RETRY_COUNT)
                {
                    var chunkRequest = GetChunkWebRequest(url, imageStreamLength, imageName, token, boundary);

                    if (useChunk)
                        chunkRequest.Headers.Set("Content-Range", string.Format("bytes {0}-{1}/{2}", start, start + bytesRead - 1, imageStreamLength));

                    //write the chunk into the request stream
                    using (var reqStream = chunkRequest.GetRequestStream())
                    {
                        reqStream.Write(buffer, 0, bytesRead);
                        reqStream.Write(trailer, 0, trailer.Length);
                    }                    

                    //send the request
                    try
                    {
                        using (var wr = chunkRequest.GetResponse())
                        {
                            using (var stream = wr.GetResponseStream())
                            {
                                using (var reader = new StreamReader(stream))
                                {
                                    var imgContentJObject = JsonConvert.DeserializeObject(reader.ReadToEnd()) as JObject;

                                    uploadedImageId = imgContentJObject["Id"].Value<int>();
                                }
                            }
                        }

                        // successful request: skip out from the retry loop
                        break;
                    }
                    catch (WebException)
                    {
                        if (retryCount >= REQUEST_RETRY_COUNT - 1)
                            throw;
                        else
                            Thread.Sleep(50);
                    }

                    retryCount++;
                }

                start += bytesRead;
            }

            return uploadedImageId;
        }

        //================================================================================================== Helper methods

        private static void SetIndexes(int originalStartIndex, int pageCount, out int startIndex, out int lastIndex, int maxPreviewCount)
        {
            startIndex = Math.Min(originalStartIndex, pageCount - 1);
            lastIndex = Math.Min(startIndex + maxPreviewCount - 1, pageCount - 1);
        }

        private static string GetPreviewNameFromPageNumber(int page)
        {
            return string.Format(PREVIEW_IMAGENAME, page);
        }
        
        private static string GetThumbnailNameFromPageNumber(int page)
        {
            return string.Format(THUMBNAIL_IMAGENAME, page);
        }

        private static System.Drawing.Image ResizeImage(System.Drawing.Image image, int maxWidth, int maxHeight)
        {
            if (image == null)
                return null;

            //do not scale up the image
            if (image.Width < maxWidth && image.Height < maxHeight)
                return image;

            int newWidth;
            int newHeight;

            ComputeResizedDimensions(image.Width, image.Height, maxWidth, maxHeight, out newWidth, out newHeight);

            try
            {
                var newImage = new Bitmap(newWidth, newHeight);
                using (var graphicsHandle = System.Drawing.Graphics.FromImage(newImage))
                {
                    graphicsHandle.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphicsHandle.DrawImage(image, 0, 0, newWidth, newHeight);
                }

                return newImage;
            }
            catch (OutOfMemoryException omex)
            {
                Logger.WriteError(ContentId, message: "Out of memory error during image resizing.", ex: omex, startIndex: StartIndex, version: Version);
                return null;
            }
        }

        private static void ComputeResizedDimensions(int originalWidth, int originalHeight, int maxWidth, int maxHeight, out int newWidth, out int newHeight)
        {
            // do not scale up the image
            if (originalWidth <= maxWidth && originalHeight <= maxHeight)
            {
                newWidth = originalWidth;
                newHeight = originalHeight;
                return;
            }

            var percentWidth = (float)maxWidth / (float)originalWidth;
            var percentHeight = (float)maxHeight / (float)originalHeight;

            // determine which dimension scale should we use (the smaller)
            var percent = percentHeight < percentWidth ? percentHeight : percentWidth;

            // compute new width and height, based on the final scale
            newWidth = (int)(originalWidth * percent);
            newHeight = (int)(originalHeight * percent);
        }

        private static JToken GetResponseJson(string url, string verb = null, string body = null)
        {
            var responseText = GetResponseContent(url, verb, body);

            return string.IsNullOrEmpty(responseText) ? null : (JsonConvert.DeserializeObject(responseText) as JObject);
        }

        private static string GetResponseContent(string url, string verb = null, string body = null)
        {
            var retryCount = 0;

            while (retryCount < REQUEST_RETRY_COUNT)
            {
                var uri = new Uri(url);
                var myRequest = WebRequest.Create(uri);

                SetBasicAuthForRequest(myRequest, uri);

                if (!string.IsNullOrEmpty(verb))
                {
                    myRequest.Method = verb;
                }

                if (!string.IsNullOrEmpty(body))
                {
                    myRequest.ContentLength = body.Length;

                    using (var requestWriter = new StreamWriter(myRequest.GetRequestStream()))
                    {
                        requestWriter.Write(body);
                    }
                }
                else
                {
                    myRequest.ContentLength = 0;
                }

                try
                {
                    using (var wr = myRequest.GetResponse())
                    {
                        using (var stream = wr.GetResponseStream())
                        {
                            using (var reader = new StreamReader(stream))
                            {
                                return reader.ReadToEnd();
                            }
                        }
                    }
                }
                catch (WebException ex)
                {
                    if (retryCount >= REQUEST_RETRY_COUNT - 1)
                        Logger.WriteError(ContentId, message: "Error during http request. Url:" + url, ex: ex, startIndex: StartIndex, version: Version);
                    else
                        Thread.Sleep(50);
                }

                retryCount++;
            }

            return string.Empty;
        }

        private static string GetUrl(string siteUrl, string odataFunctionName = null, int contentId = 0, IDictionary<string, string> parameters = null)
        {
            var url = string.Format("{0}/OData.svc/Content({1})", siteUrl, contentId);

            if (!string.IsNullOrEmpty(odataFunctionName))
                url += "/" + odataFunctionName;

            if (parameters != null && parameters.Keys.Count > 0)
                url += "?" + string.Join("&", parameters.Select(dk => string.Format("{0}={1}", dk.Key, dk.Value)));

            return url;
        }

        private static void SetBasicAuthForRequest(WebRequest myReq, Uri uri)
        {
            var usernamePassword = Username + ":" + Password;
            myReq.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(new ASCIIEncoding().GetBytes(usernamePassword)));
        }

        private static WebRequest GetInitWebRequest(string url, long fileLength, string fileName)
        {
            var myReq = WebRequest.Create(new Uri(url));
            myReq.Method = "POST";

            SetBasicAuthForRequest(myReq, myReq.RequestUri);

            myReq.ContentType = "application/x-www-form-urlencoded";

            var useChunk = fileLength > Configuration.ChunkSizeInBytes;
            var postData = string.Format("ContentType=PreviewImage&FileName={0}&Overwrite=true&UseChunk={1}", fileName, useChunk);
            var postDataBytes = Encoding.ASCII.GetBytes(postData);

            myReq.ContentLength = postDataBytes.Length;

            using (var reqStream = myReq.GetRequestStream())
            {
                reqStream.Write(postDataBytes, 0, postDataBytes.Length);
            }

            return myReq;
        }

        private static WebRequest GetChunkWebRequest(string url, long fileLength, string fileName, string token, string boundary)
        {
            var myReq = (HttpWebRequest)WebRequest.Create(new Uri(url));

            myReq.Method = "POST";
            myReq.ContentType = "multipart/form-data; boundary=" + boundary;
            myReq.KeepAlive = true;

            SetBasicAuthForRequest(myReq, myReq.RequestUri);

            myReq.Headers.Add("Content-Disposition", "attachment; filename=\"" + fileName + "\"");

            var boundarybytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
            var formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
            var headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n Content-Type: application/octet-stream\r\n\r\n";

            var useChunk = fileLength > Configuration.ChunkSizeInBytes;
            var postValues = new NameValueCollection
                                 {
                                     {"ContentType", "PreviewImage"},
                                     {"FileName", fileName},
                                     {"Overwrite", "true"},
                                     {"UseChunk", useChunk.ToString()},
                                     {"ChunkToken", token}
                                 };

            //we must not close the stream after this as we need to write 
            //the chunk into it in the caller method
            var reqStream = myReq.GetRequestStream();

            //write form data values
            foreach (string key in postValues.Keys)
            {
                reqStream.Write(boundarybytes, 0, boundarybytes.Length);

                var formitem = string.Format(formdataTemplate, key, postValues[key]);
                var formitembytes = Encoding.UTF8.GetBytes(formitem);

                reqStream.Write(formitembytes, 0, formitembytes.Length);
            }

            //write a boundary
            reqStream.Write(boundarybytes, 0, boundarybytes.Length);

            //write file name and content type
            var header = string.Format(headerTemplate, "files[]", fileName);
            var headerbytes = Encoding.UTF8.GetBytes(header);

            reqStream.Write(headerbytes, 0, headerbytes.Length);

            return myReq;
        }

        private static bool ParseParameters(string[] args)
        {
            foreach (var arg in args)
            {
                if (arg.StartsWith("REPO:", StringComparison.OrdinalIgnoreCase))
                {
                    SiteUrl = GetParameterValue(arg);
                }
                else if (arg.StartsWith("USERNAME:", StringComparison.OrdinalIgnoreCase))
                {
                    Username = GetParameterValue(arg);
                }
                else if (arg.StartsWith("PASSWORD:", StringComparison.OrdinalIgnoreCase))
                {
                    Password = GetParameterValue(arg);
                }
                else if (arg.StartsWith("DATA:", StringComparison.OrdinalIgnoreCase))
                {
                    var data = GetParameterValue(arg).Replace("\"\"", "\"");

                    var settings = new JsonSerializerSettings { DateFormatHandling = DateFormatHandling.IsoDateFormat };
                    var serializer = JsonSerializer.Create(settings);
                    var jreader = new JsonTextReader(new StringReader(data));
                    var previewData = serializer.Deserialize(jreader) as JObject;

                    ContentId = previewData["Id"].Value<int>();
                    Version = previewData["Version"].Value<string>();
                    StartIndex = previewData["StartIndex"].Value<int>();
                    MaxPreviewCount = previewData["MaxPreviewCount"].Value<int>();
                }
            }

            return ContentId > 0 && !string.IsNullOrEmpty(Version) && StartIndex >= 0 && MaxPreviewCount > 0 && !string.IsNullOrEmpty(SiteUrl);
        }

        private static string GetParameterValue(string arg)
        {
            return arg.Substring(arg.IndexOf(":") + 1).TrimStart(new char[] { '\'', '"' }).TrimEnd(new char[] { '\'', '"' });
        }

        private static void CheckLicense(string fileName)
        {
            var extension = fileName.Substring(fileName.LastIndexOf('.')).ToLower();

            try
            {
                if (WORD_EXTENSIONS.Contains(extension))
                    new Aspose.Words.License().SetLicense(LICENSEPATH);
                else if (IMAGE_EXTENSIONS.Contains(extension) || TIFF_EXTENSIONS.Contains(extension))
                    new Aspose.Imaging.License().SetLicense(LICENSEPATH);
                else if (DIAGRAM_EXTENSIONS.Contains(extension))
                    new Aspose.Diagram.License().SetLicense(LICENSEPATH);
                else if (WORKBOOK_EXTENSIONS.Contains(extension))
                    new Aspose.Cells.License().SetLicense(LICENSEPATH);
                else if (PDF_EXTENSIONS.Contains(extension))
                    new Aspose.Pdf.License().SetLicense(LICENSEPATH);
                else if (PRESENTATION_EXTENSIONS.Contains(extension) || PRESENTATIONEX_EXTENSIONS.Contains(extension))
                    new Aspose.Slides.License().SetLicense(LICENSEPATH);
                else if (PROJECT_EXTENSIONS.Contains(extension))
                    new Aspose.Tasks.License().SetLicense(LICENSEPATH);
            }
            catch (Exception ex)
            {
                Logger.WriteError(ContentId, message: "Error during license check. ", ex: ex, startIndex: StartIndex, version: Version);
            }
        }
    }
}
