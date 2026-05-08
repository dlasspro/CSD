using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI;

namespace CSD
{
    public sealed partial class RandomPickerWindow : Window
    {
        private readonly HttpClient _httpClient = new();

        private readonly StackPanel _rootPanel;
        private readonly Border _windowRoot;
        private readonly TextBlock _countDisplay;
        private readonly Button _btnDecrease;
        private readonly Button _btnIncrease;
        private readonly Button _btnNameMode;
        private readonly Button _btnNumberMode;
        private readonly Button _btnStartPick;
        private readonly TextBlock _availableCountText;
        private readonly Button _btnIncludeLate;
        private readonly Button _btnExcludeLeave;
        private readonly Button _btnExcludeAbsent;
        private readonly Border _resultPanel;
        private readonly TextBlock _resultText;
        private readonly Border _animationOverlay;
        private readonly TextBlock _animationText;
        private readonly StackPanel _resultListPanel;

        private int _pickCount = 1;
        private bool _isNameMode = true;
        private bool _includeLate = true;
        private bool _excludeLeave = true;
        private bool _excludeAbsent = true;

        private List<string> _studentNames = new();
        private List<string> _availableStudents = new();
        private Random _random = new();

        private readonly SolidColorBrush _accentBrush;
        private readonly SolidColorBrush _accentForeground;
        private readonly SolidColorBrush _transparentBrush;
        private readonly SolidColorBrush _secondaryTextBrush;
        private readonly SolidColorBrush _primaryTextBrush;

        public RandomPickerWindow()
        {
            Title = "随机点名";

            AppWindow.Resize(new SizeInt32(680, 620));
            SystemBackdrop = new MicaBackdrop();

            _accentBrush = (SolidColorBrush)Application.Current.Resources["AccentFillColorDefaultBrush"];
            _accentForeground = (SolidColorBrush)Application.Current.Resources["TextOnAccentFillColorPrimaryBrush"];
            _transparentBrush = new SolidColorBrush(Colors.Transparent);
            _secondaryTextBrush = (SolidColorBrush)Application.Current.Resources["TextFillColorSecondaryBrush"];
            _primaryTextBrush = (SolidColorBrush)Application.Current.Resources["TextFillColorPrimaryBrush"];

            _isNameMode = true;
            _pickCount = RandomPickerSettings.DefaultCount;
            _includeLate = true;
            _excludeLeave = true;
            _excludeAbsent = true;

            _rootPanel = new StackPanel { Spacing = 0 };

            var titleBar = new Grid
            {
                Padding = new Thickness(24, 20, 16, 16),
                ColumnSpacing = 12
            };
            titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleIcon = new FontIcon
            {
                Glyph = "\uE716",
                FontSize = 24,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = _primaryTextBrush
            };
            Grid.SetColumn(titleIcon, 0);
            titleBar.Children.Add(titleIcon);

            var titleText = new TextBlock
            {
                Text = "随机点名",
                FontSize = 24,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = _primaryTextBrush
            };
            Grid.SetColumn(titleText, 1);
            titleBar.Children.Add(titleText);

            var closeBtn = new Button
            {
                Content = new FontIcon { Glyph = "\uE711", FontSize = 14 },
                Background = _transparentBrush,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8),
                CornerRadius = new CornerRadius(6),
                Foreground = _secondaryTextBrush
            };
            Grid.SetColumn(closeBtn, 2);
            closeBtn.Click += (_, _) => Close();
            titleBar.Children.Add(closeBtn);

            _rootPanel.Children.Add(titleBar);

            var contentPanel = new StackPanel
            {
                Spacing = 24,
                Padding = new Thickness(32, 8, 32, 32)
            };

            var countSection = new StackPanel { Spacing = 16, HorizontalAlignment = HorizontalAlignment.Center };
            countSection.Children.Add(new TextBlock
            {
                Text = "请选择抽取人数",
                FontSize = 20,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            var countPickerRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            _btnDecrease = CreateRoundButton("\uE949", false);
            _btnDecrease.Click += DecreaseCount_Click;

            _countDisplay = new TextBlock
            {
                Text = _pickCount.ToString(),
                FontSize = 56,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = _primaryTextBrush,
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 80,
                TextAlignment = TextAlignment.Center
            };

            var countUnit = new TextBlock
            {
                Text = "人",
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 8),
                Foreground = _secondaryTextBrush
            };

            var countRight = new StackPanel { Spacing = 0, Orientation = Orientation.Horizontal };
            countRight.Children.Add(_countDisplay);
            countRight.Children.Add(countUnit);

            _btnIncrease = CreateRoundButton("\uE710", true);
            _btnIncrease.Click += IncreaseCount_Click;

            countPickerRow.Children.Add(_btnDecrease);
            countPickerRow.Children.Add(countRight);
            countPickerRow.Children.Add(_btnIncrease);
            countSection.Children.Add(countPickerRow);
            contentPanel.Children.Add(countSection);

            var modeRow = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Spacing = 0 };
            var modeBorder = new Border
            {
                CornerRadius = new CornerRadius(12),
                BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                Padding = new Thickness(4)
            };
            var modeGrid = new Grid();
            modeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            modeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _btnNameMode = CreateModeButton("姓名模式", "\uE716", true);
            Grid.SetColumn(_btnNameMode, 0);
            _btnNameMode.Click += (_, _) => SetMode(true);
            modeGrid.Children.Add(_btnNameMode);

            _btnNumberMode = CreateModeButton("学号模式", "\uE949", false);
            Grid.SetColumn(_btnNumberMode, 1);
            _btnNumberMode.Click += (_, _) => SetMode(false);
            modeGrid.Children.Add(_btnNumberMode);

            modeBorder.Child = modeGrid;
            modeRow.Children.Add(modeBorder);
            contentPanel.Children.Add(modeRow);

            _btnStartPick = new Button
            {
                Content = CreateStartButtonContent(),
                Background = _accentBrush,
                Foreground = _accentForeground,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(32, 16, 32, 16),
                CornerRadius = new CornerRadius(16),
                HorizontalAlignment = HorizontalAlignment.Center,
                MinWidth = 200,
                FontSize = 20,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            };
            _btnStartPick.Click += StartPick_Click;
            contentPanel.Children.Add(_btnStartPick);

            _availableCountText = new TextBlock
            {
                Text = "当前可抽取学生: 0人",
                FontSize = 13,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = _secondaryTextBrush
            };
            contentPanel.Children.Add(_availableCountText);

            var filterRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 12,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            _btnIncludeLate = CreateFilterButton("\uE823", "包含迟到学生", true, true);
            _btnIncludeLate.Click += (_, _) => ToggleFilter(_btnIncludeLate, ref _includeLate);
            filterRow.Children.Add(_btnIncludeLate);

            _btnExcludeLeave = CreateFilterButton("\uE716", "排除请假学生", false, false);
            _btnExcludeLeave.Click += (_, _) => ToggleFilter(_btnExcludeLeave, ref _excludeLeave);
            filterRow.Children.Add(_btnExcludeLeave);

            _btnExcludeAbsent = CreateFilterButton("\uE716", "排除不参与学生", false, false);
            _btnExcludeAbsent.Click += (_, _) => ToggleFilter(_btnExcludeAbsent, ref _excludeAbsent);
            filterRow.Children.Add(_btnExcludeAbsent);

            contentPanel.Children.Add(filterRow);

            _resultPanel = new Border
            {
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(24),
                MinHeight = 100,
                Visibility = Visibility.Collapsed
            };

            _resultText = new TextBlock
            {
                FontSize = 24,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = _primaryTextBrush,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            _resultListPanel = new StackPanel { Spacing = 8, HorizontalAlignment = HorizontalAlignment.Center };
            var resultInner = new StackPanel { Spacing = 16 };
            resultInner.Children.Add(_resultText);
            resultInner.Children.Add(_resultListPanel);
            _resultPanel.Child = resultInner;

            contentPanel.Children.Add(_resultPanel);

            _rootPanel.Children.Add(contentPanel);

            _animationOverlay = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
                Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            _animationText = new TextBlock
            {
                FontSize = 72,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var animationInner = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            animationInner.Children.Add(_animationText);
            _animationOverlay.Child = animationInner;

            _windowRoot = new Border
            {
                Background = (Brush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"],
                Child = _rootPanel
            };

            var gridRoot = new Grid();
            gridRoot.Children.Add(_windowRoot);
            gridRoot.Children.Add(_animationOverlay);

            Content = gridRoot;

            _ = LoadStudentsAsync();
        }

        private static UIElement CreateStartButtonContent()
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
            row.Children.Add(new FontIcon { Glyph = "\uE723", FontSize = 22, VerticalAlignment = VerticalAlignment.Center });
            row.Children.Add(new TextBlock { Text = "开始抽取", VerticalAlignment = VerticalAlignment.Center });
            return row;
        }

        private Button CreateRoundButton(string glyph, bool isPlus)
        {
            return new Button
            {
                Content = new FontIcon { Glyph = glyph, FontSize = 18 },
                Width = 56,
                Height = 56,
                CornerRadius = new CornerRadius(28),
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                Foreground = isPlus ? _accentBrush : _secondaryTextBrush
            };
        }

        private Button CreateModeButton(string label, string glyph, bool isActive)
        {
            return new Button
            {
                Content = CreateModeButtonContent(label, glyph),
                Background = isActive ? _accentBrush : _transparentBrush,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(16, 10, 16, 10),
                CornerRadius = new CornerRadius(8),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                FontSize = 15,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = isActive ? _accentForeground : _secondaryTextBrush
            };
        }

        private static UIElement CreateModeButtonContent(string label, string glyph)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            row.Children.Add(new FontIcon { Glyph = glyph, FontSize = 14, VerticalAlignment = VerticalAlignment.Center });
            row.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
            return row;
        }

        private Button CreateFilterButton(string glyph, string label, bool isActive, bool isAccent)
        {
            var bg = isActive
                ? new SolidColorBrush(Color.FromArgb(100, 200, 80, 80))
                : isAccent ? (Brush)_accentBrush : (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"];
            var fg = isActive
                ? new SolidColorBrush(Colors.White)
                : isAccent ? (Brush)_accentForeground : (Brush)_secondaryTextBrush;

            return new Button
            {
                Content = CreateFilterButtonContent(glyph, label),
                Background = bg,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(12, 8, 12, 8),
                CornerRadius = new CornerRadius(20),
                HorizontalAlignment = HorizontalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                FontSize = 13,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = fg
            };
        }

        private static UIElement CreateFilterButtonContent(string glyph, string label)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            row.Children.Add(new FontIcon { Glyph = glyph, FontSize = 14, VerticalAlignment = VerticalAlignment.Center });
            row.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
            return row;
        }

        private void DecreaseCount_Click(object sender, RoutedEventArgs e)
        {
            if (_pickCount > 1)
            {
                _pickCount--;
                _countDisplay.Text = _pickCount.ToString();
            }
        }

        private void IncreaseCount_Click(object sender, RoutedEventArgs e)
        {
            if (_pickCount < 20)
            {
                _pickCount++;
                _countDisplay.Text = _pickCount.ToString();
            }
        }

        private void SetMode(bool isName)
        {
            _isNameMode = isName;
            _btnNameMode.Background = isName ? _accentBrush : _transparentBrush;
            _btnNumberMode.Background = !isName ? _accentBrush : _transparentBrush;
            _btnNameMode.Foreground = isName ? _accentForeground : _secondaryTextBrush;
            _btnNumberMode.Foreground = !isName ? _accentForeground : _secondaryTextBrush;
        }

        private void ToggleFilter(Button btn, ref bool value)
        {
            value = !value;
            btn.Background = value
                ? new SolidColorBrush(Color.FromArgb(100, 200, 80, 80))
                : (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"];
            btn.Foreground = value ? new SolidColorBrush(Colors.White) : _secondaryTextBrush;
        }

        private async Task LoadStudentsAsync()
        {
            var rosterKey = "Settings_RosterList";
            if (AppSettings.Values.TryGetValue(rosterKey, out var rosterObj))
            {
                try
                {
                    var jsonStr = rosterObj.ToString();
                    var students = JsonSerializer.Deserialize<string[]>(jsonStr);
                    if (students != null)
                    {
                        _studentNames = students.ToList();
                    }
                }
                catch { }
            }

            if (_studentNames.Count == 0)
            {
                var token = AppSettings.Values["Token"] as string;
                if (!string.IsNullOrWhiteSpace(token))
                {
                    var baseUrl = (AppSettings.Values["Settings_ServerUrl"] as string ?? "https://kv-service.wuyuan.dev").TrimEnd('/');
                    try
                    {
                        using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/kv/{Uri.EscapeDataString("classworks-list-main")}");
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());
                        using var response = await _httpClient.SendAsync(request);
                        if (response.IsSuccessStatusCode)
                        {
                            var body = await response.Content.ReadAsStringAsync();
                            using var doc = JsonDocument.Parse(body);
                            if (doc.RootElement.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.String)
                            {
                                var data = dataEl.GetString();
                                if (!string.IsNullOrEmpty(data))
                                {
                                    var lines = data.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                                    _studentNames = lines.Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToList();
                                }
                            }
                        }
                    }
                    catch { }
                }
            }

            UpdateAvailableCount();
        }

        private void UpdateAvailableCount()
        {
            _availableStudents = _studentNames.ToList();
            _availableCountText.Text = $"当前可抽取学生: {_availableStudents.Count}人";
        }

        private async void StartPick_Click(object sender, RoutedEventArgs e)
        {
            if (_availableStudents.Count == 0)
            {
                await ShowDialogAsync("没有可抽取的学生，请先添加学生名单。");
                return;
            }

            var count = Math.Min(_pickCount, _availableStudents.Count);
            if (count == 0)
            {
                await ShowDialogAsync("没有可抽取的学生。");
                return;
            }

            if (RandomPickerSettings.AnimationEnabled)
            {
                await RunAnimationAsync(count);
            }
            else
            {
                ShowResults(count);
            }
        }

        private async Task RunAnimationAsync(int count)
        {
            _animationOverlay.Visibility = Visibility.Visible;
            _btnStartPick.IsEnabled = false;

            var duration = 1500;
            var interval = 50;
            var elapsed = 0;

            while (elapsed < duration)
            {
                var randomStudent = _availableStudents[_random.Next(_availableStudents.Count)];
                _animationText.Text = randomStudent;
                await Task.Delay(interval);
                elapsed += interval;
                if (interval < 200)
                    interval += 5;
            }

            _animationOverlay.Visibility = Visibility.Collapsed;
            _btnStartPick.IsEnabled = true;

            ShowResults(count);
        }

        private void ShowResults(int count)
        {
            var picked = new List<string>();
            var pool = _availableStudents.ToList();

            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                var idx = _random.Next(pool.Count);
                picked.Add(pool[idx]);
                pool.RemoveAt(idx);
            }

            _resultListPanel.Children.Clear();

            if (picked.Count == 1)
            {
                _resultText.Text = $"恭喜：{picked[0]}";
                _resultText.Visibility = Visibility.Visible;
                _resultListPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                _resultText.Text = $"共抽取 {picked.Count} 人";
                _resultText.Visibility = Visibility.Visible;
                _resultListPanel.Visibility = Visibility.Visible;

                foreach (var name in picked)
                {
                    var nameCard = new Border
                    {
                        Background = _accentBrush,
                        CornerRadius = new CornerRadius(10),
                        Padding = new Thickness(16, 10, 16, 10),
                        MinWidth = 120,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Child = new TextBlock
                        {
                            Text = name,
                            FontSize = 20,
                            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                            Foreground = _accentForeground,
                            TextAlignment = TextAlignment.Center
                        }
                    };
                    _resultListPanel.Children.Add(nameCard);
                }
            }

            _resultPanel.Visibility = Visibility.Visible;
            _resultPanel.Opacity = 0;

            var sb = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
            var da = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
            {
                To = 1.0,
                Duration = new Microsoft.UI.Xaml.Duration(TimeSpan.FromMilliseconds(300)),
                EnableDependentAnimation = true
            };
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(da, _resultPanel);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(da, "Opacity");
            sb.Children.Add(da);

            var va = new Microsoft.UI.Xaml.Media.Animation.ObjectAnimationUsingKeyFrames();
            var kf = new Microsoft.UI.Xaml.Media.Animation.DiscreteObjectKeyFrame
            {
                KeyTime = Microsoft.UI.Xaml.Media.Animation.KeyTime.FromTimeSpan(TimeSpan.Zero),
                Value = Visibility.Visible
            };
            va.KeyFrames.Add(kf);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(va, _resultPanel);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(va, "Visibility");
            sb.Children.Add(va);

            sb.Begin();
        }

        private async Task ShowDialogAsync(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "提示",
                Content = message,
                CloseButtonText = "确定",
                XamlRoot = Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
