using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;


using CSD.Views;
using CSD.Models;
using CSD.Services;
using CSD.Helpers;
using CSD.Settings;


namespace CSD.Settings
{
    public class UpdateSettingsModule : SettingsModuleBase
    {
        public override string CategoryKey => "update";
        public override string Title => "更新";
        public override string Description => "选择更新渠道以获取不同版本的更新。";
        public override string Glyph => "\uE895";

        protected override FrameworkElement BuildContent()
        {
            var root = new StackPanel { Spacing = 16 };

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

            var cardInner = new StackPanel { Spacing = 0 };

            var stableRow = CreateUpdateChannelRow("正式版 (Stable)", "获取稳定可靠的正式版本更新。", "stable", currentChannel == "stable", channelInfoBlock);
            cardInner.Children.Add(stableRow);

            cardInner.Children.Add(new Border
            {
                Height = 1,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"],
                Margin = new Thickness(16, 0, 16, 0)
            });

            var betaRow = CreateUpdateChannelRow("测试版 (Beta)", "获取最新功能的测试版本，可能存在不稳定因素。", "beta", currentChannel == "beta", channelInfoBlock);
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

        private Grid CreateUpdateChannelRow(string title, string description, string channelValue, bool isSelected, TextBlock channelInfoBlock)
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
                    channelInfoBlock.Text = $"当前更新渠道: {(newChannel == "beta" ? "测试版 (Beta)" : "正式版 (Stable)")}";
                }
            };

            var labelStack = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
            labelStack.Children.Add(new TextBlock { Text = title, FontSize = 15, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            labelStack.Children.Add(new TextBlock { Text = description, FontSize = 12, Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"] });

            var rowGrid = new Grid { Padding = new Thickness(16, 12, 16, 12), ColumnSpacing = 16 };
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Grid.SetColumn(radioButton, 0);
            Grid.SetColumn(labelStack, 1);
            rowGrid.Children.Add(radioButton);
            rowGrid.Children.Add(labelStack);

            return rowGrid;
        }
    }
}