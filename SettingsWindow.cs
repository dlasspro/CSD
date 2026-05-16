using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.System;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace CSD
{
    public class SettingsWindow : Window
    {
        private sealed class NavigationItemState
        {
            public required string CategoryKey { get; init; }
            public required Button Button { get; init; }
            public required Border HoverBackground { get; init; }
            public required TextBlock Label { get; init; }
            public required SolidColorBrush IconBrush { get; init; }
            public required SolidColorBrush LabelBrush { get; init; }
        }

        private const string MinCardWidthKey = "Settings_MinCardWidth";
        private const string CardGapKey = "Settings_CardGap";
        private const string SubjectFontSizeKey = "Settings_SubjectFontSize";
        private const string ContentFontSizeKey = "Settings_ContentFontSize";
        private const string ServerUrlKey = "Settings_ServerUrl";
        private const string AutoRefreshEnabledKey = "Settings_AutoRefreshEnabled";
        private const string AutoRefreshIntervalKey = "Settings_AutoRefreshInterval";
        private const string CarouselIntervalKey = "Settings_CarouselInterval";
        private const string CarouselFontSizeKey = "Settings_CarouselFontSize";
        private const string DebugModeKey = "Settings_DebugMode";
        private const string DataProviderKey = "Settings_DataProvider";
        private const string SubjectListKey = "Settings_SubjectList";
        private const string RosterListKey = "Settings_RosterList";
        private const string TokenKey = "Token";

        private const string RandomPickerEnabledKey = "randomPicker.enabled";
        private const string RandomPickerMinNumberKey = "randomPicker.minNumber";
        private const string RandomPickerMaxNumberKey = "randomPicker.maxNumber";
        private const string RandomPickerDefaultCountKey = "randomPicker.defaultCount";
        private const string RandomPickerAnimationKey = "randomPicker.animation";

        private const string AutoStartEnabledKey = "Settings_AutoStartEnabled";
        private const string AutoStartMethodKey = "Settings_AutoStartMethod";

        private static readonly string[] DefaultSubjectNames =
        [
            "语文", "数学", "英语", "物理", "化学", "生物", "政治", "历史", "地理", "其他"
        ];

        private static readonly string[] DataProviderOptions =
        [
            "Classworks 云端存储",
            "本地存储",
            "自定义远程服务器"
        ];

        private static readonly string[] AutoStartMethodOptions =
        [
            "注册表 (Registry)  ★★★",
            "启动文件夹 (Startup)  ★★",
            "计划任务 (Task Scheduler)  ★"
        ];

        private readonly Action _onSettingsChanged;

        private readonly NumberBox _minCardWidthBox;
        private readonly NumberBox _cardGapBox;
        private readonly NumberBox _subjectFontSizeBox;
        private readonly NumberBox _contentFontSizeBox;
        private readonly TextBox _serverUrlBox;
        private readonly ToggleSwitch _autoRefreshToggle;
        private readonly NumberBox _autoRefreshIntervalBox;
        private readonly NumberBox _carouselIntervalBox;
        private readonly NumberBox _carouselFontSizeBox;
        private readonly ToggleSwitch _debugModeToggle;
        private readonly ComboBox _dataProviderCombo;
        private readonly TextBox _kvTokenBox;
        private readonly TextBlock _deviceOwnerTitleText;
        private readonly TextBlock _deviceOwnerSubText;
        private readonly TextBlock _deviceNameText;
        private readonly TextBlock _deviceIdText;
        private readonly TextBlock _deviceCreatedText;
        private readonly TextBlock _deviceUpdatedText;
        private readonly ToggleSwitch _randomPickerEnabledToggle;
        private readonly NumberBox _randomPickerMinNumberBox;
        private readonly NumberBox _randomPickerMaxNumberBox;
        private readonly NumberBox _randomPickerDefaultCountBox;
        private readonly ToggleSwitch _randomPickerAnimationToggle;
        private readonly ToggleSwitch _autoStartToggle;
        private readonly ComboBox _autoStartMethodCombo;
        private readonly HttpClient _settingsHttpClient = new();
        private readonly TextBlock _currentTokenText;
        private readonly TextBlock _pageTitleText;
        private readonly TextBlock _pageDescriptionText;
        private readonly Grid _detailsHost;
        private readonly Button _destroyTokenButton;
        private readonly Button _exportButton;
        private readonly Button _importButton;
        private readonly Button _webSettingsButton;
        private readonly List<Button> _navigationButtons = new();
        private readonly Dictionary<string, NavigationItemState> _navigationItemStates = new();
        private readonly Dictionary<string, FrameworkElement> _categoryViews = new();
        private readonly Border _selectionHighlight;
        private readonly Grid _navigationItemsHost;
        private readonly StackPanel _navigationItemsPanel;
        private readonly ScrollViewer _navigationScrollViewer;
        private TextBox _subjectNameInput = null!;
        private StackPanel _subjectRowsPanel = null!;
        private readonly List<string> _managedSubjects = new();
        private int _subjectCloudPushGeneration;
        private TextBox _rosterNameInput = null!;
        private Grid _rosterCardsGrid = null!;
        private readonly List<string> _rosterStudents = new();
        private int _rosterCloudPushGeneration;
        private readonly ToggleSwitch _editAutoSaveToggle;
        private readonly ToggleSwitch _editBlockNonTodayAutoSaveToggle;
        private readonly ToggleSwitch _editConfirmNonTodaySaveToggle;
        private readonly ToggleSwitch _editRefreshBeforeEditToggle;
        private readonly TextBox _editAutoSavePromptTextBox;
        private readonly TextBox _editManualSavePromptTextBox;
        private int _editCloudPushGeneration;
        private bool _editPrefsPulledThisSession;
        private readonly Grid _appTitleBar;
        private readonly ColumnDefinition _leftInsetColumn;
        private readonly ColumnDefinition _rightInsetColumn;
        private string _activeCategoryKey = "server";
        private bool _hasAutoRefreshedDeviceInfo;
        private bool _isAutoSaveSuspended;

        public SettingsWindow(Action onSettingsChanged)
        {
            _onSettingsChanged = onSettingsChanged;
            _isAutoSaveSuspended = true;

            Title = "设置";
            SystemBackdrop = new MicaBackdrop();
            var settings = AppSettings.Values;

            // --- 卡片大小 ---
            _minCardWidthBox = CreateNumberBoxWithoutHeader(100, 800, 10, 220);
            _minCardWidthBox.Value = (double)(settings[MinCardWidthKey] ?? 220.0);
            _cardGapBox = CreateNumberBoxWithoutHeader(0, 60, 2, 12);
            _cardGapBox.Value = (double)(settings[CardGapKey] ?? 12.0);

            // --- 文字大小 ---
            _subjectFontSizeBox = CreateNumberBoxWithoutHeader(10, 48, 1, 18);
            _subjectFontSizeBox.Value = (double)(settings[SubjectFontSizeKey] ?? 18.0);
            _contentFontSizeBox = CreateNumberBoxWithoutHeader(8, 36, 1, 15);
            _contentFontSizeBox.Value = (double)(settings[ContentFontSizeKey] ?? 15.0);

            // --- 服务器地址 ---
            _serverUrlBox = new TextBox
            {
                PlaceholderText = "https://kv-service.wuyuan.dev",
                Text = settings[ServerUrlKey] as string ?? "https://kv-service.wuyuan.dev"
            };

            // --- 定时刷新 ---
            _autoRefreshToggle = new ToggleSwitch
            {
                IsOn = settings.ContainsKey(AutoRefreshEnabledKey) ? (bool)(settings[AutoRefreshEnabledKey] ?? false) : false,
                OnContent = null,
                OffContent = null,
                MinWidth = 0,
                Margin = new Thickness(0)
            };

            _autoRefreshIntervalBox = CreateNumberBoxWithoutHeader(10, 600, 10, 60);
            _autoRefreshIntervalBox.Value = (double)(settings[AutoRefreshIntervalKey] ?? 60.0);

            // --- 轮播设置 ---
            _carouselIntervalBox = CreateNumberBoxWithoutHeader(1, 120, 1, 5);
            _carouselIntervalBox.Value = (double)(settings[CarouselIntervalKey] ?? 5.0);

            _carouselFontSizeBox = CreateNumberBoxWithoutHeader(16, 120, 4, 48);
            _carouselFontSizeBox.Value = (double)(settings[CarouselFontSizeKey] ?? 48.0);

            // --- 调试模式 ---
            _debugModeToggle = new ToggleSwitch
            {
                IsOn = settings.ContainsKey(DebugModeKey) && (bool)(settings[DebugModeKey] ?? false)
            };

            // --- 编辑行为 ---
            _editAutoSaveToggle = new ToggleSwitch
            {
                IsOn = settings.ContainsKey(EditPreferencesKeys.AutoSave) && (bool)(settings[EditPreferencesKeys.AutoSave] ?? false),
                OnContent = null,
                OffContent = null,
                MinWidth = 0,
                Margin = new Thickness(0)
            };
            _editBlockNonTodayAutoSaveToggle = new ToggleSwitch
            {
                IsOn = settings.ContainsKey(EditPreferencesKeys.BlockNonTodayAutoSave) && (bool)(settings[EditPreferencesKeys.BlockNonTodayAutoSave] ?? false),
                OnContent = null,
                OffContent = null,
                MinWidth = 0,
                Margin = new Thickness(0)
            };
            _editConfirmNonTodaySaveToggle = new ToggleSwitch
            {
                IsOn = settings.ContainsKey(EditPreferencesKeys.ConfirmNonTodaySave) && (bool)(settings[EditPreferencesKeys.ConfirmNonTodaySave] ?? false),
                OnContent = null,
                OffContent = null,
                MinWidth = 0,
                Margin = new Thickness(0)
            };
            _editRefreshBeforeEditToggle = new ToggleSwitch
            {
                IsOn = settings.ContainsKey(EditPreferencesKeys.RefreshBeforeEdit) && (bool)(settings[EditPreferencesKeys.RefreshBeforeEdit] ?? false),
                OnContent = null,
                OffContent = null,
                MinWidth = 0,
                Margin = new Thickness(0)
            };
            _editAutoSavePromptTextBox = new TextBox
            {
                Text = settings[EditPreferencesKeys.AutoSavePromptText] as string ?? "喵？喵呜！",
                AcceptsReturn = false,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 40,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            _editManualSavePromptTextBox = new TextBox
            {
                Text = settings[EditPreferencesKeys.ManualSavePromptText] as string ?? "写完后点击上传谢谢喵",
                AcceptsReturn = false,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 40,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            // --- 随机点名 ---
            _randomPickerEnabledToggle = new ToggleSwitch
            {
                IsOn = settings.ContainsKey(RandomPickerEnabledKey) ? (bool)(settings[RandomPickerEnabledKey] ?? true) : true,
                OnContent = null,
                OffContent = null,
                MinWidth = 0,
                Margin = new Thickness(0)
            };

            _randomPickerMinNumberBox = CreateNumberBoxWithoutHeader(1, 999, 1, ConvertToDouble(settings[RandomPickerMinNumberKey], 1.0));
            _randomPickerMaxNumberBox = CreateNumberBoxWithoutHeader(1, 9999, 1, ConvertToDouble(settings[RandomPickerMaxNumberKey], 60.0));
            _randomPickerDefaultCountBox = CreateNumberBoxWithoutHeader(1, 100, 1, ConvertToDouble(settings[RandomPickerDefaultCountKey], 1.0));

            _randomPickerAnimationToggle = new ToggleSwitch
            {
                IsOn = settings.ContainsKey(RandomPickerAnimationKey) ? (bool)(settings[RandomPickerAnimationKey] ?? true) : true,
                OnContent = null,
                OffContent = null,
                MinWidth = 0,
                Margin = new Thickness(0)
            };

            // --- 自启动 ---
            _autoStartToggle = new ToggleSwitch
            {
                IsOn = settings.ContainsKey(AutoStartEnabledKey) && (bool)(settings[AutoStartEnabledKey] ?? false),
                OnContent = null,
                OffContent = null,
                MinWidth = 0,
                Margin = new Thickness(0)
            };

            _autoStartMethodCombo = new ComboBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                MinHeight = 40,
                MinWidth = 260
            };
            foreach (var label in AutoStartMethodOptions)
                _autoStartMethodCombo.Items.Add(label);
            var savedMethod = settings[AutoStartMethodKey] as string;
            if (!string.IsNullOrWhiteSpace(savedMethod))
            {
                for (int i = 0; i < AutoStartMethodOptions.Length; i++)
                {
                    if (AutoStartMethodOptions[i].StartsWith(savedMethod, StringComparison.Ordinal))
                    {
                        _autoStartMethodCombo.SelectedIndex = i;
                        break;
                    }
                }
            }
            if (_autoStartMethodCombo.SelectedIndex < 0)
                _autoStartMethodCombo.SelectedIndex = 0;

            _dataProviderCombo = new ComboBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                MinHeight = 40
            };
            foreach (var label in DataProviderOptions)
                _dataProviderCombo.Items.Add(label);
            _dataProviderCombo.SelectionChanged += DataProviderCombo_SelectionChanged;
            ApplySavedDataProvider(settings[DataProviderKey] as string);

            var tokenFromSettings = settings[TokenKey] as string ?? "";
            _kvTokenBox = new TextBox
            {
                AcceptsReturn = false,
                TextWrapping = TextWrapping.NoWrap,
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                MinHeight = 40,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Text = tokenFromSettings,
                PlaceholderText = "粘贴 KV 授权令牌"
            };

            _deviceOwnerTitleText = new TextBlock
            {
                Text = "未知管理员",
                FontSize = 24,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"]
            };
            _deviceOwnerSubText = CreateSecondaryWrappedText("管理员账号 ID: —\n此设备由贵校或贵单位管理，该管理员系此空间所有者，如有疑问请咨询他，对于恶意绑定、滥用行为请反馈。", 13);
            _deviceNameText = CreateSecondaryWrappedText("—");
            _deviceIdText = CreateSecondaryWrappedText("—");
            _deviceCreatedText = CreateSecondaryWrappedText("—");
            _deviceUpdatedText = CreateSecondaryWrappedText("—");

            // --- Token 状态 ---
            var hasToken = settings.ContainsKey(TokenKey) && !string.IsNullOrWhiteSpace(settings[TokenKey] as string);
            _currentTokenText = new TextBlock
            {
                Text = hasToken ? "已设置" : "未设置",
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            };

            _destroyTokenButton = new Button
            {
                Content = "销毁 Token",
                Margin = new Thickness(0, 4, 0, 0)
            };
            _destroyTokenButton.Click += DestroyTokenButton_Click;

            var tokenSection = new StackPanel { Spacing = 4 };
            tokenSection.Children.Add(new TextBlock { Text = "当前 Token 状态" });
            tokenSection.Children.Add(_currentTokenText);
            tokenSection.Children.Add(_destroyTokenButton);

            // --- 导出/导入 ---
            _exportButton = new Button
            {
                Content = "导出设置",
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            _exportButton.Click += ExportButton_Click;

            _importButton = new Button
            {
                Content = "导入设置",
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            _importButton.Click += ImportButton_Click;

            var ioStack = new StackPanel { Spacing = 8 };
            ioStack.Children.Add(_exportButton);
            ioStack.Children.Add(_importButton);
            _webSettingsButton = new Button
            {
                Content = "网页端设置",
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            _webSettingsButton.Click += (_, _) =>
            {
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "https://cs.houlang.cloud/settings",
                        UseShellExecute = true
                    };
                    System.Diagnostics.Process.Start(psi);
                }
                catch { }
            };
            ioStack.Children.Add(_webSettingsButton);

            _pageTitleText = new TextBlock
            {
                FontSize = 30,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            };
            _pageDescriptionText = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            };
            _detailsHost = new Grid
            {
                MaxWidth = 920
            };

            _selectionHighlight = new Border
            {
                Height = 46,
                CornerRadius = new CornerRadius(12),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                Background = new SolidColorBrush(WithAlpha(GetBrushColor("AccentFillColorDefaultBrush", Colors.DodgerBlue), 40)),
                Opacity = 1,
                IsHitTestVisible = false
            };

            _navigationItemsHost = new Grid();
            _navigationItemsPanel = new StackPanel { Spacing = 8 };
            _navigationItemsHost.Children.Add(_selectionHighlight);
            _navigationItemsHost.Children.Add(_navigationItemsPanel);

            var contentStack = new StackPanel
            {
                Spacing = 20,
                Padding = new Thickness(28, 24, 28, 32)
            };
            contentStack.Children.Add(_pageTitleText);
            contentStack.Children.Add(_pageDescriptionText);
            contentStack.Children.Add(_detailsHost);

            var contentScrollViewer = new ScrollViewer
            {
                Content = contentStack,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            var sidebar = new Border
            {
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                Padding = new Thickness(16),
                CornerRadius = new CornerRadius(12)
            };

            var navigationStack = new StackPanel { Spacing = 8 };
            navigationStack.Children.Add(new TextBlock
            {
                Text = "设置",
                FontSize = 28,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Margin = new Thickness(8, 4, 8, 12)
            });
            _navigationItemsPanel.Children.Add(CreateNavigationButton("服务器", "server", imageIconUri: AppSettings.GetAssetUri("icons/ic_gallery_cloud_synchronization.ico").AbsoluteUri));
            _navigationItemsPanel.Children.Add(CreateNavigationButton("科目", "subjects", "\uE70F"));
            _navigationItemsPanel.Children.Add(CreateNavigationButton("名单", "roster", "\uE716"));
            _navigationItemsPanel.Children.Add(CreateNavigationButton("刷新", "refresh", "\uE72C"));
            _navigationItemsPanel.Children.Add(CreateNavigationButton("编辑", "edit", "\uE70F"));
            _navigationItemsPanel.Children.Add(CreateNavigationButton("显示", "display", "\uE7F8"));
            _navigationItemsPanel.Children.Add(CreateNavigationButton("轮播与调试", "playback", "\uE8B2"));
            _navigationItemsPanel.Children.Add(CreateNavigationButton("随机点名", "randomPicker", "\uE716"));
            _navigationItemsPanel.Children.Add(CreateNavigationButton("自启动", "autostart", "\uE7E8"));
            _navigationItemsPanel.Children.Add(CreateNavigationButton("账户与数据", "account", "\uE716"));
            _navigationItemsPanel.Children.Add(CreateNavigationButton("更新", "update", "\uE895"));
            navigationStack.Children.Add(_navigationItemsHost);

            _navigationScrollViewer = new ScrollViewer
            {
                Content = navigationStack,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            sidebar.Child = _navigationScrollViewer;

            _leftInsetColumn = new ColumnDefinition { Width = new GridLength(0) };
            _rightInsetColumn = new ColumnDefinition { Width = new GridLength(0) };
            _appTitleBar = new Grid
            {
                MinHeight = 52,
                Padding = new Thickness(0, 8, 0, 8)
            };
            _appTitleBar.ColumnDefinitions.Add(_leftInsetColumn);
            _appTitleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _appTitleBar.ColumnDefinitions.Add(_rightInsetColumn);

            var titleBarTextStack = new StackPanel
            {
                Margin = new Thickness(24, 0, 16, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Spacing = 0
            };
            titleBarTextStack.Children.Add(new TextBlock
            {
                Text = "CSD",
                FontSize = 13,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });
            titleBarTextStack.Children.Add(new TextBlock
            {
                Text = "设置",
                Margin = new Thickness(0, -1, 0, 0),
                FontSize = 15,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });
            Grid.SetColumn(titleBarTextStack, 1);
            _appTitleBar.Children.Add(titleBarTextStack);

            var serverView = CreateCategoryView(BuildDataSourceSettingsContent());

            var subjectsView = CreateCategoryView(BuildSubjectManagementContent());
            LoadSubjectsFromSettings();

            var rosterView = CreateCategoryView(BuildRosterManagementContent());
            LoadRosterFromSettings();

            var displayView = CreateCategoryView(
                CreateSettingRow("最小卡片宽度", "影响首页作业卡片的最小宽度。", _minCardWidthBox),
                CreateSettingRow("卡片间距", "首页卡片之间的间距大小。", _cardGapBox),
                CreateSettingRow("科目字体大小", "控制作业科目标题的字号。", _subjectFontSizeBox),
                CreateSettingRow("内容字体大小", "控制作业详情内容的字号。", _contentFontSizeBox));

            var refreshView = CreateCategoryView(BuildRefreshSettingsContent());

            var editView = CreateCategoryView(BuildEditSettingsContent());

            var playbackView = CreateCategoryView(
                CreateSettingRow("轮播切换间隔", "课堂展示时轮播的切换速度。", _carouselIntervalBox),
                CreateSettingRow("轮播字体大小", "轮播模式下的展示字号。", _carouselFontSizeBox),
                CreateSettingRow("调试模式", "在需要排查问题时启用。", _debugModeToggle));

            var randomPickerView = CreateCategoryView(
                CreateSettingRow("是否启用随机点名功能", "控制随机点名功能的开关。", _randomPickerEnabledToggle),
                CreateSettingRow("学号模式最小值", "学号模式下可抽取的最小学号。", _randomPickerMinNumberBox),
                CreateSettingRow("学号模式最大值", "学号模式下可抽取的最大学号。", _randomPickerMaxNumberBox),
                CreateSettingRow("默认抽取人数", "打开随机点名窗口时的默认抽取人数。", _randomPickerDefaultCountBox),
                CreateSettingRow("是否启用随机点名动画效果", "控制点名时是否播放滚动动画。", _randomPickerAnimationToggle));

            var autoStartMethodStack = new StackPanel { Spacing = 4 };
            autoStartMethodStack.Children.Add(new TextBlock
            {
                Text = "优先级方式（星级越高越推荐）",
                FontSize = 12,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });
            autoStartMethodStack.Children.Add(_autoStartMethodCombo);
            var autoStartView = CreateCategoryView(
                CreateSettingRow("开机自启动", "启用后 CSD 将在系统启动时自动运行。", _autoStartToggle),
                CreateSettingRow("自启动方式", "选择自启动的优先级实现方式，星级越高越稳定可靠。", autoStartMethodStack));

            var accountView = CreateCategoryView(
                CreateSettingRow("当前 Token 状态", "查看授权状态并可重置。", tokenSection),
                CreateSettingRow("数据管理", "导入、导出本地设置，或前往网页端。", ioStack));

            var updateView = CreateCategoryView(BuildUpdateSettingsContent());

            _categoryViews["server"] = serverView;
            _categoryViews["subjects"] = subjectsView;
            _categoryViews["roster"] = rosterView;
            _categoryViews["refresh"] = refreshView;
            _categoryViews["edit"] = editView;
            _categoryViews["display"] = displayView;
            _categoryViews["playback"] = playbackView;
            _categoryViews["randomPicker"] = randomPickerView;
            _categoryViews["autostart"] = autoStartView;
            _categoryViews["account"] = accountView;
            _categoryViews["update"] = updateView;

            _detailsHost.Children.Add(serverView);
            _detailsHost.Children.Add(subjectsView);
            _detailsHost.Children.Add(rosterView);
            _detailsHost.Children.Add(refreshView);
            _detailsHost.Children.Add(editView);
            _detailsHost.Children.Add(displayView);
            _detailsHost.Children.Add(playbackView);
            _detailsHost.Children.Add(randomPickerView);
            _detailsHost.Children.Add(autoStartView);
            _detailsHost.Children.Add(accountView);
            _detailsHost.Children.Add(updateView);

            var contentRoot = new Grid
            {
                Padding = new Thickness(20),
                ColumnSpacing = 20
            };
            contentRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(240) });
            contentRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Grid.SetColumn(sidebar, 0);
            Grid.SetColumn(contentScrollViewer, 1);
            contentRoot.Children.Add(sidebar);
            contentRoot.Children.Add(contentScrollViewer);

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(_appTitleBar, 0);
            Grid.SetRow(contentRoot, 1);
            root.Children.Add(_appTitleBar);
            root.Children.Add(contentRoot);

            Content = root;
            ConfigureIntegratedTitleBar();
            ShowCategory("server");
            UpdateNavigationSelection("server", animateHighlight: false);

            root.Loaded += async (_, _) =>
            {
                AnimationHelper.AnimateEntrance(root, fromY: 18f, durationMs: 360);
                AnimationHelper.ApplyStandardInteractions(contentScrollViewer);
                UpdateNavigationSelection(_activeCategoryKey, animateHighlight: false);
                await AutoRefreshCloudDeviceInfoAsync();
            };

            HookAutoSaveHandlers();
            _isAutoSaveSuspended = false;
        }

        private void ConfigureIntegratedTitleBar()
        {
            if (!AppWindowTitleBar.IsCustomizationSupported())
                return;

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(_appTitleBar);
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            UpdateTitleBarLayout(AppWindow.TitleBar);

            try
            {
                var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    AppWindow.SetIcon(iconPath);
                }
            }
            catch { }
        }

        private void UpdateTitleBarLayout(AppWindowTitleBar titleBar)
        {
            _leftInsetColumn.Width = new GridLength(titleBar.LeftInset);
            _rightInsetColumn.Width = new GridLength(titleBar.RightInset);
        }

        private void ShowCategory(string categoryKey)
        {
            foreach (var categoryView in _categoryViews)
            {
                categoryView.Value.Visibility = string.Equals(categoryView.Key, categoryKey, StringComparison.Ordinal)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            switch (categoryKey)
            {
                case "server":
                    _pageTitleText.Visibility = Visibility.Collapsed;
                    _pageDescriptionText.Visibility = Visibility.Collapsed;
                    break;

                case "subjects":
                    _pageTitleText.Visibility = Visibility.Collapsed;
                    _pageDescriptionText.Visibility = Visibility.Collapsed;
                    break;

                case "roster":
                    _pageTitleText.Visibility = Visibility.Collapsed;
                    _pageDescriptionText.Visibility = Visibility.Collapsed;
                    break;

                case "display":
                    _pageTitleText.Visibility = Visibility.Visible;
                    _pageDescriptionText.Visibility = Visibility.Visible;
                    _pageTitleText.Text = "显示";
                    _pageDescriptionText.Text = "调整首页卡片的宽度、间距和文字大小。";
                    break;

                case "refresh":
                    _pageTitleText.Visibility = Visibility.Visible;
                    _pageDescriptionText.Visibility = Visibility.Visible;
                    _pageTitleText.Text = "刷新设置";
                    _pageDescriptionText.Text = "定时从数据源拉取最新作业，并驱动主界面等全局组件一并更新。";
                    break;

                case "edit":
                    _pageTitleText.Visibility = Visibility.Visible;
                    _pageDescriptionText.Visibility = Visibility.Visible;
                    _pageTitleText.Text = "编辑设置";
                    _pageDescriptionText.Text = "自动保存、非当天写入限制、确认与提示文案；部分项会同步到云端。";
                    RequestEditPrefsPullFromCloudIfNeeded();
                    break;

                case "playback":
                    _pageTitleText.Visibility = Visibility.Visible;
                    _pageDescriptionText.Visibility = Visibility.Visible;
                    _pageTitleText.Text = "轮播与调试";
                    _pageDescriptionText.Text = "控制课堂展示轮播效果，并按需开启调试模式。";
                    break;

                case "randomPicker":
                    _pageTitleText.Visibility = Visibility.Visible;
                    _pageDescriptionText.Visibility = Visibility.Visible;
                    _pageTitleText.Text = "随机点名";
                    _pageDescriptionText.Text = "配置随机点名功能的模式、范围和默认参数。";
                    break;

                case "autostart":
                    _pageTitleText.Visibility = Visibility.Visible;
                    _pageDescriptionText.Visibility = Visibility.Visible;
                    _pageTitleText.Text = "自启动";
                    _pageDescriptionText.Text = "配置开机自启动行为，选择不同的启动方式以实现不同程度的兼容性。";
                    break;

                case "account":
                    _pageTitleText.Visibility = Visibility.Visible;
                    _pageDescriptionText.Visibility = Visibility.Visible;
                    _pageTitleText.Text = "账户与数据";
                    _pageDescriptionText.Text = "查看 Token 状态，并进行本地设置导入导出。";
                    break;

                case "update":
                    _pageTitleText.Visibility = Visibility.Visible;
                    _pageDescriptionText.Visibility = Visibility.Visible;
                    _pageTitleText.Text = "更新";
                    _pageDescriptionText.Text = "选择更新渠道以获取不同版本的更新。";
                    break;
            }
        }

        private Button CreateNavigationButton(string title, string tag, string? glyph = null, string? imageIconUri = null)
        {
            var hoverBackground = new Border
            {
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(WithAlpha(GetBrushColor("TextFillColorSecondaryBrush", Colors.LightGray), 26)),
                Opacity = 0,
                IsHitTestVisible = false
            };

            var iconBrush = new SolidColorBrush(GetBrushColor("TextFillColorSecondaryBrush", Colors.LightGray));
            var labelBrush = new SolidColorBrush(GetBrushColor("TextFillColorPrimaryBrush", Colors.White));
            var label = new TextBlock
            {
                Text = title,
                FontSize = 15,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = labelBrush
            };

            var contentGrid = new Grid();
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(label, 1);
            label.Margin = new Thickness(12, 0, 0, 0);
            contentGrid.Children.Add(hoverBackground);
            FrameworkElement iconElement;
            if (!string.IsNullOrWhiteSpace(imageIconUri))
            {
                iconElement = new Image
                {
                    Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(imageIconUri)),
                    Width = 18,
                    Height = 18,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }
            else
            {
                iconElement = new FontIcon
                {
                    Glyph = glyph ?? "",
                    FontSize = 16,
                    Foreground = iconBrush
                };
            }
            Grid.SetColumn(iconElement, 0);
            contentGrid.Children.Add(iconElement);
            contentGrid.Children.Add(label);
            Grid.SetColumnSpan(hoverBackground, 2);

            var button = new Button
            {
                Content = contentGrid,
                Tag = tag,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(14, 12, 14, 12),
                MinHeight = 46,
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush(Colors.Transparent),
                CornerRadius = new CornerRadius(12)
            };
            button.Click += NavigationButton_Click;
            button.PointerEntered += NavigationButton_PointerEntered;
            button.PointerExited += NavigationButton_PointerExited;
            button.PointerCanceled += NavigationButton_PointerExited;
            button.PointerCaptureLost += NavigationButton_PointerExited;
            button.Loaded += NavigationButton_Loaded;
            button.SizeChanged += NavigationButton_SizeChanged;

            _navigationButtons.Add(button);
            _navigationItemStates[tag] = new NavigationItemState
            {
                CategoryKey = tag,
                Button = button,
                HoverBackground = hoverBackground,
                Label = label,
                IconBrush = iconBrush,
                LabelBrush = labelBrush
            };
            return button;
        }

        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string categoryKey)
            {
                ShowCategory(categoryKey);
                UpdateNavigationSelection(categoryKey, animateHighlight: true);
            }
        }

        private void NavigationButton_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string categoryKey &&
                _navigationItemStates.TryGetValue(categoryKey, out var state) &&
                !string.Equals(categoryKey, _activeCategoryKey, StringComparison.Ordinal))
            {
                AnimationHelper.AnimateToOpacity(state.HoverBackground, 1f, 180);
            }
        }

        private void NavigationButton_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string categoryKey &&
                _navigationItemStates.TryGetValue(categoryKey, out var state) &&
                !string.Equals(categoryKey, _activeCategoryKey, StringComparison.Ordinal))
            {
                AnimationHelper.AnimateToOpacity(state.HoverBackground, 0f, 180);
            }
        }

        private void NavigationButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && string.Equals(button.Tag as string, _activeCategoryKey, StringComparison.Ordinal))
            {
                UpdateNavigationSelection(_activeCategoryKey, animateHighlight: false);
            }
        }

        private void NavigationButton_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is Button button && string.Equals(button.Tag as string, _activeCategoryKey, StringComparison.Ordinal))
            {
                UpdateNavigationSelection(_activeCategoryKey, animateHighlight: false);
            }
        }

        private void UpdateNavigationSelection(string activeCategoryKey, bool animateHighlight)
        {
            _activeCategoryKey = activeCategoryKey;
            var primaryTextColor = GetBrushColor("TextFillColorPrimaryBrush", Colors.White);
            var secondaryTextColor = GetBrushColor("TextFillColorSecondaryBrush", Colors.LightGray);
            var accentColor = GetBrushColor("AccentTextFillColorPrimaryBrush", GetBrushColor("AccentFillColorDefaultBrush", Colors.DodgerBlue));

            foreach (var button in _navigationButtons)
            {
                bool isActive = string.Equals(button.Tag as string, activeCategoryKey, StringComparison.Ordinal);
                if (button.Tag is string categoryKey && _navigationItemStates.TryGetValue(categoryKey, out var state))
                {
                    AnimationHelper.AnimateToOpacity(state.HoverBackground, 0f, 140);
                    AnimationHelper.AnimateBrushColor(state.IconBrush, isActive ? accentColor : secondaryTextColor, 220);
                    AnimationHelper.AnimateBrushColor(state.LabelBrush, isActive ? primaryTextColor : secondaryTextColor, 220);
                }
            }

            if (_navigationItemStates.TryGetValue(activeCategoryKey, out var activeState) && activeState.Button.ActualHeight > 0)
            {
                var transform = activeState.Button.TransformToVisual(_navigationItemsHost);
                var point = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
                _selectionHighlight.Height = activeState.Button.ActualHeight;
                if (animateHighlight)
                {
                    AnimationHelper.AnimateOffsetY(_selectionHighlight, (float)point.Y, 260, 5f);
                    AnimationHelper.AnimateScaleTo(_selectionHighlight, 1f, 260, 0.025f);
                }
                else
                {
                    var visual = Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementVisual(_selectionHighlight);
                    visual.Offset = new System.Numerics.Vector3(0, (float)point.Y, 0);
                    visual.Scale = new System.Numerics.Vector3(1f, 1f, 1f);
                }
            }
        }

        private UIElement? _cloudStorageContent;
        private UIElement? _cloudStorageBanner;
        private UIElement? _localStorageBanner;
        private UIElement? _customServerBanner;
        private UIElement? _customServerContent;

        private async void DataProviderCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_dataProviderCombo.SelectedItem is string selected)
            {
                ApplyDataProviderVisibility(selected);
                PersistSettings();

                if (_cloudStorageContent != null && IsCloudStorageProvider(selected))
                    await RefreshDeviceInfoAsync(showErrors: false);
            }
        }
        private void ApplySavedDataProvider(string? saved)
        {
            if (!string.IsNullOrEmpty(saved))
            {
                // 兼容旧版设置中的名称（无空格）
                if (string.Equals(saved, "Classworks云端存储", StringComparison.Ordinal))
                    saved = DataProviderOptions[0];
                var idx = Array.IndexOf(DataProviderOptions, saved);
                if (idx >= 0)
                {
                    _dataProviderCombo.SelectedIndex = idx;
                    return;
                }
            }

            _dataProviderCombo.SelectedIndex = 0;
        }

        private static bool IsCloudStorageProvider(string? provider)
        {
            return string.Equals(provider, DataProviderOptions[0], StringComparison.Ordinal);
        }

        private void ApplyDataProviderVisibility(string selected)
        {
            bool isCloud = string.Equals(selected, DataProviderOptions[0], StringComparison.Ordinal);
            bool isLocal = string.Equals(selected, DataProviderOptions[1], StringComparison.Ordinal);
            bool isCustom = string.Equals(selected, DataProviderOptions[2], StringComparison.Ordinal);

            if (_cloudStorageContent != null)
                _cloudStorageContent.Visibility = isCloud ? Visibility.Visible : Visibility.Collapsed;
            if (_cloudStorageBanner != null)
                _cloudStorageBanner.Visibility = isCloud ? Visibility.Visible : Visibility.Collapsed;
            if (_localStorageBanner != null)
                _localStorageBanner.Visibility = isLocal ? Visibility.Visible : Visibility.Collapsed;
            if (_customServerBanner != null)
                _customServerBanner.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
            if (_customServerContent != null)
                _customServerContent.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
        }

        private async Task AutoRefreshCloudDeviceInfoAsync()
        {
            if (_hasAutoRefreshedDeviceInfo || !IsCloudStorageProvider(_dataProviderCombo.SelectedItem as string))
                return;

            _hasAutoRefreshedDeviceInfo = true;
            await RefreshDeviceInfoAsync(showErrors: false);
        }

        private void HookAutoSaveHandlers()
        {
            _minCardWidthBox.ValueChanged += (_, _) => PersistSettings();
            _cardGapBox.ValueChanged += (_, _) => PersistSettings();
            _subjectFontSizeBox.ValueChanged += (_, _) => PersistSettings();
            _contentFontSizeBox.ValueChanged += (_, _) => PersistSettings();
            _serverUrlBox.TextChanged += (_, _) => PersistSettings();
            _autoRefreshToggle.Toggled += (_, _) => PersistSettings();
            _autoRefreshIntervalBox.ValueChanged += (_, _) => PersistSettings();
            _carouselIntervalBox.ValueChanged += (_, _) => PersistSettings();
            _carouselFontSizeBox.ValueChanged += (_, _) => PersistSettings();
            _debugModeToggle.Toggled += (_, _) => PersistSettings();
            _editAutoSaveToggle.Toggled += (_, _) => { PersistSettings(); ScheduleEditPrefsCloudPush(); };
            _editBlockNonTodayAutoSaveToggle.Toggled += (_, _) => { PersistSettings(); ScheduleEditPrefsCloudPush(); };
            _editConfirmNonTodaySaveToggle.Toggled += (_, _) => { PersistSettings(); ScheduleEditPrefsCloudPush(); };
            _editRefreshBeforeEditToggle.Toggled += (_, _) => { PersistSettings(); ScheduleEditPrefsCloudPush(); };
            _editAutoSavePromptTextBox.TextChanged += (_, _) => { PersistSettings(); ScheduleEditPrefsCloudPush(); };
            _editManualSavePromptTextBox.TextChanged += (_, _) => { PersistSettings(); ScheduleEditPrefsCloudPush(); };
            _kvTokenBox.TextChanged += (_, _) => PersistSettings();

            _randomPickerEnabledToggle.Toggled += (_, _) => PersistSettings();
            _randomPickerMinNumberBox.ValueChanged += (_, _) => PersistSettings();
            _randomPickerMaxNumberBox.ValueChanged += (_, _) => PersistSettings();
            _randomPickerDefaultCountBox.ValueChanged += (_, _) => PersistSettings();
            _randomPickerAnimationToggle.Toggled += (_, _) => PersistSettings();

            _autoStartToggle.Toggled += (_, _) => { PersistSettings(); _ = ApplyAutoStartAsync(); };
            _autoStartMethodCombo.SelectionChanged += (_, _) => { PersistSettings(); _ = ApplyAutoStartAsync(); };
        }

        private void PersistSettings()
        {
            if (_isAutoSaveSuspended)
                return;

            var settings = AppSettings.Values;
            settings[MinCardWidthKey] = _minCardWidthBox.Value;
            settings[CardGapKey] = _cardGapBox.Value;
            settings[SubjectFontSizeKey] = _subjectFontSizeBox.Value;
            settings[ContentFontSizeKey] = _contentFontSizeBox.Value;
            settings[ServerUrlKey] = _serverUrlBox.Text;
            settings[AutoRefreshEnabledKey] = _autoRefreshToggle.IsOn;
            settings[AutoRefreshIntervalKey] = _autoRefreshIntervalBox.Value;
            settings[CarouselIntervalKey] = _carouselIntervalBox.Value;
            settings[CarouselFontSizeKey] = _carouselFontSizeBox.Value;
            settings[DebugModeKey] = _debugModeToggle.IsOn;
            settings[EditPreferencesKeys.AutoSave] = _editAutoSaveToggle.IsOn;
            settings[EditPreferencesKeys.BlockNonTodayAutoSave] = _editBlockNonTodayAutoSaveToggle.IsOn;
            settings[EditPreferencesKeys.ConfirmNonTodaySave] = _editConfirmNonTodaySaveToggle.IsOn;
            settings[EditPreferencesKeys.RefreshBeforeEdit] = _editRefreshBeforeEditToggle.IsOn;
            settings[EditPreferencesKeys.AutoSavePromptText] = _editAutoSavePromptTextBox.Text ?? "";
            settings[EditPreferencesKeys.ManualSavePromptText] = _editManualSavePromptTextBox.Text ?? "";

            if (_dataProviderCombo.SelectedItem is string providerLabel)
                settings[DataProviderKey] = providerLabel;
            else if (_dataProviderCombo.SelectedIndex >= 0 && _dataProviderCombo.SelectedIndex < DataProviderOptions.Length)
                settings[DataProviderKey] = DataProviderOptions[_dataProviderCombo.SelectedIndex];

            var tokenTrimmed = _kvTokenBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(tokenTrimmed))
                settings.Remove(TokenKey);
            else
                settings[TokenKey] = tokenTrimmed;

            _currentTokenText.Text = string.IsNullOrWhiteSpace(tokenTrimmed) ? "未设置" : "已设置";

            settings[RandomPickerEnabledKey] = _randomPickerEnabledToggle.IsOn;
            settings[RandomPickerMinNumberKey] = (int)_randomPickerMinNumberBox.Value;
            settings[RandomPickerMaxNumberKey] = (int)_randomPickerMaxNumberBox.Value;
            settings[RandomPickerDefaultCountKey] = (int)_randomPickerDefaultCountBox.Value;
            settings[RandomPickerAnimationKey] = _randomPickerAnimationToggle.IsOn;

            settings[AutoStartEnabledKey] = _autoStartToggle.IsOn;
            if (_autoStartMethodCombo.SelectedItem is string methodLabel)
            {
                var methodName = methodLabel.Split("  ")[0];
                settings[AutoStartMethodKey] = methodName;
            }
            else if (_autoStartMethodCombo.SelectedIndex >= 0 && _autoStartMethodCombo.SelectedIndex < AutoStartMethodOptions.Length)
            {
                var methodName = AutoStartMethodOptions[_autoStartMethodCombo.SelectedIndex].Split("  ")[0];
                settings[AutoStartMethodKey] = methodName;
            }

            _onSettingsChanged?.Invoke();
        }

        private StackPanel BuildDataSourceSettingsContent()
        {
            var root = new StackPanel { Spacing = 22 };

            var titleRow = new StackPanel();
            titleRow.Children.Add(new TextBlock
            {
                Text = "数据源设置",
                FontSize = 26,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            });
            root.Children.Add(titleRow);

            root.Children.Add(new TextBlock
            {
                Text = "数据提供者",
                FontSize = 13,
                Margin = new Thickness(0, 2, 0, 4),
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });
            root.Children.Add(_dataProviderCombo);

            var bannerBrushBg = new SolidColorBrush(ColorHelper.FromArgb(255, 26, 56, 42));
            var bannerBrushFg = new SolidColorBrush(ColorHelper.FromArgb(255, 165, 224, 190));
            var bannerStack = new StackPanel { Spacing = 6 };
            bannerStack.Children.Add(new TextBlock
            {
                Text = "Classworks 云端存储",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = bannerBrushFg,
                TextWrapping = TextWrapping.Wrap
            });
            bannerStack.Children.Add(new TextBlock
            {
                Text = "Classworks云端存储是官方提供的存储解决方案，自动配置了最优的访问设置。" + Environment.NewLine + "使用此选项时，服务器域名和网站令牌将自动配置，无需手动设置。",
                FontSize = 13,
                Foreground = bannerBrushFg,
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.95
            });
            var bannerInner = new Grid { ColumnSpacing = 12 };
            bannerInner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            bannerInner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var cloudIcon = new FontIcon
            {
                Glyph = "\uE946",
                FontSize = 18,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 2, 0, 0),
                Foreground = bannerBrushFg
            };
            Grid.SetColumn(cloudIcon, 0);
            Grid.SetColumn(bannerStack, 1);
            bannerInner.Children.Add(cloudIcon);
            bannerInner.Children.Add(bannerStack);
            _cloudStorageBanner = new Border
            {
                Background = bannerBrushBg,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 12, 14, 12),
                Child = bannerInner
            };

            var localBannerBrushBg = new SolidColorBrush(ColorHelper.FromArgb(255, 26, 42, 56));
            var localBannerBrushFg = new SolidColorBrush(ColorHelper.FromArgb(255, 165, 190, 224));
            var localBannerStack = new StackPanel { Spacing = 6 };
            localBannerStack.Children.Add(new TextBlock
            {
                Text = "本地存储",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = localBannerBrushFg,
                TextWrapping = TextWrapping.Wrap
            });
            localBannerStack.Children.Add(new TextBlock
            {
                Text = "无需网络连接即可使用。Classworks 将把所有数据保存在本地设备上。使用本地存储意味着您无法使用多设备同步功能，但可以在没有网络的环境下正常使用软件的基础功能。",
                FontSize = 13,
                Foreground = localBannerBrushFg,
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.95
            });
            var localBannerInner = new Grid { ColumnSpacing = 12 };
            localBannerInner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            localBannerInner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var localIcon = new Microsoft.UI.Xaml.Controls.ImageIcon
            {
                Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(AppSettings.GetAssetUri("icons/ic_public_folder.ico")),
                Width = 18,
                Height = 18,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 2, 0, 0),
                Foreground = localBannerBrushFg
            };
            Grid.SetColumn(localIcon, 0);
            Grid.SetColumn(localBannerStack, 1);

            localBannerInner.Children.Add(localIcon);
            localBannerInner.Children.Add(localBannerStack);
            _localStorageBanner = new Border
            {
                Background = localBannerBrushBg,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 12, 14, 12),
                Child = localBannerInner,
                Visibility = Visibility.Collapsed
            };

            var customBannerBrushBg = new SolidColorBrush(ColorHelper.FromArgb(255, 56, 42, 26));
            var customBannerBrushFg = new SolidColorBrush(ColorHelper.FromArgb(255, 224, 190, 165));
            var customBannerStack = new StackPanel { Spacing = 6 };
            customBannerStack.Children.Add(new TextBlock
            {
                Text = "自定义远程服务器",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = customBannerBrushFg,
                TextWrapping = TextWrapping.Wrap
            });
            customBannerStack.Children.Add(new TextBlock
            {
                Text = "KV存储系统使用本机唯一标识符(UUID)来区分不同设备的数据。" + Environment.NewLine + "服务器端点格式: http(s)://服务器域名/" + Environment.NewLine + "在服务器域名处仅填写基础URL，不需要任何路径。",
                FontSize = 13,
                Foreground = customBannerBrushFg,
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.95
            });
            var customBannerInner = new Grid { ColumnSpacing = 12 };
            customBannerInner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            customBannerInner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var customIcon = new FontIcon
            {
                Glyph = "\uE774",
                FontSize = 18,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 2, 0, 0),
                Foreground = customBannerBrushFg
            };
            Grid.SetColumn(customIcon, 0);
            Grid.SetColumn(customBannerStack, 1);
            customBannerInner.Children.Add(customIcon);
            customBannerInner.Children.Add(customBannerStack);
            _customServerBanner = new Border
            {
                Background = customBannerBrushBg,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 12, 14, 12),
                Child = customBannerInner,
                Visibility = Visibility.Collapsed
            };

            var bannerHost = new Grid();
            bannerHost.Children.Add(_cloudStorageBanner);
            bannerHost.Children.Add(_localStorageBanner);
            bannerHost.Children.Add(_customServerBanner);
            root.Children.Add(bannerHost);

            var cloudContentStack = new StackPanel { Spacing = 22 };

            var tokenGrid = new Grid();
            tokenGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            tokenGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var shieldIcon = new FontIcon
            {
                Glyph = "\uEA18",
                FontSize = 22,
                Margin = new Thickness(0, 14, 12, 0),
                VerticalAlignment = VerticalAlignment.Top,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            };
            Grid.SetColumn(shieldIcon, 0);

            var tokenFrame = new Grid();
            var tokenBorder = new Border
            {
                BorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 16, 10, 10),
                Child = _kvTokenBox
            };
            var labelBackdrop = (Brush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"];
            var tokenLabelChip = new Border
            {
                Background = labelBackdrop,
                Padding = new Thickness(8, 0, 8, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, -11, 0, 0),
                Child = new TextBlock
                {
                    Text = "KV 授权令牌",
                    FontSize = 12,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                }
            };
            tokenFrame.Children.Add(tokenBorder);
            tokenFrame.Children.Add(tokenLabelChip);
            Grid.SetColumn(tokenFrame, 1);
            tokenGrid.Children.Add(shieldIcon);
            tokenGrid.Children.Add(tokenFrame);
            
            var tokenContainer = new StackPanel { Spacing = 4 };
            tokenContainer.Children.Add(tokenGrid);
            tokenContainer.Children.Add(new TextBlock
            {
                Text = "令牌用于云端存储授权",
                FontSize = 12,
                Margin = new Thickness(34, 0, 0, 0),
                Foreground = (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"]
            });
            cloudContentStack.Children.Add(tokenContainer);

            var accountHeader = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(0, 6, 0, 0) };
            accountHeader.Children.Add(new FontIcon
            {
                Glyph = "\uE77B",
                FontSize = 20,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });
            accountHeader.Children.Add(new TextBlock
            {
                Text = "账号信息",
                FontSize = 18,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            });
            cloudContentStack.Children.Add(accountHeader);

            var manageCardInner = new StackPanel { Spacing = 8 };
            manageCardInner.Children.Add(_deviceOwnerTitleText);
            manageCardInner.Children.Add(_deviceOwnerSubText);
            cloudContentStack.Children.Add(CreateFilledCard(manageCardInner));

            var deviceHeader = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(0, 6, 0, 0) };
            deviceHeader.Children.Add(new Image
            {
                Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(AppSettings.GetAssetUri("icons/ic_device_matebook.ico")),
                Width = 20,
                Height = 20,
                VerticalAlignment = VerticalAlignment.Center
            });
            deviceHeader.Children.Add(new TextBlock
            {
                Text = "设备信息",
                FontSize = 18,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            });
            cloudContentStack.Children.Add(deviceHeader);

            var techInner = new StackPanel { Spacing = 12 };
            techInner.Children.Add(CreateDeviceIconRow("\uE8EC", _deviceNameText));
            techInner.Children.Add(CreateDeviceIconRow("\uE716", _deviceIdText));
            techInner.Children.Add(CreateDeviceIconRow("\uE787", _deviceCreatedText));
            techInner.Children.Add(CreateDeviceIconRow("\uE72C", _deviceUpdatedText));
            cloudContentStack.Children.Add(CreateFilledCard(techInner));

            var aboutStack = new StackPanel { Spacing = 10, Margin = new Thickness(0, 4, 0, 0) };
            aboutStack.Children.Add(new TextBlock
            {
                Text = "Classworks KV",
                FontSize = 17,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            aboutStack.Children.Add(new TextBlock
            {
                Text = "文档形键值数据库",
                FontSize = 13,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });
            aboutStack.Children.Add(new TextBlock
            {
                Text = "Classworks KV 是一个专为教育场景设计的文档型键值存储服务，适合存储每日作业、出勤与课堂配置等结构化数据，并与课堂套件其它组件互通。",
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                LineHeight = 22
            });
            var adminRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
            adminRow.Children.Add(new TextBlock
            {
                Text = "Classworks KV 的全域管理员是",
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });
            var adminLink = new HyperlinkButton
            {
                Content = "孙悟元",
                NavigateUri = new Uri("https://wuyuan.dev/"),
                Padding = new Thickness(4, 0, 4, 0),
                Margin = new Thickness(-6, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            adminRow.Children.Add(adminLink);
            aboutStack.Children.Add(adminRow);
            var kvLinkRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            kvLinkRow.Children.Add(new TextBlock
            {
                Text = "前往 Classworks KV",
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 13
            });
            kvLinkRow.Children.Add(new FontIcon
            {
                Glyph = "\uE8A7",
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"]
            });
            var kvNav = new HyperlinkButton
            {
                Content = kvLinkRow,
                NavigateUri = new Uri("https://kv.houlang.cloud/"),
                Margin = new Thickness(-12, 0, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            aboutStack.Children.Add(kvNav);
            cloudContentStack.Children.Add(aboutStack);

            var refreshOutline = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
            var refreshBtn = new Button
            {
                Content = "刷新设备信息",
                Background = new SolidColorBrush(WithAlpha(GetBrushColor("TextFillColorPrimaryBrush", Colors.White), 0)),
                BorderBrush = refreshOutline,
                BorderThickness = new Thickness(1),
                Foreground = refreshOutline,
                Padding = new Thickness(16, 8, 16, 8),
                CornerRadius = new CornerRadius(8)
            };
            refreshBtn.Click += RefreshDeviceInfo_Click;

            var dangerBrush = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
            var reinitBtn = new Button
            {
                Content = "重新初始化云端存储",
                Background = new SolidColorBrush(WithAlpha(GetBrushColor("TextFillColorPrimaryBrush", Colors.White), 0)),
                BorderBrush = dangerBrush,
                BorderThickness = new Thickness(1),
                Foreground = dangerBrush,
                Padding = new Thickness(16, 8, 16, 8),
                CornerRadius = new CornerRadius(8)
            };
            reinitBtn.Click += DestroyTokenButton_Click;

            var footer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 12,
                Margin = new Thickness(0, 12, 0, 0)
            };
            footer.Children.Add(refreshBtn);
            footer.Children.Add(reinitBtn);
            cloudContentStack.Children.Add(footer);

            _cloudStorageContent = cloudContentStack;
            root.Children.Add(_cloudStorageContent);

            var customServerStack = new StackPanel { Spacing = 22, Visibility = Visibility.Collapsed };

            var customServerUrlGrid = new Grid();
            customServerUrlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            customServerUrlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var customServerIcon = new FontIcon
            {
                Glyph = "\uE774",
                FontSize = 22,
                Margin = new Thickness(0, 14, 12, 0),
                VerticalAlignment = VerticalAlignment.Top,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            };
            Grid.SetColumn(customServerIcon, 0);

            var customServerUrlFrame = new Grid();
            var customServerUrlBorder = new Border
            {
                BorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 16, 10, 10),
                Child = _serverUrlBox
            };
            var customServerUrlLabelChip = new Border
            {
                Background = labelBackdrop,
                Padding = new Thickness(8, 0, 8, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, -11, 0, 0),
                Child = new TextBlock
                {
                    Text = "服务器地址",
                    FontSize = 12,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                }
            };
            customServerUrlFrame.Children.Add(customServerUrlBorder);
            customServerUrlFrame.Children.Add(customServerUrlLabelChip);
            Grid.SetColumn(customServerUrlFrame, 1);
            customServerUrlGrid.Children.Add(customServerIcon);
            customServerUrlGrid.Children.Add(customServerUrlFrame);
            
            var customServerUrlContainer = new StackPanel { Spacing = 4 };
            customServerUrlContainer.Children.Add(customServerUrlGrid);
            customServerUrlContainer.Children.Add(new TextBlock
            {
                Text = "KV存储系统使用本机唯一标识符(UUID)来区分不同设备的数据。\n服务器端点格式: http(s)://服务器域名/\n在服务器域名处仅填写基础URL，不需要任何路径。",
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(34, 0, 0, 0),
                Foreground = (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"]
            });
            customServerStack.Children.Add(customServerUrlContainer);

            var customTokenGrid = new Grid();
            customTokenGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            customTokenGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var customTokenIcon = new FontIcon
            {
                Glyph = "\uEA18",
                FontSize = 22,
                Margin = new Thickness(0, 14, 12, 0),
                VerticalAlignment = VerticalAlignment.Top,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            };
            Grid.SetColumn(customTokenIcon, 0);

            var customTokenFrame = new Grid();
            // Clone token box for custom server
            var customTokenBox = new TextBox
            {
                AcceptsReturn = false,
                TextWrapping = TextWrapping.NoWrap,
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                MinHeight = 40,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Text = _kvTokenBox.Text,
                PlaceholderText = "粘贴 KV 授权令牌"
            };
            customTokenBox.TextChanged += (s, e) => { _kvTokenBox.Text = customTokenBox.Text; };
            _kvTokenBox.TextChanged += (s, e) => { customTokenBox.Text = _kvTokenBox.Text; };
            
            var customTokenBorder = new Border
            {
                BorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 16, 10, 10),
                Child = customTokenBox
            };
            var customTokenLabelChip = new Border
            {
                Background = labelBackdrop,
                Padding = new Thickness(8, 0, 8, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, -11, 0, 0),
                Child = new TextBlock
                {
                    Text = "KV 授权令牌",
                    FontSize = 12,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                }
            };
            customTokenFrame.Children.Add(customTokenBorder);
            customTokenFrame.Children.Add(customTokenLabelChip);
            Grid.SetColumn(customTokenFrame, 1);
            customTokenGrid.Children.Add(customTokenIcon);
            customTokenGrid.Children.Add(customTokenFrame);
            
            var customTokenContainer = new StackPanel { Spacing = 4 };
            customTokenContainer.Children.Add(customTokenGrid);
            customTokenContainer.Children.Add(new TextBlock
            {
                Text = "令牌用于云端存储授权",
                FontSize = 12,
                Margin = new Thickness(34, 0, 0, 0),
                Foreground = (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"]
            });
            customServerStack.Children.Add(customTokenContainer);

            _customServerContent = customServerStack;
            root.Children.Add(_customServerContent);

            if (_dataProviderCombo.SelectedItem is string selected)
                ApplyDataProviderVisibility(selected);

            return root;
        }

        private static Border CreateFilledCard(UIElement inner)
        {
            return new Border
            {
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
                BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(18, 16, 18, 16),
                Child = inner
            };
        }

        private static StackPanel CreateDeviceIconRow(string glyph, TextBlock line)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
            row.Children.Add(new FontIcon
            {
                Glyph = glyph,
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });
            line.VerticalAlignment = VerticalAlignment.Center;
            row.Children.Add(line);
            return row;
        }

        private static TextBlock CreateSecondaryWrappedText(string text, double fontSize = 14)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            };
        }

        private async Task<string?> GetKvTokenSnapshotAsync()
        {
            var settings = AppSettings.Values;
            var savedToken = settings[TokenKey] as string;
            var dq = _kvTokenBox.DispatcherQueue;

            if (dq != null && !dq.HasThreadAccess)
            {
                var tcs = new TaskCompletionSource<string?>();
                if (!dq.TryEnqueue(() =>
                {
                    var liveToken = _kvTokenBox.Text?.Trim();
                    tcs.TrySetResult(!string.IsNullOrWhiteSpace(liveToken) ? liveToken : savedToken);
                }))
                    return savedToken;

                return await tcs.Task.ConfigureAwait(false);
            }

            var token = _kvTokenBox.Text?.Trim();
            return !string.IsNullOrWhiteSpace(token) ? token : savedToken;
        }

        private async void RefreshDeviceInfo_Click(object sender, RoutedEventArgs e)
        {
            await RefreshDeviceInfoAsync(showErrors: true);
        }

        private async Task RefreshDeviceInfoAsync(bool showErrors)
        {
            var settings = AppSettings.Values;
            var token = await GetKvTokenSnapshotAsync();
            var baseUrl = (settings[ServerUrlKey] as string ?? "https://kv-service.wuyuan.dev").TrimEnd('/');
            if (string.IsNullOrWhiteSpace(token))
            {
                if (showErrors)
                    await ShowSimpleDialogAsync("请先填写或保存 KV 授权令牌。");
                return;
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/kv/_info");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());
                using var response = await _settingsHttpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    if (showErrors)
                        await ShowSimpleDialogAsync($"未能获取设备信息（HTTP {(int)response.StatusCode}）。若服务端不提供 /kv/_info 接口，将保持占位内容。");
                    return;
                }

                using var doc = JsonDocument.Parse(body);
                ApplyDeviceInfoJson(doc.RootElement);
            }
            catch (Exception ex)
            {
                if (showErrors)
                    await ShowSimpleDialogAsync($"刷新失败：{ex.Message}");
            }
        }

        private async Task ShowSimpleDialogAsync(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "数据源",
                Content = message,
                CloseButtonText = "确定",
                XamlRoot = Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private void ApplyDeviceInfoJson(JsonElement root)
        {
            JsonElement data = root;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var wrapped) && wrapped.ValueKind == JsonValueKind.Object)
                data = wrapped;
            var accountObj = data;
            if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("account", out var acc) && acc.ValueKind == JsonValueKind.Object)
                accountObj = acc;

            var deviceObj = data;
            if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("device", out var dev) && dev.ValueKind == JsonValueKind.Object)
                deviceObj = dev;

            static string AsTrimmedString(JsonElement el)
            {
                return el.ValueKind switch
                {
                    JsonValueKind.String => el.GetString()?.Trim() ?? "",
                    JsonValueKind.Number => el.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => ""
                };
            }

            static string FindString(JsonElement obj, params string[] keys)
            {
                foreach (var key in keys)
                {
                    if (obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(key, out var v) && v.ValueKind != JsonValueKind.Null)
                    {
                        var s = AsTrimmedString(v);
                        if (!string.IsNullOrEmpty(s))
                            return s;
                    }
                }
                return "";
            }

            static string FormatDeviceTime(string raw)
            {
                if (string.IsNullOrEmpty(raw))
                    return "—";
                if (DateTime.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.RoundtripKind, out var utc))
                {
                    var local = utc.Kind == DateTimeKind.Utc ? utc.ToLocalTime() : utc;
                    return local.ToString("yyyy/M/d HH:mm:ss");
                }
                return raw;
            }

            var owner = FindString(accountObj, "ownerName", "owner_name", "adminName", "admin_name", "owner", "displayName", "nickname", "name");
            var adminId = FindString(accountObj, "adminId", "admin_id", "ownerId", "owner_id", "appId", "app_id", "id");
            var deviceName = FindString(deviceObj, "deviceName", "device_name", "name", "label", "namespace");
            var deviceId = FindString(deviceObj, "deviceId", "device_id", "id", "deviceType", "device_type");
            var created = FindString(deviceObj, "createdAt", "created_at", "createTime", "created", "installedAt", "installed_at");
            var updated = FindString(deviceObj, "updatedAt", "updated_at", "updateTime", "updated");

            _deviceOwnerTitleText.Text = string.IsNullOrEmpty(owner) ? "未知管理员" : owner;
            _deviceOwnerSubText.Text = $"此设备由贵校管理 管理员账号 ID: {(string.IsNullOrEmpty(adminId) ? "—" : adminId)}\n此设备由贵校或贵单位管理，该管理员系此空间所有者，如有疑问请咨询他，对于恶意绑定、滥用行为请反馈。";

            _deviceNameText.Text = $"设备名称: {(string.IsNullOrEmpty(deviceName) ? "—" : deviceName)}";
            _deviceIdText.Text = $"设备 ID: {(string.IsNullOrEmpty(deviceId) ? "—" : deviceId)}";
            _deviceCreatedText.Text = $"创建时间: {FormatDeviceTime(created)}";
            _deviceUpdatedText.Text = $"更新时间: {FormatDeviceTime(updated)}";
        }

        private StackPanel BuildSubjectManagementContent()
        {
            var root = new StackPanel { Spacing = 18 };

            var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
            titleRow.Children.Add(new FontIcon
            {
                Glyph = "\uE734",
                FontSize = 28,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"]
            });
            titleRow.Children.Add(new TextBlock
            {
                Text = "科目管理",
                FontSize = 26,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            });
            root.Children.Add(titleRow);

            var accent = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
            var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };

            var reloadBtn = new Button
            {
                Content = CreateIconTextRow("\uE72C", "重新加载"),
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                Foreground = accent,
                Padding = new Thickness(8, 6, 8, 6)
            };
            reloadBtn.Click += async (_, _) => await ReloadSubjectsFromKvAsync(showErrors: true);

            var saveBtn = new Button
            {
                Content = CreateIconTextRow("\uE74E", "保存"),
                Background = new SolidColorBrush(ColorHelper.FromArgb(255, 46, 125, 50)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(18, 10, 18, 10),
                CornerRadius = new CornerRadius(8)
            };
            saveBtn.Click += async (_, _) => await SaveSubjectsToKvAsync(showErrors: true);

            var resetBtn = new Button
            {
                Content = CreateIconTextRow("\uE777", "重置为默认"),
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"],
                Padding = new Thickness(8, 6, 8, 6)
            };
            resetBtn.Click += (_, _) => ResetSubjectsToDefaults(queueCloudSync: true);

            btnRow.Children.Add(reloadBtn);
            btnRow.Children.Add(saveBtn);
            btnRow.Children.Add(resetBtn);
            root.Children.Add(btnRow);

            _subjectNameInput = new TextBox
            {
                PlaceholderText = "科目名称",
                MinHeight = 40,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                BorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1)
            };
            _subjectNameInput.KeyDown += SubjectNameInput_KeyDown;
            root.Children.Add(_subjectNameInput);

            _subjectRowsPanel = new StackPanel();
            var listBorder = new Border
            {
                BorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(0, 4, 0, 4),
                Child = new ScrollViewer
                {
                    MaxHeight = 440,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    Content = _subjectRowsPanel
                }
            };
            root.Children.Add(listBorder);

            return root;
        }

        private static StackPanel CreateIconTextRow(string glyph, string text)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            row.Children.Add(new FontIcon
            {
                Glyph = glyph,
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center
            });
            row.Children.Add(new TextBlock
            {
                Text = text,
                VerticalAlignment = VerticalAlignment.Center
            });
            return row;
        }

        private void SubjectNameInput_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                TryAddSubjectFromInput();
                e.Handled = true;
            }
        }

        private void TryAddSubjectFromInput()
        {
            var name = _subjectNameInput.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(name))
                return;
            _managedSubjects.Add(name);
            _subjectNameInput.Text = "";
            RebuildSubjectListUi();
            PersistSubjectListLocalOnly();
        }

        private void ResetSubjectsToDefaults(bool queueCloudSync = true)
        {
            _managedSubjects.Clear();
            _managedSubjects.AddRange(DefaultSubjectNames);
            RebuildSubjectListUi();
            PersistSubjectListLocalOnly(queueCloudPush: queueCloudSync);
        }

        private void LoadSubjectsFromSettings()
        {
            var raw = AppSettings.Values[SubjectListKey] as string;
            if (!string.IsNullOrWhiteSpace(raw))
            {
                try
                {
                    var list = JsonSerializer.Deserialize<List<string>>(raw);
                    if (list != null && list.Count > 0)
                    {
                        _managedSubjects.Clear();
                        foreach (var s in list)
                        {
                            if (!string.IsNullOrWhiteSpace(s))
                                _managedSubjects.Add(s.Trim());
                        }
                        if (_managedSubjects.Count > 0)
                        {
                            RebuildSubjectListUi();
                            return;
                        }
                    }
                }
                catch
                {
                    // fall through
                }
            }

            if (_managedSubjects.Count == 0)
                ResetSubjectsToDefaults(queueCloudSync: false);
            else
                RebuildSubjectListUi();
        }

        private void PersistSubjectListLocalOnly(bool queueCloudPush = true)
        {
            AppSettings.Values[SubjectListKey] = JsonSerializer.Serialize(_managedSubjects);
            if (queueCloudPush)
                ScheduleSubjectsCloudPush();
        }

        private async void ScheduleSubjectsCloudPush()
        {
            var dq = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            var generation = ++_subjectCloudPushGeneration;
            await Task.Delay(450).ConfigureAwait(false);
            if (generation != _subjectCloudPushGeneration)
                return;
            var ok = await TryPushSubjectsToKvCoreAsync(showErrors: false).ConfigureAwait(false);
            if (!ok)
                return;
            dq?.TryEnqueue(() => _onSettingsChanged?.Invoke());
        }

        /// <summary>将当前列表 POST 到云端键 <see cref="ClassworksKvKeys.SubjectConfig"/>。</summary>
        private async Task<bool> TryPushSubjectsToKvCoreAsync(bool showErrors)
        {
            var settings = AppSettings.Values;
            var token = await GetKvTokenSnapshotAsync();
            if (string.IsNullOrWhiteSpace(token))
                return false;

            var baseUrl = (settings[ServerUrlKey] as string ?? "https://kv-service.wuyuan.dev").TrimEnd('/');
            try
            {
                var payload = new List<Dictionary<string, object>>();
                for (var i = 0; i < _managedSubjects.Count; i++)
                    payload.Add(new Dictionary<string, object> { ["order"] = i + 1, ["name"] = _managedSubjects[i] });

                var json = JsonSerializer.Serialize(payload);
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/kv/{ClassworksKvKeys.SubjectConfig}")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());
                using var response = await _settingsHttpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    if (showErrors)
                        await ShowSimpleDialogAsync($"同步到服务器失败（HTTP {(int)response.StatusCode}）。本机列表已保留。");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                if (showErrors)
                    await ShowSimpleDialogAsync($"同步失败：{ex.Message}");
                return false;
            }
        }

        private void RebuildSubjectListUi()
        {
            _subjectRowsPanel.Children.Clear();
            for (int i = 0; i < _managedSubjects.Count; i++)
            {
                var idx = i;
                var row = new Grid
                {
                    Padding = new Thickness(8, 8, 12, 8),
                    MinHeight = 48
                };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var moveCol = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 2,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                var up = new Button
                {
                    Content = new FontIcon { Glyph = "\uE70E", FontSize = 11 },
                    Padding = new Thickness(4, 2, 4, 2),
                    MinWidth = 34,
                    MinHeight = 26,
                    Background = new SolidColorBrush(Colors.Transparent),
                    BorderThickness = new Thickness(0)
                };
                up.Click += (_, _) => MoveSubject(idx, -1);
                var down = new Button
                {
                    Content = new FontIcon { Glyph = "\uE70D", FontSize = 11 },
                    Padding = new Thickness(4, 2, 4, 2),
                    MinWidth = 34,
                    MinHeight = 26,
                    Background = new SolidColorBrush(Colors.Transparent),
                    BorderThickness = new Thickness(0)
                };
                down.Click += (_, _) => MoveSubject(idx, 1);
                moveCol.Children.Add(up);
                moveCol.Children.Add(down);
                Grid.SetColumn(moveCol, 0);

                var nameTb = new TextBlock
                {
                    Text = _managedSubjects[i],
                    FontSize = 15,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                Grid.SetColumn(nameTb, 1);

                var deleteBtn = new Button
                {
                    Content = new FontIcon
                    {
                        Glyph = "\uE74D",
                        FontSize = 14
                    },
                    MinWidth = 40,
                    MinHeight = 36,
                    Padding = new Thickness(6),
                    Background = new SolidColorBrush(Colors.Transparent),
                    BorderThickness = new Thickness(0),
                    Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"]
                };
                ToolTipService.SetToolTip(deleteBtn, "删除");
                deleteBtn.Click += (_, _) => RemoveSubjectAt(idx);
                Grid.SetColumn(deleteBtn, 2);

                row.Children.Add(moveCol);
                row.Children.Add(nameTb);
                row.Children.Add(deleteBtn);
                _subjectRowsPanel.Children.Add(row);
            }
        }

        private void RemoveSubjectAt(int index)
        {
            if (index < 0 || index >= _managedSubjects.Count)
                return;
            _managedSubjects.RemoveAt(index);
            RebuildSubjectListUi();
            PersistSubjectListLocalOnly();
        }

        private void MoveSubject(int index, int delta)
        {
            var n = index + delta;
            if (n < 0 || n >= _managedSubjects.Count)
                return;
            (_managedSubjects[index], _managedSubjects[n]) = (_managedSubjects[n], _managedSubjects[index]);
            RebuildSubjectListUi();
            PersistSubjectListLocalOnly();
        }

        private async Task ReloadSubjectsFromKvAsync(bool showErrors)
        {
            var uiQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            var settings = AppSettings.Values;
            var token = await GetKvTokenSnapshotAsync();
            var baseUrl = (settings[ServerUrlKey] as string ?? "https://kv-service.wuyuan.dev").TrimEnd('/');
            if (string.IsNullOrWhiteSpace(token))
            {
                if (showErrors)
                    await ShowSimpleDialogAsync("请先配置 KV 授权令牌后再从服务器加载科目列表。");
                LoadSubjectsFromSettings();
                return;
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/kv/{ClassworksKvKeys.SubjectConfig}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());
                using var response = await _settingsHttpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    if (showErrors)
                        await ShowSimpleDialogAsync($"从服务器加载失败（HTTP {(int)response.StatusCode}）。已显示本机已保存的列表。");
                    LoadSubjectsFromSettings();
                    return;
                }

                using var doc = JsonDocument.Parse(body);
                var pairs = new List<(int order, string name)>();
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    var order = pairs.Count;
                    if (el.TryGetProperty("order", out var oEl) && oEl.ValueKind == JsonValueKind.Number)
                        order = oEl.GetInt32();
                    var name = "";
                    if (el.TryGetProperty("name", out var nEl) && nEl.ValueKind == JsonValueKind.String)
                        name = nEl.GetString()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(name))
                        pairs.Add((order, name));
                }

                pairs.Sort((a, b) => a.order.CompareTo(b.order));
                _managedSubjects.Clear();
                _managedSubjects.AddRange(pairs.Select(p => p.name));
                if (_managedSubjects.Count == 0)
                    LoadSubjectsFromSettings();
                else
                {
                    RebuildSubjectListUi();
                    PersistSubjectListLocalOnly(queueCloudPush: false);
                    uiQueue?.TryEnqueue(() => _onSettingsChanged?.Invoke());
                }
            }
            catch (Exception ex)
            {
                if (showErrors)
                    await ShowSimpleDialogAsync($"加载失败：{ex.Message}");
                LoadSubjectsFromSettings();
            }
        }

        private async Task SaveSubjectsToKvAsync(bool showErrors)
        {
            var settings = AppSettings.Values;
            var token = await GetKvTokenSnapshotAsync();

            PersistSubjectListLocalOnly(queueCloudPush: false);

            if (string.IsNullOrWhiteSpace(token))
            {
                if (showErrors)
                    await ShowSimpleDialogAsync("列表已保存到本机。若要同步到云端，请先填写 KV 授权令牌后再点保存。");
                _onSettingsChanged?.Invoke();
                return;
            }

            var ok = await TryPushSubjectsToKvCoreAsync(showErrors);
            if (ok)
                _onSettingsChanged?.Invoke();
        }

        private StackPanel BuildRosterManagementContent()
        {
            var root = new StackPanel { Spacing = 16 };

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
            titleStack.Children.Add(new FontIcon
            {
                Glyph = "\uE716",
                FontSize = 26,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"]
            });
            titleStack.Children.Add(new TextBlock
            {
                Text = "学生列表",
                FontSize = 24,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            });
            Grid.SetColumn(titleStack, 0);
            headerGrid.Children.Add(titleStack);

            var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            var sortBtn = new Button
            {
                Content = CreateIconTextRow("\uE8CB", "按姓名排序"),
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8, 4, 8, 4)
            };
            sortBtn.Click += (_, _) => SortRosterByName();
            var advBtn = new Button
            {
                Content = CreateIconTextRow("\uE943", "高级编辑"),
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8, 4, 8, 4)
            };
            advBtn.Click += async (_, _) => await AdvancedEditRosterAsync();
            var cloudReloadBtn = new Button
            {
                Content = CreateIconTextRow("\uE72C", "从云端加载"),
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8, 4, 8, 4),
                Foreground = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"]
            };
            cloudReloadBtn.Click += async (_, _) => await ReloadRosterFromKvAsync(showErrors: true);
            toolbar.Children.Add(sortBtn);
            toolbar.Children.Add(advBtn);
            toolbar.Children.Add(cloudReloadBtn);
            Grid.SetColumn(toolbar, 1);
            headerGrid.Children.Add(toolbar);
            root.Children.Add(headerGrid);

            var addRow = new Grid();
            addRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            addRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _rosterNameInput = new TextBox
            {
                PlaceholderText = "添加学生",
                MinHeight = 40,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                BorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1)
            };
            _rosterNameInput.KeyDown += RosterNameInput_KeyDown;
            Grid.SetColumn(_rosterNameInput, 0);
            var addBtn = new Button
            {
                Content = new FontIcon { Glyph = "\uE710", FontSize = 18 },
                MinWidth = 44,
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Stretch
            };
            addBtn.Click += (_, _) => TryAddRosterStudentFromInput();
            Grid.SetColumn(addBtn, 1);
            addRow.Children.Add(_rosterNameInput);
            addRow.Children.Add(addBtn);
            root.Children.Add(addRow);

            _rosterCardsGrid = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
            var rosterScroll = new ScrollViewer
            {
                MaxHeight = 520,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = _rosterCardsGrid
            };
            var rosterBorder = new Border
            {
                BorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8),
                Child = rosterScroll
            };
            root.Children.Add(rosterBorder);

            var footer = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, Margin = new Thickness(0, 8, 0, 0) };
            var saveRosterBtn = new Button
            {
                Content = CreateIconTextRow("\uE74E", "保存名单"),
                Padding = new Thickness(18, 10, 18, 10),
                CornerRadius = new CornerRadius(8),
                Background = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"],
                Foreground = new SolidColorBrush(Colors.White)
            };
            saveRosterBtn.Click += async (_, _) => await SaveRosterToKvAsync(showErrors: true);
            var resetRosterBtn = new Button
            {
                Content = CreateIconTextRow("\uE777", "重置名单"),
                Padding = new Thickness(16, 10, 16, 10),
                CornerRadius = new CornerRadius(8),
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"]
            };
            resetRosterBtn.Click += async (_, _) => await ResetRosterWithConfirmAsync();
            footer.Children.Add(saveRosterBtn);
            footer.Children.Add(resetRosterBtn);
            root.Children.Add(footer);

            return root;
        }

        private void RosterNameInput_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                TryAddRosterStudentFromInput();
                e.Handled = true;
            }
        }

        private void TryAddRosterStudentFromInput()
        {
            var name = _rosterNameInput.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(name))
                return;
            _rosterStudents.Add(name);
            _rosterNameInput.Text = "";
            RebuildRosterGridUi();
            PersistRosterLocalOnly();
        }

        private void SortRosterByName()
        {
            if (_rosterStudents.Count == 0)
                return;
            var comparer = StringComparer.Create(CultureInfo.GetCultureInfo("zh-CN"), CompareOptions.IgnoreCase);
            _rosterStudents.Sort(comparer);
            RebuildRosterGridUi();
            PersistRosterLocalOnly();
        }

        private async Task AdvancedEditRosterAsync()
        {
            var box = new TextBox
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 220,
                MinWidth = 360,
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                Text = string.Join(Environment.NewLine, _rosterStudents)
            };
            var dialog = new ContentDialog
            {
                Title = "高级编辑",
                Content = box,
                PrimaryButtonText = "应用",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Content.XamlRoot
            };
            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
                return;
            var lines = (box.Text ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            _rosterStudents.Clear();
            foreach (var line in lines)
            {
                var t = line.Trim();
                if (!string.IsNullOrEmpty(t))
                    _rosterStudents.Add(t);
            }
            RebuildRosterGridUi();
            PersistRosterLocalOnly();
        }

        private async Task ResetRosterWithConfirmAsync()
        {
            var dialog = new ContentDialog
            {
                Title = "重置名单",
                Content = "将清空当前名单（仍可随后从云端加载）。确定继续？",
                PrimaryButtonText = "清空",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = Content.XamlRoot
            };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;
            _rosterStudents.Clear();
            RebuildRosterGridUi();
            PersistRosterLocalOnly(queueCloudPush: false);
            await SaveRosterToKvAsync(showErrors: true);
        }

        private void LoadRosterFromSettings()
        {
            _rosterStudents.Clear();
            var raw = AppSettings.Values[RosterListKey] as string;
            if (!string.IsNullOrWhiteSpace(raw))
            {
                try
                {
                    var list = JsonSerializer.Deserialize<List<string>>(raw);
                    if (list != null)
                    {
                        foreach (var s in list)
                        {
                            if (!string.IsNullOrWhiteSpace(s))
                                _rosterStudents.Add(s.Trim());
                        }
                    }
                }
                catch
                {
                    // ignore invalid cache
                }
            }

            RebuildRosterGridUi();
        }

        private void PersistRosterLocalOnly(bool queueCloudPush = true)
        {
            AppSettings.Values[RosterListKey] = JsonSerializer.Serialize(_rosterStudents);
            if (queueCloudPush)
                ScheduleRosterCloudPush();
        }

        private async void ScheduleRosterCloudPush()
        {
            var dq = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            var generation = ++_rosterCloudPushGeneration;
            await Task.Delay(450).ConfigureAwait(false);
            if (generation != _rosterCloudPushGeneration)
                return;
            var ok = await TryPushRosterToKvCoreAsync(showErrors: false).ConfigureAwait(false);
            if (!ok)
                return;
            dq?.TryEnqueue(() => _onSettingsChanged?.Invoke());
        }

        private async Task<bool> TryPushRosterToKvCoreAsync(bool showErrors)
        {
            var settings = AppSettings.Values;
            var token = await GetKvTokenSnapshotAsync();
            if (string.IsNullOrWhiteSpace(token))
                return false;

            var baseUrl = (settings[ServerUrlKey] as string ?? "https://kv-service.wuyuan.dev").TrimEnd('/');
            try
            {
                var payload = new List<Dictionary<string, object>>();
                for (var i = 0; i < _rosterStudents.Count; i++)
                    payload.Add(new Dictionary<string, object> { ["order"] = i + 1, ["name"] = _rosterStudents[i] });

                var json = JsonSerializer.Serialize(payload);
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/kv/{ClassworksKvKeys.RosterConfig}")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());
                using var response = await _settingsHttpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    if (showErrors)
                        await ShowSimpleDialogAsync($"名单同步失败（HTTP {(int)response.StatusCode}）。");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                if (showErrors)
                    await ShowSimpleDialogAsync($"名单同步失败：{ex.Message}");
                return false;
            }
        }

        private async Task ReloadRosterFromKvAsync(bool showErrors)
        {
            var uiQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            var settings = AppSettings.Values;
            var token = await GetKvTokenSnapshotAsync();
            var baseUrl = (settings[ServerUrlKey] as string ?? "https://kv-service.wuyuan.dev").TrimEnd('/');
            if (string.IsNullOrWhiteSpace(token))
            {
                if (showErrors)
                    await ShowSimpleDialogAsync("请先配置 KV 授权令牌后再从云端加载名单。");
                LoadRosterFromSettings();
                return;
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/kv/{ClassworksKvKeys.RosterConfig}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());
                using var response = await _settingsHttpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    if (showErrors)
                        await ShowSimpleDialogAsync($"从云端加载名单失败（HTTP {(int)response.StatusCode}）。已显示本机缓存。");
                    LoadRosterFromSettings();
                    return;
                }

                using var doc = JsonDocument.Parse(body);
                var names = ParseRosterNamesFromJson(doc.RootElement);
                _rosterStudents.Clear();
                _rosterStudents.AddRange(names);
                RebuildRosterGridUi();
                PersistRosterLocalOnly(queueCloudPush: false);
                uiQueue?.TryEnqueue(() => _onSettingsChanged?.Invoke());
            }
            catch (Exception ex)
            {
                if (showErrors)
                    await ShowSimpleDialogAsync($"加载名单失败：{ex.Message}");
                LoadRosterFromSettings();
            }
        }

        private static List<string> ParseRosterNamesFromJson(JsonElement root)
        {
            var pairs = new List<(int order, string name)>();
            if (root.ValueKind != JsonValueKind.Array)
                return new List<string>();

            foreach (var el in root.EnumerateArray())
            {
                if (el.ValueKind == JsonValueKind.String)
                {
                    var n = el.GetString()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(n))
                        pairs.Add((pairs.Count, n));
                    continue;
                }

                if (el.ValueKind != JsonValueKind.Object)
                    continue;
                var order = pairs.Count;
                if (el.TryGetProperty("order", out var oEl) && oEl.ValueKind == JsonValueKind.Number)
                    order = oEl.GetInt32();
                var name = "";
                if (el.TryGetProperty("name", out var nEl) && nEl.ValueKind == JsonValueKind.String)
                    name = nEl.GetString()?.Trim() ?? "";
                if (!string.IsNullOrEmpty(name))
                    pairs.Add((order, name));
            }

            pairs.Sort((a, b) => a.order.CompareTo(b.order));
            return pairs.ConvertAll(p => p.name);
        }

        private async Task SaveRosterToKvAsync(bool showErrors)
        {
            var settings = AppSettings.Values;
            var token = await GetKvTokenSnapshotAsync();

            PersistRosterLocalOnly(queueCloudPush: false);

            if (string.IsNullOrWhiteSpace(token))
            {
                if (showErrors)
                    await ShowSimpleDialogAsync("名单已保存到本机。填写 KV 令牌后可同步到云端。");
                _onSettingsChanged?.Invoke();
                return;
            }

            var ok = await TryPushRosterToKvCoreAsync(showErrors);
            if (ok)
                _onSettingsChanged?.Invoke();
        }

        private void RebuildRosterGridUi()
        {
            _rosterCardsGrid.Children.Clear();
            _rosterCardsGrid.RowDefinitions.Clear();
            _rosterCardsGrid.ColumnDefinitions.Clear();

            const int cols = 4;
            var count = _rosterStudents.Count;
            var rows = count == 0 ? 1 : (count + cols - 1) / cols;
            for (var c = 0; c < cols; c++)
                _rosterCardsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (var r = 0; r < rows; r++)
                _rosterCardsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            for (var i = 0; i < count; i++)
            {
                var row = i / cols;
                var col = i % cols;
                var card = CreateStudentCard(i + 1, _rosterStudents[i], i);
                Grid.SetRow(card, row);
                Grid.SetColumn(card, col);
                _rosterCardsGrid.Children.Add(card);
            }
        }

        private Border CreateStudentCard(int displayNumber, string name, int listIndex)
        {
            var actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = 0
            };
            var editBtn = new Button
            {
                Content = new FontIcon { Glyph = "\uE70F", FontSize = 14 },
                Padding = new Thickness(4),
                MinWidth = 32,
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0)
            };
            var capturedIdx = listIndex;
            editBtn.Click += async (_, _) => await EditRosterStudentAsync(capturedIdx);
            var delBtn = new Button
            {
                Content = new FontIcon { Glyph = "\uE74D", FontSize = 14 },
                Padding = new Thickness(4),
                MinWidth = 32,
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"]
            };
            delBtn.Click += (_, _) => RemoveRosterAt(capturedIdx);
            actions.Children.Add(editBtn);
            actions.Children.Add(delBtn);

            var inner = new Grid();
            inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            inner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var numBorder = new Border
            {
                MinWidth = 28,
                Padding = new Thickness(8, 6, 8, 6),
                CornerRadius = new CornerRadius(4),
                Background = (Brush)Application.Current.Resources["ControlFillColorSecondaryBrush"],
                Child = new TextBlock
                {
                    Text = displayNumber.ToString(CultureInfo.InvariantCulture),
                    FontSize = 13,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
                }
            };
            Grid.SetColumn(numBorder, 0);

            var nameTb = new TextBlock
            {
                Text = name,
                FontSize = 15,
                Margin = new Thickness(10, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(nameTb, 1);

            Grid.SetColumn(actions, 2);
            inner.Children.Add(numBorder);
            inner.Children.Add(nameTb);
            inner.Children.Add(actions);

            var card = new Border
            {
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 8, 8, 8),
                Margin = new Thickness(4),
                Child = inner
            };
            card.PointerEntered += (_, _) => { actions.Opacity = 1; };
            card.PointerExited += (_, _) => { actions.Opacity = 0; };
            return card;
        }

        private async Task EditRosterStudentAsync(int index)
        {
            if (index < 0 || index >= _rosterStudents.Count)
                return;
            var box = new TextBox { Text = _rosterStudents[index], Width = 280 };
            var dialog = new ContentDialog
            {
                Title = "编辑姓名",
                Content = box,
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Content.XamlRoot
            };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;
            var t = box.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(t))
                return;
            _rosterStudents[index] = t;
            RebuildRosterGridUi();
            PersistRosterLocalOnly();
        }

        private void RemoveRosterAt(int index)
        {
            if (index < 0 || index >= _rosterStudents.Count)
                return;
            _rosterStudents.RemoveAt(index);
            RebuildRosterGridUi();
            PersistRosterLocalOnly();
        }

        private StackPanel BuildRefreshSettingsContent()
        {
            var root = new StackPanel { Spacing = 16 };

            var autoIcon = new Microsoft.UI.Xaml.Controls.ImageIcon
            {
                Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(AppSettings.GetAssetUri("icons/ic_public_refresh.ico")),
                Width = 20,
                Height = 20,
                VerticalAlignment = VerticalAlignment.Center,
            };

            var intervalIcon = new Microsoft.UI.Xaml.Controls.ImageIcon
            {
                Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(AppSettings.GetAssetUri("icons/ic_statusbar_alarm.ico")),
                Width = 20,
                Height = 20,
                VerticalAlignment = VerticalAlignment.Center,
            };

            var cardInner = new StackPanel { Spacing = 0 };
            cardInner.Children.Add(CreateRefreshSettingCardRow(autoIcon, "自动刷新", "refresh.auto", _autoRefreshToggle, showDividerBelow: true));
            cardInner.Children.Add(CreateRefreshSettingCardRow(intervalIcon, "刷新间隔", "refresh.interval", _autoRefreshIntervalBox, showDividerBelow: false));

            root.Children.Add(new Border
            {
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(0, 2, 0, 2),
                MaxWidth = 920,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Child = cardInner
            });

            return root;
        }

        private Border CreateRefreshSettingCardRow(
            FrameworkElement leadingIcon,
            string primaryText,
            string keyText,
            FrameworkElement trailingControl,
            bool showDividerBelow)
        {
            var labelStack = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
            labelStack.Children.Add(new TextBlock
            {
                Text = primaryText,
                FontSize = 15,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            labelStack.Children.Add(new TextBlock
            {
                Text = keyText,
                FontSize = 12,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });

            var iconHost = new Grid
            {
                Width = 32,
                Height = 32,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            leadingIcon.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            leadingIcon.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            iconHost.Children.Add(leadingIcon);

            trailingControl.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            trailingControl.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right);

            var rowGrid = new Grid
            {
                Padding = new Thickness(16, 12, 16, 12),
                ColumnSpacing = 16
            };
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(iconHost, 0);
            Grid.SetColumn(labelStack, 1);
            Grid.SetColumn(trailingControl, 2);
            rowGrid.Children.Add(iconHost);
            rowGrid.Children.Add(labelStack);
            rowGrid.Children.Add(trailingControl);

            var outer = new StackPanel { Spacing = 0 };
            outer.Children.Add(rowGrid);
            if (showDividerBelow)
            {
                outer.Children.Add(new Border
                {
                    Height = 1,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Background = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"],
                    Margin = new Thickness(16, 0, 16, 0)
                });
            }

            return new Border { Child = outer };
        }

        private StackPanel BuildEditSettingsContent()
        {
            var root = new StackPanel { Spacing = 16 };

            var cardInner = new StackPanel { Spacing = 0 };
            cardInner.Children.Add(CreateEditToggleSettingCardRow(EditSettingsGlyph("\uE74E"), "是否启用自动保存", "edit.autoSave", _editAutoSaveToggle, showDividerBelow: true));
            cardInner.Children.Add(CreateEditToggleSettingCardRow(EditSettingsGlyph("\uE787"), "禁止写入非当天作业数据", "edit.blockNonTodayWrite", _editBlockNonTodayAutoSaveToggle, showDividerBelow: true));
            cardInner.Children.Add(CreateEditToggleSettingCardRow(EditSettingsGlyph("\uE73E"), "保存非当天数据需确认", "edit.confirmNonTodaySave", _editConfirmNonTodaySaveToggle, showDividerBelow: true));
            cardInner.Children.Add(CreateEditToggleSettingCardRow(EditSettingsGlyph("\uE72C"), "编辑前是否自动刷新", "edit.refreshBeforeEdit", _editRefreshBeforeEditToggle, showDividerBelow: true));
            cardInner.Children.Add(CreateEditCompoundTextSettingRow(EditSettingsGlyph("\uE8A5"), "自动保存模式提示文本", "edit.autoSavePromptText", _editAutoSavePromptTextBox, showDividerBelow: true));
            cardInner.Children.Add(CreateEditCompoundTextSettingRow(EditSettingsGlyph("\uE8A5"), "手动保存模式提示文本", "edit.manualSavePromptText", _editManualSavePromptTextBox, showDividerBelow: false));

            root.Children.Add(new Border
            {
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(0, 2, 0, 2),
                MaxWidth = 920,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Child = cardInner
            });

            return root;
        }

        private StackPanel BuildUpdateSettingsContent()
        {
            var root = new StackPanel { Spacing = 16 };

            // 当前渠道显示
            var currentChannel = UpdateService.GetUpdateChannel();
            var channelText = currentChannel == "beta" ? "测试版 (Beta)" : "正式版 (Stable)";

            var channelInfoBlock = new TextBlock
            {
                Text = $"当前更新渠道: {channelText}",
                FontSize = 14,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                Margin = new Thickness(0, 0, 0, 8)
            };
            root.Children.Add(channelInfoBlock);

            // 渠道选择卡片
            var cardInner = new StackPanel { Spacing = 0 };

            // 正式版选项
            var stableRow = CreateUpdateChannelRow("正式版 (Stable)", "获取稳定可靠的正式版本更新。", "stable", currentChannel == "stable");
            cardInner.Children.Add(stableRow);

            // 分隔线
            cardInner.Children.Add(new Border
            {
                Height = 1,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"],
                Margin = new Thickness(16, 0, 16, 0)
            });

            // 测试版选项
            var betaRow = CreateUpdateChannelRow("测试版 (Beta)", "获取最新功能的测试版本，可能存在不稳定因素。", "beta", currentChannel == "beta");
            cardInner.Children.Add(betaRow);

            root.Children.Add(new Border
            {
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(0, 2, 0, 2),
                MaxWidth = 920,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Child = cardInner
            });

            return root;
        }

        private Grid CreateUpdateChannelRow(string title, string description, string channelValue, bool isSelected)
        {
            var radioButton = new RadioButton
            {
                IsChecked = isSelected,
                GroupName = "UpdateChannel",
                Tag = channelValue,
                VerticalAlignment = VerticalAlignment.Center
            };
            radioButton.Checked += (s, e) =>
            {
                if (s is RadioButton rb && rb.Tag is string newChannel)
                {
                    UpdateService.SetUpdateChannel(newChannel);
                }
            };

            var labelStack = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
            labelStack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 15,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            labelStack.Children.Add(new TextBlock
            {
                Text = description,
                FontSize = 12,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });

            var rowGrid = new Grid
            {
                Padding = new Thickness(16, 12, 16, 12),
                ColumnSpacing = 16
            };
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Grid.SetColumn(radioButton, 0);
            Grid.SetColumn(labelStack, 1);
            rowGrid.Children.Add(radioButton);
            rowGrid.Children.Add(labelStack);

            return rowGrid;
        }

        private static FrameworkElement EditSettingsGlyph(string glyph)
        {
            return new FontIcon
            {
                Glyph = glyph,
                FontSize = 18,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            };
        }

        private static Button CreateEditRowMoreButton()
        {
            var moreButton = new Button
            {
                Padding = new Thickness(4),
                MinWidth = 36,
                MinHeight = 36,
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(8),
                Content = new FontIcon
                {
                    Glyph = "\uE712",
                    FontSize = 14,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            moreButton.Click += (_, _) => { };
            return moreButton;
        }

        private Border CreateEditToggleSettingCardRow(
            FrameworkElement leadingIcon,
            string primaryText,
            string keyText,
            ToggleSwitch toggle,
            bool showDividerBelow)
        {
            var labelStack = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
            labelStack.Children.Add(new TextBlock
            {
                Text = primaryText,
                FontSize = 15,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            labelStack.Children.Add(new TextBlock
            {
                Text = keyText,
                FontSize = 12,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });

            var iconHost = new Grid
            {
                Width = 32,
                Height = 32,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            leadingIcon.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            leadingIcon.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            iconHost.Children.Add(leadingIcon);

            toggle.VerticalAlignment = VerticalAlignment.Center;
            toggle.HorizontalAlignment = HorizontalAlignment.Right;

            var moreButton = CreateEditRowMoreButton();

            var rowGrid = new Grid
            {
                Padding = new Thickness(16, 12, 16, 12),
                ColumnSpacing = 16
            };
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(iconHost, 0);
            Grid.SetColumn(labelStack, 1);
            Grid.SetColumn(toggle, 2);
            Grid.SetColumn(moreButton, 3);
            rowGrid.Children.Add(iconHost);
            rowGrid.Children.Add(labelStack);
            rowGrid.Children.Add(toggle);
            rowGrid.Children.Add(moreButton);

            var outer = new StackPanel { Spacing = 0 };
            outer.Children.Add(rowGrid);
            if (showDividerBelow)
            {
                outer.Children.Add(new Border
                {
                    Height = 1,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Background = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"],
                    Margin = new Thickness(16, 0, 16, 0)
                });
            }

            return new Border { Child = outer };
        }

        private Border CreateEditCompoundTextSettingRow(
            FrameworkElement leadingIcon,
            string primaryText,
            string keyText,
            TextBox textBox,
            bool showDividerBelow)
        {
            var labelStack = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
            labelStack.Children.Add(new TextBlock
            {
                Text = primaryText,
                FontSize = 15,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            labelStack.Children.Add(new TextBlock
            {
                Text = keyText,
                FontSize = 12,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });

            var iconHost = new Grid
            {
                Width = 32,
                Height = 32,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            leadingIcon.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            leadingIcon.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            iconHost.Children.Add(leadingIcon);

            var moreButton = CreateEditRowMoreButton();

            var headerGrid = new Grid
            {
                Padding = new Thickness(16, 12, 16, 8),
                ColumnSpacing = 16
            };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(iconHost, 0);
            Grid.SetColumn(labelStack, 1);
            Grid.SetColumn(moreButton, 2);
            headerGrid.Children.Add(iconHost);
            headerGrid.Children.Add(labelStack);
            headerGrid.Children.Add(moreButton);

            textBox.Margin = new Thickness(16, 0, 16, 12);
            textBox.HorizontalAlignment = HorizontalAlignment.Stretch;

            var outer = new StackPanel { Spacing = 0 };
            outer.Children.Add(headerGrid);
            outer.Children.Add(textBox);
            if (showDividerBelow)
            {
                outer.Children.Add(new Border
                {
                    Height = 1,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Background = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"],
                    Margin = new Thickness(16, 0, 16, 0)
                });
            }

            return new Border { Child = outer };
        }

        private static StackPanel CreateCategoryView(params UIElement[] sections)
        {
            var panel = new StackPanel
            {
                Spacing = 16,
                Visibility = Visibility.Collapsed
            };

            foreach (var section in sections)
            {
                panel.Children.Add(section);
            }

            return panel;
        }

        private static Windows.UI.Color GetBrushColor(string resourceKey, Windows.UI.Color fallbackColor)
        {
            if (Application.Current.Resources.TryGetValue(resourceKey, out var value) && value is SolidColorBrush brush)
            {
                return brush.Color;
            }

            return fallbackColor;
        }

        private static Windows.UI.Color WithAlpha(Windows.UI.Color color, byte alpha)
        {
            return ColorHelper.FromArgb(alpha, color.R, color.G, color.B);
        }

        private static Border CreateSettingRow(string title, string description, params UIElement[] controls)
        {
            var labelStack = new StackPanel { Spacing = 4 };
            labelStack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 15,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            labelStack.Children.Add(new TextBlock
            {
                Text = description,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });

            var controlStack = new StackPanel
            {
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            foreach (var control in controls)
            {
                controlStack.Children.Add(control);
            }

            var grid = new Grid
            {
                Padding = new Thickness(16, 12, 16, 12),
                ColumnSpacing = 16
            };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(labelStack, 0);
            Grid.SetColumn(controlStack, 1);
            grid.Children.Add(labelStack);
            grid.Children.Add(controlStack);

            return new Border
            {
                CornerRadius = new CornerRadius(10),
                Child = grid
            };
        }

        private static double ConvertToDouble(object value, double defaultValue)
        {
            if (value is double d) return d;
            if (value is int i) return i;
            if (value is float f) return f;
            return defaultValue;
        }

        private static NumberBox CreateNumberBoxWithoutHeader(double minimum, double maximum, double step, double defaultValue)
        {
            return new NumberBox
            {
                Minimum = minimum,
                Maximum = maximum,
                SmallChange = step,
                LargeChange = step * 5,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
                Value = defaultValue,
                MinWidth = 120
            };
        }

        private static Border CreateSectionCard(string title, string description, params UIElement[] children)
        {
            var stack = new StackPanel { Spacing = 12 };
            stack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 18,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            stack.Children.Add(new TextBlock
            {
                Text = description,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });

            foreach (var child in children)
            {
                stack.Children.Add(child);
            }

            return new Border
            {
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20),
                Child = stack
            };
        }

        private async Task ApplyAutoStartAsync()
        {
            if (_isAutoSaveSuspended)
                return;

            await Task.Run(() =>
            {
                try
                {
                    RemoveAllAutoStartEntries();
                    if (_autoStartToggle.IsOn)
                        ApplySelectedAutoStartMethod();
                }
                catch
                {
                    // 静默处理自启动设置失败
                }
            });
        }

        private void RemoveAllAutoStartEntries()
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath))
                return;

            RemoveRegistryAutoStart();
            RemoveStartupFolderShortcut();
            RemoveScheduledTask();
        }

        private void ApplySelectedAutoStartMethod()
        {
            var methodIndex = _autoStartMethodCombo.SelectedIndex;
            if (methodIndex < 0 || methodIndex >= AutoStartMethodOptions.Length)
                methodIndex = 0;

            switch (methodIndex)
            {
                case 0:
                    SetRegistryAutoStart();
                    break;
                case 1:
                    SetStartupFolderAutoStart();
                    break;
                case 2:
                    SetScheduledTaskAutoStart();
                    break;
            }
        }

        private static void RemoveRegistryAutoStart()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
                key?.DeleteValue("CSD", throwOnMissingValue: false);
            }
            catch
            {
            }
        }

        private static void SetRegistryAutoStart()
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath))
                return;

            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run");
                key?.SetValue("CSD", $"\"{exePath}\"");
            }
            catch
            {
            }
        }

        private static void RemoveStartupFolderShortcut()
        {
            try
            {
                var startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                var shortcutPath = System.IO.Path.Combine(startupPath, "CSD.lnk");
                if (File.Exists(shortcutPath))
                    File.Delete(shortcutPath);
            }
            catch
            {
            }
        }

        private static void SetStartupFolderAutoStart()
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath))
                return;

            try
            {
                var startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                Directory.CreateDirectory(startupPath);
                var shortcutPath = System.IO.Path.Combine(startupPath, "CSD.lnk");

                var shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                    return;

                dynamic shell = Activator.CreateInstance(shellType)!;
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = exePath;
                shortcut.WorkingDirectory = System.IO.Path.GetDirectoryName(exePath);
                shortcut.Description = "CSD - 课堂作业展示系统";
                shortcut.Save();
            }
            catch
            {
            }
        }

        private static void RemoveScheduledTask()
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo("schtasks.exe", "/delete /tn \"CSD_AutoStart\" /f")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };
                var process = System.Diagnostics.Process.Start(psi);
                process?.WaitForExit(5000);
            }
            catch
            {
            }
        }

        private static void SetScheduledTaskAutoStart()
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath))
                return;

            try
            {
                var arguments = $"/create /tn \"CSD_AutoStart\" /tr \"\\\"{exePath}\\\"\" /sc onlogon /rl limited /f /delay 0000:30";
                var psi = new System.Diagnostics.ProcessStartInfo("schtasks.exe", arguments)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };
                var process = System.Diagnostics.Process.Start(psi);
                process?.WaitForExit(5000);
            }
            catch
            {
            }
        }

        private async void DestroyTokenButton_Click(object sender, RoutedEventArgs e)
        {
            // 弹出确认对话框
            var dialog = new ContentDialog
            {
                Title = "确认销毁 Token",
                Content = "是否重新初始化并重启应用？",
                PrimaryButtonText = "是",
                CloseButtonText = "否",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Content.XamlRoot
            };
            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
                return;

            // 清除 Token
            AppSettings.Values.Remove(TokenKey);

            // 重启应用
            var exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath))
            {
                System.Diagnostics.Process.Start(exePath);
            }

            // 退出当前应用
            Application.Current.Exit();
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = "CSD设置"
            };
            savePicker.FileTypeChoices.Add("JSON 文件", new List<string> { ".json" });
            InitializeWithWindow.Initialize(savePicker, WindowNative.GetWindowHandle(this));

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                var settings = AppSettings.Values;
                var data = new Dictionary<string, object>();
                foreach (var kvp in settings)
                    data[kvp.Key] = kvp.Value ?? "";
                var json = JsonSerializer.Serialize(data, AppJsonIndentedSerializerContext.Default.DictionaryStringObject);
                await FileIO.WriteTextAsync(file, json);
            }
        }

        private void RequestEditPrefsPullFromCloudIfNeeded()
        {
            if (_editPrefsPulledThisSession)
                return;
            _editPrefsPulledThisSession = true;
            _ = PullEditPrefsFromCloudAndApplyUiAsync();
        }

        private async Task PullEditPrefsFromCloudAndApplyUiAsync()
        {
            var uiDq = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            var token = await GetKvTokenSnapshotAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(token))
                return;

            var settings = AppSettings.Values;
            var baseUrl = (settings[ServerUrlKey] as string ?? "https://kv-service.wuyuan.dev").TrimEnd('/');
            var ok = await EditPreferencesSync.TryPullMergeIntoAppSettingsAsync(_settingsHttpClient, baseUrl, token).ConfigureAwait(false);
            if (!ok)
                return;

            uiDq?.TryEnqueue(() =>
            {
                _isAutoSaveSuspended = true;
                try
                {
                    ApplyEditControlsFromAppSettings();
                }
                finally
                {
                    _isAutoSaveSuspended = false;
                }

                _onSettingsChanged?.Invoke();
            });
        }

        private void ApplyEditControlsFromAppSettings()
        {
            var s = AppSettings.Values;
            _editAutoSaveToggle.IsOn = s.ContainsKey(EditPreferencesKeys.AutoSave) && (bool)(s[EditPreferencesKeys.AutoSave] ?? false);
            _editBlockNonTodayAutoSaveToggle.IsOn = s.ContainsKey(EditPreferencesKeys.BlockNonTodayAutoSave) && (bool)(s[EditPreferencesKeys.BlockNonTodayAutoSave] ?? false);
            _editConfirmNonTodaySaveToggle.IsOn = s.ContainsKey(EditPreferencesKeys.ConfirmNonTodaySave) && (bool)(s[EditPreferencesKeys.ConfirmNonTodaySave] ?? false);
            _editRefreshBeforeEditToggle.IsOn = s.ContainsKey(EditPreferencesKeys.RefreshBeforeEdit) && (bool)(s[EditPreferencesKeys.RefreshBeforeEdit] ?? false);
            _editAutoSavePromptTextBox.Text = s[EditPreferencesKeys.AutoSavePromptText] as string ?? "";
            _editManualSavePromptTextBox.Text = s[EditPreferencesKeys.ManualSavePromptText] as string ?? "";
        }

        private async void ScheduleEditPrefsCloudPush()
        {
            var dq = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            var generation = ++_editCloudPushGeneration;
            await Task.Delay(450).ConfigureAwait(false);
            if (generation != _editCloudPushGeneration)
                return;
            _ = await TryPushEditPrefsToKvCoreAsync(showErrors: false).ConfigureAwait(false);
            dq?.TryEnqueue(() => _onSettingsChanged?.Invoke());
        }

        private async Task<bool> TryPushEditPrefsToKvCoreAsync(bool showErrors)
        {
            var token = await GetKvTokenSnapshotAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(token))
            {
                if (showErrors)
                    await ShowSimpleDialogAsync("请先填写 KV 令牌后再同步编辑偏好。");
                return false;
            }

            var settings = AppSettings.Values;
            var baseUrl = (settings[ServerUrlKey] as string ?? "https://kv-service.wuyuan.dev").TrimEnd('/');
            var ok = await EditPreferencesSync.PushAsync(_settingsHttpClient, baseUrl, token).ConfigureAwait(false);
            if (!ok && showErrors)
                await ShowSimpleDialogAsync("编辑偏好未能写入云端，请检查网络与令牌。");
            return ok;
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var openPicker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            openPicker.FileTypeFilter.Add(".json");
            InitializeWithWindow.Initialize(openPicker, WindowNative.GetWindowHandle(this));

            var file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                try
                {
                    var json = await FileIO.ReadTextAsync(file);
                    var data = JsonSerializer.Deserialize(json, AppJsonSerializerContext.Default.DictionaryStringJsonElement);
                    if (data == null) return;

                    var settings = AppSettings.Values;
                    foreach (var kvp in data)
                    {
                        if (kvp.Value.ValueKind == JsonValueKind.String)
                            settings[kvp.Key] = kvp.Value.GetString() ?? string.Empty;
                        else if (kvp.Value.ValueKind == JsonValueKind.Number)
                            settings[kvp.Key] = kvp.Value.GetDouble();
                        else if (kvp.Value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                            settings[kvp.Key] = kvp.Value.GetBoolean();
                    }

                    // 刷新 UI
                    _isAutoSaveSuspended = true;
                    _minCardWidthBox.Value = (double)(settings[MinCardWidthKey] ?? 220.0);
                    _cardGapBox.Value = (double)(settings[CardGapKey] ?? 14.0);
                    _subjectFontSizeBox.Value = (double)(settings[SubjectFontSizeKey] ?? 26.0);
                    _contentFontSizeBox.Value = (double)(settings[ContentFontSizeKey] ?? 20.0);
                    _serverUrlBox.Text = settings[ServerUrlKey] as string ?? "https://kv-service.wuyuan.dev";
                    _autoRefreshToggle.IsOn = settings.ContainsKey(AutoRefreshEnabledKey) ? (bool)(settings[AutoRefreshEnabledKey] ?? false) : false;
                    _autoRefreshIntervalBox.Value = (double)(settings[AutoRefreshIntervalKey] ?? 60.0);
                    _carouselIntervalBox.Value = (double)(settings[CarouselIntervalKey] ?? 5.0);
                    _carouselFontSizeBox.Value = (double)(settings[CarouselFontSizeKey] ?? 48.0);
                    _debugModeToggle.IsOn = settings.ContainsKey(DebugModeKey) && (bool)(settings[DebugModeKey] ?? false);
                    ApplyEditControlsFromAppSettings();
                    var importedToken = settings[TokenKey] as string ?? "";
                    _kvTokenBox.Text = importedToken;
                    _currentTokenText.Text = string.IsNullOrWhiteSpace(importedToken) ? "未设置" : "已设置";
                    ApplySavedDataProvider(settings[DataProviderKey] as string);
                    LoadSubjectsFromSettings();
                    LoadRosterFromSettings();
                    _isAutoSaveSuspended = false;
                    PersistSettings();
                    ScheduleEditPrefsCloudPush();
                }
                catch (Exception ex)
                {
                    _isAutoSaveSuspended = false;
                    var dialog = new ContentDialog
                    {
                        Title = "导入失败",
                        Content = ex.Message,
                        CloseButtonText = "确定",
                        XamlRoot = Content.XamlRoot
                    };
                    _ = dialog.ShowAsync();
                }
            }
        }
    }
}
