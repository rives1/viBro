using System;
using System.Collections.Generic;

namespace ViBro.Util;

/// <summary>
/// Natural ("file 2" before "file 10") case-insensitive order. Uses StrCmpLogicalW, the
/// function behind Windows Explorer's order; falls back to a managed implementation
/// when shlwapi is not available (tests on other platforms).
/// </summary>
internal sealed class NaturalComparer : IComparer<string>
{
    public static readonly NaturalComparer Instance = new();
    private static bool _useNative = OperatingSystem.IsWindows();

    public int Compare(string? a, string? b)
    {
        if (ReferenceEquals(a, b)) return 0;
        if (a == null) return -1;
        if (b == null) return 1;

        if (_useNative)
        {
            try { return NativeMethods.StrCmpLogicalW(a, b); }
            catch (DllNotFoundException) { _useNative = false; }
            catch (EntryPointNotFoundException) { _useNative = false; }
        }
        return ManagedCompare(a, b);
    }

    internal static int ManagedCompare(string a, string b)
    {
        int i = 0, j = 0;
        while (i < a.Length && j < b.Length)
        {
            if (char.IsDigit(a[i]) && char.IsDigit(b[j]))
            {
                int si = i, sj = j;
                while (si < a.Length && a[si] == '0') si++;
                while (sj < b.Length && b[sj] == '0') sj++;
                int ei = si, ej = sj;
                while (ei < a.Length && char.IsDigit(a[ei])) ei++;
                while (ej < b.Length && char.IsDigit(b[ej])) ej++;
                int lenA = ei - si, lenB = ej - sj;
                if (lenA != lenB) return lenA < lenB ? -1 : 1;
                int c = string.CompareOrdinal(a, si, b, sj, lenA);
                if (c != 0) return c < 0 ? -1 : 1;
                i = ei;
                j = ej;
            }
            else
            {
                int c = string.Compare(a, i, b, j, 1, StringComparison.CurrentCultureIgnoreCase);
                if (c != 0) return c < 0 ? -1 : 1;
                i++;
                j++;
            }
        }
        return (a.Length - i).CompareTo(b.Length - j);
    }
}
