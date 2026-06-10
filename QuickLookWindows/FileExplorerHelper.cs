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

    public static string? GetSelectedFile(IntPtr explorerHwnd)
    {
        try
        {
            var shellAppType = Type.GetTypeFromProgID("Shell.Application");
            if (shellAppType == null)
            {
                Logger.Log("GetSelectedFile: Shell.Application ProgID not found");
                return null;
            }

            dynamic shell = Activator.CreateInstance(shellAppType)!;
            dynamic windows = shell.Windows();
            long targetHwnd = explorerHwnd.ToInt64();
            Logger.Log($"GetSelectedFile: scanning {windows.Count} shell windows for hwnd=0x{targetHwnd:X}");

            for (int i = 0; i < windows.Count; i++)
            {
                try
                {
                    dynamic window = windows.Item(i);
                    long windowHwnd = Convert.ToInt64(window.HWND);
                    if (windowHwnd == targetHwnd)
                    {
                        dynamic document = window.Document;
                        dynamic selectedItems = document.SelectedItems();
                        Logger.Log($"GetSelectedFile: matched window, selected count={selectedItems.Count}");
                        if (selectedItems.Count > 0)
                        {
                            dynamic item = selectedItems.Item(0);
                            return item.Path as string;
                        }
                        return null;
                    }
                }
                catch (Exception ex) { Logger.Log($"GetSelectedFile: window[{i}] error: {ex.Message}"); }
            }
            Logger.Log("GetSelectedFile: no matching window found");
        }
        catch (Exception ex) { Logger.Log($"GetSelectedFile: outer error: {ex.Message}"); }
        return null;
    }
}
