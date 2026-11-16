using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Windeck.Native;
namespace Windeck;
internal sealed class NoticeWindow : Window
{
    private readonly DispatcherTimer timer=new(){Interval=TimeSpan.FromSeconds(7)};
    [DllImport("shell32")] private static extern int SHQueryUserNotificationState(out int state);
    public static bool CanDisplay(){try{return SHQueryUserNotificationState(out int state)<0||state==5;}catch{return false;}}
    public NoticeWindow(Notice notice,Win32.Rect monitor)
    {
        Width=365;SizeToContent=SizeToContent.Height;MaxHeight=220;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;Background=Brushes.Transparent;Topmost=true;ShowInTaskbar=false;ShowActivated=false;
        var title=Ui.Text(notice.Title,15);title.FontWeight=FontWeights.SemiBold;
        var body=Ui.Text(notice.Body,13,true);body.TextWrapping=TextWrapping.Wrap;body.MaxHeight=100;
        var close=Ui.Icon("\uE711",Close,"Fechar notificação");
        var grid=new Grid();grid.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});grid.ColumnDefinitions.Add(new(){Width=GridLength.Auto});grid.Children.Add(title);Grid.SetColumn(close,1);grid.Children.Add(close);
        var panel=Ui.Stack(Ui.Text("WINDECK",10,true),grid,body);
        if(notice.Activate!=null)panel.Children.Add(Ui.Button("Abrir",()=>{Close();notice.Activate();}));
        Content=Ui.Card(panel,new Thickness(16));
        SourceInitialized+=(_,_)=>{var hwnd=new WindowInteropHelper(this).Handle;Win32.SetWindowLongPtrW(hwnd,-20,Win32.GetWindowLongPtrW(hwnd,-20)|(nint)(0x80|0x08000000));};
        Loaded+=(_,_)=>{var hwnd=new WindowInteropHelper(this).Handle;double s=Win32.GetDpiForWindow(hwnd)/96d;Win32.SetWindowPos(hwnd,(nint)(-1),monitor.Right-(int)(385*s),monitor.Top+(int)(76*s),(int)(Width*s),(int)(ActualHeight*s),0x10);timer.Start();};
        timer.Tick+=(_,_)=>Close();MouseEnter+=(_,_)=>timer.Stop();MouseLeave+=(_,_)=>timer.Start();Closed+=(_,_)=>timer.Stop();
    }
}
