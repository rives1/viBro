using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ViBro.Util;

namespace ViBro;

// 1.14.0: rows that leave the file list (deleted, moved, gone at F5) fade out before the list is
// rebuilt; rows that enter it (new at F5, a new extract) fade in. Only rows on screen are animated:
// the list is virtualised, the others have no row yet. Nothing waits on the video.
public partial class MainWindow
{
    /// <summary>
    /// Fades the rows of the items to transparent. Their files cannot be loaded meanwhile (they are
    /// gone or going). Returns the faded rows: the caller rebuilds the list, then calls
    /// <see cref="ClearFadedRows"/>, because the list reuses rows for other files.
    /// </summary>
    private async Task<List<UIElement>> FadeOutRowsAsync(IEnumerable<VideoItem> items)
    {
        var list = items.ToList();
        var rows = list.Select(i => FileList.ItemContainerGenerator.ContainerFromItem(i)).OfType<UIElement>().ToList();
        if (rows.Count == 0 || !Motion.Enabled) return new List<UIElement>();
        var paths = list.Select(i => i.FullPath).Where(p => !_busyPaths.Contains(p)).ToList();
        _busyPaths.UnionWith(paths);
        try
        {
            await Motion.FadeOutAsync(rows, Motion.RowFade);
        }
        finally
        {
            _busyPaths.ExceptWith(paths);
        }
        return rows;
    }

    private static void ClearFadedRows(List<UIElement> rows)
    {
        foreach (var r in rows) Motion.Clear(r);
    }

    /// <summary>Rows of new items that are on screen fade in (the list's layout is brought up to date first).</summary>
    private void FadeInRows(IEnumerable<VideoItem> items)
    {
        if (!Motion.Enabled) return;
        FileList.UpdateLayout(); // the rows exist now, before the next frame is drawn
        foreach (var i in items)
            if (FileList.ItemContainerGenerator.ContainerFromItem(i) is UIElement row) Motion.FadeIn(row, Motion.RowFade);
    }
}
