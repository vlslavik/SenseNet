using System;
using SenseNet.ContentRepository.Security;

namespace SenseNet.Portal
{
    public class Security
    {
        [Obsolete("Use SenseNet.ContentRepository.Security.Sanitizer.Sanitize instead.")]
        public static string Sanitize(string userInput)
        {
            return Sanitizer.Sanitize(userInput);
        }
    }
}
