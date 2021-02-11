using Jerrycurl.Reflection;
using Jerrycurl.Relations.Metadata;
using System;
using System.Linq;
using System.Runtime.Serialization;

namespace Jerrycurl.Data.Metadata
{
    [Serializable]
    public class BindingException : Exception
    {
        public BindingException()
        {

        }

        public BindingException(string message)
            : base(message)
        {

        }

        public BindingException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        protected BindingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }


        public static BindingException Create(MetadataIdentity metadata, string message = null, Exception innerException = null)
        {
            message ??= innerException?.Message;

            if (message != null)
                return new BindingException($"Cannot bind to {metadata}: {message}", innerException);

            return new BindingException($"Cannot bind to {metadata}.", innerException);
        }

        internal static BindingException IsReadOnly(IBindingMetadata metadata)
            => Create(metadata.Identity, message: "Data is read-only.");

        internal static BindingException InvalidCast(IBindingMetadata metadata, Exception innerException)
            => Create(metadata.Identity, innerException: innerException);

        internal static BindingException InvalidConstructor(IBindingMetadata metadata)
            => Create(metadata.Identity, message: "No valid constructor found.");

        internal static BindingException NoReferenceFound(MetadataIdentity from, MetadataIdentity to)
            => new BindingException($"No valid reference found between {from} and {to}. Please specify matching [Key] and [Ref] annotations to map across one-to-many boundaries.");

        internal static BindingException IncompatibleReference(IReference reference)
        {
            string leftTuple = $"({string.Join(", ", reference.Key.Properties.Select(m => m.Type.GetSanitizedName()))})";
            string rightTuple = $"({string.Join(", ", reference.Other.Key.Properties.Select(m => m.Type.GetSanitizedName()))})";

            return new BindingException($"Cannot join {reference.Metadata.Identity}: {leftTuple} is incompatible with {rightTuple}.");
        }

    }
}
