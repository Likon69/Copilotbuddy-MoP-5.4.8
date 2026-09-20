using System;
using System.Runtime.Serialization;

namespace Styx
{
    /// <summary>
    /// Represents an exception that should be logged to the user.
    /// </summary>
    [Serializable]
    public class UserException : Exception
    {
        public UserException() { }
        public UserException(string message) : base(message) { }
        public UserException(string message, Exception inner) : base(message, inner) { }
        protected UserException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public override string HelpLink { get { return "http://www.honorbuddy.com"; } }
    }
}
