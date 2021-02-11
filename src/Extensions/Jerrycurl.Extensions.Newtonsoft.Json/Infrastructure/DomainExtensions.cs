using Jerrycurl.Extensions.Newtonsoft.Json.Metadata;
using Newtonsoft.Json;

namespace Jerrycurl.Mvc
{
    public static class DomainExtensions
    {
        public static void UseNewtonsoftJson(this DomainOptions options) => options.UseNewtonsoftJson(null);

        public static void UseNewtonsoftJson(this DomainOptions options, JsonSerializerSettings settings)
        {
            settings ??= JsonConvert.DefaultSettings?.Invoke() ?? new JsonSerializerSettings();

            options.Schemas.Apply(new NewtonsoftJsonBindingContractResolver(settings));
            options.Schemas.Apply(new NewtonsoftJsonContractResolver(settings));
        }
    }
}
