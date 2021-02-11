using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Reflection;
using System;
using System.Runtime.Serialization;

namespace Jerrycurl.Mvc.Projections
{
    [Serializable]
    public class ProjectionException : Exception
    {
        public ProjectionException()
        {

        }

        public ProjectionException(string message)
            : base(message)
        {

        }

        public ProjectionException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        protected ProjectionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        #region " Exception helpers "

        public static ProjectionException FromAttribute(Type schemaType, string attributeName, string message = null, Exception innerException = null)
        {
            message = message ?? innerException?.Message;

            if (schemaType == null && attributeName == null && message != null)
                message = $"Unable to create projection. {message}";
            else if (schemaType == null && attributeName == null)
                message = $"Unable to create projection.";
            else if (schemaType != null && message != null)
                message = $"Unable to create projection from attribute '{attributeName}' in schema '{schemaType.GetSanitizedFullName()}'. {message}";
            else if (schemaType != null)
                message = $"Unable to create projection from attribute '{attributeName}' in schema '{schemaType.GetSanitizedFullName()}'.";
            else if (message != null)
                message = $"Unable to create projection from attribute '{attributeName}'. {message}";
            else
                message = $"Unable to create projection from attribute '{attributeName}'.";

            return new ProjectionException(message, innerException);
        }

        public static ProjectionException FromProjection(IProjection projection, string message = null, Exception innerException = null)
            => FromAttribute(projection.Header.Source.Data.Metadata?.Identity.Schema.Model.Type, projection.Header.Source.Data.Metadata?.Identity.Name, message, innerException);

        public static ProjectionException FromProjection(IProjectionAttribute attribute, string message = null, Exception innerException = null) => FromAttribute(attribute.Metadata?.Identity.Schema.Model.Type, attribute.Metadata?.Identity.Name, message, innerException);
        public static ProjectionException FromAttribute(string attributeName, string message = null, Exception innerException = null) => FromAttribute(null, attributeName, message, innerException);

        public static ProjectionException ArgumentNull(string argumentName, IProjection projection = null) => FromProjection(projection, innerException: new ArgumentNullException(argumentName));
        public static ProjectionException ArgumentNull(string argumentName, IProjectionAttribute attribute) => FromProjection(attribute, innerException: new ArgumentNullException(argumentName));


        internal static ProjectionException InvalidProjection(IProjectionMetadata metadata, string message)
            => new ProjectionException($"Cannot create projection from {metadata.Identity}: {message}");

        internal static ProjectionException ValueNotFound(IProjectionMetadata metadata)
            => new ProjectionException($"No value information found for {metadata.Identity}.");

        internal static ProjectionException TableNotFound(IProjectionMetadata metadata)
            => new ProjectionException($"No table information found for {metadata.Identity}.");

        internal static ProjectionException ColumnNotFound(IProjectionMetadata metadata)
            => new ProjectionException($"No column information found for {metadata.Identity}.");

        internal static ProjectionException ParametersNotSupported(IProjectionAttribute attribute)
            => new ProjectionException($"Cannot create parameter for {attribute.Data.Metadata.Identity}: {attribute.Context.Domain.Dialect.GetType().Name} does not support input parameters.");

        internal static ProjectionException PropertyNotFound(IProjectionMetadata metadata)
            => new ProjectionException($"No property information found for {metadata.Identity}.");

        #endregion
    }
}
