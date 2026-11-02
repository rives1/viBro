using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ViBro.Util;

namespace ViBro.Tree;

internal enum FolderNodeKind { UserFolder, Drive, NetworkDrive, RemovableDrive, OpticalDrive, NetworkShare, Folder, Placeholder }

/// <summary>
/// One node of the folder tree. Children are listed on a worker thread the first time the
/// node is expanded (never at startup, never recursively), so unreachable drives and slow
/// shares cannot block the UI. Results are applied on the thread that requested them.
/// </summary>
internal sealed class FolderNode : INotifyPropertyChanged
{
    private static readonly EnumerationOptions DirOptions = new()
    {
        RecurseSubdirectories = false,
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.Hidden | FileAttributes.System,
    };

    private string _name;
    private bool _isExpanded;
    private bool _isSelected;
    private Task? _loadTask;

    public FolderNode(string name, string path, FolderNodeKind kind)
    {
        _name = name;
        Path = path;
        Kind = kind;
        if (kind != FolderNodeKind.Placeholder)
            Children.Add(NewPlaceholder()); // shows the expander until the real content is known
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name
    {
        get => _name;
        set { if (_name != value) { _name = value; OnChanged(); } }
    }

    public string Path { get; }
    public FolderNodeKind Kind { get; }
    public ObservableCollection<FolderNode> Children { get; } = new();
    public bool IsPlaceholder => Kind == FolderNodeKind.Placeholder;
    public bool IsLoaded { get; private set; }

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            OnChanged();
            if (value) _ = LoadChildrenAsync();
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set { if (_isSelected != value) { _isSelected = value; OnChanged(); } }
    }

    /// <summary>Lists subfolders once; concurrent callers share the same task. A failure can be retried.</summary>
    public Task LoadChildrenAsync()
    {
        if (IsPlaceholder) return Task.CompletedTask;
        return _loadTask ??= LoadCoreAsync();
    }

    private async Task LoadCoreAsync()
    {
        SetPlaceholderText("Loading...");
        List<string> names;
        try
        {
            string path = Path;
            names = await Task.Run(() => new DirectoryInfo(path)
                .EnumerateDirectories("*", DirOptions)
                .Select(d => d.Name)
                .OrderBy(n => n, NaturalComparer.Instance)
                .ToList());
        }
        catch (Exception ex)
        {
            SetPlaceholderText("(unavailable: " + ex.Message + ")");
            _loadTask = null; // allow a retry on the next expand
            _isExpanded = false;
            OnChanged(nameof(IsExpanded));
            return;
        }

        Children.Clear();
        foreach (var n in names)
            Children.Add(new FolderNode(n, System.IO.Path.Combine(Path, n), FolderNodeKind.Folder));
        IsLoaded = true;
    }

    /// <summary>Finds an already listed child by folder name (case-insensitive).</summary>
    public FolderNode? FindChild(string name)
        => Children.FirstOrDefault(c => !c.IsPlaceholder && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

    private void SetPlaceholderText(string text)
    {
        if (Children.Count == 1 && Children[0].IsPlaceholder) Children[0].Name = text;
        else
        {
            Children.Clear();
            Children.Add(new FolderNode(text, "", FolderNodeKind.Placeholder));
        }
    }

    private static FolderNode NewPlaceholder() => new("", "", FolderNodeKind.Placeholder);

    private void OnChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public override string ToString() => Path;
}
