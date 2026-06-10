using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using WpfApp = System.Windows.Application;
using WpfMsgBox = System.Windows.MessageBox;

namespace QuickLookSetup;

public partial class MainWindow : Window
{
    private string _installPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "QuickLookWindows");

    public MainWindow()
    {
        InitializeComponent();
        TxtPath.Text = _installPath;
    }

    private void BtnNext_Click(object sender, RoutedEventArgs e)
    {
        PageWelcome.Visibility = Visibility.Collapsed;
        PageInstall.Visibility = Visibility.Visible;
    }

    private void BtnBack_Click(object sender, RoutedEventArgs e)
    {
        PageInstall.Visibility = Visibility.Collapsed;
        PageWelcome.Visibility = Visibility.Visible;
    }

    private void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "QuickLook for Windows kurulum klasörünü seçin",
            SelectedPath = TxtPath.Text,
            ShowNewFolderButton = true
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            TxtPath.Text = dialog.SelectedPath;
    }

    private void BtnInstall_Click(object sender, RoutedEventArgs e)
    {
        _installPath = TxtPath.Text.Trim();
        if (string.IsNullOrWhiteSpace(_installPath))
        {
            WpfMsgBox.Show("Lütfen bir kurulum klasörü seçin.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        PageInstall.Visibility = Visibility.Collapsed;
        PageDone.Visibility = Visibility.Visible;

        try
        {
            Install();
            TxtDoneMsg.Text = $"Uygulama başarıyla kuruldu.\n\nKurulum yeri:\n{_installPath}";
        }
        catch (Exception ex)
        {
            TxtDoneMsg.Text = $"Kurulum sırasında hata oluştu:\n{ex.Message}";
        }
    }

    private void Install()
    {
        Directory.CreateDirectory(_installPath);
        string exePath = Path.Combine(_installPath, "QuickLookWindows.exe");

        // Embedded EXE'yi dışarı çıkar
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("QuickLookWindows.exe")
            ?? throw new Exception("Gömülü EXE bulunamadı.");
        using var fs = File.Create(exePath);
        stream.CopyTo(fs);

        // Start Menu kısayolu
        string startMenuDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
        CreateShortcut(Path.Combine(startMenuDir, "QuickLook for Windows.lnk"), exePath);

        // Masaüstü kısayolu
        if (ChkDesktop.IsChecked == true)
            CreateShortcut(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    "QuickLook for Windows.lnk"),
                exePath);

        // Windows başlangıcı
        if (ChkStartup.IsChecked == true)
            CreateShortcut(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                    "QuickLook for Windows.lnk"),
                exePath);

        // Program Ekle/Kaldır kaydı
        using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Uninstall\QuickLookWindows");
        key.SetValue("DisplayName", "QuickLook for Windows");
        key.SetValue("DisplayVersion", "1.0.0");
        key.SetValue("Publisher", "serdalswd");
        key.SetValue("InstallLocation", _installPath);
        key.SetValue("UninstallString",
            $"cmd /c rmdir /s /q \"{_installPath}\" && " +
            @"reg delete HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\QuickLookWindows /f");
    }

    private static void CreateShortcut(string lnkPath, string targetPath)
    {
        // PowerShell üzerinden WScript.Shell COM kullan (ek bağımlılık yok)
        string script = $"""
            $s = (New-Object -ComObject WScript.Shell).CreateShortcut('{lnkPath}');
            $s.TargetPath = '{targetPath}';
            $s.Description = 'Mac Quick Look for Windows';
            $s.Save()
            """;
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script.Replace("\"", "\\\"")}\"",
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true
        };
        Process.Start(psi)?.WaitForExit();
    }

    private void BtnLaunch_Click(object sender, RoutedEventArgs e)
    {
        string exe = Path.Combine(_installPath, "QuickLookWindows.exe");
        if (File.Exists(exe))
            Process.Start(exe);
        WpfApp.Current.Shutdown();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) =>
        WpfApp.Current.Shutdown();
}
