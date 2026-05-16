using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;


using CSD.Views;
using CSD.Models;
using CSD.Services;
using CSD.Helpers;
using CSD.Settings;


namespace CSD.Settings
{
    public class RefreshSettingsModule : SettingsModuleBase
    {
        public override string CategoryKey => "refresh";
        public override string Title => "刷新设置";
        public override string Description => "定时从数据源拉取最新作业，并驱动主界面等全局组件一并更新。";
        public override string Glyph => "\uE72C";

        private ToggleSwitch _autoRefreshToggle = null!;
        private NumberBox _autoRefreshIntervalBox = null!;

        protected override FrameworkElement BuildContent()
        {
            var autoIcon = new ImageIcon
            {
                Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(AppSettings.GetAssetUri("icons/ic_public_refresh.ico")),
                Width = 20,
                Height = 20,
                VerticalAlignment = VerticalAlignment.Center,
            };

            var intervalIcon = new ImageIcon
            {
                Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(AppSettings.GetAssetUri("icons/ic_statusbar_alarm.ico")),
                Width = 20,
                Height = 20,
                VerticalAlignment = VerticalAlignment.Center,
            };

            _autoRefreshToggle = new ToggleSwitch { OnContent = null, OffContent = null, MinWidth = 0, Margin = new Thickness(0) };
            _autoRefreshIntervalBox = SettingsUIHelper.CreateNumberBoxWithoutHeader(10, 600, 10, 60);

            var cardInner = new StackPanel { Spacing = 0 };
            cardInner.Children.Add(CreateRefreshSettingCardRow(autoIcon, "自动刷新", "refresh.auto", _autoRefreshToggle, showDividerBelow: true));
            cardInner.Children.Add(CreateRefreshSettingCardRow(intervalIcon, "刷新间隔", "refresh.interval", _autoRefreshIntervalBox, showDividerBelow: false));

            return new Border
            {
                Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(0, 2, 0, 2),
                MaxWidth = 920,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Child = cardInner
            };
        }

        private Border CreateRefreshSettingCardRow(FrameworkElement leadingIcon, string primaryText, string keyText, FrameworkElement trailingControl, bool showDividerBelow)
        {
            var labelStack = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
            labelStack.Children.Add(new TextBlock { Text = primaryText, FontSize = 15, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            labelStack.Children.Add(new TextBlock { Text = keyText, FontSize = 12, Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"] });

            var iconHost = new Grid { Width = 32, Height = 32, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            leadingIcon.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            leadingIcon.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            iconHost.Children.Add(leadingIcon);

            trailingControl.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            trailingControl.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right);

            var rowGrid = new Grid { Padding = new Thickness(16, 12, 16, 12), ColumnSpacing = 16 };
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
                    Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"],
                    Margin = new Thickness(16, 0, 16, 0)
                });
            }

            return new Border { Child = outer };
        }

        protected override void LoadSettings()
        {
            var settings = AppSettings.Values;
            _autoRefreshToggle.IsOn = settings.ContainsKey("Settings_AutoRefreshEnabled") && (bool)(settings["Settings_AutoRefreshEnabled"] ?? false);
            _autoRefreshIntervalBox.Value = (double)(settings["Settings_AutoRefreshInterval"] ?? 60.0);
        }

        protected override void HookAutoSaveHandlers()
        {
            _autoRefreshToggle.Toggled += (_, _) => NotifySettingsChanged();
            _autoRefreshIntervalBox.ValueChanged += (_, _) => NotifySettingsChanged();
        }

        public override void PersistSettings()
        {
            var settings = AppSettings.Values;
            settings["Settings_AutoRefreshEnabled"] = _autoRefreshToggle.IsOn;
            settings["Settings_AutoRefreshInterval"] = _autoRefreshIntervalBox.Value;
        }
    }
}