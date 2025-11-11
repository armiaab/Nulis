using System;
using Microsoft.UI.Xaml;
using Nulis.Services;

namespace Nulis;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
        LoggerService.Initialize();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
