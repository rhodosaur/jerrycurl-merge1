using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Shouldly;

namespace Jerrycurl.Test.Extensions
{
    public static class ShouldlyExtensions
    {
        public static void ShouldBeSameAsJson<T>(this T source, T expected)
        {
            var settings = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.None,
            };
            var sourceJson = JsonConvert.SerializeObject(source, settings);
            var expectedJson = JsonConvert.SerializeObject(expected, settings);

            sourceJson.ShouldBe(expectedJson);
        }
    }
}
