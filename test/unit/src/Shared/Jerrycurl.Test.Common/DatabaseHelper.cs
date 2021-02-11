using Jerrycurl.Data.Commands;
using Jerrycurl.Data.Filters;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries;
using Jerrycurl.Relations.Language;
using Jerrycurl.Relations.Metadata;
using Jerrycurl.Test.Profiling;
using Jerrycurl.Vendors.Sqlite.Metadata;
using Microsoft.Data.Sqlite;
using Jerrycurl.Data.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jerrycurl.Test
{
    public class DatabaseHelper
    {
        public const string TestDbConnectionString = "DATA SOURCE=testdb.db";

        public static DatabaseHelper Default { get; } = new DatabaseHelper();

        public SchemaStore Store { get; set; }
        public SchemaStore SqliteStore { get; set; }
        public QueryOptions QueryOptions { get; set; }
        public CommandOptions CommandOptions { get; set; }

        public DatabaseHelper()
        {
            this.Store = this.GetSchemas(useSqlite: false);
            this.SqliteStore = this.GetSchemas(useSqlite: true);
            this.QueryOptions = this.GetQueryOptions();
            this.CommandOptions = this.GetCommandOptions();
        }

        public SchemaStore GetSchemas(bool useSqlite = true, DotNotation notation = null, IEnumerable<object> contracts = null)
        {
            RelationMetadataBuilder relationBuilder = new RelationMetadataBuilder();
            BindingMetadataBuilder bindingBuilder = new BindingMetadataBuilder();
            ReferenceMetadataBuilder referenceBuilder = new ReferenceMetadataBuilder();
            TableMetadataBuilder tableBuilder = new TableMetadataBuilder();

            SchemaStore store = new SchemaStore(notation ?? new DotNotation(), relationBuilder, bindingBuilder, referenceBuilder, tableBuilder);

            if (useSqlite)
                bindingBuilder.Add(new SqliteContractResolver());

            if (contracts != null)
            {
                foreach (var contract in contracts)
                {
                    if (contract is IRelationContractResolver relationResolver)
                        relationBuilder.Add(relationResolver);

                    if (contract is IBindingContractResolver bindingResolver)
                        bindingBuilder.Add(bindingResolver);

                    if (contract is ITableContractResolver tableResolver)
                        tableBuilder.Add(tableResolver);
                }
            }

            return store;
        }

        public QueryOptions GetQueryOptions(SchemaStore store = null)
        {
            return new QueryOptions()
            {
                ConnectionFactory = () => new ProfilingConnection(new SqliteConnection(TestDbConnectionString)),
                Store = store ?? this.SqliteStore,
            };
        }

        public CommandOptions GetCommandOptions(params IFilter[] filters)
        {
            return new CommandOptions()
            {
                ConnectionFactory = () => new ProfilingConnection(new SqliteConnection(TestDbConnectionString)),
                Filters = filters ?? Array.Empty<IFilter>(),
            };
        }

        public QueryEngine Queries => new QueryEngine(this.QueryOptions);
        public CommandEngine Commands => new CommandEngine(this.CommandOptions);

        public async Task ExecuteAsync(params Command[] commands) => await this.Commands.ExecuteAsync(commands);
        public void Execute(params Command[] commands) => this.Commands.Execute(commands);

        public async Task<IList<TItem>> QueryAsync<TItem>(params SqliteTable[] tables) => await this.Queries.ListAsync<TItem>(tables.Select(t => t.ToQuery()));
        public async Task<IList<TItem>> QueryAsync<TItem>(params Query[] queries) => await this.Queries.ListAsync<TItem>(queries);
        public async Task<IList<TItem>> QueryAsync<TItem>(string sql) => await this.Queries.ListAsync<TItem>(new Query() { QueryText = sql });

        public IList<TItem> Query<TItem>(params SqliteTable[] tables) => this.Queries.List<TItem>(tables.Select(t => t.ToQuery()));
        public IList<TItem> Query<TItem>(params Query[] queries) => this.Queries.List<TItem>(queries);
        public IList<TItem> Query<TItem>(string sql) => this.Queries.List<TItem>(new Query() { QueryText = sql });

        public TItem Aggregate<TItem>(params SqliteTable[] tables) => this.Queries.Aggregate<IList<TItem>>(tables.Select(t => t.ToQuery())).FirstOrDefault();
        public TItem Aggregate<TItem>(params Query[] queries) => this.Queries.Aggregate<IList<TItem>>(queries).FirstOrDefault();
        public TItem Aggregate<TItem>(string sql) => this.Queries.Aggregate<IList<TItem>>(new Query() { QueryText = sql }).FirstOrDefault();

        public async Task<TItem> AggregateAsync<TItem>(params SqliteTable[] tables) => (await this.Queries.AggregateAsync<IList<TItem>>(tables.Select(t => t.ToQuery()))).FirstOrDefault();
        public async Task<TItem> AggregateAsync<TItem>(params Query[] queries) => (await this.Queries.AggregateAsync<IList<TItem>>(queries)).FirstOrDefault();
        public async Task<TItem> AggregateAsync<TItem>(string sql) => (await this.Queries.AggregateAsync<IList<TItem>>(new Query() { QueryText = sql })).FirstOrDefault();

        public IEnumerable<TItem> Enumerate<TItem>(params SqliteTable[] tables) => this.Queries.Enumerate<TItem>(tables.Select(t => t.ToQuery()));
        public IEnumerable<TItem> Enumerate<TItem>(params Query[] queries) => this.Queries.Enumerate<TItem>(queries);
        public IAsyncEnumerable<TItem> EnumerateAsync<TItem>(params SqliteTable[] tables) => this.Queries.EnumerateAsync<TItem>(tables.Select(t => t.ToQuery()));
        public IAsyncEnumerable<TItem> EnumerateAsync<TItem>(params Query[] queries) => this.Queries.EnumerateAsync<TItem>(queries);
        public IAsyncEnumerable<TItem> EnumerateAsync<TItem>(string sql) => this.Queries.EnumerateAsync<TItem>(new Query() { QueryText = sql });
    }
}
