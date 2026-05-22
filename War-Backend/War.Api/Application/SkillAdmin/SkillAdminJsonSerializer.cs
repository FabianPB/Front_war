using System.Text.Json;
using System.Text.Json.Serialization;
using War.Core.Skills;

namespace War.Api.Application.SkillAdmin;

public static class SkillAdminJsonSerializer
{
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, Options);
    }

    public static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, Options);
    }

    public static SkillDefinition DeserializeSkillDefinition(JsonElement definitionJson)
    {
        var definition = definitionJson.Deserialize<SkillDefinition>(Options);
        return definition ?? throw new InvalidOperationException("The admin request payload could not be converted into a SkillDefinition.");
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
