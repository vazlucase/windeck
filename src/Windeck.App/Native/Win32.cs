using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Windeck.Native;
internal static class Win32
{
    [StructLayout(LayoutKind.Sequential)] public struct Point { public int X,Y; }
    [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left,Top,Right,Bottom; public int Width=>Right-Left; public int Height=>Bottom-Top; }
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] public struct MonitorInfo { public int Size; public Rect Monitor, Work; public uint Flags; [MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)] public string Device; }
    [StructLayout(LayoutKind.Sequential)] public struct AppBarData { public uint Size; public nint Hwnd; public uint Callback,Edge; public Rect Rect; public nint Param; }
    [StructLayout(LayoutKind.Sequential)] public struct PowerStatus { public byte Ac,BatteryFlag,Percent,Reserved; public uint Life,Full; }
    [StructLayout(LayoutKind.Sequential)] public struct LastInput { public uint Size,Time; }
    [StructLayout(LayoutKind.Sequential)] struct Input { public uint Type; public InputUnion Data; }
    [StructLayout(LayoutKind.Explicit)] struct InputUnion { [FieldOffset(0)] public KeyboardInput Keyboard; [FieldOffset(0)] public MouseInput Mouse; }
    [StructLayout(LayoutKind.Sequential)] struct KeyboardInput { public ushort Key,Scan; public uint Flags,Time; public nuint Extra; }
    [StructLayout(LayoutKind.Sequential)] struct MouseInput { public int X,Y; public uint Data,Flags,Time; public nuint Extra; }
    public delegate bool EnumWindow(nint hwnd,nint param);
    public delegate bool EnumMonitor(nint monitor,nint dc,ref Rect rect,nint param);
    [DllImport("user32")] public static extern bool SetWindowPos(nint hwnd,nint after,int x,int y,int cx,int cy,uint flags);
    [DllImport("user32")] public static extern short GetAsyncKeyState(int key);
    [DllImport("user32")] public static extern bool GetCursorPos(out Point point);
    [DllImport("user32")] public static extern bool GetWindowRect(nint hwnd,out Rect rect);
    [DllImport("user32")] public static extern bool IsWindowVisible(nint hwnd);
    [DllImport("user32")] public static extern bool IsWindow(nint hwnd);
    [DllImport("user32")] public static extern bool ShowWindow(nint hwnd,int command);
    [DllImport("user32")] public static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32")] public static extern nint GetForegroundWindow();
    [DllImport("user32")] public static extern uint GetWindowThreadProcessId(nint hwnd,out uint process);
    [DllImport("user32",CharSet=CharSet.Unicode)] public static extern int GetClassName(nint hwnd,StringBuilder name,int count);
    [DllImport("user32")] public static extern bool EnumWindows(EnumWindow callback,nint param);
    [DllImport("user32")] public static extern bool EnumDisplayMonitors(nint dc,nint clip,EnumMonitor callback,nint param);
    [DllImport("user32",CharSet=CharSet.Unicode)] public static extern bool GetMonitorInfo(nint monitor,ref MonitorInfo info);
    [DllImport("user32")] public static extern uint GetDpiForWindow(nint hwnd);
    [DllImport("shcore")] public static extern int GetDpiForMonitor(nint monitor,int type,out uint x,out uint y);
    [DllImport("user32")] public static extern nint MonitorFromWindow(nint hwnd,uint flags);
    [DllImport("shell32")] public static extern nuint SHAppBarMessage(uint message,ref AppBarData data);
    [DllImport("user32",CharSet=CharSet.Unicode)] public static extern uint RegisterWindowMessage(string name);
    [DllImport("user32",CharSet=CharSet.Unicode)] public static extern bool PostMessage(nint hwnd,uint message,nint w,nint l);
    [DllImport("user32")] public static extern nint GetWindowLongPtrW(nint hwnd,int index);
    [DllImport("user32")] public static extern nint SetWindowLongPtrW(nint hwnd,int index,nint value);
    [DllImport("user32")] public static extern bool RegisterHotKey(nint hwnd,int id,uint modifiers,uint key);
    [DllImport("user32")] public static extern bool UnregisterHotKey(nint hwnd,int id);
    [DllImport("user32")] public static extern uint GetDoubleClickTime();
    [DllImport("user32")] static extern uint SendInput(uint count,Input[] inputs,int size);
    [DllImport("user32")] public static extern bool GetLastInputInfo(ref LastInput info);
    [DllImport("user32")] public static extern bool LockWorkStation();
    [DllImport("kernel32")] public static extern bool GetSystemPowerStatus(out PowerStatus status);
    [DllImport("dwmapi")] public static extern int DwmSetWindowAttribute(nint hwnd,int attribute,ref int value,int size);
    [DllImport("dwmapi")] public static extern int DwmRegisterThumbnail(nint destination,nint source,out nint thumbnail);
    [DllImport("dwmapi")] public static extern int DwmUnregisterThumbnail(nint thumbnail);
    [StructLayout(LayoutKind.Sequential)] public struct ThumbnailProperties { public uint Flags; public Rect Destination,Source; public byte Opacity; [MarshalAs(UnmanagedType.Bool)] public bool Visible; [MarshalAs(UnmanagedType.Bool)] public bool ClientOnly; }
    [DllImport("dwmapi")] public static extern int DwmUpdateThumbnailProperties(nint thumbnail,ref ThumbnailProperties properties);

    public static List<(nint Handle,MonitorInfo Info,double Scale)> Monitors()
    {
        var result=new List<(nint,MonitorInfo,double)>();
        EnumDisplayMonitors(0,0,(nint m,nint dc,ref Rect r,nint p)=>{
            var info=new MonitorInfo { Size=Marshal.SizeOf<MonitorInfo>(),Device="" };
            if(GetMonitorInfo(m,ref info)) { GetDpiForMonitor(m,0,out uint dpi,out _); result.Add((m,info,dpi>0?dpi/96d:1)); }
            return true;
        },0);
        return result;
    }
    public static List<nint> ExplorerTaskbars()
    {
        var result=new List<nint>();
        EnumWindows((hwnd,p)=>{
            var name=new StringBuilder(128); GetClassName(hwnd,name,name.Capacity);
            if(name.ToString() is "Shell_TrayWnd" or "Shell_SecondaryTrayWnd") {
                GetWindowThreadProcessId(hwnd,out uint pid);
                try { if(Process.GetProcessById((int)pid).ProcessName.Equals("explorer",StringComparison.OrdinalIgnoreCase)) result.Add(hwnd); } catch { }
            }
            return true;
        },0);
        return result;
    }
    public static void Shortcut(params ushort[] keys)
    {
        var inputs=new List<Input>();
        foreach(var key in keys) inputs.Add(new(){Type=1,Data=new(){Keyboard=new(){Key=key}}});
        foreach(var key in keys.Reverse()) inputs.Add(new(){Type=1,Data=new(){Keyboard=new(){Key=key,Flags=2}}});
        SendInput((uint)inputs.Count,inputs.ToArray(),Marshal.SizeOf<Input>());
    }
    public static void Open(string target)
    { Process.Start(new ProcessStartInfo(target){UseShellExecute=true}); }
    public static uint MousePoint()
    { GetCursorPos(out var p); return unchecked((uint)((p.Y & 65535)<<16 | (p.X & 65535))); }
}
