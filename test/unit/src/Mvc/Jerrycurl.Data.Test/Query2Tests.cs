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

namespace Jerrycurl.Data.Test
{
    public class Query2Tests
    {
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
    }
}
