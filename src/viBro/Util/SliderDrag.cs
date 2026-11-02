using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace ViBro.Util;

/// <summary>
/// "Click and drag" for sliders. With IsMoveToPointEnabled, WPF moves the value to the clicked
/// point but does not start a drag: the thumb only followed the mouse after a second click.
/// After WPF has moved the value, the same mouse press is handed to the thumb, which captures the
/// mouse and drags as if it had been pressed directly.
/// </summary>
internal static class SliderDrag
{
    public static void Enable(Slider slider)
    {
        slider.IsMoveToPointEnabled = true;
        // handledEventsToo: Slider's own class handler marks the press as handled after moving.
        slider.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent,
            new MouseButtonEventHandler(OnPreviewMouseLeftButtonDown), handledEventsToo: true);
    }

    private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Slider slider || !slider.IsEnabled) return;
        if (slider.Template?.FindName("PART_Track", slider) is not Track { Thumb: Thumb thumb }) return;
        thumb.Focusable = false; // dragging must not take keyboard focus from the file list
        if (thumb.IsMouseOver || thumb.IsDragging) return; // a press on the thumb already drags

        slider.UpdateLayout();   // put the thumb under the pointer before the drag starts
        thumb.RaiseEvent(new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Left)
        {
            RoutedEvent = UIElement.MouseLeftButtonDownEvent,
            Source = thumb,
        });
    }
}
