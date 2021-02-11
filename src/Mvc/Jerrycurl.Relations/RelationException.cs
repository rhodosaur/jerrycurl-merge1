using Jerrycurl.Relations.Internal.Queues;
using Jerrycurl.Relations.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Jerrycurl.Relations
{
    [Serializable]
    public class RelationException : Exception
    {
        public RelationException()
        {

        }

        public RelationException(string message)
            : base(message)
        {

        }

        public RelationException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        protected RelationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        #region " Exception helpers "

        public static RelationException From(RelationHeader header, string message = null, Exception innerException = null)
        {
            string attributeList = string.Join(", ", header.Attributes.Select(a => a.Metadata.Identity));
            string fullMessage = $"Error in relation {header.Schema}({attributeList}).";

            if (message != null || innerException != null)
                fullMessage += $" {message ?? innerException.Message}";

            return new RelationException(fullMessage, innerException);
        }

        internal static RelationException InvalidDataReaderHeader(RelationDataReader dataReader)
        {
            string dataHeader = string.Join(", ", dataReader.Header.Select(a => $"\"{a}\""));

            return From(dataReader.InnerReader.Relation.Header, $"Degree does not match IDataReader({dataHeader}).");
        }

        internal static RelationException HeaderCannotBeEmpty(RelationDataReader dataReader, int emptyIndex)
        {
            return From(dataReader.InnerReader.Relation.Header, $"Attribute name at index {emptyIndex} cannot be empty.");
        }

        internal static RelationException HeaderCannotHaveDupes(RelationDataReader dataReader, int dupeIndex)
        {
            return From(dataReader.InnerReader.Relation.Header, $"Attribute \"{dataReader.Header[dupeIndex]}\" at index {dupeIndex} is already specified.");
        }

        internal static RelationException CannotForwardQueue(IRelation relation2, IRelationQueue queue, Exception innerException)
            => From(relation2.Header, $"Cannot move cursor for '{queue.Metadata.Identity}'.", innerException);

        internal static RelationException Unreachable(MetadataIdentity source, RelationHeader header, IEnumerable<IRelationMetadata> attributes)
        {
            string attributeNames = string.Join(", ", attributes.Select(a => a.Identity));

            return From(header, $"Following attributes are unreachable from {source}: {attributeNames}");
        }
            
        #endregion
    }
}
