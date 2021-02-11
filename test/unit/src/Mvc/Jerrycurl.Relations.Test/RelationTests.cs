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
    public class RelationTests
    {
        public void Test_Reading_UnknownProperty_Throws()
        {
            RootModel model = new RootModel();

            Should.Throw<MetadataException>(() => DatabaseHelper.Default.Relation2(model, "Unknown123"));
        }

        public void Test_Binding_ToModel_Throws()
        {
            RootModel model = new RootModel();
            IRelation2 rel = DatabaseHelper.Default.Relation2(model);

            Should.Throw<BindingException>(() =>
            {
                rel.Source.Model.Update(new RootModel());
                rel.Source.Model.Commit();
            });
        }

        public void Test_Binding_ToMissing_Throws()
        {
            RootModel model = new RootModel();
            IRelation2 rel = DatabaseHelper.Default.Relation2(model, "Complex.Value");

            IField2 value = rel.Scalar();

            Should.Throw<BindingException>(() =>
            {
                value.Update(10);
                value.Commit();
            });
        }

        public void Test_Binding_ToReadOnlyProperty_Throws()
        {
            IRelation2 rel = DatabaseHelper.Default.Relation2(new RootModel(), "ReadOnly");
            IField2 scalar = rel.Scalar();

            Should.Throw<BindingException>(() =>
            {
                scalar.Update(12);
                scalar.Commit();
            });
        }

        public void Test_Binding_OfNonConvertibleValue_Throws()
        {
            RootModel model = new RootModel();
            IRelation2 rel = DatabaseHelper.Default.Relation2(model, "Complex.Value");

            IField2 value = rel.Scalar();

            Should.Throw<BindingException>(() =>
            {
                value.Update("String");
                value.Commit();
            });
        }


        public void Test_Binding_NullToValueType_Throws()
        {
            RootModel model = new RootModel();
            IRelation2 rel = DatabaseHelper.Default.Relation2(model, "Complex.Value");

            IField2 value = rel.Scalar();

            Should.Throw<BindingException>(() =>
            {
                value.Update(null);
                value.Commit();
            });
        }


        public void Test_Binding_ToProperty()
        {
            RootModel model = new RootModel() { Complex = new RootModel.SubModel() { Value = 6 } };
            IRelation2 rel = DatabaseHelper.Default.Relation2(model, "Complex.Value");

            IField2 value = rel.Scalar();
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

        public void Test_Binding_ToNullValue()
        {
            RootModel model = new RootModel();
            IRelation2 rel = DatabaseHelper.Default.Relation2(model, "Complex");

            IField2 complex = rel.Scalar();

            complex.ShouldNotBeNull();

            Should.NotThrow(() =>
            {
                complex.Update(new RootModel.SubModel() { Value = 10 });
                complex.Commit();
            });

            model.Complex.Value.ShouldBe(10);
        }

        public void Test_Binding_ToListIndexer()
        {
            RootModel model = new RootModel() { IntList = new List<int>() { 1, 2, 3, 4, 5 } };
            IRelation2 rel = DatabaseHelper.Default.Relation2(model, "IntList.Item");
            IField2 value = rel.Column().ElementAt(2);

            Should.NotThrow(() =>
            {
                value.Update(10);
                value.Commit();
            });

            model.IntList.ShouldBe(new[] { 1, 2, 10, 4, 5 });
        }

        public void Test_Binding_ToEnumerableIndexer_Throws()
        {
            RootModel model = new RootModel() { IntEnumerable = Enumerable.Range(1, 5) };
            IRelation2 rel = DatabaseHelper.Default.Relation2(model, "IntEnumerable.Item");
            IField2 value = rel.Column().ElementAt(2);

            Should.Throw<BindingException>(() =>
            {
                value.Update(10);
                value.Commit();
            });
        }

        public void Test_Binding_ToEnumerableListIndexer()
        {
            RootModel model = new RootModel() { IntEnumerable = new List<int>() { 1, 2, 3, 4, 5 } };
            IRelation2 rel = DatabaseHelper.Default.Relation2(model, "IntEnumerable.Item");
            IField2 value = rel.Column().ElementAt(2);

            Should.NotThrow(() =>
            {
                value.Update(10);
                value.Commit();
            });

            model.IntEnumerable.ShouldBe(new[] { 1, 2, 10, 4, 5 });
        }

        public void Test_Binding_WithContravariance()
        {
            RootModel model = new RootModel();
            IRelation2 rel = DatabaseHelper.Default.Relation2(model, "Object");
            IField2 value = rel.Scalar();

            Should.NotThrow(() =>
            {
                value.Update(new RootModel());
                value.Commit();
            });
        }

        public void Test_Binding_ToDeepObjectGraph()
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
            IRelation2 rel1 = DatabaseHelper.Default.Relation2(model, "Complex.Value", "Complex.Complex.Value");
            IRelation2 rel2 = DatabaseHelper.Default.Relation2(model, "ComplexList.Item.Complex.Value");

            ITuple2 tuple1 = rel1.Row();
            IField2[] tuple2 = rel2.Column().ToArray();

            tuple1[0].Update(100); tuple1[0].Commit();
            tuple1[1].Update("String 3"); tuple1[1].Commit();
            tuple2[0].Update("String 4"); tuple2[0].Commit();
            tuple2[1].Update("String 5"); tuple2[1].Commit();


            model.Complex.Value.ShouldBe(100);
            model.Complex.Complex.Value.ShouldBe("String 3");
            model.ComplexList[0].Complex.Value.ShouldBe("String 4");
            model.ComplexList[1].Complex.Value.ShouldBe("String 5");
        }


        public void Test_Reading_OfDeepObjectGraphFromDifferentSources()
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

            IRelation2 rel1 = DatabaseHelper.Default.Relation2(model, valueAttr);
            IRelation2 rel2 = rel1.Source.Select("Sub1").Scalar().Select(valueAttr);
            IRelation2 rel3 = rel2.Source.Select("Sub1.Sub2").Scalar().Select(valueAttr);
            IRelation2 rel4 = rel3.Source.Select("Sub1.Sub2.Sub3").Scalar().Select(valueAttr);
            IRelation2 rel5 = rel4.Source.Select("Sub1.Sub2.Sub3.Item").Scalar().Select(valueAttr);
            IRelation2 rel6 = rel5.Source.Select("Sub1.Sub2.Sub3.Item.Sub4").Scalar().Select(valueAttr);
            IRelation2 rel7 = rel6.Source.Select("Sub1.Sub2.Sub3.Item.Sub4.Sub5").Scalar().Select(valueAttr);
            IRelation2 rel8 = rel7.Source.Select("Sub1.Sub2.Sub3.Item.Sub4.Sub5.Item").Scalar().Select(valueAttr);
            IRelation2 rel9 = rel8.Source.Select("Sub1.Sub2.Sub3.Item.Sub4.Sub5.Item.Sub6").Scalar().Select(valueAttr);
            IRelation2 rel10 = rel9.Source.Select("Sub1.Sub2.Sub3.Item.Sub4.Sub5.Item.Sub6.Item").Scalar().Select(valueAttr);
            IRelation2 rel11 = rel10.Source.Select("Sub1.Sub2.Sub3.Item.Sub4.Sub5.Item.Sub6.Item.Value").Scalar().Select(valueAttr);

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

        public void Test_Reading_OneToOneWithOuterJoin()
        {
            RootModel model = new RootModel() { IntValue = 1 };
            ITuple2 tuple = DatabaseHelper.Default.Relation2(model, "IntValue", "Complex.Complex.Value").Row();

            tuple.Degree.ShouldBe(2);

            tuple[0].Snapshot.ShouldBe(1);
            tuple[1].Snapshot.ShouldBeNull();
        }

        public void Test_Reading_OneToManyWithInnerJoin()
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

            ITuple2[] result = DatabaseHelper.Default.Relation2(model, "Item.IntValue", "Item.ComplexList.Item.Value").Body.ToArray();

            result.Length.ShouldBe(3);

            result[0][0].Snapshot.ShouldBe(1);
            result[0][1].Snapshot.ShouldBe(10);

            result[1][0].Snapshot.ShouldBe(2);
            result[1][1].Snapshot.ShouldBe(11);

            result[2][0].Snapshot.ShouldBe(2);
            result[2][1].Snapshot.ShouldBe(12);
        }

        public void Test_Reading_AdjacentListsWithCrossJoin()
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
            IRelation2 rel1 = DatabaseHelper.Default.Relation2(model, "ComplexList.Item.Value", "ComplexList2.Item.Value");
            IRelation2 rel2 = DatabaseHelper.Default.Relation2(model, "ComplexList2.Item.Value", "ComplexList.Item.Value");

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

        public void Test_Reading_ScalarList()
        {
            RootModel model = new RootModel()
            {
                IntList = new List<int>() { 1, 2, 3, 4, 5 },
            };
            IRelation2 rel = DatabaseHelper.Default.Relation2(model, "IntList.Item");
            IEnumerable<int> ints = rel.Column().Select(f => (int)f.Snapshot);

            ints.ShouldBe(new[] { 1, 2, 3, 4, 5 });
        }

        public void Test_Reading_OfValueFromNonParentSource_Throws()
        {
            RootModel model = new RootModel()
            {
                IntValue = 100,
                IntList = new List<int>(),
            };
            IRelation2 rel1 = DatabaseHelper.Default.Relation2(model, "IntValue");
            IField2 nonParent = rel1.Scalar();
            
            IRelation2 rel2 = Should.NotThrow(() => nonParent.Select("IntList"));
            Should.Throw<RelationException>(() => rel2.Scalar());
        }

    }
}
