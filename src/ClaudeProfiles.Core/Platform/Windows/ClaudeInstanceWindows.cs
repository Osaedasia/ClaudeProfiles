using System.Runtime.Versioning;
using System.Text;
using ClaudeProfiles.Core.Profiles;

namespace ClaudeProfiles.Core.Platform.Windows;

[SupportedOSPlatform("windows")]
internal sealed record ClaudeInstanceWindows(
    uint ProcessId,
    IReadOnlyList<IntPtr> ApplicationWindows,
    IReadOnlyList<IntPtr> NotifyIconHosts)
{
    private const string SingletonWindowClass = "Chrome_MessageWindow";
    private const string ApplicationWindowClass = "Chrome_WidgetWin_1";
    private const string NotifyIconHostClass = "Electron_NotifyIconHostWindow";

    public static ClaudeInstanceWindows? Find(string dataDirectory)
    {
        uint? processId = FindProcessUsing(dataDirectory);
        return processId is uint id ? CollectWindowsOf(id) : null;
    }

    public string? FindLogoDirectory()
    {
        string? executablePath = GetExecutablePath(ProcessId);
        DirectoryInfo? packageRoot = executablePath is null ? null : Directory.GetParent(executablePath)?.Parent;
        string? assetsDirectory = packageRoot is null ? null : Path.Combine(packageRoot.FullName, "Assets");

        return assetsDirectory is not null && Directory.Exists(assetsDirectory) ? assetsDirectory : null;
    }

    // Chromium, donc Electron, crée une fenêtre de messages cachée qui porte en titre le dossier de données de
    // l'instance : c'est par elle qu'une seconde instance retrouve la première. On s'en sert de la même façon.
    // https://source.chromium.org/chromium/chromium/src/+/main:chrome/browser/process_singleton_win.cc
    private static uint? FindProcessUsing(string dataDirectory)
    {
        IntPtr window = IntPtr.Zero;

        while ((window = NativeMethods.FindWindowEx(NativeMethods.MessageOnlyWindowParent, window, SingletonWindowClass, null)) != IntPtr.Zero)
        {
            string title = NativeMethods.GetWindowText(window);

            if (title.Length > 0 && ClaudeDataLocations.AreSameDirectory(title, dataDirectory))
            {
                NativeMethods.GetWindowThreadProcessId(window, out uint processId);
                return processId;
            }
        }

        return null;
    }

    private static ClaudeInstanceWindows CollectWindowsOf(uint processId)
    {
        var applicationWindows = new List<IntPtr>();
        var notifyIconHosts = new List<IntPtr>();

        NativeMethods.EnumWindows((window, _) =>
        {
            NativeMethods.GetWindowThreadProcessId(window, out uint ownerProcessId);

            if (ownerProcessId == processId)
            {
                ClassifyWindow(window, applicationWindows, notifyIconHosts);
            }

            return true;
        }, IntPtr.Zero);

        return new ClaudeInstanceWindows(processId, applicationWindows, notifyIconHosts);
    }

    private static void ClassifyWindow(IntPtr window, List<IntPtr> applicationWindows, List<IntPtr> notifyIconHosts)
    {
        string className = NativeMethods.GetClassName(window);

        if (className == NotifyIconHostClass)
        {
            notifyIconHosts.Add(window);
            return;
        }

        bool isTopLevelApplicationWindow = className == ApplicationWindowClass
            && NativeMethods.IsWindowVisible(window)
            && NativeMethods.GetWindow(window, NativeMethods.GetWindowOwner) == IntPtr.Zero;

        if (isTopLevelApplicationWindow)
        {
            applicationWindows.Add(window);
        }
    }

    private static string? GetExecutablePath(uint processId)
    {
        IntPtr process = NativeMethods.OpenProcess(NativeMethods.ProcessQueryLimitedInformation, inheritHandle: false, processId);
        if (process == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var path = new StringBuilder(1024);
            int length = path.Capacity;
            return NativeMethods.QueryFullProcessImageName(process, 0, path, ref length) ? path.ToString() : null;
        }
        finally
        {
            NativeMethods.CloseHandle(process);
        }
    }
}
