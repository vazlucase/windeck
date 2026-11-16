using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Windeck.Native;
namespace Windeck;
internal sealed class ThumbnailWindow : Window
{
    private nint thumb;
    public ThumbnailWindow(nint source,string title,Point origin)
    {
        WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;ShowInTaskbar=false;Topmost=true;Width=300;Height=205;Left=origin.X;Top=origin.Y;Title=title;Background=Ui.Brush("#141A25");
        var name=Ui.Text(title,12);name.Margin=new Thickness(12);name.VerticalAlignment=VerticalAlignment.Bottom;Content=name;
        SourceInitialized+=(_,_)=>{
            nint handle=new WindowInteropHelper(this).Handle;Win32.SetWindowLongPtrW(handle,-20,Win32.GetWindowLongPtrW(handle,-20)|(nint)0x80);
            if(Win32.DwmRegisterThumbnail(handle,source,out thumb)>=0){double s=Win32.GetDpiForWindow(handle)/96d;var p=new Win32.ThumbnailProperties{Flags=1|4|8|16,Destination=new(){Left=8,Top=8,Right=(int)(292*s),Bottom=(int)(165*s)},Opacity=255,Visible=true,ClientOnly=false};Win32.DwmUpdateThumbnailProperties(thumb,ref p);}
        };
        Closed+=(_,_)=>{if(thumb!=0){Win32.DwmUnregisterThumbnail(thumb);thumb=0;}};
        MouseLeftButtonUp+=(_,_)=>{Win32.ShowWindow(source,9);Win32.SetForegroundWindow(source);Close();};
    }
}
