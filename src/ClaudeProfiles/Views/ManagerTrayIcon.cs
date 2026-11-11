using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using ClaudeProfiles.ViewModels;

namespace ClaudeProfiles.Views;

public sealed class ManagerTrayIcon : IDisposable
{
    private static readonly Uri IconUri = new("avares://ClaudeProfiles/Assets/app.ico");

    private readonly TrayIcon _trayIcon;
    private readonly MainWindowViewModel _viewModel;
    private readonly Action _showWindow;
    private readonly Action _quit;

    public ManagerTrayIcon(Application application, MainWindowViewModel viewModel, Action showWindow, Action quit)
    {
        _viewModel = viewModel;
        _showWindow = showWindow;
        _quit = quit;

        _trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(IconUri)),
            ToolTipText = "Profils Claude",
            Menu = BuildMenu()
        };
        _trayIcon.Clicked += OnTrayIconClicked;
        TrayIcon.SetIcons(application, [_trayIcon]);

        _viewModel.Profiles.CollectionChanged += OnProfilesChanged;
    }

    public void Dispose()
    {
        _viewModel.Profiles.CollectionChanged -= OnProfilesChanged;
        _trayIcon.Clicked -= OnTrayIconClicked;
        _trayIcon.Dispose();
    }

    private NativeMenu BuildMenu()
    {
        var menu = new NativeMenu();

        foreach (ProfileItemViewModel profile in _viewModel.Profiles)
        {
            menu.Items.Add(new NativeMenuItem($"Ouvrir Claude · {profile.DisplayName}") { Command = profile.LaunchCommand });
        }

        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(CreateItem("Gérer les profils…", _showWindow));
        menu.Items.Add(CreateItem("Quitter Profils Claude", _quit));

        return menu;
    }

    private static NativeMenuItem CreateItem(string header, Action action)
    {
        var item = new NativeMenuItem(header);
        item.Click += (_, _) => action();
        return item;
    }

    private void OnProfilesChanged(object? sender, NotifyCollectionChangedEventArgs e) => _trayIcon.Menu = BuildMenu();

    private void OnTrayIconClicked(object? sender, EventArgs e) => _showWindow();
}
