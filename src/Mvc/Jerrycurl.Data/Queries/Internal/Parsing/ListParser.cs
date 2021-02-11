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
using Jerrycurl.Data.Queries.Internal.IO.Targets;
using System.IO;

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

            this.AddWriters(result, nodeTree);
            this.AddAggregates(result, nodeTree);

            return result;
        }

        private void AddAggregates(ListResult result, NodeTree nodeTree)
        {
            IEnumerable<Node> aggregateNodes = nodeTree.Nodes.Where(this.IsAggregateNode);

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

            foreach (Node node in nodeTree.Items.Where(n => n.Metadata.Relation.Depth == 1))
            {
                int bufferIndex = this.Buffer.GetListIndex(node.Identity);

                AggregateWriter writer = new AggregateWriter(node)
                {
                    Attribute = new AggregateAttribute(node.Metadata.Parent.Identity.Name, aggregateIndex: null, listIndex: bufferIndex),
                };

                result.Aggregates.Add(writer);
            }
        }

        private bool IsAggregateNode(Node node)
        {
            if (this.QueryType != QueryType.Aggregate)
                return false;
            else if (node.Metadata.Relation.Depth == 0)
                return true;
            else if (node.Metadata.Relation.Depth == 1 && node.Metadata.MemberOf.Parent.HasFlag(BindingMetadataFlags.Model))
                return true;

            return false;
        }

        private void AddWriters(ListResult result, NodeTree nodeTree)
        {
            foreach (Node node in nodeTree.Items.Where(n => !this.IsAggregateNode(n)))
            {
                TargetWriter writer = new TargetWriter()
                {
                    Source = this.CreateReader(result, node),
                };

                this.AddPrimaryKey(writer);
                this.AddChildKey(result, writer);

                result.Writers.Add(writer);
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

        private void AddPrimaryKey(TargetWriter writer)
        {
            if (writer.Source is NewReader newReader)
            {
                writer.PrimaryKey = newReader.PrimaryKey;
                newReader.PrimaryKey = null;
            }
        }

        private int GetListIndex(BaseReader source, KeyReader joinKey)
        {
            if (source.Metadata.HasFlag(BindingMetadataFlags.Model))
                return this.Buffer.GetResultIndex();
            else if (source.Metadata.HasFlag(BindingMetadataFlags.Item) && source.Metadata.Parent.HasFlag(BindingMetadataFlags.Model))
                return this.Buffer.GetResultIndex();
            else if (joinKey == null)
                return this.Buffer.GetListIndex(source.Metadata.Identity);
            else
                return this.Buffer.GetParentIndex(joinKey.Reference);
        }

        private ParameterExpression GetListVariable(BaseReader source, KeyReader joinKey)
        {
            if (joinKey != null)
            {
                Type dictType = typeof(Dictionary<,>).MakeGenericType(joinKey.Variable.Type, typeof(ElasticArray));

                return Expression.Variable(dictType);
            }
            else if (source.Metadata.HasFlag(BindingMetadataFlags.Item))
                return Expression.Variable(source.Metadata.Parent.Composition.Construct.Type);

            return null;
        }

        private ListTarget GetListTarget(ListResult result, BaseReader source, KeyReader joinKey)
        {
            int bufferIndex = this.GetListIndex(source, joinKey);

            ListTarget target = result.Targets.FirstOrDefault(t => t.Index == bufferIndex);

            if (target != null)
                return target;

            target = new ListTarget()
            {
                Index = bufferIndex,
                Variable = this.GetListVariable(source, joinKey),
            };

            if (joinKey == null && source.Metadata.HasFlag(BindingMetadataFlags.Item))
            {
                target.NewList = target.NewTarget = source.Metadata.Parent.Composition.Construct;
                target.AddMethod = source.Metadata.Parent.Composition.Add;
            }
            else if (joinKey == null && source.Metadata.HasFlag(BindingMetadataFlags.List))
            {
                target.NewList = source.Metadata.Composition.Construct;
                target.AddMethod = source.Metadata.Composition.Add;
            }

            if (joinKey != null)
                target.NewTarget = Expression.New(target.Variable.Type);

            if (target.NewTarget != null)
                result.Targets.Add(target);

            return target;
        }

        private JoinTarget GetJoinTarget(ListResult result, BaseReader source, KeyReader joinKey)
        {
            if (joinKey == null)
                return null;

            ListTarget list = this.GetListTarget(result, source, joinKey);
            JoinTarget target = new JoinTarget()
            {
                Key = joinKey,
                Buffer = Expression.Variable(typeof(ElasticArray)),
                Index = this.Buffer.GetChildIndex(joinKey.Reference),
                List = list,
            };

            if (source.Metadata.HasFlag(BindingMetadataFlags.Item))
            {
                target.NewList = source.Metadata.Parent.Composition.Construct;
                target.AddMethod = source.Metadata.Parent.Composition.Add;
            }
            else if (source.Metadata.HasFlag(BindingMetadataFlags.List))
            {
                target.NewList = source.Metadata.Composition.Construct;
                target.AddMethod = source.Metadata.Composition.Add;
            }

            return target;
        }

        private void AddParentKeys(ListResult result, NewReader reader)
        {
            foreach (IReference reference in this.GetParentReferences(reader.Metadata))
            {
                KeyReader joinKey = this.FindParentKey(reader, reference);

                if (joinKey != null)
                {
                    this.InitializeKey(joinKey);

                    JoinReader join = new JoinReader(reference);

                    join.Target = this.GetJoinTarget(result, join, joinKey);

                    reader.Joins.Add(join.Target);
                    reader.Properties.Add(join);
                }
            }
        }
        private void AddChildKey(ListResult result, TargetWriter writer)
        {
            IList<IReference> references = this.GetChildReferences(writer.Source.Metadata).ToList();
            KeyReader childKey = references.Select(r => this.FindChildKey(writer.Source, r)).NotNull().FirstOrDefault();

            if (childKey != null)
                this.InitializeKey(childKey, throwOnInvalid: true);

            if (childKey == null && this.RequiresReference(writer))
                throw new InvalidOperationException();

            writer.List = this.GetListTarget(result, writer.Source, childKey);
            writer.Join = this.GetJoinTarget(result, writer.Source, childKey);
        }

        private bool RequiresReference(TargetWriter writer)
        {
            return false;
        }

        private void InitializeKey(KeyReader key, bool throwOnInvalid = false)
        {
            if (key.Reference != null)
            {
                IList<Type> keyTypes = this.GetReferenceKeyTypes(key.Reference, throwOnInvalid).ToList();

                foreach (var (value, keyType) in key.Values.Zip(keyTypes))
                    value.KeyType = keyType;

                key.Variable = Expression.Variable(CompositeKey.Create(keyTypes));

                if (key.Reference.HasFlag(ReferenceFlags.Self))
                    key.Reference = this.GetRecursiveReference(key.Reference);
            }

            foreach (DataReader value in key.Values)
            {
                value.IsDbNull ??= Expression.Variable(typeof(bool), "kv_isnull");
                value.Variable ??= Expression.Variable(value.KeyType, "kv");

                if (key.Reference.HasFlag(ReferenceFlags.Child))
                    value.CanBeDbNull = false;
            }

        }

        private IEnumerable<Type> GetReferenceKeyTypes(IReference reference, bool throwOnInvalid = false)
        {
            IReferenceKey parentKey = reference.FindParentKey();
            IReferenceKey childKey = reference.FindChildKey();

            foreach (var (childValue, parentValue) in childKey.Properties.Zip(parentKey.Properties))
            {
                Type childType = this.GetKeyType(childValue.Type);
                Type parentType = this.GetKeyType(parentValue.Type);

                if (throwOnInvalid && childType != parentType)
                    throw BindingException.IncompatibleReference(reference);

                yield return parentType;
            }
        }

        private Type GetKeyType(Type type) => (Nullable.GetUnderlyingType(type) ?? type);

        private IReference GetRecursiveReference(IReference reference)
        {
            return reference; // somehow locate the other reference through reference.Find(Parent).References.HasFlag(Child).Other
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
    }
}
