using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ClaudeProfiles.Core.Instance;
using ClaudeProfiles.Core.Platform;
using ClaudeProfiles.Core.Profiles;
using ClaudeProfiles.Icons;
using ClaudeProfiles.ViewModels;
using ClaudeProfiles.Views;

namespace ClaudeProfiles;

public partial class App : Application
{
    private static readonly TimeSpan RunningInstancesRefreshInterval = TimeSpan.FromSeconds(2);

    private readonly ManagerMessage _startupMessage;
    private readonly CancellationTokenSource _shutdown = new();

    private MainWindow? _window;
    private MainWindowViewModel? _viewModel;
    private IInstanceMarker? _instanceMarker;
    private ManagerTrayIcon? _trayIcon;
    private DispatcherTimer? _refreshTimer;

    public App()
        : this(ManagerMessage.ShowWindow)
    {
    }

    public App(ManagerMessage startupMessage)
    {
        _startupMessage = startupMessage;
    }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            desktop.Startup += OnDesktopStartup;
            desktop.Exit += OnDesktopExit;
            CreateServices(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void CreateServices(IClassicDesktopStyleApplicationLifetime desktop)
    {
        ClaudeDataLocations locations = PlatformServices.CreateDataLocations();
        IInstanceDetector instanceDetector = PlatformServices.CreateInstanceDetector();
        var catalog = new ProfileCatalog(new JsonProfileStore(locations.ManagerSettingsFile), locations, instanceDetector);
        var iconFactory = new AvaloniaProfileIconFactory(locations.ManagerIconsDirectory);
        string managerExecutable = Environment.ProcessPath ?? throw new InvalidOperationException("The manager executable path is unknown.");

        _instanceMarker = PlatformServices.CreateInstanceMarker(iconFactory, managerExecutable);
        _window = new MainWindow();
        _viewModel = new MainWindowViewModel(catalog, PlatformServices.CreateLauncher(), instanceDetector, _window.Launcher, _instanceMarker);
        _window.DataContext = _viewModel;
        _trayIcon = new ManagerTrayIcon(this, _viewModel, ShowWindow, () => desktop.Shutdown());

        _refreshTimer = new DispatcherTimer { Interval = RunningInstancesRefreshInterval };
        _refreshTimer.Tick += (_, _) => _viewModel.RefreshRunningInstances();
    }

    private async void OnDesktopStartup(object? sender, ControlledApplicationLifetimeStartupEventArgs e)
    {
        if (_viewModel is null || _refreshTimer is null)
        {
            return;
        }

        await _viewModel.LoadAsync(_shutdown.Token);
        HandleMessage(_startupMessage);
        _refreshTimer.Start();

        await ListenForOtherInstancesAsync();
    }

    private async Task ListenForOtherInstancesAsync()
    {
        try
        {
            await ManagerInstanceChannel.ListenAsync(HandleMessage, _shutdown.Token);
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
            return;
        }
    }

    private void HandleMessage(ManagerMessage message)
    {
        switch (message.Request)
        {
            case ManagerRequest.ShowWindow:
                ShowWindow();
                break;
            case ManagerRequest.OpenProfile when message.ProfileDirectory is string profileDirectory:
                OpenProfile(profileDirectory);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(message), message, "Unsupported manager message.");
        }
    }

    private void OpenProfile(string profileDirectory)
    {
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.OpenProfile(profileDirectory);

        if (_viewModel.ErrorMessage is not null)
        {
            ShowWindow();
        }
    }

    private void ShowWindow() => _window?.Reveal();

    private void OnDesktopExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        _shutdown.Cancel();
        _refreshTimer?.Stop();
        _trayIcon?.Dispose();

        if (_instanceMarker is IDisposable disposableMarker)
        {
            disposableMarker.Dispose();
        }

        _shutdown.Dispose();
    }
}
