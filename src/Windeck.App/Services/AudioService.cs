using System.Runtime.InteropServices;
namespace Windeck.Services;
internal sealed class AudioService : IDisposable
{
    private IEndpointVolume? endpoint;
    private string? endpointId;
    public float Volume {get;private set;}
    public bool Muted {get;private set;}
    public bool Available {get;private set;}
    public void Refresh()
    {
        try{
            var enumerator=(IDeviceEnumerator)new DeviceEnumerator();
            try{
                enumerator.GetDefaultAudioEndpoint(0,1,out var device);
                try{
                    device.GetId(out var idPointer);string? id;
                    try{id=Marshal.PtrToStringUni(idPointer);}finally{Marshal.FreeCoTaskMem(idPointer);}
                    if(endpoint==null||id!=endpointId){Release();var iid=typeof(IEndpointVolume).GUID;device.Activate(ref iid,23,0,out var obj);endpoint=(IEndpointVolume)obj;endpointId=id;}
                }finally{Marshal.ReleaseComObject(device);}
            }finally{Marshal.ReleaseComObject(enumerator);}
            endpoint.GetMasterVolumeLevelScalar(out var v);endpoint.GetMute(out bool muted);Volume=v;Muted=muted;Available=true;
        }catch{Available=false;Release();}
    }
    public void Set(double value){try{Guid context=Guid.Empty;endpoint?.SetMasterVolumeLevelScalar((float)Math.Clamp(value,0,1),ref context);Refresh();}catch{}}
    public void ToggleMute(){try{Guid context=Guid.Empty;endpoint?.SetMute(!Muted,ref context);Refresh();}catch{}}
    private void Release(){if(endpoint!=null){Marshal.ReleaseComObject(endpoint);endpoint=null;}}
    public void Dispose()=>Release();
    [ComImport,Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]class DeviceEnumerator{}
    [ComImport,Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface IDeviceEnumerator{
        void EnumAudioEndpoints(int f,uint m,out nint e);void GetDefaultAudioEndpoint(int f,int r,out IDevice device);void GetDevice([MarshalAs(UnmanagedType.LPWStr)]string id,out IDevice device);void RegisterEndpointNotificationCallback(nint c);void UnregisterEndpointNotificationCallback(nint c);
    }
    [ComImport,Guid("D666063F-1587-4E43-81F1-B948E807363F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface IDevice{
        void Activate(ref Guid iid,uint cls,nint parameters,[MarshalAs(UnmanagedType.IUnknown)]out object obj);void OpenPropertyStore(uint s,out nint p);void GetId(out nint id);void GetState(out uint state);
    }
    [ComImport,Guid("5CDF2C82-841E-4546-9722-0CF74078229A"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface IEndpointVolume{
        void RegisterControlChangeNotify(nint n);void UnregisterControlChangeNotify(nint n);void GetChannelCount(out uint c);
        void SetMasterVolumeLevel(float v,ref Guid g);void SetMasterVolumeLevelScalar(float v,ref Guid g);void GetMasterVolumeLevel(out float v);void GetMasterVolumeLevelScalar(out float v);
        void SetChannelVolumeLevel(uint c,float v,ref Guid g);void SetChannelVolumeLevelScalar(uint c,float v,ref Guid g);void GetChannelVolumeLevel(uint c,out float v);void GetChannelVolumeLevelScalar(uint c,out float v);
        void SetMute([MarshalAs(UnmanagedType.Bool)]bool mute,ref Guid g);void GetMute([MarshalAs(UnmanagedType.Bool)]out bool mute);void GetVolumeStepInfo(out uint step,out uint count);void VolumeStepUp(ref Guid g);void VolumeStepDown(ref Guid g);void QueryHardwareSupport(out uint m);void GetVolumeRange(out float a,out float b,out float c);
    }
}
