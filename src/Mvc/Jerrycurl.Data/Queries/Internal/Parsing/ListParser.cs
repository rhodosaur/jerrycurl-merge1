using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.IO;
using Jerrycurl.Data.Queries.Internal.Caching;
using Jerrycurl.Data.Queries.Internal.Extensions;
using Jerrycurl.Relations.Metadata;
using Jerrycurl.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using Jerrycurl.Data.Queries.Internal.IO.Readers;
using Jerrycurl.Data.Queries.Internal.IO.Writers;

namespace Jerrycurl.Data.Queries.Internal.Parsing
{
    internal class ListParser : BaseParser
    {
        public BufferCache Buffer { get; }
        public QueryType QueryType { get; }

        public ListParser(BufferCache cache, QueryType queryType)
            : base(cache?.Schema)
        {            
            this.Buffer = cache ?? throw new ArgumentException(nameof(cache));
            this.QueryType = queryType;
        }

        public ListResult Parse(IEnumerable<ColumnAttribute> header)
        {
            NodeTree nodeTree = NodeParser.Parse(this.Schema, header);
            ListResult result = new ListResult(this.Schema, this.QueryType);

            this.CreateJoins(result, nodeTree);
            this.CreateAggregates(result, nodeTree);
            this.CreateLists(result);

            return result;
        }

        private void CreateAggregates(ListResult result, NodeTree nodeTree)
        {
            if (this.QueryType != QueryType.Aggregate)
                return;

            IEnumerable<Node> aggregateNodes = nodeTree.Nodes.Where(IsAggregateNode);

            foreach (Node node in aggregateNodes.Where(n => n.Data != null))
            {
                int bufferIndex = this.Buffer.GetAggregateIndex(node.Identity);

                AggregateWriter writer = new AggregateWriter(node)
                {
                    Attribute = new AggregateAttribute(node.Identity.Name, aggregateIndex: bufferIndex, listIndex: null),
                    Value = this.CreateDataReader(result, node)
                };

                result.Aggregates.Add(writer);
            }

            static bool IsAggregateNode(Node node)
            {
                if (node.Metadata.Relation.Depth == 0)
                    return true;
                else if (node.Metadata.Relation.Depth == 1 && node.Metadata.MemberOf.Parent.HasFlag(BindingMetadataFlags.Model))
                    return true;

                return false;
            }
        }

        private void CreateJoins(ListResult result, NodeTree nodeTree)
        {
            foreach (Node node in nodeTree.Items)
            {
                JoinWriter writer = new JoinWriter(node)
                {
                    Value = this.CreateReader(result, node),
                    Depth = node.Metadata.Relation.Depth,
                };

                this.AddPrimaryKey(writer);
                this.AddChildKey(writer);

                result.Joins.Add(writer);
            }

            result.Joins = result.Joins.OrderByDescending(w => w.Depth).ThenByDescending(GetNameDepth).ToList();

            int GetNameDepth(JoinWriter writer)
                => result.Schema.Notation.Depth(writer.Metadata.Identity.Name);
        }

        private void CreateLists(ListResult result)
        {
            foreach (JoinWriter writer in result.Joins)
            {
                
            }
        }

        private void AddPrimaryKey(JoinWriter writer)
        {
            if (writer.Value is NewReader newValue)
            {
                writer.PrimaryKey = newValue.PrimaryKey;
                newValue.PrimaryKey = null;
            }
        }

        private void InitializeKeyVariables(KeyReader reader)
        {
            int index = 0;

            foreach (DataReader value in reader.Values)
            {
                value.IsDbNull ??= Expression.Variable(typeof(bool), $"key_{index++}_isnull");
                value.Variable ??= Expression.Variable(value.KeyType, $"key_{index}");
            }
        }

        private void AddParentKeys(ListResult result, NewReader reader)
        {
            IEnumerable<IReference> references = this.GetParentReferences(reader.Metadata);
            IEnumerable<KeyReader> joinKeys = references.Select(r => this.FindParentKey(reader, r));

            foreach (KeyReader joinKey in joinKeys.NotNull().DistinctBy(k => k.Reference.Other.Metadata.Identity))
            {
                if (joinKey.Reference.HasFlag(ReferenceFlags.Self))
                    joinKey.Reference = this.GetRecursiveReference(reader.Metadata);

                JoinReader join = new JoinReader()
                {
                    JoinIndex = this.Buffer.GetChildIndex(joinKey.Reference),
                    JoinKey = joinKey,
                    Metadata = joinKey.Reference.Other.Metadata.Identity.Require<IBindingMetadata>(),
                };

                if (joinKey.Reference.List != null)
                {
                    IBindingMetadata metadata = joinKey.Reference.List.Identity.Require<IBindingMetadata>();

                    join.Metadata = metadata;
                    join.List = new NewReader(metadata);
                }

                reader.JoinKeys.Add(joinKey);
                reader.Properties.Add(join);

                this.InitializeKeyVariables(joinKey);
            }
        }

        private void AddChildKey(JoinWriter writer)
        {
            IEnumerable<IReference> references = this.GetChildReferences(writer.Metadata);

            if (writer.Value is NewReader value)
            {
                IEnumerable<KeyReader> joinKeys = references.Select(r => this.FindChildKey(value, r));

                KeyReader joinKey = joinKeys.NotNull().FirstOrDefault();

                if (joinKey != null)
                {
                    this.InitializeKeyVariables(joinKey);

                    writer.JoinKey = joinKey;
                    writer.BufferIndex = this.Buffer.GetChildIndex(joinKey.Reference);

                    joinKey.Array ??= Expression.Variable(typeof(ElasticArray), $"array_{writer.BufferIndex}");
                }
            }

            if (writer.JoinKey == null && references.Any())
            {
                IReference invalidRef = references.First();

                throw BindingException.NoValidReference(invalidRef.Metadata.Identity, invalidRef.Other.Metadata.Identity);
            }
        }

        private IReference GetRecursiveReference(IBindingMetadata metadata)
            => this.GetChildReferences(metadata).FirstOrDefault().Other;

        private IEnumerable<IReference> GetParentReferences(IBindingMetadata metadata)
            => this.GetValidReferences(metadata).Where(r => r.HasFlag(ReferenceFlags.Parent));

        private IEnumerable<IReference> GetChildReferences(IBindingMetadata metadata)
            => this.GetValidReferences(metadata).Where(r => r.HasFlag(ReferenceFlags.Child) && !r.HasFlag(ReferenceFlags.Self));

        private IEnumerable<IReference> GetValidReferences(IBindingMetadata metadata)
        {
            IReferenceMetadata referenceMetadata = metadata.Identity.Lookup<IReferenceMetadata>();

            if (referenceMetadata != null)
                return referenceMetadata.References.Where(IsValid).OrderBy(GetPriority);

            return Array.Empty<IReference>();

            bool IsValid(IReference reference)
            {
                if (!reference.HasFlag(ReferenceFlags.Many) && !reference.Other.HasFlag(ReferenceFlags.Many))
                    return false;

                IReference parentRef = reference.Find(ReferenceFlags.Parent);
                IReference childRef = reference.Find(ReferenceFlags.Child);

                if (this.QueryType == QueryType.Aggregate)
                {
                    if (parentRef.Metadata.Relation.HasFlag(RelationMetadataFlags.Model))
                        return false;
                    else if (parentRef.Metadata.Relation.Parent.HasFlag(RelationMetadataFlags.Model))
                        return false;
                }

                return true;
            }

            static int GetPriority(IReference reference)
            {
                if (reference.HasFlag(ReferenceFlags.One | ReferenceFlags.Primary))
                    return 0;
                else if (reference.HasFlag(ReferenceFlags.One | ReferenceFlags.Candidate))
                    return 1;
                else if (reference.HasFlag(ReferenceFlags.Many | ReferenceFlags.Foreign))
                    return 2;
                else
                    return 3;
            }
        }

        private Type GetDictionaryType(KeyReader key)
            => typeof(Dictionary<,>).MakeGenericType(key.CompositeType, typeof(ElasticArray));
    }
}
