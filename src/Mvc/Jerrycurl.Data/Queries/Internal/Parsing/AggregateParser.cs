using System;
using System.Collections.Generic;
using System.Linq;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.IO;
using Jerrycurl.Data.Queries.Internal.Caching;
using Jerrycurl.Relations.Metadata;
using Jerrycurl.Collections;

namespace Jerrycurl.Data.Queries.Internal.Parsing
{
    internal class AggregateParser
    {
        public ISchema Schema => this.Buffer.Schema;
        public BufferCache Buffer { get; }

        public AggregateParser(BufferCache cache)
        {
            this.Buffer = cache ?? throw new ArgumentException(nameof(cache));
        }

        public AggregateTree Parse(IEnumerable<AggregateName> values)
        {
            AggregateName modelValue = new AggregateName(this.Schema.Notation.Model(), useSlot: false);
            NodeTree nodeTree = NodeParser.Parse(this.Schema, values.Concat(new[] { modelValue }));
            Node modelNode = nodeTree.Items.FirstOrDefault(n => n.Metadata.HasFlag(BindingMetadataFlags.Model));

            return new AggregateTree()
            {
                Schema = this.Schema,
                Aggregate = this.CreateBinder(modelNode, values),
            };
        }

        private AggregateBinder FindValue(Node node, IEnumerable<AggregateName> values)
        {
            AggregateName value = values.OrderBy(n => n.UseSlot).FirstOrDefault(n => node.Identity.Equals(n.Name));

            if (value != null)
            {
                return new AggregateBinder(node)
                {
                    CanBeDbNull = true,
                    UseSlot = value.UseSlot,
                    BufferIndex = value.UseSlot ? this.Buffer.GetListIndex(node.Identity) : this.Buffer.GetAggregateIndex(node.Identity),
                };
            }

            return null;
        }

        private NodeBinder CreateBinder(Node node, IEnumerable<AggregateName> valueNames)
        {
            if (node == null)
                return this.CreateEmptyBinder();

            AggregateBinder value = this.FindValue(node, valueNames);

            if (value != null)
                return value;

            NewBinder binder = new NewBinder(node)
            {
                Properties = node.Properties.Select(n => this.CreateBinder(n, valueNames)).ToList(),
            };

            BindingHelper.AddPrimaryKey(binder);

            return binder;
        }

        private NewBinder CreateEmptyBinder()
        {
            IBindingMetadata metadata = this.Schema.Require<IBindingMetadata>().Item;

            return new NewBinder(metadata);
        }
    }
}
