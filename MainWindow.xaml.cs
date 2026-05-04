using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace CSD
{
    public sealed class HomeworkItem
    {
        public string Subject { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private const string BaseUrl = "https://kv-service.wuyuan.dev";
        private const string TokenSettingsKey = "Token";
        private readonly HttpClient _httpClient = new();

        public MainWindow()
        {
            InitializeComponent();
            _ = LoadTodayHomeworkAsync();
        }

        private async void RefreshHomeworkButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadTodayHomeworkAsync();
        }

        private async Task LoadTodayHomeworkAsync()
        {
            var todayKey = $"classworks-data-{DateTime.Now:yyyyMMdd}";
            TodayKeyText.Text = todayKey;
            StatusText.Text = "正在加载今日作业...";
            HomeworkItemsControl.ItemsSource = null;

            var responseBody = await SendKvRequestAsync(HttpMethod.Get, $"/kv/{Uri.EscapeDataString(todayKey)}");
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return;
            }

            ShowHomework(responseBody);
        }

        private async Task<string?> SendKvRequestAsync(HttpMethod method, string path, string? jsonBody = null)
        {
            var token = ApplicationData.Current.LocalSettings.Values[TokenSettingsKey] as string;
            if (string.IsNullOrWhiteSpace(token))
            {
                StatusText.Text = "本地没有 Token，请先完成初始化。";
                ResultBox.Text = StatusText.Text;
                return null;
            }

            try
            {
                using var request = new HttpRequestMessage(method, BaseUrl + path);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                if (jsonBody is not null)
                {
                    request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                }

                ResultBox.Text = "请求中...";

                using var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                ResultBox.Text = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}\r\n\r\n{responseBody}";

                if (!response.IsSuccessStatusCode)
                {
                    StatusText.Text = "获取今日作业失败。";
                    return null;
                }

                return responseBody;
            }
            catch (Exception ex)
            {
                StatusText.Text = "获取今日作业失败。";
                ResultBox.Text = ex.Message;
                return null;
            }
        }

        private void ShowHomework(string json)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                if (!document.RootElement.TryGetProperty("homework", out var homework) || homework.ValueKind != JsonValueKind.Object)
                {
                    StatusText.Text = "今日暂无作业。";
                    HomeworkItemsControl.ItemsSource = null;
                    return;
                }

                var items = new List<HomeworkItem>();
                foreach (var subject in homework.EnumerateObject())
                {
                    var content = subject.Value.ValueKind == JsonValueKind.Object && subject.Value.TryGetProperty("content", out var contentElement)
                        ? contentElement.GetString()
                        : subject.Value.ToString();

                    items.Add(new HomeworkItem
                    {
                        Subject = subject.Name,
                        Content = string.IsNullOrWhiteSpace(content) ? "暂无内容" : content
                    });
                }

                HomeworkItemsControl.ItemsSource = items;
                StatusText.Text = items.Count == 0 ? "今日暂无作业。" : $"共 {items.Count} 项作业";
            }
            catch (JsonException ex)
            {
                StatusText.Text = "作业数据格式错误。";
                HomeworkItemsControl.ItemsSource = null;
                ResultBox.Text += $"\r\n\r\nJSON 解析失败：{ex.Message}";
            }
        }
    }
}
