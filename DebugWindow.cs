using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Threading.Tasks;

namespace CSD
{
    public sealed class DebugWindow : Window
    {
        private const string DebugModeKey = "Settings_DebugMode";

        private readonly StackPanel _logPanel;
        private readonly ScrollViewer _scrollViewer;
        private readonly StackPanel _headerPanel;
        private readonly TextBlock _versionText;
        private readonly TextBlock _updateStatusText;
        private readonly Button _checkUpdateButton;
        private readonly ProgressRing _updateProgressRing;
        private readonly UpdateService _updateService = new();
        private int _entryCount;

        public DebugWindow()
        {
            Title = "调试日志";
            SystemBackdrop = new MicaBackdrop();

            var rootGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
                }
            };

            _headerPanel = new StackPanel
            {
                Spacing = 8,
                Padding = new Thickness(16),
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"]
            };

            var versionStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
            _versionText = new TextBlock
            {
                Text = $"当前版本：{UpdateService.GetCurrentVersion()}",
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };
            var updateIcon = new FontIcon
            {
                Glyph = "\uE895",
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            };
            _updateStatusText = new TextBlock
            {
                Text = "点击检查更新",
                FontSize = 12,
                Foreground = (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"],
                VerticalAlignment = VerticalAlignment.Center
            };
            _updateProgressRing = new ProgressRing
            {
                IsActive = false,
                Width = 16,
                Height = 16,
                Visibility = Visibility.Collapsed
            };
            _checkUpdateButton = new Button
            {
                Content = "检查更新",
                Padding = new Thickness(12, 4, 12, 4)
            };
            _checkUpdateButton.Click += async (_, _) => await CheckForUpdateAsync();

            versionStack.Children.Add(updateIcon);
            versionStack.Children.Add(_versionText);
            versionStack.Children.Add(_updateStatusText);

            var updateRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 4, 0, 0) };
            updateRow.Children.Add(_updateProgressRing);
            updateRow.Children.Add(_checkUpdateButton);

            _headerPanel.Children.Add(versionStack);
            _headerPanel.Children.Add(updateRow);

            Grid.SetRow(_headerPanel, 0);
            rootGrid.Children.Add(_headerPanel);

            _logPanel = new StackPanel { Spacing = 4 };

            _scrollViewer = new ScrollViewer
            {
                Content = _logPanel,
                Padding = new Thickness(16),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            Grid.SetRow(_scrollViewer, 1);
            rootGrid.Children.Add(_scrollViewer);

            Content = rootGrid;
            _scrollViewer.Loaded += (_, _) =>
            {
                AnimationHelper.AnimateEntrance(_scrollViewer, fromY: 18f, durationMs: 360);
            };

            AppWindow.Resize(new Windows.Graphics.SizeInt32(600, 500));
        }

        private async Task CheckForUpdateAsync()
        {
            _checkUpdateButton.IsEnabled = false;
            _updateStatusText.Text = "正在检查更新...";
            _updateProgressRing.IsActive = true;
            _updateProgressRing.Visibility = Visibility.Visible;

            try
            {
                var updateInfo = await _updateService.CheckForUpdateAsync();

                if (updateInfo == null)
                {
                    _updateStatusText.Text = "检查更新失败";
                    AppendLog("UPDATE", "/api/distribute/check/csd/", 0, "", "检查更新失败，请稍后重试");
                    return;
                }

                if (!updateInfo.HasUpdate)
                {
                    _updateStatusText.Text = "已是最新版本";
                    AppendLog("UPDATE", "/api/distribute/check/csd/", 200, $"{{\"has_update\":false,\"version\":\"{updateInfo.Version}\"}}");
                    return;
                }

                _updateStatusText.Text = $"发现新版本：{updateInfo.Version}";
                AppendLog("UPDATE", "/api/distribute/check/csd/", 200,
                    $"{{\"has_update\":true,\"version\":\"{updateInfo.Version}\",\"version_code\":{updateInfo.VersionCode},\"file_size\":{updateInfo.FileSize}}}");

                await ShowUpdateDialogAsync(updateInfo);
            }
            catch (Exception ex)
            {
                _updateStatusText.Text = "检查更新失败";
                AppendLog("UPDATE", "/api/distribute/check/csd/", 0, "", ex.Message);
            }
            finally
            {
                _checkUpdateButton.IsEnabled = true;
                _updateProgressRing.IsActive = false;
                _updateProgressRing.Visibility = Visibility.Collapsed;
            }
        }

        private async Task ShowUpdateDialogAsync(UpdateInfo updateInfo)
        {
            var dialog = new ContentDialog
            {
                Title = "发现新版本",
                Content = new StackPanel { Spacing = 12 },
                CloseButtonText = "稍后",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Content.XamlRoot
            };

            var content = (StackPanel)dialog.Content;

            content.Children.Add(new TextBlock
            {
                Text = $"{updateInfo.Title} 可用",
                FontSize = 16,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });

            content.Children.Add(new TextBlock
            {
                Text = $"当前版本：{UpdateService.GetCurrentVersion()}",
                FontSize = 13,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });

            content.Children.Add(new TextBlock
            {
                Text = $"新版本：{updateInfo.Version} (大小：{updateInfo.FileSizeFormatted})",
                FontSize = 13,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });

            if (!string.IsNullOrWhiteSpace(updateInfo.ReleaseNotes))
            {
                content.Children.Add(new TextBlock
                {
                    Text = "更新内容：",
                    FontSize = 13,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Margin = new Thickness(0, 8, 0, 0)
                });

                var releaseNotesScroll = new ScrollViewer
                {
                    Content = new TextBlock
                    {
                        Text = updateInfo.ReleaseNotes,
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 13,
                        Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
                    },
                    MaxHeight = 150,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                };
                content.Children.Add(releaseNotesScroll);
            }

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 8, 0, 0)
            };

            var downloadButton = new Button { Content = "下载更新" };
            downloadButton.Click += (_, _) =>
            {
                try
                {
                    _ = Windows.System.Launcher.LaunchUriAsync(new Uri(updateInfo.DownloadUrl));
                }
                catch { }
            };
            buttonPanel.Children.Add(downloadButton);

            content.Children.Add(buttonPanel);

            await dialog.ShowAsync();
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
                AnimationHelper.AttachHoverAnimation(entry, 1.01f, 0.995f, -2f);
                AnimationHelper.AnimateEntrance(entry, fromY: 10f, durationMs: 220);
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
