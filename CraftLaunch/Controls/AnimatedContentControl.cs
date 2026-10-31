using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CraftLaunch.Controls;

public class AnimatedContentControl : ContentControl
{
    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        if (newContent is null)
            return;

        var transform = new TranslateTransform(28, 0);
        RenderTransform = transform;

        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260)) { EasingFunction = easing };
        var slide = new DoubleAnimation(28, 0, TimeSpan.FromMilliseconds(260)) { EasingFunction = easing };

        BeginAnimation(OpacityProperty, fade);
        transform.BeginAnimation(TranslateTransform.XProperty, slide);
    }
}
