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

            //this.MoveManyToOneNodes(nodeTree);

            this.AddWriters(tree, nodeTree, valueNames);
            this.AddAggregates(tree, nodeTree, valueNames);
            this.PrioritizeWriters(tree);

            return tree;
        }

        //private void MoveManyToOneNodes(NodeTree nodeTree)
        //{
        //    foreach (Node node in nodeTree.Nodes.Where(n => this.HasOneAttribute(n.Metadata)))
        //    {
        //        Node parentNode = nodeTree.FindNode(node.Metadata.Parent);

        //        parentNode.Properties.Remove(node);

        //        nodeTree.Items.Add(node);
        //    }
        //}

        //private bool HasOneAttribute(IBindingMetadata metadata) => metadata.Annotations.OfType<OneAttribute>().Any();
        //private bool HasOneAttribute(IReference reference) => reference.Metadata.Annotations.OfType<OneAttribute>().Any();

        private void PrioritizeWriters(BufferTree tree)
        {
            int priority = 0;

            foreach (ListWriter writer in tree.Lists.Reverse())
                writer.Priority = priority++;
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

                    AggregateName name = new AggregateName(value.Identity.Name, isPrincipal: false);

                    tree.Aggregates.Add(writer);
                    tree.AggregateNames.Add(name);
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
                    Metadata = node.Metadata.Parent,
                    Item = this.CreateBinder(tree, node, valueNames),
                };

                if (writer.Item is NewBinder newBinder)
                {
                    writer.PrimaryKey = newBinder.PrimaryKey;
                    newBinder.PrimaryKey = null;
                }

                if (!this.IsPrincipalSet(node.Metadata))
                {
                    this.AddChildKey(writer);

                    if (writer.JoinKey == null)
                        throw new BindingException($"No valid reference found for {node.Identity}. Please specify matching [Key] and [Ref] annotations to map across one-to-many boundaries.");
                }
                else if (this.IsAggregateSet(writer.Metadata))
                    tree.AggregateNames.Add(new AggregateName(writer.Metadata.Identity.Name, isPrincipal: true));

                writer.Slot = this.AddSlot(tree, writer.Metadata, writer.JoinKey);

                if (writer.JoinKey != null)
                {
                    writer.BufferIndex = this.Buffer.GetChildIndex(writer.JoinKey.Metadata);
                }

                tree.Lists.Add(writer);
            }
        }

        private bool IsPrincipalSet(IBindingMetadata metadata)
        {
            if (this.Type == QueryType.Aggregate)
                return (metadata.Relation.Depth == 2);

            bool isPrincipal = metadata.Relation.Depth == (this.Type == QueryType.Aggregate ? 2 : 1);

            return (isPrincipal); // check this for many-to-ones somehow
        }

        private bool IsAggregateSet(IBindingMetadata metadata) => (this.Type == QueryType.Aggregate && metadata.Relation.Depth == 1);

        private ParameterExpression AddSlot(BufferTree tree, IBindingMetadata metadata, KeyBinder joinKey)
        {
            int bufferIndex;
            Type variableType;

            if (joinKey == null)
            {
                bufferIndex = metadata.Relation.Depth == 0 ? this.Buffer.GetResultIndex() : this.Buffer.GetListIndex(metadata.Identity);
                variableType = metadata.Composition.Construct.Type;
            }
            else
            {
                bufferIndex = this.Buffer.GetParentIndex(joinKey.Metadata);
                variableType = this.GetDictionaryType(joinKey);
            }

            SlotWriter slotWriter = tree.Slots.FirstOrDefault(w => w.BufferIndex == bufferIndex);

            if (slotWriter == null)
            {
                slotWriter = new SlotWriter()
                {
                    BufferIndex = bufferIndex,
                    Variable = BindingHelper.Variable(variableType, metadata.Identity),
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
                    Variable = BindingHelper.Variable(binder.Metadata.Helper.Type, binder),
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
            foreach (ValueBinder value in key.Values)
            {
                value.IsDbNull ??= BindingHelper.Variable(typeof(bool), value);
                value.Variable ??= BindingHelper.Variable(value.KeyType, value);
            }
        }

        private void AddParentKeys(BufferTree tree, NewBinder binder)
        {
            IEnumerable<IReference> references = this.GetValidReferences(binder.Metadata).Where(r => r.HasFlag(ReferenceFlags.Parent));
            IEnumerable<KeyBinder> joinKeys = references.Select(r => BindingHelper.FindParentKey(binder, r));

            foreach (KeyBinder joinKey in joinKeys.NotNull().DistinctBy(k => k.Metadata.Other))
            {
                IBindingMetadata metadata = (joinKey.Metadata.List ?? joinKey.Metadata.Other.Metadata).Identity.Lookup<IBindingMetadata>();

                JoinBinder joinBinder = new JoinBinder(metadata)
                {
                    Array = joinKey.Array ??= BindingHelper.Variable(typeof(ElasticArray), metadata.Identity),
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
            if (writer.Item is NewBinder binder)
            {
                IEnumerable<IReference> references = this.GetValidReferences(writer.Item.Metadata).Where(r => r.HasFlag(ReferenceFlags.Child));
                IEnumerable<KeyBinder> joinKeys = references.Select(r => BindingHelper.FindChildKey(binder, r));

                KeyBinder joinKey = writer.JoinKey = joinKeys.NotNull().FirstOrDefault();

                if (joinKey != null)
                {
                    this.InitializeKeyVariables(joinKey);

                    joinKey.Array ??= BindingHelper.Variable(typeof(ElasticArray), joinKey.Metadata.Metadata.Identity);
                }
            }
        }

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
