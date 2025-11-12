using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI;
using Microsoft.UI.Xaml.Documents;

namespace Nulis.Controls;

public sealed class CustomCommandPalette
{
    private Popup? _popup;
    private Grid? _overlayGrid;
    private Border? _paletteContainer;
    private TextBox? _searchBox;
    private ItemsControl? _commandsItemsControl;
    private StackPanel? _noResultsText;
    
    private List<CommandItem> _allCommands = new();
    private List<CommandItem> _filteredCommands = new();
    private int _selectedIndex = 0;
    private WeakReference<FrameworkElement>? _rootElementRef;

    public event EventHandler? CloseRequested;

    public CustomCommandPalette()
    {
        CreateUI();
    }

    private void CreateUI()
    {
        _overlayGrid = new Grid
        {
            Background = (Brush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"]
        };

        var overlayRect = new Rectangle
        {
            Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0))
        };
        overlayRect.Tapped += (s, e) => CloseRequested?.Invoke(this, EventArgs.Empty);
        _overlayGrid.Children.Add(overlayRect);

        _paletteContainer = new Border
        {
            Background = (Brush)Application.Current.Resources["LayerFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Center,
            Shadow = new ThemeShadow()
        };

        var innerGrid = new Grid
        {
            Margin = new Thickness(20)
        };
        innerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        innerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        _searchBox = new TextBox
        {
            PlaceholderText = "Type a command or search...",
            FontSize = 14,
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(0, 0, 0, 16),
            CornerRadius = new CornerRadius(6),
            BorderThickness = new Thickness(1)
        };
        _searchBox.TextChanged += SearchBox_TextChanged;
        _searchBox.KeyDown += SearchBox_KeyDown;
        Grid.SetRow(_searchBox, 0);

        var scrollViewer = new ScrollViewer
        {
            Padding = new Thickness(0, 0, 0, 8),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        Grid.SetRow(scrollViewer, 1);

        _commandsItemsControl = new ItemsControl();
        scrollViewer.Content = _commandsItemsControl;

        _noResultsText = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 8,
            Visibility = Visibility.Collapsed
        };

        var noResultsIcon = new FontIcon
        {
            Glyph = "\uE11A",
            FontSize = 32,
            Foreground = (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"]
        };

        var noResultsTextBlock = new TextBlock
        {
            Text = "No commands found",
            FontSize = 14,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
        };

        _noResultsText.Children.Add(noResultsIcon);
        _noResultsText.Children.Add(noResultsTextBlock);
        Grid.SetRow(_noResultsText, 1);

        innerGrid.Children.Add(_searchBox);
        innerGrid.Children.Add(scrollViewer);
        innerGrid.Children.Add(_noResultsText);

        _paletteContainer.Child = innerGrid;
        _overlayGrid.Children.Add(_paletteContainer);

        _popup = new Popup
        {
            Child = _overlayGrid,
            IsLightDismissEnabled = false
        };
        _popup.Closed += Popup_Closed;
    }

    private void Popup_Closed(object? sender, object e)
    {
        CleanupEventHandlers();
    }

    private void CleanupEventHandlers()
    {
        if (_rootElementRef != null && _rootElementRef.TryGetTarget(out var rootElement))
        {
            rootElement.SizeChanged -= RootElement_SizeChanged;
        }
        _rootElementRef = null;
    }

    public void Show(XamlRoot xamlRoot)
    {
        if (_popup != null)
        {
            _popup.XamlRoot = xamlRoot;
    
            if (xamlRoot.Content is FrameworkElement rootElement)
            {
                CleanupEventHandlers();
                _rootElementRef = new WeakReference<FrameworkElement>(rootElement);
                rootElement.SizeChanged += RootElement_SizeChanged;
                UpdateSizeForWindow(rootElement.ActualWidth, rootElement.ActualHeight);
            }
      
            _popup.IsOpen = true;
            _searchBox?.Focus(FocusState.Programmatic);
        }
    }

    private void RootElement_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_popup?.IsOpen == true)
        {
            UpdateSizeForWindow(e.NewSize.Width, e.NewSize.Height);
        }
    }

    private void UpdateSizeForWindow(double windowWidth, double windowHeight)
    {
        if (_overlayGrid == null || _paletteContainer == null) return;

        _overlayGrid.Width = windowWidth;
        _overlayGrid.Height = windowHeight;

        double paletteWidth = Math.Max(400, Math.Min(1200, windowWidth * 0.7));
        double paletteHeight = Math.Max(300, Math.Min(700, windowHeight * 0.6));
        double marginHorizontal = windowWidth * 0.1;
        double marginVertical = windowHeight * 0.1;

        _paletteContainer.Width = paletteWidth;
        _paletteContainer.Height = paletteHeight;
        _paletteContainer.Margin = new Thickness(marginHorizontal, marginVertical, marginHorizontal, 0);
    }

    public void Hide()
    {
        if (_popup != null)
        {
            _popup.IsOpen = false;
        }
    }

    public void SetCommands(List<CommandItem> commands)
    {
        _allCommands = commands;
        _filteredCommands = new List<CommandItem>(commands);
        UpdateCommandsList();
    }

    public void SetSize(double width, double height)
    {
        if (_paletteContainer != null)
        {
            _paletteContainer.Width = width;
            _paletteContainer.Height = height;
        }
    }

    public void SetResponsiveSize(double windowWidth, double windowHeight)
    {
        UpdateSizeForWindow(windowWidth, windowHeight);
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_searchBox == null) return;

        var searchText = _searchBox.Text;
        
        if (string.IsNullOrWhiteSpace(searchText))
        {
            _filteredCommands = new List<CommandItem>(_allCommands);
        }
        else
        {
            _filteredCommands = _allCommands
                .Select(cmd => new { Command = cmd, Match = FuzzyMatch(searchText, cmd) })
                .Where(x => x.Match.Matches)
                .OrderByDescending(x => x.Match.Score)
                .Select(x => x.Command)
                .ToList();
        }

        _selectedIndex = 0;
        UpdateCommandsList();
    }

    private (bool Matches, int Score) FuzzyMatch(string query, CommandItem item)
    {
        if (string.IsNullOrWhiteSpace(query))
            return (true, 0);

        query = query.ToLower();

        var nameMatch = FuzzyMatchString(query, item.Name.ToLower());
        var descMatch = FuzzyMatchString(query, item.Description.ToLower());
        var termsMatch = item.SearchTerms
            .Select(term => FuzzyMatchString(query, term.ToLower()))
            .Where(m => m.Matches)
            .OrderByDescending(m => m.Score)
            .FirstOrDefault();

        if (nameMatch.Matches)
            return nameMatch;
        if (descMatch.Matches)
            return (true, descMatch.Score - 5);
        if (termsMatch.Matches)
            return (true, termsMatch.Score - 10);

        return (false, 0);
    }

    private (bool Matches, int Score) FuzzyMatchString(string query, string text)
    {
        int queryIndex = 0;
        int textIndex = 0;
        int score = 0;
        int consecutiveMatches = 0;

        while (queryIndex < query.Length && textIndex < text.Length)
        {
            if (query[queryIndex] == text[textIndex])
            {
                consecutiveMatches++;
                score += 10 + (consecutiveMatches * 5);

                if (textIndex == 0)
                    score += 15;

                if (textIndex > 0 && (char.IsWhiteSpace(text[textIndex - 1]) || 
                    text[textIndex - 1] == '-' || text[textIndex - 1] == '_'))
                    score += 12;

                queryIndex++;
            }
            else
            {
                consecutiveMatches = 0;
            }
            textIndex++;
        }

        if (queryIndex < query.Length)
            return (false, 0);

        score -= (int)((text.Length - query.Length) * 0.5);

        return (true, score);
    }

    private async void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Down)
        {
            e.Handled = true;
            _selectedIndex = Math.Min(_selectedIndex + 1, _filteredCommands.Count - 1);
            UpdateSelection();
        }
        else if (e.Key == Windows.System.VirtualKey.Up)
        {
            e.Handled = true;
            _selectedIndex = Math.Max(_selectedIndex - 1, 0);
            UpdateSelection();
        }
        else if (e.Key == Windows.System.VirtualKey.Enter)
        {
            e.Handled = true;
            await ExecuteSelectedCommand();
        }
        else if (e.Key == Windows.System.VirtualKey.Escape)
        {
            e.Handled = true;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void UpdateCommandsList()
    {
        if (_commandsItemsControl == null || _noResultsText == null) return;

        _commandsItemsControl.Items.Clear();
     
        foreach (var command in _filteredCommands)
        {
            var button = CreateCommandButton(command);
            _commandsItemsControl.Items.Add(button);
        }

        _noResultsText.Visibility = _filteredCommands.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
     
        if (_filteredCommands.Count > 0)
        {
            UpdateSelection();
        }
    }

    private Button CreateCommandButton(CommandItem command)
    {
        var button = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(0, 0, 0, 2),
            CornerRadius = new CornerRadius(6),
            BorderThickness = new Thickness(1),
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0)),
            Tag = command
        };

        button.Click += async (s, e) =>
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
            await command.Action();
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var leftPanel = new StackPanel
        {
            Spacing = 3,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(leftPanel, 0);

        var nameText = new TextBlock
        {
            Text = command.Name,
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"]
        };

        var descText = new TextBlock
        {
            Text = command.Description,
            FontSize = 11,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            TextWrapping = TextWrapping.Wrap,
            Visibility = string.IsNullOrEmpty(command.Description) ? Visibility.Collapsed : Visibility.Visible,
            MaxLines = 2,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        leftPanel.Children.Add(nameText);
        leftPanel.Children.Add(descText);

        var shortcutBorder = new Border
        {
            Background = (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"],
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 4, 8, 4),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0),
            Visibility = string.IsNullOrEmpty(command.Shortcut) ? Visibility.Collapsed : Visibility.Visible
        };
        Grid.SetColumn(shortcutBorder, 1);

        var shortcutText = new TextBlock
        {
            Text = command.Shortcut,
            FontSize = 11,
            FontFamily = new FontFamily("Consolas"),
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
        };

        shortcutBorder.Child = shortcutText;

        grid.Children.Add(leftPanel);
        grid.Children.Add(shortcutBorder);
        button.Content = grid;

        return button;
    }

    private void UpdateSelection()
    {
        if (_commandsItemsControl == null) return;

        for (int i = 0; i < _commandsItemsControl.Items.Count; i++)
        {
            if (_commandsItemsControl.Items[i] is Button button)
            {
                UpdateButtonStyle(button, i == _selectedIndex);
            }
        }
    }

    private void UpdateButtonStyle(Button button, bool isSelected)
    {
        if (isSelected)
        {
            var accentColor = (Windows.UI.Color)Application.Current.Resources["SystemAccentColor"];
            button.BorderBrush = new SolidColorBrush(accentColor);
            button.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(25, accentColor.R, accentColor.G, accentColor.B));
        }
        else
        {
            button.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));
            button.Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"];
        }
    }

    private async Task ExecuteSelectedCommand()
    {
        if (_filteredCommands.Count > 0 && _selectedIndex >= 0 && _selectedIndex < _filteredCommands.Count)
        {
            var command = _filteredCommands[_selectedIndex];
            CloseRequested?.Invoke(this, EventArgs.Empty);
            await command.Action();
        }
    }
}

public class CommandItem : INotifyPropertyChanged
{
    private string _name = "";
    private string _description = "";
    private string _shortcut = "";
    private List<string> _searchTerms = new();
    private Func<Task> _action = () => Task.CompletedTask;
    private bool _isSelected = false;

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            if (_description != value)
            {
                _description = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DescriptionVisibility));
            }
        }
    }

    public string Shortcut
    {
        get => _shortcut;
        set
        {
            if (_shortcut != value)
            {
                _shortcut = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShortcutVisibility));
            }
        }
    }

    public List<string> SearchTerms
    {
        get => _searchTerms;
        set
        {
            if (_searchTerms != value)
            {
                _searchTerms = value;
                OnPropertyChanged();
            }
        }
    }

    public Func<Task> Action
    {
        get => _action;
        set
        {
            if (_action != value)
            {
                _action = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public Visibility DescriptionVisibility => string.IsNullOrEmpty(Description) ? Visibility.Collapsed : Visibility.Visible;
    public Visibility ShortcutVisibility => string.IsNullOrEmpty(Shortcut) ? Visibility.Collapsed : Visibility.Visible;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}