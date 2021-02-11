using System.Collections.Generic;
using Jerrycurl.Mvc;
using Jerrycurl.Test.Project.Models;

namespace Jerrycurl.Test.Project.Accessors
{
    public class Runner : Accessor
    {
        private IList<TResult> QueryInternal<TModel, TResult>(Runnable<TModel, TResult> runner) => this.Query<TResult>(runner, queryName: "Query");
        private void CommandInternal<TModel, TResult>(Runnable<TModel, TResult> runner) => this.Execute(runner, commandName: "Command");

        public static IList<TResult> Query<TModel, TResult>(Runnable<TModel, TResult> runner) => new Runner().QueryInternal(runner);
        public static void Command<TModel, TResult>(Runnable<TModel, TResult> runner) => new Runner().CommandInternal(runner);

        public string Sql<TModel, TResult>(Runnable<TModel, TResult> runner)
        {
            IProcLocator locator = this.Context?.Locator ?? new ProcLocator();
            IProcEngine engine = this.Context?.Engine ?? new ProcEngine(null);

            PageDescriptor descriptor = locator.FindPage("Query", this.GetType());
            ProcArgs args = new ProcArgs(typeof(TModel), typeof(TResult));
            ProcFactory factory = engine.Proc(descriptor, args);

            return factory(runner).Buffer.ReadToEnd().Text.Trim();
        }
    }
}
