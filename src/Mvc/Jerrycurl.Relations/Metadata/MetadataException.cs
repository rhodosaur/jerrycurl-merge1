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

        internal static MetadataException NotFound<TMetadata>(string attributeName) where TMetadata : IMetadata
            => new MetadataException($"Metadata of type {typeof(TMetadata).Name} was not found for attribute '{attributeName}'.");
    }
}
