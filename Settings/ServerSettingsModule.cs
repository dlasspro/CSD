using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace CSD.Settings
{
    public class ServerSettingsModule : SettingsModuleBase
    {
        public override string CategoryKey => "server";
        public override string Title => "服务器";
        public override string Description => "";
        public override string ImageIconUri => AppSettings.GetAssetUri("icons/ic_gallery_cloud_synchronization.ico").AbsoluteUri;

        private ComboBox _dataProviderCombo = null!;
        private TextBox _serverUrlBox = null!;
        private TextBox _kvTokenBox = null!;
        private TextBlock _deviceOwnerTitleText = null!;
        private TextBlock _deviceOwnerSubText = null!;
        private TextBlock _deviceNameText = null!;
        private TextBlock _deviceIdText = null!;
        private TextBlock _deviceCreatedText = null!;
        private TextBlock _deviceUpdatedText = null!;
        
        private UIElement? _cloudStorageContent;
        private UIElement? _cloudStorageBanner;
        private UIElement? _localStorageBanner;
        private UIElement? _customServerBanner;
        private UIElement? _customServerContent;

        private static readonly string[] DataProviderOptions =
        [
            "Classworks 云端存储",
            "本地存储",
            "自定义远程服务器"
        ];

        private bool _hasAutoRefreshedDeviceInfo;

        protected override FrameworkElement BuildContent()
        {
            _serverUrlBox = new TextBox { PlaceholderText = "https://kv-service.wuyuan.dev" };
            _kvTokenBox = new TextBox
            {
                AcceptsReturn = false,
                TextWrapping = TextWrapping.NoWrap,
                FontFamily = new FontFamily("Consolas"),
                MinHeight = 40,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                PlaceholderText = "粘贴 KV 授权令牌"
            };

            _deviceOwnerTitleText = new TextBlock { Text = "未知管理员", FontSize = 24, FontWeight = Microsoft.UI.Text.FontWeights.Bold, TextWrapping = TextWrapping.Wrap, Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"] };
            _deviceOwnerSubText = SettingsUIHelper.CreateSecondaryWrappedText("管理员账号 ID: —\n此设备由贵校或贵单位管理，该管理员系此空间所有者，如有疑问请咨询他，对于恶意绑定、滥用行为请反馈。", 13);
            _deviceNameText = SettingsUIHelper.CreateSecondaryWrappedText("—");
            _deviceIdText = SettingsUIHelper.CreateSecondaryWrappedText("—");
            _deviceCreatedText = SettingsUIHelper.CreateSecondaryWrappedText("—");
            _deviceUpdatedText = SettingsUIHelper.CreateSecondaryWrappedText("—");

            _dataProviderCombo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, MinHeight = 40 };
            foreach (var label in DataProviderOptions)
                _dataProviderCombo.Items.Add(label);

            var root = new StackPanel { Spacing = 22 };

            var titleRow = new StackPanel();
            titleRow.Children.Add(new TextBlock { Text = "数据源设置", FontSize = 26, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
            root.Children.Add(titleRow);

            root.Children.Add(new TextBlock { Text = "数据提供者", FontSize = 13, Margin = new Thickness(0, 2, 0, 4), Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"] });
            root.Children.Add(_dataProviderCombo);

            var bannerHost = new Grid();
            _cloudStorageBanner = CreateBanner("Classworks 云端存储", "Classworks云端存储是官方提供的存储解决方案，自动配置了最优的访问设置。\n使用此选项时，服务器域名和网站令牌将自动配置，无需手动设置。", "\uE946", ColorHelper.FromArgb(255, 26, 56, 42), ColorHelper.FromArgb(255, 165, 224, 190));
            _localStorageBanner = CreateBanner("本地存储", "无需网络连接即可使用。Classworks 将把所有数据保存在本地设备上。使用本地存储意味着您无法使用多设备同步功能，但可以在没有网络的环境下正常使用软件的基础功能。", "\uE8B7", ColorHelper.FromArgb(255, 26, 42, 56), ColorHelper.FromArgb(255, 165, 190, 224));
            _customServerBanner = CreateBanner("自定义远程服务器", "KV存储系统使用本机唯一标识符(UUID)来区分不同设备的数据。\n服务器端点格式: http(s)://服务器域名/\n在服务器域名处仅填写基础URL，不需要任何路径。", "\uE774", ColorHelper.FromArgb(255, 56, 42, 26), ColorHelper.FromArgb(255, 224, 190, 165));
            bannerHost.Children.Add(_cloudStorageBanner);
            bannerHost.Children.Add(_localStorageBanner);
            bannerHost.Children.Add(_customServerBanner);
            root.Children.Add(bannerHost);

            _cloudStorageContent = BuildCloudContent();
            root.Children.Add(_cloudStorageContent);

            _customServerContent = BuildCustomServerContent();
            root.Children.Add(_customServerContent);

            return root;
        }

        private UIElement CreateBanner(string title, string desc, string glyph, Windows.UI.Color bg, Windows.UI.Color fg)
        {
            var stack = new StackPanel { Spacing = 6 };
            stack.Children.Add(new TextBlock { Text = title, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = new SolidColorBrush(fg), TextWrapping = TextWrapping.Wrap });
            stack.Children.Add(new TextBlock { Text = desc, FontSize = 13, Foreground = new SolidColorBrush(fg), TextWrapping = TextWrapping.Wrap, Opacity = 0.95 });
            
            var inner = new Grid { ColumnSpacing = 12 };
            inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            inner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var icon = new FontIcon { Glyph = glyph, FontSize = 18, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 2, 0, 0), Foreground = new SolidColorBrush(fg) };
            Grid.SetColumn(icon, 0);
            Grid.SetColumn(stack, 1);
            inner.Children.Add(icon);
            inner.Children.Add(stack);
            
            return new Border { Background = new SolidColorBrush(bg), CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 12, 14, 12), Child = inner, Visibility = Visibility.Collapsed };
        }

        private UIElement BuildCloudContent()
        {
            var stack = new StackPanel { Spacing = 22 };

            var tokenContainer = new StackPanel { Spacing = 4 };
            tokenContainer.Children.Add(CreateFieldWithIcon("\uEA18", "KV 授权令牌", _kvTokenBox));
            tokenContainer.Children.Add(new TextBlock { Text = "令牌用于云端存储授权", FontSize = 12, Margin = new Thickness(34, 0, 0, 0), Foreground = (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"] });
            stack.Children.Add(tokenContainer);

            var accountHeader = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(0, 6, 0, 0) };
            accountHeader.Children.Add(new FontIcon { Glyph = "\uE77B", FontSize = 20, VerticalAlignment = VerticalAlignment.Center, Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"] });
            accountHeader.Children.Add(new TextBlock { Text = "账号信息", FontSize = 18, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
            stack.Children.Add(accountHeader);

            var manageCardInner = new StackPanel { Spacing = 8 };
            manageCardInner.Children.Add(_deviceOwnerTitleText);
            manageCardInner.Children.Add(_deviceOwnerSubText);
            stack.Children.Add(SettingsUIHelper.CreateFilledCard(manageCardInner));

            var deviceHeader = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(0, 6, 0, 0) };
            deviceHeader.Children.Add(new Image { Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(AppSettings.GetAssetUri("icons/ic_device_matebook.ico")), Width = 20, Height = 20, VerticalAlignment = VerticalAlignment.Center });
            deviceHeader.Children.Add(new TextBlock { Text = "设备信息", FontSize = 18, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
            stack.Children.Add(deviceHeader);

            var techInner = new StackPanel { Spacing = 12 };
            techInner.Children.Add(CreateDeviceIconRow("\uE8EC", _deviceNameText));
            techInner.Children.Add(CreateDeviceIconRow("\uE716", _deviceIdText));
            techInner.Children.Add(CreateDeviceIconRow("\uE787", _deviceCreatedText));
            techInner.Children.Add(CreateDeviceIconRow("\uE72C", _deviceUpdatedText));
            stack.Children.Add(SettingsUIHelper.CreateFilledCard(techInner));

            var refreshBtn = new Button
            {
                Content = "刷新设备信息",
                Background = new SolidColorBrush(SettingsUIHelper.WithAlpha(SettingsUIHelper.GetBrushColor("TextFillColorPrimaryBrush", Colors.White), 0)),
                BorderBrush = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                Foreground = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"],
                Padding = new Thickness(16, 8, 16, 8),
                CornerRadius = new CornerRadius(8)
            };
            refreshBtn.Click += async (_, _) => await RefreshDeviceInfoAsync(showErrors: true);

            var dangerBrush = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
            var reinitBtn = new Button
            {
                Content = "重新初始化云端存储",
                Background = new SolidColorBrush(SettingsUIHelper.WithAlpha(SettingsUIHelper.GetBrushColor("TextFillColorPrimaryBrush", Colors.White), 0)),
                BorderBrush = dangerBrush,
                BorderThickness = new Thickness(1),
                Foreground = dangerBrush,
                Padding = new Thickness(16, 8, 16, 8),
                CornerRadius = new CornerRadius(8)
            };
            reinitBtn.Click += ReinitBtn_Click;

            var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 12, Margin = new Thickness(0, 12, 0, 0) };
            footer.Children.Add(refreshBtn);
            footer.Children.Add(reinitBtn);
            stack.Children.Add(footer);

            return stack;
        }

        private UIElement BuildCustomServerContent()
        {
            var stack = new StackPanel { Spacing = 22, Visibility = Visibility.Collapsed };

            var customServerUrlContainer = new StackPanel { Spacing = 4 };
            customServerUrlContainer.Children.Add(CreateFieldWithIcon("\uE774", "服务器地址", _serverUrlBox));
            customServerUrlContainer.Children.Add(new TextBlock { Text = "KV存储系统使用本机唯一标识符(UUID)来区分不同设备的数据。\n服务器端点格式: http(s)://服务器域名/\n在服务器域名处仅填写基础URL，不需要任何路径。", FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(34, 0, 0, 0), Foreground = (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"] });
            stack.Children.Add(customServerUrlContainer);

            var customTokenBox = new TextBox { AcceptsReturn = false, TextWrapping = TextWrapping.NoWrap, FontFamily = new FontFamily("Consolas"), MinHeight = 40, HorizontalAlignment = HorizontalAlignment.Stretch, PlaceholderText = "粘贴 KV 授权令牌" };
            customTokenBox.TextChanged += (s, e) => { _kvTokenBox.Text = customTokenBox.Text; };
            _kvTokenBox.TextChanged += (s, e) => { customTokenBox.Text = _kvTokenBox.Text; };

            var customTokenContainer = new StackPanel { Spacing = 4 };
            customTokenContainer.Children.Add(CreateFieldWithIcon("\uEA18", "KV 授权令牌", customTokenBox));
            customTokenContainer.Children.Add(new TextBlock { Text = "令牌用于云端存储授权", FontSize = 12, Margin = new Thickness(34, 0, 0, 0), Foreground = (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"] });
            stack.Children.Add(customTokenContainer);

            return stack;
        }

        private Grid CreateFieldWithIcon(string glyph, string label, TextBox textBox)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            var icon = new FontIcon { Glyph = glyph, FontSize = 22, Margin = new Thickness(0, 14, 12, 0), VerticalAlignment = VerticalAlignment.Top, Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"] };
            Grid.SetColumn(icon, 0);

            var frame = new Grid();
            var border = new Border { BorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"], BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 16, 10, 10), Child = textBox };
            var labelChip = new Border { Background = (Brush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"], Padding = new Thickness(8, 0, 8, 0), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, -11, 0, 0), Child = new TextBlock { Text = label, FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold } };
            frame.Children.Add(border);
            frame.Children.Add(labelChip);
            
            Grid.SetColumn(frame, 1);
            grid.Children.Add(icon);
            grid.Children.Add(frame);
            return grid;
        }

        private static StackPanel CreateDeviceIconRow(string glyph, TextBlock line)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
            row.Children.Add(new FontIcon { Glyph = glyph, FontSize = 16, VerticalAlignment = VerticalAlignment.Center, Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"] });
            line.VerticalAlignment = VerticalAlignment.Center;
            row.Children.Add(line);
            return row;
        }

        protected override void LoadSettings()
        {
            var settings = AppSettings.Values;
            _serverUrlBox.Text = settings["Settings_ServerUrl"] as string ?? "https://kv-service.wuyuan.dev";
            _kvTokenBox.Text = settings["Token"] as string ?? "";

            var saved = settings["Settings_DataProvider"] as string;
            if (string.Equals(saved, "Classworks云端存储", StringComparison.Ordinal)) saved = DataProviderOptions[0];
            var idx = Array.IndexOf(DataProviderOptions, saved ?? "");
            _dataProviderCombo.SelectedIndex = idx >= 0 ? idx : 0;
            
            ApplyDataProviderVisibility(_dataProviderCombo.SelectedItem as string);
        }

        protected override void HookAutoSaveHandlers()
        {
            _serverUrlBox.TextChanged += (_, _) => NotifySettingsChanged();
            _kvTokenBox.TextChanged += (_, _) => NotifySettingsChanged();
            _dataProviderCombo.SelectionChanged += async (_, _) =>
            {
                if (_dataProviderCombo.SelectedItem is string selected)
                {
                    ApplyDataProviderVisibility(selected);
                    NotifySettingsChanged();

                    if (_cloudStorageContent != null && string.Equals(selected, DataProviderOptions[0], StringComparison.Ordinal))
                        await RefreshDeviceInfoAsync(showErrors: false);
                }
            };
        }

        public override void PersistSettings()
        {
            var settings = AppSettings.Values;
            settings["Settings_ServerUrl"] = _serverUrlBox.Text;

            if (_dataProviderCombo.SelectedItem is string providerLabel)
                settings["Settings_DataProvider"] = providerLabel;

            var tokenTrimmed = _kvTokenBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(tokenTrimmed))
                settings.Remove("Token");
            else
                settings["Token"] = tokenTrimmed;
        }

        public override async void OnNavigatedTo()
        {
            if (!_hasAutoRefreshedDeviceInfo && string.Equals(_dataProviderCombo.SelectedItem as string, DataProviderOptions[0], StringComparison.Ordinal))
            {
                _hasAutoRefreshedDeviceInfo = true;
                await RefreshDeviceInfoAsync(showErrors: false);
            }
        }

        private void ApplyDataProviderVisibility(string? selected)
        {
            bool isCloud = string.Equals(selected, DataProviderOptions[0], StringComparison.Ordinal);
            bool isLocal = string.Equals(selected, DataProviderOptions[1], StringComparison.Ordinal);
            bool isCustom = string.Equals(selected, DataProviderOptions[2], StringComparison.Ordinal);

            if (_cloudStorageContent != null) _cloudStorageContent.Visibility = isCloud ? Visibility.Visible : Visibility.Collapsed;
            if (_cloudStorageBanner != null) _cloudStorageBanner.Visibility = isCloud ? Visibility.Visible : Visibility.Collapsed;
            if (_localStorageBanner != null) _localStorageBanner.Visibility = isLocal ? Visibility.Visible : Visibility.Collapsed;
            if (_customServerBanner != null) _customServerBanner.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
            if (_customServerContent != null) _customServerContent.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
        }

        private async Task RefreshDeviceInfoAsync(bool showErrors)
        {
            var token = _kvTokenBox.Text?.Trim();
            var baseUrl = _serverUrlBox.Text.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(token))
            {
                if (showErrors) await ShowSimpleDialogAsync("请先填写或保存 KV 授权令牌。");
                return;
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/kv/_info");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using var response = await Context.HttpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    if (showErrors) await ShowSimpleDialogAsync($"未能获取设备信息（HTTP {(int)response.StatusCode}）。若服务端不提供 /kv/_info 接口，将保持占位内容。");
                    return;
                }

                using var doc = JsonDocument.Parse(body);
                ApplyDeviceInfoJson(doc.RootElement);
            }
            catch (Exception ex)
            {
                if (showErrors) await ShowSimpleDialogAsync($"刷新失败：{ex.Message}");
            }
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

            static string AsTrimmedString(JsonElement el) => el.ValueKind switch
            {
                JsonValueKind.String => el.GetString()?.Trim() ?? "",
                JsonValueKind.Number => el.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => ""
            };

            static string FindString(JsonElement obj, params string[] keys)
            {
                foreach (var key in keys)
                {
                    if (obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(key, out var v) && v.ValueKind != JsonValueKind.Null)
                    {
                        var s = AsTrimmedString(v);
                        if (!string.IsNullOrEmpty(s)) return s;
                    }
                }
                return "";
            }

            static string FormatDeviceTime(string raw)
            {
                if (string.IsNullOrEmpty(raw)) return "—";
                if (DateTime.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var utc))
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

        private async void ReinitBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "确认销毁 Token",
                Content = "是否重新初始化并重启应用？",
                PrimaryButtonText = "是",
                CloseButtonText = "否",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Context.Window.Content.XamlRoot
            };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;

            AppSettings.Values.Remove("Token");
            var exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath))
                System.Diagnostics.Process.Start(exePath);
            Application.Current.Exit();
        }

        private async Task ShowSimpleDialogAsync(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "数据源",
                Content = message,
                CloseButtonText = "确定",
                XamlRoot = Context.Window.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}