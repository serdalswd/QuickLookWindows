using System;
using System.Runtime.InteropServices;

namespace QuickLookWindows;

public class KeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int VK_SPACE = 0x20;

    private IntPtr _hookId = IntPtr.Zero;
    private readonly LowLevelKeyboardProc _proc;

    public event Action<IntPtr>? SpaceInExplorer;
    public bool IsInstalled => _hookId != IntPtr.Zero;

    public KeyboardHook()
    {
        _proc = HookCallback;
        // WH_KEYBOARD_LL is system-wide and does not require DLL injection.
        // IntPtr.Zero is the correct hMod per MSDN — using GetModuleHandle
        // breaks for single-file published EXEs because the module name is a
        // temp-extracted path that the kernel doesn't recognise.
        _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, IntPtr.Zero, 0);
        Logger.Log($"KeyboardHook install: {(_hookId != IntPtr.Zero ? "OK" : $"FAILED (error {Marshal.GetLastWin32Error()})")}");
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            if (vkCode == VK_SPACE)
            {
                IntPtr hwnd = GetForegroundWindow();
                bool isExplorer = FileExplorerHelper.IsFileExplorerWindow(hwnd);
                Logger.Log($"SPACE pressed — hwnd=0x{hwnd:X} explorer={isExplorer}");
                if (isExplorer)
                {
                    SpaceInExplorer?.Invoke(hwnd);
                    return (IntPtr)1;
                }
            }
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
}
