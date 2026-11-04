using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ViBro.Settings;
using ViBro.Util;

namespace ViBro;

/// <summary>Small modal dialogs built in code (delete confirmation, rename, name conflict, transfer progress).</summary>
internal static class DialogStyle
{
    public static void Apply(Window w, Window owner, string title)
    {
        w.Owner = owner;
        w.Title = title;
        w.SizeToContent = SizeToContent.Height;
        w.Width = 520;
        w.ResizeMode = ResizeMode.NoResize;
        w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        w.ShowInTaskbar = false;
        ThemeManager.AttachWindow(w);
    }

    public static StackPanel Buttons(params Button[] buttons)
    {
        var p = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
        foreach (var b in buttons)
        {
            b.MinWidth = 90;
            b.Margin = new Thickness(6, 0, 0, 0);
            b.Padding = new Thickness(10, 3, 10, 3);
            p.Children.Add(b);
        }
        return p;
    }
}

/// <summary>F2: edits the name only; the extension is kept. The caret starts at the beginning of the name, nothing selected. Enter confirms, Esc cancels.</summary>
internal sealed class RenameDialog : Window
{
    private readonly TextBox _box;
    private readonly TextBlock _error;
    private readonly string _extension;

    public string? ResultName { get; private set; }

    public RenameDialog(Window owner, string currentName, string? proposedStem = null, string? error = null)
    {
        DialogStyle.Apply(this, owner, "Rename");
        _extension = Path.GetExtension(currentName);

        var panel = new StackPanel { Margin = new Thickness(12) };
        panel.Children.Add(new TextBlock { Text = "New name:", Margin = new Thickness(0, 0, 0, 4) });

        var row = new DockPanel();
        var ext = new TextBlock { Text = _extension, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) };
        DockPanel.SetDock(ext, Dock.Right);
        row.Children.Add(ext);
        _box = new TextBox { Text = proposedStem ?? Path.GetFileNameWithoutExtension(currentName), Padding = new Thickness(2) };
        row.Children.Add(_box);
        panel.Children.Add(row);

        _error = new TextBlock
        {
            Foreground = Brushes.Firebrick,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0),
            Text = error ?? "",
            Visibility = error == null ? Visibility.Collapsed : Visibility.Visible,
        };
        panel.Children.Add(_error);

        var ok = new Button { Content = "Rename", IsDefault = true };
        var cancel = new Button { Content = "Cancel", IsCancel = true };
        ok.Click += (_, _) => Confirm();
        panel.Children.Add(DialogStyle.Buttons(ok, cancel));
        Content = panel;

        Loaded += (_, _) =>
        {
            _box.Focus();
            _box.CaretIndex = 0; // at the beginning of the name, nothing selected (user's choice, 1.1.0)
        };
    }

    private void Confirm()
    {
        string stem = _box.Text;
        string? problem = FileOps.ValidateBaseName(stem, _extension);
        if (problem != null)
        {
            _error.Text = problem;
            _error.Visibility = Visibility.Visible;
            _box.Focus();
            return;
        }
        ResultName = stem + _extension;
        DialogResult = true;
    }
}

/// <summary>
/// Delete confirmation (replaces the MessageBox, which cannot be positioned): opens next to the mouse
/// pointer, inside the work area of that monitor. Enter = delete, Esc = keep the file.
/// </summary>
internal sealed class DeleteConfirmDialog : Window
{
    private DeleteConfirmDialog(Window owner, string title, string text, bool warning)
    {
        DialogStyle.Apply(this, owner, title);
        Width = 440;
        WindowStartupLocation = WindowStartupLocation.Manual;
        var panel = new StackPanel { Margin = new Thickness(12) };
        var message = new TextBlock { TextWrapping = TextWrapping.Wrap, Text = text };
        if (warning) message.Foreground = Brushes.DarkOrange; // permanent deletion (network)
        panel.Children.Add(message);
        var yes = new Button { Content = "Delete", IsDefault = true };
        var no = new Button { Content = "Cancel", IsCancel = true };
        yes.Click += (_, _) => DialogResult = true;
        panel.Children.Add(DialogStyle.Buttons(yes, no));
        Content = panel;
        Loaded += (_, _) =>
        {
            PlaceNearPointer();
            yes.Focus();
        };
    }

    public static bool Ask(Window owner, string title, string text, bool warning)
        => new DeleteConfirmDialog(owner, title, text, warning).ShowDialog() == true;

    /// <summary>Top-left corner just below-right of the pointer; moved inside the monitor's work area.</summary>
    private void PlaceNearPointer()
    {
        var source = PresentationSource.FromVisual(Owner);
        if (source?.CompositionTarget == null || !NativeMethods.GetCursorPos(out var cursor))
        {
            Left = Owner.Left + (Owner.ActualWidth - ActualWidth) / 2;
            Top = Owner.Top + (Owner.ActualHeight - ActualHeight) / 2;
            return;
        }
        var toDip = source.CompositionTarget.TransformFromDevice;
        Point p = toDip.Transform(new Point(cursor.X + 12, cursor.Y + 12));
        double left = p.X, top = p.Y;
        var area = NativeMethods.WorkAreaAt(cursor.X, cursor.Y);
        if (area is var (l, t, r, b))
        {
            Point min = toDip.Transform(new Point(l, t));
            Point max = toDip.Transform(new Point(r, b));
            left = Math.Max(min.X, Math.Min(left, max.X - ActualWidth));
            top = Math.Max(min.Y, Math.Min(top, max.Y - ActualHeight));
        }
        Left = left;
        Top = top;
    }
}

/// <summary>Name conflict at the move/copy destination. Overwrite is offered only as an explicit choice.</summary>
internal sealed class ConflictDialog : Window
{
    private ConflictChoice _choice = ConflictChoice.Cancel;

    private ConflictDialog(Window owner, string fileName, string destFolder, string keepBothName, bool copy)
    {
        DialogStyle.Apply(this, owner, "File already exists");
        var panel = new StackPanel { Margin = new Thickness(12) };
        panel.Children.Add(new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Text = $"A file named \"{fileName}\" already exists in:\n{destFolder}\n\n" +
                   $"Keep both: the {(copy ? "copied" : "moved")} file is saved as \"{keepBothName}\".\n" +
                   "Overwrite: the existing file in the destination is replaced and cannot be recovered.",
        });

        var keep = new Button { Content = "Keep both", IsDefault = true };
        var overwrite = new Button { Content = "Overwrite" };
        var cancel = new Button { Content = "Cancel", IsCancel = true };
        keep.Click += (_, _) => { _choice = ConflictChoice.KeepBoth; DialogResult = true; };
        overwrite.Click += (_, _) => { _choice = ConflictChoice.Overwrite; DialogResult = true; };
        panel.Children.Add(DialogStyle.Buttons(keep, overwrite, cancel));
        Content = panel;
        Loaded += (_, _) => keep.Focus();
    }

    public static ConflictChoice Ask(Window owner, string fileName, string destFolder, string keepBothName, bool copy = false)
    {
        var d = new ConflictDialog(owner, fileName, destFolder, keepBothName, copy);
        return d.ShowDialog() == true ? d._choice : ConflictChoice.Cancel;
    }
}

/// <summary>Progress of a long transfer, with cancellation. Closing the window also cancels.</summary>
internal sealed class ProgressDialog : Window
{
    private readonly ProgressBar _bar;
    private readonly TextBlock _percent;
    private readonly CancellationTokenSource _cts;
    private bool _finished;

    public ProgressDialog(Window owner, string fileName, string destFolder, CancellationTokenSource cts, bool copy = false, string? verb = null)
    {
        verb ??= copy ? "Copying" : "Moving";
        DialogStyle.Apply(this, owner, verb + " file");
        _cts = cts;
        var panel = new StackPanel { Margin = new Thickness(12) };
        panel.Children.Add(new TextBlock { TextWrapping = TextWrapping.Wrap, Text = $"{verb} \"{fileName}\"\nto {destFolder}" });
        _bar = new ProgressBar { Minimum = 0, Maximum = 1, Height = 18, Margin = new Thickness(0, 10, 0, 4) };
        panel.Children.Add(_bar);
        _percent = new TextBlock { Text = "0 %" };
        panel.Children.Add(_percent);
        var cancel = new Button { Content = "Cancel", IsCancel = true };
        cancel.Click += (_, _) => RequestCancel();
        panel.Children.Add(DialogStyle.Buttons(cancel));
        Content = panel;
        Closing += (_, e) =>
        {
            if (_finished) return;
            e.Cancel = true; // stay open until the operation has really stopped
            RequestCancel();
        };
    }

    public void Report(double fraction)
    {
        _bar.Value = Math.Clamp(fraction, 0, 1);
        _percent.Text = $"{_bar.Value * 100:0} %";
    }

    private void RequestCancel()
    {
        _percent.Text = "Cancelling...";
        _cts.Cancel();
    }

    public void Finish()
    {
        _finished = true;
        Close();
    }
}

/// <summary>Ctrl+F2: keyboard and mouse commands in one window. Esc or Enter closes it.</summary>
internal sealed class CommandsDialog : Window
{
    /// <summary>The seek rows show the steps read from [Seek] in viBro.ini (1.4.0).</summary>
    private static (string Section, (string Keys, string Action)[] Rows)[] Commands(AppSettings s) => new[]
    {
        ("Keyboard", new[]
        {
            ("← / →", "Back / forward " + AppSettings.FormatSeconds(s.SeekLeftRightSeconds)),
            ("↓ / ↑", "Back / forward " + AppSettings.FormatSeconds(s.SeekUpDownSeconds)),
            ("PgDown / PgUp", "Back / forward " + AppSettings.FormatSeconds(s.SeekPageSeconds)),
            ("Alt + PgDown / PgUp", "Back / forward " + AppSettings.FormatSeconds(s.SeekAltPageSeconds)),
            ("Space", "Play / Pause"),
            ("E / D", "Previous / next video (play state kept)"),
            ("M", "Mute on / off"),
            ("F11 / Esc", "Enter / leave fullscreen"),
            ("Ctrl + L", "Edit the path (Enter opens, Esc cancels)"),
            ("Enter", "Open the selected folder (\"..\" = parent folder)"),
            ("Shift + Enter", "Open the video with the Windows default application"),
            ("Del or R", "Delete the file (confirmation)"),
            ("F2", "Rename the file"),
            ("W / Shift + W", "Move to a folder / to the last move folder"),
            ("C / Shift + C", "Copy to a folder / to the last copy folder"),
            ("I / O", "Start / end marker for the extract"),
            ("F9", FfmpegExtractor.IsAvailable
                ? "Extract the marked part to a new file in the same folder (no re-encoding: starts at the keyframe before I)"
                : "Extract: not available, " + FfmpegExtractor.ExeName + " not found in the viBro folder"),
            ("F10", FfmpegExtractor.IsAvailable
                ? $"Gallery of the folder: {s.GalleryPercents.Length} images per video (click a row: load it; click an image: load it at that time; Esc closes)"
                : "Gallery: not available, " + FfmpegExtractor.ExeName + " not found in the viBro folder"),
            ("Ctrl + F2", "This window"),
            ("Ctrl + W", "Close viBro"),
        }),
        ("File list", new[]
        {
            ("Click", "Load the video (play state kept)"),
            ("Double click on a folder", "Open it (\"..\" = parent folder)"),
        }),
        ("Mouse on the video", new[]
        {
            ("Click", "Play / Pause"),
            ("Double click", "Enter / leave fullscreen"),
            ("Right click", "Menu: Delete, Move, Copy"),
            ("Right button + drag", "Move in the video (full width = full length)"),
            ("Wheel", "Back / forward " + AppSettings.FormatSeconds(s.SeekWheelSeconds)),
        }),
        ("Timeline", new[]
        {
            ("Click and drag", "Move in the video"),
            ("Wheel", "Next (down) / previous (up) video"),
            ("Pointer", "Time under the pointer"),
        }),
    };

    private CommandsDialog(Window owner, AppSettings settings)
    {
        DialogStyle.Apply(this, owner, "Commands");
        Width = 560;
        var grid = new Grid { Margin = new Thickness(12) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        int row = 0;
        foreach (var (section, rows) in Commands(settings))
        {
            AddRow(grid, row++, new TextBlock
            {
                Text = section,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, row == 1 ? 0 : 10, 0, 4),
            }, span: true);
            foreach (var (keys, action) in rows)
            {
                AddRow(grid, row, new TextBlock { Text = keys, Margin = new Thickness(0, 1, 8, 1) });
                var text = new TextBlock { Text = action, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 1, 0, 1) };
                Grid.SetColumn(text, 1);
                Grid.SetRow(text, row++);
                grid.Children.Add(text);
            }
        }
        var close = new Button { Content = "Close", IsDefault = true, IsCancel = true };
        var panel = new StackPanel();
        panel.Children.Add(new ScrollViewer
        {
            Content = grid,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxHeight = SystemParameters.WorkArea.Height * 0.8,
        });
        var buttons = DialogStyle.Buttons(close);
        buttons.Margin = new Thickness(12, 0, 12, 12);
        panel.Children.Add(buttons);
        Content = panel;
        Loaded += (_, _) => close.Focus();
    }

    private static void AddRow(Grid grid, int row, UIElement element, bool span = false)
    {
        while (grid.RowDefinitions.Count <= row) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Grid.SetRow(element, row);
        if (span) Grid.SetColumnSpan(element, 2);
        grid.Children.Add(element);
    }

    public static void Open(Window owner, AppSettings settings) => new CommandsDialog(owner, settings).ShowDialog();
}
