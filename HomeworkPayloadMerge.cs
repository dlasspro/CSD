using System.Collections.Generic;
using System.Text.Json;

namespace CSD
{
    internal static class HomeworkPayloadMerge
    {
        public static string BuildPostJson(string? rawJson, bool rawJsonMatchesCurrentDate, string subject, string content)
        {
            var homeworkDict = new Dictionary<string, object>();
            var attendanceDict = new Dictionary<string, object>();

            if (rawJsonMatchesCurrentDate && !string.IsNullOrWhiteSpace(rawJson))
            {
                try
                {
                    using var document = JsonDocument.Parse(rawJson);
                    if (document.RootElement.TryGetProperty("homework", out var homeworkElement) && homeworkElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var subj in homeworkElement.EnumerateObject())
                        {
                            if (subj.Value.ValueKind == JsonValueKind.Object)
                            {
                                var inner = new Dictionary<string, object>();
                                foreach (var p in subj.Value.EnumerateObject())
                                {
                                    inner[p.Name] = p.Value.ValueKind == JsonValueKind.String
                                        ? p.Value.GetString()!
                                        : p.Value.GetRawText();
                                }
                                homeworkDict[subj.Name] = inner;
                            }
                            else
                            {
                                homeworkDict[subj.Name] = subj.Value.GetRawText();
                            }
                        }
                    }

                    if (document.RootElement.TryGetProperty("attendance", out var attendanceElement) && attendanceElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var att in attendanceElement.EnumerateObject())
                        {
                            if (att.Value.ValueKind == JsonValueKind.Array)
                            {
                                var list = new List<string>();
                                foreach (var item in att.Value.EnumerateArray())
                                    list.Add(item.GetString() ?? "");
                                attendanceDict[att.Name] = list;
                            }
                            else
                            {
                                attendanceDict[att.Name] = att.Value.GetRawText();
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                    homeworkDict.Clear();
                    attendanceDict.Clear();
                }
            }

            homeworkDict[subject] = new Dictionary<string, object> { ["content"] = content };

            var payload = new Dictionary<string, object>
            {
                ["homework"] = homeworkDict,
                ["attendance"] = attendanceDict
            };

            return JsonSerializer.Serialize(payload, AppJsonSerializerContext.Default.DictionaryStringObject);
        }
    }
}
