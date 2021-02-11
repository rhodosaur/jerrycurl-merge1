using Jerrycurl.Data.Metadata;
using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Mvc.Projections;
using Jerrycurl.Relations;
using System;
using System.Linq.Expressions;

namespace Jerrycurl.Mvc.Sql
{
    internal static class ProjectionHelper
    {
        public static ITableMetadata GetTableMetadata(IProjection projection) => GetTableMetadata(projection.Metadata);
        public static ITableMetadata GetTableMetadata(IProjectionAttribute attribute) => GetTableMetadata(attribute.Metadata);
        private static ITableMetadata GetTableMetadata(IProjectionMetadata metadata)
            => metadata.Table ?? metadata.Item?.Table ?? throw ProjectionException.TableNotFound(metadata);
        public static ITableMetadata GetColumnMetadata(IProjectionAttribute attribute)
            => attribute.Metadata.Column ?? attribute.Metadata.Item?.Column ?? throw ProjectionException.ColumnNotFound(attribute.Metadata);

        public static IProjectionMetadata GetMetadataFromRelativeLambda(IProjection projection, LambdaExpression expression)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            string name = projection.Metadata.Identity.Notation.Lambda(expression);
            string fullName = projection.Metadata.Identity.Notation.Combine(projection.Metadata.Identity.Name, name);

            return projection.Metadata.Identity.Schema.Require<IProjectionMetadata>(fullName);
        }

        public static IField GetFieldValue(IProjectionAttribute attribute)
        {
            if (attribute.Field == null)
                throw ProjectionException.ValueNotFound(attribute);

            return attribute.Field();
        }
    }
}
