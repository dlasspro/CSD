using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;

namespace CSD
{
    public sealed class DebugWindow : Window
    {
        private const string DebugModeKey = "Settings_DebugMode";

        private readonly StackPanel _logPanel;
        private readonly ScrollViewer _scrollViewer;
        private int _entryCount;

        public DebugWindow()
        {
            Title = "调试日志";
            SystemBackdrop = new MicaBackdrop();

            _logPanel = new StackPanel { Spacing = 4 };

            _scrollViewer = new ScrollViewer
            {
                Content = _logPanel,
                Padding = new Thickness(16),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            Content = _scrollViewer;

            // 设置窗口大小
            AppWindow.Resize(new Windows.Graphics.SizeInt32(600, 400));
        }

        public void AppendLog(string method, string path, int statusCode, string responseBody, string? errorMessage = null)
        {
            // 使用 Dispatcher 确保 UI 线程安全
            _ = DispatcherQueue.TryEnqueue(() =>
            {
                _entryCount++;

                var entry = new Border
                {
                    Background = errorMessage != null
                        ? new SolidColorBrush(Windows.UI.Color.FromArgb(30, 255, 0, 0))
                        : statusCode >= 200 && statusCode < 300
                            ? new SolidColorBrush(Windows.UI.Color.FromArgb(20, 0, 200, 0))
                            : new SolidColorBrush(Windows.UI.Color.FromArgb(30, 255, 165, 0)),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(12)
                };

                var stack = new StackPanel { Spacing = 6 };

                // 请求行
                stack.Children.Add(new TextBlock
                {
                    Text = $"[#{_entryCount}] {method} {path}",
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    FontSize = 14
                });

                // 状态码
                stack.Children.Add(new TextBlock
                {
                    Text = $"状态码: {statusCode}",
                    FontSize = 13,
                    Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
                });

                // 响应内容（截取前 500 字符）
                if (!string.IsNullOrEmpty(responseBody))
                {
                    var displayText = responseBody.Length > 500
                        ? responseBody[..500] + "..."
                        : responseBody;
                    stack.Children.Add(new TextBlock
                    {
                        Text = displayText,
                        FontSize = 12,
                        TextWrapping = TextWrapping.Wrap,
                        FontFamily = new FontFamily("Consolas"),
                        Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
                    });
                }

                // 错误信息
                if (errorMessage != null)
                {
                    stack.Children.Add(new TextBlock
                    {
                        Text = $"错误: {errorMessage}",
                        FontSize = 13,
                        Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 50, 50))
                    });
                }

                entry.Child = stack;
                _logPanel.Children.Add(entry);

                // 自动滚动到底部
                _scrollViewer.ChangeView(null, _scrollViewer.ScrollableHeight, null);
            });
        }

        public static bool IsDebugModeEnabled()
        {
            var settings = AppSettings.Values;
            return settings.ContainsKey(DebugModeKey) && (bool)(settings[DebugModeKey] ?? false);
        }
    }
}
