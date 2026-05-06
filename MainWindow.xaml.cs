using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading;
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
        private const string ThemeModeKey = "Display_ThemeMode";
        private const string TimeCardEnabledKey = "Display_TimeCardEnabled";
        private const string EmptySubjectDisplayKey = "Display_EmptySubjectDisplay";
        private const string ShowRandomButtonKey = "Display_ShowRandomButton";
        private const string ShowFullscreenButtonKey = "Display_ShowFullscreenButton";
        private const string CardHoverEffectKey = "Display_CardHoverEffect";
        private const string EnhancedTouchModeKey = "Display_EnhancedTouchMode";
        private const string ShowAntiScreenBurnCardKey = "Display_ShowAntiScreenBurnCard";
        private const string ForceDesktopModeKey = "Display_ForceDesktopMode";
        private const string LateStudentsArePresentKey = "Display_LateStudentsArePresent";

        private readonly HttpClient _httpClient = new();
        private readonly SemaphoreSlim _homeworkSaveLock = new(1, 1);
        private int _loadingSequence = 0;
        private DateTime _currentDate = DateTime.Now;
        private string? _rawJson;
        private string _homeworkJsonDateKey = "";
        private readonly DispatcherTimer _autoRefreshTimer = new();
        private DispatcherTimer? _policyToastDismissTimer;
        private DateTime _lastNonTodayPolicyToastShownUtc = DateTime.MinValue;
        private List<HomeworkItem> _carouselItems = new();
        private int _carouselIndex = 0;
        private readonly DispatcherTimer _carouselTimer = new();
        private DebugWindow? _debugWindow;
        private SettingsWindow? _settingsWindow;
        private bool _isUpdatingCalendarSelection;
        private int _carouselOverlayAnimationToken;
        private bool _lateStudentsArePresent = false;
        private bool _isFullscreen = false;
        private SizeInt32 _preFullscreenSize = new(960, 720);

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
            ConfigureIntegratedTitleBar();

            try
            {
                AppWindow.SetIcon("Assets/StoreLogo.png");
            }
            catch { }

            RestoreWindowState();

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

        private void ConfigureIntegratedTitleBar()
        {
            if (!AppWindowTitleBar.IsCustomizationSupported())
            {
                return;
            }

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            UpdateTitleBarLayout(AppWindow.TitleBar);
        }

        private void UpdateTitleBarLayout(AppWindowTitleBar titleBar)
        {
            LeftInsetColumn.Width = new GridLength(titleBar.LeftInset);
            RightInsetColumn.Width = new GridLength(titleBar.RightInset);
        }

        private void ApplyDisplaySettings()
        {
            var settings = AppSettings.Values;

            var showRandom = settings.ContainsKey(ShowRandomButtonKey) && (bool)(settings[ShowRandomButtonKey] ?? true);
            PickRandomStudentButton.Visibility = showRandom ? Visibility.Visible : Visibility.Collapsed;

            var showFullscreen = settings.ContainsKey(ShowFullscreenButtonKey) && (bool)(settings[ShowFullscreenButtonKey] ?? true);
            ToggleFullscreenButton.Visibility = showFullscreen ? Visibility.Visible : Visibility.Collapsed;

            var cardHover = settings.ContainsKey(CardHoverEffectKey) && (bool)(settings[CardHoverEffectKey] ?? true);
            if (cardHover)
            {
                AddCardHoverEffects();
            }
            else
            {
                RemoveCardHoverEffects();
            }

            var showAntiScreenBurn = settings.ContainsKey(ShowAntiScreenBurnCardKey) && (bool)(settings[ShowAntiScreenBurnCardKey] ?? false);
            if (showAntiScreenBurn)
            {
                ShowAntiScreenBurnToast();
            }

            var themeMode = settings[ThemeModeKey] as string ?? "dark";
            try
            {
                if (Application.Current is Application app)
                {
                    app.RequestedTheme = themeMode == "dark" ? ApplicationTheme.Dark : ApplicationTheme.Light;
                }
            }
            catch (System.Runtime.InteropServices.COMException)
            {
            }

            var timeCardEnabled = settings.ContainsKey(TimeCardEnabledKey) && (bool)(settings[TimeCardEnabledKey] ?? false);
            if (timeCardEnabled)
            {
                StartTimeCard();
            }
            else
            {
                StopTimeCard();
            }

            var forceDesktopMode = settings.ContainsKey(ForceDesktopModeKey) && (bool)(settings[ForceDesktopModeKey] ?? false);
            var enhancedTouch = settings.ContainsKey(EnhancedTouchModeKey) && (bool)(settings[EnhancedTouchModeKey] ?? false);
            var scalingEnabled = forceDesktopMode || enhancedTouch;

            double scale = scalingEnabled ? 1.25 : 1.0;

            var compositeTransform = new Microsoft.UI.Xaml.Media.CompositeTransform
            {
                ScaleX = scale,
                ScaleY = scale
            };
            ToggleFullscreenButton.RenderTransform = compositeTransform;
            PickRandomStudentButton.RenderTransform = compositeTransform;
            ToggleCarouselButton.RenderTransform = compositeTransform;
            SyncButton.RenderTransform = compositeTransform;
            PrevDateButton.RenderTransform = compositeTransform;
            NextDateButton.RenderTransform = compositeTransform;
            TutorialButton.RenderTransform = compositeTransform;
            AboutButton.RenderTransform = compositeTransform;

            if (forceDesktopMode)
            {
                var newWidth = (int)(_preFullscreenSize.Width * scale);
                var newHeight = (int)(_preFullscreenSize.Height * scale);
                this.AppWindow.Resize(new SizeInt32(Math.Max(newWidth, 960), Math.Max(newHeight, 600)));
            }

            var lateStudentsArePresent = settings.ContainsKey(LateStudentsArePresentKey) && (bool)(settings[LateStudentsArePresentKey] ?? false);
            _lateStudentsArePresent = lateStudentsArePresent;

            if (!string.IsNullOrWhiteSpace(_rawJson))
            {
                ShowHomework(_rawJson, animate: false);
            }
        }

        private DispatcherTimer? _timeCardTimer;

        private void StartTimeCard()
        {
            TimeCard.Visibility = Visibility.Visible;
            _timeCardTimer?.Stop();
            _timeCardTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timeCardTimer.Tick += (_, _) => UpdateTimeCard();
            _timeCardTimer.Start();
            UpdateTimeCard();
        }

        private void StopTimeCard()
        {
            TimeCard.Visibility = Visibility.Collapsed;
            _timeCardTimer?.Stop();
            _timeCardTimer = null;
        }

        private void UpdateTimeCard()
        {
            var now = DateTime.Now;
            TimeCardClockText.Text = now.ToString("HH:mm:ss");
            TimeCardDateText.Text = now.ToString("yyyy年M月d日 dddd", new System.Globalization.CultureInfo("zh-CN"));
        }

        private void ToggleFullscreenButton_Click(object sender, RoutedEventArgs e)
        {
            var presenter = this.AppWindow.Presenter as OverlappedPresenter;
            if (presenter == null) return;

            if (!_isFullscreen)
            {
                _preFullscreenSize = this.AppWindow.Size;
                presenter.IsAlwaysOnTop = true;
                presenter.SetBorderAndTitleBar(false, false);
                this.AppWindow.Resize(new SizeInt32(1920, 1080));
                _isFullscreen = true;
                ToggleFullscreenButton.Content = "退出全屏";
            }
            else
            {
                presenter.IsAlwaysOnTop = false;
                presenter.SetBorderAndTitleBar(true, true);
                this.AppWindow.Resize(_preFullscreenSize);
                _isFullscreen = false;
                ToggleFullscreenButton.Content = "全屏";
            }
        }

        private void AddCardHoverEffects()
        {
            foreach (var child in HomeworkContainer.Children)
            {
                if (child is Grid rowGrid)
                {
                    foreach (var card in rowGrid.Children)
                    {
                        if (card is Button cardButton && cardButton.Content is Border cardBorder)
                        {
                            cardButton.PointerEntered -= CardBorder_PointerEntered;
                            cardButton.PointerEntered += CardBorder_PointerEntered;
                            cardButton.PointerExited -= CardBorder_PointerExited;
                            cardButton.PointerExited += CardBorder_PointerExited;
                            cardButton.Tapped -= CardBorder_Tapped;
                            cardButton.Tapped += CardBorder_Tapped;
                        }
                    }
                }
            }
        }

        private void RemoveCardHoverEffects()
        {
            foreach (var child in HomeworkContainer.Children)
            {
                if (child is Grid rowGrid)
                {
                    foreach (var card in rowGrid.Children)
                    {
                        if (card is Button cardButton && cardButton.Content is Border cardBorder)
                        {
                            cardButton.PointerEntered -= CardBorder_PointerEntered;
                            cardButton.PointerExited -= CardBorder_PointerExited;
                            cardButton.Tapped -= CardBorder_Tapped;
                        }
                    }
                }
            }
        }

        private void CardBorder_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is Button btn && btn.Content is Border brd)
            {
                brd.Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"];
                brd.RenderTransform = new Microsoft.UI.Xaml.Media.TranslateTransform { Y = -4 };
            }
        }

        private void CardBorder_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is Button btn && btn.Content is Border brd)
            {
                brd.Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"];
                brd.RenderTransform = new Microsoft.UI.Xaml.Media.TranslateTransform { Y = 0 };
            }
        }

        private void CardBorder_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (sender is Button btn && btn.Content is Border brd)
            {
                brd.RenderTransform = new Microsoft.UI.Xaml.Media.TranslateTransform { Y = 0 };
            }
        }

        private void ShowAntiScreenBurnToast()
        {
            if (PolicyToastRoot == null || PolicyToastTitle == null || PolicyToastSubtitle == null)
                return;

            PolicyToastTitle.Text = "防烧屏保护";
            PolicyToastSubtitle.Text = "此提示有助于防止屏幕烧屏，感谢使用！";
            PolicyToastRoot.Visibility = Visibility.Visible;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                PolicyToastRoot.Visibility = Visibility.Collapsed;
            };
            timer.Start();
        }

        private void RootContent_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement rootContent)
            {
                rootContent.Loaded -= RootContent_Loaded;
                AnimationHelper.AnimateEntrance(rootContent, fromY: 16f, durationMs: 380);
                AnimationHelper.ApplyStandardInteractions(rootContent);
            }

            _ = PullEditPreferencesFromCloudOnStartupAsync();
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
            _ = RefreshAllGlobalComponentsAsync();
        }

        private async Task RefreshAllGlobalComponentsAsync(bool animate = true)
        {
            await LoadHomeworkAsync(_currentDate, animate);
        }

        private async Task RefreshDataOnlyAsync()
        {
            var dateKey = GetCurrentHomeworkDateKey();
            var (responseBody, dayNotFound) = await GetClassworksDayOrNotFoundAsync(dateKey);

            await _homeworkSaveLock.WaitAsync();
            try
            {
                if (dayNotFound)
                {
                    _rawJson = """{"homework":{},"attendance":{}}""";
                    _homeworkJsonDateKey = dateKey;
                }
                else if (!string.IsNullOrWhiteSpace(responseBody))
                {
                    _rawJson = responseBody;
                    _homeworkJsonDateKey = dateKey;
                }
            }
            finally
            {
                _homeworkSaveLock.Release();
            }
        }

        private async Task<bool> ConfirmStartNonTodayEditIfNeededAsync(XamlRoot xamlRoot)
        {
            if (_currentDate.Date == DateTime.Today.Date)
                return true;

            var settings = AppSettings.Values;
            if (!settings.ContainsKey(EditPreferencesKeys.ConfirmNonTodaySave) || !(bool)(settings[EditPreferencesKeys.ConfirmNonTodaySave] ?? false))
                return true;

            var dialog = new ContentDialog
            {
                Title = "非今日日期",
                Content = "当前查看的不是今天，编辑内容将写入所选日期。是否继续？",
                PrimaryButtonText = "继续",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = xamlRoot
            };

            return await dialog.ShowAsync() == ContentDialogResult.Primary;
        }

        private async Task PullEditPreferencesFromCloudOnStartupAsync()
        {
            try
            {
                await Task.Delay(600).ConfigureAwait(false);
                var token = AppSettings.Values[TokenSettingsKey] as string;
                if (string.IsNullOrWhiteSpace(token))
                    return;

                await EditPreferencesSync.TryPullMergeIntoAppSettingsAsync(_httpClient, BaseUrl, token).ConfigureAwait(false);
            }
            catch
            {
            }
        }

        private string GetCurrentHomeworkDateKey() => $"classworks-data-{_currentDate:yyyyMMdd}";

        private void RedrawHomeworkFromRawJson(bool animate = true)
        {
            if (!string.IsNullOrWhiteSpace(_rawJson))
                ShowHomework(_rawJson, animate);
        }

        private async Task DispatchUiAsync(Func<Task> uiWork)
        {
            var dq = DispatcherQueue;
            if (dq.HasThreadAccess)
            {
                await uiWork().ConfigureAwait(true);
                return;
            }

            var tcs = new TaskCompletionSource<object?>();
            if (!dq.TryEnqueue(() => { _ = RunUiWorkAsync(uiWork, tcs); }))
                tcs.TrySetException(new InvalidOperationException("DispatcherQueue.TryEnqueue failed"));

            await tcs.Task.ConfigureAwait(true);
        }

        private static async Task RunUiWorkAsync(Func<Task> uiWork, TaskCompletionSource<object?> tcs)
        {
            try
            {
                await uiWork().ConfigureAwait(true);
                tcs.TrySetResult(null);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }

        private bool IsNonTodayWriteBlocked()
        {
            if (_currentDate.Date == DateTime.Today.Date)
                return false;
            var s = AppSettings.Values;
            return s.ContainsKey(EditPreferencesKeys.BlockNonTodayAutoSave) && (bool)(s[EditPreferencesKeys.BlockNonTodayAutoSave] ?? false);
        }

        private void TryShowNonTodayWriteBlockedToast()
        {
            if (PolicyToastRoot == null || PolicyToastTitle == null || PolicyToastSubtitle == null)
                return;

            if ((DateTime.UtcNow - _lastNonTodayPolicyToastShownUtc).TotalSeconds < 1.2)
                return;
            _lastNonTodayPolicyToastShownUtc = DateTime.UtcNow;

            PolicyToastTitle.Text = "当前禁止写入非当天的作业数据";
            PolicyToastSubtitle.Text = "可在设置 > 编辑里修改此设置";

            _policyToastDismissTimer?.Stop();
            _policyToastDismissTimer = null;

            var toastVisual = ElementCompositionPreview.GetElementVisual(PolicyToastRoot);
            toastVisual.Offset = new Vector3(0, 0, 0);
            toastVisual.Opacity = 1f;

            PolicyToastRoot.Opacity = 1;
            PolicyToastRoot.Visibility = Visibility.Visible;
            AnimationHelper.AnimateEntrance(PolicyToastRoot, fromY: 16f, fromOpacity: 0f, durationMs: 280);

            var dismiss = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5.5) };
            _policyToastDismissTimer = dismiss;
            dismiss.Tick += (_, _) =>
            {
                dismiss.Stop();
                _policyToastDismissTimer = null;
                if (PolicyToastRoot == null)
                    return;
                AnimationHelper.AnimateToOpacity(PolicyToastRoot, 0f, 220);
                var hide = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(260) };
                hide.Tick += (_, _) =>
                {
                    hide.Stop();
                    if (PolicyToastRoot != null)
                    {
                        ElementCompositionPreview.GetElementVisual(PolicyToastRoot).Offset = new Vector3(0, 0, 0);
                        PolicyToastRoot.Visibility = Visibility.Collapsed;
                    }
                };
                hide.Start();
            };
            dismiss.Start();
        }

        private async Task<bool> MergeAndPostHomeworkAsync(string subject, string content, CancellationToken cancellationToken = default)
        {
            if (IsNonTodayWriteBlocked())
            {
                await DispatchUiAsync(async () =>
                {
                    StatusText.Text = "已禁止写入非当天的作业数据。";
                    TryShowNonTodayWriteBlockedToast();
                    await Task.CompletedTask;
                }).ConfigureAwait(false);
                return false;
            }

            await _homeworkSaveLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var dateKey = GetCurrentHomeworkDateKey();
                var json = HomeworkPayloadMerge.BuildPostJson(
                    _rawJson,
                    string.Equals(_homeworkJsonDateKey, dateKey, StringComparison.Ordinal),
                    subject,
                    content);

                var response = await SendKvRequestAsync(HttpMethod.Post, $"/kv/{Uri.EscapeDataString(dateKey)}", json, cancellationToken).ConfigureAwait(false);
                if (response == null)
                    return false;

                _rawJson = json;
                _homeworkJsonDateKey = dateKey;

                var seq = _loadingSequence;
                await DispatchUiAsync(async () =>
                {
                    RedrawHomeworkFromRawJson(animate: false);
                    await LoadUndoneHomeworkAsync(seq, animate: false).ConfigureAwait(true);
                }).ConfigureAwait(false);

                return true;
            }
            finally
            {
                _homeworkSaveLock.Release();
            }
        }

        private async Task RunHomeworkEditorDialogAsync(XamlRoot xamlRoot, string subject, string initialContent, bool isAddingNew)
        {
            var appSettings = AppSettings.Values;

            if (appSettings.ContainsKey(EditPreferencesKeys.RefreshBeforeEdit) && (bool)(appSettings[EditPreferencesKeys.RefreshBeforeEdit] ?? false))
                _ = RefreshDataOnlyAsync();

            if (IsNonTodayWriteBlocked())
            {
                var blocked = new ContentDialog
                {
                    Title = "当前禁止写入非当天的作业数据",
                    Content = "请切换到「今天」后再添加或修改作业；也可在「设置 → 编辑」中关闭此限制。",
                    CloseButtonText = "知道了",
                    XamlRoot = xamlRoot
                };
                await blocked.ShowAsync();
                return;
            }

            if (!await ConfirmStartNonTodayEditIfNeededAsync(xamlRoot))
                return;

            bool autoSaveOn = appSettings.ContainsKey(EditPreferencesKeys.AutoSave) && (bool)(appSettings[EditPreferencesKeys.AutoSave] ?? false);
            bool allowAutoSave = autoSaveOn;

            var promptRaw = (autoSaveOn
                ? appSettings[EditPreferencesKeys.AutoSavePromptText] as string
                : appSettings[EditPreferencesKeys.ManualSavePromptText] as string)?.Trim() ?? "";

            var editBox = new TextBox
            {
                Text = initialContent,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 300,
                PlaceholderText = isAddingNew ? "输入作业内容..." : "修改作业内容..."
            };

            var hintBlock = new TextBlock
            {
                Text = promptRaw,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            };
            if (string.IsNullOrEmpty(promptRaw))
                hintBlock.Visibility = Visibility.Collapsed;

            var dialogContent = new StackPanel { Spacing = 8 };
            dialogContent.Children.Add(hintBlock);
            dialogContent.Children.Add(editBox);

            var dialog = new ContentDialog
            {
                Title = isAddingNew ? $"添加 {subject} 作业" : $"修改 {subject} 作业",
                Content = dialogContent,
                PrimaryButtonText = autoSaveOn ? "完成" : "保存",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = xamlRoot
            };

            DispatcherTimer? debounce = null;
            var lastPosted = initialContent;

            if (allowAutoSave)
            {
                debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(900) };
                debounce.Tick += async (_, _) =>
                {
                    debounce.Stop();
                    try
                    {
                        var t = editBox.Text;
                        if (string.Equals(t, lastPosted, StringComparison.Ordinal))
                            return;

                        if (await MergeAndPostHomeworkAsync(subject, t))
                            lastPosted = t;
                    }
                    catch
                    {
                    }
                };

                editBox.TextChanged += (_, _) =>
                {
                    debounce!.Stop();
                    debounce.Start();
                };
            }

            var result = await dialog.ShowAsync();
            debounce?.Stop();

            if (result != ContentDialogResult.Primary)
                return;

            await MergeAndPostHomeworkAsync(subject, editBox.Text);
        }

        private async void RefreshHomeworkButton_Click(object sender, RoutedEventArgs e)
        {
            var originalBackground = SyncButton.Background;
            var originalForeground = SyncButton.Foreground;
            var success = await LoadHomeworkAsync(_currentDate);

            if (success)
            {
                SyncButton.Background = new SolidColorBrush(ColorHelper.FromArgb(255, 0x67, 0xAC, 0x5B));
                StatusText.Text = "同步成功";
            }
            else
            {
                SyncButton.Background = new SolidColorBrush(ColorHelper.FromArgb(255, 0xC0, 0x6D, 0x7A));
                StatusText.Text = "同步失败";
            }
            SyncButton.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);

            _ = Task.Delay(2000).ContinueWith(_ =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    SyncButton.Background = originalBackground;
                    SyncButton.Foreground = originalForeground;
                });
            });
        }

        private void OpenSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsWindow == null)
            {
                _settingsWindow = new SettingsWindow(() =>
                {
                    RestartAutoRefreshTimer();
                    _ = RefreshAllGlobalComponentsAsync();

                    if (!DebugWindow.IsDebugModeEnabled() && _debugWindow != null)
                    {
                        _debugWindow.Close();
                        _debugWindow = null;
                    }

                    DispatcherQueue.TryEnqueue(() =>
                    {
                        ApplyDisplaySettings();
                    });
                });
                _settingsWindow.Closed += (_, _) => _settingsWindow = null;
            }

            _settingsWindow.Activate();
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

        private async Task<bool> LoadHomeworkAsync(DateTime date, bool animate = true)
        {
            int currentSequence = ++_loadingSequence;

            _currentDate = date.Date;
            UpdateDateDisplay();

            var dateKey = $"classworks-data-{date:yyyyMMdd}";

            bool isToday = date.Date == DateTime.Now.Date;
            StatusText.Text = isToday
                ? "正在加载今日作业..."
                : $"正在加载 {date:yyyy-MM-dd} 的作业...";
            HomeworkContainer.Children.Clear();

            var (responseBody, dayNotFound) = await GetClassworksDayOrNotFoundAsync(dateKey);

            if (currentSequence != _loadingSequence)
                return false;

            if (dayNotFound)
            {
                _rawJson = """{"homework":{},"attendance":{}}""";
                _homeworkJsonDateKey = dateKey;
                _currentHomeworkSubjects.Clear();
                StatusText.Text = isToday
                    ? "当天暂无作业数据，可在未完成科目中布置。"
                    : $"{date:yyyy-MM-dd} 暂无作业数据。";
                ShowHomework(_rawJson, animate);
                await LoadUndoneHomeworkAsync(currentSequence, animate);
                return true;
            }

            if (string.IsNullOrWhiteSpace(responseBody))
            {
                _currentHomeworkSubjects.Clear();
                await LoadUndoneHomeworkAsync(currentSequence, animate);
                return false;
            }

            _rawJson = responseBody;
            _homeworkJsonDateKey = dateKey;
            ShowHomework(responseBody, animate);

            if (currentSequence != _loadingSequence)
                return false;

            await LoadUndoneHomeworkAsync(currentSequence, animate);
            return true;
        }

        private async Task<(string? Body, bool DayNotFound)> GetClassworksDayOrNotFoundAsync(string dateKey, CancellationToken cancellationToken = default)
        {
            var token = AppSettings.Values[TokenSettingsKey] as string;
            if (string.IsNullOrWhiteSpace(token))
            {
                StatusText.Text = "本地没有 Token，请先完成初始化。";
                return (null, false);
            }

            try
            {
                var path = $"/kv/{Uri.EscapeDataString(dateKey)}";
                using var request = new HttpRequestMessage(HttpMethod.Get, BaseUrl + path);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                LogToDebugWindow(request.Method.Method, path, (int)response.StatusCode, responseBody);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound &&
                    dateKey.StartsWith("classworks-data-", StringComparison.OrdinalIgnoreCase))
                {
                    StatusText.Text = "当天没有作业，请点击按钮布置";
                    return (null, true);
                }

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        StatusText.Text = "token配置错误，请去设置销毁重设";
                    else
                        StatusText.Text = $"请求失败 ({(int)response.StatusCode})";
                    return (null, false);
                }

                return (responseBody, false);
            }
            catch (Exception ex)
            {
                StatusText.Text = "网络请求失败。";
                LogToDebugWindow("GET", $"/kv/{dateKey}", 0, "", ex.Message);
                return (null, false);
            }
        }

        private async Task<string?> SendKvRequestAsync(HttpMethod method, string path, string? jsonBody = null, CancellationToken cancellationToken = default)
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

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync();

                LogToDebugWindow(method.Method, path, (int)response.StatusCode, responseBody);

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound && path.StartsWith("/kv/classworks-data-", StringComparison.OrdinalIgnoreCase))
                    {
                        StatusText.Text = "当天没有作业，请点击按钮布置";
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        StatusText.Text = "token配置错误，请去设置销毁重设";
                    }
                    else
                    {
                        StatusText.Text = $"请求失败 ({(int)response.StatusCode})";
                    }
                    return null;
                }

                return responseBody;
            }
            catch (Exception ex)
            {
                StatusText.Text = "网络请求失败。";

                LogToDebugWindow(method.Method, path, 0, "", ex.Message);

                return null;
            }
        }

        private void LogToDebugWindow(string method, string path, int statusCode, string responseBody, string? errorMessage = null)
        {
            if (!DebugWindow.IsDebugModeEnabled())
                return;

            if (_debugWindow == null)
            {
                _debugWindow = new DebugWindow();
                _debugWindow.Closed += (_, _) => _debugWindow = null;
            }

            _debugWindow.Activate();
            _debugWindow.AppendLog(method, path, statusCode, responseBody, errorMessage);
        }

        private void ShowHomework(string json, bool animate = true)
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

                var settings = AppSettings.Values;
                var emptySubjectDisplay = settings[EmptySubjectDisplayKey] as string ?? "卡片";

                if (items.Count == 0 && emptySubjectDisplay == "隐藏") return;

                if (items.Count == 0 && emptySubjectDisplay == "按钮")
                {
                    StatusText.Text = "暂无作业，去设置中添加吧";
                    HomeworkContainer.Children.Clear();
                    var addBtn = new Button
                    {
                        Content = "+ 点击添加作业",
                        FontSize = 16,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        CornerRadius = new CornerRadius(10)
                    };
                    addBtn.Click += async (s, e) =>
                    {
                        var dialog = new ContentDialog
                        {
                            Title = "添加作业科目",
                            PrimaryButtonText = "确定",
                            CloseButtonText = "取消",
                            DefaultButton = ContentDialogButton.Primary,
                            XamlRoot = addBtn.XamlRoot
                        };
                        var subjectInput = new TextBox
                        {
                            PlaceholderText = "输入科目名称",
                            HorizontalAlignment = HorizontalAlignment.Stretch
                        };
                        var contentInput = new TextBox
                        {
                            PlaceholderText = "输入作业内容",
                            AcceptsReturn = true,
                            TextWrapping = TextWrapping.Wrap,
                            MinHeight = 120,
                            HorizontalAlignment = HorizontalAlignment.Stretch
                        };
                        var sp = new StackPanel { Spacing = 12 };
                        sp.Children.Add(subjectInput);
                        sp.Children.Add(contentInput);
                        dialog.Content = sp;
                        var result = await dialog.ShowAsync();
                        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(subjectInput.Text))
                        {
                            await MergeAndPostHomeworkAsync(subjectInput.Text.Trim(), contentInput.Text.Trim());
                        }
                    };
                    AnimationHelper.AttachHoverAnimation(addBtn, 1.02f, 0.985f, -2f);
                    if (animate)
                        AnimationHelper.AnimateEntrance(addBtn, fromY: 10f, durationMs: 240);
                    var btnGrid = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch, MinHeight = 100 };
                    btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    Grid.SetColumn(addBtn, 0);
                    btnGrid.Children.Add(addBtn);
                    HomeworkContainer.Children.Add(btnGrid);
                    return;
                }

                if (items.Count == 0)
                {
                    var emptyMessage = new TextBlock
                    {
                        Text = "暂无作业，去设置中添加吧",
                        FontSize = 16,
                        Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 40, 0, 0)
                    };
                    HomeworkContainer.Children.Add(emptyMessage);
                    return;
                }

                _carouselItems = items;
                _carouselTimer.Stop();
                CarouselOverlay.Visibility = Visibility.Collapsed;

                double minCardWidth = (double)(settings["Settings_MinCardWidth"] ?? 220.0);
                double gap = (double)(settings["Settings_CardGap"] ?? 14.0);
                double subjectFontSize = (double)(settings["Settings_SubjectFontSize"] ?? 22.0);
                double contentFontSize = (double)(settings["Settings_ContentFontSize"] ?? 17.0);

                double availableWidth = HomeworkContainer.ActualWidth;
                if (availableWidth <= 0) availableWidth = 800;

                var rows = BuildCardRows(items, availableWidth, minCardWidth, gap, contentFontSize);
                int animationIndex = 0;

                if (!animate)
                {
                    HomeworkContainer.ChildrenTransitions = null;
                }

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
                        var card = CreateCard(rowItems[column], subjectFontSize, contentFontSize, animationIndex++, animate);
                        Grid.SetColumn(card, column);
                        rowGrid.Children.Add(card);
                    }

                    HomeworkContainer.Children.Add(rowGrid);
                    if (animate)
                        AnimationHelper.AnimateEntrance(rowGrid, fromY: 10f, durationMs: 260);
                }

                if (!animate)
                    DispatcherQueue.TryEnqueue(() => RestoreHomeworkContainerTransitions());
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

        private Button CreateCard(HomeworkItem item, double subjectFontSize, double contentFontSize, int index, bool animate = true)
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
            if (animate)
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

            await RunHomeworkEditorDialogAsync(button.XamlRoot, item.Subject, item.Content, isAddingNew: false);
        }

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

        private async Task LoadUndoneHomeworkAsync(int sequence, bool animate = true)
        {
            if (sequence != _loadingSequence)
                return;

            UndoneHomeworkPanel.Children.Clear();

            var listResponse = await SendKvRequestAsync(HttpMethod.Get, $"/kv/{ClassworksKvKeys.SubjectConfig}");
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

                var undoneHomework = allHomework
                    .Where(h => !_currentHomeworkSubjects.Contains(h.Name))
                    .ToList();

                if (sequence != _loadingSequence)
                    return;

                UndoneHomeworkPanel.Visibility = Visibility.Visible;

                if (!animate)
                {
                    UndoneHomeworkPanel.ChildrenTransitions = null;
                }

                if (undoneHomework.Count == 0)
                {
                    var placeholderButton = new Button
                    {
                        Content = "所有科目均已布置作业",
                        Tag = "no_undone",
                        MinWidth = 100
                    };
                    placeholderButton.Click += (s, e) =>
                    {
                        System.Diagnostics.Debug.WriteLine("所有科目已完成，暂无补做作业");
                    };
                    AnimationHelper.AttachHoverAnimation(placeholderButton, 1.02f, 0.985f, -2f);
                    if (animate)
                        AnimationHelper.AnimateEntrance(placeholderButton, fromY: 10f, durationMs: 240);
                    UndoneHomeworkPanel.Children.Add(placeholderButton);

                    if (!animate)
                        DispatcherQueue.TryEnqueue(() => RestoreUndonePanelTransitions());
                    return;
                }

                for (int index = 0; index < undoneHomework.Count; index++)
                {
                    if (sequence != _loadingSequence)
                        return;

                    var (_, name) = undoneHomework[index];
                    var button = new Button
                    {
                        Content = name,
                        Tag = name,
                        MinWidth = 100
                    };
                    button.Click += UndoneHomeworkButton_Click;
                    AnimationHelper.AttachHoverAnimation(button, 1.02f, 0.985f, -2f);
                    if (animate)
                        AnimationHelper.AnimateEntrance(button, fromY: 10f, durationMs: 240, delayMs: Math.Min(index, 8) * 30);
                    UndoneHomeworkPanel.Children.Add(button);
                }

                if (!animate)
                    DispatcherQueue.TryEnqueue(() => RestoreUndonePanelTransitions());
            }
            catch (JsonException)
            {
            }
        }

        private TransitionCollection? _savedUndonePanelTransitions;

        private void RestoreUndonePanelTransitions()
        {
            if (_savedUndonePanelTransitions == null)
            {
                _savedUndonePanelTransitions = new TransitionCollection
                {
                    new EntranceThemeTransition { IsStaggeringEnabled = true },
                    new RepositionThemeTransition()
                };
            }
            UndoneHomeworkPanel.ChildrenTransitions = _savedUndonePanelTransitions;
        }

        private TransitionCollection? _savedHomeworkContainerTransitions;

        private void RestoreHomeworkContainerTransitions()
        {
            if (_savedHomeworkContainerTransitions == null)
            {
                _savedHomeworkContainerTransitions = new TransitionCollection
                {
                    new EntranceThemeTransition { IsStaggeringEnabled = true },
                    new RepositionThemeTransition()
                };
            }
            HomeworkContainer.ChildrenTransitions = _savedHomeworkContainerTransitions;
        }

        private async void UndoneHomeworkButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string homeworkName)
                return;
            if (string.Equals(homeworkName, "no_undone", StringComparison.Ordinal))
                return;

            await RunHomeworkEditorDialogAsync(button.XamlRoot, homeworkName, "", isAddingNew: true);
        }

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
                                },
                                new TextBlock
                                {
                                    Text = _lateStudentsArePresent ? "注：迟到人数已算入出勤人数" : "注：迟到人数不计入出勤",
                                    FontSize = 12,
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
