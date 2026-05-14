using Microsoft.UI.Windowing;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.UI;

namespace CSD
{
    /// <summary>
    /// Initialization window shown when the app starts.
    /// </summary>
    public sealed partial class InitializationWindow : Window
    {
        private const string TokenSettingsKey = "Token";
        private const string IntroWord = "Classworks";

        private readonly List<Border> _introBlocks = new();
        private readonly List<TextBlock> _introTexts = new();
        private readonly List<TranslateTransform> _introBlockTranslations = new();
        private readonly List<ScaleTransform> _introBlockScales = new();
        private readonly List<PlaneProjection> _introBlockProjections = new();
        private readonly List<PlaneProjection> _introTextProjections = new();
        private Grid? _animationStage;
        private Grid? _contentRoot;
        private Grid? _introOverlay;
        private Image? _welcomeLogo;
        private StackPanel? _welcomeActionsPanel;
        private StackPanel? _contentTextHost;
        private StackPanel? _introTextPanel;
        private TranslateTransform? _introTextPanelTranslation;
        private TextBlock? _introDesktopText;
        private TranslateTransform? _introDesktopTextTranslation;
        private ScaleTransform? _introDesktopTextScale;
        private Button? _nextButton;

        private bool _hasPlayedInitializationAnimation;
        private bool _isTransitioningToForm;

        public InitializationWindow()
        {
            InitializeComponent();
            BuildAnimationVisuals();
            ConfigureIntegratedTitleBar();

            try
            {
                var iconUri = AppSettings.GetAssetUri("Assets/StoreLogo.png");
                AppWindow.SetIcon(iconUri.LocalPath);
            }
            catch { }

            RestoreWindowState();
            Closed += (sender, args) => SaveWindowState();

            if (Content is FrameworkElement rootContent)
            {
                rootContent.Loaded += RootContent_Loaded;
            }
        }

        private void ConfigureIntegratedTitleBar()
        {
            if (!AppWindowTitleBar.IsCustomizationSupported())
            {
                return;
            }

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

        private void RootContent_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement rootContent)
            {
                rootContent.Loaded -= RootContent_Loaded;
                _ = RunInitializationSequenceAsync();
            }
        }

        private void BuildAnimationVisuals()
        {
            FormPanel.Opacity = 0;
            FormPanel.IsHitTestVisible = false;

            _animationStage = new Grid
            {
                IsHitTestVisible = false
            };

            _contentRoot = new Grid
            {
                Opacity = 0,
                RowDefinitions =
                {
                    new RowDefinition(),
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition()
                }
            };

            _welcomeLogo = new Image
            {
                Width = 80,
                Height = 80,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 8),
                Opacity = 0,
                Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(AppSettings.GetAssetUri("icons/Classworks.ico"))
            };
            Grid.SetRow(_welcomeLogo, 0);
            _contentRoot.Children.Add(_welcomeLogo);

            _contentTextHost = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = 0
            };
            for (int i = 0; i < IntroWord.Length; i++)
            {
                var tb = new TextBlock
                {
                    Text = IntroWord[i].ToString(),
                    FontSize = 32,
                    FontWeight = FontWeights.SemiBold,
                    TextAlignment = TextAlignment.Center
                };
                _contentTextHost.Children.Add(tb);
            }
            Grid.SetRow(_contentTextHost, 1);
            _contentRoot.Children.Add(_contentTextHost);

            _welcomeActionsPanel = new StackPanel
            {
                Spacing = 4,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Opacity = 0
            };
            _introOverlay = new Grid
            {
                Opacity = 0,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5)
            };
            _introOverlay.RenderTransform = new ScaleTransform { ScaleX = 1.25, ScaleY = 1.25 };

            var introGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            BuildIntroVisualTree(introGrid);
            _introOverlay.Children.Add(introGrid);

            _nextButton = new Button
            {
                Content = new SymbolIcon(Symbol.Forward),
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = 48,
                Height = 48,
                CornerRadius = new CornerRadius(24),
                Margin = new Thickness(0, 8, 0, 0)
            };
            _nextButton.Click += NextButton_Click;
            _welcomeActionsPanel.Children.Add(_nextButton);
            Grid.SetRow(_welcomeActionsPanel, 2);
            _contentRoot.Children.Add(_welcomeActionsPanel);

            _animationStage.Children.Add(_contentRoot);
            _animationStage.Children.Add(_introOverlay);
            ContentHost.Children.Add(_animationStage);
        }

        private void BuildIntroVisualTree(Grid host)
        {
            _introBlocks.Clear();
            _introTexts.Clear();
            _introBlockTranslations.Clear();
            _introBlockScales.Clear();
            _introBlockProjections.Clear();
            _introTextProjections.Clear();

            var rects = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 4
            };
            _introTextPanelTranslation = new TranslateTransform();
            _introTextPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 4,
                RenderTransform = _introTextPanelTranslation
            };

            for (int i = 0; i < IntroWord.Length; i++)
            {
                var translateTransform = new TranslateTransform { Y = 50 };
                var scaleTransform = new ScaleTransform { ScaleX = 1, ScaleY = 1 };
                var blockTransforms = new TransformGroup();
                blockTransforms.Children.Add(translateTransform);
                blockTransforms.Children.Add(scaleTransform);
                var blockProjection = new PlaneProjection { RotationX = 0 };

                var block = new Border
                {
                    Width = 32,
                    Height = 32,
                    CornerRadius = new CornerRadius(4),
                    Background = new SolidColorBrush(i == 5
                        ? Color.FromArgb(255, 0, 191, 255)
                        : Color.FromArgb(255, 242, 242, 242)),
                    Opacity = 0,
                    RenderTransformOrigin = new Windows.Foundation.Point(0, 12),
                    RenderTransform = blockTransforms,
                    Projection = blockProjection
                };
                if (i == 5)
                {
                    Canvas.SetZIndex(block, 1);
                }

                var textProjection = new PlaneProjection { RotationX = -90 };
                var text = new TextBlock
                {
                    Text = IntroWord[i].ToString(),
                    MinWidth = 32,
                    FontSize = 32,
                    FontWeight = FontWeights.SemiBold,
                    TextAlignment = TextAlignment.Center,
                    Opacity = 0,
                    RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5),
                    Projection = textProjection
                };

                rects.Children.Add(block);
                _introTextPanel.Children.Add(text);

                _introBlocks.Add(block);
                _introTexts.Add(text);
                _introBlockTranslations.Add(translateTransform);
                _introBlockScales.Add(scaleTransform);
                _introBlockProjections.Add(blockProjection);
                _introTextProjections.Add(textProjection);
            }

            _introDesktopTextTranslation = new TranslateTransform();
            _introDesktopTextScale = new ScaleTransform { ScaleX = 0.965, ScaleY = 0.965 };
            var introDesktopTextTransforms = new TransformGroup();
            introDesktopTextTransforms.Children.Add(_introDesktopTextScale);
            introDesktopTextTransforms.Children.Add(_introDesktopTextTranslation);
            _introDesktopText = new TextBlock
            {
                Text = "Desktop",
                FontSize = 32,
                FontWeight = FontWeights.SemiBold,
                Opacity = 0,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5),
                RenderTransform = introDesktopTextTransforms,
                Foreground = new LinearGradientBrush
                {
                    StartPoint = new Windows.Foundation.Point(0, 0),
                    EndPoint = new Windows.Foundation.Point(1, 1),
                    GradientStops =
                    {
                        new GradientStop { Color = Color.FromArgb(255, 0x00, 0xCC, 0xFF), Offset = 0 },
                        new GradientStop { Color = Color.FromArgb(255, 0x00, 0x7F, 0xFF), Offset = 1 }
                    }
                }
            };

            host.Children.Add(rects);
            host.Children.Add(_introTextPanel);
            host.Children.Add(_introDesktopText);
        }

        private void ResetIntroVisualState()
        {
            if (_introOverlay?.RenderTransform is ScaleTransform overlayScale)
            {
                overlayScale.ScaleX = 1.25;
                overlayScale.ScaleY = 1.25;
            }

            if (_introOverlay != null)
            {
                _introOverlay.Opacity = 0;
            }

            if (_contentRoot != null)
            {
                _contentRoot.Opacity = 0;
            }
            if (_welcomeLogo != null)
            {
                _welcomeLogo.Opacity = 0;
            }
            if (_welcomeActionsPanel != null)
            {
                _welcomeActionsPanel.Opacity = 0;
            }
            if (_contentTextHost != null)
            {
                _contentTextHost.Opacity = 0;
            }
            if (_introTextPanelTranslation != null)
            {
                _introTextPanelTranslation.X = 0;
            }
            if (_introDesktopText != null)
            {
                _introDesktopText.Opacity = 0;
            }
            if (_introDesktopTextTranslation != null)
            {
                _introDesktopTextTranslation.X = 0;
            }
            if (_introDesktopTextScale != null)
            {
                _introDesktopTextScale.ScaleX = 0.965;
                _introDesktopTextScale.ScaleY = 0.965;
            }
            if (_nextButton != null)
            {
                _nextButton.IsEnabled = false;
            }

            for (int i = 0; i < _introBlocks.Count; i++)
            {
                _introBlocks[i].Opacity = 0;
                _introBlockTranslations[i].Y = 50;
                _introBlockScales[i].ScaleX = 1;
                _introBlockScales[i].ScaleY = 1;
                _introBlockProjections[i].RotationX = 0;
                _introTexts[i].Opacity = 0;
                _introTexts[i].MinWidth = 32;
                _introTextProjections[i].RotationX = -90;
            }
        }

        private async Task RunInitializationSequenceAsync()
        {
            if (_hasPlayedInitializationAnimation)
            {
                return;
            }

            _hasPlayedInitializationAnimation = true;
            ResetIntroVisualState();

            await Task.Delay(80);
            await PlayOobeIntroAnimationAsync();
        }

        private async Task PlayOobeIntroAnimationAsync()
        {
            if (_introOverlay == null)
            {
                return;
            }

            TitleStatusText.Text = "欢迎";

            if (_introOverlay.RenderTransform is ScaleTransform overlayScale)
            {
                StartDoubleAnimation(overlayScale, nameof(ScaleTransform.ScaleX), 1.25, 1, 3000, 0, new CubicEase { EasingMode = EasingMode.EaseOut });
                StartDoubleAnimation(overlayScale, nameof(ScaleTransform.ScaleY), 1.25, 1, 3000, 0, new CubicEase { EasingMode = EasingMode.EaseOut });
            }

            StartDoubleAnimation(_introOverlay, nameof(UIElement.Opacity), 0, 1, 3000, 0, new CubicEase { EasingMode = EasingMode.EaseOut });

            double elapsedDelayMs = 0;
            double maxEndMs = 0;
            int count = _introBlocks.Count;
            const double durationMs = 500;

            for (int i = 0; i < count; i++)
            {
                var stepDelay = Math.Sin(((i + 2d) / (count + 2d)) * (Math.PI / 2d)) * durationMs / count;
                var phaseMs = stepDelay * 9;
                StartIntroLetterAnimation(i, phaseMs);
                maxEndMs = Math.Max(maxEndMs, elapsedDelayMs + (phaseMs * 2) + 750);
                elapsedDelayMs += stepDelay;
                await Task.Delay(TimeSpan.FromMilliseconds(stepDelay));
            }

            await Task.Delay(TimeSpan.FromMilliseconds(Math.Max(0, maxEndMs - elapsedDelayMs)));
            await RevealWelcomeChromeAsync();
        }

        private void StartIntroLetterAnimation(int index, double phaseMs)
        {
            var easeOut = new ExponentialEase { EasingMode = EasingMode.EaseOut };
            var easeIn = new ExponentialEase { EasingMode = EasingMode.EaseIn };

            StartDoubleAnimation(_introBlockTranslations[index], nameof(TranslateTransform.Y), 50, 0, phaseMs, 0, easeOut);
            StartDoubleAnimation(_introBlocks[index], nameof(UIElement.Opacity), 0, 1, phaseMs, 0, easeOut);

            if (index == 5)
            {
                StartDoubleAnimation(_introBlockScales[index], nameof(ScaleTransform.ScaleX), 1, 2.17, 250, 307, new CubicEase { EasingMode = EasingMode.EaseOut });
            }

            if (index == 6)
            {
                StartDoubleAnimation(_introBlocks[index], nameof(UIElement.Opacity), 1, 0, 1, phaseMs + 1, easeOut);
            }

            StartDoubleAnimation(_introBlockProjections[index], nameof(PlaneProjection.RotationX), 0, 90, phaseMs * 0.5, phaseMs + 750, easeIn);
            StartDoubleAnimation(_introBlocks[index], nameof(UIElement.Opacity), index == 6 ? 0 : 1, 0, phaseMs * 0.5, phaseMs + 750, easeIn);

            StartDoubleAnimation(_introTextProjections[index], nameof(PlaneProjection.RotationX), -90, 0, phaseMs * 0.5, (phaseMs * 1.5) + 750, easeOut);
            StartDoubleAnimation(_introTexts[index], nameof(UIElement.Opacity), 0, 1, phaseMs * 0.5, (phaseMs * 1.5) + 750, easeOut);
        }

        private async Task RevealWelcomeChromeAsync()
        {
            if (_contentRoot == null || _introOverlay == null || _nextButton == null || _welcomeLogo == null || _welcomeActionsPanel == null)
            {
                return;
            }

            TitleStatusText.Text = "欢迎";

            _contentRoot.Opacity = 1;
            StartDoubleAnimation(_welcomeLogo, nameof(UIElement.Opacity), 0, 1, 200, 0, new CubicEase { EasingMode = EasingMode.EaseOut });
            StartDoubleAnimation(_welcomeActionsPanel, nameof(UIElement.Opacity), 0, 1, 200, 0, new CubicEase { EasingMode = EasingMode.EaseOut });
            await Task.Delay(20);

            foreach (var tb in _introTexts)
            {
                StartDoubleAnimation(tb, nameof(FrameworkElement.MinWidth), 32, 0, 700, 0, new CubicEase { EasingMode = EasingMode.EaseOut });
            }
            if (_introTextPanel != null)
            {
                StartDoubleAnimation(_introTextPanel, nameof(StackPanel.Spacing), 4, 0, 700, 0, new CubicEase { EasingMode = EasingMode.EaseOut });
            }

            await Task.Delay(720);

            if (_introDesktopText != null && _introDesktopTextTranslation != null && _introTextPanel != null)
            {
                _introOverlay.UpdateLayout();
                _introTextPanel.UpdateLayout();
                _introDesktopText.UpdateLayout();

                const double gap = 10;
                double desktopWidth = _introDesktopText.ActualWidth;
                double classworksTargetX = -((gap + desktopWidth) / 2d);
                double desktopTargetX = (_introTextPanel.ActualWidth / 2d) + (gap / 2d);

                if (_introTextPanelTranslation != null)
                {
                    StartDoubleAnimation(_introTextPanelTranslation, nameof(TranslateTransform.X), 0, classworksTargetX, 300, 0, new ExponentialEase { Exponent = 5, EasingMode = EasingMode.EaseOut });
                }

                _introDesktopTextTranslation.X = desktopTargetX + 14;
                StartDoubleAnimation(_introDesktopTextTranslation, nameof(TranslateTransform.X), desktopTargetX + 14, desktopTargetX, 220, 20, new ExponentialEase { Exponent = 6, EasingMode = EasingMode.EaseOut });
                StartDoubleAnimation(_introDesktopText, nameof(UIElement.Opacity), 0, 1, 180, 35, new ExponentialEase { Exponent = 6, EasingMode = EasingMode.EaseOut });
                if (_introDesktopTextScale != null)
                {
                    StartDoubleAnimation(_introDesktopTextScale, nameof(ScaleTransform.ScaleX), 0.965, 1, 220, 20, new QuadraticEase { EasingMode = EasingMode.EaseOut });
                    StartDoubleAnimation(_introDesktopTextScale, nameof(ScaleTransform.ScaleY), 0.965, 1, 220, 20, new QuadraticEase { EasingMode = EasingMode.EaseOut });
                }
            }

            await Task.Delay(320);
            _nextButton.IsEnabled = true;
            _animationStage!.IsHitTestVisible = true;
        }

        private async Task ShowFormAsync()
        {
            if (_isTransitioningToForm)
            {
                return;
            }
            _isTransitioningToForm = true;

            if (_animationStage != null)
            {
                await FadeVisualOpacityAsync(_animationStage, 1f, 0f, 220);
                _animationStage.Visibility = Visibility.Collapsed;
                _animationStage.IsHitTestVisible = false;
            }

            FormPanel.Opacity = 1;
            FormPanel.IsHitTestVisible = true;
            TitleStatusText.Text = "初始化";

            AnimationHelper.AnimateEntrance(FormPanel, fromY: 150f, durationMs: 360);
            AnimationHelper.ApplyStandardInteractions(FormPanel);

            await Task.Delay(380);
            TokenBox.Focus(FocusState.Programmatic);
        }

        private async Task FadeVisualOpacityAsync(UIElement element, float from, float to, double durationMs)
        {
            var visual = ElementCompositionPreview.GetElementVisual(element);
            var compositor = visual.Compositor;
            var easing = compositor.CreateCubicBezierEasingFunction(new Vector2(0.19f, 1f), new Vector2(0.22f, 1f));

            visual.Opacity = from;

            var animation = compositor.CreateScalarKeyFrameAnimation();
            animation.InsertKeyFrame(1f, to, easing);
            animation.Duration = TimeSpan.FromMilliseconds(durationMs);

            visual.StartAnimation("Opacity", animation);
            await Task.Delay(animation.Duration);
        }

        private void StartDoubleAnimation(
            DependencyObject target,
            string property,
            double from,
            double to,
            double durationMs,
            double beginTimeMs,
            EasingFunctionBase easing)
        {
            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                BeginTime = TimeSpan.FromMilliseconds(beginTimeMs),
                EnableDependentAnimation = true,
                EasingFunction = easing
            };

            var storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, property);
            storyboard.Begin();
        }

        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowFormAsync();
        }

        private void RestoreWindowState()
        {
            try
            {
                var settings = AppSettings.Values;

                if (settings.ContainsKey("InitWindow_Width") && settings.ContainsKey("InitWindow_Height"))
                {
                    int width = Math.Max(400, (int)(double)settings["InitWindow_Width"]);
                    int height = Math.Max(300, (int)(double)settings["InitWindow_Height"]);
                    this.AppWindow.Resize(new SizeInt32(width, height));
                }

                if (settings.ContainsKey("InitWindow_X") && settings.ContainsKey("InitWindow_Y"))
                {
                    int x = (int)(double)settings["InitWindow_X"];
                    int y = (int)(double)settings["InitWindow_Y"];
                    this.AppWindow.Move(new PointInt32(x, y));
                }

                if (settings.ContainsKey("InitWindow_State"))
                {
                    string? state = settings["InitWindow_State"] as string;
                    if (state == "Maximized" && this.AppWindow.Presenter is OverlappedPresenter presenter)
                    {
                        presenter.Maximize();
                    }
                }
            }
            catch { }
        }

        private void SaveWindowState()
        {
            try
            {
                var settings = AppSettings.Values;
                settings["InitWindow_X"] = (double)this.AppWindow.Position.X;
                settings["InitWindow_Y"] = (double)this.AppWindow.Position.Y;
                settings["InitWindow_Width"] = (double)this.AppWindow.Size.Width;
                settings["InitWindow_Height"] = (double)this.AppWindow.Size.Height;

                if (this.AppWindow.Presenter is OverlappedPresenter presenter)
                {
                    settings["InitWindow_State"] = presenter.State.ToString();
                }
            }
            catch { }
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TokenBox.Password))
            {
                return;
            }

            AppSettings.Values[TokenSettingsKey] = TokenBox.Password;

            var mainWindow = new MainWindow();
            mainWindow.Activate();
            Close();
        }

        private void TutorialButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://520.re/csh",
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch { }
        }
    }
}
