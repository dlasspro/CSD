using Microsoft.UI.Xaml;
using System;


using CSD.Views;
using CSD.Models;
using CSD.Services;
using CSD.Helpers;
using CSD.Settings;


namespace CSD.Settings
{
    /// <summary>
    /// 设置模块接口，用于定义不同类型的设置项。
    /// </summary>
    public interface ISettingsModule
    {
        /// <summary>
        /// 获取设置分类的唯一标识键
        /// </summary>
        string CategoryKey { get; }

        /// <summary>
        /// 获取设置分类的显示名称
        /// </summary>
        string Title { get; }

        /// <summary>
        /// 获取设置分类的描述信息
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 导航栏图标 (可以是字体图标或图片URI)
        /// </summary>
        string Glyph { get; }

        /// <summary>
        /// 导航栏图片图标 URI
        /// </summary>
        string ImageIconUri { get; }

        /// <summary>
        /// 初始化模块，传入上下文环境
        /// </summary>
        void Initialize(SettingsContext context);

        /// <summary>
        /// 创建并返回该设置模块的 UI 内容
        /// </summary>
        FrameworkElement CreateView();

        /// <summary>
        /// 当设置项被显示时调用
        /// </summary>
        void OnNavigatedTo();

        /// <summary>
        /// 保存或持久化该模块的设置
        /// </summary>
        void PersistSettings();

        /// <summary>
        /// 当设置项发生更改时触发
        /// </summary>
        event Action SettingsChanged;
    }
}