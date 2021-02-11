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
using System.Runtime.CompilerServices;

namespace Jerrycurl.Data.Queries.Internal.Parsing
{
    internal class ListParser : BaseParser
    {
        public BufferCache2 Buffer { get; }
        public QueryType QueryType { get; }

        public ListParser(BufferCache2 cache, QueryType queryType)
            : base(cache?.Schema)
        {
            this.Buffer = cache ?? throw new ArgumentException(nameof(cache));
            this.QueryType = queryType;
        }

        public ListResult Parse(IEnumerable<ColumnAttribute> header)
        {
            NodeTree nodeTree = NodeParser.Parse(this.Schema, header);
            ListResult result = new ListResult(this.Schema, this.QueryType);

            this.CreateLists(result, nodeTree);
            this.CreateAggregates(result, nodeTree);

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

        private void CreateLists(ListResult result, NodeTree nodeTree)
        {
            foreach (Node node in nodeTree.Items)
            {
                ListWriter writer = new ListWriter(node)
                {
                    Source = this.CreateReader(result, node),
                };

                this.AddPrimaryKey(writer);
                this.AddChildKey(result, writer);

                result.Lists.Add(writer);
            }

            //result.Joins = result.Joins.OrderByDescending(w => w.Depth).ThenByDescending(GetNameDepth).ToList();

            //int GetNameDepth(JoinWriter writer) => result.Schema.Notation.Depth(writer.Metadata.Identity.Name);
        }

        protected override BaseReader CreateReader(BaseResult result, Node node)
        {
            BaseReader reader = base.CreateReader(result, node);

            if (reader is NewReader newReader)
                this.AddParentKeys((ListResult)result, newReader);

            return reader;
        }

        private void AddPrimaryKey(ListWriter writer)
        {
            if (writer.Value is NewReader newValue)
            {
                writer.PrimaryKey = newValue.PrimaryKey;
                newValue.PrimaryKey = null;
            }
        }

        private ListTarget CreateTarget(BaseReader source, KeyReader key, IReference reference)
        {
            if (reference == null)
            {
                ListTarget target = new ListTarget();

                {
                    BufferIndex = this.Buffer.GetListIndex()
                }
            }
            ListTarget index = new ListTarget()
            {
                BufferIndex = this.Buffer.GetParentIndex(reference),
            };

            if (reference.List != null)
            {
                IBindingMetadata listMetadata = reference.List.Identity.Require<IBindingMetadata>();

                index.NewList = new NewReader(listMetadata);
                index.Variable = Expression.Variable(listMetadata.Composition.Construct.Type);
            }

            if (reference != null)
            {
                Type compositeType = CompositeKey.Create(key.Values.Select(v => v.KeyType));

                index.Variable = Expression.Variable(typeof(Dictionary<,>).MakeGenericType(compositeType, typeof(ElasticArray)));
                index.Join = new JoinTarget()
                {
                    Buffer = Expression.Variable(typeof(ElasticArray)),
                    BufferIndex = this.Buffer.GetChildIndex(reference),
                    Key = key,
                    Reference = reference,
                };
            }

            return index;
        }

        private void AddParentKeys(ListResult result, NewReader reader)
        {
            foreach (IReference reference in this.GetParentReferences(reader.Metadata))
            {
                KeyReader key = this.FindParentKey(reader, reference);

                if (key != null)
                {
                    this.InitializeKey(key, reference);

                    ListReader join = new ListReader(reference)
                    {
                        Index = this.CreateIndex(reader, key, reference),
                    };

                    reader.Joins.Add(join.Index);
                    reader.Properties.Add(join);
                    result.Indices.Add(join.Index);
                }
            }
        }

        private IEnumerable<Type> GetReferenceKeyTypes(IReference reference, bool throwOnInvalid = false)
        {
            IReferenceKey parentKey = reference.FindParentKey();
            IReferenceKey childKey = reference.FindChildKey();

            foreach (var (childValue, parentValue) in childKey.Properties.Zip(parentKey.Properties))
            {
                Type childType = Nullable.GetUnderlyingType(childValue.Type) ?? childValue.Type;
                Type parentType = Nullable.GetUnderlyingType(parentValue.Type) ?? parentValue.Type;

                if (throwOnInvalid && childType != parentType)
                    throw BindingException.IncompatibleReference(reference);

                yield return parentType;
            }
        }

        private BaseTarget CreateTarget()

        private void AddChildKey(ListResult result, ListWriter writer)
        {
            IEnumerable<IReference> references = this.GetChildReferences(writer.Metadata);

            if (writer.Source is NewReader reader)
            {
                foreach (IReference reference in references)
                {
                    KeyReader key = this.FindChildKey(reader, reference);

                    if (key != null)
                    {
                        this.InitializeKey(key, reference, throwOnInvalid: true);

                        writer.Index = this.CreateIndex(writer.Value, key, reference);
                        result.Indices.Add(writer.Index);

                        break;
                    }
                }
            }

            //if (writer.JoinKey == null && references.Any())
            //{
            //    IReference reference = references.First();

            //    throw BindingException.NoReferenceFound(reference.Metadata.Identity, reference.Other.Metadata.Identity);
            //}
        }

        private void InitializeKey(KeyReader key, IReference reference, bool throwOnInvalid = false)
        {
            foreach (DataReader value in key.Values)
            {
                value.CanBeDbNull = true;
                value.IsDbNull ??= Expression.Variable(typeof(bool), "kv_isnull");
                value.Variable ??= Expression.Variable(value.Metadata.Type, "kv");
            }

            IList<Type> keyTypes = this.GetReferenceKeyTypes(reference, throwOnInvalid).ToList();

            foreach (var (value, keyType) in key.Values.Zip(keyTypes))
                value.KeyType = keyType;

            key.Variable = Expression.Variable(CompositeKey.Create(keyTypes));
        }

        private IReference GetRecursiveReference(IBindingMetadata metadata)
        {
            return this.GetChildReferences(metadata).FirstOrDefault().Other; // this aint good enough
        }

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

                //IReference parentRef = reference.Find(ReferenceFlags.Parent);
                //IReference childRef = reference.Find(ReferenceFlags.Child);

                //if (this.QueryType == QueryType.Aggregate)
                //{
                //    if (parentRef.Metadata.Relation.HasFlag(RelationMetadataFlags.Model))
                //        return false;
                //    else if (parentRef.Metadata.Relation.Parent.HasFlag(RelationMetadataFlags.Model))
                //        return false;
                //}

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
    }
}
