using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Jerrycurl.Collections;
using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Mvc.Projections;
using Jerrycurl.Relations;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Mvc.Sql
{
    public static class ValueExtensions
    {
        public static IProjectionValues<TModel> Vals<TModel>(this IProjection<TModel> projection, int batchIndex = -1)
        {
            if (projection.Data == null)
                return new ProjectionValues<TModel>(projection.Context, projection.Identity, Array.Empty<IProjection<TModel>>());

            IProjectionMetadata[] header = new[] { projection.Metadata }.Concat(projection.Header.Select(a => a.Metadata)).ToArray();
            IProjectionAttribute[] attributes = header.Skip(1).Select(m => new ProjectionAttribute(projection.Identity, projection.Context, m, data: null)).ToArray();

            return new ProjectionValues<TModel>(projection.Context, projection.Identity, innerReader());

            IEnumerable<IProjection<TModel>> innerReader()
            {
                using ProjectionReader reader = new ProjectionReader(projection.Data.Source, header);

                while (reader.Read())
                {
                    IProjectionData[] dataSet = reader.GetData().ToArray();
                    IEnumerable<IProjectionAttribute> valueHeader = attributes.Zip(dataSet.Skip(1)).Select(t => t.First.With(data: t.Second));

                    yield return projection.With(data: dataSet[0], header: valueHeader);
                }
            }
        }

        public static IProjectionValues<TModel> Desc<TModel>(this IProjectionValues<TModel> projections)
            => new ProjectionValues<TModel>(projections.Context, projections.Identity, projections.Items.Reverse());

        public static IProjectionValues<TModel> Union<TModel>(this IProjectionValues<TModel> projections, Expression<Func<TModel, IEnumerable<TModel>>> expression)
        {
            return new ProjectionValues<TModel>(projections.Context, projections.Identity, InnerUnion());

            IEnumerable<IProjection<TModel>> InnerUnion()
            {
                List<IProjection<TModel>> valueList = new List<IProjection<TModel>>();

                foreach (IProjection<TModel> projection in projections)
                {
                    valueList.Add(projection);

                    yield return projection;
                }

                foreach (IProjection<TModel> projection in valueList.SelectMany(p => p.Vals(expression)))
                    yield return projection;
            }
        }

        public static IProjection Val(this IProjection projection)
        {
            IProjection value = projection.Vals().FirstOrDefault();

            if (value == null)
                throw ProjectionException.ValueNotFound(projection.Metadata);

            return value;
        }

        public static IProjectionAttribute ValList(this IProjection projection, Func<IProjectionAttribute, IProjectionAttribute> itemFactory)
        {
            if (projection.Data == null)
                throw ProjectionException.ValueNotFound(projection.Metadata);

            using ProjectionReader reader = new ProjectionReader(projection.Data.Source, new[] { projection.Metadata });
            IProjectionAttribute attribute = projection.Attr();

            if (reader.Read())
            {
                IProjectionData data = reader.GetData().First();

                attribute = itemFactory(attribute.With(data: data));
            }

            while (reader.Read())
            {
                IProjectionData data = reader.GetData().First();

                attribute = attribute.Append(", ");
                attribute = itemFactory(attribute.With(data: data));
            }

            return attribute;
        }

        public static IEnumerable<IProjection> Vals(this IProjection projection, int batchIndex = -1) => projection.Cast<object>().Vals(batchIndex);
        public static IProjectionValues<TItem> Vals<TModel, TItem>(this IProjection<TModel> projection, Expression<Func<TModel, IEnumerable<TItem>>> expression, int batchIndex = -1) => projection.Open(expression).Vals(batchIndex);

        public static IProjection<TProperty> Val<TModel, TProperty>(this IProjection<TModel> projection, Expression<Func<TModel, TProperty>> expression) => projection.For(expression).Val();
        public static IProjection<TModel> Val<TModel>(this IProjection<TModel> projection) => ((IProjection)projection).Val().Cast<TModel>();
    }
}
