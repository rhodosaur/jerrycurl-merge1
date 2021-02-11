using Jerrycurl.Relations.Metadata;
using Jerrycurl.Relations.Test.Metadata;
using Jerrycurl.Relations.Test.Models;
using Jerrycurl.Test;
using Shouldly;
using System;
using System.Linq;

namespace Jerrycurl.Relations.Test
{
    public class MetadataTests
    {
        public void Test_MetadataBuilder_DisallowsRecursiveCalls()
        {
            SchemaStore store = new SchemaStore(new DotNotation(StringComparer.Ordinal)) { new RecursiveMetadataBuilder() };

            ISchema schema = store.GetSchema(typeof(TupleModel));

            schema.ShouldNotBeNull();

            Should.Throw<MetadataBuilderException>(() => schema.Lookup<CustomMetadata>("Item.Value"));
        }

        public void Test_MetadataNotation_StringComparison()
        {
            SchemaStore sensitive = new SchemaStore(new DotNotation(StringComparer.Ordinal)) { new RelationMetadataBuilder() };
            SchemaStore insensitive = new SchemaStore(new DotNotation()) { new RelationMetadataBuilder() };

            IRelationMetadata sensitive1 = sensitive.GetSchema(typeof(TupleModel)).Lookup<IRelationMetadata>("List.Item.Name");
            IRelationMetadata sensitive2 = sensitive.GetSchema(typeof(TupleModel)).Lookup<IRelationMetadata>("list.item.name");

            IRelationMetadata insensitive1 = insensitive.GetSchema(typeof(TupleModel)).Lookup<IRelationMetadata>("List.Item.Name");
            IRelationMetadata insensitive2 = insensitive.GetSchema(typeof(TupleModel)).Lookup<IRelationMetadata>("list.item.name");

            sensitive1.ShouldNotBeNull();
            sensitive2.ShouldBeNull();

            insensitive1.ShouldNotBeNull();
            insensitive2.ShouldNotBeNull();
        }

        public void Test_Metadata_WithCustomListContract()
        {
            RelationMetadataBuilder builder = new RelationMetadataBuilder() { new CustomContractResolver() };
            SchemaStore customStore = new SchemaStore(new DotNotation()) { builder };

            ISchema schema1 = DatabaseHelper.Default.Schemas.GetSchema(typeof(CustomModel));
            ISchema schema2 = customStore.GetSchema(typeof(CustomModel));

            IRelationMetadata notFound = schema1.Lookup<IRelationMetadata>("Values.Item");
            IRelationMetadata found = schema2.Lookup<IRelationMetadata>("Values.Item");

            notFound.ShouldBeNull();
            found.ShouldNotBeNull();
            found.Type.ShouldBe(typeof(int));
            found.Annotations.OfType<CustomAttribute>().FirstOrDefault().ShouldNotBeNull();
        }

        public void Test_Metadata_WithInvalidListContract_Throws()
        {
            RelationMetadataBuilder builder = new RelationMetadataBuilder() { new InvalidContractResolver() };
            SchemaStore customStore = new SchemaStore(new DotNotation()) { builder };

            ISchema schema = customStore.GetSchema(typeof(CustomModel));

            Should.Throw<MetadataBuilderException>(() => schema.Lookup<IRelationMetadata>("List1"));
        }

        public void Test_OneType_Equality()
        {
            One<int> empty = new One<int>();
            One<int> zero = new One<int>(0);
            One<int> one = new One<int>(1);

            One<int> empty2 = new One<int>();
            One<int> zero2 = new One<int>(0);
            One<int> one2 = new One<int>(1);

            empty.Equals(empty2).ShouldBeTrue();
            empty.Equals(zero).ShouldBeFalse();
            empty.Equals(0).ShouldBeFalse();
            empty.Equals(one).ShouldBeFalse();
            empty.Equals(1).ShouldBeFalse();

            zero.Equals(empty).ShouldBeFalse();
            zero.Equals(zero2).ShouldBeTrue();
            zero.Equals(0).ShouldBeTrue();
            zero.Equals(one).ShouldBeFalse();
            zero.Equals(1).ShouldBeFalse();

            one.Equals(empty).ShouldBeFalse();
            one.Equals(zero).ShouldBeFalse();
            one.Equals(0).ShouldBeFalse();
            one.Equals(one2).ShouldBeTrue();
            one.Equals(1).ShouldBeTrue();
        }
    }
}
