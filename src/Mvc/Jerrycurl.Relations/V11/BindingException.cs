using Jerrycurl.Reflection;
using Jerrycurl.Relations.V11;
using Jerrycurl.Relations.V11.Internal;
using System;
using System.Runtime.Serialization;

namespace Jerrycurl.Relations.V11
{
    [Serializable]
    public class BindingException2 : Exception
    {
        public BindingException2()
        {

        }

        public BindingException2(string message)
            : base(message)
        {

        }

        public BindingException2(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        protected BindingException2(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        #region " Exception helpers "

        public static BindingException2 From(IField2 field, string message = null, Exception innerException = null)
        {
            string fullMessage = $"Error binding to {field.Identity} in {field.Metadata.Identity.Schema}.";

            if (message != null || innerException != null)
                fullMessage += $" {message ?? innerException.Message}";

            return new BindingException2(fullMessage, innerException);
        }

        #endregion
    }
}
