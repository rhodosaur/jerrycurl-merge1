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
            NodeTree nodeTree = NodeParser.Parse(this.Schema, values);
            Node modelNode = nodeTree.Items.FirstOrDefault(n => BindingHelper.IsModelOrModelItem(n.Metadata));

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
                AggregateBinder binder = new AggregateBinder(node)
                {
                    CanBeDbNull = true,
                    UseSlot = value.UseSlot,
                };

                if (value.UseSlot)
                    binder.BufferIndex = this.Buffer.GetListIndex(node.Identity);
                else
                    binder.BufferIndex = this.Buffer.GetAggregateIndex(node.Identity);

                return binder;
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
            IBindingMetadata metadata = this.Schema.Require<IBindingMetadata>();

            return new NewBinder(metadata.Item ?? metadata);
        }
    }
}
