using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ClaudeProfiles.Core.Instance;
using ClaudeProfiles.Core.Profiles;

namespace ClaudeProfiles.Core.Platform.Windows;

[SupportedOSPlatform("windows")]
public sealed class WindowsInstanceMarker : IInstanceMarker, IDisposable
{
    private const ushort StringValueType = 31;
    private const uint RelaunchCommandProperty = 2;
    private const uint RelaunchIconProperty = 3;
    private const uint RelaunchDisplayNameProperty = 4;
    private const uint AppUserModelIdProperty = 5;
    private const string AppUserModelIdPrefix = "ClaudeProfiles.Profile.";
    private const uint FirstNotifyIconIdToTry = 1;
    private const uint LastNotifyIconIdToTry = 16;

    private static readonly Guid AppUserModelFormatId = new("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3");

    private readonly IProfileIconFactory _iconFactory;
    private readonly string _managerExecutable;
    private readonly Dictionary<IntPtr, string> _taskbarIconByWindow = [];
    private readonly Dictionary<IntPtr, uint> _notifyIconIdByHost = [];
    private readonly Dictionary<string, IntPtr> _loadedTrayIcons = [];

    public WindowsInstanceMarker(IProfileIconFactory iconFactory, string managerExecutable)
    {
        _iconFactory = iconFactory;
        _managerExecutable = managerExecutable;
    }

    public void Mark(ClaudeProfile profile)
    {
        if (ClaudeInstanceWindows.Find(profile.DataDirectory) is not ClaudeInstanceWindows instance)
        {
            return;
        }

        ProfileIconSet icons = _iconFactory.GetIcons(profile, instance.FindLogoDirectory());
        string label = $"Claude · {profile.DisplayName}";

        foreach (IntPtr window in instance.ApplicationWindows)
        {
            MarkTaskbarButton(window, profile, icons.TaskbarIconFile, label);
        }

        foreach (IntPtr host in instance.NotifyIconHosts)
        {
            MarkNotifyIcon(host, icons.TrayIconFile, label);
        }
    }

    public void Dispose()
    {
        foreach (IntPtr icon in _loadedTrayIcons.Values)
        {
            NativeMethods.DestroyIcon(icon);
        }

        _loadedTrayIcons.Clear();
    }

    // Un identifiant AppUserModelID propre à la fenêtre la sort du groupe « Claude » de la barre des tâches et lui
    // donne l'icône et le nom de relance fournis, y compris pour Claude installé en MSIX (constaté avec la 4.23118).
    // https://learn.microsoft.com/en-us/windows/win32/shell/appids
    // https://learn.microsoft.com/en-us/windows/win32/properties/props-system-appusermodel-relaunchcommand
    private void MarkTaskbarButton(IntPtr window, ClaudeProfile profile, string taskbarIconFile, string label)
    {
        if (_taskbarIconByWindow.TryGetValue(window, out string? appliedIcon) && appliedIcon == taskbarIconFile)
        {
            return;
        }

        Guid propertyStoreId = typeof(NativeMethods.IPropertyStore).GUID;
        if (NativeMethods.SHGetPropertyStoreForWindow(window, ref propertyStoreId, out NativeMethods.IPropertyStore store) != 0)
        {
            return;
        }

        try
        {
            SetString(store, RelaunchCommandProperty, ManagerMessage.BuildOpenCommandLine(_managerExecutable, profile.DataDirectory));
            SetString(store, RelaunchIconProperty, taskbarIconFile + ",0");
            SetString(store, RelaunchDisplayNameProperty, label);
            SetString(store, AppUserModelIdProperty, AppUserModelIdPrefix + Path.GetFileName(profile.DataDirectory));
            store.Commit();
            _taskbarIconByWindow[window] = taskbarIconFile;
        }
        catch (COMException)
        {
            // La fenêtre a été fermée entre son repérage et son marquage : il n'y a plus rien à marquer.
        }
        finally
        {
            Marshal.ReleaseComObject(store);
        }
    }

    // Explorer accepte la modification d'une icône de notification identifiée par sa fenêtre et son numéro, quel
    // que soit le processus appelant. Electron numérote ses icônes à partir d'une base fixe (3 pour la première,
    // constaté avec Claude 4.23118) : on cherche le numéro une fois, puis on le réutilise.
    // https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shell_notifyiconw
    private void MarkNotifyIcon(IntPtr host, string trayIconFile, string label)
    {
        IntPtr icon = LoadTrayIcon(trayIconFile);

        if (_notifyIconIdByHost.TryGetValue(host, out uint knownId) && TryModifyNotifyIcon(host, knownId, icon, label))
        {
            return;
        }

        for (uint candidateId = FirstNotifyIconIdToTry; candidateId <= LastNotifyIconIdToTry; candidateId++)
        {
            if (TryModifyNotifyIcon(host, candidateId, icon, label))
            {
                _notifyIconIdByHost[host] = candidateId;
                return;
            }
        }
    }

    private static bool TryModifyNotifyIcon(IntPtr host, uint id, IntPtr icon, string label)
    {
        var data = new NativeMethods.NotifyIconData
        {
            Size = Marshal.SizeOf<NativeMethods.NotifyIconData>(),
            Window = host,
            Id = id,
            Flags = NativeMethods.NotifyIconFlagIcon | NativeMethods.NotifyIconFlagTip,
            Icon = icon,
            Tip = label,
            Info = string.Empty,
            InfoTitle = string.Empty
        };

        return NativeMethods.Shell_NotifyIcon(NativeMethods.NotifyIconModify, ref data);
    }

    private IntPtr LoadTrayIcon(string trayIconFile)
    {
        if (_loadedTrayIcons.TryGetValue(trayIconFile, out IntPtr loadedIcon))
        {
            return loadedIcon;
        }

        int size = NativeMethods.GetSystemMetrics(NativeMethods.SmallIconWidthMetric);
        IntPtr icon = NativeMethods.LoadImage(IntPtr.Zero, trayIconFile, NativeMethods.ImageIcon, size, size, NativeMethods.LoadFromFile);
        if (icon == IntPtr.Zero)
        {
            throw new IOException($"The tray icon '{trayIconFile}' could not be loaded.");
        }

        _loadedTrayIcons[trayIconFile] = icon;
        return icon;
    }

    private static void SetString(NativeMethods.IPropertyStore store, uint propertyId, string value)
    {
        var key = new NativeMethods.PropertyKey { FormatId = AppUserModelFormatId, PropertyId = propertyId };
        var variant = new NativeMethods.PropVariant { ValueType = StringValueType, Pointer = Marshal.StringToCoTaskMemUni(value) };

        try
        {
            store.SetValue(ref key, ref variant);
        }
        finally
        {
            Marshal.FreeCoTaskMem(variant.Pointer);
        }
    }
}
