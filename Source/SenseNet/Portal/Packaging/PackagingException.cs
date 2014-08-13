using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Packaging
{
    [Serializable]
    public class PackagingException : ApplicationException
    {
        public PackagingException() { Initialize(EventId.PackageError); }
        public PackagingException(string message) : base(message) { Initialize(EventId.PackageError); }
        public PackagingException(string message, Exception inner) : base(message, inner) { Initialize(EventId.PackageError); }
        public PackagingException(int eventId) { Initialize(eventId); }
        public PackagingException(int eventId, string message) : base(message) { Initialize(eventId); }
        public PackagingException(int eventId, string message, Exception inner) : base(message, inner) { Initialize(eventId); }
        protected PackagingException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        private void Initialize(int eventId)
        {
            this.Data.Add("EventId", eventId);
        }
    }
}
