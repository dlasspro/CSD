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
            var currentChannel = UpdateService.GetUpdateChannel();
            var channelInfoBlock = new TextBlock
            {
                Text = $"当前更新渠道: {(currentChannel == "beta" ? "测试版 (Beta)" : "正式版 (Stable)")}",
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            };

            return SettingsUIHelper.CreateCategoryView(
                SettingsUIHelper.CreateSettingsGroup("版本控制",
                    SettingsUIHelper.CreateSettingRow("更新渠道", "选择获取更新的版本类型。", new FontIcon { Glyph = "\uE895" }, channelInfoBlock),
                    CreateUpdateChannelRow("正式版 (Stable)", "获取稳定可靠的正式版本更新。", "stable", currentChannel == "stable", channelInfoBlock),
                    CreateUpdateChannelRow("测试版 (Beta)", "获取最新功能的测试版本，可能存在不稳定因素。", "beta", currentChannel == "beta", channelInfoBlock)));
        }

        private Border CreateUpdateChannelRow(string title, string description, string channelValue, bool isSelected, TextBlock channelInfoBlock)
        {
            var radioButton = new RadioButton
            {
                IsChecked = isSelected,
                GroupName = "UpdateChannel",
                Tag = channelValue,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            radioButton.Checked += (s, e) =>
            {
                if (s is RadioButton rb && rb.Tag is string newChannel)
                {
                    UpdateService.SetUpdateChannel(newChannel);
                    channelInfoBlock.Text = $"当前更新渠道: {(newChannel == "beta" ? "测试版 (Beta)" : "正式版 (Stable)")}";
                }
            };

            return SettingsUIHelper.CreateSettingRow(title, description, null, radioButton);
        }
    }
}