using Jerrycurl.Reflection;
using Jerrycurl.Relations.Internal.Queues;
using Jerrycurl.Relations.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Jerrycurl.Relations
{
    [Serializable]
    public class RelationException2 : Exception
    {
        public RelationException2()
        {

        }

        public RelationException2(string message)
            : base(message)
        {

        }

        public RelationException2(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        protected RelationException2(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        #region " Exception helpers "

        public static RelationException2 From(RelationHeader header, string message = null, Exception innerException = null)
        {
            string attributeList = string.Join(", ", header.Attributes.Select(a => a.Metadata.Identity));
            string fullMessage = $"Error in relation {header.Schema}({attributeList}).";

            if (message != null || innerException != null)
                fullMessage += $" {message ?? innerException.Message}";

            return new RelationException2(fullMessage, innerException);
        }

        internal static RelationException2 InvalidDataReaderHeader(RelationDataReader dataReader)
        {
            string dataHeader = string.Join(", ", dataReader.Header.Select(a => $"\"{a}\""));

            return From(dataReader.InnerReader.Relation.Header, $"Degree does not match IDataReader({dataHeader}).");
        }

        internal static RelationException2 HeaderCannotBeEmpty(RelationDataReader dataReader, int emptyIndex)
        {
            return From(dataReader.InnerReader.Relation.Header, $"Name at index {emptyIndex} cannot be empty.");
        }

        internal static RelationException2 HeaderCannotHaveDupes(RelationDataReader dataReader, int dupeIndex)
        {
            return From(dataReader.InnerReader.Relation.Header, $"Name at index {dupeIndex} is already specified.");
        }

        internal static RelationException2 CannotForwardQueue(IRelation2 relation2, IRelationQueue queue, Exception innerException)
            => From(relation2.Header, $"Cannot move cursor for '{queue.Metadata.Identity}'.", innerException);

        internal static RelationException2 Unreachable(MetadataIdentity source, RelationHeader header, IEnumerable<IRelationMetadata> attributes)
        {
            string attributeNames = string.Join(", ", attributes.Select(a => a.Identity));

            return From(header, $"Following attributes are unreachable from {source}: {attributeNames}");
        }
            
        #endregion
    }
}
