using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace QuickLookWindows;

public partial class ToastWindow : Window
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(5) };

    public ToastWindow()
    {
        InitializeComponent();
        PositionBottomRight();
        _timer.Tick += (_, _) => FadeOut();
    }

    private void PositionBottomRight()
    {
        var screen = SystemParameters.WorkArea;
        Left = screen.Right - Width - 16;
        Top = screen.Bottom - Height - 16;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Slide up + fade in
        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(280));
        var slideUp = new DoubleAnimation(20, 0, TimeSpan.FromMilliseconds(280))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Card.BeginAnimation(OpacityProperty, fadeIn);
        SlideTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slideUp);
        _timer.Start();
    }

    private void FadeOut()
    {
        _timer.Stop();
        var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
        fade.Completed += (_, _) => Close();
        Card.BeginAnimation(OpacityProperty, fade);
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e) => FadeOut();
}
