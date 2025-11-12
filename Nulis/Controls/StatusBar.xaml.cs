using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Nulis.Controls;

public sealed partial class StatusBar : UserControl
{
    private DispatcherTimer? _timer;
    private TimeSpan _remainingTime;
    private bool _isTimerRunning = false;
    private bool _isHovering = false;

    public StatusBar()
    {
        InitializeComponent();
        _remainingTime = TimeSpan.FromMinutes(15);
        UpdateTimerDisplay();
    }

    public void UpdateWordCount(int wordCount)
    {
        WordCountText.Text = wordCount.ToString();
    }

    public void UpdateCharacterCount(int characterCount)
    {
        CharacterCountText.Text = characterCount.ToString();
    }

    public void UpdateFileType(string fileType)
    {
        FileTypeText.Text = fileType;
    }

    public void UpdateStats(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            UpdateWordCount(0);
            UpdateCharacterCount(0);
            return;
        }

        var characterCount = CountCharacters(markdown);
        var wordCount = CountWords(markdown);

        UpdateWordCount(wordCount);
        UpdateCharacterCount(characterCount);
    }

    private int CountCharacters(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        text = text.Replace("\r\n", "\n").Replace("\r", "\n");
        
        var count = 0;
        foreach (var c in text)
        {
            if (c != ' ' && c != '\t' && c != '\n' && c != '\r' && c != '\v' && c != '\f')
            {
                count++;
            }
        }
        return count;
    }

    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        text = text.Replace("\r\n", "\n").Replace("\r", "\n");
        
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
        
        var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        
        var validWords = 0;
        foreach (var word in words)
        {
            var trimmed = word.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                validWords++;
            }
        }
        
        return validWords;
    }

    private void Timer_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _isHovering = true;
        UpdateTimerHoverState();
    }

    private void Timer_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _isHovering = false;
        UpdateTimerHoverState();
    }

    private void Timer_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_isTimerRunning)
        {
            StopTimer();
        }
        else
        {
            StartTimer();
        }
    }

    private void Timer_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        ResetTimer();
    }

    private void StartTimer()
    {
        _isTimerRunning = true;
        _remainingTime = TimeSpan.FromMinutes(15);

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += Timer_Tick;
        _timer.Start();

        UpdateTimerDisplay();
        UpdateTimerStyle();
    }

    private void StopTimer()
    {
        _isTimerRunning = false;
        _timer?.Stop();

        UpdateTimerStyle();
    }

    private void ResetTimer()
    {
        _isTimerRunning = false;
        _timer?.Stop();
        _timer = null;
        _remainingTime = TimeSpan.FromMinutes(15);

        UpdateTimerDisplay();
        UpdateTimerStyle();
    }

    private void Timer_Tick(object? sender, object e)
    {
        _remainingTime = _remainingTime.Subtract(TimeSpan.FromSeconds(1));

        if (_remainingTime.TotalSeconds <= 0)
        {
            ResetTimer();
            ShowTimerCompletedNotification();
        }
        else
        {
            UpdateTimerDisplay();
        }
    }

    private void UpdateTimerDisplay()
    {
        TimerText.Text = $"{_remainingTime.Minutes:D2}:{_remainingTime.Seconds:D2}";
    }

    private void UpdateTimerHoverState()
    {
        if (_isHovering)
        {
            if (_isTimerRunning)
            {
                var accentColor = (Windows.UI.Color)Application.Current.Resources["SystemAccentColor"];
                TimerContainer.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(30, accentColor.R, accentColor.G, accentColor.B));
            }
            else
            {
                TimerContainer.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(20, 128, 128, 128));
            }
        }
        else
        {
            TimerContainer.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));
        }
    }

    private void UpdateTimerStyle()
    {
    if (_isTimerRunning)
    {
        var accentColor = (Windows.UI.Color)Application.Current.Resources["SystemAccentColor"];
        TimerText.Foreground = new SolidColorBrush(accentColor);
        TimerIcon.Foreground = new SolidColorBrush(accentColor);
        TimerText.Opacity = 1.0;
        TimerIcon.Opacity = 1.0;
    }
    else
    {
        TimerText.Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
        TimerIcon.Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
        TimerText.Opacity = 0.8;
        TimerIcon.Opacity = 0.8;
    }
    
    UpdateTimerHoverState();
}

private async void ShowTimerCompletedNotification()
{
    var dialog = new ContentDialog
    {
        Title = "Timer Completed",
        Content = "Your 15-minute focus session is complete!",
        CloseButtonText = "OK",
        XamlRoot = this.XamlRoot
    };

    await dialog.ShowAsync();
}

public void Cleanup()
{
    ResetTimer();
}
}
