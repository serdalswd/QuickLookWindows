using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using WpfColor = System.Windows.Media.Color;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfImage = System.Windows.Controls.Image;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfHAlign = System.Windows.HorizontalAlignment;
using WpfVAlign = System.Windows.VerticalAlignment;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace QuickLookWindows;

public partial class PreviewWindow : Window
{
    private string _filePath;
    private MediaElement? _media;
    private ScaleTransform? _imageScale;
    private bool _isCardExpanded;
    private string[] _siblingFiles = [];
    private int _currentIndex = -1;

    public PreviewWindow(string filePath)
    {
        InitializeComponent();
        _filePath = filePath;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        LoadSiblingFiles();
        RefreshHeader();
        LoadPreview();
    }

    // ─── Header / navigation ────────────────────────────────────────────────

    private void RefreshHeader()
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
            SeparatorDot.Visibility = Visibility.Visible;
            try { FileSizeLabel.Text = FormatSize(new FileInfo(_filePath).Length); }
            catch { FileSizeLabel.Text = ""; }
        }

        BtnPrev.Visibility = _currentIndex > 0 ? Visibility.Visible : Visibility.Collapsed;
        BtnNext.Visibility = (_currentIndex >= 0 && _currentIndex < _siblingFiles.Length - 1)
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private void LoadSiblingFiles()
    {
        if (Directory.Exists(_filePath)) return;
        var dir = Path.GetDirectoryName(_filePath);
        if (dir == null) return;
        try
        {
            _siblingFiles = Directory.GetFiles(dir)
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            _currentIndex = Array.IndexOf(_siblingFiles, _filePath);
        }
        catch { }
    }

    private void NavigateSibling(int dir)
    {
        if (_siblingFiles.Length == 0 || _currentIndex < 0) return;
        int next = _currentIndex + dir;
        if (next < 0 || next >= _siblingFiles.Length) return;

        _currentIndex = next;
        _filePath = _siblingFiles[_currentIndex];
        _media?.Stop();
        _media = null;
        _imageScale = null;

        ContentGrid.Children.Clear();
        RefreshHeader();
        LoadPreview();
    }

    // ─── Traffic light button handlers ──────────────────────────────────────

    private void TrafficLights_MouseEnter(object sender, WpfMouseEventArgs e)
    {
        CloseSymbol.Opacity = 1;
        OpenSymbol.Opacity = 1;
        ExpandSymbol.Opacity = 1;
    }

    private void TrafficLights_MouseLeave(object sender, WpfMouseEventArgs e)
    {
        CloseSymbol.Opacity = 0;
        OpenSymbol.Opacity = 0;
        ExpandSymbol.Opacity = 0;
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => ClosePreview();

    private void BtnOpenFile_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _filePath,
                UseShellExecute = true
            });
        }
        catch { }
        ClosePreview();
    }

    private void BtnExpand_Click(object sender, RoutedEventArgs e)
    {
        _isCardExpanded = !_isCardExpanded;
        if (_isCardExpanded)
        {
            var wa = SystemParameters.WorkArea;
            CardBorder.Width = wa.Width * 0.93;
            CardBorder.Height = wa.Height * 0.93;
            ExpandSymbol.Text = "⊠";
        }
        else
        {
            CardBorder.Width = 900;
            CardBorder.Height = 640;
            ExpandSymbol.Text = "⤢";
        }
    }

    private void BtnPrev_Click(object sender, RoutedEventArgs e) => NavigateSibling(-1);
    private void BtnNext_Click(object sender, RoutedEventArgs e) => NavigateSibling(1);

    // ─── Preview routing ────────────────────────────────────────────────────

    private void LoadPreview()
    {
        if (Directory.Exists(_filePath)) { ShowFolder(); return; }

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

    // ─── Folder preview ─────────────────────────────────────────────────────

    private void ShowFolder()
    {
        FileTypeLabel.Text = "Klasör";
        LoadingText.Visibility = Visibility.Collapsed;

        string[] entries;
        try
        {
            entries = Directory.GetFileSystemEntries(_filePath)
                .OrderBy(e => !Directory.Exists(e))
                .ThenBy(e => Path.GetFileName(e), StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch { entries = []; }

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Background = new SolidColorBrush(WpfColor.FromRgb(0x1C, 0x1C, 0x1E))
        };

        var panel = new StackPanel { Margin = new Thickness(28, 20, 28, 20) };

        // ── Folder header ──
        var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 20) };
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var folderIcon = new TextBlock
        {
            Text = "📁", FontSize = 44,
            VerticalAlignment = WpfVAlign.Center,
            Margin = new Thickness(0, 0, 14, 0)
        };
        Grid.SetColumn(folderIcon, 0);
        headerGrid.Children.Add(folderIcon);

        var headerInfo = new StackPanel { VerticalAlignment = WpfVAlign.Center };
        headerInfo.Children.Add(new TextBlock
        {
            Text = Path.GetFileName(_filePath),
            Foreground = new SolidColorBrush(WpfColor.FromRgb(0xEB, 0xEB, 0xF5)),
            FontSize = 18, FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        headerInfo.Children.Add(new TextBlock
        {
            Text = $"{entries.Length} öğe",
            Foreground = new SolidColorBrush(WpfColor.FromRgb(0x8E, 0x8E, 0x93)),
            FontSize = 13, Margin = new Thickness(0, 4, 0, 0)
        });
        Grid.SetColumn(headerInfo, 1);
        headerGrid.Children.Add(headerInfo);
        panel.Children.Add(headerGrid);

        // ── Item list ──
        const int maxDisplay = 100;
        int imageCount = 0;
        const int maxImages = 25;

        foreach (var entry in entries.Take(maxDisplay))
        {
            bool isImg = !Directory.Exists(entry) &&
                         GetCategory(Path.GetExtension(entry).ToLowerInvariant()) == FileCategory.Image;
            bool doThumb = isImg && imageCount < maxImages;
            if (isImg) imageCount++;
            panel.Children.Add(BuildFolderItem(entry, doThumb));
        }

        if (entries.Length > maxDisplay)
            panel.Children.Add(new TextBlock
            {
                Text = $"↓  {entries.Length - maxDisplay} öğe daha",
                Foreground = new SolidColorBrush(WpfColor.FromRgb(0x5E, 0x5E, 0x7A)),
                FontSize = 12,
                HorizontalAlignment = WpfHAlign.Center,
                Margin = new Thickness(0, 10, 0, 4)
            });

        scroll.Content = panel;
        ContentGrid.Children.Clear();
        ContentGrid.Children.Add(scroll);
    }

    private UIElement BuildFolderItem(string path, bool loadThumbnail)
    {
        bool isDir = Directory.Exists(path);
        string ext = isDir ? "" : Path.GetExtension(path).ToLowerInvariant();
        var category = isDir ? FileCategory.Unknown : GetCategory(ext);

        var container = new Border
        {
            Background = new SolidColorBrush(WpfColor.FromRgb(0x28, 0x28, 0x2A)),
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(0, 0, 0, 6),
            ClipToBounds = true
        };

        var grid = new Grid { Height = 72 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(96) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // ── Left: content preview ──
        var previewBorder = new Border
        {
            Background = new SolidColorBrush(WpfColor.FromRgb(0x20, 0x20, 0x22)),
            ClipToBounds = true
        };

        UIElement previewEl;

        if (isDir)
        {
            int sub = 0;
            try { sub = Directory.GetFileSystemEntries(path).Length; } catch { }
            var sp = new StackPanel { VerticalAlignment = WpfVAlign.Center, HorizontalAlignment = WpfHAlign.Center };
            sp.Children.Add(new TextBlock { Text = "📁", FontSize = 28, HorizontalAlignment = WpfHAlign.Center });
            sp.Children.Add(new TextBlock
            {
                Text = $"{sub} öğe",
                FontSize = 10,
                Foreground = new SolidColorBrush(WpfColor.FromRgb(0x8E, 0x8E, 0x93)),
                HorizontalAlignment = WpfHAlign.Center
            });
            previewEl = sp;
        }
        else if (category == FileCategory.Image && loadThumbnail)
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(path);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.DecodePixelWidth = 96;
                bmp.EndInit();
                previewEl = new WpfImage
                {
                    Source = bmp,
                    Stretch = Stretch.UniformToFill,
                    Width = 96, Height = 72
                };
            }
            catch
            {
                previewEl = CenteredEmoji("🖼️");
            }
        }
        else if (category == FileCategory.Text)
        {
            string snippet = ReadSnippet(path);
            previewEl = new Border
            {
                Padding = new Thickness(7, 6, 7, 6),
                Child = new TextBlock
                {
                    Text = string.IsNullOrEmpty(snippet) ? ext.TrimStart('.').ToUpperInvariant() : snippet,
                    FontFamily = new WpfFontFamily("Cascadia Code, Consolas"),
                    FontSize = 8.5,
                    Foreground = new SolidColorBrush(WpfColor.FromRgb(0x7E, 0xC8, 0x8E)),
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = WpfVAlign.Top,
                    LineHeight = 14
                }
            };
        }
        else
        {
            string emoji = (category, ext) switch
            {
                (FileCategory.Video, _) => "🎬",
                (FileCategory.Audio, _) => "🎵",
                (FileCategory.Image, _) => "🖼️",
                (_, ".pdf") => "📕",
                (_, ".zip") or (_, ".rar") or (_, ".7z") or (_, ".tar") or (_, ".gz") => "📦",
                (_, ".exe") or (_, ".msi") or (_, ".dll") => "⚙️",
                (_, ".docx") or (_, ".doc") => "📝",
                (_, ".xlsx") or (_, ".xls") => "📊",
                (_, ".pptx") or (_, ".ppt") => "📋",
                _ => "📄"
            };
            previewEl = CenteredEmoji(emoji);
        }

        previewBorder.Child = previewEl;
        Grid.SetColumn(previewBorder, 0);
        grid.Children.Add(previewBorder);

        // ── Right: name + metadata ──
        var info = new StackPanel
        {
            VerticalAlignment = WpfVAlign.Center,
            Margin = new Thickness(12, 0, 12, 0)
        };
        info.Children.Add(new TextBlock
        {
            Text = Path.GetFileName(path),
            Foreground = new SolidColorBrush(WpfColor.FromRgb(0xEB, 0xEB, 0xF5)),
            FontSize = 13, FontWeight = FontWeights.Medium,
            TextTrimming = TextTrimming.CharacterEllipsis
        });

        string meta;
        if (isDir)
        {
            int sub = 0;
            try { sub = Directory.GetFileSystemEntries(path).Length; } catch { }
            meta = $"Klasör · {sub} öğe";
        }
        else
        {
            string typeStr = category switch
            {
                FileCategory.Image => "Görüntü",
                FileCategory.Text => ext.TrimStart('.').ToUpperInvariant(),
                FileCategory.Video => "Video",
                FileCategory.Audio => "Ses",
                _ => string.IsNullOrEmpty(ext) ? "Dosya" : ext.TrimStart('.').ToUpperInvariant()
            };
            try { meta = $"{typeStr} · {FormatSize(new FileInfo(path).Length)}"; }
            catch { meta = typeStr; }
        }

        info.Children.Add(new TextBlock
        {
            Text = meta,
            Foreground = new SolidColorBrush(WpfColor.FromRgb(0x8E, 0x8E, 0x93)),
            FontSize = 11,
            Margin = new Thickness(0, 3, 0, 0)
        });

        Grid.SetColumn(info, 1);
        grid.Children.Add(info);
        container.Child = grid;
        return container;
    }

    private static TextBlock CenteredEmoji(string emoji) => new()
    {
        Text = emoji, FontSize = 28,
        HorizontalAlignment = WpfHAlign.Center,
        VerticalAlignment = WpfVAlign.Center
    };

    private static string ReadSnippet(string path)
    {
        try
        {
            using var reader = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var lines = new List<string>();
            string? line;
            while ((line = reader.ReadLine()) != null && lines.Count < 4)
            {
                var t = line.TrimStart();
                if (t.Length > 0)
                    lines.Add(t.Length > 22 ? t[..20] + "…" : t);
            }
            return string.Join("\n", lines);
        }
        catch { return ""; }
    }

    // ─── Image preview with zoom ─────────────────────────────────────────────

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

            FileSizeLabel.Text = $"{FormatSize(new FileInfo(_filePath).Length)} · {bmp.PixelWidth}×{bmp.PixelHeight}";
            HintText.Text = "Ctrl+↕ zoom  ·  ESC · SPACE kapat  ·  ← → gezin";

            _imageScale = new ScaleTransform(1.0, 1.0);
            var img = new WpfImage
            {
                Source = bmp,
                Stretch = Stretch.Uniform,
                StretchDirection = StretchDirection.DownOnly,
                Margin = new Thickness(12),
                RenderTransformOrigin = new System.Windows.Point(0.5, 0.5),
                RenderTransform = _imageScale
            };

            var scroll = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = img
            };
            scroll.PreviewMouseWheel += ImageScroll_MouseWheel;

            ContentGrid.Children.Clear();
            ContentGrid.Children.Add(scroll);
        }
        catch { ShowError("Görüntü yüklenemedi."); }
    }

    private void ImageScroll_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (_imageScale == null) return;
        if (!(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))) return;

        e.Handled = true;
        double factor = e.Delta > 0 ? 1.15 : 0.87;
        double newZ = Math.Clamp(_imageScale.ScaleX * factor, 0.1, 12.0);
        _imageScale.ScaleX = newZ;
        _imageScale.ScaleY = newZ;
    }

    // ─── Text / code preview ─────────────────────────────────────────────────

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

    // ─── Video preview ───────────────────────────────────────────────────────

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

    // ─── Audio preview ───────────────────────────────────────────────────────

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
            FontSize = 16, FontWeight = FontWeights.Medium,
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

    // ─── Unsupported / error ─────────────────────────────────────────────────

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

    // ─── Input handlers ──────────────────────────────────────────────────────

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
            case Key.Space:
                e.Handled = true;
                ClosePreview();
                break;
            case Key.Left:
                e.Handled = true;
                NavigateSibling(-1);
                break;
            case Key.Right:
                e.Handled = true;
                NavigateSibling(1);
                break;
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

    // ─── Helpers ─────────────────────────────────────────────────────────────

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
