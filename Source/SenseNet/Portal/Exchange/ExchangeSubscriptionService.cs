using System;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Mail;
using SenseNet.ContentRepository.Storage;
using SenseNet.Search;
using SenseNet.Diagnostics;

namespace SenseNet.Portal.Exchange
{
    public class ExchangeSubscriptionService : ISnService
    {
        public bool Start()
        {
            if (Settings.GetValue(MailHelper.MAILPROCESSOR_SETTINGS, MailHelper.SETTINGS_MODE, null, MailProcessingMode.ExchangePull) != MailProcessingMode.ExchangePush)
                return false;

            // renew subscriptions
            //  1: go through doclibs with email addresses
            var doclibs = ContentQuery.Query("+TypeIs:DocumentLibrary +ListEmail:* -ListEmail:\"\"");
            if (doclibs.Count > 0)
            {
                Logger.WriteInformation(Logger.EventId.NotDefined, String.Concat("Exchange subscription service enabled, running subscriptions (", doclibs.Count.ToString(), " found)"), ExchangeHelper.ExchangeLogCategory);
                foreach (var doclib in doclibs.Nodes)
                {
                    try
                    {
                        ExchangeHelper.Subscribe(doclib);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteException(ex, ExchangeHelper.ExchangeLogCategory);
                    }
                }
            }
            else
            {
                Logger.WriteInformation(Logger.EventId.NotDefined, "Exchange subscription service enabled, no subscriptions found.", ExchangeHelper.ExchangeLogCategory);
            }

            return true;
        }

        /// <summary>
        /// Shuts down the service. Called when the Repository is finishing.
        /// </summary>
        public void Shutdown()
        {
        }
    }
}
