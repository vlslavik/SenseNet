using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.BackgroundOperations;
using Newtonsoft.Json.Converters;

namespace SenseNet.ContentRepository.Preview
{
    public enum WatermarkPosition { BottomLeftToUpperRight, UpperLeftToBottomRight, Top, Bottom, Center }
    public enum DocumentFormat { NonDefined, Doc, Docx, Pdf, Ppt, Pptx, Xls, Xlsx }

    [Flags]
    public enum RestrictionType
    {
        NoAccess = 1,
        NoRestriction = 2,
        Redaction = 4,
        Watermark = 8
    }

    public enum PreviewStatus
    {
        NoProvider = -5,
        Postponed = -4,
        Error = -3,
        NotSupported = -2,
        InProgress = -1,
        EmptyDocument = 0,
        Ready = 1
    }

    public abstract class DocumentPreviewProvider : IPreviewProvider
    {
        public static readonly string DOCUMENTPREVIEW_SETTINGS = "DocumentPreview";
        public static readonly string WATERMARK_TEXT = "WatermarkText";
        public static readonly string WATERMARK_ENABLED = "WatermarkEnabled";
        public static readonly string WATERMARK_FONT = "WatermarkFont";
        public static readonly string WATERMARK_BOLD = "WatermarkBold";
        public static readonly string WATERMARK_ITALIC = "WatermarkItalic";
        public static readonly string WATERMARK_FONTSIZE = "WatermarkFontSize";
        public static readonly string WATERMARK_POSITION = "WatermarkPosition";
        public static readonly string WATERMARK_OPACITY = "WatermarkOpacity";
        public static readonly string WATERMARK_COLOR = "WatermarkColor";
        public static readonly string MAXPREVIEWCOUNT = "MaxPreviewCount";
        public static readonly string PREVIEWS_FOLDERNAME = "Previews";
        public static readonly string PREVIEW_IMAGENAME = "preview{0}.png";
        public static readonly string PREVIEW_THUMBNAIL_REGEX = "(preview|thumbnail)(?<page>\\d+).png";
        public static readonly string THUMBNAIL_REGEX = "thumbnail(?<page>\\d+).png";
        public static readonly string THUMBNAIL_IMAGENAME = "thumbnail{0}.png";

        public static readonly int PREVIEW_WIDTH = 1240;
        public static readonly int PREVIEW_HEIGHT = 1754;
        public static readonly int PREVIEW_POWERPOINT_WIDTH = 1280;
        public static readonly int PREVIEW_POWERPOINT_HEIGHT = 960;

        public static readonly int THUMBNAIL_WIDTH = 200;
        public static readonly int THUMBNAIL_HEIGHT = 200;

        protected static readonly float THUMBNAIL_PREVIEW_WIDTH_RATIO = THUMBNAIL_WIDTH / (float)PREVIEW_WIDTH;
        protected static readonly float THUMBNAIL_PREVIEW_HEIGHT_RATIO = THUMBNAIL_HEIGHT / (float)PREVIEW_HEIGHT;

        protected static readonly int PREVIEW_PDF_WIDTH = 600;
        protected static readonly int PREVIEW_PDF_HEIGHT = 850;
        protected static readonly int PREVIEW_WORD_WIDTH = 800;
        protected static readonly int PREVIEW_WORD_HEIGHT = 870;
        protected static readonly int PREVIEW_EXCEL_WIDTH = 1000;
        protected static readonly int PREVIEW_EXCEL_HEIGHT = 750;

        public static readonly ImageFormat PREVIEWIMAGEFORMAT = ImageFormat.Png;

        //============================================================================== Configuration

        private const string DEFAULT_DOCUMENTPREVIEWPROVIDER_CLASSNAME = "SenseNet.ContentRepository.Preview.DefaultDocumentPreviewProvider";

        private const string DOCUMENTPREVIEWPROVIDERCLASSNAMEKEY = "DocumentPreviewProvider";
        private static string DocumentPreviewProviderClassName
        {
            get
            {
                return ConfigurationManager.AppSettings[DOCUMENTPREVIEWPROVIDERCLASSNAMEKEY];
            }
        }

        //===================================================================================================== Static provider instance

        private static DocumentPreviewProvider _current;
        private static readonly object _lock = new object();
        private static bool _isInitialized;
        public static DocumentPreviewProvider Current
        {
            get
            {
                //This property has a duplicate in the Storage layer in the PreviewProvider
                //class. If you change this, please propagate changes there.

                if ((_current == null) && (!_isInitialized))
                {
                    lock (_lock)
                    {
                        if ((_current == null) && (!_isInitialized))
                        {
                            try
                            {
                                if (!string.IsNullOrEmpty(DocumentPreviewProviderClassName))
                                    _current = (DocumentPreviewProvider)TypeHandler.CreateInstance(DocumentPreviewProviderClassName);
                                else
                                    _current = (DocumentPreviewProvider)TypeHandler.CreateInstance(DEFAULT_DOCUMENTPREVIEWPROVIDER_CLASSNAME);
                            }
                            catch (TypeNotFoundException) //rethrow
                            {
                                throw new ConfigurationErrorsException(String.Concat(SR.Exceptions.Configuration.Msg_DocumentPreviewProviderImplementationDoesNotExist, ": ", DocumentPreviewProviderClassName));
                            }
                            catch (InvalidCastException) //rethrow
                            {
                                throw new ConfigurationErrorsException(String.Concat(SR.Exceptions.Configuration.Msg_InvalidDocumentPreviewProviderImplementation, ": ", DocumentPreviewProviderClassName));
                            }
                            finally
                            {
                                _isInitialized = true;
                            }

                            if (_current == null)
                                Logger.WriteInformation(Logger.EventId.NotDefined, "DocumentPreviewProvider not present.");
                            else
                                Logger.WriteInformation(Logger.EventId.NotDefined, "DocumentPreviewProvider created: " + _current.GetType().FullName);
                        }
                    }
                }
                return _current;
            }
        }

        //===================================================================================================== Helper methods

        protected static string GetPreviewsSubfolderName(Node content)
        {
            return content.Version.ToString();
        }
        
        protected static string GetPreviewNameFromPageNumber(int page)
        {
            return string.Format(PREVIEW_IMAGENAME, page);
        }
        protected static string GetThumbnailNameFromPageNumber(int page)
        {
            return string.Format(THUMBNAIL_IMAGENAME, page);
        }

        protected static bool GetDisplayWatermarkQueryParameter()
        {
            if (HttpContext.Current == null)
                return false;

            var watermarkVal = HttpContext.Current.Request["watermark"];
            if (string.IsNullOrEmpty(watermarkVal))
                return false;

            if (watermarkVal == "1")
                return true;

            bool wm;
            return bool.TryParse(watermarkVal, out wm) && wm;
        }

        protected static Color ParseColor(string color)
        {
            //rgba(0,0,0,1)
            if (string.IsNullOrEmpty(color))
                return Color.DarkBlue;

            var i1 = color.IndexOf('(');
            var colorVals = color.Substring(i1 + 1, color.IndexOf(')') - i1 - 1).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            return Color.FromArgb(Convert.ToInt32(colorVals[3]), Convert.ToInt32(colorVals[0]),
                                  Convert.ToInt32(colorVals[1]), Convert.ToInt32(colorVals[2]));
        }

        protected static System.Drawing.Image ResizeImage(System.Drawing.Image image, int maxWidth, int maxHeight)
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
                using (var graphicsHandle = Graphics.FromImage(newImage))
                {
                    graphicsHandle.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphicsHandle.DrawImage(image, 0, 0, newWidth, newHeight);
                }

                return newImage;
            }
            catch (OutOfMemoryException omex)
            {
                Logger.WriteException(omex);
                return null;
            }
        }

        protected static void ComputeResizedDimensions(int originalWidth, int originalHeight, int maxWidth, int maxHeight, out int newWidth, out int newHeight)
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

        protected static void SavePageCount(File file, int pageCount)
        {
            if (file == null || file.PageCount == pageCount)
                return;

            using (new SystemAccount())
            {
                file.PageCount = pageCount;
                file.DisableObserver(typeof(DocumentPreviewObserver));
                file.DisableObserver(TypeHandler.GetType(NodeObserverNames.NOTIFICATION));
                file.DisableObserver(TypeHandler.GetType(NodeObserverNames.WORKFLOWNOTIFICATION));

                file.Save(SavingMode.KeepVersion);
            }
        }

        protected static VersionNumber GetVersionFromPreview(NodeHead previewHead)
        {
            if (previewHead == null)
                return null;

            // Expected structure: /Root/.../DocumentLibrary/doc1.docx/Previews/V1.2.D/preview1.png
            var parentName = RepositoryPath.GetFileName(RepositoryPath.GetParentPath(previewHead.Path));
            VersionNumber version;

            return !VersionNumber.TryParse(parentName, out version) ? null : version;
        }

        protected static File GetDocumentForPreviewImage(NodeHead previewHead)
        {
            using (new SystemAccount())
            {
                var document = Node.GetAncestorOfType<File>(Node.LoadNode(previewHead.ParentId));

                // we need to load the appropriate document version for this preview image
                var version = GetVersionFromPreview(previewHead);
                if (version != null && version.VersionString != document.Version.VersionString)
                    document = Node.Load<File>(document.Id, version);

                return document;
            }
        }

        /// <summary>
        /// This method ensures the existence of all the preview images in a range. 
        /// It synchronously waits for all the images to be created.
        /// </summary>
        protected static void CheckPreviewImages(Content content, int start, int end)
        {
            if (content == null)
                throw new PreviewNotAvailableException("Content deleted.", -1, 0);

            var pc = (int)content["PageCount"];
            if (pc < 0)
                throw new PreviewNotAvailableException("Preview not available. State: " + pc + ".", -1, pc);
            if (end < 0)
                throw new PreviewNotAvailableException("Invalid 'end' value: " + end, -1, pc);

            Image image;
            var missingIndexes = new List<int>();
            for (var i = start; i <= end; i++)
            {
                AssertResultIsStillRequired();
                image = DocumentPreviewProvider.Current.GetPreviewImage(content, i);
                if (image == null || image.Index < 1)
                    missingIndexes.Add(i);
            }
            foreach (var i in missingIndexes)
            {
                do
                {
                    AssertResultIsStillRequired();

                    //this call will start a preview workflow if the image does not exist
                    image = DocumentPreviewProvider.Current.GetPreviewImage(content, i);
                    if (image == null || image.Index < 1)
                    {
                        // document was deleted in the meantime
                        if (!Node.Exists(content.Path))
                            throw new PreviewNotAvailableException("Content deleted.", -1, 0);

                        Thread.Sleep(1000);
                    }

                } while (image == null);
            }
        }

        protected static IEnumerable<Node> QueryPreviewImages(string path)
        {
            return NodeQuery.QueryNodesByTypeAndPath(NodeType.GetByName("PreviewImage"), false, path + "/", false)
                .Identifiers
                .Select(i => NodeHead.Get(i))
                .Where(h => h.Name.StartsWith("preview", StringComparison.OrdinalIgnoreCase))
                .Select(h => Node.LoadNode(h));
        }

        protected static void AssertResultIsStillRequired()
        {
            if (HttpContext.Current != null && !HttpContext.Current.Response.IsClientConnected)
            {
                //TODO: create a new exception class for this
                throw new Exception("Client is disconnected");
            }
        }

        //===================================================================================================== Server-side interface

        public abstract bool IsContentSupported(Node content);
        public abstract string PreviewGeneratorTaskName { get; }

        public virtual bool IsPreviewOrThumbnailImage(NodeHead imageHead)
        {
            return (imageHead != null &&
                    imageHead.GetNodeType().IsInstaceOfOrDerivedFrom(ActiveSchema.NodeTypes["PreviewImage"]) &&
                    imageHead.Path.Contains(RepositoryPath.PathSeparator + PREVIEWS_FOLDERNAME + RepositoryPath.PathSeparator)) &&
                    new Regex(PREVIEW_THUMBNAIL_REGEX).IsMatch(imageHead.Name);
        }

        public virtual bool IsThumbnailImage(Image image)
        {
            return (image != null &&
                    image.NodeType.IsInstaceOfOrDerivedFrom(ActiveSchema.NodeTypes["PreviewImage"]) &&
                    new Regex(THUMBNAIL_REGEX).IsMatch(image.Name));
        }

        public bool HasPreviewPermission(NodeHead nodeHead)
        {
            return (GetRestrictionType(nodeHead) & RestrictionType.NoAccess) != RestrictionType.NoAccess;
        }

        public virtual bool IsPreviewAccessible(NodeHead previewHead)
        {
            if (!HasPreviewPermission(previewHead))
                return false;

            var version = GetVersionFromPreview(previewHead);

            // The image is outside of a version folder (which is not valid), we have to allow accessing the image.
            if (version == null)
                return true;

            // This method was created to check if the user has access to preview images of minor document versions,
            // so do not bother if this is a preview for a major version.
            if (version.IsMajor)
                return true;

            // Here we assume that permissions are not broken on previews! This means the current user
            // has the same permissions (e.g. OpenMinor) on the preview image as on the document (if this 
            // is a false assumption, than we need to load the document itself and check OpenMinor on it).
            return SecurityHandler.HasPermission(previewHead, PermissionType.OpenMinor);
        }

        public virtual RestrictionType GetRestrictionType(NodeHead nodeHead)
        {
            //if the lowest preview permission is not granted, the user has no access to the preview image
            if (nodeHead == null || !SecurityHandler.HasPermission(nodeHead, PermissionType.Preview))
                return RestrictionType.NoAccess;

            //has Open permission: means no restriction
            if (SecurityHandler.HasPermission(nodeHead, PermissionType.Open))
                return RestrictionType.NoRestriction;

            var seeWithoutRedaction = SecurityHandler.HasPermission(nodeHead, PermissionType.PreviewWithoutRedaction);
            var seeWithoutWatermark = SecurityHandler.HasPermission(nodeHead, PermissionType.PreviewWithoutWatermark);

            //both restrictions should be applied
            if (!seeWithoutRedaction && !seeWithoutWatermark)
                return RestrictionType.Redaction | RestrictionType.Watermark;

            if (!seeWithoutRedaction)
                return RestrictionType.Redaction;

            if (!seeWithoutWatermark)
                return RestrictionType.Watermark;

            return RestrictionType.NoRestriction;
        }

        public virtual IEnumerable<Content> GetPreviewImages(Content content)
        {
            if (content == null || !this.IsContentSupported(content.ContentHandler))
                return new List<Content>();

            var pc = (int)content["PageCount"];

            while (pc == (int)PreviewStatus.InProgress || pc == (int)PreviewStatus.Postponed)
            {
                //create task if it does not exists. Otherwise page count will not be calculated.
                StartPreviewGenerationInternal(content.ContentHandler, priority: TaskPriority.Immediately);

                Thread.Sleep(4000);

                AssertResultIsStillRequired();

                content = Content.Load(content.Id);
                if (content == null)
                    throw new PreviewNotAvailableException("Content deleted.", -1, 0);

                pc = (int)content["PageCount"];
            }

            var previewPath = RepositoryPath.Combine(content.Path, PREVIEWS_FOLDERNAME, GetPreviewsSubfolderName(content.ContentHandler));
            var images = QueryPreviewImages(previewPath).ToArray();

            //all preview images exist
            if (images.Length != pc)
            {
                // check all preview images one-by-one (wait for complete)
                CheckPreviewImages(content, 1, pc);
                images = QueryPreviewImages(previewPath).ToArray();
            }
            return images.Select(n => Content.Create(n));
        }
        
        public virtual bool HasPreviewImages(Node content)
        {
            var pageCount = (int)content["PageCount"];
            if (pageCount > 0)
                return true;

            var status = (PreviewStatus)pageCount;
            switch (status)
            {
                case PreviewStatus.Postponed:
                case PreviewStatus.InProgress:
                case PreviewStatus.Ready:
                    return true;
                default:
                    return false;
            }
        }

        public virtual Image GetPreviewImage(Content content, int page)
        {
            return GetImage(content, page, false);
        }

        public virtual Image GetThumbnailImage(Content content, int page)
        {
            return GetImage(content, page, true);
        }

        private Image GetImage(Content content, int page, bool thumbnail)
        {
            if (content == null || page < 1)
                return null;

            //invalid request: not a file or not enough pages
            var file = content.ContentHandler as File;
            if (file == null || file.PageCount < page)
                return null;

            using (new SystemAccount())
            {
                var previewName = thumbnail ? GetThumbnailNameFromPageNumber(page) : GetPreviewNameFromPageNumber(page);
                var path = RepositoryPath.Combine(content.Path, PREVIEWS_FOLDERNAME, GetPreviewsSubfolderName(content.ContentHandler), previewName);
                var img = Node.Load<Image>(path);
                if (img != null)
                    return img;

                StartPreviewGenerationInternal(file, page - 1, TaskPriority.Immediately);
            }

            return null;
        }

        public virtual Stream GetRestrictedImage(Image image, string binaryFieldName = null, RestrictionType? restrictionType = null)
        {
            var previewImage = image;

            //we need to reload the image in elevated mode to have access to its properties
            if (previewImage.IsHeadOnly)
            {
                using (new SystemAccount())
                {
                    previewImage = Node.Load<Image>(image.Id);
                }
            }

            BinaryData binaryData = null;

            if (!string.IsNullOrEmpty(binaryFieldName))
            {
                var property = previewImage.PropertyTypes[binaryFieldName];
                if (property != null && property.DataType == DataType.Binary)
                    binaryData = previewImage.GetBinary(property);
            }

            if (binaryData == null)
                binaryData = previewImage.Binary;

            //if the image is not a preview, return the requested binary without changes
            if (!IsPreviewOrThumbnailImage(NodeHead.Get(previewImage.Id)))
                return binaryData.GetStream();

            var isThumbnail = IsThumbnailImage(previewImage);

            //check restriction type
            var previewHead = NodeHead.Get(previewImage.Id);
            var rt = restrictionType.HasValue ? restrictionType.Value : GetRestrictionType(previewHead);
            var displayRedaction = (rt & RestrictionType.Redaction) == RestrictionType.Redaction;
            var displayWatermark = (rt & RestrictionType.Watermark) == RestrictionType.Watermark || GetDisplayWatermarkQueryParameter();

            if (!displayRedaction && !displayWatermark)
            {
                return binaryData.GetStream();
            }

            //load the parent document in elevated mode to have access to its properties
            var document = GetDocumentForPreviewImage(previewHead);

            var shapes = document != null ? (string)document["Shapes"] : null;
            var watermark = document != null ? document.Watermark : null;

            //if local watermark is empty, look for setting
            if (string.IsNullOrEmpty(watermark))
                watermark = Settings.GetValue<string>(DOCUMENTPREVIEW_SETTINGS, WATERMARK_TEXT, image.Path);

            //no redaction/highlight data found
            if (string.IsNullOrEmpty(shapes) && string.IsNullOrEmpty(watermark))
                return binaryData.GetStream();

            //return a memory stream containing the new image
            var ms = new MemoryStream();

            using (var img = System.Drawing.Image.FromStream(binaryData.GetStream()))
            {
                using (var g = Graphics.FromImage(img))
                {
                    //draw redaction
                    if (displayRedaction && !string.IsNullOrEmpty(shapes))
                    {
                        var imageIndex = GetPreviewImagePageIndex(previewImage);
                        var settings = new JsonSerializerSettings();
                        var serializer = JsonSerializer.Create(settings);
                        var jreader = new JsonTextReader(new StringReader(shapes));
                        var shapeCollection = (JArray)serializer.Deserialize(jreader);

                        foreach (var redaction in shapeCollection[0]["redactions"].Where(jt => (int)jt["imageIndex"] == imageIndex))
                        {
                            //var color = ParseColor(redaction["fill"].Value<string>());
                            var color = Color.Black;
                            var shapeBrush = new SolidBrush(color);
                            var shapeRectangle = new Rectangle(redaction["x"].Value<int>(), redaction["y"].Value<int>(),
                                                            redaction["w"].Value<int>(), redaction["h"].Value<int>());

                            //convert shape to thumbnail size if needed
                            if (isThumbnail)
                            {
                                shapeRectangle = new Rectangle(
                                    (int)Math.Round(shapeRectangle.X * THUMBNAIL_PREVIEW_WIDTH_RATIO),
                                    (int)Math.Round(shapeRectangle.Y * THUMBNAIL_PREVIEW_HEIGHT_RATIO),
                                    (int)Math.Round(shapeRectangle.Width * THUMBNAIL_PREVIEW_WIDTH_RATIO),
                                    (int)Math.Round(shapeRectangle.Height * THUMBNAIL_PREVIEW_HEIGHT_RATIO));
                            }

                            g.FillRectangle(shapeBrush, shapeRectangle);
                        }
                    }

                    //draw watermark
                    if (displayWatermark && !string.IsNullOrEmpty(watermark))
                    {
                        watermark = TemplateManager.Replace(typeof(WatermarkTemplateReplacer), watermark, new[] { document, image });

                        //check watermark master switch in settings
                        if (Settings.GetValue(DOCUMENTPREVIEW_SETTINGS, WATERMARK_ENABLED, image.Path, true))
                        {
                            var fontName = Settings.GetValue<string>(DOCUMENTPREVIEW_SETTINGS, WATERMARK_FONT, image.Path) ?? "Microsoft Sans Serif";
                            var fs = FontStyle.Regular;
                            if (Settings.GetValue(DOCUMENTPREVIEW_SETTINGS, WATERMARK_BOLD, image.Path, true))
                                fs = fs | FontStyle.Bold;
                            if (Settings.GetValue(DOCUMENTPREVIEW_SETTINGS, WATERMARK_ITALIC, image.Path, false))
                                fs = fs | FontStyle.Italic;

                            var size = Settings.GetValue(DOCUMENTPREVIEW_SETTINGS, WATERMARK_FONTSIZE, image.Path, 72.0f);

                            //resize font in case of thumbnails
                            if (isThumbnail)
                                size = size * THUMBNAIL_PREVIEW_WIDTH_RATIO;

                            var font = new Font(fontName, size, fs);

                            var textSize = g.MeasureString(watermark, font);
                            int charCount = watermark.Length;
                            float charSize = textSize.Width / charCount;
                            float wx = 0;
                            float wy = 0;

                            double maxTextWithOnImage = 0; //maximum width of a line
                            switch (Settings.GetValue(DOCUMENTPREVIEW_SETTINGS, WATERMARK_POSITION, image.Path, WatermarkPosition.BottomLeftToUpperRight))
                            {
                                case WatermarkPosition.BottomLeftToUpperRight:
                                    g.TranslateTransform(img.Width / 2, img.Height / 2);
                                    g.RotateTransform(-45);
                                    wx = -(textSize.Width / 2);
                                    wy = -(textSize.Height / 2);
                                    maxTextWithOnImage = Math.Sqrt((img.Width * img.Width) + (img.Height * img.Height)) * 0.7;
                                    break;
                                case WatermarkPosition.UpperLeftToBottomRight:
                                    g.TranslateTransform(img.Width / 2, img.Height / 2);
                                    g.RotateTransform(45);
                                    wx = -(textSize.Width / 2);
                                    wy = -(textSize.Height / 2);
                                    maxTextWithOnImage = Math.Sqrt((img.Width * img.Width) + (img.Height * img.Height)) * 0.7;
                                    break;
                                case WatermarkPosition.Top:
                                    wx = (img.Width - textSize.Width) / 2;
                                    wy = 10;
                                    maxTextWithOnImage = img.Width;
                                    break;
                                case WatermarkPosition.Bottom:
                                    wx = (img.Width - textSize.Width) / 2;
                                    wy = img.Height - textSize.Height - 10;
                                    maxTextWithOnImage = img.Width;
                                    break;
                                case WatermarkPosition.Center:
                                    wx = (img.Width - textSize.Width) / 2;
                                    wy = img.Height / 2 - textSize.Height / 2;
                                    maxTextWithOnImage = img.Width;
                                    break;
                                default:
                                    g.RotateTransform(45);
                                    break;
                            }

                            var color = Color.FromArgb(Settings.GetValue(DOCUMENTPREVIEW_SETTINGS, WATERMARK_OPACITY, image.Path, 50),
                                                       (Color)(new ColorConverter().ConvertFromString(Settings.GetValue(DOCUMENTPREVIEW_SETTINGS, WATERMARK_COLOR, image.Path, "Black"))));

                            //check if the watermark text must be broken into multiple lines
                            if (maxTextWithOnImage < textSize.Width)
                            {
                                int maxCharNumInLine = (int)Math.Round((maxTextWithOnImage - (maxTextWithOnImage * 0.2)) / charSize); //maximum number of characters in one line
                                int maxLineNumber = 3;
                                string[] words = watermark.Split(' ');
                                string[] lines = new string[maxLineNumber];

                                int lineNumber = 0;
                                int lineLength = 0;

                                for (int j = 0; j < words.Length; j++)
                                {
                                    if (lineNumber < maxLineNumber)
                                    {
                                        if (lineLength < maxCharNumInLine && (lineLength + words[j].Length + 1) < maxCharNumInLine)
                                        {
                                            if (lineLength == 0)
                                            {
                                                lines[lineNumber] = lines[lineNumber] + words[j];
                                                lineLength += words[j].Length;
                                            }
                                            else
                                            {
                                                lines[lineNumber] = lines[lineNumber] + " " + words[j];
                                                lineLength += (words[j].Length + 1);
                                            }
                                        }
                                        else
                                        {
                                            j--;
                                            lineLength = 0;
                                            lineNumber += 1;
                                        }
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }

                                //three lines
                                if (lines[2] != null)
                                {
                                    for (int j = 0; j < lines.Length; j++)
                                    {

                                        var w = g.MeasureString(lines[j], font); //current line width

                                        switch (Settings.GetValue(DOCUMENTPREVIEW_SETTINGS, WATERMARK_POSITION, image.Path, WatermarkPosition.BottomLeftToUpperRight))
                                        {
                                            case WatermarkPosition.BottomLeftToUpperRight:
                                                wx = -w.Width / 2;
                                                if (j == 0)
                                                {
                                                    wy = -(w.Height / 2) - (w.Height + 10);
                                                }
                                                else if (j == 1)
                                                {
                                                    wy = -(w.Height / 2);
                                                }
                                                else
                                                {
                                                    wy = -(w.Height / 2) + (w.Height + 10);
                                                }
                                                break;
                                            case WatermarkPosition.UpperLeftToBottomRight:
                                                wx = -w.Width / 2;
                                                if (j == 0)
                                                {
                                                    wy = -(w.Height / 2) - (w.Height + 10);
                                                }
                                                else if (j == 1)
                                                {
                                                    wy = -(w.Height / 2);
                                                }
                                                else
                                                {
                                                    wy = -(w.Height / 2) + (w.Height + 10);
                                                }
                                                break;
                                            case WatermarkPosition.Top:
                                                wx = (img.Width - w.Width) / 2;
                                                wy = (w.Height - 10) * j;
                                                break;
                                            case WatermarkPosition.Bottom:
                                                wx = (img.Width - w.Width) / 2;
                                                wy = img.Height - ((textSize.Height - 10) * (3 - j));
                                                break;
                                            case WatermarkPosition.Center:
                                                wx = (img.Width - w.Width) / 2;
                                                wy = img.Height / 2 - textSize.Height / 2;
                                                if (j == 0)
                                                {
                                                    wy = (img.Height / 2 - w.Height / 2) - (w.Height + 10);
                                                }
                                                else if (j == 1)
                                                {
                                                    wy = img.Height / 2 - w.Height / 2;
                                                }
                                                else
                                                {
                                                    wy = (img.Height / 2 - w.Height / 2) + (w.Height + 10);
                                                }
                                                break;
                                            default:
                                                break;
                                        }
                                        g.DrawString(lines[j], font, new SolidBrush(color), wx, wy);
                                    }
                                }
                                //two lines
                                else
                                {
                                    for (int j = 0; j < 2; j++)
                                    {

                                        var w = g.MeasureString(lines[j], font); //current line width

                                        switch (Settings.GetValue(DOCUMENTPREVIEW_SETTINGS, WATERMARK_POSITION, image.Path, WatermarkPosition.BottomLeftToUpperRight))
                                        {
                                            case WatermarkPosition.BottomLeftToUpperRight:
                                                wx = -w.Width / 2;
                                                wy = j == 0
                                                    ? -w.Height
                                                    : 0;
                                                break;
                                            case WatermarkPosition.UpperLeftToBottomRight:
                                                wx = -w.Width / 2;
                                                wy = j == 0
                                                    ? -w.Height
                                                    : 0;
                                                break;
                                            case WatermarkPosition.Top:
                                                wx = (img.Width - w.Width) / 2;
                                                wy = j == 0
                                                    ? 10
                                                    : 10 + w.Height;
                                                break;
                                            case WatermarkPosition.Bottom:
                                                wx = (img.Width - w.Width) / 2;
                                                wy = j == 0
                                                    ? img.Height - (2 * w.Height) - 10
                                                    : img.Height - w.Height - 10;
                                                break;
                                            case WatermarkPosition.Center:
                                                wx = (img.Width - w.Width) / 2;
                                                wy = j == 0
                                                    ? (img.Height / 2 - w.Height / 2) - w.Height
                                                    : (img.Height / 2 - w.Height / 2);

                                                break;
                                            default:
                                                break;
                                        }

                                        g.DrawString(lines[j], font, new SolidBrush(color), wx, wy);

                                    }
                                }
                            }
                            //one line
                            else
                            {
                                g.DrawString(watermark, font, new SolidBrush(color), wx, wy);
                            }

                        }
                    }

                    g.Save();
                }

                ImageFormat imgFormat;

                switch (Path.GetExtension(previewImage.Path).ToLower())
                {
                    case ".png":
                        imgFormat = ImageFormat.Png;
                        break;
                    case ".jpg":
                    case ".jpeg":
                        imgFormat = ImageFormat.Jpeg;
                        break;
                    case ".bmp":
                        imgFormat = ImageFormat.Bmp;
                        break;
                    default:
                        throw new NotImplementedException("Unknown image preview type: " + previewImage.Path);
                }

                img.Save(ms, imgFormat);
            }

            ms.Seek(0, SeekOrigin.Begin);

            return ms;
        }

        public Stream GetPreviewImagesDocumentStream(Content content, DocumentFormat? documentFormat = null, RestrictionType? restrictionType = null)
        {
            if (!documentFormat.HasValue)
                documentFormat = DocumentFormat.NonDefined;

            var pImages = GetPreviewImages(content);
            return GetPreviewImagesDocumentStream(content, pImages.AsEnumerable().Select(c => c.ContentHandler as Image), documentFormat.Value, restrictionType);
        }

        protected virtual Stream GetPreviewImagesDocumentStream(Content content, IEnumerable<Image> previewImages, DocumentFormat documentFormat, RestrictionType? restrictionType = null)
        {
            throw new NotImplementedException("Please implement PDF generator mechanism in your custom preview provider.");
        }

        protected virtual int GetPreviewImagePageIndex(Image image)
        {
            if (image == null)
                return 0;

            // preview5.png --> 5
            var r = new Regex(PREVIEW_THUMBNAIL_REGEX);
            var m = r.Match(image.Name);

            return m.Success ? Convert.ToInt32(m.Groups["page"].Value) : 0;
        }

        protected virtual Node GetPreviewsFolder(Node content)
        {
            var previewsFolderPath = RepositoryPath.Combine(content.Path, PREVIEWS_FOLDERNAME);
            var previewsFolder = Node.LoadNode(previewsFolderPath);
            if (previewsFolder == null)
            {
                using (new SystemAccount())
                {
                    try
                    {
                        previewsFolder = new SystemFolder(content) { Name = PREVIEWS_FOLDERNAME };
                        previewsFolder.DisableObserver(TypeHandler.GetType(NodeObserverNames.NOTIFICATION));
                        previewsFolder.DisableObserver(TypeHandler.GetType(NodeObserverNames.WORKFLOWNOTIFICATION));
                        previewsFolder.Save();
                    }
                    catch (NodeAlreadyExistsException)
                    {
                        // no problem, reload to have the correct node
                        previewsFolder = Node.LoadNode(previewsFolderPath);
                    }
                }
            }

            var previewSubfolderName = GetPreviewsSubfolderName(content);
            var previewsSubfolderPath = RepositoryPath.Combine(previewsFolderPath, previewSubfolderName);
            var previewsSubfolder = Node.LoadNode(previewsSubfolderPath);
            if (previewsSubfolder == null)
            {
                using (new SystemAccount())
                {
                    try
                    {
                        previewsSubfolder = new SystemFolder(previewsFolder) { Name = previewSubfolderName };
                        previewsSubfolder.DisableObserver(TypeHandler.GetType(NodeObserverNames.NOTIFICATION));
                        previewsSubfolder.DisableObserver(TypeHandler.GetType(NodeObserverNames.WORKFLOWNOTIFICATION));
                        previewsSubfolder.Save();
                    }
                    catch (NodeAlreadyExistsException)
                    {
                        // no problem, reload to have the correct node
                        previewsSubfolder = Node.LoadNode(previewsSubfolderPath);
                    }
                }
            }

            return previewsSubfolder;
        }
        protected virtual Node EmptyPreviewsFolder(Node previews)
        {
            var gc = previews as GenericContent;
            if (gc == null)
                return null;

            using (new SystemAccount())
            {
                var parent = previews.Parent;
                var name = previews.Name;
                var type = previews.NodeType.Name;

                previews.DisableObserver(TypeHandler.GetType(NodeObserverNames.NOTIFICATION));
                previews.ForceDelete();

                var content = Content.CreateNew(type, parent, name);
                content.ContentHandler.DisableObserver(TypeHandler.GetType(NodeObserverNames.NOTIFICATION));
                content.ContentHandler.DisableObserver(TypeHandler.GetType(NodeObserverNames.WORKFLOWNOTIFICATION));
                content.Save();

                return Node.LoadNode(content.Id);
            }
        }

        //===================================================================================================== Static access

        public static void StartPreviewGeneration(Node node, TaskPriority priority = TaskPriority.Normal)
        {
            var previewProvider = DocumentPreviewProvider.Current;
            if (previewProvider == null)
                return;

            //check if the feature is enabled on the content type
            var content = Content.Create(node);
            if (!content.ContentType.Preview)
                return;

            // check if content is supported by the provider. if not, don't bother starting the preview generation)
            if (!previewProvider.IsContentSupported(node) || previewProvider.IsPreviewOrThumbnailImage(NodeHead.Get(node.Id)))
            {
                DocumentPreviewProvider.SetPreviewStatus(node as File, PreviewStatus.NotSupported);
                return;
            }

            DocumentPreviewProvider.StartPreviewGenerationInternal(node, priority: priority);
        }

        private static void StartPreviewGenerationInternal(Node relatedContent, int startIndex = 0, TaskPriority priority = TaskPriority.Normal)
        {
            if (DocumentPreviewProvider.Current == null || DocumentPreviewProvider.Current is DefaultDocumentPreviewProvider)
            {
                Logger.WriteVerbose("Preview image generation is available only in the enterprise edition. No document preview provider is present.");
                return;
            }

            string previewData;
            var maxPreviewCount = Settings.GetValue(DOCUMENTPREVIEW_SETTINGS, MAXPREVIEWCOUNT, relatedContent.Path, 10);
            var roundedStartIndex = startIndex - startIndex % maxPreviewCount;

            // serialize data for preview generator task (json format)
            var serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());

            using (var sw = new StringWriter())
            {
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, new
                    {
                        Id = relatedContent.Id,
                        Version = relatedContent.Version.ToString(),
                        StartIndex = roundedStartIndex,
                        MaxPreviewCount = maxPreviewCount
                    });
                }

                previewData = sw.GetStringBuilder().ToString();
            }

            // start generating previews only if there is a task type defined
            if (!string.IsNullOrEmpty(DocumentPreviewProvider.Current.PreviewGeneratorTaskName))
                TaskManager.RegisterTask(DocumentPreviewProvider.Current.PreviewGeneratorTaskName, priority, previewData);
        }

        public static void SetPreviewStatus(File file, PreviewStatus status)
        {
            if (file == null)
                return;

            if (status == PreviewStatus.Ready)
                throw new NotSupportedException("Setting preview status to Ready is not supported. This scenario is handled by the document preview provider itself.");

            try
            {
                SavePageCount(file, (int)status);
            }
            catch (Exception ex)
            {
                Logger.WriteWarning(EventId.Preview.PreviewGenerationStatusError, "Error setting preview status. " + ex,
                                  properties: new Dictionary<string, object>
                                                  {
                                                      {"Path", file.Path},
                                                      {"Status", Enum.GetName(typeof (PreviewStatus), status)}
                                                  });
            }
        }

        //===================================================================================================== OData interface

        [ODataFunction]
        public static IEnumerable<Content> GetPreviewImagesForOData(Content content)
        {
            return Current != null ? Current.GetPreviewImages(content) : null;
        }

        [ODataFunction]
        public static object PreviewAvailable(Content content, int page)
        {
            var thumb = Current != null ? Current.GetThumbnailImage(content, page) : null;
            if (thumb != null)
            {
                var pi = Current != null ? Current.GetPreviewImage(content, page) : null;
                if (pi != null)
                {
                    return new
                    {
                        PreviewAvailable = pi.Path,
                        Width = (int)pi["Width"],
                        Height = (int)pi["Height"]
                    };
                }
            }

            return new { PreviewAvailable = (string)null };
        }

        [ODataAction]
        public static int GetPageCount(Content content)
        {
            var pageCount = (int)content["PageCount"];
            var file = content.ContentHandler as File;

            //default DocumentPreviewProvider is the current provider --> set status to noprovider
            if (DocumentPreviewProvider.Current is DefaultDocumentPreviewProvider && pageCount == -4)
            {
                pageCount = (int)PreviewStatus.NoProvider;
            }
            else
            {
                if (pageCount == -4)
                {
                    //status is postponed --> set status to inprogress and start preview generation
                    SetPreviewStatus(file, PreviewStatus.InProgress);

                    pageCount = (int)PreviewStatus.InProgress;
                    StartPreviewGeneration(file, TaskPriority.Immediately);
                }
                else if (pageCount == -1)
                {
                    StartPreviewGeneration(file, TaskPriority.Immediately);
                }
            }
            return pageCount;
        }

        [ODataAction]
        public static object GetPreviewsFolder(Content content, bool empty)
        {
            if (content == null)
                throw new ArgumentNullException("content");

            var previewsFolder = DocumentPreviewProvider.Current.GetPreviewsFolder(content.ContentHandler);

            if (empty)
                previewsFolder = DocumentPreviewProvider.Current.EmptyPreviewsFolder(previewsFolder);

            return new
            {
                Id = previewsFolder.Id,
                Path = previewsFolder.Path
            };
        }

        [ODataAction]
        public static void SetPreviewStatus(Content content, PreviewStatus status)
        {
            if (content == null)
                throw new ArgumentNullException("content");

            SetPreviewStatus(content.ContentHandler as File, status);
        }

        [ODataAction]
        public static void SetPageCount(Content content, int pageCount)
        {
            if (content == null)
                throw new ArgumentNullException("content");

            SavePageCount(content.ContentHandler as File, pageCount);
        }

        [ODataAction]
        public static void SetInitialPreviewProperties(Content content)
        {
            if (content == null)
                throw new ArgumentNullException("content");

            var previewImage = content.ContentHandler as Image;
            if (previewImage == null)
                throw new InvalidOperationException("This content is not an image.");

            var document = GetDocumentForPreviewImage(NodeHead.Get(content.Id));
            if (document == null)
                throw new InvalidOperationException("Document not found for preview image: " + content.Path);

            var realCreatorUser = document.CreatedBy;

            // set the creator/modifier user of the preview image: it should be 
            // the owner of the document, instead of admin
            previewImage.CreatedBy = realCreatorUser;
            previewImage.ModifiedBy = realCreatorUser;
            previewImage.VersionCreatedBy = realCreatorUser;
            previewImage.VersionModifiedBy = realCreatorUser;
            previewImage.Index = DocumentPreviewProvider.Current.GetPreviewImagePageIndex(previewImage);

            previewImage.Save(SavingMode.KeepVersion);
        }

        [ODataAction]
        public static void RegeneratePreviews(Content content)
        {
            if (content == null)
                throw new ArgumentNullException("content");

            // Regardless of the current status, generate preview images again
            // (e.g. because previously there was an error).
            SetPreviewStatus(content.ContentHandler as File, PreviewStatus.InProgress);
            StartPreviewGeneration(content.ContentHandler, TaskPriority.Immediately);
        }
    }

    public sealed class DefaultDocumentPreviewProvider : DocumentPreviewProvider
    {
        public override string PreviewGeneratorTaskName
        {
            get { return string.Empty; }
        }

        public override bool IsContentSupported(Node content)
        {
            //in community edition support only files stored in libraries
            return content != null && content is File && content.ContentListId > 0;
        }
    }
}