using System.Runtime.InteropServices;
using System.Windows.Interop;
using ManagedShell;
using Windeck.Native;

namespace Windeck.Services;
internal sealed class DesktopIntegration : IDisposable
{
    public ShellManager Shell {get;}
    private nint window;
    private readonly HashSet<nint> reserved=[];
    private readonly Dictionary<nint,Win32.Rect> positions=[];
    public bool Replacing {get;private set;}
    public string? Error {get;private set;}
    public DesktopIntegration(string[]? pinnedTray=null)
    {
        var config=ShellManager.DefaultShellConfig;
        config.EnableWin11DpiWorkaround=false;
        if(pinnedTray is {Length:>0})config.PinnedNotifyIcons=pinnedTray;
        Shell=new ShellManager(config);
        if(Shell.NotificationArea.IsFailed||Shell.NotificationArea.Handle==0)Error="A bandeja não inicializou. A barra original será mantida.";
    }
    public async Task<bool> ReplaceAsync(nint hwnd,Win32.Rect monitor,bool reserve)
    {
        if(Error!=null)return false;
        if(!await Recovery.ArmAsync()){Error="O processo de recuperação não iniciou. A barra original será mantida.";return false;}
        window=hwnd;
        if(reserve)Reserve(hwnd,monitor);
        Shell.ExplorerHelper.HideExplorerTaskbar=true;
        Replacing=true;
        return true;
    }
    public void Reserve(nint hwnd,Win32.Rect monitor)
    {
        if(hwnd==0)return;
        var data=new Win32.AppBarData {Size=(uint)Marshal.SizeOf<Win32.AppBarData>(),Hwnd=hwnd,Callback=Win32.RegisterWindowMessage("Windeck.AppBar"),Edge=1};
        if(reserved.Add(hwnd))Win32.SHAppBarMessage(0,ref data);
        data.Rect=monitor;
        Win32.SHAppBarMessage(2,ref data);
        double scale=Win32.GetDpiForWindow(hwnd)/96d;
        data.Rect.Bottom=data.Rect.Top+(int)Math.Round(54*Math.Max(1,scale));
        if(positions.TryGetValue(hwnd,out var previous)&&previous.Left==data.Rect.Left&&previous.Top==data.Rect.Top&&previous.Right==data.Rect.Right&&previous.Bottom==data.Rect.Bottom)return;
        positions[hwnd]=data.Rect;Win32.SHAppBarMessage(3,ref data);
    }
    public void Release(nint hwnd)
    {
        positions.Remove(hwnd);
        if(reserved.Remove(hwnd)){var data=new Win32.AppBarData{Size=(uint)Marshal.SizeOf<Win32.AppBarData>(),Hwnd=hwnd};Win32.SHAppBarMessage(1,ref data);}
    }
    public void Restore()
    {
        Shell.ExplorerHelper.HideExplorerTaskbar=false;
        foreach(var hwnd in reserved){var data=new Win32.AppBarData{Size=(uint)Marshal.SizeOf<Win32.AppBarData>(),Hwnd=hwnd};Win32.SHAppBarMessage(1,ref data);}reserved.Clear();positions.Clear();
        Replacing=false;Recovery.Restore();
    }
    public void Dispose(){try{Restore();Shell.AppBarManager.SignalGracefulShutdown();Shell.Dispose();}finally{Recovery.Restore();}}
}
