using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Numerics;

namespace CSD
{
    internal static class AnimationHelper
    {
        public static void AnimateEntrance(UIElement element, float fromY = 20f, float fromOpacity = 0f, double durationMs = 360, double delayMs = 0)
        {
            if (element.XamlRoot is null && element is FrameworkElement frameworkElement)
            {
                RoutedEventHandler? loadedHandler = null;
                loadedHandler = (_, _) =>
                {
                    frameworkElement.Loaded -= loadedHandler;
                    RunEntranceAnimation(element, fromY, fromOpacity, durationMs, delayMs);
                };
                frameworkElement.Loaded += loadedHandler;
                return;
            }

            RunEntranceAnimation(element, fromY, fromOpacity, durationMs, delayMs);
        }

        public static void AttachHoverAnimation(UIElement element, float hoverScale = 1.02f, float pressedScale = 0.985f, float hoverOffsetY = -4f, bool enablePressedFeedback = true)
        {
            EnsureCenterPoint(element);

            element.PointerEntered += (_, _) => AnimateInteraction(element, hoverScale, 180);
            element.PointerExited += (_, _) => AnimateInteraction(element, 1f, 180);
            element.PointerCanceled += (_, _) => AnimateInteraction(element, 1f, 180);
            element.PointerCaptureLost += (_, _) => AnimateInteraction(element, 1f, 180);

            if (enablePressedFeedback)
            {
                element.PointerPressed += (_, _) => AnimateInteraction(element, pressedScale, 90);
                element.PointerReleased += (_, _) => AnimateInteraction(element, hoverScale, 140);
            }
        }

        public static void AnimateOpacity(UIElement element, float fromOpacity, float toOpacity, double durationMs = 220)
        {
            var visual = ElementCompositionPreview.GetElementVisual(element);
            var compositor = visual.Compositor;

            visual.Opacity = fromOpacity;

            var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
            opacityAnimation.InsertKeyFrame(1f, toOpacity);
            opacityAnimation.Duration = TimeSpan.FromMilliseconds(durationMs);
            opacityAnimation.Target = "Opacity";

            visual.StartAnimation("Opacity", opacityAnimation);
        }

        public static void ApplyStandardInteractions(DependencyObject root)
        {
            if (root is FrameworkElement { Tag: "DisableHoverAnimation" })
            {
                return;
            }

            if (root is Button button)
            {
                AttachHoverAnimation(button, 1.02f, 0.985f, -2f);
                return;
            }
            else if (root is TextBox textBox)
            {
                AttachHoverAnimation(textBox, 1.005f, 1f, -1f);
                return;
            }
            else if (root is PasswordBox passwordBox)
            {
                AttachHoverAnimation(passwordBox, 1.005f, 1f, -1f);
                return;
            }
            else if (root is NumberBox numberBox)
            {
                AttachHoverAnimation(numberBox, 1.005f, 1f, -1f);
                return;
            }
            else if (root is ToggleSwitch toggleSwitch)
            {
                AttachHoverAnimation(toggleSwitch, 1.01f, 1f, -1f);
                return;
            }

            int childCount = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < childCount; i++)
            {
                ApplyStandardInteractions(VisualTreeHelper.GetChild(root, i));
            }
        }

        private static void RunEntranceAnimation(UIElement element, float fromY, float fromOpacity, double durationMs, double delayMs)
        {
            var visual = ElementCompositionPreview.GetElementVisual(element);
            var compositor = visual.Compositor;

            visual.Opacity = fromOpacity;
            visual.Offset = new Vector3(0, fromY, 0);

            var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
            offsetAnimation.InsertKeyFrame(1f, Vector3.Zero);
            offsetAnimation.Duration = TimeSpan.FromMilliseconds(durationMs);
            offsetAnimation.DelayTime = TimeSpan.FromMilliseconds(delayMs);
            offsetAnimation.Target = "Offset";

            var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
            opacityAnimation.InsertKeyFrame(1f, 1f);
            opacityAnimation.Duration = TimeSpan.FromMilliseconds(durationMs);
            opacityAnimation.DelayTime = TimeSpan.FromMilliseconds(delayMs);
            opacityAnimation.Target = "Opacity";

            visual.StartAnimation("Offset", offsetAnimation);
            visual.StartAnimation("Opacity", opacityAnimation);
        }

        private static void AnimateInteraction(UIElement element, float scale, double durationMs)
        {
            var visual = ElementCompositionPreview.GetElementVisual(element);
            var compositor = visual.Compositor;

            var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
            scaleAnimation.InsertKeyFrame(1f, new Vector3(scale, scale, 1f));
            scaleAnimation.Duration = TimeSpan.FromMilliseconds(durationMs);
            scaleAnimation.Target = "Scale";

            visual.StartAnimation("Scale", scaleAnimation);
        }

        private static void EnsureCenterPoint(UIElement element)
        {
            if (element is not FrameworkElement frameworkElement)
            {
                return;
            }

            void updateCenterPoint()
            {
                var visual = ElementCompositionPreview.GetElementVisual(frameworkElement);
                visual.CenterPoint = new Vector3(
                    (float)Math.Max(0, frameworkElement.ActualWidth / 2),
                    (float)Math.Max(0, frameworkElement.ActualHeight / 2),
                    0);
            }

            frameworkElement.Loaded += (_, _) => updateCenterPoint();
            frameworkElement.SizeChanged += (_, _) => updateCenterPoint();
        }
    }
}
