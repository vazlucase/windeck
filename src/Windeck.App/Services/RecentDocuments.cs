using System.Runtime.InteropServices;
namespace Windeck.Services;
internal static class RecentDocuments
{
    public static IReadOnlyList<(string Name,string Path)> Get(string appId)
    {
        if(string.IsNullOrWhiteSpace(appId))return [];
        var result=new List<(string,string)>();ILists? lists=null;IObjects? objects=null;
        try{
            lists=(ILists)Activator.CreateInstance(Type.GetTypeFromCLSID(new("86bec222-30f2-47e0-9f25-60d11cd75c28"))!)!;
            lists.SetAppID(appId);var iid=typeof(IObjects).GUID;lists.GetList(0,8,ref iid,out objects);objects.GetCount(out uint count);
            for(uint i=0;i<Math.Min(count,8);i++){
                object? item=null;
                try{var si=typeof(IShellItem).GUID;objects.GetAt(i,ref si,out item);var shell=(IShellItem)item;
                    shell.GetDisplayName(0,out var label);string name;try{name=Marshal.PtrToStringUni(label)??"Documento";}finally{Marshal.FreeCoTaskMem(label);}
                    shell.GetDisplayName(0x80058000,out var path);try{string? target=Marshal.PtrToStringUni(path);if(!string.IsNullOrEmpty(target))result.Add((name,target));}finally{Marshal.FreeCoTaskMem(path);}
                }catch{}finally{if(item!=null&&Marshal.IsComObject(item))Marshal.ReleaseComObject(item);}
            }
        }catch{}finally{if(objects!=null)Marshal.ReleaseComObject(objects);if(lists!=null)Marshal.ReleaseComObject(lists);}
        return result;
    }
    [ComImport,Guid("3c594f9f-9f30-47a1-979a-c9e83d3d0a06"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface ILists{
        void SetAppID([MarshalAs(UnmanagedType.LPWStr)]string id);void GetList(int type,uint count,ref Guid iid,out IObjects list);
    }
    [ComImport,Guid("92CA9DCD-5622-4bba-A805-5E9F541BD8C9"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface IObjects{
        void GetCount(out uint count);void GetAt(uint index,ref Guid iid,[MarshalAs(UnmanagedType.IUnknown)]out object item);
    }
    [ComImport,Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface IShellItem{
        void BindToHandler(nint context,ref Guid handler,ref Guid iid,out nint result);void GetParent(out IShellItem parent);void GetDisplayName(uint flags,out nint name);void GetAttributes(uint mask,out uint attributes);void Compare(IShellItem other,uint hint,out int order);
    }
}
