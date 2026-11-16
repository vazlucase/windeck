using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Windeck.Native;
namespace Windeck.Services;

internal sealed record RecoveryLease(int Parent,uint OriginalState,DateTime Started,string SessionId);
internal static class Recovery
{
    public static readonly string Folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Windeck");
    public static readonly string LeasePath=Path.Combine(Folder,"session.json");
    public static readonly string HeartbeatPath=Path.Combine(Folder,"heartbeat");
    public static readonly string RecoveredPath=Path.Combine(Folder,"recovered");
    public static bool Active=>File.Exists(LeasePath);
    public static uint TaskbarState()
    { var d=new Win32.AppBarData {Size=(uint)Marshal.SizeOf<Win32.AppBarData>(),Hwnd=Win32.ExplorerTaskbars().FirstOrDefault()}; return (uint)Win32.SHAppBarMessage(4,ref d); }
    public static async Task<bool> ArmAsync()
    {
        Directory.CreateDirectory(Folder);
        Restore(false);
        foreach(var name in new[]{"ready","recovered"}) try {File.Delete(Path.Combine(Folder,name));}catch{}
        string session=Guid.NewGuid().ToString("N");
        File.WriteAllText(LeasePath,JsonSerializer.Serialize(new RecoveryLease(Environment.ProcessId,TaskbarState(),DateTime.UtcNow,session)));
        Beat();
        var process=Process.Start(new ProcessStartInfo(Environment.ProcessPath!) {
            UseShellExecute=false,CreateNoWindow=true,ArgumentList={"--guard",Environment.ProcessId.ToString(),session}
        });
        for(int i=0;i<40;i++) { if(File.Exists(Path.Combine(Folder,"ready"))&&File.ReadAllText(Path.Combine(Folder,"ready"))==session) return true; if(process==null||process.HasExited)break; await Task.Delay(50); }
        Restore(); return false;
    }
    public static void Beat() { try{File.WriteAllText(HeartbeatPath,DateTime.UtcNow.ToString("O"));}catch{} }
    private static void ShowExplorer()
    {
        foreach(var hwnd in Win32.ExplorerTaskbars()){Win32.ShowWindow(hwnd,5);Win32.SetWindowPos(hwnd,(nint)(-1),0,0,0,0,0x53);}
    }
    public static void Restore(bool markRecovered=false)
    {
        ShowExplorer();
        RecoveryLease? lease=null;
        try{if(File.Exists(LeasePath))lease=JsonSerializer.Deserialize<RecoveryLease>(File.ReadAllText(LeasePath));}catch{}
        if(lease!=null) {
            var d=new Win32.AppBarData{Size=(uint)Marshal.SizeOf<Win32.AppBarData>(),Hwnd=Win32.ExplorerTaskbars().FirstOrDefault(),Param=(nint)lease.OriginalState};
            Win32.SHAppBarMessage(10,ref d);
        }
        foreach(var hwnd in Win32.ExplorerTaskbars())Win32.ShowWindow(hwnd,5);
        Win32.PostMessage((nint)0xffff,Win32.RegisterWindowMessage("TaskbarCreated"),0,0);
        try {File.Delete(LeasePath);File.Delete(Path.Combine(Folder,"ready")); if(markRecovered) File.WriteAllText(RecoveredPath,"Barra original restaurada pelo processo de recuperação.");}catch{}
    }
    private static bool OwnsLease(int parent,string session)
    {
        try {var lease=JsonSerializer.Deserialize<RecoveryLease>(File.ReadAllText(LeasePath));return lease?.Parent==parent&&lease.SessionId==session;}
        catch{return false;}
    }
    public static int Guard(int parent,string session)
    {
        try {
            using var process=Process.GetProcessById(parent);
            if(!OwnsLease(parent,session))return 0;
            Directory.CreateDirectory(Folder);
            File.WriteAllText(Path.Combine(Folder,"ready"),session);
            while(!process.WaitForExit(1000)) {
                if(!OwnsLease(parent,session))return 0;
                if(File.Exists(HeartbeatPath)&&DateTime.UtcNow-File.GetLastWriteTimeUtc(HeartbeatPath)>TimeSpan.FromSeconds(15)) {
                    // Reveal Explorer before making any potentially blocking shell call.
                    ShowExplorer();
                    try{if(string.Equals(process.MainModule?.FileName,Environment.ProcessPath,StringComparison.OrdinalIgnoreCase)){process.Kill();process.WaitForExit(3000);}}catch{}
                    Restore(true);return 0;
                }
            }
            if(OwnsLease(parent,session))Restore(true);
        }catch{if(OwnsLease(parent,session))Restore(true);}
        return 0;
    }
}
