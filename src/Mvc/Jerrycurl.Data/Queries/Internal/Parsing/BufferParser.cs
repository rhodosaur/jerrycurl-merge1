using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Metadata.Annotations;
using Jerrycurl.Data.Queries.Internal.IO;
using Jerrycurl.Data.Queries.Internal.Caching;
using Jerrycurl.Data.Queries.Internal.Extensions;
using Jerrycurl.Relations.Metadata;
using Jerrycurl.Collections;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Jerrycurl.Data.Queries.Internal.Parsing
{
    internal class BufferParser
    {
        public ISchema Schema => this.Buffer.Schema;
        public BufferCache Buffer { get; }
        public QueryType Type { get; }

        public BufferParser(QueryType type, BufferCache cache)
        {
            this.Type = type;
            this.Buffer = cache ?? throw new ArgumentException(nameof(cache));
        }

        public BufferTree Parse(IEnumerable<ColumnName> valueNames)
        {
            NodeTree nodeTree = NodeParser.Parse(this.Schema, valueNames);
            BufferTree tree = new BufferTree()
            {
                QueryType = this.Type,
                Schema = this.Schema,
            };

            this.AddWriters(tree, nodeTree, valueNames);
            this.AddAggregates(tree, nodeTree, valueNames);

            return tree;
        }

        private void AddAggregates(BufferTree tree, NodeTree nodeTree, IEnumerable<ColumnName> valueNames)
        {
            IEnumerable<Node> aggregateNodes = nodeTree.Nodes.Where(n => this.IsAggregateSet(n.Metadata));

            foreach (Node node in aggregateNodes)
            {
                ColumnBinder value = BindingHelper.FindValue(node, valueNames);

                if (value != null)
                {
                    AggregateWriter writer = new AggregateWriter()
                    {
                        BufferIndex = this.Buffer.GetAggregateIndex(node.Identity),
                        Data = value,
                    };

                    tree.Aggregates.Add(writer);
                    tree.AggregateNames.Add(new AggregateName(value.Identity.Name, useSlot: false));
                }
            }
        }

        private void AddWriters(BufferTree tree, NodeTree nodeTree, IEnumerable<ColumnName> valueNames)
        {
            IEnumerable<Node> itemNodes = nodeTree.Items.Where(n => !this.IsAggregateSet(n.Metadata));

            foreach (Node node in itemNodes)
            {
                ListWriter writer = new ListWriter()
                {
                    Metadata = node.Metadata.Parent ?? node.Metadata,
                    Item = this.CreateBinder(tree, node, valueNames),
                };

                this.AddPrimaryKey(writer);
                this.AddChildKey(writer);

                if (this.IsAggregateSet(writer.Metadata))
                    tree.AggregateNames.Add(new AggregateName(writer.Metadata.Identity.Name, useSlot: true));

                if (!writer.Metadata.HasFlag(BindingMetadataFlags.Model))
                    writer.Slot = this.AddSlot(tree, writer.Metadata, writer.JoinKey);

                tree.Lists.Add(writer);
            }
        }

        private void AddPrimaryKey(ListWriter writer)
        {
            if (writer.Item is NewBinder newBinder)
            {
                writer.PrimaryKey = newBinder.PrimaryKey;
                newBinder.PrimaryKey = null;
            }
        }

        private bool IsAggregateSet(IBindingMetadata metadata)
        {
            if (this.Type != QueryType.Aggregate)
                return false;

            if (metadata.Relation.Depth == 0)
                return true;
            else if (metadata.Relation.Depth == 1 && metadata.MemberOf.Parent.HasFlag(BindingMetadataFlags.Model))
                return true;

            return false;
        }

        private ParameterExpression AddSlot(BufferTree tree, IBindingMetadata metadata, KeyBinder joinKey)
        {
            int bufferIndex;
            Type variableType;
            string variableName;

            if (joinKey == null)
            {
                bufferIndex = metadata.Relation.Depth == 0 ? this.Buffer.GetResultIndex() : this.Buffer.GetListIndex(metadata.Identity);
                variableType = metadata.Composition.Construct.Type;
                variableName = $"list_{bufferIndex}";
            }
            else
            {
                bufferIndex = this.Buffer.GetParentIndex(joinKey.Metadata);
                variableType = this.GetDictionaryType(joinKey);
                variableName = $"dic_{bufferIndex}";
            }

            SlotWriter slotWriter = tree.Slots.FirstOrDefault(w => w.BufferIndex == bufferIndex);

            if (slotWriter == null)
            {
                slotWriter = new SlotWriter()
                {
                    BufferIndex = bufferIndex,
                    Variable = Expression.Variable(variableType, variableName),
                    Metadata = metadata,
                    KeyType = joinKey?.KeyType,
                };

                tree.Slots.Add(slotWriter);
            }

            if (joinKey != null)
                joinKey.Slot = slotWriter.Variable;

            return slotWriter.Variable;
        }

        private void AddHelper(BufferTree tree, ColumnBinder binder)
        {
            if (binder.Metadata.Helper != null)
            {
                HelperWriter writer = new HelperWriter()
                {
                    Object = binder.Metadata.Helper.Object,
                    BufferIndex = tree.Helpers.Count,
                    Variable = Expression.Variable(binder.Metadata.Helper.Type, $"helper_{tree.Helpers.Count}"),
                };

                binder.Helper = writer.Variable;

                tree.Helpers.Add(writer);
            }
        }

        private NodeBinder CreateBinder(BufferTree tree, Node node, IEnumerable<ColumnName> valueNames)
        {
            ColumnBinder columnBinder = BindingHelper.FindValue(node, valueNames);

            if (columnBinder != null)
            {
                this.AddHelper(tree, columnBinder);

                return columnBinder;
            }

            NewBinder binder = new NewBinder(node)
            {
                Properties = node.Properties.Select(n => this.CreateBinder(tree, n, valueNames)).ToList(),
            };

            BindingHelper.AddPrimaryKey(binder);

            this.AddParentKeys(tree, binder);

            return binder;
        }

        private void InitializeKeyVariables(KeyBinder key)
        {
            int index = 0;

            foreach (ValueBinder value in key.Values)
            {
                value.IsDbNull ??= Expression.Variable(typeof(bool), $"key_{index++}_isnull");
                value.Variable ??= Expression.Variable(value.KeyType, $"key_{index}");
            }
        }

        private void AddParentKeys(BufferTree tree, NewBinder binder)
        {
            IEnumerable<IReference> references = this.GetParentReferences(binder.Metadata);
            IEnumerable<KeyBinder> joinKeys = references.Select(r => BindingHelper.FindParentKey(binder, r));

            foreach (KeyBinder joinKey in joinKeys.NotNull().DistinctBy(k => k.Metadata.Other.Metadata.Identity))
            {
                IReference reference = joinKey.Metadata;

                if (joinKey.Metadata.HasFlag(ReferenceFlags.Self))
                    reference = joinKey.Metadata = this.GetRecursiveReference(binder.Metadata);

                IBindingMetadata metadata = (reference.List ?? reference.Other.Metadata).Identity.Lookup<IBindingMetadata>();

                JoinBinder joinBinder = new JoinBinder(metadata)
                {
                    Array = joinKey.Array ??= Expression.Variable(typeof(ElasticArray), $"array"),
                    ArrayIndex = this.Buffer.GetChildIndex(joinKey.Metadata),
                    Key = joinKey,
                };

                binder.JoinKeys.Add(joinKey);
                binder.Properties.Add(joinBinder);

                this.InitializeKeyVariables(joinKey);
                this.AddSlot(tree, metadata, joinKey);
            }
        }

        private void AddChildKey(ListWriter writer)
        {
            IEnumerable<IReference> references = this.GetChildReferences(writer.Item.Metadata);

            if (writer.Item is NewBinder binder)
            {
                IEnumerable<KeyBinder> joinKeys = references.Select(r => BindingHelper.FindChildKey(binder, r));

                KeyBinder joinKey = joinKeys.NotNull().FirstOrDefault();

                if (joinKey != null)
                {
                    this.InitializeKeyVariables(joinKey);

                    writer.JoinKey = joinKey;
                    writer.BufferIndex = this.Buffer.GetChildIndex(joinKey.Metadata);

                    joinKey.Array ??= Expression.Variable(typeof(ElasticArray), $"array_{writer.BufferIndex}");
                }
            }

            if (this.Type == QueryType.List)
            {
                if (writer.Depth > 1 && writer.JoinKey == null)
                    throw BindingException.NoValidReference(writer.Metadata.Identity);
                else if (writer.Depth == 1 && writer.JoinKey == null && references.Any())
                    throw BindingException.NoValidReference(writer.Metadata.Identity);
                else if (writer.Depth < 1)
                    writer.JoinKey = null;
            }
            else
            {
                if (writer.Depth > 2 && writer.JoinKey == null)
                    throw BindingException.NoValidReference(writer.Metadata.Identity);
                if (writer.Depth == 2 && writer.Metadata.Parent.MemberOf.Parent.HasFlag(BindingMetadataFlags.Model) && writer.JoinKey == null && references.Any())
                    throw BindingException.NoValidReference(writer.Metadata.Identity);
                else if (writer.Depth < 2)
                    writer.JoinKey = null;
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

            static bool IsValid(IReference reference) => (reference.HasFlag(ReferenceFlags.Many) || reference.Other.HasFlag(ReferenceFlags.Many));

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

        private Type GetDictionaryType(KeyBinder key)
            => typeof(Dictionary<,>).MakeGenericType(key.KeyType, typeof(ElasticArray));
    }
}
