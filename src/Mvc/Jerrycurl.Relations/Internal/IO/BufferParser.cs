using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Linq;
using Jerrycurl.Relations.Internal.Queues;
using Jerrycurl.Relations.Internal.Parsing;
using Jerrycurl.Relations.Metadata;
using System.Xml.XPath;

namespace Jerrycurl.Relations.Internal.IO
{
    internal class BufferParser
    {
        public BufferTree Parse(MetadataIdentity source, RelationHeader header)
        {
            NodeTree nodeTree = NodeParser.Parse(source, header);
            BufferTree tree = new BufferTree()
            {
                Notation = new DotNotation(),
            };

            this.CreateSource(nodeTree.Source, tree);

            return tree;
        }

        private void CreateSource(Node node, BufferTree tree)
        {
            SourceReader reader = new SourceReader(node);

            tree.Source = reader;

            this.AddWritersAndProperties(node, reader, tree, null);
        }

        private PropertyReader CreateProperty(Node node, BufferTree tree, QueueIndex queue)
        {
            PropertyReader reader = new PropertyReader(node);

            this.AddWritersAndProperties(node, reader, tree, queue);

            return reader;
        }

        private QueueReader CreateQueue(Node node, BufferTree tree, QueueIndex queue)
        {
            QueueReader reader = new QueueReader(node)
            {
                Index = queue,
            };

            tree.Queues.Add(reader);

            this.AddWritersAndProperties(node, reader, tree, queue);

            return reader;
        }

        private QueueIndex CreateIndex(Node node, BufferTree tree)
        {
            if (node.Item == null && !node.Metadata.HasFlag(RelationMetadataFlags.Recursive))
                return null;

            if (node.Metadata.HasFlag(RelationMetadataFlags.Recursive))
            {
                QueueReader anchorReader = tree.Queues.FirstOrDefault(qr => qr.Index.Item.Equals(node.Metadata.Recursor));

                return anchorReader.Index;
            }

            Type queueType = typeof(RelationQueue<,>).MakeGenericType(node.Metadata.Type, node.Item.Metadata.Type);

            return new QueueIndex()
            {
                Buffer = tree.Queues.Count,
                Variable = Expression.Parameter(queueType),
                List = node.Metadata,
                Item = node.Item.Metadata,
            };
        }

        private void AddWritersAndProperties(Node node, NodeReader reader, BufferTree tree, QueueIndex queue)
        {
            this.AddWriters(node, reader, tree, queue);
            this.AddProperties(node, reader, tree, queue);
        }

        private void AddProperties(Node node, NodeReader reader, BufferTree tree, QueueIndex queue)
        {
            reader.Properties = node.Properties.Select(n => this.CreateProperty(n, tree, queue)).ToList();
        }

        private void AddWriters(Node node, NodeReader reader, BufferTree tree, QueueIndex queue)
        {
            foreach (int index in node.Index)
            {
                FieldWriter fieldWriter = new FieldWriter(node)
                {
                    BufferIndex = index,
                    NamePart = this.GetNamePart(node, queue, tree),
                    Queue = queue,
                };

                reader.Writers.Add(fieldWriter);
                tree.Fields.Add(fieldWriter);
            }

            if (node.Item != null || node.Metadata.HasFlag(RelationMetadataFlags.List | RelationMetadataFlags.Recursive))
            {
                QueueIndex nextQueue = this.CreateIndex(node, tree);
                QueueIndex prevQueue = tree.Queues.LastOrDefault()?.Index;

                QueueWriter writer = new QueueWriter(node)
                {
                    NamePart = this.GetNamePart(node.Item ?? node, queue, tree),
                    Queue = queue,
                    Next = nextQueue,
                };

                if ((node.Item ?? node).Metadata.HasFlag(RelationMetadataFlags.Recursive))
                    nextQueue.Type = RelationQueueType.Recursive;
                else if (prevQueue != null && !prevQueue.List.Identity.Equals(nextQueue.List.MemberOf.Parent?.Identity))
                    nextQueue.Type = RelationQueueType.Cartesian;

                reader.Writers.Add(writer);

                if (node.Item != null)
                    this.CreateQueue(node.Item, tree, nextQueue);
            }
        }

        private string GetNamePart(Node node, QueueIndex queue, BufferTree tree)
        {
            if (queue != null)
                return tree.Notation.Path(queue.Item.Identity.Name, node.Metadata.Identity.Name);
            else
                return tree.Notation.Path(tree.Source.Metadata.Identity.Name, node.Metadata.Identity.Name);
        }
    }
}
