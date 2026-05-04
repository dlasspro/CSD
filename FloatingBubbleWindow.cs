using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using Windows.Graphics;
using Windows.Storage;
using Microsoft.UI;
using Windows.UI;

namespace CSD
{
    public sealed class FloatingBubbleWindow : Window
    {
        private const string BubbleXKey = "Bubble_X";
        private const string BubbleYKey = "Bubble_Y";
        private const string BubbleEnabledKey = "Settings_BubbleEnabled";
        private const string BubbleDisplayModeKey = "Settings_BubbleDisplayMode";

        private readonly MainWindow _mainWindow;
        private bool _isDragging;
        private PointInt32 _dragStartPosition;
        private PointInt32 _windowStartPosition;
        private readonly Ellipse _bubbleEllipse;
        private readonly TextBlock _bubbleText;

        public FloatingBubbleWindow(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            Title = "CSD 悬浮球";

            // 配置窗口
            var presenter = (OverlappedPresenter)AppWindow.Presenter;
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsAlwaysOnTop = true;

            AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            // 设置大小
            AppWindow.Resize(new SizeInt32(64, 64));

            // 恢复上次位置
            RestoreBubblePosition();

            // 创建 UI
            var grid = new Grid();

            _bubbleEllipse = new Ellipse
            {
                Width = 56,
                Height = 56,
                Fill = new SolidColorBrush(Color.FromArgb(255, 0, 120, 212)),
                Stroke = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                StrokeThickness = 2,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            _bubbleText = new TextBlock
            {
                Text = "CSD",
                FontSize = 12,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };

            grid.Children.Add(_bubbleEllipse);
            grid.Children.Add(_bubbleText);
            Content = grid;

            // 拖拽事件
            grid.PointerPressed += Bubble_PointerPressed;
            grid.PointerMoved += Bubble_PointerMoved;
            grid.PointerReleased += Bubble_PointerReleased;
            grid.PointerCanceled += (_, _) => _isDragging = false;

            // 点击恢复主窗口
            grid.Tapped += Bubble_Tapped;

            // 保存位置
            Closed += (_, _) => SaveBubblePosition();
        }

        private void Bubble_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _isDragging = true;
            var point = e.GetCurrentPoint(null);
            _dragStartPosition = new PointInt32((int)point.Position.X, (int)point.Position.Y);
            _windowStartPosition = AppWindow.Position;
        }

        private void Bubble_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDragging) return;

            var point = e.GetCurrentPoint(null);
            int deltaX = (int)point.Position.X - _dragStartPosition.X;
            int deltaY = (int)point.Position.Y - _dragStartPosition.Y;

            AppWindow.Move(new PointInt32(
                _windowStartPosition.X + deltaX,
                _windowStartPosition.Y + deltaY
            ));
        }

        private void Bubble_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDragging) return;
            _isDragging = false;
            SnapToEdge();
        }

        private void Bubble_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _mainWindow.RestoreFromBubble();
        }

        private void SnapToEdge()
        {
            try
            {
                var position = AppWindow.Position;
                var size = AppWindow.Size;

                // 获取当前显示器的工作区域
                var displayArea = DisplayArea.GetFromPoint(
                    new PointInt32(position.X + size.Width / 2, position.Y + size.Height / 2),
                    DisplayAreaFallback.Primary
                );
                var workArea = displayArea.WorkArea;

                int newX = position.X;
                int newY = position.Y;

                // 水平停靠：离哪边近就停哪边
                int centerX = position.X + size.Width / 2;
                int screenCenterX = workArea.X + workArea.Width / 2;
                if (centerX < screenCenterX)
                    newX = workArea.X;
                else
                    newX = workArea.X + workArea.Width - size.Width;

                // 垂直方向：限制在屏幕内
                newY = Math.Max(workArea.Y, Math.Min(position.Y, workArea.Y + workArea.Height - size.Height));

                AppWindow.Move(new PointInt32(newX, newY));
            }
            catch
            {
                // 静默失败
            }
        }

        private void RestoreBubblePosition()
        {
            try
            {
                var settings = ApplicationData.Current.LocalSettings.Values;
                if (settings.ContainsKey(BubbleXKey) && settings.ContainsKey(BubbleYKey))
                {
                    int x = (int)(double)settings[BubbleXKey];
                    int y = (int)(double)settings[BubbleYKey];
                    AppWindow.Move(new PointInt32(x, y));
                }
                else
                {
                    // 默认位置：右侧中间
                    var displayArea = DisplayArea.GetFromPoint(
                        new PointInt32(0, 0),
                        DisplayAreaFallback.Primary
                    );
                    var workArea = displayArea.WorkArea;
                    AppWindow.Move(new PointInt32(
                        workArea.X + workArea.Width - 64,
                        workArea.Y + workArea.Height / 2 - 32
                    ));
                }
            }
            catch { }
        }

        private void SaveBubblePosition()
        {
            try
            {
                var settings = ApplicationData.Current.LocalSettings.Values;
                settings[BubbleXKey] = (double)AppWindow.Position.X;
                settings[BubbleYKey] = (double)AppWindow.Position.Y;
            }
            catch { }
        }

        public void RefreshDisplay()
        {
            var settings = ApplicationData.Current.LocalSettings.Values;
            int mode = (int)(double)(settings[BubbleDisplayModeKey] ?? 0.0);

            switch (mode)
            {
                case 1:
                    _bubbleText.Text = _mainWindow.GetStudentCountDisplay();
                    break;
                case 2:
                    _bubbleText.Text = _mainWindow.GetLastPickedStudentDisplay();
                    break;
                default:
                    _bubbleText.Text = "CSD";
                    break;
            }
        }
    }
}
