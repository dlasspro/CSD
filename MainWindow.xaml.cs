using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
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
            HomeworkContainer.Children.Clear();

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
                    HomeworkContainer.Children.Clear();
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

                HomeworkContainer.Children.Clear();
                StatusText.Text = items.Count == 0 ? "今日暂无作业。" : $"共 {items.Count} 项作业";

                if (items.Count == 0) return;

                // 计算响应式列数
                double availableWidth = HomeworkContainer.ActualWidth;
                if (availableWidth <= 0) availableWidth = 800;

                double minCardWidth = 220;
                double gap = 14;
                int itemsPerRow = Math.Max(1, Math.Min(items.Count, (int)((availableWidth + gap) / (minCardWidth + gap))));
                int rows = (int)Math.Ceiling((double)items.Count / itemsPerRow);

                var grid = new Grid
                {
                    ColumnSpacing = gap,
                    RowSpacing = gap
                };

                for (int j = 0; j < itemsPerRow; j++)
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                for (int i = 0; i < rows; i++)
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                for (int idx = 0; idx < items.Count; idx++)
                {
                    int row = idx / itemsPerRow;
                    int col = idx % itemsPerRow;
                    var card = CreateCard(items[idx]);
                    Grid.SetRow(card, row);
                    Grid.SetColumn(card, col);
                    grid.Children.Add(card);
                }

                HomeworkContainer.Children.Add(grid);
            }
            catch (JsonException ex)
            {
                StatusText.Text = "作业数据格式错误。";
                HomeworkContainer.Children.Clear();
                ResultBox.Text += $"\r\n\r\nJSON 解析失败：{ex.Message}";
            }
        }

        private static Border CreateCard(HomeworkItem item)
        {
            var border = new Border
            {
                Padding = new Thickness(22),
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                CornerRadius = new CornerRadius(10),
                Translation = new Vector3(0, 0, 16)
            };

            border.Shadow = new ThemeShadow();

            var stack = new StackPanel { Spacing = 10 };
            stack.Children.Add(new TextBlock
            {
                FontSize = 22,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Text = item.Subject
            });
            stack.Children.Add(new TextBlock
            {
                FontSize = 17,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                Text = item.Content,
                TextWrapping = TextWrapping.WrapWholeWords
            });

            border.Child = stack;
            return border;
        }
    }
}