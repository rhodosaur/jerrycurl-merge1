using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Jerrycurl.Data.Queries;
using Jerrycurl.Data.Test.Models;
using Jerrycurl.Test;
using Jerrycurl.Data.Sessions;
using Jerrycurl.Data.Metadata;

namespace Jerrycurl.Data.Test
{
    public class QueryTests
    {
        public async Task Test_Binding_OfEnumerateAsyncWithMultipleSets()
        {
            SqliteTable table1 = new SqliteTable("Item")
            {
                new object[] { 1 },
                new object[] { 2 },
                new object[] { 3 },
            };
            SqliteTable table2 = new SqliteTable("Item")
            {
                new object[] { 4 },
                new object[] { 5 },
                new object[] { 6 },
            };

            DatabaseHelper.Default.Enumerate<int>(table1, table2).ShouldBe(new[] { 1, 2, 3, 4, 5, 6 });
            (await (DatabaseHelper.Default.EnumerateAsync<int>(table1, table2)).ToList()).ShouldBe(new[] { 1, 2, 3, 4, 5, 6 });
        }

        public async Task Test_Binding_OfNestedStructs()
        {
            SqliteTable table = new SqliteTable("Item.Integer", "Item.String", "Item.Sub.Value")
            {
                new object[] { 1, "Jerrycurl", 2 },
            };

            BigStruct result1 = DatabaseHelper.Default.Query<BigStruct>(table).FirstOrDefault();
            BigStruct result2 = (await DatabaseHelper.Default.QueryAsync<BigStruct>(table)).FirstOrDefault();

            result1.Integer.ShouldBe(1);
            result2.Integer.ShouldBe(1);

            result1.String.ShouldBe("Jerrycurl");
            result2.String.ShouldBe("Jerrycurl");

            result1.Sub.Value.ShouldBe(2);
            result2.Sub.Value.ShouldBe(2);
        }

        public async Task Test_Binding_OfValuesFromNullableParameters()
        {
            Query query = new Query()
            {
                QueryText = @"SELECT @P0 AS `Item` UNION
                              SELECT @P1 AS `Item` UNION
                              SELECT @P2 AS `Item`
                              ORDER BY `Item`",
                Parameters = new IParameter[]
                {
                    new Parameter("P0", DatabaseHelper.Default.Model<int?>(0)),
                    new Parameter("P1", DatabaseHelper.Default.Model<int?>()),
                    new Parameter("P2", DatabaseHelper.Default.Model<int?>(1)),
                }
            };

            IList<int?> result1 = DatabaseHelper.Default.Query<int?>(query);
            IList<int?> result2 = await DatabaseHelper.Default.QueryAsync<int?>(query);

            static void verifyResult(IList<int?> result)
            {
                result.ShouldNotBeNull();
                result.ShouldBe(new int?[] { null, 0, 1 });
            }

            verifyResult(result1);
            verifyResult(result2);
        }

        public async Task Test_Binding_OfBigAggregateResult()
        {
            SqliteTable table2 = new SqliteTable("Item.None.BigKey", "Item.None.Value")
            {
                new object[] { null, 1 },
            };
            SqliteTable table1 = new SqliteTable("Item.Scalar")
            {
                new object[] { 2 },
                new object[] { 1 },
            };
            SqliteTable table3 = new SqliteTable("Item.One.BigKey", "Item.One.Value")
            {
                new object[] { 1, 22 },
            };
            SqliteTable table4 = new SqliteTable("Item.Many.Item.BigKey", "Item.Many.Item.Value", "Item.Many.Item.OneToMany.Item.BigKey", "Item.Many.Item.OneToMany.Item.Value")
            {
                new object[] { 1, 33, 3, 22 },
                new object[] { null, 34, null, 23 },
                new object[] { 3, 35, 3, 24 },
            };

            BigAggregate result1 = DatabaseHelper.Default.Aggregate<BigAggregate>(table1, table2, table3, table4);
            BigAggregate result2 = await DatabaseHelper.Default.AggregateAsync<BigAggregate>(table1, table2, table3, table4);

            static void verifyResult(BigAggregate result)
            {
                result.ShouldNotBeNull();

                result.Scalar.ShouldBe(2);
                result.None.ShouldBeNull();
                result.One.ShouldNotBeNull();
                result.One.Value.ShouldBe(22);

                result.Many.ShouldNotBeNull();
                result.Many.Select(m => m.Value).ShouldBe(new[] { 33, 35 });

                result.Many[0].OneToMany.ShouldBeEmpty();
                result.Many[1].OneToMany.ShouldNotBeNull();
                result.Many[1].OneToMany.Select(m => m.Value).ShouldBe(new[] { 22, 24 });
            }

            verifyResult(result1);
            verifyResult(result2);
        }

        public async Task Test_Binding_OfScalarNullIntResult()
        {
            SqliteTable table1 = new SqliteTable("Item")
            {
                new object[] { 1 },
                new object[] { null },
                new object[] { 2 },
            };

            DatabaseHelper.Default.Query<int?>(table1).ShouldBe(new int?[] { 1, null, 2 });
            DatabaseHelper.Default.Enumerate<int?>(table1).ShouldBe(new int?[] { 1, null, 2 });
            (await DatabaseHelper.Default.QueryAsync<int?>(table1)).ShouldBe(new int?[] { 1, null, 2 });
        }

        public async Task Test_Binding_OfOneToOneSelfJoins()
        {
            SqliteTable table1 = new SqliteTable("Item.Parent.Id", "Item.Parent.ParentId")
            {
                new object[] { 4,    null },
                new object[] { 3,    4 },
                new object[] { 2,    3 },
            };
            SqliteTable table2 = new SqliteTable("Item.Id", "Item.ParentId")
            {
                new object[] { 1, 2 },
            };

            IList<BigRecurse.One> result1 = DatabaseHelper.Default.Query<BigRecurse.One>(table1, table2);
            IList<BigRecurse.One> result2 = await DatabaseHelper.Default.QueryAsync<BigRecurse.One>(table1, table2);

            static void verifyResult(IList<BigRecurse.One> result)
            {
                result.ShouldNotBeNull();
                result.Count.ShouldBe(1);

                result[0].Id.ShouldBe(1);
                result[0].Parent.ShouldNotBeNull();

                result[0].Parent.Id.ShouldBe(2);
                result[0].Parent.Parent.ShouldNotBeNull();

                result[0].Parent.Parent.Id.ShouldBe(3);
                result[0].Parent.Parent.Parent.ShouldNotBeNull();

                result[0].Parent.Parent.Parent.Id.ShouldBe(4);
                result[0].Parent.Parent.Parent.Parent.ShouldBeNull();
            }

            verifyResult(result1);
            verifyResult(result2);
        }

        public async Task Test_Binding_OfBigModelWithDifferentJoins()
        {
            SqliteTable table1 = new SqliteTable("Item.OneToManyAsOne.BigKey", "Item.OneToManyAsOne.Value")
            {
                new object[] { 3, 99 },
                new object[] { 2, 999 },
            };
            SqliteTable table2 = new SqliteTable("Item.BigKey", "Item.Value", "Item.OneToOne.SubKey", "Item.OneToOne.Value")
            {
                new object[] { 1, 77, 1, 1 },
                new object[] { 2, 777, null, 2 },
            };
            SqliteTable table3 = new SqliteTable("Item.BigKey", "Item.Value", "Item.OneToOne.Value")
            {
                new object[] { 3, 7777, 3 },
            };
            SqliteTable table4 = new SqliteTable("Item.OneToMany.Item.BigKey", "Item.OneToMany.Item.Value")
            {
                new object[] { 2, 55 },
                new object[] { 2, 555 },
                new object[] { 3, 66 },
            };
            SqliteTable table5 = new SqliteTable("Item.OneToManySelf.Item.BigKey", "Item.OneToManySelf.Item.Id", "Item.OneToManySelf.Item.ParentId")
            {
                new object[] { 1, 1, null },
                new object[] { 1, 2, null },
                new object[] { 1, 3, null },
                new object[] { 2, 4, null },
            };
            SqliteTable table6 = new SqliteTable("Item.OneToManySelf.Item.Children.Item.Id", "Item.OneToManySelf.Item.Children.Item.ParentId")
            {
                new object[] { 5, 2 },
                new object[] { 6, 2 },
                new object[] { 7, 3 },
                new object[] { 8, 3 },
                new object[] { 9, 3 },
                new object[] { 10, 6 },
                new object[] { 11, 6 },
                new object[] { 12, 9 },
            };

            IList<BigModel> result1 = DatabaseHelper.Default.Query<BigModel>(table1, table2, table3, table4, table5, table6);
            IList<BigModel> result2 = await DatabaseHelper.Default.QueryAsync<BigModel>(table1, table2, table3, table4, table5, table6);

            static void verifyResult(IList<BigModel> result)
            {
                result.ShouldNotBeNull();
                result.Select(m => m.Value).ShouldBe(new[] { 77, 777, 7777 });

                result[0].OneToOne.ShouldNotBeNull();
                result[0].OneToOne.Value.ShouldBe(1);
                result[1].OneToOne.ShouldBeNull();
                result[2].OneToOne.ShouldNotBeNull();
                result[2].OneToOne.Value.ShouldBe(3);

                result[0].OneToManyAsOne.ShouldBeNull();
                result[1].OneToManyAsOne.ShouldNotBeNull();
                result[1].OneToManyAsOne.Value.ShouldBe(999);
                result[2].OneToManyAsOne.ShouldNotBeNull();
                result[2].OneToManyAsOne.Value.ShouldBe(99);

                result[0].OneToMany.ShouldNotBeNull();
                result[0].OneToMany.ShouldBeEmpty();
                result[1].OneToMany.ShouldNotBeNull();
                result[1].OneToMany.Select(m => m.Value).ShouldBe(new[] { 55, 555 });
                result[2].OneToMany.ShouldNotBeNull();
                result[2].OneToMany.Select(m => m.Value).ShouldBe(new[] { 66 });

                result[0].OneToManySelf.ShouldNotBeNull();
                result[0].OneToManySelf.Select(m => m.Id).ShouldBe(new[] { 1, 2, 3 });
                result[1].OneToManySelf.ShouldNotBeNull();
                result[1].OneToManySelf.Select(m => m.Id).ShouldBe(new[] { 4 });
                result[2].OneToManySelf.ShouldBeEmpty();

                result[0].OneToManySelf[0].Children.ShouldBeEmpty();
                result[0].OneToManySelf[1].Children.Select(m => m.Id).ShouldBe(new[] { 5, 6 });
                result[0].OneToManySelf[2].Children.Select(m => m.Id).ShouldBe(new[] { 7, 8, 9 });

                result[0].OneToManySelf[1].Children[1].Children.Select(m => m.Id).ShouldBe(new[] { 10, 11 });
                result[0].OneToManySelf[2].Children[2].Children.Select(m => m.Id).ShouldBe(new[] { 12 });
            };

            verifyResult(result1);
            verifyResult(result2);
        }

        public async Task Test_Binding_OfAggregateWithEmptySets()
        {
            Query query = new Query()
            {
                QueryText = @"SELECT 1 AS `Item.NotUsedOne.Value` FROM sqlite_master WHERE 0 = 1;
                              SELECT 1 AS `Item.NotUsedMany.Item.Value` FROM sqlite_master WHERE 0 = 1",
            };

            BigAggregate result1 = DatabaseHelper.Default.Aggregate<BigAggregate>(query);
            BigAggregate result2 = await DatabaseHelper.Default.AggregateAsync<BigAggregate>(query);

            result1.ShouldNotBeNull();
            result2.ShouldNotBeNull();

            result1.NotUsedOne.ShouldBeNull();
            result2.NotUsedOne.ShouldBeNull();

            result1.NotUsedMany.ShouldBeNull();
            result2.NotUsedMany.ShouldBeNull();
        }
    }
}
