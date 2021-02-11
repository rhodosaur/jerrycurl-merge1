using System.Threading.Tasks;
using Shouldly;
using Jerrycurl.Test;
using Jerrycurl.Data.Test.Model;
using Jerrycurl.Data.Queries;
using Jerrycurl.Data.Language;
using Jerrycurl.Relations.Language;
using System.Collections.Generic;
using Jerrycurl.Data.Metadata;
using System.Collections.Immutable;
using System.Linq;
using Newtonsoft.Json.Serialization;
using System.IO;
using System;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Data.Test
{
    public class Query2Tests
    {
        public void Test_Empty_ObjectAndList()
        {
            var store = DatabaseHelper.Default.Store;
            var schema1 = store.GetSchema(typeof(Blog));
            var schema2 = store.GetSchema(typeof(IList<Blog>));

            var buffer1 = new QueryBuffer(schema1, QueryType.List);
            var buffer2 = new QueryBuffer(schema2, QueryType.List);

            var result1 = buffer1.Commit<Blog>();
            var result2 = buffer2.Commit<IList<Blog>>();

            result1.ShouldBeNull();
            result2.ShouldBeNull();
        }

        public void Test_Read_NullableSet()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new int?[] { 1, 2, null };

            using var dataReader = store.From(data).Select("Item").As("Item");

            var reader = new QueryReader(store, dataReader);
            var result = reader.Read<int?>();

            result.ShouldBe(new int?[] { 1, 2, null });
        }

        public void Test_Read_IntegerSet()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new[] { 1, 2, 3, 4, 5, 6 };

            using var dataReader = store.From(data).Select("Item").As("Item");

            var reader = new QueryReader(store, dataReader);
            var result = reader.Read<int>();

            result.ShouldBe(new[] { 1, 2, 3, 4, 5, 6 });
        }

        public void Test_Insert_ManyToOne_List()
        {
            var store = DatabaseHelper.Default.Store;

            var data1 = new (int, int)[]
            {
                // BlogPost(Id, BlogId)
                (1, 1),
                (2, 1),
                (3, 2),
                (4, 4),
            };
            var data2 = new[] { 1, 2, 3 }; // Blog(Id)

            var schema = store.GetSchema(typeof(List<BlogPostView>));
            var buffer = new QueryBuffer(schema, QueryType.List);

            buffer.Insert(data1,
                ("Item.Item1", "Item.Id"),
                ("Item.Item2", "Item.BlogId")
            );

            buffer.Insert(data2,
                ("Item", "Item.Blog2.Item.Id")
            );

            var result = buffer.Commit<List<BlogPostView>>();

            result.Count.ShouldBe(4);

            result[0].Blog2.ShouldNotBeNull();
            result[0].Blog2.HasValue.ShouldBeTrue();
            result[0].Blog2.Value.Id.ShouldBe(1);
            result[0].Blog2.ShouldBeSameAs(result[1].Blog2);

            result[2].Blog2.ShouldNotBeNull();
            result[2].Blog2.HasValue.ShouldBeTrue();
            result[2].Blog2.Value.Id.ShouldBe(2);

            result[3].Blog2.ShouldNotBeNull();
            result[3].Blog2.HasValue.ShouldBeFalse();
        }

        public void Test_Insert_ManyToOne_Object()
        {
            var store = DatabaseHelper.Default.Store;

            var data1 = new[] { 2 }; // Blog(Id)
            var data2 = new (int, int)[]
            {
                // BlogPost(Id, BlogId)
                (2, 1),
                (1, 2),
            };
            var data3 = new[] { 1 }; // Blog(Id)

            var schema = store.GetSchema(typeof(List<BlogPostView>));
            var buffer = new QueryBuffer(schema, QueryType.List);

            buffer.Insert(data1,
                ("Item", "Item.Blog1.Id")
            );

            buffer.Insert(data2,
                ("Item.Item1", "Item.Id"),
                ("Item.Item2", "Item.BlogId")
            );

            buffer.Insert(data3,
                ("Item", "Item.Blog1.Id")
            );

            var result = buffer.Commit<List<BlogPostView>>();

            result.Count.ShouldBe(2);

            result[0].Blog1.ShouldBeNull();

            result[1].Blog1.ShouldNotBeNull();
            result[1].Blog1.Id.ShouldBe(2);
        }

        public void Test_Insert_DualRecursiveTree()
        {
            var store = DatabaseHelper.Default.Store;
            var schema = store.GetSchema(typeof(IList<BlogCategory>));
            var buffer = new QueryBuffer(schema, QueryType.List);

            // BlogCategory(Id, ParentId)
            var data1 = new (int, int?)[] // Parents
            {
                (1, null), (2, 0), (3, 1), (4, 1),
            };

            var data2 = new (int, int?)[]
            {
                (5, 2), (6, 3)
            };

            var data3 = new (int, int?)[] // Children
            {
                (7, 6), (8, 6), (9, 8), (10, 9),
            };

            buffer.Insert(data1,
                ("Item.Item1", "Item.Parent.Item.Id"),
                ("Item.Item2", "Item.Parent.Item.ParentId")
            );

            buffer.Insert(data2,
                ("Item.Item1", "Item.Id"),
                ("Item.Item2", "Item.ParentId")
            );

            buffer.Insert(data3,
                ("Item.Item1", "Item.Children.Item.Id"),
                ("Item.Item2", "Item.Children.Item.ParentId")
            );

            var result = buffer.Commit<IList<BlogCategory>>();

            result.Count.ShouldBe(2);
            result[0].Id.ShouldBe(5);
            result[0].ParentId.ShouldBe(2);
            result[0].Parent.ShouldNotBeNull();
            result[0].Parent.HasValue.ShouldBeTrue();

            result[0].Parent.HasValue.ShouldBeTrue();
            result[0].Parent.Value.Id.ShouldBe(2);
            result[0].Parent.Value.ParentId.ShouldBe(0);
            result[0].Parent.Value.Parent.ShouldNotBeNull();
            result[0].Parent.Value.Parent.HasValue.ShouldBeFalse();

            result[1].Id.ShouldBe(6);
            result[1].ParentId.ShouldBe(3);

            result[1].Parent.HasValue.ShouldBeTrue();
            result[1].Parent.Value.Id.ShouldBe(3);
            result[1].Parent.Value.ParentId.ShouldBe(1);

            result[1].Parent.Value.Parent.HasValue.ShouldBeTrue();
            result[1].Parent.Value.Parent.Value.Id.ShouldBe(1);
            result[1].Parent.Value.Parent.Value.ParentId.ShouldBeNull();
            result[1].Parent.Value.Parent.Value.Parent.ShouldBeNull();

            result[1].Children.ShouldNotBeNull();
            result[1].Children.Count.ShouldBe(2);
            result[1].Children.Select(c => c.Id).ShouldBe(new[] { 7, 8 });

            result[1].Children[0].Children.ShouldNotBeNull();
            result[1].Children[0].Children.Count.ShouldBe(0);

            result[1].Children[1].Children.ShouldNotBeNull();
            result[1].Children[1].Children.Count.ShouldBe(1);
            result[1].Children[1].Children[0].Id.ShouldBe(9);
            result[1].Children[1].Children[0].ParentId.ShouldBe(8);
            result[1].Children[1].Children[0].Children.ShouldNotBeNull();
            result[1].Children[1].Children[0].Children.Count.ShouldBe(1);
            result[1].Children[1].Children[0].Children[0].Id.ShouldBe(10);
            result[1].Children[1].Children[0].Children[0].ParentId.ShouldBe(9);
            result[1].Children[1].Children[0].Children[0].Children.ShouldNotBeNull();
            result[1].Children[1].Children[0].Children[0].Children.Count.ShouldBe(0);
        }

        public void Test_Aggregate_PrimaryKey()
        {
            var store = DatabaseHelper.Default.Store;
            var schema = store.GetSchema(typeof(Blog));
            var buffer = new QueryBuffer(schema, QueryType.Aggregate);

            buffer.Insert<object>(null, ("", "Id"));

            var result = buffer.Commit();

            result.ShouldBeNull();
        }

        public void Test_Aggregate_NonPrimary()
        {
            var store = DatabaseHelper.Default.Store;
            var schema = store.GetSchema(typeof(Blog));
            var buffer = new QueryBuffer(schema, QueryType.Aggregate);

            buffer.Insert<object>(null, ("", "Id2"));

            var result = buffer.Commit<Blog>();

            result.ShouldNotBeNull();
            result.Id2.ShouldBe(0);
        }

        public void Test_Insert_One()
        {
            var store = DatabaseHelper.Default.Store;
            
            var data = new (int, string)[]
            {
                (1, "Hello World!"),
                (2, "Hello Universe!"),
            };

            var schema = store.GetSchema(typeof(Blog));
            var buffer = new QueryBuffer(schema, QueryType.List);

            buffer.Insert(data,
                ("Item.Item1", "Id"),
                ("Item.Item2", "Title")
            );

            var result = buffer.Commit<Blog>();

            result.ShouldNotBeNull();
            result.Id.ShouldBe(2);
            result.Title.ShouldBe("Hello Universe!");
        }

        public void Test_Insert_OneToMany_NonPrimary()
        {
            var store = DatabaseHelper.Default.Store;

            var data1 = new (int?, string)[]
            {
                ( 1, "Blog 1" ),
                ( null, "Blog 2" ),
            };
            var data2 = new (int, int, string)[]
            {
                ( 1, 1, "Post 1.1" ),
                ( 2, 1, "Post 1.2" ),
                ( 3, 2, "Post 2.1" ),
            };

            var buffer = new QueryBuffer(store.Describe<IList<Blog>>(), QueryType.List);

            buffer.Insert(data1,
                ("Item.Item1", "Item.Id2"),
                ("Item.Item2", "Item.Title")
            );

            buffer.Insert(data2,
                ("Item.Item1", "Item.Posts.Item.Id"),
                ("Item.Item2", "Item.Posts.Item.BlogId2"),
                ("Item.Item3", "Item.Posts.Item.Headline")
            );

            var result = buffer.Commit<IList<Blog>>();

            result.Count.ShouldBe(2);

            result[0].Id2.ShouldBe(1);
            result[0].Title.ShouldBe("Blog 1");

            result[0].Posts.Count.ShouldBe(2);
            result[0].Posts[0].BlogId2.ShouldBe(result[0].Id2);
            result[0].Posts[0].Headline.ShouldBe("Post 1.1");
            result[0].Posts[1].BlogId2.ShouldBe(result[0].Id2);
            result[0].Posts[1].Headline.ShouldBe("Post 1.2");

            result[1].Id2.ShouldBe(0);
            result[1].Title.ShouldBe("Blog 2");

            result[1].Posts.ShouldBeNull();
        }

        public void Test_Insert_NonMatching()
        {
            var store = DatabaseHelper.Default.Store;
            var schema1 = store.GetSchema(typeof(Blog));
            var schema2 = store.GetSchema(typeof(List<Blog>));
            var buffer1 = new QueryBuffer(schema1, QueryType.List);
            var buffer2 = new QueryBuffer(schema2, QueryType.List);

            buffer1.Insert(50, ("", "Foo"));
            buffer2.Insert(50, ("", "Item.Bar"));

            var result1 = buffer1.Commit();
            var result2 = buffer1.Commit();

            result1.ShouldBeNull();
            result2.ShouldBeNull();
        }

        public void Test_Aggregate_NonMatching()
        {
            var store = DatabaseHelper.Default.Store;
            var schema1 = store.GetSchema(typeof(Blog));
            var schema2 = store.GetSchema(typeof(List<Blog>));
            var buffer1 = new QueryBuffer(schema1, QueryType.Aggregate);
            var buffer2 = new QueryBuffer(schema2, QueryType.Aggregate);

            buffer1.Insert(50, ("", "Foo"));
            buffer2.Insert(50, ("", "Item.Bar"));

            var result1 = buffer1.Commit();
            var result2 = buffer1.Commit();

            result1.ShouldBeNull();
            result2.ShouldBeNull();
        }

        public void Test_Insert_EmptySet()
        {
            var store = DatabaseHelper.Default.Store;
            var schema1 = store.GetSchema(typeof(Blog));
            var schema2 = store.GetSchema(typeof(List<Blog>));
            var buffer1 = new QueryBuffer(schema1, QueryType.List);
            var buffer2 = new QueryBuffer(schema2, QueryType.List);
            var buffer3 = new QueryBuffer(schema1, QueryType.Aggregate);
            var buffer4 = new QueryBuffer(schema2, QueryType.Aggregate);

            var data = (50, new List<int>());
            var relation = store.From(data).Select("Item1", "Item2.Item");

            buffer1.Insert(relation, "Id", "Foo");
            buffer2.Insert(relation, "Item.Id", "Foo");
            buffer3.Insert(relation, "Id", "Foo");
            buffer4.Insert(relation, "Item.Id", "Foo");

            var result1 = buffer1.Commit<Blog>();
            var result2 = buffer2.Commit<List<Blog>>();
            var result3 = buffer3.Commit<Blog>();
            var result4 = buffer4.Commit<List<Blog>>();

            result1.ShouldBeNull();
            result3.ShouldBeNull();

            result2.ShouldNotBeNull();
            result2.Count.ShouldBe(0);

            result4.ShouldNotBeNull();
            result4.Count.ShouldBe(1);
            result4[0].ShouldNotBeNull();
            result4[0].Id.ShouldBe(50);
        }

        public void Test_Insert_Invalid_Constructor()
        {
            throw new NotImplementedException();
        }

        public void Test_Insert_Invalid_ParentKey()
        {
            throw new NotImplementedException();
        }
        public void Test_Insert_Invalid_ChildKey()
        {
            throw new NotImplementedException();
        }

        public void Test_Insert_CaseInsensitive()
        {
            var store = DatabaseHelper.Default.Store;
            var schema = store.GetSchema(typeof(IList<Blog>));
            var buffer = new QueryBuffer(schema, QueryType.List);

            buffer.Insert(50, ("", "ITEM.id"));

            var result = buffer.Commit<IList<Blog>>();

            result.Count.ShouldBe(1);
            result[0].Id.ShouldBe(50);
        }

        public void Test_Insert_CaseSensitive()
        {
            var store = DatabaseHelper.Default.GetSchemas(useSqlite: false, new DotNotation(StringComparer.OrdinalIgnoreCase));
            var schema = store.GetSchema(typeof(IList<Blog>));
            var buffer = new QueryBuffer(schema, QueryType.List);

            buffer.Insert(50, ("", "ITEM.id"));

            var result = buffer.Commit<IList<Blog>>();

            result.ShouldBeNull();
        }

        public async Task Test_Insert_OneToMany_NonPrimary_Async()
        {
            var store = DatabaseHelper.Default.Store;

            var data1 = new (int?, string)[]
            {
                ( 1, "Blog 1" ),
                ( null, "Blog 2" ),
            };
            var data2 = new (int, int, string)[]
            {
                ( 1, 1, "Post 1.1" ),
                ( 2, 1, "Post 1.2" ),
                ( 3, 2, "Post 2.1" ),
            };

            var buffer = new QueryBuffer(store.Describe<IList<Blog>>(), QueryType.List);

            await buffer.InsertAsync(data1,
                ("Item.Item1", "Item.Id2"),
                ("Item.Item2", "Item.Title")
            );

            await buffer.InsertAsync(data2,
                ("Item.Item1", "Item.Posts.Item.Id"),
                ("Item.Item2", "Item.Posts.Item.BlogId2"),
                ("Item.Item3", "Item.Posts.Item.Headline")
            );

            var result = buffer.Commit<IList<Blog>>();

            result.Count.ShouldBe(2);

            result[0].Id2.ShouldBe(1);
            result[0].Title.ShouldBe("Blog 1");

            result[0].Posts.Count.ShouldBe(2);
            result[0].Posts[0].BlogId2.ShouldBe(result[0].Id2);
            result[0].Posts[0].Headline.ShouldBe("Post 1.1");
            result[0].Posts[1].BlogId2.ShouldBe(result[0].Id2);
            result[0].Posts[1].Headline.ShouldBe("Post 1.2");

            result[1].Id2.ShouldBe(0);
            result[1].Title.ShouldBe("Blog 2");

            result[1].Posts.ShouldBeNull();
        }

        public async Task Test_Insert_OneToMany_Async()
        {
            var store = DatabaseHelper.Default.Store;
            var data1 = new (int, string)[]
            {
                ( 1, "Blog 1" ),
                ( 2, "Blog 2" ),
            };
            var data2 = new (int, int, string)[]
            {
                ( 1, 1, "Post 1.1" ),
                ( 2, 1, "Post 1.2" ),
                ( 3, 2, "Post 2.1" ),
            };
            var data3 = new (int, int, string)[]
            {
                ( 1, 2, "Comment 1.2.1" ),
                ( 2, 2, "Comment 1.2.2" ),
                ( 3, 2, "Comment 1.2.3" ),
                ( 4, 3, "Comment 2.1.1" ),
            };

            var buffer = new QueryBuffer(store.Describe<IList<Blog>>(), QueryType.List);

            await buffer.InsertAsync(data1,
                ("Item.Item1", "Item.Id"),
                ("Item.Item2", "Item.Title")
            );

            await buffer.InsertAsync(data2,
                ("Item.Item1", "Item.Posts.Item.Id"),
                ("Item.Item2", "Item.Posts.Item.BlogId"),
                ("Item.Item3", "Item.Posts.Item.Headline")
            );

            await buffer.InsertAsync(data3,
                ("Item.Item1", "Item.Posts.Item.Comments.Item.Id"),
                ("Item.Item2", "Item.Posts.Item.Comments.Item.BlogPostId"),
                ("Item.Item3", "Item.Posts.Item.Comments.Item.Comment")
            );

            var result = buffer.Commit<IList<Blog>>();

            result.Count.ShouldBe(2);

            result[0].Id.ShouldBe(1);
            result[0].Title.ShouldBe("Blog 1");

            result[0].Posts.Count.ShouldBe(2);
            result[0].Posts[0].BlogId.ShouldBe(result[0].Id);
            result[0].Posts[0].Headline.ShouldBe("Post 1.1");
            result[0].Posts[1].BlogId.ShouldBe(result[0].Id);
            result[0].Posts[1].Headline.ShouldBe("Post 1.2");

            result[0].Posts[0].Comments.Count.ShouldBe(0);

            result[0].Posts[1].Comments.Count.ShouldBe(3);
            result[0].Posts[1].Comments[0].BlogPostId.ShouldBe(result[0].Posts[1].Id);
            result[0].Posts[1].Comments[0].Comment.ShouldBe("Comment 1.2.1");
            result[0].Posts[1].Comments[1].BlogPostId.ShouldBe(result[0].Posts[1].Id);
            result[0].Posts[1].Comments[1].Comment.ShouldBe("Comment 1.2.2");
            result[0].Posts[1].Comments[2].BlogPostId.ShouldBe(result[0].Posts[1].Id);
            result[0].Posts[1].Comments[2].Comment.ShouldBe("Comment 1.2.3");

            result[1].Posts.Count.ShouldBe(1);
            result[1].Posts[0].BlogId.ShouldBe(result[1].Id);
            result[1].Posts[0].Headline.ShouldBe("Post 2.1");

            result[1].Posts[0].Comments.Count.ShouldBe(1);
            result[1].Posts[0].Comments[0].BlogPostId.ShouldBe(result[1].Posts[0].Id);
            result[1].Posts[0].Comments[0].Comment.ShouldBe("Comment 2.1.1");
        }

        public void Test_Insert_OneToMany()
        {
            var store = DatabaseHelper.Default.Store;
            var data1 = new (int, string)[]
            {
                ( 1, "Blog 1" ),
                ( 2, "Blog 2" ),
            };
            var data2 = new (int, int, string)[]
            {
                ( 1, 1, "Post 1.1" ),
                ( 2, 1, "Post 1.2" ),
                ( 3, 2, "Post 2.1" ),
            };
            var data3 = new (int, int, string)[]
            {
                ( 1, 2, "Comment 1.2.1" ),
                ( 2, 2, "Comment 1.2.2" ),
                ( 3, 2, "Comment 1.2.3" ),
                ( 4, 3, "Comment 2.1.1" ),
            };

            var buffer = new QueryBuffer(store.Describe<IList<Blog>>(), QueryType.List);

            buffer.Insert(data1,
                ("Item.Item1", "Item.Id"),
                ("Item.Item2", "Item.Title")
            );

            buffer.Insert(data2,
                ("Item.Item1", "Item.Posts.Item.Id"),
                ("Item.Item2", "Item.Posts.Item.BlogId"),
                ("Item.Item3", "Item.Posts.Item.Headline")
            );

            buffer.Insert(data3,
                ("Item.Item1", "Item.Posts.Item.Comments.Item.Id"),
                ("Item.Item2", "Item.Posts.Item.Comments.Item.BlogPostId"),
                ("Item.Item3", "Item.Posts.Item.Comments.Item.Comment")
            );

            var result = buffer.Commit<IList<Blog>>();

            result.Count.ShouldBe(2);

            result[0].Id.ShouldBe(1);
            result[0].Title.ShouldBe("Blog 1");

            result[0].Posts.Count.ShouldBe(2);
            result[0].Posts[0].BlogId.ShouldBe(result[0].Id);
            result[0].Posts[0].Headline.ShouldBe("Post 1.1");
            result[0].Posts[1].BlogId.ShouldBe(result[0].Id);
            result[0].Posts[1].Headline.ShouldBe("Post 1.2");

            result[0].Posts[0].Comments.Count.ShouldBe(0);

            result[0].Posts[1].Comments.Count.ShouldBe(3);
            result[0].Posts[1].Comments[0].BlogPostId.ShouldBe(result[0].Posts[1].Id);
            result[0].Posts[1].Comments[0].Comment.ShouldBe("Comment 1.2.1");
            result[0].Posts[1].Comments[1].BlogPostId.ShouldBe(result[0].Posts[1].Id);
            result[0].Posts[1].Comments[1].Comment.ShouldBe("Comment 1.2.2");
            result[0].Posts[1].Comments[2].BlogPostId.ShouldBe(result[0].Posts[1].Id);
            result[0].Posts[1].Comments[2].Comment.ShouldBe("Comment 1.2.3");

            result[1].Posts.Count.ShouldBe(1);
            result[1].Posts[0].BlogId.ShouldBe(result[1].Id);
            result[1].Posts[0].Headline.ShouldBe("Post 2.1");

            result[1].Posts[0].Comments.Count.ShouldBe(1);
            result[1].Posts[0].Comments[0].BlogPostId.ShouldBe(result[1].Posts[0].Id);
            result[1].Posts[0].Comments[0].Comment.ShouldBe("Comment 2.1.1");
        }

        public void Test_Insert_NonPrimaryKeys()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new (int?, string)[]
            {
                ( null, "Blog 1" ),
                ( 10,   "Blog 2" )
            };

            var buffer = new QueryBuffer(store.Describe<IList<Blog>>(), QueryType.List);

            buffer.Insert(data,
                ("Item.Item1", "Item.Id2"),
                ("Item.Item2", "Item.Title")
            );

            var result = buffer.Commit<IList<Blog>>();

            result.Count.ShouldBe(2);
            result[0].Id2.ShouldBe(0);
            result[0].Title.ShouldBe("Blog 1");
            result[1].Id2.ShouldBe(10);
            result[1].Title.ShouldBe("Blog 2");
        }

        public async Task Test_Insert_NonPrimaryKeys_Async()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new (int?, string)[]
            {
                ( null, "Blog 1" ),
                ( 10,   "Blog 2" )
            };

            var buffer = new QueryBuffer(store.Describe<IList<Blog>>(), QueryType.List);

            await buffer.InsertAsync(data,
                ("Item.Item1", "Item.Id2"),
                ("Item.Item2", "Item.Title")
            );

            var result = buffer.Commit<IList<Blog>>();

            result.Count.ShouldBe(2);
            result[0].Id2.ShouldBe(0);
            result[0].Title.ShouldBe("Blog 1");
            result[1].Id2.ShouldBe(10);
            result[1].Title.ShouldBe("Blog 2");
        }

        public void Test_Insert_PrimaryKeys()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new (int?, string)[]
            {
                ( null, "Blog 1" ),
                ( 10,   "Blog 2" )
            };

            var buffer = new QueryBuffer(store.Describe<IList<Blog>>(), QueryType.List);

            buffer.Insert(data,
                ("Item.Item1", "Item.Id"),
                ("Item.Item2", "Item.Title")
            );

            var result = buffer.Commit<IList<Blog>>();

            result.Count.ShouldBe(1);
            result[0].Id.ShouldBe(10);
            result[0].Title.ShouldBe("Blog 2");
        }

        public async Task Test_Insert_PrimaryKeys_Async()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new (int?, string)[]
            {
                ( null, "Blog 1" ),
                ( 10,   "Blog 2" )
            };

            var buffer = new QueryBuffer(store.Describe<IList<Blog>>(), QueryType.List);

            await buffer.InsertAsync(data,
                ("Item.Item1", "Item.Id"),
                ("Item.Item2", "Item.Title")
            );

            var result = buffer.Commit<IList<Blog>>();

            result.Count.ShouldBe(1);
            result[0].Id.ShouldBe(10);
            result[0].Title.ShouldBe("Blog 2");
        }

        public void Test_Insert_DynamicResult()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new int?[] { 1, 2, null };

            var schema1 = store.GetSchema(typeof(object));
            var schema2 = store.GetSchema(typeof(IList<object>));
            var buffer1 = new QueryBuffer(schema1, QueryType.List);
            var buffer2 = new QueryBuffer(schema2, QueryType.List);

            buffer1.Insert(data,
                ("Item", "")
            );

            buffer2.Insert(data,
                ("Item", "Item")
            );

            var result1 = buffer1.Commit<dynamic>();
            var result2 = buffer2.Commit<IList<dynamic>>();

            ((object)result1).ShouldBeNull();
            result2.Select(d => (int?)d).ShouldBe(new int?[] { 1, 2, null });
        }

        public void Test_Aggregate_InvalidDataType()
        {
            var store = DatabaseHelper.Default.Store;
            var schema = store.GetSchema(typeof(Blog));
            var buffer = new QueryBuffer(schema, QueryType.Aggregate);

            //Should.Throw<BindingException>(() => buffer.Insert("Text", ("", "Id"))); // compile time
            Should.Throw<BindingException>(() => buffer.Insert((object)12, ("", "Title"))); // runtime
        }

        public void Test_Insert_InvalidDataType()
        {
            var store = DatabaseHelper.Default.Store;
            var schema = store.GetSchema(typeof(Blog));
            var buffer = new QueryBuffer(schema, QueryType.List);

            Should.Throw<BindingException>(() => buffer.Insert("Text", ("", "Id"))); // compile time
            Should.Throw<BindingException>(() => buffer.Insert((object)12, ("", "Title"))); // runtime
        }

        public void Test_Insert_ThrowingProperty()
        {
            var store = DatabaseHelper.Default.Store;
            var schema = store.GetSchema(typeof(Blog));
            var buffer = new QueryBuffer(schema, QueryType.List);

            Should.Throw<BindingException>(() => buffer.Insert(100, ("", "GetOnly")));
        }

        public void Test_Aggregate_Dynamic()
        {
            var store = DatabaseHelper.Default.Store;
            var data = (3, "L3");

            var schema = store.GetSchema(typeof(object));
            var buffer = new QueryBuffer(schema, QueryType.Aggregate);

            buffer.Insert(data, ("Item1", "Id"));
            buffer.Insert(data, ("Item2", "Text.String"));

            var result = buffer.Commit<dynamic>();

            DynamicShould.HaveProperty(result, "Id");
            DynamicShould.HaveProperty(result, "Text");
            DynamicShould.HaveProperty(result.Text, "String");

            int id = Should.NotThrow(() => (int)result.Id);
            string text = Should.NotThrow(() => (string)result.Text.String);

            id.ShouldBe(3);
            text.ShouldBe("L3");
        }
        public void Test_Insert_DynamicGraph()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new (int, string)[]
            {
                (1, "L1"),
                (2, "L2"),
            };

            var schema = store.GetSchema(typeof(IList<object>));
            var buffer = new QueryBuffer(schema, QueryType.List);

            buffer.Insert(data,
                ("Item.Item1", "Item.Id"),
                ("Item.Item2", "Item.Text.String")
            );

            var result = buffer.Commit<IList<dynamic>>();

            result.ShouldNotBeNull();
            result.Count.ShouldBe(2);

            DynamicShould.HaveProperty(result[0], "Id");
            DynamicShould.HaveProperty(result[0], "Text");
            DynamicShould.HaveProperty(result[0].Text, "String");

            DynamicShould.HaveProperty(result[1], "Id");
            DynamicShould.HaveProperty(result[1], "Text");
            DynamicShould.HaveProperty(result[1].Text, "String");

            int id0 = Should.NotThrow(() => (int)result[0].Id);
            string text0 = Should.NotThrow(() => (string)result[0].Text.String);
            int id1 = Should.NotThrow(() => (int)result[1].Id);
            string text1 = Should.NotThrow(() => (string)result[1].Text.String);


            id0.ShouldBe(1);
            text0.ShouldBe("L1");
            id1.ShouldBe(2);
            text1.ShouldBe("L2");
        }
    }
}
