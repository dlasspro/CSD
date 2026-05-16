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
    public class RandomPickerSettingsModule : SettingsModuleBase
    {
        public override string CategoryKey => "randomPicker";
        public override string Title => "随机点名";
        public override string Description => "配置随机点名功能的模式、范围和默认参数。";
        public override string Glyph => "\uE716";

        private ToggleSwitch _randomPickerEnabledToggle = null!;
        private NumberBox _randomPickerMinNumberBox = null!;
        private NumberBox _randomPickerMaxNumberBox = null!;
        private NumberBox _randomPickerDefaultCountBox = null!;
        private ToggleSwitch _randomPickerAnimationToggle = null!;

        protected override FrameworkElement BuildContent()
        {
            _randomPickerEnabledToggle = new ToggleSwitch { OnContent = null, OffContent = null, MinWidth = 0, Margin = new Thickness(0) };
            _randomPickerMinNumberBox = SettingsUIHelper.CreateNumberBoxWithoutHeader(1, 999, 1, 1.0);
            _randomPickerMaxNumberBox = SettingsUIHelper.CreateNumberBoxWithoutHeader(1, 9999, 1, 60.0);
            _randomPickerDefaultCountBox = SettingsUIHelper.CreateNumberBoxWithoutHeader(1, 100, 1, 1.0);
            _randomPickerAnimationToggle = new ToggleSwitch { OnContent = null, OffContent = null, MinWidth = 0, Margin = new Thickness(0) };

            return SettingsUIHelper.CreateCategoryView(
                SettingsUIHelper.CreateSettingRow("是否启用随机点名功能", "控制随机点名功能的开关。", _randomPickerEnabledToggle),
                SettingsUIHelper.CreateSettingRow("学号模式最小值", "学号模式下可抽取的最小学号。", _randomPickerMinNumberBox),
                SettingsUIHelper.CreateSettingRow("学号模式最大值", "学号模式下可抽取的最大学号。", _randomPickerMaxNumberBox),
                SettingsUIHelper.CreateSettingRow("默认抽取人数", "打开随机点名窗口时的默认抽取人数。", _randomPickerDefaultCountBox),
                SettingsUIHelper.CreateSettingRow("是否启用随机点名动画效果", "控制点名时是否播放滚动动画。", _randomPickerAnimationToggle));
        }

        private static double ConvertToDouble(object? value, double defaultValue)
        {
            if (value is double d) return d;
            if (value is int i) return i;
            if (value is float f) return f;
            return defaultValue;
        }

        protected override void LoadSettings()
        {
            var settings = AppSettings.Values;
            _randomPickerEnabledToggle.IsOn = settings.ContainsKey("randomPicker.enabled") ? (bool)(settings["randomPicker.enabled"] ?? true) : true;
            _randomPickerMinNumberBox.Value = ConvertToDouble(settings["randomPicker.minNumber"], 1.0);
            _randomPickerMaxNumberBox.Value = ConvertToDouble(settings["randomPicker.maxNumber"], 60.0);
            _randomPickerDefaultCountBox.Value = ConvertToDouble(settings["randomPicker.defaultCount"], 1.0);
            _randomPickerAnimationToggle.IsOn = settings.ContainsKey("randomPicker.animation") ? (bool)(settings["randomPicker.animation"] ?? true) : true;
        }

        protected override void HookAutoSaveHandlers()
        {
            _randomPickerEnabledToggle.Toggled += (_, _) => NotifySettingsChanged();
            _randomPickerMinNumberBox.ValueChanged += (_, _) => NotifySettingsChanged();
            _randomPickerMaxNumberBox.ValueChanged += (_, _) => NotifySettingsChanged();
            _randomPickerDefaultCountBox.ValueChanged += (_, _) => NotifySettingsChanged();
            _randomPickerAnimationToggle.Toggled += (_, _) => NotifySettingsChanged();
        }

        public override void PersistSettings()
        {
            var settings = AppSettings.Values;
            settings["randomPicker.enabled"] = _randomPickerEnabledToggle.IsOn;
            settings["randomPicker.minNumber"] = (int)_randomPickerMinNumberBox.Value;
            settings["randomPicker.maxNumber"] = (int)_randomPickerMaxNumberBox.Value;
            settings["randomPicker.defaultCount"] = (int)_randomPickerDefaultCountBox.Value;
            settings["randomPicker.animation"] = _randomPickerAnimationToggle.IsOn;
        }
    }
}