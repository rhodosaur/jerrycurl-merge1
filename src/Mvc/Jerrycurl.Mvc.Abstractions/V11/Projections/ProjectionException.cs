using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Reflection;
using System;
using System.Runtime.Serialization;

namespace Jerrycurl.Mvc.V11.Projections
{
    [Serializable]
    public class ProjectionException2 : Exception
    {
        public ProjectionException2()
        {

        }

        public ProjectionException2(string message)
            : base(message)
        {

        }

        public ProjectionException2(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        protected ProjectionException2(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        #region " Exception helpers "

        public static ProjectionException2 FromAttribute(Type schemaType, string attributeName, string message = null, Exception innerException = null)
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

            return new ProjectionException2(message, innerException);
        }

        public static ProjectionException2 FromProjection(IProjection2 projection, string message = null, Exception innerException = null)
            => FromAttribute(projection.Header.Source.Metadata?.Identity.Schema.Model.Type, projection.Header.Source.Metadata?.Identity.Name, message, innerException);

        public static ProjectionException2 FromProjection(IProjectionAttribute2 attribute, string message = null, Exception innerException = null) => FromAttribute(attribute.Metadata?.Identity.Schema.Model.Type, attribute.Metadata?.Identity.Name, message, innerException);
        public static ProjectionException2 FromAttribute(string attributeName, string message = null, Exception innerException = null) => FromAttribute(null, attributeName, message, innerException);

        public static ProjectionException2 ArgumentNull(string argumentName, IProjection2 projection = null) => FromProjection(projection, innerException: new ArgumentNullException(argumentName));
        public static ProjectionException2 ArgumentNull(string argumentName, IProjectionAttribute2 attribute) => FromProjection(attribute, innerException: new ArgumentNullException(argumentName));

        public static ProjectionException2 ValueNotFound(IProjection2 projection) => FromProjection(projection, "Value not found.");
        public static ProjectionException2 ValueNotFound(IProjectionAttribute2 attribute) => FromProjection(attribute, "Value not found.");

        internal static ProjectionException2 TableNotFound(IProjectionMetadata metadata)
            => new ProjectionException2($"No table information found for {metadata.Identity}.");

        internal static ProjectionException2 ColumnNotFound(IProjectionMetadata metadata)
            => new ProjectionException2($"No column information found for {metadata.Identity}.");

        internal static ProjectionException2 PropertyNotFound(IProjectionMetadata metadata)
            => new ProjectionException2($"No property information found for {metadata.Identity}.");

        #endregion
    }
}
