using Jerrycurl.Extensions.EntityFrameworkCore.Metadata;
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
            => options.Schemas.AddContract(new EntityFrameworkCoreContractResolver(dbContext));
    }
}