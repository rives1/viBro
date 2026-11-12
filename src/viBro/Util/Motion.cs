using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace ViBro.Util;

/// <summary>
/// Fades of the interface (1.14.0): opacity animations of WPF elements only. The video is never
/// animated (mpv draws in a native child window, which WPF opacity does not reach) and no load waits
/// for an animation: they run on WPF's composition clock while the commands to mpv go out at once.
/// All fades follow Windows: with "Animation effects" off (Settings > Accessibility > Visual effects)
/// nothing is animated.
/// </summary>
internal static class Motion
{
    /// <summary>Opening and closing of the galleries.</summary>
    public static readonly Duration WindowFade = new(TimeSpan.FromMilliseconds(200));

    /// <summary>Rows that leave or enter the file list, and the border of the loaded video in the galleries.</summary>
    public static readonly Duration RowFade = new(TimeSpan.FromMilliseconds(150));

    /// <summary>Windows' own animation setting (SPI_GETCLIENTAREAANIMATION), read each time: it can change while viBro runs.</summary>
    public static bool Enabled
    {
        get
        {
            try
            {
                return SystemParameters.ClientAreaAnimation;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    /// <summary>From transparent to the element's own opacity; nothing is held at the end.</summary>
    public static void FadeIn(UIElement element, Duration duration)
    {
        if (!Enabled) return;
        element.BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(0, 1, duration) { FillBehavior = FillBehavior.Stop });
    }

    /// <summary>
    /// Fades the elements to transparent and completes when they are; they stay transparent (held)
    /// until <see cref="Clear"/>. Completes at once when animations are off or there is nothing to fade.
    /// </summary>
    public static Task FadeOutAsync(IEnumerable<UIElement> elements, Duration duration)
    {
        var list = elements.Distinct().ToList();
        if (list.Count == 0 || !Enabled) return Task.CompletedTask;
        var done = new TaskCompletionSource();
        var animation = new DoubleAnimation(0, duration) { FillBehavior = FillBehavior.HoldEnd };
        int left = list.Count;
        animation.Completed += (_, _) =>
        {
            if (--left == 0) done.TrySetResult(); // one Completed per element (one clock each)
        };
        foreach (var e in list) e.BeginAnimation(UIElement.OpacityProperty, animation);
        // Never wait longer than the fade itself (plus a margin): an animation replaced or stopped on
        // the way (a recycled row, say) raises no Completed, and a file operation must not hang on it.
        return Task.WhenAny(done.Task, Task.Delay(duration.TimeSpan + TimeSpan.FromMilliseconds(250)));
    }

    /// <summary>Removes any animation (and held value) from the element's opacity.</summary>
    public static void Clear(UIElement element) => element.BeginAnimation(UIElement.OpacityProperty, null);
}
