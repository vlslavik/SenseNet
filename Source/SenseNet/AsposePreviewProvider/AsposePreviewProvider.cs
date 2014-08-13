using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aspose.Cells;
using Aspose.Pdf;
using Aspose.Pdf.Generator;
using Aspose.Slides;
using Aspose.Words;
using Aspose.Words.Drawing;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Preview;
using SenseNet.Diagnostics;
using SNCR = SenseNet.ContentRepository;
using STORAGE = SenseNet.ContentRepository.Storage;

namespace AsposePreviewProvider
{
    public class AsposePreviewProvider : DocumentPreviewProvider
    {
        protected enum LicenseProvider { Words, Diagram, Cells, Pdf, Slides, Tasks, Imaging }

        //===================================================================================================== Constants

        private static readonly string LICENSEPATH = "Aspose.Total.lic";

        // The duplicate of this list exists in the AsposePreviewGenerator project! 
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
        
        //missing: ".emf", ".pcl", ".wmf", ".xps"
        internal static readonly string[] SUPPORTED_EXTENSIONS = WORD_EXTENSIONS
            .Concat(DIAGRAM_EXTENSIONS)
            .Concat(IMAGE_EXTENSIONS)
            .Concat(TIFF_EXTENSIONS)
            .Concat(WORKBOOK_EXTENSIONS)
            .Concat(PDF_EXTENSIONS)
            .Concat(PRESENTATION_EXTENSIONS)
            .Concat(PRESENTATIONEX_EXTENSIONS)
            .Concat(PROJECT_EXTENSIONS)
            .ToArray();

        //===================================================================================================== Properties

        protected static bool LicenseChecked { get; set; }

        //===================================================================================================== Overrides

        public override string PreviewGeneratorTaskName { get { return "AsposePreviewGenerator"; } }
        
        public override bool IsContentSupported(STORAGE.Node content)
        {
            return SUPPORTED_EXTENSIONS.Contains(ContentNamingHelper.GetFileExtension(content.Name).ToLower());
        }

        protected override Stream GetPreviewImagesDocumentStream(Content content, IEnumerable<SNCR.Image> previewImages, DocumentFormat documentFormat, RestrictionType? restrictionType = null)
        {
            if (documentFormat == DocumentFormat.NonDefined)
                documentFormat = GetFormatByName(content.Name);

            //Unfortunately we need to create a new memory stream here
            //instead of writing into the output stream directly, because
            //Aspose needs to Seek the stream during document creation, 
            //which is not supported by the Http Response output stream.

            switch (documentFormat)
            {
                case DocumentFormat.Doc:
                case DocumentFormat.Docx: return GetPreviewImagesWordStream(content, previewImages, restrictionType);
                case DocumentFormat.NonDefined:
                case DocumentFormat.Pdf: return GetPreviewImagesPdfStream(content, previewImages, restrictionType);
                case DocumentFormat.Ppt:
                case DocumentFormat.Pptx: return GetPreviewImagesPowerPointStream(content, previewImages, restrictionType);
                case DocumentFormat.Xls:
                case DocumentFormat.Xlsx: return GetPreviewImagesExcelStream(content, previewImages, restrictionType);
            }

            return null;
        }

        //===================================================================================================== Generate documents

        protected Stream GetPreviewImagesPdfStream(Content content, IEnumerable<SNCR.Image> previewImages, RestrictionType? restrictionType = null)
        {
            CheckLicense(LicenseProvider.Pdf);

            try
            {
                var ms = new MemoryStream();
                var pdf = new Pdf();
                var document = new Aspose.Pdf.Document(pdf);
                var index = 1;

                foreach (var previewImage in previewImages.Where(previewImage => previewImage != null))
                {
                    using (var imgStream = GetRestrictedImage(previewImage, restrictionType: restrictionType))
                    {
                        int newWidth;
                        int newHeight;

                        ComputeResizedDimensions((int)previewImage["Width"], (int)previewImage["Height"], PREVIEW_PDF_WIDTH, PREVIEW_PDF_HEIGHT, out newWidth, out newHeight);

                        var imageStamp = new ImageStamp(imgStream)
                                             {
                                                 TopMargin = 10,
                                                 HorizontalAlignment = Aspose.Pdf.HorizontalAlignment.Center,
                                                 VerticalAlignment = Aspose.Pdf.VerticalAlignment.Top,
                                                 Width = newWidth,
                                                 Height = newHeight
                                             };

                        try
                        {
                            var page = index == 1 ? document.Pages[1] : document.Pages.Add();
                            page.AddStamp(imageStamp);
                        }
                        catch (IndexOutOfRangeException ex)
                        {
                            Logger.WriteException(new Exception("Error during pdf generation. Path: " + previewImage.Path, ex));
                            break;
                        }
                    }

                    index++;
                }

                document.Save(ms);

                ms.Seek(0, SeekOrigin.Begin);
                return ms;
            }
            catch (Exception ex)
            {
                Logger.WriteException(ex);
            }

            return null;
        }

        protected Stream GetPreviewImagesWordStream(Content content, IEnumerable<SNCR.Image> previewImages, RestrictionType? restrictionType = null)
        {
            CheckLicense(LicenseProvider.Words);

            try
            {
                var ms = new MemoryStream();
                var document = new Aspose.Words.Document();
                var builder = new DocumentBuilder(document);
                var index = 1;
                var saveFormat = content.Name.ToLower().EndsWith(".docx") ? Aspose.Words.SaveFormat.Docx : Aspose.Words.SaveFormat.Doc;

                foreach (var previewImage in previewImages.Where(previewImage => previewImage != null))
                {
                    int newWidth;
                    int newHeight;

                    ComputeResizedDimensions((int)previewImage["Width"], (int)previewImage["Height"], PREVIEW_WORD_WIDTH, PREVIEW_WORD_HEIGHT, out newWidth, out newHeight);

                    using (var imgStream = GetRestrictedImage(previewImage, restrictionType: restrictionType))
                    {
                        var image = System.Drawing.Image.FromStream(imgStream);

                        try
                        {
                            //skip to the next page
                            if (index > 1)
                                builder.Writeln("");

                            builder.InsertImage(image,
                                                RelativeHorizontalPosition.LeftMargin,
                                                -5,
                                                RelativeVerticalPosition.TopMargin,
                                                -50,
                                                newWidth,
                                                newHeight,
                                                WrapType.Square);
                        }
                        catch (IndexOutOfRangeException ex)
                        {
                            Logger.WriteException(
                                new Exception("Error during document generation. Path: " + previewImage.Path, ex));
                            break;
                        }
                    }

                    index++;
                }

                document.Save(ms, saveFormat);

                ms.Seek(0, SeekOrigin.Begin);
                return ms;
            }
            catch (Exception ex)
            {
                Logger.WriteException(ex);
            }

            return null;
        }

        protected Stream GetPreviewImagesExcelStream(Content content, IEnumerable<SNCR.Image> previewImages, RestrictionType? restrictionType = null)
        {
            CheckLicense(LicenseProvider.Cells);

            try
            {
                var ms = new MemoryStream();
                var oldExcel = content.Name.ToLower().EndsWith(".xls");
                var fileFormat = oldExcel ? FileFormatType.Excel97To2003 : FileFormatType.Xlsx;
                var saveFormat = oldExcel ? Aspose.Cells.SaveFormat.Excel97To2003 : Aspose.Cells.SaveFormat.Xlsx;
                var document = new Workbook(fileFormat);
                var index = 1;

                foreach (var previewImage in previewImages.Where(previewImage => previewImage != null))
                {
                    using (var imgStream = GetRestrictedImage(previewImage, restrictionType: restrictionType))
                    {
                        var image = System.Drawing.Image.FromStream(imgStream);
                        var imageForDocument = ResizeImage(image, Math.Min(image.Width, PREVIEW_EXCEL_WIDTH), Math.Min(image.Height, PREVIEW_EXCEL_HEIGHT));

                        if (imageForDocument != null)
                        {
                            using (var imageStream = new MemoryStream())
                            {
                                imageForDocument.Save(imageStream, PREVIEWIMAGEFORMAT);

                                try
                                {
                                    var ws = index == 1 ? document.Worksheets[0] : document.Worksheets.Add("Sheet" + index);
                                    ws.Pictures.Add(0, 0, imageStream);
                                }
                                catch (IndexOutOfRangeException ex)
                                {
                                    Logger.WriteException(new Exception("Error during document generation. Path: " + previewImage.Path, ex));
                                    break;
                                }
                            }
                        }
                    }

                    index++;
                }

                document.Save(ms, saveFormat);
                ms.Seek(0, SeekOrigin.Begin);

                return ms;
            }
            catch (Exception ex)
            {
                Logger.WriteException(ex);
            }

            return null;
        }

        protected Stream GetPreviewImagesPowerPointStream(Content content, IEnumerable<SNCR.Image> previewImages, RestrictionType? restrictionType = null)
        {
            CheckLicense(LicenseProvider.Slides);

            try
            {
                var ms = new MemoryStream();
                var extension = ContentNamingHelper.GetFileExtension(content.Name).ToLower();
                var oldPpt = PRESENTATION_EXTENSIONS.Contains(extension);
                var saveFormat = oldPpt ? Aspose.Slides.Export.SaveFormat.Ppt : Aspose.Slides.Export.SaveFormat.Pptx;
                var docPresentation = new Presentation();
                var index = 1;

                foreach (var previewImage in previewImages.Where(previewImage => previewImage != null))
                {
                    using (var imgStream = GetRestrictedImage(previewImage, restrictionType: restrictionType))
                    {
                        var image = System.Drawing.Image.FromStream(imgStream);
                        var imageForDocument = ResizeImage(image, Math.Min(image.Width, PREVIEW_POWERPOINT_WIDTH), Math.Min(image.Height, PREVIEW_POWERPOINT_HEIGHT));

                        if (imageForDocument != null)
                        {
                            try
                            {
                                var img = docPresentation.Images.AddImage(imageForDocument);
                                var slide = docPresentation.Slides[0];
                                if (index > 1)
                                {
                                    docPresentation.Slides.AddClone(slide);
                                    slide = docPresentation.Slides[index - 1];
                                }

                                slide.Shapes.AddPictureFrame(Aspose.Slides.ShapeType.Rectangle, 10, 10,
                                                             imageForDocument.Width, imageForDocument.Height,
                                                             img);
                            }
                            catch (IndexOutOfRangeException ex)
                            {
                                Logger.WriteException(new Exception("Error during document generation. Path: " + previewImage.Path, ex));
                                break;
                            }
                        }
                    }

                    index++;
                }
                
                docPresentation.Save(ms, saveFormat);
                ms.Seek(0, SeekOrigin.Begin);

                return ms;
            }
            catch (Exception ex)
            {
                Logger.WriteException(ex);
            }

            return null;
        }

        //===================================================================================================== Helper methods

        protected static void SetIndexes(string path, int originalStartIndex, int pageCount, out int startIndex, out int lastIndex)
        {
            var imageCount = Settings.GetValue(DOCUMENTPREVIEW_SETTINGS, MAXPREVIEWCOUNT, path, 10);

            SetIndexes(path, originalStartIndex, pageCount, out startIndex, out lastIndex, imageCount);
        }

        protected static void SetIndexes(string path, int originalStartIndex, int pageCount, out int startIndex, out int lastIndex, int maxPreviewCount)
        {
            startIndex = Math.Min(originalStartIndex, pageCount - 1);
            lastIndex = Math.Min(startIndex + maxPreviewCount - 1, pageCount - 1);
        }

        protected static void CheckLicense(LicenseProvider provider)
        {
            try
            {
                switch (provider)
                {
                    case LicenseProvider.Cells:
                        var license1 = new Aspose.Cells.License();
                        license1.SetLicense(LICENSEPATH);
                        break;
                    case LicenseProvider.Diagram:
                        var license2 = new Aspose.Diagram.License();
                        license2.SetLicense(LICENSEPATH);
                        break;
                    case LicenseProvider.Pdf:
                        var license3 = new Aspose.Pdf.License();
                        license3.SetLicense(LICENSEPATH);
                        break;
                    case LicenseProvider.Slides:
                        var license4 = new Aspose.Slides.License();
                        license4.SetLicense(LICENSEPATH);
                        break;
                    case LicenseProvider.Words:
                        var license5 = new Aspose.Words.License();
                        license5.SetLicense(LICENSEPATH);
                        break;
                    case LicenseProvider.Tasks:
                        var license6 = new Aspose.Tasks.License();
                        license6.SetLicense(LICENSEPATH);
                        break;
                    case LicenseProvider.Imaging:
                        var license7 = new Aspose.Imaging.License();
                        license7.SetLicense(LICENSEPATH);
                        break;
                }
            }
            catch (Exception ex)
            {
                WriteLicenseException(ex);
            }
        }

        protected static void WriteLicenseException(Exception ex)
        {
            var lex = new Exception("There was an error using Apose License (" + LICENSEPATH + ")", ex);
            Logger.WriteException(lex);
        }

        protected static DocumentFormat GetFormatByName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return DocumentFormat.Pdf;

            var extension = ContentNamingHelper.GetFileExtension(fileName).ToLower();

            if (PDF_EXTENSIONS.Contains(extension))
                return DocumentFormat.Pdf;

            if (PRESENTATION_EXTENSIONS.Contains(extension))
                return DocumentFormat.Ppt;

            if (PRESENTATIONEX_EXTENSIONS.Contains(extension))
                return DocumentFormat.Pptx;

            if (WORKBOOK_EXTENSIONS.Contains(extension))
                return extension.EndsWith("x") ? DocumentFormat.Xlsx : DocumentFormat.Xls;

            if (WORD_EXTENSIONS.Contains(extension))
                return extension.EndsWith("x") ? DocumentFormat.Docx : DocumentFormat.Doc;

            return DocumentFormat.Pdf;
        }
    }
}
