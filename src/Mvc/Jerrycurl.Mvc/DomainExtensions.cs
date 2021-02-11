using Jerrycurl.Collections;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Relations.Metadata;
using System;
using System.Linq;

namespace Jerrycurl.Mvc
{
    public static class DomainExtensions
    {
        public static void Apply(this ISchemaStore store, ITableContractResolver resolver)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            TableMetadataBuilder builder = store.FirstOfType<TableMetadataBuilder>();

            if (builder == null)
                throw new InvalidOperationException("No TableMetadataBuilder instance found.");

            builder.Add(resolver);
        }

        public static void Apply(this ISchemaStore store, IBindingContractResolver resolver)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            BindingMetadataBuilder builder = store.FirstOfType<BindingMetadataBuilder>();

            if (builder == null)
                throw new InvalidOperationException("No BindingMetadataBuilder instance found.");

            builder.Add(resolver);
        }

        public static void Apply(this ISchemaStore store, IJsonContractResolver resolver)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            JsonMetadataBuilder builder = store.FirstOfType<JsonMetadataBuilder>();

            if (builder == null)
                throw new InvalidOperationException("No JsonMetadataBuilder instance found.");

            builder.Add(resolver);
        }

        public static void Apply(this ISchemaStore store, IRelationContractResolver resolver)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            RelationMetadataBuilder builder = store.OfType<RelationMetadataBuilder>().FirstOrDefault();

            if (builder == null)
                throw new InvalidOperationException("No RelationMetadataBuilder instance found.");

            builder.Add(resolver);
        }
    }
}
