using Jerrycurl.Relations.Metadata;
using Jerrycurl.Relations.Test.Models;
using Jerrycurl.Relations.V11;
using Jerrycurl.Relations.V11.Language;
using Jerrycurl.Test;
using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace Jerrycurl.Relations.Test
{
    public class LanguageTests
    {
        public static void Test_Select_NonDimensional()
        {
            ISchemaStore store = DatabaseHelper.Default.Schemas;

            RelationHeader actual = store.For<RootModel>()
                                         .Select(m => m.Object)
                                         .Select(m => m.ReadOnly);

            RelationHeader expected = store.GetSchema<RootModel>()
                                           .Select("Object", "ReadOnly");

            actual.ShouldBe(expected);
        }

        public static void Test_Select_OneDimensional()
        {
            ISchemaStore store = DatabaseHelper.Default.Schemas;

            RelationHeader actual = store.For<List<RootModel>>()
                                         .Join(m => m)
                                         .Select(m => m.Object)
                                         .Select(m => m.ReadOnly)
                                         .Select(m => m.Complex.Value);

            RelationHeader expected = store.GetSchema<List<RootModel>>()
                                           .Select("Item.Object", "Item.ReadOnly", "Item.Complex.Value");

            actual.ShouldBe(expected);
        }

        public static void Test_Select_TwoDimensional()
        {
            ISchemaStore store = DatabaseHelper.Default.Schemas;

            RelationHeader actual = store.For<List<RootModel>>()
                                         .Join(m => m)
                                         .Select()
                                         .Select(m => m.IntValue)
                                         .Select(m => m.Complex)
                                         .Join(m => m.IntList)
                                         .Select();

            RelationHeader expected = store.GetSchema<List<RootModel>>()
                                           .Select("Item", "Item.IntValue", "Item.Complex", "Item.IntList.Item");

            actual.ShouldBe(expected);
        }

        public static void Test_Select_All()
        {
            ISchemaStore store = DatabaseHelper.Default.Schemas;

            RelationHeader actual = store.For<RootModel>()
                                         .SelectAll(m => m.Complex);

            RelationHeader expected = store.GetSchema<RootModel>()
                                           .Select("Complex.Value", "Complex.Complex");

            actual.ShouldBe(expected);
        }
    }
}
