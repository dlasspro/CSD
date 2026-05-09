using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;

namespace CSD
{
    public sealed class AboutWindow : Window
    {
        public AboutWindow()
        {
            Title = "关于 CSD";
            SystemBackdrop = new MicaBackdrop();

            // 使用 Grid 布局，顶部固定，内容区可滚动
            var root = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },  // Hero 区域
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }  // 内容区
                }
            };

            // ========== 顶部 Hero 区域 ==========
            var heroBorder = new Border
            {
                Padding = new Thickness(32, 40, 32, 32),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            Grid.SetRow(heroBorder, 0);

            var heroStack = new StackPanel { Spacing = 12, HorizontalAlignment = HorizontalAlignment.Center };

            // 应用图标
            var iconImage = new Image
            {
                Width = 72,
                Height = 72,
                Source = new BitmapImage(new Uri("ms-appx:///Assets/Classworks.ico")),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            heroStack.Children.Add(iconImage);

            // 应用名称
            heroStack.Children.Add(new TextBlock
            {
                Text = "CSD",
                FontSize = 32,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            // 版本号
            heroStack.Children.Add(new TextBlock
            {
                Text = GetAppVersion(),
                FontSize = 13,
                Foreground = (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"],
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, -4, 0, 0)
            });

            // 副标题
            heroStack.Children.Add(new TextBlock
            {
                Text = "Classworks Desktop",
                FontSize = 15,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                HorizontalAlignment = HorizontalAlignment.Center
            });

            heroBorder.Child = heroStack;
            root.Children.Add(heroBorder);

            // ========== 内容区域 ==========
            var contentScroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(24, 0, 24, 24)
            };
            Grid.SetRow(contentScroll, 1);

            var contentStack = new StackPanel { Spacing = 16 };

            // --- 应用描述 ---
            contentStack.Children.Add(CreateSectionCard(
                "应用简介",
                "Classworks Desktop 是一款桌面应用程序，为 Classworks 提供原生桌面体验。支持作业管理、Markdown/MFM 富文本编辑与渲染等功能。"
            ));

            // --- 致谢 ---
            var creditsPanel = new StackPanel { Spacing = 10 };
            creditsPanel.Children.Add(CreateCreditItem("翟十光", "客户端开发者", "ms-appx:///Assets/zhaishis.png"));
            creditsPanel.Children.Add(CreateCreditItem("Saskia", "提供了开发环境和 Token", "ms-appx:///Assets/saskia.jpeg"));
            creditsPanel.Children.Add(CreateCreditItem("孙悟元", "Classworks 开发者", "ms-appx:///Assets/wuyuan.jpeg"));
            contentStack.Children.Add(CreateSectionCard("致谢", creditsPanel));

            // --- 链接 ---
            var linksPanel = new StackPanel { Spacing = 8 };
            linksPanel.Children.Add(CreateLinkItem("GitHub 仓库", "https://github.com/dlasspro/CSD", "\uE8F4"));
            linksPanel.Children.Add(CreateLinkItem("官方网站", "https://cs.dy.ci", "\uE774"));
            contentStack.Children.Add(CreateSectionCard("链接", linksPanel));

            // --- 技术信息 ---
            var techPanel = new StackPanel { Spacing = 6 };
            techPanel.Children.Add(CreateInfoRow("框架", "WinUI 3 / Windows App SDK 2.0"));
            techPanel.Children.Add(CreateInfoRow("运行时", ".NET 8"));
            techPanel.Children.Add(CreateInfoRow("后端", "KV 存储服务"));
            techPanel.Children.Add(CreateInfoRow("渲染", "Markdown + MFM"));
            contentStack.Children.Add(CreateSectionCard("技术栈", techPanel));

            // --- 版权信息 ---
            contentStack.Children.Add(new TextBlock
            {
                Text = "\u00A9 2026 dy.ci. All rights reserved.",
                FontSize = 12,
                Foreground = (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"],
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0)
            });

            contentScroll.Content = contentStack;
            root.Children.Add(contentScroll);

            Content = root;
            root.Loaded += (_, _) =>
            {
                AnimationHelper.AnimateEntrance(root, fromY: 18f, durationMs: 360);
                AnimationHelper.ApplyStandardInteractions(contentScroll);
            };

            AppWindow.Resize(new Windows.Graphics.SizeInt32(440, 620));
        }

        /// <summary>
        /// 获取应用版本号
        /// </summary>
        private static string GetAppVersion()
        {
            try
            {
                var version = Windows.ApplicationModel.Package.Current.Id.Version;
                return $"v{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
            }
            catch
            {
                return "v1.0.0.0";
            }
        }

        /// <summary>
        /// 创建带圆角背景的分区卡片
        /// </summary>
        private static Border CreateSectionCard(string title, string description)
        {
            var panel = new StackPanel { Spacing = 6 };
            panel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            panel.Children.Add(new TextBlock
            {
                Text = description,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });

            return new Border
            {
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 12, 16, 12),
                Child = panel
            };
        }

        /// <summary>
        /// 创建带圆角背景的分区卡片（自定义内容）
        /// </summary>
        private static Border CreateSectionCard(string title, UIElement content)
        {
            var panel = new StackPanel { Spacing = 10 };
            panel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            panel.Children.Add(content);

            return new Border
            {
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 12, 16, 12),
                Child = panel
            };
        }

        /// <summary>
        /// 创建致谢条目（带头像图片）
        /// </summary>
        private static StackPanel CreateCreditItem(string name, string role, string avatarUri)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 12
            };

            // 头像圆圈
            panel.Children.Add(new Border
            {
                Width = 32,
                Height = 32,
                CornerRadius = new CornerRadius(16),
                Child = new Image
                {
                    Source = new BitmapImage(new Uri(avatarUri)),
                    Stretch = Stretch.UniformToFill
                }
            });

            // 名称和角色
            var infoPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            infoPanel.Children.Add(new TextBlock
            {
                Text = name,
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            infoPanel.Children.Add(new TextBlock
            {
                Text = role,
                FontSize = 12,
                Foreground = (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"]
            });

            panel.Children.Add(infoPanel);
            return panel;
        }

        /// <summary>
        /// 创建可点击的链接条目
        /// </summary>
        private static Button CreateLinkItem(string title, string url, string glyph)
        {
            var btn = new Button
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(8, 6, 8, 6),
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(4)
            };

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10
            };
            panel.Children.Add(new FontIcon
            {
                Glyph = glyph,
                FontSize = 16,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                VerticalAlignment = VerticalAlignment.Center
            });
            panel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            });

            btn.Content = panel;
            btn.Click += (_, _) =>
            {
                try { _ = Windows.System.Launcher.LaunchUriAsync(new Uri(url)); } catch { }
            };

            return btn;
        }

        /// <summary>
        /// 创建技术信息行
        /// </summary>
        private static StackPanel CreateInfoRow(string label, string value)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8
            };
            panel.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 13,
                Foreground = (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"],
                MinWidth = 60
            });
            panel.Children.Add(new TextBlock
            {
                Text = value,
                FontSize = 13,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            return panel;
        }
    }
}
