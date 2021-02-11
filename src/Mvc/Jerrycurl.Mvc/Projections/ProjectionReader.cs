using Jerrycurl.Collections;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Mvc.Projections;
using Jerrycurl.Relations;
using Jerrycurl.Relations.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Jerrycurl.Mvc.Projections
{
    internal class ProjectionReader : IDisposable
    {
        public IField Source { get; }
        public IEnumerable<IProjectionMetadata> Header { get; }

        private RelationReader innerReader;
        private List<int> indexMap;
        private int headerSize;

        public ProjectionReader(IField source, IEnumerable<IProjectionMetadata> header)
        {
            this.Source = source ?? throw new ArgumentNullException(nameof(source));
            this.Header = header ?? throw new ArgumentNullException(nameof(header));
        }

        public static IEnumerable<IProjectionData> Lookup(IField source, IEnumerable<IProjectionMetadata> header)
        {
            using ProjectionReader reader = new ProjectionReader(source, header);

            if (reader.Read())
            {
                foreach (IProjectionData data in reader.GetData())
                    yield return data;
            }
        }

        public IEnumerable<IProjectionData> GetData()
        {
            ITuple data = this.innerReader;

            for (int i = 0; i < this.headerSize; i++)
            {
                int sourceIndex = this.indexMap[i * 3];
                int inputIndex = this.indexMap[i * 3 + 1];
                int outputIndex = this.indexMap[i * 3 + 2];

                yield return new ProjectionData(data[sourceIndex], data[inputIndex], data[outputIndex]);
            }
        }

        private RelationReader CreateReader()
        {
            this.indexMap = new List<int>();
            this.headerSize = 0;

            List<IRelationMetadata> header = new List<IRelationMetadata>();
            int i = 0;

            foreach (IProjectionMetadata attribute in this.Header)
            {
                header.Add(attribute.Relation);
                this.indexMap.Add(i);

                if (attribute.Input == attribute && attribute.Output == attribute)
                {
                    this.indexMap.Add(i);
                    this.indexMap.Add(i++);
                }
                else if (attribute.Input == attribute.Output)
                {
                    header.Add(attribute.Input.Relation);

                    this.indexMap.Add(++i);
                    this.indexMap.Add(i++);
                }
                else
                {
                    if (attribute.Input != attribute)
                    {
                        header.Add(attribute.Input.Relation);
                        this.indexMap.Add(++i);
                    }

                    if (attribute.Output != attribute)
                    {
                        header.Add(attribute.Output.Relation);
                        this.indexMap.Add(++i);
                    }
                }

                this.headerSize++;
            }

            Relation body = new Relation(this.Source, new RelationHeader(this.Source.Identity.Schema, header));

            return body.GetReader();
        }

        public bool Read()
        {
            if (this.innerReader == null)
                this.innerReader = this.CreateReader();

            return this.innerReader.Read();
        }

        public void Dispose()
        {
            this.innerReader?.Dispose();
        }
    }
}
