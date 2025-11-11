using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace Nulis.Controls;

public sealed partial class CommandPalette : ContentDialog
{
    private List<CommandItem> _allCommands = new();
    private List<CommandItem> _filteredCommands = new();
    private int _selectedIndex = 0;

    public CommandPalette()
    {
        InitializeComponent();
        Loaded += CommandPalette_Loaded;
    }

    private void CommandPalette_Loaded(object sender, RoutedEventArgs e)
    {
        SearchBox.Focus(FocusState.Programmatic);
        UpdateCommandsList();
    }

    public void SetCommands(List<CommandItem> commands)
    {
        _allCommands = commands;
        _filteredCommands = new List<CommandItem>(commands);
        UpdateCommandsList();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = SearchBox.Text.ToLower();
        
        if (string.IsNullOrWhiteSpace(searchText))
        {
            _filteredCommands = new List<CommandItem>(_allCommands);
        }
        else
        {
            _filteredCommands = _allCommands
                .Where(cmd => 
                    cmd.Name.ToLower().Contains(searchText) || 
                    cmd.Description.ToLower().Contains(searchText) ||
                    cmd.SearchTerms.Any(term => term.ToLower().Contains(searchText)))
                .ToList();
        }

        _selectedIndex = 0;
        UpdateCommandsList();
    }

    private void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Down)
        {
            e.Handled = true;
            _selectedIndex = Math.Min(_selectedIndex + 1, _filteredCommands.Count - 1);
            UpdateSelection();
            ScrollToSelected();
        }
        else if (e.Key == Windows.System.VirtualKey.Up)
        {
            e.Handled = true;
            _selectedIndex = Math.Max(_selectedIndex - 1, 0);
            UpdateSelection();
            ScrollToSelected();
        }
        else if (e.Key == Windows.System.VirtualKey.Enter)
        {
            e.Handled = true;
            ExecuteSelectedCommand();
        }
        else if (e.Key == Windows.System.VirtualKey.Escape)
        {
            e.Handled = true;
            Hide();
        }
    }

    private void UpdateCommandsList()
    {
        CommandsItemsControl.ItemsSource = _filteredCommands;
        NoResultsText.Visibility = _filteredCommands.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        
        if (_filteredCommands.Count > 0)
        {
            // Delay to ensure items are rendered before updating selection
            DispatcherQueue.TryEnqueue(() =>
            {
                UpdateSelection();
            });
        }
    }

    private void UpdateSelection()
    {
        // Update the visual state of all buttons
        for (int i = 0; i < CommandsItemsControl.Items.Count; i++)
        {
            var container = CommandsItemsControl.ContainerFromIndex(i);
            if (container != null)
            {
                var button = FindButton(container as FrameworkElement);
                if (button != null)
                {
                    UpdateButtonStyle(button, i == _selectedIndex);
                }
            }
        }
    }

    private Button? FindButton(FrameworkElement? element)
    {
        if (element == null) return null;
        if (element is Button button) return button;
        
        var childCount = VisualTreeHelper.GetChildrenCount(element);
        for (int i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(element, i) as FrameworkElement;
            var result = FindButton(child);
            if (result != null) return result;
        }
        return null;
    }

    private void UpdateButtonStyle(Button button, bool isSelected)
    {
        if (isSelected)
        {
            // Use theme-aware accent color
            var accentColor = (Windows.UI.Color)Application.Current.Resources["SystemAccentColor"];
            button.BorderBrush = new SolidColorBrush(accentColor);
            button.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(25, accentColor.R, accentColor.G, accentColor.B));
        }
        else
        {
            button.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));
            button.Background = Application.Current.Resources["CardBackgroundFillColorDefaultBrush"] as SolidColorBrush;
        }
    }

    private void ScrollToSelected()
    {
        if (_selectedIndex >= 0 && _selectedIndex < CommandsItemsControl.Items.Count)
        {
            var container = CommandsItemsControl.ContainerFromIndex(_selectedIndex);
            if (container is FrameworkElement element)
            {
                element.StartBringIntoView(new BringIntoViewOptions
                {
                    AnimationDesired = true,
                    VerticalAlignmentRatio = 0.5
                });
            }
        }
    }

    private async void CommandButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is CommandItem command)
        {
            Hide();
            await command.Action();
        }
    }

    private async void ExecuteSelectedCommand()
    {
        if (_filteredCommands.Count > 0 && _selectedIndex >= 0 && _selectedIndex < _filteredCommands.Count)
        {
            var command = _filteredCommands[_selectedIndex];
            Hide();
            await command.Action();
        }
    }
}

public class CommandItem
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Shortcut { get; set; } = "";
    public List<string> SearchTerms { get; set; } = new();
    public Func<Task> Action { get; set; } = () => Task.CompletedTask;
    public bool IsSelected { get; set; } = false;

    public Visibility DescriptionVisibility => string.IsNullOrEmpty(Description) ? Visibility.Collapsed : Visibility.Visible;
    public Visibility ShortcutVisibility => string.IsNullOrEmpty(Shortcut) ? Visibility.Collapsed : Visibility.Visible;
}
