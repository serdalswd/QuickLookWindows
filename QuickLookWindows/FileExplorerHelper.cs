using System;
using System.Runtime.InteropServices;
using System.Text;

namespace QuickLookWindows;

public static class FileExplorerHelper
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    public static bool IsFileExplorerForeground()
    {
        var hwnd = GetForegroundWindow();
        var className = new StringBuilder(256);
        GetClassName(hwnd, className, 256);
        return className.ToString() == "CabinetWClass";
    }

    public static string? GetSelectedFile()
    {
        try
        {
            var foregroundHwnd = GetForegroundWindow();

            var shellAppType = Type.GetTypeFromProgID("Shell.Application");
            if (shellAppType == null) return null;

            dynamic shell = Activator.CreateInstance(shellAppType)!;
            dynamic windows = shell.Windows();

            for (int i = 0; i < windows.Count; i++)
            {
                try
                {
                    dynamic window = windows.Item(i);
                    int hwnd = (int)window.HWND;

                    if ((IntPtr)hwnd == foregroundHwnd)
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
