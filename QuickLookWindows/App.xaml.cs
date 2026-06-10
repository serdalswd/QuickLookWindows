using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace QuickLookWindows;

public partial class App : Application
{
    private KeyboardHook? _hook;
    private NotifyIcon? _trayIcon;
    private PreviewWindow? _previewWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        SetupTrayIcon();
        SetupKeyboardHook();
        new ToastWindow().Show();
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "Quick Look for Windows",
            Visible = true
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Hakkında", null, (_, _) => ShowAbout());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Çıkış", null, (_, _) => Shutdown());
        _trayIcon.ContextMenuStrip = menu;

        _trayIcon.BalloonTipTitle = "Quick Look for Windows";
        _trayIcon.BalloonTipText = "Çalışıyor! File Explorer'da bir dosya seçip SPACE'e bas.";
        _trayIcon.ShowBalloonTip(3000);
    }

    private void SetupKeyboardHook()
    {
        _hook = new KeyboardHook();
        // BeginInvoke: hook callback'ten UI thread'e güvenli geçiş
        _hook.SpaceInExplorer += hwnd => Dispatcher.BeginInvoke(() => OnSpaceInExplorer(hwnd));
    }

    private void OnSpaceInExplorer(IntPtr explorerHwnd)
    {
        if (_previewWindow != null)
        {
            _previewWindow.Close();
            _previewWindow = null;
            return;
        }

        string? filePath = FileExplorerHelper.GetSelectedFile(explorerHwnd);
        if (filePath == null) return;
        // Hem dosya hem klasör desteklenir
        if (!File.Exists(filePath) && !Directory.Exists(filePath)) return;

        _previewWindow = new PreviewWindow(filePath);
        _previewWindow.Closed += (_, _) => _previewWindow = null;
        _previewWindow.Show();
    }

    private void ShowAbout()
    {
        System.Windows.MessageBox.Show(
            "Quick Look for Windows\n\nFile Explorer'da bir dosya seçin ve SPACE tuşuna basın.\n\nDesteklenen: Resim, Video, Ses, Kod, Metin dosyaları",
            "Quick Look for Windows",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hook?.Dispose();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
