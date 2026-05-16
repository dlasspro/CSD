using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;


using CSD.Views;
using CSD.Models;
using CSD.Services;
using CSD.Helpers;
using CSD.Settings;


namespace CSD.Models
{
    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSerializable(typeof(Dictionary<string, JsonElement>))]
    [JsonSerializable(typeof(List<string>))]
    internal partial class AppJsonSerializerContext : JsonSerializerContext
    {
    }

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(Dictionary<string, object>))]
    internal partial class AppJsonIndentedSerializerContext : JsonSerializerContext
    {
    }
}
