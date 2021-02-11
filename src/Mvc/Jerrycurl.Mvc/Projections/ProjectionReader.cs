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

            for (int i = 0; i < data.Degree; i += 3)
                yield return new ProjectionData(data[i], data[i + 1], data[i + 2]);
        }

        private RelationReader CreateReader()
        {
            List<IRelationMetadata> header = new List<IRelationMetadata>();

            foreach (IProjectionMetadata attribute in this.Header)
            {
                header.Add(attribute.Relation);
                header.Add(attribute.Input.Relation);
                header.Add(attribute.Output.Relation);
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
