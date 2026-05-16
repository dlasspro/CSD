using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace CSD.Settings
{
    public class AccountSettingsModule : SettingsModuleBase
    {
        public override string CategoryKey => "account";
        public override string Title => "账户与数据";
        public override string Description => "查看 Token 状态，并进行本地设置导入导出。";
        public override string Glyph => "\uE716";

        private TextBlock _currentTokenText = null!;
        private PasswordBox _tokenInputBox = null!;

        protected override FrameworkElement BuildContent()
        {
            var tokenSection = new StackPanel { Spacing = 4 };
            tokenSection.Children.Add(new TextBlock { Text = "当前 Token 状态" });
            
            _currentTokenText = new TextBlock
            {
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            };
            tokenSection.Children.Add(_currentTokenText);

            var destroyTokenButton = new Button { Content = "销毁 Token", Margin = new Thickness(0, 4, 0, 0) };
            destroyTokenButton.Click += DestroyTokenButton_Click;
            tokenSection.Children.Add(destroyTokenButton);

            var inputTokenSection = new StackPanel { Spacing = 8 };
            _tokenInputBox = new PasswordBox
            {
                Header = "输入 Token",
                PlaceholderText = "输入 KV 授权令牌",
                Width = 320
            };
            inputTokenSection.Children.Add(_tokenInputBox);

            var applyTokenButton = new Button { Content = "应用 Token", Width = 120 };
            applyTokenButton.Click += ApplyTokenButton_Click;
            inputTokenSection.Children.Add(applyTokenButton);

            var exportButton = new Button { Content = "导出设置", HorizontalAlignment = HorizontalAlignment.Stretch };
            exportButton.Click += ExportButton_Click;

            var importButton = new Button { Content = "导入设置", HorizontalAlignment = HorizontalAlignment.Stretch };
            importButton.Click += ImportButton_Click;

            var webSettingsButton = new Button { Content = "网页端设置", HorizontalAlignment = HorizontalAlignment.Stretch };
            webSettingsButton.Click += (_, _) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://cs.houlang.cloud/settings", UseShellExecute = true });
                }
                catch { }
            };

            var ioStack = new StackPanel { Spacing = 8 };
            ioStack.Children.Add(exportButton);
            ioStack.Children.Add(importButton);
            ioStack.Children.Add(webSettingsButton);

            return SettingsUIHelper.CreateCategoryView(
                SettingsUIHelper.CreateSettingRow("当前 Token 状态", "查看授权状态并可重置。", tokenSection),
                SettingsUIHelper.CreateSettingRow("输入 Token", "在此输入新的 KV 授权令牌。", inputTokenSection),
                SettingsUIHelper.CreateSettingRow("数据管理", "导入、导出本地设置，或前往网页端。", ioStack));
        }

        protected override void LoadSettings()
        {
            var settings = AppSettings.Values;
            var token = settings["Token"] as string ?? "";
            _currentTokenText.Text = string.IsNullOrWhiteSpace(token) ? "未设置" : "已设置";
        }

        private async void DestroyTokenButton_Click(object sender, RoutedEventArgs e)
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

        private async void ApplyTokenButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_tokenInputBox.Password))
                return;

            AppSettings.Values["Token"] = _tokenInputBox.Password;
            _currentTokenText.Text = "已设置";
            _tokenInputBox.Password = string.Empty;

            var dialog = new ContentDialog
            {
                Title = "Token 已更新",
                Content = "Token 已成功保存。是否重新启动应用？",
                PrimaryButtonText = "重新启动",
                CloseButtonText = "稍后",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Context.Window.Content.XamlRoot
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                    System.Diagnostics.Process.Start(exePath);
                Application.Current.Exit();
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = "CSD设置"
            };
            savePicker.FileTypeChoices.Add("JSON 文件", new List<string> { ".json" });
            InitializeWithWindow.Initialize(savePicker, WindowNative.GetWindowHandle(Context.Window));

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                var settings = AppSettings.Values;
                var data = new Dictionary<string, object>();
                foreach (var kvp in settings)
                    data[kvp.Key] = kvp.Value ?? "";
                var json = JsonSerializer.Serialize(data, AppJsonIndentedSerializerContext.Default.DictionaryStringObject);
                await Windows.Storage.FileIO.WriteTextAsync(file, json);
            }
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var openPicker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary };
            openPicker.FileTypeFilter.Add(".json");
            InitializeWithWindow.Initialize(openPicker, WindowNative.GetWindowHandle(Context.Window));

            var file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                try
                {
                    var json = await Windows.Storage.FileIO.ReadTextAsync(file);
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

                    NotifySettingsChanged();
                }
                catch (Exception ex)
                {
                    var dialog = new ContentDialog
                    {
                        Title = "导入失败",
                        Content = ex.Message,
                        CloseButtonText = "确定",
                        XamlRoot = Context.Window.Content.XamlRoot
                    };
                    _ = dialog.ShowAsync();
                }
            }
        }
    }
}