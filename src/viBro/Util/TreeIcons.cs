using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ViBro.Tree;

namespace ViBro.Util;

/// <summary>
/// Colour vector icons (16×16 design grid) for the folder tree. Drawn in code, frozen and shared:
/// no file or shell access per node, sharp at every DPI, the same in Light and Dark.
/// </summary>
internal static class TreeIcons
{
    public static readonly ImageSource Folder = Make(FolderBack(), Fill(FolderClosedFront, "#FFCA4B"));
    public static readonly ImageSource FolderOpen = Make(FolderBack(), Fill(FolderOpenFront, "#FFD966"));

    public static readonly ImageSource Desktop = Make(
        Fill("M1.5,2.5 L14.5,2.5 L14.5,11.5 L1.5,11.5 Z", "#1F6FC5"),
        Fill("M2.5,3.5 L13.5,3.5 L13.5,10.5 L2.5,10.5 Z", "#8CC8FF"),
        Fill("M6.5,11.5 L9.5,11.5 L10,13.5 L6,13.5 Z", "#6B7280"),
        Fill("M4.5,13.5 L11.5,13.5 L11.5,14.5 L4.5,14.5 Z", "#4B5563"));

    public static readonly ImageSource Documents = Make(
        Draw("M3,1.5 L9.5,1.5 L13,5 L13,14.5 L3,14.5 Z", "#FFFFFF", "#2B79D0", 1),
        Draw("M9.5,1.5 L9.5,5 L13,5", null, "#2B79D0", 1),
        Draw("M5,7.5 L11,7.5 M5,9.5 L11,9.5 M5,11.5 L9,11.5", null, "#2B79D0", 1));

    public static readonly ImageSource Downloads = Make(
        new GeometryDrawing(SolidBrush("#1E88E5"), null, new EllipseGeometry(new System.Windows.Point(8, 8), 7, 7)),
        Draw("M8,3.8 L8,11 M4.9,8.2 L8,11.3 L11.1,8.2", null, "#FFFFFF", 1.7));

    public static readonly ImageSource Videos = Make(
        Fill("M1.5,3 L14.5,3 L14.5,13 L1.5,13 Z", "#7E57C2"),
        Fill("M2.5,4 L3.5,4 L3.5,5 L2.5,5 Z M2.5,7.5 L3.5,7.5 L3.5,8.5 L2.5,8.5 Z M2.5,11 L3.5,11 L3.5,12 L2.5,12 Z " +
             "M12.5,4 L13.5,4 L13.5,5 L12.5,5 Z M12.5,7.5 L13.5,7.5 L13.5,8.5 L12.5,8.5 Z M12.5,11 L13.5,11 L13.5,12 L12.5,12 Z", "#D1C4E9"),
        Fill("M6.5,5.5 L10.8,8 L6.5,10.5 Z", "#FFFFFF"));

    public static readonly ImageSource Drive = Make(DriveBody(4.5, 11.5), DriveLed(4.5, 11.5));

    public static readonly ImageSource NetworkDrive = Make(
        DriveBody(2, 9), DriveLed(2, 9),
        Draw("M8,9 L8,13.5 M2.5,13.5 L13.5,13.5", null, "#2E9E4F", 1.6),
        Fill("M6.5,12.5 L9.5,12.5 L9.5,14.5 L6.5,14.5 Z", "#2E9E4F"));

    public static readonly ImageSource RemovableDrive = Make(
        Fill("M6,1.5 L10,1.5 L10,5 L6,5 Z", "#C3C8D0"),
        Fill("M7,2.5 L7.8,2.5 L7.8,3.5 L7,3.5 Z M8.2,2.5 L9,2.5 L9,3.5 L8.2,3.5 Z", "#4B5563"),
        Fill("M4.5,5 L11.5,5 L11.5,13.5 Q11.5,14.5 10.5,14.5 L5.5,14.5 Q4.5,14.5 4.5,13.5 Z", "#2B79D0"),
        Fill("M6.5,7 L9.5,7 L9.5,8 L6.5,8 Z", "#8CC8FF"));

    public static readonly ImageSource OpticalDrive = Make(
        new GeometryDrawing(SolidBrush("#B8C4D6"), MakePen("#7A8494", 1), Ring(8, 8, 6.5, 1.3)),
        new GeometryDrawing(SolidBrush("#E8EDF3"), null, Ring(8, 8, 2.6, 1.3)),
        Draw("M8,3.2 A4.8,4.8 0 0 1 12.8,8", null, "#FFFFFF", 1));

    public static readonly ImageSource NetworkShare = Make(
        FolderBack(), Fill(FolderClosedFront, "#FFCA4B"),
        new GeometryDrawing(SolidBrush("#2E9E4F"), MakePen("#FFFFFF", 1), new EllipseGeometry(new System.Windows.Point(12, 12), 3.5, 3.5)),
        Draw("M12,9.9 L12,12.6 M10,13 L14,13", null, "#FFFFFF", 1.2));

    private const string FolderClosedFront =
        "M1,6 L15,6 L15,13 Q15,14 14,14 L2,14 Q1,14 1,13 Z";
    private const string FolderOpenFront =
        "M3.2,7 L15,7 Q15.6,7 15.4,7.6 L13.9,13.3 Q13.7,14 13,14 L1.6,14 Q0.9,14 1.1,13.3 L2.4,7.6 Q2.6,7 3.2,7 Z";

    /// <summary>Icon for a tree node; null for the "loading" placeholder.</summary>
    public static ImageSource? For(FolderNodeKind kind, string? name, bool expanded) => kind switch
    {
        FolderNodeKind.Placeholder => null,
        FolderNodeKind.UserFolder => name switch
        {
            "Desktop" => Desktop,
            "Documents" => Documents,
            "Downloads" => Downloads,
            "Videos" => Videos,
            _ => expanded ? FolderOpen : Folder,
        },
        FolderNodeKind.Drive => Drive,
        FolderNodeKind.NetworkDrive => NetworkDrive,
        FolderNodeKind.RemovableDrive => RemovableDrive,
        FolderNodeKind.OpticalDrive => OpticalDrive,
        FolderNodeKind.NetworkShare => NetworkShare,
        _ => expanded ? FolderOpen : Folder,
    };

    // ---------------------------------------------------------------- helpers

    private static GeometryDrawing FolderBack()
        => Fill("M1,3.5 Q1,2.5 2,2.5 L6,2.5 L7.5,4 L14,4 Q15,4 15,5 L15,13 Q15,14 14,14 L2,14 Q1,14 1,13 Z", "#E0A526");

    private static GeometryDrawing DriveBody(double top, double bottom)
        => new(new LinearGradientBrush(Hex("#A9AFB8"), Hex("#7C838E"), 90).FreezeAnd(), MakePen("#5F6672", 1),
               new RectangleGeometry(new System.Windows.Rect(1.5, top, 13, bottom - top), 1.5, 1.5));

    private static GeometryDrawing DriveLed(double top, double bottom)
        => new(SolidBrush("#3DDC84"), null, new EllipseGeometry(new System.Windows.Point(12, (top + bottom) / 2), 1, 1));

    private static Geometry Ring(double cx, double cy, double outer, double inner)
    {
        var g = new GeometryGroup { FillRule = FillRule.EvenOdd };
        g.Children.Add(new EllipseGeometry(new System.Windows.Point(cx, cy), outer, outer));
        g.Children.Add(new EllipseGeometry(new System.Windows.Point(cx, cy), inner, inner));
        return g;
    }

    private static GeometryDrawing Fill(string path, string color) => new(SolidBrush(color), null, Geometry.Parse(path));

    private static GeometryDrawing Draw(string path, string? fill, string stroke, double thickness)
        => new(fill == null ? null : SolidBrush(fill), MakePen(stroke, thickness), Geometry.Parse(path));

    private static Color Hex(string hex) => (Color)ColorConverter.ConvertFromString(hex);

    private static SolidColorBrush SolidBrush(string hex) => new SolidColorBrush(Hex(hex)).FreezeAnd();

    private static Pen MakePen(string hex, double thickness)
        => new Pen(SolidBrush(hex), thickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round }.FreezeAnd();

    /// <summary>Fixed 16×16 viewport, so every icon keeps its position and size whatever its bounds.</summary>
    private static ImageSource Make(params Drawing[] parts)
    {
        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new System.Windows.Rect(0, 0, 16, 16))));
        foreach (var p in parts) group.Children.Add(p);
        var image = new DrawingImage(group);
        image.Freeze();
        return image;
    }

    private static T FreezeAnd<T>(this T f) where T : System.Windows.Freezable
    {
        f.Freeze();
        return f;
    }
}

/// <summary>Kind, Name, IsExpanded → icon. Used by the tree's item template.</summary>
internal sealed class TreeIconConverter : IMultiValueConverter
{
    private static bool _logged;

    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 3 || values[0] is not FolderNodeKind kind) return null;
        try
        {
            return TreeIcons.For(kind, values[1] as string, values[2] is true);
        }
        catch (Exception ex) // never let an icon problem break the tree: show the node without it
        {
            if (!_logged)
            {
                _logged = true;
                ErrorLog.Write("ui", "Tree icons unavailable: " + ex);
            }
            return null;
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
