using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

// Explicit aliases to avoid WinForms vs WPF name clashes
using WpfColor = System.Windows.Media.Color;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfImage = System.Windows.Controls.Image;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfHAlign = System.Windows.HorizontalAlignment;
using WpfVAlign = System.Windows.VerticalAlignment;

namespace QuickLookWindows;

public partial class PreviewWindow : Window
{
    private readonly string _filePath;
    private MediaElement? _media;

    public PreviewWindow(string filePath)
    {
        InitializeComponent();
        _filePath = filePath;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var name = Path.GetFileName(_filePath);
        TitleText.Text = string.IsNullOrEmpty(name) ? _filePath : name;

        if (Directory.Exists(_filePath))
        {
            FileTypeLabel.Text = "Klasör";
            FileSizeLabel.Text = "";
            SeparatorDot.Visibility = Visibility.Collapsed;
        }
        else
        {
            var info = new FileInfo(_filePath);
            FileSizeLabel.Text = FormatSize(info.Length);
        }
        LoadPreview();
    }

    private void LoadPreview()
    {
        // Klasör kontrolü
        if (Directory.Exists(_filePath))
        {
            ShowFolder();
            return;
        }

        var ext = Path.GetExtension(_filePath).ToLowerInvariant();
        switch (GetCategory(ext))
        {
            case FileCategory.Image:  ShowImage(); break;
            case FileCategory.Text:   ShowText(ext); break;
            case FileCategory.Video:  ShowVideo(ext); break;
            case FileCategory.Audio:  ShowAudio(ext); break;
            default:                  ShowUnsupported(ext); break;
        }
    }

    private void ShowFolder()
    {
        FileTypeLabel.Text = "Klasör";
        LoadingText.Visibility = Visibility.Collapsed;

        string[] entries;
        try { entries = Directory.GetFileSystemEntries(_filePath); }
        catch { entries = []; }

        var panel = new StackPanel
        {
            VerticalAlignment = WpfVAlign.Center,
            HorizontalAlignment = WpfHAlign.Center,
            Margin = new Thickness(32)
        };
        panel.Children.Add(new TextBlock
        {
            Text = "📁",
            FontSize = 72,
            HorizontalAlignment = WpfHAlign.Center,
            Margin = new Thickness(0, 0, 0, 16)
        });
        panel.Children.Add(new TextBlock
        {
            Text = Path.GetFileName(_filePath),
            Foreground = new SolidColorBrush(WpfColor.FromRgb(0xEB, 0xEB, 0xF5)),
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = WpfHAlign.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 500,
            Margin = new Thickness(0, 0, 0, 8)
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"{entries.Length} öğe",
            Foreground = new SolidColorBrush(WpfColor.FromRgb(0x8E, 0x8E, 0x93)),
            FontSize = 14,
            HorizontalAlignment = WpfHAlign.Center,
            Margin = new Thickness(0, 0, 0, 20)
        });

        // İlk 6 öğeyi listele
        var listPanel = new StackPanel { HorizontalAlignment = WpfHAlign.Center };
        foreach (var entry in entries.Take(6))
        {
            var isDir = Directory.Exists(entry);
            listPanel.Children.Add(new TextBlock
            {
                Text = $"{(isDir ? "📁" : "📄")} {Path.GetFileName(entry)}",
                Foreground = new SolidColorBrush(WpfColor.FromRgb(0x6E, 0x6E, 0x8A)),
                FontSize = 13,
                Margin = new Thickness(0, 2, 0, 2)
            });
        }
        if (entries.Length > 6)
            listPanel.Children.Add(new TextBlock
            {
                Text = $"... ve {entries.Length - 6} öğe daha",
                Foreground = new SolidColorBrush(WpfColor.FromRgb(0x4E, 0x4E, 0x6A)),
                FontSize = 12,
                Margin = new Thickness(0, 6, 0, 0)
            });

        panel.Children.Add(listPanel);
        ContentGrid.Children.Clear();
        ContentGrid.Children.Add(panel);
    }

    private void ShowImage()
    {
        FileTypeLabel.Text = "Görüntü";
        LoadingText.Visibility = Visibility.Collapsed;
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(_filePath);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();

            var img = new WpfImage
            {
                Source = bmp,
                Stretch = Stretch.Uniform,
                StretchDirection = StretchDirection.DownOnly,
                Margin = new Thickness(12)
            };
            ContentGrid.Children.Clear();
            ContentGrid.Children.Add(img);
        }
        catch { ShowError("Görüntü yüklenemedi."); }
    }

    private void ShowText(string ext)
    {
        FileTypeLabel.Text = ext.TrimStart('.').ToUpperInvariant() + " Dosyası";
        LoadingText.Visibility = Visibility.Collapsed;
        try
        {
            var content = File.ReadAllText(_filePath, Encoding.UTF8);
            var tb = new WpfTextBox
            {
                Text = content,
                IsReadOnly = true,
                FontFamily = new WpfFontFamily("Cascadia Code, Consolas, Courier New"),
                FontSize = 13,
                Background = new SolidColorBrush(WpfColor.FromRgb(0x1C, 0x1C, 0x1E)),
                Foreground = new SolidColorBrush(WpfColor.FromRgb(0xEB, 0xEB, 0xF5)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(16),
                AcceptsReturn = true,
                TextWrapping = TextWrapping.NoWrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            ContentGrid.Children.Clear();
            ContentGrid.Children.Add(tb);
        }
        catch { ShowError("Dosya okunamadı."); }
    }

    private void ShowVideo(string ext)
    {
        FileTypeLabel.Text = ext.TrimStart('.').ToUpperInvariant() + " Video";
        LoadingText.Visibility = Visibility.Collapsed;
        _media = new MediaElement
        {
            Source = new Uri(_filePath),
            LoadedBehavior = MediaState.Play,
            UnloadedBehavior = MediaState.Close,
            Stretch = Stretch.Uniform,
            Volume = 0.7
        };
        ContentGrid.Children.Clear();
        ContentGrid.Children.Add(_media);
    }

    private void ShowAudio(string ext)
    {
        FileTypeLabel.Text = ext.TrimStart('.').ToUpperInvariant() + " Ses";
        LoadingText.Visibility = Visibility.Collapsed;

        _media = new MediaElement
        {
            Source = new Uri(_filePath),
            LoadedBehavior = MediaState.Play,
            UnloadedBehavior = MediaState.Close,
            Volume = 0.8,
            Width = 0, Height = 0
        };

        var panel = new StackPanel
        {
            VerticalAlignment = WpfVAlign.Center,
            HorizontalAlignment = WpfHAlign.Center,
            Margin = new Thickness(32)
        };
        panel.Children.Add(new TextBlock
        {
            Text = "🎵",
            FontSize = 72,
            HorizontalAlignment = WpfHAlign.Center,
            Margin = new Thickness(0, 0, 0, 20)
        });
        panel.Children.Add(new TextBlock
        {
            Text = Path.GetFileName(_filePath),
            Foreground = new SolidColorBrush(WpfColor.FromRgb(0xEB, 0xEB, 0xF5)),
            FontSize = 16,
            FontWeight = FontWeights.Medium,
            HorizontalAlignment = WpfHAlign.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 500
        });
        panel.Children.Add(new TextBlock
        {
            Text = "Çalınıyor...",
            Foreground = new SolidColorBrush(WpfColor.FromRgb(0x8E, 0x8E, 0x93)),
            FontSize = 13,
            HorizontalAlignment = WpfHAlign.Center,
            Margin = new Thickness(0, 8, 0, 0)
        });
        panel.Children.Add(_media);

        ContentGrid.Children.Clear();
        ContentGrid.Children.Add(panel);
    }

    private void ShowUnsupported(string ext)
    {
        FileTypeLabel.Text = string.IsNullOrEmpty(ext)
            ? "Dosya"
            : ext.TrimStart('.').ToUpperInvariant() + " Dosyası";
        LoadingText.Visibility = Visibility.Collapsed;

        var panel = new StackPanel
        {
            VerticalAlignment = WpfVAlign.Center,
            HorizontalAlignment = WpfHAlign.Center
        };
        panel.Children.Add(new TextBlock
        {
            Text = "📄",
            FontSize = 80,
            HorizontalAlignment = WpfHAlign.Center,
            Margin = new Thickness(0, 0, 0, 16)
        });
        panel.Children.Add(new TextBlock
        {
            Text = "Bu dosya türü önizlenemiyor",
            Foreground = new SolidColorBrush(WpfColor.FromRgb(0x8E, 0x8E, 0x93)),
            FontSize = 16,
            HorizontalAlignment = WpfHAlign.Center,
            Margin = new Thickness(0, 0, 0, 8)
        });
        panel.Children.Add(new TextBlock
        {
            Text = Path.GetFileName(_filePath),
            Foreground = new SolidColorBrush(WpfColor.FromRgb(0xEB, 0xEB, 0xF5)),
            FontSize = 14,
            HorizontalAlignment = WpfHAlign.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 500
        });

        ContentGrid.Children.Clear();
        ContentGrid.Children.Add(panel);
    }

    private void ShowError(string msg)
    {
        ContentGrid.Children.Clear();
        ContentGrid.Children.Add(new TextBlock
        {
            Text = msg,
            Foreground = new SolidColorBrush(WpfColor.FromRgb(0xFF, 0x5F, 0x57)),
            FontSize = 16,
            HorizontalAlignment = WpfHAlign.Center,
            VerticalAlignment = WpfVAlign.Center
        });
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape || e.Key == Key.Space)
        {
            e.Handled = true;
            ClosePreview();
        }
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(CardBorder);
        if (pos.X < 0 || pos.Y < 0 ||
            pos.X > CardBorder.ActualWidth || pos.Y > CardBorder.ActualHeight)
        {
            ClosePreview();
        }
    }

    private void ClosePreview()
    {
        _media?.Stop();
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _media?.Stop();
        base.OnClosed(e);
    }

    private static FileCategory GetCategory(string ext) => ext switch
    {
        ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".tiff" or ".tif"
            or ".webp" or ".ico" => FileCategory.Image,

        ".txt" or ".md" or ".cs" or ".py" or ".js" or ".ts" or ".html" or ".htm"
            or ".css" or ".xml" or ".json" or ".yaml" or ".yml" or ".ini" or ".cfg"
            or ".log" or ".bat" or ".cmd" or ".ps1" or ".sh" or ".cpp" or ".c"
            or ".h" or ".java" or ".kt" or ".rs" or ".go" or ".rb" or ".php"
            or ".sql" or ".toml" or ".env" => FileCategory.Text,

        ".mp4" or ".avi" or ".mkv" or ".mov" or ".wmv" or ".flv" or ".webm" => FileCategory.Video,
        ".mp3" or ".wav" or ".flac" or ".aac" or ".ogg" or ".wma" or ".m4a" => FileCategory.Audio,
        _ => FileCategory.Unknown
    };

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F1} GB"
    };

    private enum FileCategory { Image, Text, Video, Audio, Unknown }
}
