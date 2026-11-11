using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Threading;
using ClaudeProfiles.ViewModels;

namespace ClaudeProfiles.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Activated += OnActivated;
        DataContextChanged += OnDataContextChanged;
    }

    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    public void Reveal()
    {
        Show();
        WindowState = WindowState == WindowState.Minimized ? WindowState.Normal : WindowState;
        Activate();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (e.CloseReason == WindowCloseReason.WindowClosing)
        {
            e.Cancel = true;
            Hide();
        }

        base.OnClosing(e);
    }

    private void OnActivated(object? sender, EventArgs e) => ViewModel?.RefreshRunningInstances();

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (ViewModel is not null)
        {
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.Editor) && ViewModel?.Editor is not null)
        {
            Dispatcher.UIThread.Post(FocusProfileNameBox, DispatcherPriority.Input);
        }
    }

    private void FocusProfileNameBox()
    {
        ProfileNameBox.Focus();
        ProfileNameBox.SelectAll();
    }
}
