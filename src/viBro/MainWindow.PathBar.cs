using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ViBro;

// Path bar as "breadcrumbs" (1.13.0): one button per folder of the path, a click opens it. A click on
// the empty part of the bar, or Ctrl+L, turns it into the editable path (Enter opens, Esc or leaving
// the field goes back to the breadcrumbs), as in File Explorer. Backspace (ParentFolder) opens the
// parent folder with the folder just left selected, like "..".
public partial class MainWindow
{
    /// <summary>"E:\A\B" → (E:, E:\), (A, E:\A), (B, E:\A\B); "\\srv\share\A" → (\\srv\share, …), (A, …).</summary>
    internal static List<(string Name, string Path)> PathSegments(string path)
    {
        var list = new List<(string, string)>();
        if (string.IsNullOrWhiteSpace(path)) return list;
        string? root = Path.GetPathRoot(path);
        if (string.IsNullOrEmpty(root)) return list;
        string rootPath = root.EndsWith('\\') ? root : root + "\\";
        list.Add((root.TrimEnd('\\'), rootPath));
        string current = rootPath;
        foreach (string part in path.Substring(root.Length).Split('\\', StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, part);
            list.Add((part, current));
        }
        return list;
    }

    private void UpdateBreadcrumb(string path)
    {
        CrumbPanel.Children.Clear();
        var segments = PathSegments(path);
        for (int i = 0; i < segments.Count; i++)
        {
            var (name, target) = segments[i];
            if (i > 0)
                CrumbPanel.Children.Add(new TextBlock
                {
                    Text = "›", Opacity = 0.6, Margin = new Thickness(1, 0, 1, 0), VerticalAlignment = VerticalAlignment.Center,
                });
            var button = new Button
            {
                Content = name,
                Padding = new Thickness(6, 2, 6, 2),
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Focusable = false,
                Cursor = Cursors.Hand,
                FontWeight = i == segments.Count - 1 ? FontWeights.SemiBold : FontWeights.Normal,
                ToolTip = target,
                VerticalAlignment = VerticalAlignment.Center,
            };
            button.Click += (_, _) => OpenFolder(target, revealInTree: true);
            CrumbPanel.Children.Add(button);
        }
        // The end of a long path stays visible.
        Dispatcher.BeginInvoke(() => CrumbScroll.ScrollToRightEnd(), DispatcherPriority.Loaded);
    }

    /// <summary>A click on the empty part of the bar (not on a folder button) edits the path.</summary>
    private void CrumbBar_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        for (var d = e.OriginalSource as DependencyObject; d != null && !ReferenceEquals(d, CrumbBar); d = VisualTreeHelper.GetParent(d))
            if (d is Button) return;
        EnterPathEdit();
        e.Handled = true;
    }

    private void EnterPathEdit()
    {
        PathBox.Text = _currentFolder ?? PathBox.Text;
        CrumbBar.Visibility = Visibility.Collapsed;
        PathBox.Visibility = Visibility.Visible;
        PathBox.Focus();
        PathBox.SelectAll();
    }

    private void LeavePathEdit()
    {
        if (PathBox.Visibility != Visibility.Visible) return;
        PathBox.Visibility = Visibility.Collapsed;
        CrumbBar.Visibility = Visibility.Visible;
    }

    private void PathBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        PathBox.Text = _currentFolder ?? "";
        LeavePathEdit();
    }

    /// <summary>Backspace by default (1.13.0): the parent folder, with the folder just left selected.</summary>
    private void OpenParentFolder()
    {
        if (_currentFolder == null) return;
        string? parent = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(_currentFolder));
        if (string.IsNullOrEmpty(parent))
        {
            SetStatus("This is the top folder of the drive or share.");
            return;
        }
        OpenFolder(parent, revealInTree: true, selectAfterOpen: _currentFolder);
    }
}
