using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jerrycurl.Relations;
using Jerrycurl.Data.Commands;
using Shouldly;
using Jerrycurl.Data.Test.Models;
using Jerrycurl.Test;
using Jerrycurl.Data.Sessions;
using Jerrycurl.Relations.Metadata;
using Jerrycurl.Relations.Language;
using Jerrycurl.Data.V11.Language;
using Jerrycurl.Data.Test.Model;
using Jerrycurl.Data.Queries;

namespace Jerrycurl.Data.Test
{
    public class Query2Tests
    {
        public async Task Test_Insert_OneToMany_Async()
        {
            var store = DatabaseHelper.Default.Schemas;
            var data1 = new (int?, string)[]
            {
                ( 1, "Blog 1" ),
                ( 2, "Blog 2" )
            };
            var data2 = new (int?, string)[]
            {
                ( 1, "Blog post 1.1" ),
                ( 1, "Blog post 1.2" ),
                ( 2, "Blog post 2.1" )
            };

            var buffer = new ListBuffer<Blog>(store);

            await buffer.InsertAsync(data1,
                ("Item.Item1", "Item.Id"),
                ("Item.Item2", "Item.Title")
            );

            await buffer.InsertAsync(data2,
                ("Item.Item1", "Item.Posts.Item.BlogId"),
                ("Item.Item2", "Item.Posts.Item.Headline")
            );

            var result = buffer.Commit();

            result.Count.ShouldBe(2);

            result[0].Id.ShouldBe(1);
            result[0].Title.ShouldBe("Blog 1");

            result[0].Posts.Count.ShouldBe(2);
            result[0].Posts[0].BlogId.ShouldBe(result[0].Id);
            result[0].Posts[0].Headline.ShouldBe("Blog post 1.1");
            result[0].Posts[1].BlogId.ShouldBe(result[0].Id);
            result[0].Posts[1].Headline.ShouldBe("Blog post 1.2");

            result[1].Posts.Count.ShouldBe(1);
            result[1].Posts[0].BlogId.ShouldBe(result[1].Id);
            result[1].Posts[0].Headline.ShouldBe("Blog post 1.2");
        }

        public void Test_Insert_OneToMany()
        {
            var store = DatabaseHelper.Default.Schemas;
            var data1 = new (int?, string)[]
            {
                ( 1, "Blog 1" ),
                ( 2, "Blog 2" )
            };
            var data2 = new (int?, string)[]
            {
                ( 1, "Blog post 1.1" ),
                ( 1, "Blog post 1.2" ),
                ( 2, "Blog post 2.1" )
            };

            var buffer = new ListBuffer<Blog>(store);

            buffer.Insert(data1,
                ("Item.Item1", "Item.Id"),
                ("Item.Item2", "Item.Title")
            );

            buffer.Insert(data2,
                ("Item.Item1", "Item.Posts.Item.BlogId"),
                ("Item.Item2", "Item.Posts.Item.Headline")
            );

            var result = buffer.Commit();

            result.Count.ShouldBe(2);

            result[0].Id.ShouldBe(1);
            result[0].Title.ShouldBe("Blog 1");

            result[0].Posts.Count.ShouldBe(2);
            result[0].Posts[0].BlogId.ShouldBe(result[0].Id);
            result[0].Posts[0].Headline.ShouldBe("Blog post 1.1");
            result[0].Posts[1].BlogId.ShouldBe(result[0].Id);
            result[0].Posts[1].Headline.ShouldBe("Blog post 1.2");

            result[1].Posts.Count.ShouldBe(1);
            result[1].Posts[0].BlogId.ShouldBe(result[1].Id);
            result[1].Posts[0].Headline.ShouldBe("Blog post 1.2");
        }

        public void Test_Insert_PrimaryKeys()
        {
            var store = DatabaseHelper.Default.Schemas;
            var data = new (int?, string)[]
            {
                ( null, "Blog 1" ),
                ( 10,   "Blog 2" )
            };

            var buffer = new ListBuffer<Blog>(store);

            buffer.Insert(data,
                ("Item.Item1", "Item.Id"),
                ("Item.Item2", "Item.Title")
            );

            var result = buffer.Commit();

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

            var buffer = new ListBuffer<Blog>(store);

            await buffer.InsertAsync(data,
                ("Item.Item1", "Item.Id"),
                ("Item.Item2", "Item.Title")
            );

            var result = buffer.Commit();

            result.Count.ShouldBe(1);
            result[0].Id.ShouldBe(10);
            result[0].Title.ShouldBe("Blog 2");
        }
    }
}
