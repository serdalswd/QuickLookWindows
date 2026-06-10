using System;
using System.Runtime.InteropServices;
using System.Text;

namespace QuickLookWindows;

public static class FileExplorerHelper
{
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    public static bool IsFileExplorerWindow(IntPtr hwnd)
    {
        var sb = new StringBuilder(256);
        GetClassName(hwnd, sb, 256);
        var cls = sb.ToString();
        return cls == "CabinetWClass" || cls == "ExploreWClass";
    }

    // explorerHwnd: hook callback'te yakalanan HWND (değişmeden geçirilir)
    public static string? GetSelectedFile(IntPtr explorerHwnd)
    {
        try
        {
            var shellAppType = Type.GetTypeFromProgID("Shell.Application");
            if (shellAppType == null) return null;

            dynamic shell = Activator.CreateInstance(shellAppType)!;
            dynamic windows = shell.Windows();
            long targetHwnd = explorerHwnd.ToInt64();

            for (int i = 0; i < windows.Count; i++)
            {
                try
                {
                    dynamic window = windows.Item(i);
                    long windowHwnd = Convert.ToInt64(window.HWND); // 64-bit safe
                    if (windowHwnd == targetHwnd)
                    {
                        dynamic document = window.Document;
                        dynamic selectedItems = document.SelectedItems();
                        if (selectedItems.Count > 0)
                        {
                            dynamic item = selectedItems.Item(0);
                            return item.Path as string;
                        }
                    }
                }
                catch { }
            }
        }
        catch { }
        return null;
    }
}
