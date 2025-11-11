using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Web.WebView2.Core;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Nulis.Services;

namespace Nulis;

public sealed partial class MainWindow : Window
{
    private readonly ILogger<MainWindow> _logger;
    private bool _isReady = false;
    private string _currentFileName = "Untitled.md";
    private string? _currentFilePath;

    // File state tracking
    private bool _hasUnsavedChanges = false;
    private string _lastSavedContent = "";
    private bool _isFileModified = false;
    private DispatcherTimer? _contentCheckTimer;

    public MainWindow()
    {
        _logger = LoggerService.GetLogger<MainWindow>();

        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(CustomTitleBar);
        UpdateTitleBarTheme();

        SetupKeyboardShortcuts();

        if (Content is FrameworkElement content)
        {
            content.ActualThemeChanged += Content_ActualThemeChanged;
        }

        UpdateTitle();
        InitializeEditor();
    }

    private async void Content_ActualThemeChanged(FrameworkElement sender, object args)
    {
        var isDark = sender.ActualTheme == ElementTheme.Dark;
        _logger.LogInformation("Theme changed to: {Theme} (isDark: {IsDark})", sender.ActualTheme, isDark);

        UpdateTitleBarTheme();

        if (_isReady)
        {
            await ExecuteScriptSafely($"if(window.setTheme) window.setTheme({(isDark ? "true" : "false")});");
        }
    }

    private void UpdateTitleBarTheme()
    {
        var isDark = Content is FrameworkElement fe && fe.ActualTheme == ElementTheme.Dark;

        if (AppWindow.TitleBar is not null)
        {
            var titleBar = AppWindow.TitleBar;
            var transparent = Windows.UI.Color.FromArgb(0, 0, 0, 0);

            if (isDark)
            {
                // Dark theme colors
                titleBar.ButtonForegroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255);
                titleBar.ButtonHoverForegroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255);
                titleBar.ButtonPressedForegroundColor = Windows.UI.Color.FromArgb(255, 200, 200, 200);
                titleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(255, 128, 128, 128);

                titleBar.ButtonBackgroundColor = transparent;
                titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(255, 60, 60, 60);
                titleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(255, 40, 40, 40);
                titleBar.ButtonInactiveBackgroundColor = transparent;
            }
            else
            {
                // Light theme colors
                titleBar.ButtonForegroundColor = Windows.UI.Color.FromArgb(255, 0, 0, 0);
                titleBar.ButtonHoverForegroundColor = Windows.UI.Color.FromArgb(255, 0, 0, 0);
                titleBar.ButtonPressedForegroundColor = Windows.UI.Color.FromArgb(255, 96, 96, 96);
                titleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(255, 128, 128, 128);

                titleBar.ButtonBackgroundColor = transparent;
                titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(255, 220, 220, 220);
                titleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(255, 200, 200, 200);
                titleBar.ButtonInactiveBackgroundColor = transparent;
            }

            _logger.LogDebug("Title bar theme updated to: {Theme}", isDark ? "Dark" : "Light");
        }
    }

    private void SetupKeyboardShortcuts()
    {
        if (Content is Grid rootGrid)
        {
            rootGrid.KeyDown += RootGrid_KeyDown;
        }

        _logger.LogDebug("Keyboard shortcuts configured (Ctrl+N = New File, Ctrl+O = Open, Ctrl+S = Save, Ctrl+Shift+S = Save As, F2 = Rename, Ctrl+Shift+P = Command Palette)");
    }

    private async void RootGrid_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        var ctrlState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control);
        var isCtrlPressed = ctrlState.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

        var shiftState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift);
        var isShiftPressed = shiftState.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

        if (e.Key == Windows.System.VirtualKey.N && isCtrlPressed)
        {
            e.Handled = true;
            _logger.LogDebug("Ctrl+N pressed, creating new file");
            await CreateNewFile();
        }
        else if (e.Key == Windows.System.VirtualKey.F2)
        {
            e.Handled = true;
            _logger.LogDebug("F2 pressed, showing rename dialog");
            await ShowRenameDialog();
        }
        else if (e.Key == Windows.System.VirtualKey.P && isCtrlPressed && isShiftPressed)
        {
            e.Handled = true;
            _logger.LogDebug("Ctrl+Shift+P pressed, showing command palette");
            await ShowCommandPalette();
        }
        else if (e.Key == Windows.System.VirtualKey.O && isCtrlPressed)
        {
            e.Handled = true;
            _logger.LogDebug("Ctrl+O pressed, showing open file dialog");
            await ShowOpenFileDialog();
        }
        else if (e.Key == Windows.System.VirtualKey.S && isCtrlPressed && isShiftPressed)
        {
            e.Handled = true;
            _logger.LogDebug("Ctrl+Shift+S pressed, showing save as dialog");
            await SaveFileAsAsync();
        }
        else if (e.Key == Windows.System.VirtualKey.S && isCtrlPressed)
        {
            e.Handled = true;
            _logger.LogDebug("Ctrl+S pressed, saving file");
            await SaveFileAsync();
        }
        else if (e.Key == Windows.System.VirtualKey.Escape)
        {
            e.Handled = true;
            _logger.LogDebug("Escape pressed, hiding command palette if open");
            HideCommandPalette();
        }
    }

    private Controls.CustomCommandPalette? _currentPalette;

    private async Task ShowCommandPalette()
    {
        // Don't show if already showing
        if (_currentPalette != null) return;

        var commands = new List<Controls.CommandItem>
        {
            new Controls.CommandItem
            {
                Name = "New File",
                Description = "Create a new markdown file",
                Shortcut = "Ctrl+N",
                SearchTerms = new List<string> { "new", "create", "file" },
                Action = async () => await CreateNewFile()
            },
            new Controls.CommandItem
            {
                Name = "Open File",
                Description = "Open a markdown or text file",
                Shortcut = "Ctrl+O",
                SearchTerms = new List<string> { "open", "file", "load" },
                Action = async () => await ShowOpenFileDialog()
            },
            new Controls.CommandItem
            {
                Name = "Save",
                Description = "Save the current file",
                Shortcut = "Ctrl+S",
                SearchTerms = new List<string> { "save", "write" },
                Action = async () => await SaveFileAsync()
            },
            new Controls.CommandItem
            {
                Name = "Save As...",
                Description = "Save as a new file",
                Shortcut = "Ctrl+Shift+S",
                SearchTerms = new List<string> { "save as", "save new", "export" },
                Action = async () => await SaveFileAsAsync()
            },
            new Controls.CommandItem
            {
                Name = "Rename File",
                Description = "Rename the current file",
                Shortcut = "F2",
                SearchTerms = new List<string> { "rename", "name" },
                Action = async () => await ShowRenameDialog()
            },
            new Controls.CommandItem
            {
                Name = "Undo",
                Description = "Undo the last action",
                Shortcut = "Ctrl+Z",
                SearchTerms = new List<string> { "undo", "revert" },
                Action = async () => await ExecuteScriptSafely("executeCommand('undo')")
            },
            new Controls.CommandItem
            {
                Name = "Redo",
                Description = "Redo the last undone action",
                Shortcut = "Ctrl+Y",
                SearchTerms = new List<string> { "redo", "repeat" },
                Action = async () => await ExecuteScriptSafely("executeCommand('redo')")
            },
            new Controls.CommandItem
            {
                Name = "Bold",
                Description = "Make text bold",
                Shortcut = "Ctrl+B",
                SearchTerms = new List<string> { "bold", "strong", "format" },
                Action = async () => await ExecuteScriptSafely("document.execCommand('bold')")
            },
            new Controls.CommandItem
            {
                Name = "Italic",
                Description = "Make text italic",
                Shortcut = "Ctrl+I",
                SearchTerms = new List<string> { "italic", "emphasis", "format" },
                Action = async () => await ExecuteScriptSafely("document.execCommand('italic')")
            },
            new Controls.CommandItem
            {
                Name = "Select All",
                Description = "Select all text in the editor",
                Shortcut = "Ctrl+A",
                SearchTerms = new List<string> { "select", "select all", "all" },
                Action = async () => await ExecuteScriptSafely("document.execCommand('selectAll')")
            }
        };

        // Create the custom command palette
        _currentPalette = new Controls.CustomCommandPalette();

        // Set up commands and size
        _currentPalette.SetCommands(commands);

        // Make it responsive to window size
        var windowSize = AppWindow.Size;
        _currentPalette.SetResponsiveSize(windowSize.Width, windowSize.Height);

        // Handle close event
        _currentPalette.CloseRequested += (s, e) => HideCommandPalette();

        // Show the palette
        _currentPalette.Show(this.Content.XamlRoot);
    }

    private void HideCommandPalette()
    {
        if (_currentPalette != null)
        {
            _currentPalette.Hide();
            _currentPalette = null;
        }
    }

    private async Task ShowOpenFileDialog()
    {
        // Check for unsaved changes before opening a new file
        bool hasUnsavedChanges = await CheckForUnsavedChanges();
        
        if (hasUnsavedChanges)
        {
            var dialog = new ContentDialog
            {
                Title = "Unsaved Changes",
                Content = "You have unsaved changes. Would you like to save them before opening a new file?",
                PrimaryButtonText = "Save",
                SecondaryButtonText = "Don't Save",
                CloseButtonText = "Cancel",
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // User chose to save
                await SaveFileAsync();
            }
            else if (result == ContentDialogResult.Secondary)
            {
                // User chose not to save, discard changes
                _logger.LogDebug("Unsaved changes discarded for file open");
            }
            else
            {
                // User cancelled, do not open new file
                return;
            }
        }

        try
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".md");
            picker.FileTypeFilter.Add(".markdown");
            picker.FileTypeFilter.Add(".txt");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                _logger.LogInformation("File selected: {FileName} at {Path}", file.Name, file.Path);
                await LoadFileAsync(file);
            }
            else
            {
                _logger.LogDebug("File open cancelled");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show open file dialog");
        }
    }

    private async Task LoadFileAsync(Windows.Storage.StorageFile file)
    {
        try
        {
            var content = await Windows.Storage.FileIO.ReadTextAsync(file);
            _logger.LogInformation("File read successfully, content length: {Length}", content.Length);

            _currentFilePath = file.Path;
            _currentFileName = file.Name;
            _lastSavedContent = content; // Track the content that was loaded

            int attempts = 0;
            while (!_isReady && attempts < 100)
            {
                _logger.LogDebug("Waiting for editor to be ready... attempt {Attempt}", attempts);
                await Task.Delay(100);
                attempts++;
            }

            if (!_isReady)
            {
                _logger.LogError("Editor not ready after 10 seconds");
                return;
            }

            _logger.LogInformation("Editor is ready, loading content...");

            bool success = false;

            try
            {
                await SetMarkdownAsync(content);
                success = true;
                _logger.LogInformation("Content loaded using SetMarkdownAsync");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Method 1 failed, trying alternative...");
            }

            if (!success)
            {
                try
                {
                    var escapedContent = System.Text.Json.JsonSerializer.Serialize(content);
                    var script = $"if (window.setMarkdown) {{ window.setMarkdown({escapedContent}); }} else {{ console.error('setMarkdown not available'); }}";
                    await EditorWebView.ExecuteScriptAsync(script);
                    success = true;
                    _logger.LogInformation("Content loaded using direct script execution");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Method 2 also failed");
                }
            }

            if (success)
            {
                MarkFileAsSaved(); // File is loaded and saved
                _logger.LogInformation("File loaded successfully: {FileName}", file.Name);
            }
            else
            {
                _logger.LogError("All loading methods failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load file");
        }
    }

    private async Task ShowRenameDialog()
    {
        // Can't rename a file that hasn't been saved yet
        if (string.IsNullOrEmpty(_currentFilePath))
        {
            var infoDialog = new ContentDialog
            {
                Title = "Cannot Rename",
                Content = "Please save the file first before renaming.",
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await infoDialog.ShowAsync();
            return;
        }

        var dialog = new ContentDialog
        {
            Title = "Rename File",
            PrimaryButtonText = "Rename",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.Content.XamlRoot
        };

        var textBox = new TextBox
        {
            Text = _currentFileName,
            PlaceholderText = "Enter filename...",
            SelectionStart = 0,
            SelectionLength = _currentFileName.Length - 3
        };

        dialog.Content = textBox;
        dialog.Opened += (s, e) => textBox.Focus(FocusState.Programmatic);

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            var newFileName = textBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(newFileName))
            {
                _logger.LogWarning("Empty filename entered, keeping current name");
                return;
            }

            if (!newFileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase) &&
                !newFileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) &&
                !newFileName.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase))
            {
                newFileName += ".md";
            }

            // Don't rename if the name hasn't changed
            if (newFileName.Equals(_currentFileName, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Filename unchanged, skipping rename");
                return;
            }

            // Attempt to rename the actual file
            try
            {
                var oldFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(_currentFilePath);
                var folder = await oldFile.GetParentAsync();
                var newPath = Path.Combine(folder.Path, newFileName);

                // Check if a file with the new name already exists
                if (File.Exists(newPath))
                {
                    var conflictDialog = new ContentDialog
                    {
                        Title = "File Already Exists",
                        Content = $"A file named '{newFileName}' already exists in this location. Please choose a different name.",
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await conflictDialog.ShowAsync();
                    return;
                }

                // Rename the file
                await oldFile.RenameAsync(newFileName, Windows.Storage.NameCollisionOption.FailIfExists);

                // Update the current file path and name
                _currentFilePath = newPath;
                _currentFileName = newFileName;
                UpdateTitle();

                _logger.LogInformation("File successfully renamed from '{OldName}' to '{NewName}' at {Path}",
                    oldFile.Name, newFileName, newPath);

                // Show success message
                var successDialog = new ContentDialog
                {
                    Title = "File Renamed",
                    Content = $"File successfully renamed to '{newFileName}'",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await successDialog.ShowAsync();
            }
            catch (FileNotFoundException)
            {
                _logger.LogError("File not found at path: {Path}", _currentFilePath);
                var errorDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = "The file could not be found. It may have been moved or deleted.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied while renaming file");
                var errorDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = "Access denied. You may not have permission to rename this file.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rename file");
                var errorDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Failed to rename file: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }
        else
        {
            _logger.LogDebug("Rename cancelled");
        }
    }

    private void UpdateTitle()
    {
        var displayName = _hasUnsavedChanges ? $"*{_currentFileName}" : _currentFileName;
        TitleBarFileNameText.Text = displayName;
    }

    private async void InitializeEditor()
    {
        try
        {
            await EditorWebView.EnsureCoreWebView2Async();

            ConfigureWebView2Performance();

            EditorWebView.CoreWebView2.ContextMenuRequested += CoreWebView2_ContextMenuRequested;

            var appFolder = AppDomain.CurrentDomain.BaseDirectory;
            var htmlPath = Path.Combine(appFolder, "Assets", "milkdown", "index.html");

            if (File.Exists(htmlPath))
            {
                var htmlUri = new Uri($"file:///{htmlPath.Replace("\\", "/")}");
                EditorWebView.CoreWebView2.Navigate(htmlUri.AbsoluteUri);
                _logger.LogInformation("Loading Milkdown from: {HtmlUri}", htmlUri.AbsoluteUri);
            }
            else
            {
                _logger.LogError("Milkdown not found at: {HtmlPath}", htmlPath);
            }

            EditorWebView.NavigationCompleted += OnNavigationCompleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize editor");
        }
    }

    private void ConfigureWebView2Performance()
    {
        try
        {
            var settings = EditorWebView.CoreWebView2.Settings;

            settings.AreDefaultScriptDialogsEnabled = false;
            settings.AreDevToolsEnabled = false;
            settings.IsStatusBarEnabled = false;
            settings.IsZoomControlEnabled = true;
            settings.IsGeneralAutofillEnabled = false;
            settings.IsPasswordAutosaveEnabled = false;
            settings.IsPinchZoomEnabled = false;
            settings.IsSwipeNavigationEnabled = false;
            settings.AreBrowserAcceleratorKeysEnabled = true;
            settings.IsScriptEnabled = true;
            settings.IsWebMessageEnabled = true;

            EditorWebView.CoreWebView2.MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Low;

            _logger.LogInformation("WebView2 performance settings configured");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure WebView2 performance settings");
        }
    }

    private void CoreWebView2_ContextMenuRequested(CoreWebView2 sender, CoreWebView2ContextMenuRequestedEventArgs args)
    {
        var menuItems = args.MenuItems;

        _logger.LogDebug("Context menu requested: {Count} items", menuItems.Count);

        for (int i = 0; i < menuItems.Count; i++)
        {
            var item = menuItems[i];
            _logger.LogTrace("[{Index}] Name: '{Name}', Label: '{Label}', Kind: {Kind}",
                i, item.Name, item.Label, item.Kind);
        }

        var hasSpellingSuggestions = false;
        for (int i = 0; i < menuItems.Count; i++)
        {
            var item = menuItems[i];
            if (item.Label.Contains("suggestion") ||
                item.Name.Contains("spell") ||
                item.Label.Contains("Add to") ||
                (i < 5 && menuItems.Count > 8 && item.Kind == CoreWebView2ContextMenuItemKind.Command))
            {
                hasSpellingSuggestions = true;
                break;
            }
        }

        if (!hasSpellingSuggestions)
        {
            args.Handled = true;
            ShowCustomContextMenu(args);
        }
        else
        {
            _logger.LogDebug("Spell-check context detected");

            for (int i = menuItems.Count - 1; i >= 0; i--)
            {
                var item = menuItems[i];
                var name = item.Name?.ToLower() ?? "";
                var label = item.Label?.ToLower() ?? "";

                if (name.Contains("inspect") ||
                    name.Contains("reload") ||
                    name.Contains("view") ||
                    label.Contains("inspect") ||
                    label.Contains("reload"))
                {
                    _logger.LogTrace("Removing menu item: {Label}", item.Label);
                    menuItems.RemoveAt(i);
                }
            }

            var separator = sender.Environment.CreateContextMenuItem(
                "", null, CoreWebView2ContextMenuItemKind.Separator);
            menuItems.Add(separator);

            AddCustomMenuItems(sender, menuItems);
        }
    }

    private async void ShowCustomContextMenu(CoreWebView2ContextMenuRequestedEventArgs args)
    {
        var sender = EditorWebView.CoreWebView2;
        var menuItems = args.MenuItems;

        menuItems.Clear();

        var undoItem = sender.Environment.CreateContextMenuItem(
            "Undo", null, CoreWebView2ContextMenuItemKind.Command);
        undoItem.CustomItemSelected += async (s, e) =>
            await ExecuteScriptSafely("executeCommand('undo')");
        menuItems.Add(undoItem);

        var redoItem = sender.Environment.CreateContextMenuItem(
            "Redo", null, CoreWebView2ContextMenuItemKind.Command);
        redoItem.CustomItemSelected += async (s, e) =>
            await ExecuteScriptSafely("executeCommand('redo')");
        menuItems.Add(redoItem);

        menuItems.Add(sender.Environment.CreateContextMenuItem(
            "", null, CoreWebView2ContextMenuItemKind.Separator));

        var cutItem = sender.Environment.CreateContextMenuItem(
            "Cut", null, CoreWebView2ContextMenuItemKind.Command);
        cutItem.CustomItemSelected += async (s, e) =>
            await ExecuteScriptSafely("document.execCommand('cut')");
        menuItems.Add(cutItem);

        var copyItem = sender.Environment.CreateContextMenuItem(
            "Copy", null, CoreWebView2ContextMenuItemKind.Command);
        copyItem.CustomItemSelected += async (s, e) =>
            await ExecuteScriptSafely("document.execCommand('copy')");
        menuItems.Add(copyItem);

        var pasteItem = sender.Environment.CreateContextMenuItem(
            "Paste", null, CoreWebView2ContextMenuItemKind.Command);
        pasteItem.CustomItemSelected += async (s, e) =>
            await ExecuteScriptSafely("document.execCommand('paste')");
        menuItems.Add(pasteItem);

        menuItems.Add(sender.Environment.CreateContextMenuItem(
            "", null, CoreWebView2ContextMenuItemKind.Separator));

        var boldItem = sender.Environment.CreateContextMenuItem(
            "Bold", null, CoreWebView2ContextMenuItemKind.Command);
        boldItem.CustomItemSelected += async (s, e) =>
            await ExecuteScriptSafely("document.execCommand('bold')");
        menuItems.Add(boldItem);

        var italicItem = sender.Environment.CreateContextMenuItem(
            "Italic", null, CoreWebView2ContextMenuItemKind.Command);
        italicItem.CustomItemSelected += async (s, e) =>
            await ExecuteScriptSafely("document.execCommand('italic')");
        menuItems.Add(italicItem);

        menuItems.Add(sender.Environment.CreateContextMenuItem(
            "", null, CoreWebView2ContextMenuItemKind.Separator));

        var selectAllItem = sender.Environment.CreateContextMenuItem(
            "Select All", null, CoreWebView2ContextMenuItemKind.Command);
        selectAllItem.CustomItemSelected += async (s, e) =>
            await ExecuteScriptSafely("document.execCommand('selectAll')");
        menuItems.Add(selectAllItem);
    }

    private void AddCustomMenuItems(CoreWebView2 sender, IList<CoreWebView2ContextMenuItem> menuItems)
    {
        var boldItem = sender.Environment.CreateContextMenuItem(
            "Bold", null, CoreWebView2ContextMenuItemKind.Command);
        boldItem.CustomItemSelected += async (s, e) =>
            await ExecuteScriptSafely("document.execCommand('bold')");
        menuItems.Add(boldItem);

        var italicItem = sender.Environment.CreateContextMenuItem(
            "Italic", null, CoreWebView2ContextMenuItemKind.Command);
        italicItem.CustomItemSelected += async (s, e) =>
            await ExecuteScriptSafely("document.execCommand('italic')");
        menuItems.Add(italicItem);

        menuItems.Add(sender.Environment.CreateContextMenuItem(
            "", null, CoreWebView2ContextMenuItemKind.Separator));

        var selectAllItem = sender.Environment.CreateContextMenuItem(
            "Select All", null, CoreWebView2ContextMenuItemKind.Command);
        selectAllItem.CustomItemSelected += async (s, e) =>
            await ExecuteScriptSafely("document.execCommand('selectAll')");
        menuItems.Add(selectAllItem);

        menuItems.Add(sender.Environment.CreateContextMenuItem(
            "", null, CoreWebView2ContextMenuItemKind.Separator));

        var commandPaletteItem = sender.Environment.CreateContextMenuItem(
            "Command Palette (Ctrl+Shift+P)", null, CoreWebView2ContextMenuItemKind.Command);
        commandPaletteItem.CustomItemSelected += async (s, e) =>
            await ShowCommandPalette();
        menuItems.Add(commandPaletteItem);
    }

    private async void OnNavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (_isReady || !args.IsSuccess)
        {
            if (!args.IsSuccess)
            {
                _logger.LogWarning("Navigation failed with WebErrorStatus: {Status}", args.WebErrorStatus);
            }
            return;
        }

        _isReady = true;
        _logger.LogInformation("Editor navigation completed, setting ready flag");

        EditorWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

        await Task.Delay(500);

        try
        {
            await ExecuteScriptSafely("if(window.forceSpellcheck) window.forceSpellcheck();");

            await Task.Delay(200);

            await ExecuteScriptSafely(@"
                    if(window.forceSpellcheck) window.forceSpellcheck();
                    
                    document.querySelectorAll('[contenteditable]').forEach((el, i) => {
                        console.log('Editable ' + i + ':', {
                            tag: el.tagName,
                            spellcheck_attr: el.getAttribute('spellcheck'),
                            spellcheck_prop: el.spellcheck,
                            contenteditable: el.contentEditable
                        });
                    });
                ");

            await ExecuteScriptSafely(@"
                    const style = document.createElement('style');
                    style.textContent = `
                        ::-webkit-scrollbar { display: none; }
                        body { -ms-overflow-style: none; scrollbar-width: none; }
                    `;
                    document.head.appendChild(style);
                ");

            var isDark = Content is FrameworkElement fe && fe.ActualTheme == ElementTheme.Dark;
            await ExecuteScriptSafely($"if(window.setTheme) window.setTheme({(isDark ? "true" : "false")});");

            // Set up content change tracking
            await ExecuteScriptSafely("if(window.setupContentChangeTracking) window.setupContentChangeTracking();");

            _logger.LogInformation("Milkdown editor fully initialized and ready with theme: {IsDark}", isDark);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete navigation setup");
        }
    }

    private async void CoreWebView2_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var message = e.WebMessageAsJson;
            if (string.IsNullOrEmpty(message))
                return;

            _logger.LogDebug("WebMessage received: {Message}", message);

            var data = System.Text.Json.JsonDocument.Parse(message);
            if (!data.RootElement.TryGetProperty("action", out var action))
                return;

            var actionValue = action.GetString();
            _logger.LogInformation("Processing action: {Action}", actionValue);

            switch (actionValue)
            {
                case "contentChanged":
                    MarkFileAsModified();
                    break;
                case "new":
                    await CreateNewFile();
                    break;
                case "open":
                    await ShowOpenFileDialog();
                    break;
                case "rename":
                    await ShowRenameDialog();
                    break;
                case "save":
                    await SaveFileAsync();
                    break;
                case "saveAs":
                    await SaveFileAsAsync();
                    break;
                case "pickImage":
                    await ShowImagePickerDialog();
                    break;
                case "commandPalette":
                    await ShowCommandPalette();
                    break;
                default:
                    _logger.LogWarning("Unknown action: {Action}", actionValue);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle web message");
        }
    }

    private async Task ShowImagePickerDialog()
    {
        try
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".gif");
            picker.FileTypeFilter.Add(".bmp");
            picker.FileTypeFilter.Add(".svg");
            picker.FileTypeFilter.Add(".webp");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                _logger.LogInformation("Image selected: {FileName}", file.Name);
                await ProcessImageFile(file);
            }
            else
            {
                _logger.LogDebug("Image picker cancelled");
                await ExecuteScriptSafely("if(window.onImagePicked) window.onImagePicked(null, null);");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show image picker dialog");
            await ExecuteScriptSafely("if(window.onImagePicked) window.onImagePicked(null, null);");
        }
    }

    private async Task ProcessImageFile(Windows.Storage.StorageFile file)
    {
        try
        {
            var buffer = await Windows.Storage.FileIO.ReadBufferAsync(file);
            var bytes = new byte[buffer.Length];
            using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer))
            {
                reader.ReadBytes(bytes);
            }

            var base64 = Convert.ToBase64String(bytes);

            var extension = file.FileType.ToLower();
            var mimeType = extension switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".svg" => "image/svg+xml",
                ".webp" => "image/webp",
                _ => "image/png"
            };

            var dataUrl = $"data:{mimeType};base64,{base64}";

            var escapedFileName = System.Text.Json.JsonSerializer.Serialize(file.Name);
            var escapedDataUrl = System.Text.Json.JsonSerializer.Serialize(dataUrl);

            var script = $"if(window.onImagePicked) window.onImagePicked({escapedDataUrl}, {escapedFileName});";
            await ExecuteScriptSafely(script);

            _logger.LogInformation("Image processed and sent to editor: {FileName}, size: {Size} bytes", file.Name, bytes.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process image file");
            await ExecuteScriptSafely("if(window.onImagePicked) window.onImagePicked(null, null);");
        }
    }

    private async Task ExecuteScriptSafely(string script)
    {
        try
        {
            await EditorWebView.ExecuteScriptAsync(script);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Script execution failed: {Script}", script.Length > 50 ? script.Substring(0, 50) + "..." : script);
        }
    }

    public async Task<string> GetMarkdownAsync()
    {
        if (!_isReady)
        {
            _logger.LogWarning("GetMarkdownAsync called before editor is ready");
            return "";
        }

        try
        {
            var result = await EditorWebView.ExecuteScriptAsync("getMarkdown()");
            return System.Text.Json.JsonSerializer.Deserialize<string>(result) ?? "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get markdown content");
            return "";
        }
    }

    public async Task SetMarkdownAsync(string markdown)
    {
        if (!_isReady)
        {
            _logger.LogWarning("SetMarkdownAsync called before editor is ready");
            return;
        }

        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(markdown);
            await EditorWebView.ExecuteScriptAsync($"setMarkdown({json})");
            _logger.LogDebug("SetMarkdownAsync completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set markdown content");
        }
    }

    public void SetFileName(string fileName)
    {
        _currentFileName = fileName;
        UpdateTitle();
    }

    private async Task SaveFileAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                await SaveFileAsAsync();
                return;
            }

            var content = await GetMarkdownAsync();

            var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(_currentFilePath);
            await Windows.Storage.FileIO.WriteTextAsync(file, content);

            _lastSavedContent = content; // Update last saved content
            MarkFileAsSaved(); // Mark as saved

            _logger.LogInformation("File saved: {FileName}", _currentFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save file");
        }
    }

    private async Task SaveFileAsAsync()
    {
        try
        {
            var picker = new Windows.Storage.Pickers.FileSavePicker();

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("Markdown", new List<string> { ".md" });
            picker.FileTypeChoices.Add("Text", new List<string> { ".txt" });
            picker.SuggestedFileName = _currentFileName;

            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                var content = await GetMarkdownAsync();
                await Windows.Storage.FileIO.WriteTextAsync(file, content);

                _currentFilePath = file.Path;
                _currentFileName = file.Name;
                _lastSavedContent = content; // Update last saved content
                MarkFileAsSaved(); // Mark as saved

                _logger.LogInformation("File saved as: {FileName} at {Path}", file.Name, file.Path);
            }
            else
            {
                _logger.LogDebug("Save as cancelled");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show save file dialog");
        }
    }

    private void MarkFileAsModified()
    {
        if (!_hasUnsavedChanges)
        {
            _hasUnsavedChanges = true;
            _isFileModified = true;
            
            // Update title immediately on UI thread
            DispatcherQueue.TryEnqueue(() => {
                UpdateTitle();
            });
            
            // Start periodic content checking for ultra-fast updates
            StartContentCheckTimer();
            
            _logger.LogDebug("File marked as modified");
        }
    }

    private void MarkFileAsSaved()
    {
        _hasUnsavedChanges = false;
        _isFileModified = false;
        
        // Update title immediately on UI thread
        DispatcherQueue.TryEnqueue(() => {
            UpdateTitle();
        });
        
        // Stop periodic checking when file is saved
        StopContentCheckTimer();
        
        _logger.LogDebug("File marked as saved");
    }

    private void StartContentCheckTimer()
    {
        if (_contentCheckTimer == null)
        {
            _contentCheckTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50) // Check every 50ms for ultra-fast response
            };
            _contentCheckTimer.Tick += async (s, e) => await ContentCheckTimer_Tick();
        }
        
        if (!_contentCheckTimer.IsEnabled)
        {
            _contentCheckTimer.Start();
            _logger.LogDebug("Content check timer started for fast title updates");
        }
    }

    private void StopContentCheckTimer()
    {
        if (_contentCheckTimer?.IsEnabled == true)
        {
            _contentCheckTimer.Stop();
            _logger.LogDebug("Content check timer stopped");
        }
    }

    private async Task ContentCheckTimer_Tick()
    {
        try
        {
            // Only check if editor is ready and we think there might be changes
            if (_isReady && _hasUnsavedChanges)
            {
                var currentContent = await GetMarkdownAsync();
                
                // If content matches saved content, mark as saved
                if (currentContent == _lastSavedContent && _hasUnsavedChanges)
                {
                    _hasUnsavedChanges = false;
                    _isFileModified = false;
                    UpdateTitle();
                    StopContentCheckTimer();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in content check timer");
            StopContentCheckTimer();
        }
    }

    private async Task<bool> CheckForUnsavedChanges()
    {
        if (!_isReady) return false;

        try
        {
            var currentContent = await GetMarkdownAsync();

            // Compare with last saved content
            bool hasChanges = currentContent != _lastSavedContent;

            if (hasChanges != _hasUnsavedChanges)
            {
                _hasUnsavedChanges = hasChanges;
                _isFileModified = hasChanges;
                UpdateTitle();
            }

            return hasChanges;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for unsaved changes");
            return false;
        }
    }

    private async Task CreateNewFile()
    {
        // Check for actual unsaved changes
        bool hasUnsavedChanges = await CheckForUnsavedChanges();

        if (hasUnsavedChanges)
        {
            var dialog = new ContentDialog
            {
                Title = "Unsaved Changes",
                Content = "You have unsaved changes. Would you like to save them before creating a new file?",
                PrimaryButtonText = "Save",
                SecondaryButtonText = "Don't Save",
                CloseButtonText = "Cancel",
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // User chose to save
                await SaveFileAsync();
            }
            else if (result == ContentDialogResult.Secondary)
            {
                // User chose not to save, discard changes
                _logger.LogDebug("Unsaved changes discarded");
            }
            else
            {
                // User cancelled, do not create new file
                return;
            }
        }

        // Create a new empty file
        _currentFileName = "Untitled.md";
        _currentFilePath = null;
        _lastSavedContent = "";

        await SetMarkdownAsync("");
        MarkFileAsSaved(); // New empty file is considered "saved"

        _logger.LogInformation("Created new file");
    }
}