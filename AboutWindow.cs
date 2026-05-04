using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;

namespace CSD
{
    public sealed class AboutWindow : Window
    {
        public AboutWindow()
        {
            Title = "关于 CSD";
            SystemBackdrop = new MicaBackdrop();

            var stack = new StackPanel
            {
                Spacing = 16,
                Padding = new Thickness(32),
                MaxWidth = 400
            };

            // 标题
            stack.Children.Add(new TextBlock
            {
                Text = "CSD",
                FontSize = 36,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            // 副标题
            stack.Children.Add(new TextBlock
            {
                Text = "Classworks Student Dashboard",
                FontSize = 16,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, -8, 0, 0)
            });

            // 分隔线
            stack.Children.Add(new Rectangle
            {
                Height = 1,
                Fill = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"],
                Margin = new Thickness(0, 4, 0, 4)
            });

            // 描述
            stack.Children.Add(new TextBlock
            {
                Text = "Classworks 学生仪表板是一款桌面应用程序，用于查看和管理每日作业、记录考勤信息，并提供轮播展示和随机抽取学生等辅助功能。",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });

            // 功能列表
            var features = new StackPanel { Spacing = 8, Margin = new Thickness(0, 4, 0, 0) };
            features.Children.Add(new TextBlock
            {
                Text = "主要功能",
                FontSize = 16,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            features.Children.Add(CreateFeatureItem("作业管理", "查看、添加、修改每日作业"));
            features.Children.Add(CreateFeatureItem("考勤记录", "记录学生出勤情况"));
            features.Children.Add(CreateFeatureItem("轮播展示", "全屏轮播展示作业内容"));
            features.Children.Add(CreateFeatureItem("随机抽取", "随机抽取学生姓名"));
            features.Children.Add(CreateFeatureItem("空作业提醒", "在导航栏显示未布置的作业科目"));
            stack.Children.Add(features);

            // 分隔线
            stack.Children.Add(new Rectangle
            {
                Height = 1,
                Fill = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"],
                Margin = new Thickness(0, 4, 0, 4)
            });

            // 技术信息
            var techStack = new StackPanel { Spacing = 6 };
            techStack.Children.Add(new TextBlock
            {
                Text = "技术信息",
                FontSize = 16,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            techStack.Children.Add(CreateInfoRow("框架", "WinUI 3 / Windows App SDK"));
            techStack.Children.Add(CreateInfoRow("运行时", ".NET 8"));
            techStack.Children.Add(CreateInfoRow("后端", "KV 存储服务"));
            stack.Children.Add(techStack);

            // 关闭按钮
            var closeButton = new Button
            {
                Content = "关闭",
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0),
                MinWidth = 120
            };
            closeButton.Click += (_, _) => Close();
            stack.Children.Add(closeButton);

            var scroll = new ScrollViewer
            {
                Content = stack,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            Content = scroll;

            AppWindow.Resize(new Windows.Graphics.SizeInt32(420, 560));
        }

        private static StackPanel CreateFeatureItem(string title, string description)
        {
            var panel = new StackPanel { Spacing = 2 };
            panel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            panel.Children.Add(new TextBlock
            {
                Text = description,
                FontSize = 13,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });
            return panel;
        }

        private static StackPanel CreateInfoRow(string label, string value)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            panel.Children.Add(new TextBlock
            {
                Text = label + ":",
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                MinWidth = 80
            });
            panel.Children.Add(new TextBlock
            {
                Text = value,
                FontSize = 14,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });
            return panel;
        }
    }
}
