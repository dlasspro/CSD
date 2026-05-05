using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Graphics;

namespace CSD
{
    public sealed class HomeworkItem
    {
        public string Subject { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public sealed partial class MainWindow : Window
    {
        private const string TokenSettingsKey = "Token";
        private const string ServerUrlKey = "Settings_ServerUrl";
        private const string AutoRefreshEnabledKey = "Settings_AutoRefreshEnabled";
        private const string AutoRefreshIntervalKey = "Settings_AutoRefreshInterval";
        private const string CarouselIntervalKey = "Settings_CarouselInterval";
        private const string CarouselFontSizeKey = "Settings_CarouselFontSize";
        private const string DebugModeKey = "Settings_DebugMode";

        private readonly HttpClient _httpClient = new();
        private DateTime _currentDate = DateTime.Now;
        private string? _rawJson;
        private readonly DispatcherTimer _autoRefreshTimer = new();
        private List<HomeworkItem> _carouselItems = new();
        private int _carouselIndex = 0;
        private readonly DispatcherTimer _carouselTimer = new();
        private DebugWindow? _debugWindow;
        private bool _isUpdatingCalendarSelection;
        private int _carouselOverlayAnimationToken;

        // 当前作业的科目名称集合（用于判断未完成作业）
        private HashSet<string> _currentHomeworkSubjects = new();

        private string BaseUrl
        {
            get
            {
                var url = AppSettings.Values[ServerUrlKey] as string;
                return string.IsNullOrWhiteSpace(url) ? "https://kv-service.wuyuan.dev" : url;
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            // 设置窗口标题栏图标
            try
            {
                AppWindow.SetIcon("Assets/StoreLogo.png");
            }
            catch { }

            RestoreWindowState();

            // 关闭时保存窗口状态
            AppWindow.Closing += (sender, args) =>
            {
                SaveWindowState();
            };

            Closed += (sender, args) => SaveWindowState();

            _autoRefreshTimer.Tick += AutoRefreshTimer_Tick;
            RestartAutoRefreshTimer();
            if (Content is FrameworkElement rootContent)
            {
                rootContent.Loaded += RootContent_Loaded;
            }

            UpdateDateDisplay();
            _ = LoadHomeworkAsync(_currentDate);
        }

        private void RootContent_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement rootContent)
            {
                rootContent.Loaded -= RootContent_Loaded;
                AnimationHelper.AnimateEntrance(rootContent, fromY: 16f, durationMs: 380);
                AnimationHelper.ApplyStandardInteractions(rootContent);
            }
        }

        private void RestoreWindowState()
        {
            try
            {
                var settings = AppSettings.Values;

                if (settings.ContainsKey("MainWindow_Width") && settings.ContainsKey("MainWindow_Height"))
                {
                    int width = Math.Max(400, (int)(double)settings["MainWindow_Width"]);
                    int height = Math.Max(300, (int)(double)settings["MainWindow_Height"]);
                    this.AppWindow.Resize(new SizeInt32(width, height));
                }

                if (settings.ContainsKey("MainWindow_X") && settings.ContainsKey("MainWindow_Y"))
                {
                    int x = (int)(double)settings["MainWindow_X"];
                    int y = (int)(double)settings["MainWindow_Y"];
                    this.AppWindow.Move(new PointInt32(x, y));
                }

                if (settings.ContainsKey("MainWindow_State"))
                {
                    string? state = settings["MainWindow_State"] as string;
                    if (state == "Maximized" && this.AppWindow.Presenter is OverlappedPresenter presenter)
                    {
                        presenter.Maximize();
                    }
                }
            }
            catch { }
        }

        private void SaveWindowState()
        {
            try
            {
                var settings = AppSettings.Values;
                settings["MainWindow_X"] = (double)this.AppWindow.Position.X;
                settings["MainWindow_Y"] = (double)this.AppWindow.Position.Y;
                settings["MainWindow_Width"] = (double)this.AppWindow.Size.Width;
                settings["MainWindow_Height"] = (double)this.AppWindow.Size.Height;

                if (this.AppWindow.Presenter is OverlappedPresenter presenter)
                {
                    settings["MainWindow_State"] = presenter.State.ToString();
                }
            }
            catch { }
        }

        private void RestartAutoRefreshTimer()
        {
            _autoRefreshTimer.Stop();

            var settings = AppSettings.Values;
            bool enabled = settings.ContainsKey(AutoRefreshEnabledKey)
                ? (bool)(settings[AutoRefreshEnabledKey] ?? false)
                : false;

            if (enabled)
            {
                double intervalSeconds = (double)(settings[AutoRefreshIntervalKey] ?? 60.0);
                _autoRefreshTimer.Interval = TimeSpan.FromSeconds(intervalSeconds);
                _autoRefreshTimer.Start();
            }
        }

        private void AutoRefreshTimer_Tick(object? sender, object e)
        {
            _ = LoadHomeworkAsync(_currentDate);
        }

        private async void RefreshHomeworkButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadHomeworkAsync(_currentDate);
        }

        private void OpenSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(() =>
            {
                RestartAutoRefreshTimer();
                _ = LoadHomeworkAsync(_currentDate);

                // 如果关闭了调试模式，关闭调试窗口
                if (!DebugWindow.IsDebugModeEnabled() && _debugWindow != null)
                {
                    _debugWindow.Close();
                    _debugWindow = null;
                }
            });
            settingsWindow.Activate();
        }

        private void TutorialButton_Click(object sender, RoutedEventArgs e)
        {
            _ = Windows.System.Launcher.LaunchUriAsync(new Uri("https://520.re/csh"));
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.Activate();
        }

        private async void PrevDateButton_Click(object sender, RoutedEventArgs e)
        {
            _currentDate = _currentDate.AddDays(-1);
            await LoadHomeworkAsync(_currentDate);
        }

        private async void NextDateButton_Click(object sender, RoutedEventArgs e)
        {
            _currentDate = _currentDate.AddDays(1);
            await LoadHomeworkAsync(_currentDate);
        }

        private void DateFlyout_Opening(object sender, object e)
        {
            UpdateDateDisplay();
        }

        private async void DateCalendarView_SelectedDatesChanged(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs args)
        {
            if (_isUpdatingCalendarSelection || sender.SelectedDates.Count == 0)
            {
                return;
            }

            var selectedDate = sender.SelectedDates[0].Date;
            if (DateFlyout.IsOpen)
            {
                DateFlyout.Hide();
            }

            _currentDate = selectedDate.Date;
            await LoadHomeworkAsync(_currentDate);
        }

        private void UpdateDateDisplay()
        {
            CurrentDateTitleText.Text = _currentDate.Date == DateTime.Today.Date
                ? "今日作业"
                : _currentDate.ToString("yyyy年M月d日");

            TodayKeyText.Text = _currentDate.Date == DateTime.Today.Date
                ? $"{_currentDate:yyyy-MM-dd} · {_currentDate:dddd} · 点击选择日期"
                : $"{_currentDate:yyyy-MM-dd} · {_currentDate:dddd} · 点击切换日期";

            _isUpdatingCalendarSelection = true;
            DateCalendarView.SelectedDates.Clear();
            DateCalendarView.SelectedDates.Add(_currentDate);
            DateCalendarView.SetDisplayDate(_currentDate);
            _isUpdatingCalendarSelection = false;
        }

        private async Task LoadHomeworkAsync(DateTime date)
        {
            _currentDate = date.Date;
            UpdateDateDisplay();

            var dateKey = $"classworks-data-{date:yyyyMMdd}";

            bool isToday = date.Date == DateTime.Now.Date;
            StatusText.Text = isToday
                ? "正在加载今日作业..."
                : $"正在加载 {date:yyyy-MM-dd} 的作业...";
            HomeworkContainer.Children.Clear();

            var responseBody = await SendKvRequestAsync(HttpMethod.Get, $"/kv/{Uri.EscapeDataString(dateKey)}");
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return;
            }

            _rawJson = responseBody;
            ShowHomework(responseBody);

            // 加载完成后，刷新未完成作业列表
            await LoadUndoneHomeworkAsync(responseBody);
        }

        private async Task<string?> SendKvRequestAsync(HttpMethod method, string path, string? jsonBody = null)
        {
            var token = AppSettings.Values[TokenSettingsKey] as string;
            if (string.IsNullOrWhiteSpace(token))
            {
                StatusText.Text = "本地没有 Token，请先完成初始化。";
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

                using var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                // 调试日志
                LogToDebugWindow(method.Method, path, (int)response.StatusCode, responseBody);

                if (!response.IsSuccessStatusCode)
                {
                    StatusText.Text = $"请求失败 ({(int)response.StatusCode})";
                    return null;
                }

                return responseBody;
            }
            catch (Exception ex)
            {
                StatusText.Text = "网络请求失败。";

                // 调试日志（记录错误）
                LogToDebugWindow(method.Method, path, 0, "", ex.Message);

                return null;
            }
        }

        private void LogToDebugWindow(string method, string path, int statusCode, string responseBody, string? errorMessage = null)
        {
            if (!DebugWindow.IsDebugModeEnabled())
                return;

            // 确保调试窗口已创建
            if (_debugWindow == null)
            {
                _debugWindow = new DebugWindow();
                _debugWindow.Closed += (_, _) => _debugWindow = null;
            }

            _debugWindow.Activate();
            _debugWindow.AppendLog(method, path, statusCode, responseBody, errorMessage);
        }

        private void ShowHomework(string json)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                if (!document.RootElement.TryGetProperty("homework", out var homework) || homework.ValueKind != JsonValueKind.Object)
                {
                    StatusText.Text = "暂无作业。";
                    HomeworkContainer.Children.Clear();
                    return;
                }

                var items = new List<HomeworkItem>();
                _currentHomeworkSubjects.Clear();
                foreach (var subject in homework.EnumerateObject())
                {
                    var content = subject.Value.ValueKind == JsonValueKind.Object && subject.Value.TryGetProperty("content", out var contentElement)
                        ? contentElement.GetString()
                        : subject.Value.ToString();

                    // 内容为空时不显示为卡片
                    if (string.IsNullOrWhiteSpace(content))
                        continue;

                    items.Add(new HomeworkItem
                    {
                        Subject = subject.Name,
                        Content = content
                    });
                    _currentHomeworkSubjects.Add(subject.Name);
                }

                HomeworkContainer.Children.Clear();
                StatusText.Text = items.Count == 0 ? "暂无作业。" : $"共 {items.Count} 项作业";

                // 更新轮播数据，退出轮播模式
                _carouselItems = items;
                _carouselTimer.Stop();
                CarouselOverlay.Visibility = Visibility.Collapsed;

                if (items.Count == 0) return;

                // 从设置读取卡片大小参数
                var settings = AppSettings.Values;
                double minCardWidth = (double)(settings["Settings_MinCardWidth"] ?? 220.0);
                double gap = (double)(settings["Settings_CardGap"] ?? 14.0);
                double subjectFontSize = (double)(settings["Settings_SubjectFontSize"] ?? 22.0);
                double contentFontSize = (double)(settings["Settings_ContentFontSize"] ?? 17.0);

                // 按可用宽度与内容长度分行，保证同排等高且偏宽内容能换到下一行
                double availableWidth = HomeworkContainer.ActualWidth;
                if (availableWidth <= 0) availableWidth = 800;

                var rows = BuildCardRows(items, availableWidth, minCardWidth, gap, contentFontSize);
                int animationIndex = 0;

                foreach (var rowItems in rows)
                {
                    var rowGrid = new Grid
                    {
                        ColumnSpacing = gap,
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    };

                    for (int column = 0; column < rowItems.Count; column++)
                    {
                        rowGrid.ColumnDefinitions.Add(new ColumnDefinition
                        {
                            Width = new GridLength(1, GridUnitType.Star)
                        });
                    }

                    for (int column = 0; column < rowItems.Count; column++)
                    {
                        var card = CreateCard(rowItems[column], subjectFontSize, contentFontSize, animationIndex++);
                        Grid.SetColumn(card, column);
                        rowGrid.Children.Add(card);
                    }

                    HomeworkContainer.Children.Add(rowGrid);
                    AnimationHelper.AnimateEntrance(rowGrid, fromY: 10f, durationMs: 260);
                }
            }
            catch (JsonException)
            {
                StatusText.Text = "作业数据格式错误。";
                HomeworkContainer.Children.Clear();
            }
        }

        private static List<List<HomeworkItem>> BuildCardRows(
            List<HomeworkItem> items,
            double availableWidth,
            double minCardWidth,
            double gap,
            double contentFontSize)
        {
            var rows = new List<List<HomeworkItem>>();
            var currentRow = new List<HomeworkItem>();
            double currentRowWidth = 0;

            foreach (var item in items)
            {
                double estimatedWidth = EstimateCardWidth(item, minCardWidth, availableWidth, contentFontSize);
                double nextRowWidth = currentRow.Count == 0
                    ? estimatedWidth
                    : currentRowWidth + gap + estimatedWidth;

                bool exceedsWidth = nextRowWidth > availableWidth;
                bool exceedsMaxColumns = currentRow.Count >= 4;

                if (currentRow.Count > 0 && (exceedsWidth || exceedsMaxColumns))
                {
                    rows.Add(currentRow);
                    currentRow = new List<HomeworkItem>();
                    currentRowWidth = 0;
                }

                currentRow.Add(item);
                currentRowWidth = currentRow.Count == 1
                    ? estimatedWidth
                    : currentRowWidth + gap + estimatedWidth;
            }

            if (currentRow.Count > 0)
            {
                rows.Add(currentRow);
            }

            return rows;
        }

        private static double EstimateCardWidth(HomeworkItem item, double minCardWidth, double availableWidth, double contentFontSize)
        {
            int longestLineLength = item.Content
                .Split('\n')
                .Select(line => line.Trim().Length)
                .DefaultIfEmpty(0)
                .Max();

            int titleLength = item.Subject.Trim().Length;
            int referenceLength = Math.Max(longestLineLength, titleLength);

            double extraWidth = Math.Max(0, referenceLength - 16) * Math.Max(5, contentFontSize * 0.45);
            double desiredWidth = minCardWidth + extraWidth;
            double maxWidth = Math.Max(minCardWidth, availableWidth);

            return Math.Clamp(desiredWidth, minCardWidth, maxWidth);
        }

        private Button CreateCard(HomeworkItem item, double subjectFontSize, double contentFontSize, int index)
        {
            var border = new Border
            {
                Padding = new Thickness(22),
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                CornerRadius = new CornerRadius(10),
                Translation = new Vector3(0, 0, 16),
                VerticalAlignment = VerticalAlignment.Stretch
            };

            border.Shadow = new ThemeShadow();
            AnimationHelper.AnimateEntrance(border, fromY: 18f, durationMs: 320, delayMs: Math.Min(index, 10) * 35);

            var stack = new StackPanel { Spacing = 10 };
            stack.Children.Add(new TextBlock
            {
                FontSize = subjectFontSize,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Text = item.Subject
            });
            stack.Children.Add(new TextBlock
            {
                FontSize = contentFontSize,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                Text = item.Content,
                TextWrapping = TextWrapping.WrapWholeWords
            });

            border.Child = stack;

            var button = new Button
            {
                Content = border,
                Tag = item,
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };
            button.Click += CardButton_Click;

            return button;
        }

        private async void CardButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not HomeworkItem item)
                return;

            var editBox = new TextBox
            {
                Text = item.Content,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 300,
                PlaceholderText = "修改作业内容..."
            };

            var dialog = new ContentDialog
            {
                Title = $"修改 {item.Subject} 作业",
                Content = editBox,
                PrimaryButtonText = "保存",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = button.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await SaveHomeworkAsync(item.Subject, editBox.Text);
            }
        }

        private async Task SaveHomeworkAsync(string subject, string newContent)
        {
            if (string.IsNullOrWhiteSpace(_rawJson))
                return;

            try
            {
                using var document = JsonDocument.Parse(_rawJson);
                var root = new Dictionary<string, JsonElement>();
                foreach (var prop in document.RootElement.EnumerateObject())
                    root[prop.Name] = prop.Value;

                // 构建新的 homework 对象
                var homeworkDict = new Dictionary<string, object>();
                if (document.RootElement.TryGetProperty("homework", out var homeworkElement) && homeworkElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var subj in homeworkElement.EnumerateObject())
                    {
                        if (subj.Name == subject)
                        {
                            homeworkDict[subj.Name] = new Dictionary<string, object> { ["content"] = newContent };
                        }
                        else
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
                }

                // 构建 attendance 对象
                var attendanceDict = new Dictionary<string, object>();
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

                var payload = new Dictionary<string, object>
                {
                    ["homework"] = homeworkDict,
                    ["attendance"] = attendanceDict
                };

                var json = JsonSerializer.Serialize(payload, AppJsonSerializerContext.Default.DictionaryStringObject);
                var dateKey = $"classworks-data-{_currentDate:yyyyMMdd}";
                var response = await SendKvRequestAsync(HttpMethod.Post, $"/kv/{Uri.EscapeDataString(dateKey)}", json);

                if (response != null)
                {
                    _rawJson = json;
                    await LoadHomeworkAsync(_currentDate);
                }
            }
            catch (Exception)
            {
                StatusText.Text = "保存作业失败。";
            }
        }

        // ========== 轮播功能 ==========

        private void ToggleCarouselButton_Click(object sender, RoutedEventArgs e)
        {
            if (_carouselItems.Count == 0)
            {
                StatusText.Text = "暂无作业可供轮播。";
                return;
            }

            _carouselIndex = 0;
            ShowCarouselItem();
            StartCarouselTimer();
            ShowCarouselOverlay();
        }

        private async void ExitCarouselButton_Click(object sender, RoutedEventArgs e)
        {
            _carouselTimer.Stop();
            await HideCarouselOverlayAsync();
        }

        private void CarouselOverlay_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // 点击内容切换到下一项
            if (_carouselItems.Count > 0)
            {
                _carouselIndex = (_carouselIndex + 1) % _carouselItems.Count;
                ShowCarouselItem();
            }
        }

        private void StartCarouselTimer()
        {
            _carouselTimer.Stop();
            var settings = AppSettings.Values;
            double interval = (double)(settings[CarouselIntervalKey] ?? 5.0);
            _carouselTimer.Interval = TimeSpan.FromSeconds(interval);
            _carouselTimer.Tick -= CarouselTimer_Tick;
            _carouselTimer.Tick += CarouselTimer_Tick;
            _carouselTimer.Start();
        }

        private void CarouselTimer_Tick(object? sender, object e)
        {
            if (_carouselItems.Count > 0)
            {
                _carouselIndex = (_carouselIndex + 1) % _carouselItems.Count;
                ShowCarouselItem();
            }
        }

        private void ShowCarouselItem()
        {
            if (_carouselItems.Count == 0) return;

            var item = _carouselItems[_carouselIndex];
            var settings = AppSettings.Values;
            double carouselFontSize = (double)(settings[CarouselFontSizeKey] ?? 48.0);

            CarouselSubjectText.Text = item.Subject;
            CarouselSubjectText.FontSize = Math.Min(carouselFontSize * 1.3, 96);
            CarouselContentText.Text = item.Content;
            CarouselContentText.FontSize = carouselFontSize;
            CarouselProgressText.Text = $"{_carouselIndex + 1} / {_carouselItems.Count}";

            AnimationHelper.AnimateEntrance(CarouselSubjectText, fromY: 10f, durationMs: 220);
            AnimationHelper.AnimateEntrance(CarouselContentText, fromY: 18f, durationMs: 260, delayMs: 40);
            AnimationHelper.AnimateEntrance(CarouselProgressText, fromY: 8f, durationMs: 200, delayMs: 80);
        }

        // ========== 未完成作业 ==========

        private async Task LoadUndoneHomeworkAsync(string currentHomeworkJson)
        {
            UndoneHomeworkPanel.Children.Clear();

            // 获取全部作业列表
            var listResponse = await SendKvRequestAsync(HttpMethod.Get, "/kv/classworks-config-subject");
            if (string.IsNullOrWhiteSpace(listResponse))
            {
                return;
            }

            try
            {
                using var document = JsonDocument.Parse(listResponse);
                var allHomework = new List<(int Order, string Name)>();

                foreach (var element in document.RootElement.EnumerateArray())
                {
                    if (element.TryGetProperty("order", out var orderElement) &&
                        element.TryGetProperty("name", out var nameElement))
                    {
                        int order = orderElement.GetInt32();
                        string name = nameElement.GetString() ?? "";
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            allHomework.Add((order, name));
                        }
                    }
                }

                // 过滤出未完成的作业（不在当前作业科目中的）
                var undoneHomework = allHomework
                    .Where(h => !_currentHomeworkSubjects.Contains(h.Name))
                    .ToList();

                if (undoneHomework.Count == 0)
                {
                    UndoneHomeworkPanel.Visibility = Visibility.Collapsed;
                    return;
                }

                UndoneHomeworkPanel.Visibility = Visibility.Visible;

                for (int index = 0; index < undoneHomework.Count; index++)
                {
                    var (order, name) = undoneHomework[index];
                    var button = new Button
                    {
                        Content = $"#{order} {name}",
                        Tag = name,
                        MinWidth = 100
                    };
                    button.Click += UndoneHomeworkButton_Click;
                    AnimationHelper.AttachHoverAnimation(button, 1.02f, 0.985f, -2f);
                    AnimationHelper.AnimateEntrance(button, fromY: 10f, durationMs: 240, delayMs: Math.Min(index, 8) * 30);
                    UndoneHomeworkPanel.Children.Add(button);
                }
            }
            catch (JsonException)
            {
                // 静默失败
            }
        }

        private async void UndoneHomeworkButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string homeworkName)
                return;

            var xamlRoot = button.XamlRoot;

            // 弹出编辑对话框，和正常修改作业流一致
            var editBox = new TextBox
            {
                Text = "",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 300,
                PlaceholderText = "输入作业内容..."
            };

            var dialog = new ContentDialog
            {
                Title = $"添加 {homeworkName} 作业",
                Content = editBox,
                PrimaryButtonText = "保存",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = xamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
                return;

            var newContent = editBox.Text;

            // 构建新的 homework 对象
            try
            {
                var homeworkDict = new Dictionary<string, object>();

                if (!string.IsNullOrWhiteSpace(_rawJson))
                {
                    using var document = JsonDocument.Parse(_rawJson);
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
                }

                // 添加新作业
                homeworkDict[homeworkName] = new Dictionary<string, object> { ["content"] = newContent };

                // 构建 attendance 对象
                var attendanceDict = new Dictionary<string, object>();
                if (!string.IsNullOrWhiteSpace(_rawJson))
                {
                    using var document = JsonDocument.Parse(_rawJson);
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

                var payload = new Dictionary<string, object>
                {
                    ["homework"] = homeworkDict,
                    ["attendance"] = attendanceDict
                };

                var json = JsonSerializer.Serialize(payload, AppJsonSerializerContext.Default.DictionaryStringObject);
                var dateKey = $"classworks-data-{_currentDate:yyyyMMdd}";
                var response = await SendKvRequestAsync(HttpMethod.Post, $"/kv/{Uri.EscapeDataString(dateKey)}", json);

                if (response != null)
                {
                    _rawJson = json;
                    await LoadHomeworkAsync(_currentDate);
                }
            }
            catch (Exception)
            {
                StatusText.Text = "添加作业失败。";
            }
        }

        // ========== 随机抽取学生 ==========

        private async void PickRandomStudentButton_Click(object sender, RoutedEventArgs e)
        {
            var responseBody = await SendKvRequestAsync(HttpMethod.Get, "/kv/classworks-list-main");
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                StatusText.Text = "获取学生列表失败。";
                return;
            }

            try
            {
                using var document = JsonDocument.Parse(responseBody);
                var students = new List<string>();

                foreach (var element in document.RootElement.EnumerateArray())
                {
                    if (element.TryGetProperty("name", out var nameElement))
                    {
                        var name = nameElement.GetString();
                        if (!string.IsNullOrWhiteSpace(name))
                            students.Add(name);
                    }
                }

                if (students.Count == 0)
                {
                    StatusText.Text = "学生列表为空。";
                    return;
                }

                var random = new Random();
                var xamlRoot = ((Button)sender).XamlRoot;

                // 使用循环而非递归，避免 ContentDialog 并发冲突
                while (true)
                {
                    var picked = students[random.Next(students.Count)];

                    var dialog = new ContentDialog
                    {
                        Title = "随机抽取结果",
                        Content = new StackPanel
                        {
                            Spacing = 12,
                            Children =
                            {
                                new TextBlock
                                {
                                    Text = $"共 {students.Count} 名学生",
                                    FontSize = 14,
                                    Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
                                },
                                new TextBlock
                                {
                                    Text = picked,
                                    FontSize = 48,
                                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    Margin = new Thickness(0, 8, 0, 8)
                                },
                                new TextBlock
                                {
                                    Text = "恭喜被抽中！",
                                    FontSize = 16,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
                                }
                            }
                        },
                        PrimaryButtonText = "重新抽取",
                        CloseButtonText = "确定",
                        DefaultButton = ContentDialogButton.Primary,
                        XamlRoot = xamlRoot
                    };

                    var result = await dialog.ShowAsync();
                    if (result != ContentDialogResult.Primary)
                        break;
                    // 如果点击"重新抽取"，继续循环重新抽取
                }
            }
            catch (JsonException)
            {
                StatusText.Text = "学生列表数据格式错误。";
            }
        }

        private void ShowCarouselOverlay()
        {
            _carouselOverlayAnimationToken++;
            CarouselOverlay.Visibility = Visibility.Visible;
            AnimationHelper.AnimateOpacity(CarouselOverlay, 0f, 1f, 180);
            AnimationHelper.AnimateEntrance(CarouselContentPanel, fromY: 24f, durationMs: 280);
            AnimationHelper.AnimateEntrance(ExitCarouselButton, fromY: 12f, durationMs: 220);
        }

        private async Task HideCarouselOverlayAsync()
        {
            _carouselOverlayAnimationToken++;
            int animationToken = _carouselOverlayAnimationToken;
            AnimationHelper.AnimateOpacity(CarouselOverlay, 1f, 0f, 160);
            await Task.Delay(170);

            if (animationToken == _carouselOverlayAnimationToken)
            {
                CarouselOverlay.Visibility = Visibility.Collapsed;
            }
        }
    }
}
