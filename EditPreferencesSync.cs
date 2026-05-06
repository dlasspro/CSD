using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CSD
{
    /// <summary>将「编辑」类设置与 KV 键 <see cref="ClassworksKvKeys.EditConfig"/> 同步。</summary>
    internal static class EditPreferencesSync
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public static Dictionary<string, object> BuildCloudDocumentFromAppSettings()
        {
            var s = AppSettings.Values;
            return new Dictionary<string, object>
            {
                ["schemaVersion"] = 1,
                ["autoSave"] = ReadBool(s, EditPreferencesKeys.AutoSave),
                ["blockNonTodayAutoSave"] = ReadBool(s, EditPreferencesKeys.BlockNonTodayAutoSave),
                ["blockNonTodayWrite"] = ReadBool(s, EditPreferencesKeys.BlockNonTodayAutoSave),
                ["confirmNonTodaySave"] = ReadBool(s, EditPreferencesKeys.ConfirmNonTodaySave),
                ["refreshBeforeEdit"] = ReadBool(s, EditPreferencesKeys.RefreshBeforeEdit),
                ["autoSavePromptText"] = ReadString(s, EditPreferencesKeys.AutoSavePromptText),
                ["manualSavePromptText"] = ReadString(s, EditPreferencesKeys.ManualSavePromptText)
            };
        }

        public static void MergeCloudDocumentIntoAppSettings(JsonElement root)
        {
            var s = AppSettings.Values;
            if (root.ValueKind != JsonValueKind.Object)
                return;

            TryWriteBool(s, EditPreferencesKeys.AutoSave, root, "autoSave");
            if (root.TryGetProperty("blockNonTodayWrite", out var writeEl) && (writeEl.ValueKind == JsonValueKind.True || writeEl.ValueKind == JsonValueKind.False))
                s[EditPreferencesKeys.BlockNonTodayAutoSave] = writeEl.GetBoolean();
            else
                TryWriteBool(s, EditPreferencesKeys.BlockNonTodayAutoSave, root, "blockNonTodayAutoSave");
            TryWriteBool(s, EditPreferencesKeys.ConfirmNonTodaySave, root, "confirmNonTodaySave");
            TryWriteBool(s, EditPreferencesKeys.RefreshBeforeEdit, root, "refreshBeforeEdit");
            TryWriteString(s, EditPreferencesKeys.AutoSavePromptText, root, "autoSavePromptText");
            TryWriteString(s, EditPreferencesKeys.ManualSavePromptText, root, "manualSavePromptText");
        }

        public static async Task<bool> PushAsync(HttpClient http, string baseUrl, string bearerToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(bearerToken))
                return false;

            var url = $"{baseUrl.TrimEnd('/')}/kv/{Uri.EscapeDataString(ClassworksKvKeys.EditConfig)}";
            var json = JsonSerializer.Serialize(BuildCloudDocumentFromAppSettings(), SerializerOptions);
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken.Trim());
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>GET 云端配置并合并写入 <see cref="AppSettings.Values"/>（不更新设置窗口 UI）。</summary>
        public static async Task<bool> TryPullMergeIntoAppSettingsAsync(HttpClient http, string baseUrl, string bearerToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(bearerToken))
                return false;

            var url = $"{baseUrl.TrimEnd('/')}/kv/{Uri.EscapeDataString(ClassworksKvKeys.EditConfig)}";
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken.Trim());
                using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return false;
                if (!response.IsSuccessStatusCode)
                    return false;

                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(body))
                    return false;

                using var doc = JsonDocument.Parse(body);
                MergeCloudDocumentIntoAppSettings(doc.RootElement);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool ReadBool(IDictionary<string, object> s, string key) =>
            s.ContainsKey(key) && (bool)(s[key] ?? false);

        private static string ReadString(IDictionary<string, object> s, string key) =>
            s[key] as string ?? "";

        private static void TryWriteBool(IDictionary<string, object> s, string appKey, JsonElement root, string jsonName)
        {
            if (!root.TryGetProperty(jsonName, out var el))
                return;
            if (el.ValueKind == JsonValueKind.True || el.ValueKind == JsonValueKind.False)
                s[appKey] = el.GetBoolean();
        }

        private static void TryWriteString(IDictionary<string, object> s, string appKey, JsonElement root, string jsonName)
        {
            if (!root.TryGetProperty(jsonName, out var el) || el.ValueKind != JsonValueKind.String)
                return;
            s[appKey] = el.GetString() ?? "";
        }
    }
}
