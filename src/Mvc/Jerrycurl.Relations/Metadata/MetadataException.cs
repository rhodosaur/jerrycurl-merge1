using Jerrycurl.Reflection;
using System;
using System.Runtime.Serialization;

namespace Jerrycurl.Relations.Metadata
{
    [Serializable]
    public class MetadataException : Exception
    {
        public MetadataException()
        {

        }

        public MetadataException(string message)
            : base(message)
        {

        }

        public MetadataException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        protected MetadataException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
    }
}
