using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.Packaging
{
    [Serializable]
    public class InvalidStepParameterException : PackagingException
    {
        public InvalidStepParameterException() { }
        public InvalidStepParameterException(string message) : base(message) { }
        public InvalidStepParameterException(string message, Exception inner) : base(message, inner) { }
        protected InvalidStepParameterException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
