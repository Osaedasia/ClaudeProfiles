using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace ClaudeProfiles.Core.Platform.Windows;

[SupportedOSPlatform("windows")]
internal static class NativeMethods
{
    public const uint GetWindowOwner = 4;
    public const uint ImageIcon = 1;
    public const uint LoadFromFile = 0x10;
    public const int SmallIconWidthMetric = 49;
    public const uint NotifyIconModify = 1;
    public const uint NotifyIconFlagIcon = 0x2;
    public const uint NotifyIconFlagTip = 0x4;
    public const uint ProcessQueryLimitedInformation = 0x1000;

    public static readonly IntPtr MessageOnlyWindowParent = new(-3);

    public delegate bool EnumWindowsCallback(IntPtr window, IntPtr parameter);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindowEx(IntPtr parent, IntPtr childAfter, string? className, string? windowName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr parameter);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetClassName(IntPtr window, StringBuilder className, int maxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr window, StringBuilder text, int maxCount);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr window);

    [DllImport("user32.dll")]
    public static extern IntPtr GetWindow(IntPtr window, uint relationship);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr LoadImage(IntPtr instance, string name, uint type, int width, int height, uint options);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyIcon(IntPtr icon);

    [DllImport("user32.dll")]
    public static extern int GetSystemMetrics(int index);

    [DllImport("shell32.dll")]
    public static extern int SHGetPropertyStoreForWindow(IntPtr window, ref Guid interfaceId, out IPropertyStore store);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool Shell_NotifyIcon(uint message, ref NotifyIconData data);

    [DllImport("kernel32.dll")]
    public static extern IntPtr OpenProcess(uint access, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, uint processId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool QueryFullProcessImageName(IntPtr process, uint flags, StringBuilder path, ref int size);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr handle);

    public static string GetClassName(IntPtr window)
    {
        var className = new StringBuilder(256);
        GetClassName(window, className, className.Capacity);
        return className.ToString();
    }

    public static string GetWindowText(IntPtr window)
    {
        var text = new StringBuilder(1024);
        GetWindowText(window, text, text.Capacity);
        return text.ToString();
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct NotifyIconData
    {
        public int Size;
        public IntPtr Window;
        public uint Id;
        public uint Flags;
        public uint CallbackMessage;
        public IntPtr Icon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Tip;
        public uint State;
        public uint StateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Info;
        public uint Version;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string InfoTitle;
        public uint InfoFlags;
        public Guid Item;
        public IntPtr BalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct PropertyKey
    {
        public Guid FormatId;
        public uint PropertyId;
    }

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct PropVariant
    {
        [FieldOffset(0)] public ushort ValueType;
        [FieldOffset(8)] public IntPtr Pointer;
    }

    [ComImport]
    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IPropertyStore
    {
        void GetCount(out uint count);

        void GetAt(uint index, out PropertyKey key);

        void GetValue(ref PropertyKey key, out PropVariant value);

        void SetValue(ref PropertyKey key, ref PropVariant value);

        void Commit();
    }
}
