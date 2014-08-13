using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.i18n;

namespace SenseNet.Portal.Portlets
{
    internal class SR
    {
        internal class Exceptions
        {
            internal class ContentView
            {
                public static string NotFound = "$Error_Portlets:ContentView_NotFound";
            }
        }

        public static string GetString(string fullResourceKey)
        {
            return SenseNetResourceManager.Current.GetString(fullResourceKey);
        }

        public static string GetString(string fullResourceKey, params object[] args)
        {
            return String.Format(GetString(fullResourceKey), args);
        }
    }
}
