using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;


using CSD.Views;
using CSD.Models;
using CSD.Services;
using CSD.Helpers;
using CSD.Settings;


namespace CSD.Settings
{
    public static class SettingsUIHelper
    {
        public static StackPanel CreateCategoryView(params UIElement[] sections)
        {
            var panel = new StackPanel
            {
                Spacing = 16,
                Visibility = Visibility.Collapsed
            };

            foreach (var section in sections)
            {
                panel.Children.Add(section);
            }

            return panel;
        }

        public static Color GetBrushColor(string resourceKey, Color fallbackColor)
        {
            if (Application.Current.Resources.TryGetValue(resourceKey, out var value) && value is SolidColorBrush brush)
            {
                return brush.Color;
            }

            return fallbackColor;
        }

        public static Color WithAlpha(Color color, byte alpha)
        {
            return ColorHelper.FromArgb(alpha, color.R, color.G, color.B);
        }

        public static Border CreateSettingRow(string title, string description, params UIElement[] controls)
        {
            var labelStack = new StackPanel { Spacing = 4 };
            labelStack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 15,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            labelStack.Children.Add(new TextBlock
            {
                Text = description,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });

            var controlStack = new StackPanel
            {
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            foreach (var control in controls)
            {
                controlStack.Children.Add(control);
            }

            var grid = new Grid
            {
                Padding = new Thickness(16, 12, 16, 12),
                ColumnSpacing = 16
            };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(labelStack, 0);
            Grid.SetColumn(controlStack, 1);
            grid.Children.Add(labelStack);
            grid.Children.Add(controlStack);

            return new Border
            {
                CornerRadius = new CornerRadius(10),
                Child = grid
            };
        }

        public static NumberBox CreateNumberBoxWithoutHeader(double minimum, double maximum, double step, double defaultValue)
        {
            return new NumberBox
            {
                Minimum = minimum,
                Maximum = maximum,
                SmallChange = step,
                LargeChange = step * 5,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
                Value = defaultValue,
                MinWidth = 120
            };
        }

        public static Border CreateSectionCard(string title, string description, params UIElement[] children)
        {
            var stack = new StackPanel { Spacing = 12 };
            stack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 18,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            stack.Children.Add(new TextBlock
            {
                Text = description,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });

            foreach (var child in children)
            {
                stack.Children.Add(child);
            }

            return new Border
            {
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20),
                Child = stack
            };
        }

        public static StackPanel CreateIconTextRow(string glyph, string text)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            row.Children.Add(new FontIcon
            {
                Glyph = glyph,
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center
            });
            row.Children.Add(new TextBlock
            {
                Text = text,
                VerticalAlignment = VerticalAlignment.Center
            });
            return row;
        }

        public static Border CreateFilledCard(UIElement inner)
        {
            return new Border
            {
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
                BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(18, 16, 18, 16),
                Child = inner
            };
        }

        public static TextBlock CreateSecondaryWrappedText(string text, double fontSize = 14)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            };
        }
    }
}