using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.Packaging
{
    [Serializable]
    public class InvalidPackageException : PackagingException
    {
        public InvalidPackageException(string message) : base(EventId.InvalidPackage, message) { }
        public InvalidPackageException(string message, Exception inner) : base(EventId.InvalidPackage, message, inner) { }
        protected InvalidPackageException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
    [Serializable]
    public class PackagePreconditionException : PackagingException
    {
        public PackagePreconditionException(string message) : base(EventId.PackagePrecondition, message) { }
        public PackagePreconditionException(string message, Exception inner) : base(EventId.PackagePrecondition, message, inner) { }
        protected PackagePreconditionException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
