using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository
{
    public enum InvalidContentActionReason
    {
        NotSpecified,
        UnknownAction,
        InvalidStateAction,
        CheckedOutToSomeoneElse,
        UndoSingleVersion,
        NotEnoughPermissions,
        MultistepSaveInProgress
    }

    [global::System.Serializable]
    public class InvalidContentActionException : Exception
    {
        protected static string Error_InvalidContentAction = "$Error_ContentRepository:InvalidContentAction_";

        public InvalidContentActionReason Reason { get; private set; }
        public string Path { get; private set; }

        public InvalidContentActionException(InvalidContentActionReason reason, string path) : this(reason, path, GetMessage(reason)) {}
        public InvalidContentActionException(InvalidContentActionReason reason, string path, string message) : base(message)
        {
            Reason = reason;
            Path = path;
        }

        public InvalidContentActionException(string message) : base(message) { }
        public InvalidContentActionException(string message, Exception inner) : base(message, inner) { }

        protected InvalidContentActionException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
            Reason = (InvalidContentActionReason)info.GetInt32("reason");
            Path = info.GetString("path");
        }

        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("reason", (int)Reason);
            info.AddValue("path", Path);
        }

        protected static string GetMessage(InvalidContentActionReason reason)
        {
            return SR.GetString(Error_InvalidContentAction + Enum.GetName(typeof (InvalidContentActionReason), reason));
        }
    }
}
