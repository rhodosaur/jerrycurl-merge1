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

namespace Jerrycurl.Data.Test
{
    public class Query2Tests
    {
        public void Test_Read_IntegerSet()
        {
            var store = DatabaseHelper.Default.Schemas2;
            var data = new[] { 1, 2, 3, 4, 5, 6 };

            using var dataReader = store.From(data).Select("Item").As("Item");

            var reader = new QueryReader(store, dataReader);
            var result = reader.Read<int>();

            result.ShouldBe(new[] { 1, 2, 3, 4, 5, 6 });
        }

        public void Test_Insert_ManyToOne_List()
        {
            var store = DatabaseHelper.Default.Schemas2;

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
            var store = DatabaseHelper.Default.Schemas2;

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

        }

        public void Test_Insert_OneToMany_NonPrimary()
        {
            var store = DatabaseHelper.Default.Schemas2;

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

        public void Test_Insert_CaseInsensitive()
        {
            var store = DatabaseHelper.Default.Schemas2;
            var schema = store.GetSchema(typeof(IList<Blog>));
            var buffer = new QueryBuffer(schema, QueryType.List);

            buffer.Insert(50, ("", "ITEM.id"));

            var result = buffer.Commit<IList<Blog>>();

            result.Count.ShouldBe(1);
            result[0].Id.ShouldBe(50);
        }

        public async Task Test_Insert_OneToMany_NonPrimary_Async()
        {
            var store = DatabaseHelper.Default.Schemas2;

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
            var store = DatabaseHelper.Default.Schemas2;
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
            var store = DatabaseHelper.Default.Schemas2;
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
            var store = DatabaseHelper.Default.Schemas;
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
            var store = DatabaseHelper.Default.Schemas;
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
            var store = DatabaseHelper.Default.Schemas;
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
            var store = DatabaseHelper.Default.Schemas;
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
            var store = DatabaseHelper.Default.Schemas;
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

        public void Test_Insert_InvalidDataType()
        {
            var store = DatabaseHelper.Default.Schemas;
            var schema = store.GetSchema(typeof(Blog));
            var buffer = new QueryBuffer(schema, QueryType.List);

            Should.Throw<BindingException>(() => buffer.Insert("Text", ("", "Id")));
        }

        public void Test_Insert_ThrowingProperty()
        {
            var store = DatabaseHelper.Default.Schemas;
            var schema = store.GetSchema(typeof(Blog));
            var buffer = new QueryBuffer(schema, QueryType.List);

            Should.Throw<BindingException>(() => buffer.Insert(100, ("", "GetOnly")));
        }

        public void Test_Insert_DynamicAggregate()
        {
            var store = DatabaseHelper.Default.Schemas;
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
            var store = DatabaseHelper.Default.Schemas;
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
