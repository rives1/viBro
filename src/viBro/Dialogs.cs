using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ViBro.Util;

namespace ViBro;

/// <summary>Small modal dialogs built in code (rename, name conflict, transfer progress).</summary>
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

/// <summary>F2: edits the name only; the extension is kept. Enter confirms, Esc cancels.</summary>
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
            _box.SelectAll();
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

/// <summary>Name conflict at the move destination. Overwrite is offered only as an explicit choice.</summary>
internal sealed class ConflictDialog : Window
{
    private ConflictChoice _choice = ConflictChoice.Cancel;

    private ConflictDialog(Window owner, string fileName, string destFolder, string keepBothName)
    {
        DialogStyle.Apply(this, owner, "File already exists");
        var panel = new StackPanel { Margin = new Thickness(12) };
        panel.Children.Add(new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Text = $"A file named \"{fileName}\" already exists in:\n{destFolder}\n\n" +
                   $"Keep both: the moved file is saved as \"{keepBothName}\".\n" +
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

    public static ConflictChoice Ask(Window owner, string fileName, string destFolder, string keepBothName)
    {
        var d = new ConflictDialog(owner, fileName, destFolder, keepBothName);
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

    public ProgressDialog(Window owner, string fileName, string destFolder, CancellationTokenSource cts)
    {
        DialogStyle.Apply(this, owner, "Moving file");
        _cts = cts;
        var panel = new StackPanel { Margin = new Thickness(12) };
        panel.Children.Add(new TextBlock { TextWrapping = TextWrapping.Wrap, Text = $"Moving \"{fileName}\"\nto {destFolder}" });
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
