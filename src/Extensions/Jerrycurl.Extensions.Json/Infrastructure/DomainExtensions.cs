using System.Text.Json;
using Jerrycurl.Extensions.Json.Metadata;

namespace Jerrycurl.Mvc
{
    public static class DomainExtensions
    {
        public static void UseJson(this DomainOptions options) => options.UseJson(null);

        public static void UseJson(this DomainOptions options, JsonSerializerOptions serializerOptions)
        {
            serializerOptions ??= new JsonSerializerOptions();

            options.Schemas.Apply(new JsonBindingContractResolver(serializerOptions));
            options.Schemas.Apply(new JsonContractResolver(serializerOptions));
        }
    }
}
