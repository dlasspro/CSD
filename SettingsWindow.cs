using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace CSD
{
    public class SettingsWindow : Window
    {
        private const string MinCardWidthKey = "Settings_MinCardWidth";
        private const string CardGapKey = "Settings_CardGap";
        private const string SubjectFontSizeKey = "Settings_SubjectFontSize";
        private const string ContentFontSizeKey = "Settings_ContentFontSize";
        private const string ServerUrlKey = "Settings_ServerUrl";
        private const string TokenKey = "Token";

        private readonly Action _onSettingsChanged;

        private readonly NumberBox _minCardWidthBox;
        private readonly NumberBox _cardGapBox;
        private readonly NumberBox _subjectFontSizeBox;
        private readonly NumberBox _contentFontSizeBox;
        private readonly TextBox _serverUrlBox;
        private readonly TextBlock _currentTokenText;

        public SettingsWindow(Action onSettingsChanged)
        {
            _onSettingsChanged = onSettingsChanged;

            Title = "设置";
            var settings = ApplicationData.Current.LocalSettings.Values;

            // --- 卡片大小 ---
            _minCardWidthBox = CreateNumberBox("最小卡片宽度", 100, 800, 10, 220);
            _minCardWidthBox.Value = (double)(settings[MinCardWidthKey] ?? 220.0);
            _cardGapBox = CreateNumberBox("卡片间距", 0, 60, 2, 14);
            _cardGapBox.Value = (double)(settings[CardGapKey] ?? 14.0);

            // --- 文字大小 ---
            _subjectFontSizeBox = CreateNumberBox("科目字体大小", 10, 48, 1, 22);
            _subjectFontSizeBox.Value = (double)(settings[SubjectFontSizeKey] ?? 22.0);
            _contentFontSizeBox = CreateNumberBox("内容字体大小", 8, 36, 1, 17);
            _contentFontSizeBox.Value = (double)(settings[ContentFontSizeKey] ?? 17.0);

            // --- 服务器地址 ---
            _serverUrlBox = new TextBox
            {
                Header = "后端服务器地址",
                PlaceholderText = "https://kv-service.wuyuan.dev",
                Text = settings[ServerUrlKey] as string ?? "https://kv-service.wuyuan.dev"
            };

            // --- Token 状态 ---
            var hasToken = settings.ContainsKey(TokenKey) && !string.IsNullOrWhiteSpace(settings[TokenKey] as string);
            _currentTokenText = new TextBlock
            {
                Text = hasToken ? "已设置" : "未设置",
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            };

            var destroyTokenButton = new Button
            {
                Content = "销毁 Token",
                Margin = new Thickness(0, 4, 0, 0)
            };
            destroyTokenButton.Click += DestroyTokenButton_Click;

            var tokenSection = new StackPanel { Spacing = 4 };
            tokenSection.Children.Add(new TextBlock { Text = "当前 Token 状态" });
            tokenSection.Children.Add(_currentTokenText);
            tokenSection.Children.Add(destroyTokenButton);

            // --- 导出/导入 ---
            var exportButton = new Button { Content = "导出设置" };
            exportButton.Click += ExportButton_Click;

            var importButton = new Button { Content = "导入设置", Margin = new Thickness(8, 0, 0, 0) };

            importButton.Click += ImportButton_Click;

            var ioStack = new StackPanel { Orientation = Orientation.Horizontal };
            ioStack.Children.Add(exportButton);
            ioStack.Children.Add(importButton);

            // --- 保存/取消 ---
            var saveButton = new Button
            {
                Content = "保存",
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            saveButton.Click += SaveButton_Click;

            var cancelButton = new Button
            {
                Content = "取消",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 8, 0, 0)
            };
            cancelButton.Click += (_, _) => Close();

            // --- 组装 ---
            var form = new StackPanel
            {
                Spacing = 16,
                Width = 400
            };
            form.Children.Add(_minCardWidthBox);
            form.Children.Add(_cardGapBox);
            form.Children.Add(_subjectFontSizeBox);
            form.Children.Add(_contentFontSizeBox);
            form.Children.Add(_serverUrlBox);
            form.Children.Add(tokenSection);
            form.Children.Add(ioStack);
            form.Children.Add(saveButton);
            form.Children.Add(cancelButton);

            var scroll = new ScrollViewer
            {
                Content = form,
                Padding = new Thickness(32),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            Content = scroll;
        }

        private static NumberBox CreateNumberBox(string header, double minimum, double maximum, double step, double defaultValue)
        {
            return new NumberBox
            {
                Header = header,
                Minimum = minimum,
                Maximum = maximum,
                SmallChange = step,
                LargeChange = step * 5,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
                Value = defaultValue
            };
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = ApplicationData.Current.LocalSettings.Values;
            settings[MinCardWidthKey] = _minCardWidthBox.Value;
            settings[CardGapKey] = _cardGapBox.Value;
            settings[SubjectFontSizeKey] = _subjectFontSizeBox.Value;
            settings[ContentFontSizeKey] = _contentFontSizeBox.Value;
            settings[ServerUrlKey] = _serverUrlBox.Text;
            _onSettingsChanged?.Invoke();
            Close();
        }

        private void DestroyTokenButton_Click(object sender, RoutedEventArgs e)
        {
            ApplicationData.Current.LocalSettings.Values.Remove(TokenKey);
            _currentTokenText.Text = "未设置";
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
                var settings = ApplicationData.Current.LocalSettings.Values;
                var data = new Dictionary<string, object>();
                foreach (var kvp in settings)
                    data[kvp.Key] = kvp.Value ?? "";
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                await FileIO.WriteTextAsync(file, json);
            }
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
                    var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                    if (data == null) return;

                    var settings = ApplicationData.Current.LocalSettings.Values;
                    foreach (var kvp in data)
                    {
                        if (kvp.Value.ValueKind == JsonValueKind.String)
                            settings[kvp.Key] = kvp.Value.GetString();
                        else if (kvp.Value.ValueKind == JsonValueKind.Number)
                            settings[kvp.Key] = kvp.Value.GetDouble();
                    }

                    // 刷新 UI
                    _minCardWidthBox.Value = (double)(settings[MinCardWidthKey] ?? 220.0);
                    _cardGapBox.Value = (double)(settings[CardGapKey] ?? 14.0);
                    _subjectFontSizeBox.Value = (double)(settings[SubjectFontSizeKey] ?? 22.0);
                    _contentFontSizeBox.Value = (double)(settings[ContentFontSizeKey] ?? 17.0);
                    _serverUrlBox.Text = settings[ServerUrlKey] as string ?? "https://kv-service.wuyuan.dev";
                    _currentTokenText.Text = settings.ContainsKey(TokenKey) ? "已设置" : "未设置";
                }
                catch (Exception ex)
                {
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
