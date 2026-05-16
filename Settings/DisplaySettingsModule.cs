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
    public class DisplaySettingsModule : SettingsModuleBase
    {
        public override string CategoryKey => "display";
        public override string Title => "显示";
        public override string Description => "调整首页卡片的宽度、间距和文字大小。";
        public override string Glyph => "\uE7F8";

        private NumberBox _minCardWidthBox = null!;
        private NumberBox _cardGapBox = null!;
        private NumberBox _subjectFontSizeBox = null!;
        private NumberBox _contentFontSizeBox = null!;

        protected override FrameworkElement BuildContent()
        {
            _minCardWidthBox = SettingsUIHelper.CreateNumberBoxWithoutHeader(100, 800, 10, 220);
            _cardGapBox = SettingsUIHelper.CreateNumberBoxWithoutHeader(0, 60, 2, 12);
            _subjectFontSizeBox = SettingsUIHelper.CreateNumberBoxWithoutHeader(10, 48, 1, 18);
            _contentFontSizeBox = SettingsUIHelper.CreateNumberBoxWithoutHeader(8, 36, 1, 15);

            return SettingsUIHelper.CreateCategoryView(
                SettingsUIHelper.CreateSettingRow("最小卡片宽度", "影响首页作业卡片的最小宽度。", _minCardWidthBox),
                SettingsUIHelper.CreateSettingRow("卡片间距", "首页卡片之间的间距大小。", _cardGapBox),
                SettingsUIHelper.CreateSettingRow("科目字体大小", "控制作业科目标题的字号。", _subjectFontSizeBox),
                SettingsUIHelper.CreateSettingRow("内容字体大小", "控制作业详情内容的字号。", _contentFontSizeBox));
        }

        protected override void LoadSettings()
        {
            var settings = AppSettings.Values;
            _minCardWidthBox.Value = (double)(settings["Settings_MinCardWidth"] ?? 220.0);
            _cardGapBox.Value = (double)(settings["Settings_CardGap"] ?? 12.0);
            _subjectFontSizeBox.Value = (double)(settings["Settings_SubjectFontSize"] ?? 18.0);
            _contentFontSizeBox.Value = (double)(settings["Settings_ContentFontSize"] ?? 15.0);
        }

        protected override void HookAutoSaveHandlers()
        {
            _minCardWidthBox.ValueChanged += (_, _) => NotifySettingsChanged();
            _cardGapBox.ValueChanged += (_, _) => NotifySettingsChanged();
            _subjectFontSizeBox.ValueChanged += (_, _) => NotifySettingsChanged();
            _contentFontSizeBox.ValueChanged += (_, _) => NotifySettingsChanged();
        }

        public override void PersistSettings()
        {
            var settings = AppSettings.Values;
            settings["Settings_MinCardWidth"] = _minCardWidthBox.Value;
            settings["Settings_CardGap"] = _cardGapBox.Value;
            settings["Settings_SubjectFontSize"] = _subjectFontSizeBox.Value;
            settings["Settings_ContentFontSize"] = _contentFontSizeBox.Value;
        }
    }
}