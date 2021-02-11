using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jerrycurl.Relations;
using Jerrycurl.Data.Commands;
using Jerrycurl.Data.Language;
using Shouldly;
using Jerrycurl.Data.Test.Models;
using Jerrycurl.Test;
using Jerrycurl.Data.Sessions;
using Jerrycurl.Data.Test.Model;
using Jerrycurl.Relations.Language;

namespace Jerrycurl.Data.Test
{
    public class Command2Tests
    {
        public void Test_Update_FromParameter()
        {
            var store = DatabaseHelper.Default.Schemas;
            var data = new Blog();
            var targets = store.From(data).Lookup("Id", "Title");

            var buffer = new CommandBuffer();

            buffer.Add(new Parameter("P0", targets[0]), targets[0]);
            buffer.Add(new ParameterBinding(targets[1], "P1"));

            var parameters = buffer.Prepare(() => new MockParameter());

            parameters[0].Value = 12;
            parameters[1].Value = "Blog!";

            data.Id.ShouldBe(default);
            data.Title.ShouldBe(default);

            buffer.Commit();

            data.Id.ShouldBe(12);
            data.Title.ShouldBe("Blog!");
        }

        public void Test_Update_FromColumn()
        {
            var store = DatabaseHelper.Default.Schemas;
            var sourceData = new Blog() { Id = 12, Title = "Blog!" };
            var targetData = new Blog();

            var targets = store.From(targetData).Lookup("Id", "Title");
            using var source = store.From(sourceData)
                                    .Select("Id", "Title")
                                    .As("C1", "C2");

            var buffer = new CommandBuffer();

            buffer.Add(new ColumnBinding(targets[0], "C1"));
            buffer.Add(new ColumnBinding(targets[1], "C2"));

            buffer.Update(source);

            targetData.Id.ShouldBe(default);
            targetData.Title.ShouldBe(default);

            buffer.Commit();

            targetData.Id.ShouldBe(12);
            targetData.Title.ShouldBe("Blog!");
        }

        public async Task Test_Update_FromColumn_Async()
        {
            var store = DatabaseHelper.Default.Schemas;
            var sourceData = new Blog() { Id = 12, Title = "Blog!" };
            var targetData = new Blog();

            var targets = store.From(targetData).Lookup("Id", "Title");
            using var source = store.From(sourceData)
                                    .Select("Id", "Title")
                                    .As("C1", "C2");

            var buffer = new CommandBuffer();

            buffer.Add(new ColumnBinding(targets[0], "C1"));
            buffer.Add(new ColumnBinding(targets[1], "C2"));

            await buffer.UpdateAsync(source);

            targetData.Id.ShouldBe(default);
            targetData.Title.ShouldBe(default);

            buffer.Commit();

            targetData.Id.ShouldBe(12);
            targetData.Title.ShouldBe("Blog!");
        }

        public void Test_Update_FromCascadeCyclic_Throws()
        {
            var store = DatabaseHelper.Default.Schemas;
            var target1 = store.From(new Blog()).Lookup("Id");
            var target2 = store.From(new Blog()).Lookup("Id");
            var target3 = store.From(new Blog()).Lookup("Id");

            var buffer = new CommandBuffer();

            buffer.Add(new CascadeBinding(target1, target2));
            buffer.Add(new CascadeBinding(target2, target3));
            buffer.Add(new CascadeBinding(target3, target1));

            Should.Throw<BindingException>(() =>
            {
                buffer.Commit();
            });
        }

        public void Test_Update_FromCascadingParameter()
        {
            var store = DatabaseHelper.Default.Schemas;
            var data1 = new Blog();
            var data2 = new Blog();
            var data3 = new Blog();
            var target1 = store.From(data1).Lookup("Id");
            var target2 = store.From(data2).Lookup("Id");
            var target3 = store.From(data3).Lookup("Id");

            var buffer = new CommandBuffer();

            buffer.Add(new ParameterBinding(target1, "P0"));
            buffer.Add(new CascadeBinding(target2, target1));
            buffer.Add(new CascadeBinding(target3, target2));

            var parameters = buffer.Prepare(() => new MockParameter());

            parameters[0].Value = 11;

            buffer.Commit();

            data1.Id.ShouldBe(11);
            data2.Id.ShouldBe(11);
            data3.Id.ShouldBe(11);
        }
    }
}
