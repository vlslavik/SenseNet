using System;
using System.Configuration;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage
{
    public interface IPreviewProvider
    {
        bool HasPreviewPermission(NodeHead nodeHead);
    }

    /// <summary>
    /// This internal class was created to make the DocumentPreviewProvider feature (that resides up in the ContentRepository layer) accessible here in the Storage layer.
    /// </summary>
    internal class PreviewProvider
    {
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

        //============================================================================== Static internal API

        private static IPreviewProvider _previewProvider;
        private static readonly object _previewLock = new object();
        private static bool _isInitialized;

        /// <summary>
        /// Instance of a DocumentPreviewProvider in the Storage layer. This property is a duplicate of the Current property of the DocumentPreviewProvider class.
        /// </summary>
        private static IPreviewProvider Current
        {
            get
            {
                if ((_previewProvider == null) && (!_isInitialized))
                {
                    lock (_previewLock)
                    {
                        if ((_previewProvider == null) && (!_isInitialized))
                        {
                            try
                            {
                                if (!string.IsNullOrEmpty(DocumentPreviewProviderClassName))
                                    _previewProvider = (IPreviewProvider)TypeHandler.CreateInstance(DocumentPreviewProviderClassName);
                                else
                                    _previewProvider = (IPreviewProvider)TypeHandler.CreateInstance(DEFAULT_DOCUMENTPREVIEWPROVIDER_CLASSNAME);
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

                            if (_previewProvider == null)
                                Logger.WriteInformation(Logger.EventId.NotDefined, "DocumentPreviewProvider not present.");
                            else
                                Logger.WriteInformation(Logger.EventId.NotDefined, "DocumentPreviewProvider created: " + _previewProvider.GetType().FullName);
                        }
                    }
                }
                return _previewProvider;
            }
        }

        internal static bool HasPreviewPermission(NodeHead nodeHead)
        {
            return Current != null && Current.HasPreviewPermission(nodeHead);
        }
    }
}
