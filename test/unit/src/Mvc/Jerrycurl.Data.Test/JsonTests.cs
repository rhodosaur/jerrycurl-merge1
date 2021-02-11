using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Jerrycurl.Data.Queries;
using Jerrycurl.Data.Test.Models;
using Jerrycurl.Test;
using Jerrycurl.Data.Sessions;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Test.Model;
using Jerrycurl.Data.Language;
using Jerrycurl.Mvc;
using System.Text.Json;
using Jerrycurl.Relations.Language;
using Microsoft.Data.Sqlite;
using Jerrycurl.Extensions.Json.Metadata;

namespace Jerrycurl.Data.Test
{
    public class JsonTests
    {
        public void Test_Insert_Json()
        {
            var options = new JsonSerializerOptions();
            var store = DatabaseHelper.Default.GetSchemas(useSqlite: false);
            store.AddContract(new JsonBindingContractResolver(options));

            var data = "{ \"Id\": 12, \"Title\": \"Hello World!\" }";
            var schema = store.GetSchema(typeof(JsonBlog));
            var buffer = new QueryBuffer(schema, QueryType.List);

            buffer.Insert(data,
                ("", "Blog")
            );

            var result = buffer.Commit<JsonBlog>();

            result.ShouldNotBeNull();
            result.Blog.ShouldNotBeNull();
            result.Blog.Id.ShouldBe(12);
            result.Blog.Title.ShouldBe("Hello World!");
        }

        public void Test_Insert_Json_NoContract()
        {
            var store = DatabaseHelper.Default.Schemas2;
            var data = "{ \"Id\": 12 }";

            var schema = store.GetSchema(typeof(JsonBlog));
            var buffer = new QueryBuffer(schema, QueryType.List);

            Should.Throw<BindingException>(() =>
            {
                buffer.Insert(data,
                    ("", "Blog")
                );
            });
        }

        public void Test_Select_Json_Parameter()
        {
            var options = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IgnoreNullValues = true,
            };
            var store = DatabaseHelper.Default.GetSchemas(useSqlite: false);

            store.AddContract(new JsonBindingContractResolver(options));

            var data = new JsonBlog() { Blog = new Blog() { Id = 12 } };
            var model = store.From(data).Lookup("Blog");
            var parameter = new Parameter("P0", model);
            var sqlParameter = new SqliteParameter();
            var expected = JsonSerializer.Serialize(data.Blog, options);

            parameter.Build(sqlParameter);

            sqlParameter.Value.ShouldBe(expected);
        }
    }
}
