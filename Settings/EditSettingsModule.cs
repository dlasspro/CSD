using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Threading.Tasks;


using CSD.Views;
using CSD.Models;
using CSD.Services;
using CSD.Helpers;
using CSD.Settings;


namespace CSD.Settings
{
    public class EditSettingsModule : SettingsModuleBase
    {
        public override string CategoryKey => "edit";
        public override string Title => "编辑设置";
        public override string Description => "自动保存、非当天写入限制、确认与提示文案；部分项会同步到云端。";
        public override string Glyph => "\uE70F";

        private ToggleSwitch _editAutoSaveToggle = null!;
        private ToggleSwitch _editBlockNonTodayAutoSaveToggle = null!;
        private ToggleSwitch _editConfirmNonTodaySaveToggle = null!;
        private ToggleSwitch _editRefreshBeforeEditToggle = null!;
        private TextBox _editAutoSavePromptTextBox = null!;
        private TextBox _editManualSavePromptTextBox = null!;
        
        private int _editCloudPushGeneration;
        private bool _editPrefsPulledThisSession;

        protected override FrameworkElement BuildContent()
        {
            _editAutoSaveToggle = new ToggleSwitch { OnContent = null, OffContent = null, MinWidth = 0, Margin = new Thickness(0) };
            _editBlockNonTodayAutoSaveToggle = new ToggleSwitch { OnContent = null, OffContent = null, MinWidth = 0, Margin = new Thickness(0) };
            _editConfirmNonTodaySaveToggle = new ToggleSwitch { OnContent = null, OffContent = null, MinWidth = 0, Margin = new Thickness(0) };
            _editRefreshBeforeEditToggle = new ToggleSwitch { OnContent = null, OffContent = null, MinWidth = 0, Margin = new Thickness(0) };
            _editAutoSavePromptTextBox = new TextBox { AcceptsReturn = false, TextWrapping = TextWrapping.Wrap, MinHeight = 40 };
            _editManualSavePromptTextBox = new TextBox { AcceptsReturn = false, TextWrapping = TextWrapping.Wrap, MinHeight = 40 };

            var cardInner = new StackPanel { Spacing = 0 };
            cardInner.Children.Add(CreateEditToggleSettingCardRow(EditSettingsGlyph("\uE74E"), "是否启用自动保存", "edit.autoSave", _editAutoSaveToggle, showDividerBelow: true));
            cardInner.Children.Add(CreateEditToggleSettingCardRow(EditSettingsGlyph("\uE787"), "禁止写入非当天作业数据", "edit.blockNonTodayWrite", _editBlockNonTodayAutoSaveToggle, showDividerBelow: true));
            cardInner.Children.Add(CreateEditToggleSettingCardRow(EditSettingsGlyph("\uE73E"), "保存非当天数据需确认", "edit.confirmNonTodaySave", _editConfirmNonTodaySaveToggle, showDividerBelow: true));
            cardInner.Children.Add(CreateEditToggleSettingCardRow(EditSettingsGlyph("\uE72C"), "编辑前是否自动刷新", "edit.refreshBeforeEdit", _editRefreshBeforeEditToggle, showDividerBelow: true));
            cardInner.Children.Add(CreateEditCompoundTextSettingRow(EditSettingsGlyph("\uE8A5"), "自动保存模式提示文本", "edit.autoSavePromptText", _editAutoSavePromptTextBox, showDividerBelow: true));
            cardInner.Children.Add(CreateEditCompoundTextSettingRow(EditSettingsGlyph("\uE8A5"), "手动保存模式提示文本", "edit.manualSavePromptText", _editManualSavePromptTextBox, showDividerBelow: false));

            return new Border
            {
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(0, 2, 0, 2),
                MaxWidth = 920,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Child = cardInner
            };
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
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(8),
                Content = new FontIcon { Glyph = "\uE712", FontSize = 14, VerticalAlignment = VerticalAlignment.Center }
            };
            moreButton.Click += (_, _) => { };
            return moreButton;
        }

        private Border CreateEditToggleSettingCardRow(FrameworkElement leadingIcon, string primaryText, string keyText, ToggleSwitch toggle, bool showDividerBelow)
        {
            var labelStack = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
            labelStack.Children.Add(new TextBlock { Text = primaryText, FontSize = 15, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            labelStack.Children.Add(new TextBlock { Text = keyText, FontSize = 12, Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"] });

            var iconHost = new Grid { Width = 32, Height = 32, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            leadingIcon.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            leadingIcon.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            iconHost.Children.Add(leadingIcon);

            toggle.VerticalAlignment = VerticalAlignment.Center;
            toggle.HorizontalAlignment = HorizontalAlignment.Right;

            var moreButton = CreateEditRowMoreButton();

            var rowGrid = new Grid { Padding = new Thickness(16, 12, 16, 12), ColumnSpacing = 16 };
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

        private Border CreateEditCompoundTextSettingRow(FrameworkElement leadingIcon, string primaryText, string keyText, TextBox textBox, bool showDividerBelow)
        {
            var labelStack = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
            labelStack.Children.Add(new TextBlock { Text = primaryText, FontSize = 15, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            labelStack.Children.Add(new TextBlock { Text = keyText, FontSize = 12, Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"] });

            var iconHost = new Grid { Width = 32, Height = 32, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            leadingIcon.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            leadingIcon.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            iconHost.Children.Add(leadingIcon);

            var moreButton = CreateEditRowMoreButton();

            var headerGrid = new Grid { Padding = new Thickness(16, 12, 16, 8), ColumnSpacing = 16 };
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

        protected override void LoadSettings()
        {
            var s = AppSettings.Values;
            _editAutoSaveToggle.IsOn = s.ContainsKey(EditPreferencesKeys.AutoSave) && (bool)(s[EditPreferencesKeys.AutoSave] ?? false);
            _editBlockNonTodayAutoSaveToggle.IsOn = s.ContainsKey(EditPreferencesKeys.BlockNonTodayAutoSave) && (bool)(s[EditPreferencesKeys.BlockNonTodayAutoSave] ?? false);
            _editConfirmNonTodaySaveToggle.IsOn = s.ContainsKey(EditPreferencesKeys.ConfirmNonTodaySave) && (bool)(s[EditPreferencesKeys.ConfirmNonTodaySave] ?? false);
            _editRefreshBeforeEditToggle.IsOn = s.ContainsKey(EditPreferencesKeys.RefreshBeforeEdit) && (bool)(s[EditPreferencesKeys.RefreshBeforeEdit] ?? false);
            _editAutoSavePromptTextBox.Text = s[EditPreferencesKeys.AutoSavePromptText] as string ?? "喵？喵呜！";
            _editManualSavePromptTextBox.Text = s[EditPreferencesKeys.ManualSavePromptText] as string ?? "写完后点击上传谢谢喵";
        }

        protected override void HookAutoSaveHandlers()
        {
            _editAutoSaveToggle.Toggled += (_, _) => { NotifySettingsChanged(); ScheduleEditPrefsCloudPush(); };
            _editBlockNonTodayAutoSaveToggle.Toggled += (_, _) => { NotifySettingsChanged(); ScheduleEditPrefsCloudPush(); };
            _editConfirmNonTodaySaveToggle.Toggled += (_, _) => { NotifySettingsChanged(); ScheduleEditPrefsCloudPush(); };
            _editRefreshBeforeEditToggle.Toggled += (_, _) => { NotifySettingsChanged(); ScheduleEditPrefsCloudPush(); };
            _editAutoSavePromptTextBox.TextChanged += (_, _) => { NotifySettingsChanged(); ScheduleEditPrefsCloudPush(); };
            _editManualSavePromptTextBox.TextChanged += (_, _) => { NotifySettingsChanged(); ScheduleEditPrefsCloudPush(); };
        }

        public override void PersistSettings()
        {
            var s = AppSettings.Values;
            s[EditPreferencesKeys.AutoSave] = _editAutoSaveToggle.IsOn;
            s[EditPreferencesKeys.BlockNonTodayAutoSave] = _editBlockNonTodayAutoSaveToggle.IsOn;
            s[EditPreferencesKeys.ConfirmNonTodaySave] = _editConfirmNonTodaySaveToggle.IsOn;
            s[EditPreferencesKeys.RefreshBeforeEdit] = _editRefreshBeforeEditToggle.IsOn;
            s[EditPreferencesKeys.AutoSavePromptText] = _editAutoSavePromptTextBox.Text ?? "";
            s[EditPreferencesKeys.ManualSavePromptText] = _editManualSavePromptTextBox.Text ?? "";
        }

        public override void OnNavigatedTo()
        {
            RequestEditPrefsPullFromCloudIfNeeded();
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
            var settings = AppSettings.Values;
            var token = settings["Token"] as string;
            if (string.IsNullOrWhiteSpace(token))
                return;

            var baseUrl = (settings["Settings_ServerUrl"] as string ?? "https://kv-service.wuyuan.dev").TrimEnd('/');
            var ok = await EditPreferencesSync.TryPullMergeIntoAppSettingsAsync(Context.HttpClient, baseUrl, token).ConfigureAwait(false);
            if (!ok)
                return;

            uiDq?.TryEnqueue(() =>
            {
                IsAutoSaveSuspended = true;
                try
                {
                    LoadSettings();
                }
                finally
                {
                    IsAutoSaveSuspended = false;
                }

                NotifySettingsChanged();
            });
        }

        private async void ScheduleEditPrefsCloudPush()
        {
            var dq = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            var generation = ++_editCloudPushGeneration;
            await Task.Delay(450).ConfigureAwait(false);
            if (generation != _editCloudPushGeneration)
                return;
            _ = await TryPushEditPrefsToKvCoreAsync(showErrors: false).ConfigureAwait(false);
            dq?.TryEnqueue(() => NotifySettingsChanged());
        }

        private async Task<bool> TryPushEditPrefsToKvCoreAsync(bool showErrors)
        {
            var settings = AppSettings.Values;
            var token = settings["Token"] as string;
            if (string.IsNullOrWhiteSpace(token))
            {
                if (showErrors)
                    await ShowSimpleDialogAsync("请先填写 KV 令牌后再同步编辑偏好。");
                return false;
            }

            var baseUrl = (settings["Settings_ServerUrl"] as string ?? "https://kv-service.wuyuan.dev").TrimEnd('/');
            var ok = await EditPreferencesSync.PushAsync(Context.HttpClient, baseUrl, token).ConfigureAwait(false);
            if (!ok && showErrors)
                await ShowSimpleDialogAsync("编辑偏好未能写入云端，请检查网络与令牌。");
            return ok;
        }

        private async Task ShowSimpleDialogAsync(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "编辑偏好",
                Content = message,
                CloseButtonText = "确定",
                XamlRoot = Context.Window.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}