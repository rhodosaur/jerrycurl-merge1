using Jerrycurl.Data.Metadata;
using Jerrycurl.Extensions.EntityFrameworkCore.Metadata;
using Jerrycurl.Relations.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Jerrycurl.Mvc
{
    public static class DomainExtensions
    {
        public static void UseEntityFrameworkCore<TContext>(this DomainOptions options)
            where TContext : DbContext, new()
        {
            using TContext dbContext = new TContext();

            options.UseEntityFrameworkCore(dbContext);
        }

        public static void UseEntityFrameworkCore(this DomainOptions options, DbContext dbContext)
        {
            EntityFrameworkCoreContractResolver resolver = new EntityFrameworkCoreContractResolver(dbContext);

            options.Use((IRelationContractResolver)resolver);
            options.Use((ITableContractResolver)resolver);
        }
    }
}