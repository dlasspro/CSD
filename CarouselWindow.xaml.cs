using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;

namespace CSD
{
    public sealed partial class CarouselWindow : Window
    {
        private readonly List<HomeworkItem> _carouselItems;
        private int _carouselIndex = 0;
        private readonly DispatcherTimer _carouselTimer = new();
        private bool _isFirstShow = true;

        /// <summary>
        /// 退出轮播时的回调，用于重新打开主窗口。
        /// </summary>
        public Action? OnExitCarousel { get; set; }

        public CarouselWindow(List<HomeworkItem> items)
        {
            InitializeComponent();

            _carouselItems = items ?? new List<HomeworkItem>();

            // 窗口初始化
            try { AppWindow.SetIcon(AppSettings.GetAssetUri("Assets/StoreLogo.png").LocalPath); } catch { }

            // 自定义标题栏
            ConfigureIntegratedTitleBar();

            // 最大化窗口
            if (AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }

            // 点击窗口 X 关闭时，触发退出轮播逻辑
            AppWindow.Closing += (sender, args) =>
            {
                _carouselTimer.Stop();
                OnExitCarousel?.Invoke();
            };

            // 点击内容切换到下一项
            StartCarouselTimer();
            ShowCarouselItem();
        }

        private void ConfigureIntegratedTitleBar()
        {
            if (!AppWindowTitleBar.IsCustomizationSupported())
                return;

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            UpdateTitleBarLayout(AppWindow.TitleBar);
        }

        private void UpdateTitleBarLayout(AppWindowTitleBar titleBar)
        {
            LeftInsetColumn.Width = new GridLength(titleBar.LeftInset);
            RightInsetColumn.Width = new GridLength(titleBar.RightInset);
        }

        private void ExitCarouselButton_Click(object sender, RoutedEventArgs e)
        {
            _carouselTimer.Stop();
            OnExitCarousel?.Invoke();
            this.Close();
        }

        private void CarouselOverlay_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_carouselItems.Count > 0)
            {
                _carouselIndex = (_carouselIndex + 1) % _carouselItems.Count;
                ShowCarouselItem();
            }
        }

        private void StartCarouselTimer()
        {
            _carouselTimer.Stop();
            var settings = AppSettings.Values;
            double interval = (double)(settings["Settings_CarouselInterval"] ?? 5.0);
            _carouselTimer.Interval = TimeSpan.FromSeconds(interval);
            _carouselTimer.Tick -= CarouselTimer_Tick;
            _carouselTimer.Tick += CarouselTimer_Tick;
            _carouselTimer.Start();
        }

        private void CarouselTimer_Tick(object? sender, object e)
        {
            if (_carouselItems.Count > 0)
            {
                _carouselIndex = (_carouselIndex + 1) % _carouselItems.Count;
                ShowCarouselItem();
            }
        }

        private void ShowCarouselItem()
        {
            if (_carouselItems.Count == 0) return;

            var item = _carouselItems[_carouselIndex];
            var settings = AppSettings.Values;
            double carouselFontSize = (double)(settings["Settings_CarouselFontSize"] ?? 48.0);

            CarouselSubjectText.Text = item.Subject;
            CarouselSubjectText.FontSize = Math.Min(carouselFontSize * 1.3, 96);

            // 使用 Markdown 渲染内容
            CarouselContentHost.Content = MarkdownTextRenderer.CreateRichTextBlock(
                item.Content,
                carouselFontSize,
                (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"]);

            CarouselProgressText.Text = $"{_carouselIndex + 1} / {_carouselItems.Count}";

            if (_isFirstShow)
            {
                _isFirstShow = false;
                AnimationHelper.AnimateEntrance(CarouselProgressText, fromY: -8f, durationMs: 200);
                AnimationHelper.AnimateEntrance(CarouselContentPanel, fromY: 18f, durationMs: 280, delayMs: 40);
                AnimationHelper.AnimateEntrance(ExitCarouselButton, fromY: 12f, durationMs: 220, delayMs: 100);
            }
            else
            {
                // 切换科目时统一对整个内容面板做动画，避免标题和作业文本分别位移导致视觉重合
                AnimationHelper.AnimateEntrance(CarouselContentPanel, fromY: 14f, durationMs: 240);
            }
        }
    }
}
