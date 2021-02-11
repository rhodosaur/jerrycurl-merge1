using Jerrycurl.Relations.Metadata;
using Jerrycurl.Relations.Test.Models;
using Jerrycurl.Relations.Language;
using Jerrycurl.Test;
using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace Jerrycurl.Relations.Test
{
    public class RelationTests
    {
        public void Test_Update_Model_Throws()
        {
            RootModel model = new RootModel();
            IRelation rel = DatabaseHelper.Default.Relation(model);

            Should.Throw<BindingException>(() =>
            {
                rel.Source.Model.Update(new RootModel());
                rel.Source.Model.Commit();
            });
        }

        public void Test_Update_Missing_Throws()
        {
            RootModel model = new RootModel();
            IRelation rel = DatabaseHelper.Default.Relation(model, "Complex.Value");

            IField value = rel.Scalar();

            Should.Throw<BindingException>(() =>
            {
                value.Update(10);
                value.Commit();
            });
        }

        public void Test_Update_ReadOnlyProperty_Throws()
        {
            IRelation rel = DatabaseHelper.Default.Relation(new RootModel(), "ReadOnly");
            IField scalar = rel.Scalar();

            Should.Throw<BindingException>(() =>
            {
                scalar.Update(12);
                scalar.Commit();
            });
        }

        public void Test_Update_NonConvertibleValue_Throws()
        {
            RootModel model = new RootModel();
            IRelation rel = DatabaseHelper.Default.Relation(model, "Complex.Value");

            IField value = rel.Scalar();

            Should.Throw<BindingException>(() =>
            {
                value.Update("String");
                value.Commit();
            });
        }


        public void Test_Update_NullToValueType_Throws()
        {
            RootModel model = new RootModel();
            IRelation rel = DatabaseHelper.Default.Relation(model, "Complex.Value");

            IField value = rel.Scalar();

            Should.Throw<BindingException>(() =>
            {
                value.Update(null);
                value.Commit();
            });
        }


        public void Test_Update_Property()
        {
            RootModel model = new RootModel() { Complex = new RootModel.SubModel() { Value = 6 } };
            IRelation rel = DatabaseHelper.Default.Relation(model, "Complex.Value");

            IField value = rel.Scalar();
            value.ShouldNotBeNull();

            value.Update(12);
            value.Snapshot.ShouldBe(12);
            value.Data.Value.ShouldBe(6);
            model.Complex.Value.ShouldBe(6);

            value.Commit();
            value.Snapshot.ShouldBe(12);
            value.Data.Value.ShouldBe(12);
            model.Complex.Value.ShouldBe(12);
        }

        public void Test_Update_NullValue()
        {
            RootModel model = new RootModel();
            IRelation rel = DatabaseHelper.Default.Relation(model, "Complex");

            IField complex = rel.Scalar();

            complex.ShouldNotBeNull();
            complex.Snapshot.ShouldBeNull();

            Should.NotThrow(() =>
            {
                complex.Update(new RootModel.SubModel() { Value = 10 });
                complex.Commit();
            });

            model.Complex.Value.ShouldBe(10);
        }

        public void Test_Update_ListIndexer()
        {
            RootModel model = new RootModel() { IntList = new List<int>() { 1, 2, 3, 4, 5 } };
            IRelation rel = DatabaseHelper.Default.Relation(model, "IntList.Item");
            IField value = rel.Column().ElementAt(2);

            Should.NotThrow(() =>
            {
                value.Update(10);
                value.Commit();
            });

            model.IntList.ShouldBe(new[] { 1, 2, 10, 4, 5 });
        }

        public void Test_Update_EnumerableIndexer_Throws()
        {
            RootModel model = new RootModel() { IntEnumerable = Enumerable.Range(1, 5) };
            IRelation rel = DatabaseHelper.Default.Relation(model, "IntEnumerable.Item");
            IField value = rel.Column().ElementAt(2);

            Should.Throw<BindingException>(() =>
            {
                value.Update(10);
                value.Commit();
            });
        }

        public void Test_Update_EnumerableListIndexer()
        {
            RootModel model = new RootModel() { IntEnumerable = new List<int>() { 1, 2, 3, 4, 5 } };
            IRelation rel = DatabaseHelper.Default.Relation(model, "IntEnumerable.Item");
            IField value = rel.Column().ElementAt(2);

            Should.NotThrow(() =>
            {
                value.Update(10);
                value.Commit();
            });

            model.IntEnumerable.ShouldBe(new[] { 1, 2, 10, 4, 5 });
        }

        public void Test_Update_Contravariant()
        {
            RootModel model = new RootModel();
            IRelation rel = DatabaseHelper.Default.Relation(model, "Object");
            IField value = rel.Scalar();

            Should.NotThrow(() =>
            {
                value.Update(new RootModel());
                value.Commit();
            });
        }

        public void Test_Update_ObjectGraph()
        {
            RootModel model = new RootModel()
            {
                Complex = new RootModel.SubModel()
                {
                    Value = 50,
                    Complex = new RootModel.SubModel2()
                    {
                        Value = "String 1",
                    },
                },
                ComplexList = new List<RootModel.SubModel>()
                {
                    new RootModel.SubModel() { Complex = new RootModel.SubModel2() { Value = "String 2" } },
                    new RootModel.SubModel() { Complex = new RootModel.SubModel2() { Value = "String 3" } },
                },
            };
            IRelation rel1 = DatabaseHelper.Default.Relation(model, "Complex.Value", "Complex.Complex.Value");
            IRelation rel2 = DatabaseHelper.Default.Relation(model, "ComplexList.Item.Complex.Value");

            ITuple tuple1 = rel1.Row();
            IField[] tuple2 = rel2.Column().ToArray();

            tuple1[0].Update(100); tuple1[0].Commit();
            tuple1[1].Update("String 3"); tuple1[1].Commit();
            tuple2[0].Update("String 4"); tuple2[0].Commit();
            tuple2[1].Update("String 5"); tuple2[1].Commit();


            model.Complex.Value.ShouldBe(100);
            model.Complex.Complex.Value.ShouldBe("String 3");
            model.ComplexList[0].Complex.Value.ShouldBe("String 4");
            model.ComplexList[1].Complex.Value.ShouldBe("String 5");
        }


        public void Test_Select_UnknownProperty_Throws()
        {
            RootModel model = new RootModel();

            Should.Throw<MetadataException>(() => DatabaseHelper.Default.Relation(model, "Unknown123"));
        }

        public void Test_Select_SourceTraverse()
        {
            DeepModel model = new DeepModel()
            {
                Sub1 = new DeepModel.SubModel1()
                {
                    Sub2 = new DeepModel.SubModel2()
                    {
                        Sub3 = new List<DeepModel.SubModel3>()
                        {
                            new DeepModel.SubModel3()
                            {
                                Sub4 = new DeepModel.SubModel4()
                                {
                                    Sub5 = new List<DeepModel.SubModel5>()
                                    {
                                        new DeepModel.SubModel5()
                                        {
                                            Sub6 = new List<DeepModel.SubModel6>()
                                            {
                                                new DeepModel.SubModel6() { Value = 1 },
                                                new DeepModel.SubModel6() { Value = 2 },
                                                new DeepModel.SubModel6() { Value = 3 },
                                            },
                                        },
                                        new DeepModel.SubModel5()
                                        {
                                            Sub6 = new List<DeepModel.SubModel6>()
                                            {
                                                new DeepModel.SubModel6() { Value = 4 },
                                                new DeepModel.SubModel6() { Value = 5 },
                                            },
                                        },
                                        new DeepModel.SubModel5()
                                        {
                                            Sub6 = new List<DeepModel.SubModel6>()
                                            {
                                                new DeepModel.SubModel6() { Value = 6 },
                                            },
                                        }
                                    },
                                },
                            },
                            new DeepModel.SubModel3()
                            {
                                Sub4 = new DeepModel.SubModel4()
                                {
                                    Sub5 = new List<DeepModel.SubModel5>()
                                    {
                                        new DeepModel.SubModel5()
                                        {
                                            Sub6 = new List<DeepModel.SubModel6>()
                                            {
                                                new DeepModel.SubModel6() { Value = 7 },
                                                new DeepModel.SubModel6() { Value = 8 },
                                                null,
                                            },
                                        },
                                        new DeepModel.SubModel5()
                                        {
                                            Sub6 = new List<DeepModel.SubModel6>()
                                            {
                                                new DeepModel.SubModel6() { Value = 9 },
                                            },
                                        },
                                    },
                                },
                            },
                            new DeepModel.SubModel3()
                            {
                                Sub4 = new DeepModel.SubModel4()
                                {
                                    Sub5 = new List<DeepModel.SubModel5>(),
                                },
                            },
                        },
                    },
                },
            };

            string valueAttr = "Sub1.Sub2.Sub3.Item.Sub4.Sub5.Item.Sub6.Item.Value";

            IRelation rel1 = DatabaseHelper.Default.Relation(model, valueAttr);
            IRelation rel2 = rel1.Source.Lookup("Sub1").Select(valueAttr);
            IRelation rel3 = rel2.Source.Lookup("Sub1.Sub2").Select(valueAttr);
            IRelation rel4 = rel3.Source.Lookup("Sub1.Sub2.Sub3").Select(valueAttr);
            IRelation rel5 = rel4.Source.Lookup("Sub1.Sub2.Sub3.Item").Select(valueAttr);
            IRelation rel6 = rel5.Source.Lookup("Sub1.Sub2.Sub3.Item.Sub4").Select(valueAttr);
            IRelation rel7 = rel6.Source.Lookup("Sub1.Sub2.Sub3.Item.Sub4.Sub5").Select(valueAttr);
            IRelation rel8 = rel7.Source.Lookup("Sub1.Sub2.Sub3.Item.Sub4.Sub5.Item").Select(valueAttr);
            IRelation rel9 = rel8.Source.Lookup("Sub1.Sub2.Sub3.Item.Sub4.Sub5.Item.Sub6").Select(valueAttr);
            IRelation rel10 = rel9.Source.Lookup("Sub1.Sub2.Sub3.Item.Sub4.Sub5.Item.Sub6.Item").Select(valueAttr);
            IRelation rel11 = rel10.Source.Lookup("Sub1.Sub2.Sub3.Item.Sub4.Sub5.Item.Sub6.Item.Value").Select(valueAttr);

            rel1.Column().Select(f => (int?)f.Snapshot).ShouldBe(new int?[] { 1, 2, 3, 4, 5, 6, 7, 8, null, 9 });
            rel2.Column().Select(f => (int?)f.Snapshot).ShouldBe(new int?[] { 1, 2, 3, 4, 5, 6, 7, 8, null, 9 });
            rel3.Column().Select(f => (int?)f.Snapshot).ShouldBe(new int?[] { 1, 2, 3, 4, 5, 6, 7, 8, null, 9 });
            rel4.Column().Select(f => (int?)f.Snapshot).ShouldBe(new int?[] { 1, 2, 3, 4, 5, 6, 7, 8, null, 9 });
            rel5.Column().Select(f => (int?)f.Snapshot).ShouldBe(new int?[] { 1, 2, 3, 4, 5, 6 });
            rel6.Column().Select(f => (int?)f.Snapshot).ShouldBe(new int?[] { 1, 2, 3, 4, 5, 6 });
            rel7.Column().Select(f => (int?)f.Snapshot).ShouldBe(new int?[] { 1, 2, 3, 4, 5, 6 });
            rel8.Column().Select(f => (int?)f.Snapshot).ShouldBe(new int?[] { 1, 2, 3 });
            rel9.Column().Select(f => (int?)f.Snapshot).ShouldBe(new int?[] { 1, 2, 3 });
            rel10.Column().Select(f => (int?)f.Snapshot).ShouldBe(new int?[] { 1 });
            rel11.Column().Select(f => (int?)f.Snapshot).ShouldBe(new int?[] { 1 });
        }

        public void Test_Select_OneToOneOuterJoin()
        {
            RootModel model = new RootModel() { IntValue = 1 };
            ITuple tuple = DatabaseHelper.Default.Relation(model, "IntValue", "Complex.Complex.Value").Row();

            tuple.Degree.ShouldBe(2);

            tuple[0].Snapshot.ShouldBe(1);
            tuple[1].Snapshot.ShouldBeNull();
        }

        public void Test_Select_OneToManyInnerJoin()
        {
            List<RootModel> model = new List<RootModel>()
            {
                new RootModel()
                {
                    IntValue = 1,
                    ComplexList = new List<RootModel.SubModel>()
                    {
                        new RootModel.SubModel() { Value = 10 }
                    }
                },
                new RootModel()
                {
                    IntValue = 2,
                    ComplexList = new List<RootModel.SubModel>()
                    {
                        new RootModel.SubModel() { Value = 11 },
                        new RootModel.SubModel() { Value = 12 }
                    }
                },
                new RootModel() { IntValue = 3 },
                new RootModel() { IntValue = 4, ComplexList = new List<RootModel.SubModel>() },
            };

            ITuple[] result = DatabaseHelper.Default.Relation(model, "Item.IntValue", "Item.ComplexList.Item.Value").Body.ToArray();

            result.Length.ShouldBe(3);

            result[0][0].Snapshot.ShouldBe(1);
            result[0][1].Snapshot.ShouldBe(10);

            result[1][0].Snapshot.ShouldBe(2);
            result[1][1].Snapshot.ShouldBe(11);

            result[2][0].Snapshot.ShouldBe(2);
            result[2][1].Snapshot.ShouldBe(12);
        }

        public void Test_Select_AdjacentCrossJoin()
        {
            RootModel model = new RootModel()
            {
                ComplexList = new List<RootModel.SubModel>()
                {
                    new RootModel.SubModel() { Value = 1 },
                    new RootModel.SubModel() { Value = 2 },
                    new RootModel.SubModel() { Value = 3 },
                },
                ComplexList2 = new List<RootModel.SubModel>()
                {
                    new RootModel.SubModel() { Value = 4 },
                    new RootModel.SubModel() { Value = 5 },
                    new RootModel.SubModel() { Value = 6 },
                    new RootModel.SubModel() { Value = 7 },
                }
            };
            IRelation rel1 = DatabaseHelper.Default.Relation(model, "ComplexList.Item.Value", "ComplexList2.Item.Value");
            IRelation rel2 = DatabaseHelper.Default.Relation(model, "ComplexList2.Item.Value", "ComplexList.Item.Value");

            IList<(int, int)> pairs1 = rel1.Body.Select(t => ((int)t[0].Snapshot, (int)t[1].Snapshot)).ToList();
            IList<(int, int)> pairs2 = rel2.Body.Select(t => ((int)t[0].Snapshot, (int)t[1].Snapshot)).ToList();

            pairs1.Count.ShouldBe(3 * 4);
            pairs2.Count.ShouldBe(4 * 3);

            pairs1[0].ShouldBe((1, 4));
            pairs1[1].ShouldBe((1, 5));
            pairs1[2].ShouldBe((1, 6));
            pairs1[3].ShouldBe((1, 7));
            pairs1[4].ShouldBe((2, 4));
            pairs1[5].ShouldBe((2, 5));
            pairs1[6].ShouldBe((2, 6));
            pairs1[7].ShouldBe((2, 7));
            pairs1[8].ShouldBe((3, 4));
            pairs1[9].ShouldBe((3, 5));
            pairs1[10].ShouldBe((3, 6));
            pairs1[11].ShouldBe((3, 7));

            pairs2[0].ShouldBe((4, 1));
            pairs2[1].ShouldBe((4, 2));
            pairs2[2].ShouldBe((4, 3));
            pairs2[3].ShouldBe((5, 1));
            pairs2[4].ShouldBe((5, 2));
            pairs2[5].ShouldBe((5, 3));
            pairs2[6].ShouldBe((6, 1));
            pairs2[7].ShouldBe((6, 2));
            pairs2[8].ShouldBe((6, 3));
            pairs2[9].ShouldBe((7, 1));
            pairs2[10].ShouldBe((7, 2));
            pairs2[11].ShouldBe((7, 3));
        }

        public void Test_Select_ScalarList()
        {
            RootModel model = new RootModel()
            {
                IntList = new List<int>() { 1, 2, 3, 4, 5 },
            };
            IRelation rel = DatabaseHelper.Default.Relation(model, "IntList.Item");
            IEnumerable<int> ints = rel.Column().Select(f => (int)f.Snapshot);

            ints.ShouldBe(new[] { 1, 2, 3, 4, 5 });
        }

        public void Test_Select_NotReachable_Throws()
        {
            RootModel model = new RootModel()
            {
                IntValue = 100,
                IntList = new List<int>(),
            };
            IRelation rel1 = DatabaseHelper.Default.Relation(model, "IntValue");
            IField nonParent = rel1.Scalar();
            
            IRelation rel2 = Should.NotThrow(() => nonParent.Select("IntList"));
            Should.Throw<RelationException>(() => rel2.Scalar());
        }

        public void Test_Select_RecursiveBreadthFirst()
        {
            List<RecursiveModel> model = new List<RecursiveModel>()
            {
                new RecursiveModel()
                {
                    Name = "1",
                    Subs = new List<RecursiveModel>()
                    {
                        new RecursiveModel()
                        {
                            Name = "1.1",
                            Subs = new List<RecursiveModel>()
                            {
                                new RecursiveModel()
                                {
                                    Name = "1.1.1",
                                    Subs = new List<RecursiveModel>()
                                    {
                                        new RecursiveModel() { Name = "1.1.1.1" },
                                        new RecursiveModel() { Name = "1.1.1.2" },
                                        new RecursiveModel()
                                        {
                                            Name = "1.1.1.3",
                                            Subs = new List<RecursiveModel>()
                                            {
                                                new RecursiveModel() { Name = "1.1.1.3.1" },
                                            }
                                        },
                                        new RecursiveModel() { Name = "1.1.1.4" },
                                    }
                                },
                                new RecursiveModel()
                                {
                                    Name = "1.1.2",
                                    Subs = new List<RecursiveModel>()
                                    {
                                        new RecursiveModel() { Name = "1.1.2.1" },
                                        new RecursiveModel() { Name = "1.1.2.2" },
                                    }
                                },
                                new RecursiveModel()
                                {
                                    Name = "1.1.3",
                                }
                            }
                        }
                    }
                },
                new RecursiveModel()
                {
                    Name = "2",
                    Subs = new List<RecursiveModel>()
                    {
                        new RecursiveModel()
                        {
                            Name = "2.1",
                            Subs = new List<RecursiveModel>()
                            {
                                new RecursiveModel() { Name = "2.1.1" },
                                new RecursiveModel() { Name = "2.1.2" },
                                new RecursiveModel()
                                {
                                    Name = "2.1.3",
                                    Subs = new List<RecursiveModel>()
                                    {
                                        new RecursiveModel() { Name = "2.1.3.1" },
                                        new RecursiveModel() { Name = "2.1.3.2" },
                                        new RecursiveModel() { Name = "2.1.3.3" },
                                    }
                                }
                            }
                        },
                        new RecursiveModel()
                        {
                            Name = "2.2",
                            Subs = new List<RecursiveModel>()
                            {
                                new RecursiveModel() { Name = "2.2.1" },
                                new RecursiveModel() { Name = "2.2.2" },
                            }
                        }
                    }
                },
                new RecursiveModel() { Name = "3" },
                new RecursiveModel() { Name = "4" },
            };

            IRelation rel1 = DatabaseHelper.Default.Relation(model, "Item.Name");
            IRelation rel2 = DatabaseHelper.Default.Relation(model, "Item.Subs.Item.Name");

            IList<string> actual1 = rel1.Body.Select(t => (string)t[0].Snapshot).ToList();
            IList<string> actual2 = rel2.Body.Select(t => (string)t[0].Snapshot).ToList();

            actual1.ShouldBe(new[] { "1", "2", "3", "4" });
            actual2.ShouldBe(new[] { "1.1",
                                       "1.1.1", "1.1.2", "1.1.3",
                                         "1.1.1.1", "1.1.1.2", "1.1.1.3", "1.1.1.4", "1.1.2.1", "1.1.2.2",
                                           "1.1.1.3.1",
                                     "2.1", "2.2",
                                       "2.1.1", "2.1.2", "2.1.3", "2.2.1", "2.2.2",
                                         "2.1.3.1", "2.1.3.2", "2.1.3.3"
            });
        }
    }
}
