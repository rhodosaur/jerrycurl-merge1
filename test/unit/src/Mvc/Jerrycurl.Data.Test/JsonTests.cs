using Shouldly;
using Jerrycurl.Data.Queries;
using Jerrycurl.Test;
using Jerrycurl.Data.Sessions;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Language;
using Jerrycurl.Mvc;
using System.Text.Json;
using Jerrycurl.Relations.Language;
using Microsoft.Data.Sqlite;
using Jerrycurl.Extensions.Json.Metadata;
using Jerrycurl.Data.Commands;
using Jerrycurl.Data.Test.Model.Blogging;

namespace Jerrycurl.Data.Test
{
    public class JsonTests
    {
        public void Test_Update_Json()
        {
            var store = DatabaseHelper.Default.GetStore();

            store.Use(new JsonBindingContractResolver(new JsonSerializerOptions()));

            var json = "{ \"Id\": 12, \"Title\": \"Hello World!\" }";
            var data1 = new JsonBlog();
            var data2 = new JsonBlog();
            var target1 = store.From(data1).Lookup("Blog");
            var target2 = store.From(data2).Lookup("Blog");
            var buffer = new CommandBuffer(store);

            buffer.Add(new ColumnBinding(target1, "B0"));
            buffer.Add(new ParameterBinding(target2, "P0"));

            var parameters = buffer.Prepare(() => new MockParameter());

            parameters[0].Value = json;

            buffer.Update(json, ("", "B0"));

            data1.Blog.ShouldBeNull();
            data2.Blog.ShouldBeNull();

            buffer.Commit();

            data1.Blog.ShouldNotBeNull();
            data1.Blog.Id.ShouldBe(12);
            data1.Blog.Title.ShouldBe("Hello World!");

            data2.Blog.ShouldNotBeNull();
            data2.Blog.Id.ShouldBe(12);
            data2.Blog.Title.ShouldBe("Hello World!");
        }

        public void Test_Insert_Json()
        {
            var options = new JsonSerializerOptions();
            var store = DatabaseHelper.Default.GetStore();
            store.Use(new JsonBindingContractResolver(options));

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
            var store = DatabaseHelper.Default.Store;
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
            var store = DatabaseHelper.Default.GetStore();

            store.Use(new JsonBindingContractResolver(options));

            var data = new JsonBlog() { Blog = new Blog() { Id = 12 } };
            var model = store.From(data).Lookup("Blog");
            var parameter = new Parameter("P0", model);
            var sqlParameter = new SqliteParameter();
            var expected = JsonSerializer.Serialize(data.Blog, options);

            parameter.Build(sqlParameter);

            sqlParameter.Value.ShouldBe(expected);
        }

        public void Test_Select_JsonDocument_Parameter()
        {
            var options = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IgnoreNullValues = true,
            };
            var store = DatabaseHelper.Default.GetStore();

            store.Use(new JsonBindingContractResolver(options));

            var json = "{\"Hello\":\"World!\"}";
            var data1 = JsonDocument.Parse(json);
            var data2 = data1.RootElement;
            var parameter1 = new Parameter("P0", store.From(data1));
            var parameter2 = new Parameter("P1", store.From(data2));
            var sqlParameter1 = new SqliteParameter();
            var sqlParameter2 = new SqliteParameter();

            parameter1.Build(sqlParameter1);
            parameter2.Build(sqlParameter2);

            sqlParameter1.Value.ShouldBe(json);
            sqlParameter2.Value.ShouldBe(json);
        }

        public void Test_Insert_JsonElement()
        {
            var options = new JsonSerializerOptions();
            var store = DatabaseHelper.Default.GetStore();
            store.Use(new JsonBindingContractResolver(options));

            var data = "{ \"Id\": 12, \"Title\": \"Hello World!\" }";
            var schema = store.GetSchema(typeof(JsonElement));
            var buffer = new QueryBuffer(schema, QueryType.List);

            buffer.Insert(data,
                ("", "")
            );

            var result = buffer.Commit<JsonElement>();

            Should.NotThrow(() =>
            {
                var id = result.GetProperty("Id");
                var title = result.GetProperty("Title");

                id.GetInt32().ShouldBe(12);
                title.GetString().ShouldBe("Hello World!");
            });
        }

        public void Test_Insert_JsonDocument()
        {
            var options = new JsonSerializerOptions();
            var store = DatabaseHelper.Default.GetStore();
            store.Use(new JsonBindingContractResolver(options));

            var data = "{ \"Id\": 12, \"Title\": \"Hello World!\" }";
            var schema = store.GetSchema(typeof(JsonDocument));
            var buffer = new QueryBuffer(schema, QueryType.List);

            buffer.Insert(data,
                ("", "")
            );

            var result = buffer.Commit<JsonDocument>();

            result.ShouldNotBeNull();

            Should.NotThrow(() =>
            {
                var id = result.RootElement.GetProperty("Id");
                var title = result.RootElement.GetProperty("Title");

                id.GetInt32().ShouldBe(12);
                title.GetString().ShouldBe("Hello World!");
            });
        }
    }
}
