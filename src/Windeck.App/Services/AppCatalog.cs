using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows;
using System.Windows.Interop;
using Windeck.Core;
using Windeck.Native;
namespace Windeck.Services;
internal sealed record CatalogApp(string Name,string Target,ImageSource? Icon);
internal static class AppCatalog
{
    public static Task<List<CatalogApp>> LoadAsync()
    {
        var completion=new TaskCompletionSource<List<CatalogApp>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread=new Thread(()=>{
            var result=new List<CatalogApp>();
            object? shell=null,folder=null,items=null;
            try{
                shell=Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application")!);
                folder=((dynamic)shell!).NameSpace("shell:AppsFolder");items=((dynamic)folder!).Items();
                foreach(dynamic item in (dynamic)items!){
                    try{string name=item.Name;string path=item.Path;if(!string.IsNullOrWhiteSpace(name))result.Add(new(name,"shell:AppsFolder\\"+path,GetShellIcon("shell:AppsFolder\\"+path)));}catch{}
                    finally{if(Marshal.IsComObject(item))Marshal.ReleaseComObject(item);}
                }
            }catch(Exception e){App.Log(e);}
            finally{foreach(var com in new[]{items,folder,shell})if(com!=null&&Marshal.IsComObject(com))Marshal.FinalReleaseComObject(com);}
            foreach(var root in new[]{Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu)}){
                try{foreach(var file in Directory.EnumerateFiles(root,"*.lnk",SearchOption.AllDirectories))result.Add(new(Path.GetFileNameWithoutExtension(file),file,GetIcon(file)));}catch{}
            }
            completion.SetResult(result.DistinctBy(x=>x.Name,StringComparer.CurrentCultureIgnoreCase).OrderBy(x=>x.Name,StringComparer.CurrentCultureIgnoreCase).ToList());
        });
        thread.IsBackground=true;thread.SetApartmentState(ApartmentState.STA);thread.Start();return completion.Task;
    }
    public static List<PinnedApp> ImportPins()
    {
        string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Microsoft","Internet Explorer","Quick Launch","User Pinned","TaskBar");
        try{return Directory.EnumerateFiles(root,"*.lnk").Select(f=>new PinnedApp(Path.GetFileNameWithoutExtension(f),f)).ToList();}catch{return [];}
    }
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] struct FileInfo { public nint Icon;public int Index;public uint Attributes;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=260)]public string Display;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=80)]public string Type; }
    [DllImport("shell32",CharSet=CharSet.Unicode)]static extern nint SHGetFileInfo(string path,uint attributes,out FileInfo info,uint size,uint flags);
    [DllImport("user32")]static extern bool DestroyIcon(nint icon);
    private static ImageSource? GetShellIcon(string path)
    {
        try {using var item=new ManagedShell.ShellFolders.ShellItem(path);item.AllowAsync=false;var icon=item.LargeIcon;icon?.Freeze();return icon;}catch{return null;}
    }
    public static ImageSource? GetIcon(string path)
    {
        try{SHGetFileInfo(path,0,out var info,(uint)Marshal.SizeOf<FileInfo>(),0x100);if(info.Icon==0)return null;
            try{var image=Imaging.CreateBitmapSourceFromHIcon(info.Icon,Int32Rect.Empty,System.Windows.Media.Imaging.BitmapSizeOptions.FromWidthAndHeight(40,40));image.Freeze();return image;}finally{DestroyIcon(info.Icon);}
        }catch{return null;}
    }
    private static readonly Dictionary<string,string> resolvedPins=new(StringComparer.OrdinalIgnoreCase);
    public static string ResolveTarget(string target)
    {
        if(!target.EndsWith(".lnk",StringComparison.OrdinalIgnoreCase))return target;
        if(resolvedPins.TryGetValue(target,out string? cached))return cached;
        object? shell=null,shortcut=null;
        try{shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!);shortcut=((dynamic)shell!).CreateShortcut(target);string path=((dynamic)shortcut).TargetPath;resolvedPins[target]=string.IsNullOrWhiteSpace(path)?target:path;}
        catch{resolvedPins[target]=target;}
        finally{foreach(var com in new[]{shortcut,shell})if(com!=null&&Marshal.IsComObject(com))Marshal.ReleaseComObject(com);}
        return resolvedPins[target];
    }
    public static void Launch(string target,IEnumerable<string>? files=null)
    {
        if(target.StartsWith("shell:AppsFolder\\",StringComparison.Ordinal)){
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe"){UseShellExecute=true,ArgumentList={target}});return;
        }
        var info=new System.Diagnostics.ProcessStartInfo(target){UseShellExecute=true};
        if(files!=null)foreach(var file in files)info.ArgumentList.Add(file);
        System.Diagnostics.Process.Start(info);
    }
}
