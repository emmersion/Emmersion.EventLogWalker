using System.Text.Json;
using System.Text.Json.Serialization;

namespace Emmersion.EventLogWalker
{
    public interface IJsonSerializer
    {
        T Deserialize<T>(string input);
        string Serialize<T>(T input);
    }

    public class JsonSerializer : IJsonSerializer
    {
        private static readonly JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        static JsonSerializer()
        {
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, true));
        }

        public T Deserialize<T>(string input)
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(input, options);
        }

        public string Serialize<T>(T input)
        {
            return System.Text.Json.JsonSerializer.Serialize(input, options);
        }
    }
}
