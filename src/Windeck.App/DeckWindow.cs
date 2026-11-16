using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ManagedShell.WindowsTasks;
using Windeck.Core;
using Windeck.Native;
using Windeck.Services;

namespace Windeck;
internal sealed partial class DeckWindow : Window
{
    private readonly Runtime runtime;
    private Win32.MonitorInfo monitor;
    private readonly IslandController interaction=new();
    private readonly MotionSpring width=new(190),height=new(40),pupilX=new(0,28),pupilY=new(0,28);
    private readonly Stopwatch clock=Stopwatch.StartNew();
    private double lastFrame,scale=1;
    private nint previousForeground;
    private readonly Border surface=new(){CornerRadius=new CornerRadius(25),BorderThickness=new Thickness(1),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Top,ClipToBounds=true};
    private readonly Grid rest=new(),peek=new(),expanded=new();
    private readonly TextBlock restClock=Ui.Text("",12,true),peekTitle=Ui.Text("",15),peekSubtitle=Ui.Text("",11,true);
    private readonly TextBlock headerClock=Ui.Text("",13),dateLabel=Ui.Text("",12,true),status=Ui.Text("",11,true);
    private readonly TranslateTransform eyeLeft=new(),eyeRight=new();
    private readonly Border contentHost=new(){Margin=new Thickness(16,0,16,0)};
    private readonly StackPanel taskDock=new(){Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};
    private readonly List<(FrameworkElement Element,ScaleTransform Scale)> dockTransforms=[];
    private readonly DispatcherTimer hoverTimer=new(){Interval=TimeSpan.FromMilliseconds(550)};
    private readonly DispatcherTimer configDelay=new(){Interval=TimeSpan.FromMilliseconds(400)};
    private string dockKey="";
    private TextBox? search;
    private WrapPanel? appResults;
    private StackPanel? searchFiles;
    private CancellationTokenSource? searchCancel;
    private Button? playButton,previousButton,nextButton;
    private TextBlock? mediaTitle,mediaArtist,mediaTime,volumeText,batteryText,networkText;
    private Image? mediaImage;
    private Slider? mediaProgress,volumeSlider;
    private bool updatingSlider;
    private readonly DispatcherTimer thumbnailDismiss=new(){Interval=TimeSpan.FromMilliseconds(180)};
    private ApplicationWindow? hoverTask;
    private ThumbnailWindow? thumbnail;
    private DateTime selectedDate=DateTime.Today;
    private DateTime month=DateTime.Today;
    public nint Handle=>new WindowInteropHelper(this).Handle;
    public Win32.Rect MonitorRect=>monitor.Monitor;
    internal string MonitorDevice=>monitor.Device;
    internal bool Retiring {get;set;}
    private double DeckWidth=>Math.Max(280,Math.Min(1140,monitor.Monitor.Width/scale-32));
    private double DeckHeight=>Math.Max(300,Math.Min(610,monitor.Monitor.Height/scale-44));
    public DeckWindow(Runtime runtime,Win32.MonitorInfo monitor,double initialScale=1)
    {
        this.runtime=runtime;this.monitor=monitor;scale=initialScale;
        Title="Windeck";WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;Background=Brushes.Transparent;Topmost=true;ShowInTaskbar=false;ShowActivated=false;
        FontFamily=new FontFamily("Segoe UI Variable Text, Segoe UI");
        Width=1140;Height=630;Content=surface;
        surface.SetResourceReference(Border.BackgroundProperty,"Surface");surface.SetResourceReference(Border.BorderBrushProperty,"Stroke");
        surface.Effect=new DropShadowEffect{BlurRadius=28,ShadowDepth=8,Opacity=.35,Color=Colors.Black};
        var layers=new Grid();layers.Children.Add(rest);layers.Children.Add(peek);layers.Children.Add(expanded);surface.Child=layers;
        BuildRest();BuildPeek();BuildExpanded();
        interaction.StrikeSpeed=runtime.Preferences.StrikeSpeed;interaction.GesturesEnabled=runtime.Preferences.Gestures;
        interaction.Changed+=OnStateChanged;
        runtime.Changed+=RuntimeChanged;
        surface.MouseLeftButtonUp+=(_,e)=>{if(interaction.State!=IslandState.Expanded){interaction.Expand();e.Handled=true;}};
        surface.MouseRightButtonUp+=(_,e)=>{if(interaction.State!=IslandState.Expanded){ShowDeckMenu();e.Handled=true;}};
        surface.Focusable=true;surface.KeyDown+=(_,e)=>{if(interaction.State!=IslandState.Expanded&&e.Key is Key.Enter or Key.Space){interaction.Expand();e.Handled=true;}};
        AllowDrop=true;DragEnter+=(_,_)=>interaction.Expand();
        PreviewKeyDown+=HandleKey;
        SourceInitialized+=(_,_)=>{
            var hwnd=Handle;var source=HwndSource.FromHwnd(hwnd);source?.AddHook(WindowMessages);
            Win32.SetWindowLongPtrW(hwnd,-20,Win32.GetWindowLongPtrW(hwnd,-20)|(nint)0x80);
            int dark=1;Win32.DwmSetWindowAttribute(hwnd,20,ref dark,4);
            if((monitor.Flags&1)!=0){
                if(!Win32.RegisterHotKey(hwnd,1,0x4000|1|2,0x20))runtime.AddNotice("Atalho indisponível","Ctrl+Alt+Espaço já está em uso por outro aplicativo.");
                if(!Win32.RegisterHotKey(hwnd,2,0x4000|1|2,0x08))runtime.AddNotice("Atalho indisponível","Ctrl+Alt+Backspace já está em uso. Use Windeck.exe --restore se precisar restaurar a barra.");
            }
            Position();
        };
        Loaded+=(_,_)=>{Position();ShowPage(DeckPage.Home);OnStateChanged(IslandState.Rest);CompositionTarget.Rendering+=OnFrame;RefreshTasks();UpdateLabels();};
        Deactivated+=(_,_)=>{if(interaction.State==IslandState.Expanded&&!IsKeyboardFocusWithin&&!surface.IsMouseOver)interaction.Collapse();};
        Closing+=(_,e)=>{if(!runtime.Closing&&!Retiring){e.Cancel=true;runtime.Quit();}};
        hoverTimer.Tick+=(_,_)=>{
            hoverTimer.Stop();if(hoverTask==null||interaction.State!=IslandState.Expanded)return;
            thumbnail?.Close();var origin=PointToScreen(new Point(Math.Max(0,(Width-300)/2),Math.Min(height.Value-225,320)));
            var preview=new ThumbnailWindow(hoverTask.Handle,hoverTask.Title,new Point(origin.X/scale,origin.Y/scale)){ShowActivated=false,Owner=this};thumbnail=preview;
            preview.MouseEnter+=(_,_)=>thumbnailDismiss.Stop();preview.MouseLeave+=(_,_)=>thumbnailDismiss.Start();
            preview.Closed+=(_,_)=>{if(ReferenceEquals(thumbnail,preview))thumbnail=null;};preview.Show();
        };
        Closed+=(_,_)=>{CompositionTarget.Rendering-=OnFrame;runtime.Changed-=RuntimeChanged;hoverTimer.Stop();thumbnailDismiss.Stop();configDelay.Stop();searchCancel?.Cancel();thumbnail?.Close();Win32.UnregisterHotKey(Handle,1);Win32.UnregisterHotKey(Handle,2);};
        thumbnailDismiss.Tick+=(_,_)=>{thumbnailDismiss.Stop();if(hoverTask==null&&thumbnail is {IsMouseOver:false})thumbnail.Hide();};
        configDelay.Tick+=(_,_)=>{configDelay.Stop();runtime.Save();};
    }
    internal void SyncMonitor(Win32.MonitorInfo info,double dpiScale)
    {
        bool primaryChanged=(monitor.Flags&1)!=(info.Flags&1);
        monitor=info;scale=dpiScale;
        if(primaryChanged){Win32.UnregisterHotKey(Handle,1);Win32.UnregisterHotKey(Handle,2);if((info.Flags&1)!=0){Win32.RegisterHotKey(Handle,1,0x4003,0x20);Win32.RegisterHotKey(Handle,2,0x4003,0x08);}}
        Position();if(interaction.State==IslandState.Expanded){width.Target=DeckWidth;height.Target=DeckHeight;}
    }
    private void Position()
    {
        var monitors=Win32.Monitors();var found=monitors.FirstOrDefault(m=>m.Info.Device==monitor.Device);scale=found.Scale>0?found.Scale:1;
        Width=DeckWidth+64;Height=DeckHeight+40;
        Win32.SetWindowPos(Handle,(nint)(-1),monitor.Monitor.Left+(monitor.Monitor.Width-(int)(Width*scale))/2,monitor.Monitor.Top+(int)(10*scale),(int)(Width*scale),(int)(Height*scale),0x10);
    }
    private nint WindowMessages(nint hwnd,int message,nint w,nint l,ref bool handled)
    {
        if(message==0x0312){handled=true;if(w==2){runtime.Desktop.Restore();runtime.Preferences.ReplaceTaskbar=false;runtime.Save();interaction.Collapse();}else{
            Win32.GetCursorPos(out var point);var target=runtime.Windows.FirstOrDefault(window=>window.MonitorRect.Left<=point.X&&window.MonitorRect.Right>=point.X&&window.MonitorRect.Top<=point.Y&&window.MonitorRect.Bottom>=point.Y)??this;
            target.interaction.Toggle();target.Activate();
        }}
        if(message==0x02e0)Dispatcher.BeginInvoke(Position);
        if(message==0x007e)runtime.RequestMonitorRefresh();
        if((uint)message==Win32.RegisterWindowMessage("Windeck.AppBar")&&runtime.Desktop.Replacing&&runtime.Preferences.ReserveSpace)Dispatcher.BeginInvoke(()=>runtime.Desktop.Reserve(Handle,monitor.Monitor));
        return 0;
    }
    private void BuildRest()
    {
        var dot=new Border{Width=7,Height=7,CornerRadius=new CornerRadius(4),Background=Ui.Brush("#8FBAFF"),Margin=new Thickness(0,0,10,0)};
        var name=Ui.Text("windeck",13);name.FontWeight=FontWeights.SemiBold;name.Margin=new Thickness(0,0,14,0);
        var row=Ui.Row(dot,name,restClock);row.HorizontalAlignment=HorizontalAlignment.Center;row.VerticalAlignment=VerticalAlignment.Center;rest.Children.Add(row);
    }
    private void BuildPeek()
    {
        var row=new Grid{Margin=new Thickness(22,14,22,14)};row.ColumnDefinitions.Add(new(){Width=new GridLength(85)});row.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});
        var eyes=new Grid{Width=64,Height=38,HorizontalAlignment=HorizontalAlignment.Left};
        foreach(var (offset,transform) in new[]{(0d,eyeLeft),(31d,eyeRight)}){
            var eye=new Border{Width=24,Height=32,CornerRadius=new CornerRadius(13),Background=Brushes.White,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(offset,0,0,0)};
            eye.Child=new Border{Width=9,Height=14,Background=Ui.Brush("#0F1118"),CornerRadius=new CornerRadius(5),RenderTransform=transform};eyes.Children.Add(eye);
        }
        row.Children.Add(eyes);var labels=Ui.Stack(peekTitle,peekSubtitle);labels.VerticalAlignment=VerticalAlignment.Center;Grid.SetColumn(labels,1);row.Children.Add(labels);peek.Children.Add(row);
    }
    private void BuildExpanded()
    {
        expanded.RowDefinitions.Add(new(){Height=new GridLength(68)});expanded.RowDefinitions.Add(new(){Height=new GridLength(48)});expanded.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});expanded.RowDefinitions.Add(new(){Height=new GridLength(92)});
        var header=new Grid{Margin=new Thickness(24,12,20,8)};header.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});header.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
        var brand=Ui.Row(Ui.Text("windeck",24),Ui.Text("  /  seu espaço, em movimento",12,true));header.Children.Add(brand);
        var actions=Ui.Row(headerClock,Ui.Icon("\uE713",()=>Navigate(DeckPage.Settings),"Configurações"),Ui.Icon("\uE70D",()=>interaction.Collapse(),"Recolher · Escape"));Grid.SetColumn(actions,1);header.Children.Add(actions);expanded.Children.Add(header);
        var nav=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(20,0,20,4)};
        foreach(var (text,page) in new[]{("Visão geral",DeckPage.Home),("Aplicativos",DeckPage.Apps),("Janelas",DeckPage.Windows),("Bandeja",DeckPage.Tray),("Música",DeckPage.Media),("Agenda",DeckPage.Calendar),("Controle",DeckPage.Controls)})nav.Children.Add(Ui.Button(text,()=>Navigate(page)));
        var navScroll=new ScrollViewer{Content=nav,HorizontalScrollBarVisibility=ScrollBarVisibility.Hidden,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled};Grid.SetRow(navScroll,1);expanded.Children.Add(navScroll);
        Grid.SetRow(contentHost,2);expanded.Children.Add(contentHost);
        var bottom=new Grid{Margin=new Thickness(20,8,20,8)};bottom.RowDefinitions.Add(new(){Height=new GridLength(58)});bottom.RowDefinitions.Add(new(){Height=new GridLength(18)});
        var dockScroll=new ScrollViewer{Content=taskDock,HorizontalScrollBarVisibility=ScrollBarVisibility.Hidden,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled};bottom.Children.Add(dockScroll);
        var foot=new Grid();foot.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});foot.ColumnDefinitions.Add(new(){Width=GridLength.Auto});foot.Children.Add(status);Grid.SetColumn(dateLabel,1);foot.Children.Add(dateLabel);Grid.SetRow(foot,1);bottom.Children.Add(foot);Grid.SetRow(bottom,3);expanded.Children.Add(bottom);
    }
    private void Navigate(DeckPage page){interaction.Expand(page);ShowPage(page);}
    private void OnStateChanged(IslandState state)
    {
        width.Target=state switch{IslandState.Rest=>190,IslandState.Peek=>Math.Min(360,DeckWidth),_=>DeckWidth};
        height.Target=state switch{IslandState.Rest=>40,IslandState.Peek=>84,_=>DeckHeight};
        rest.Visibility=state==IslandState.Rest?Visibility.Visible:Visibility.Collapsed;
        peek.Visibility=state==IslandState.Peek?Visibility.Visible:Visibility.Collapsed;
        expanded.Visibility=state==IslandState.Expanded?Visibility.Visible:Visibility.Collapsed;
        if(state==IslandState.Expanded){var foreground=Win32.GetForegroundWindow();if(foreground!=Handle)previousForeground=foreground;ShowPage(interaction.Page);RefreshTasks();Activate();Win32.SetForegroundWindow(Handle);}
        else{searchCancel?.Cancel();thumbnail?.Hide();if(Win32.GetForegroundWindow()==Handle&&Win32.IsWindow(previousForeground))Win32.SetForegroundWindow(previousForeground);}
        if(runtime.Preferences.ReduceMotion){width.Snap(width.Target);height.Snap(height.Target);}
        PublishTrayPosition();
    }
    private void OnFrame(object? sender,EventArgs e)
    {
        if(!IsVisible)return;
        double now=clock.Elapsed.TotalSeconds,dt=lastFrame==0?1d/60:now-lastFrame;lastFrame=now;
        Win32.GetCursorPos(out var pointer);
        var local=PointFromScreen(new Point(pointer.X,pointer.Y));
        var rect=new SurfaceRect((Width-width.Value)/2,0,width.Value,height.Value);
        interaction.Update(new(local.X,local.Y,now),rect,!IsVisible||Opacity<.05);
        bool reduced=runtime.Preferences.ReduceMotion||!SystemParameters.ClientAreaAnimation;
        surface.Width=width.Step(dt,reduced);surface.Height=height.Step(dt,reduced);
        pupilX.Target=Math.Clamp((local.X-Width/2)/100,-5,5);pupilY.Target=Math.Clamp((local.Y-40)/130,-7,7);
        eyeLeft.X=eyeRight.X=pupilX.Step(dt,reduced);eyeLeft.Y=eyeRight.Y=pupilY.Step(dt,reduced);
        foreach(var (element,transform) in dockTransforms){
            double target=1;
            if(!reduced&&runtime.Preferences.Theme=="Dock"&&taskDock.IsMouseOver){var p=element.PointToScreen(new Point(element.ActualWidth/2,element.ActualHeight/2));double distance=Math.Abs(p.X-pointer.X)/scale;target=1+.25*Math.Exp(-distance*distance/4000);}
            transform.ScaleX=transform.ScaleY=target;
        }
        if(runtime.Preferences.HideOnFullscreen&&now%1<dt)CheckFullscreen();
        // An outside click dismisses the expanded surface without swallowing that click.
        if(interaction.State==IslandState.Expanded&&!rect.Contains(local.X,local.Y)&&Win32.GetAsyncKeyState(1)<0&&!IsKeyboardFocusWithin)interaction.Collapse();
    }
    private void CheckFullscreen()
    {
        nint foreground=Win32.GetForegroundWindow();
        if(!runtime.Preferences.HideOnFullscreen||foreground==0||foreground==Handle){Opacity=1;return;}
        var cls=new System.Text.StringBuilder(128);Win32.GetClassName(foreground,cls,cls.Capacity);
        if(cls.ToString() is "Progman" or "WorkerW"){Opacity=1;return;}
        bool full=Win32.GetWindowRect(foreground,out var r)&&r.Left<=monitor.Monitor.Left&&r.Top<=monitor.Monitor.Top&&r.Right>=monitor.Monitor.Right&&r.Bottom>=monitor.Monitor.Bottom;
        Opacity=full?0:1;
    }
    private void HandleKey(object sender,KeyEventArgs e)
    {
        if(e.Key==Key.Escape){interaction.Collapse();e.Handled=true;}
        if(e.Key==Key.F&&Keyboard.Modifiers==ModifierKeys.Control){Navigate(DeckPage.Apps);search?.Focus();e.Handled=true;}
        if(e.Key==Key.D&&Keyboard.Modifiers==ModifierKeys.Control){SystemAction("desktop");e.Handled=true;}
    }
    private void RuntimeChanged(string reason)
    {
        if(runtime.Closing)return;
        if(!Dispatcher.CheckAccess()){Dispatcher.BeginInvoke(()=>RuntimeChanged(reason));return;}
        if(reason=="preferences"){
            Ui.Theme(Application.Current.Resources,runtime.Preferences.Theme);surface.CornerRadius=new CornerRadius(runtime.Preferences.Theme=="Dock"?32:25);interaction.GesturesEnabled=runtime.Preferences.Gestures;interaction.StrikeSpeed=runtime.Preferences.StrikeSpeed;
            if(interaction.Page!=DeckPage.Settings)ShowPage(interaction.Page);
        }
        if(reason is "tasks" or "preferences")RefreshTasks();
        if(reason=="tasks"&&interaction.Page==DeckPage.Windows&&interaction.State==IslandState.Expanded)ShowPage(DeckPage.Windows);
        if(reason=="tray"&&interaction.Page==DeckPage.Tray&&interaction.State==IslandState.Expanded)ShowPage(DeckPage.Tray);
        if(reason is "weather" or "notices"&&interaction.State==IslandState.Expanded&&interaction.Page==DeckPage.Home)ShowPage(DeckPage.Home);
        if(reason=="catalog"&&interaction.Page==DeckPage.Apps)RenderApps(search?.Text??"");
        UpdateLabels();
    }
    private void UpdateLabels()
    {
        restClock.Text=DateTime.Now.ToString("HH:mm");headerClock.Text=DateTime.Now.ToString("HH:mm");dateLabel.Text=DateTime.Now.ToString("dddd, dd MMMM",System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
        var m=runtime.Media.Snapshot;
        peekTitle.Text=m.hasTitle();peekSubtitle.Text=m.hasSubtitle();
        status.Text=$"{runtime.Tasks.Count()} janelas · {(runtime.Desktop.Replacing?"Substituindo a barra do Windows":"Modo de prévia")} · Ctrl+Alt+Espaço";
        if(mediaTitle!=null)mediaTitle.Text=m.Title;if(mediaArtist!=null)mediaArtist.Text=m.Artist;
        if(mediaImage!=null)mediaImage.Source=m.Cover;
        if(playButton!=null){playButton.Content=m.Playing?"\uE769":"\uE768";playButton.IsEnabled=m.CanPlay;}
        if(previousButton!=null)previousButton.IsEnabled=m.CanPrevious;if(nextButton!=null)nextButton.IsEnabled=m.CanNext;if(mediaProgress!=null)mediaProgress.IsEnabled=m.CanSeek;
        if(mediaTime!=null)mediaTime.Text=$"{TimeSpan.FromSeconds(m.Position):mm\\:ss}  /  {TimeSpan.FromSeconds(m.Duration):mm\\:ss}";
        updatingSlider=true;
        if(mediaProgress!=null&&!mediaProgress.IsMouseCaptureWithin)mediaProgress.Value=m.Duration>0?m.Position/m.Duration:0;
        if(volumeSlider!=null&&!volumeSlider.IsMouseCaptureWithin)volumeSlider.Value=runtime.Audio.Volume;
        updatingSlider=false;
        if(volumeText!=null)volumeText.Text=runtime.Audio.Available?$"{(runtime.Audio.Muted?"Mudo · ":"")}{runtime.Audio.Volume:P0}":"Áudio indisponível";
        if(batteryText!=null)batteryText.Text=BatteryDescription();
        if(networkText!=null)networkText.Text=NetworkDescription();
    }
    private void ShowPage(DeckPage page)
    {
        mediaTitle=mediaArtist=mediaTime=volumeText=batteryText=networkText=null;mediaImage=null;playButton=previousButton=nextButton=null;mediaProgress=volumeSlider=null;search=null;appResults=null;searchFiles=null;
        contentHost.Child=page switch{DeckPage.Apps=>BuildApps(),DeckPage.Windows=>BuildWindows(),DeckPage.Tray=>BuildTray(),DeckPage.Media=>Ui.Scroll(BuildMusic(true)),DeckPage.Calendar=>BuildCalendarPage(),DeckPage.Controls=>BuildControls(),DeckPage.Settings=>BuildSettings(),_=>BuildHome()};
        if(!runtime.Preferences.ReduceMotion&&SystemParameters.ClientAreaAnimation&&contentHost.Child is UIElement child)child.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(160)));
        UpdateLabels();
    }
    private void PublishTrayPosition()
    {
        if(Handle==0)return;
        var origin=PointToScreen(new Point((Width-width.Target)/2,0));
        runtime.Desktop.Shell.NotificationArea.SetTrayHostSizeData(new(){edge=(ManagedShell.Interop.NativeMethods.ABEdge)1,rc=new(){Left=(int)origin.X,Top=(int)origin.Y,Right=(int)(origin.X+width.Target*scale),Bottom=(int)(origin.Y+height.Target*scale)}});
    }
    private void ShowDeckMenu()
    {
        var menu=new ContextMenu();
        foreach(var (text,action) in new (string,Action)[]{("Abrir Windeck",()=>interaction.Expand()),("Configurações",()=>Navigate(DeckPage.Settings)),("Restaurar barra do Windows",()=>{runtime.Desktop.Restore();runtime.Preferences.ReplaceTaskbar=false;runtime.Save();}), ("Sair do Windeck",runtime.Quit)}){var item=new MenuItem{Header=text};item.Click+=(_,_)=>action();menu.Items.Add(item);}
        menu.IsOpen=true;
    }
    private void SystemAction(string name)
    {
        interaction.Collapse();
        Dispatcher.BeginInvoke(async()=>{await Task.Delay(100);switch(name){
            case "start":Win32.Shortcut(0x5b);break;case "search":Win32.Shortcut(0x5b,0x53);break;case "tasks":Win32.Shortcut(0x5b,0x09);break;
            case "notifications":Win32.Shortcut(0x5b,0x4e);break;case "quick":Win32.Shortcut(0x5b,0x41);break;case "widgets":Win32.Shortcut(0x5b,0x57);break;
            case "desktop":Win32.Shortcut(0x5b,0x44);break;case "input":Win32.Shortcut(0x5b,0x20);break;case "winx":Win32.Shortcut(0x5b,0x58);break;
            case "clipboard":Win32.Shortcut(0x5b,0x56);break;case "emoji":Win32.Shortcut(0x5b,0xbe);break;case "cast":Win32.Shortcut(0x5b,0x4b);break;
            case "project":Win32.Shortcut(0x5b,0x50);break;case "capture":Win32.Shortcut(0x5b,0x10,0x53);break;
        }});
    }
    private static string BatteryDescription()
    {
        if(!Win32.GetSystemPowerStatus(out var s))return "Bateria indisponível";
        if(s.BatteryFlag==255)return "Bateria indisponível";
        if((s.BatteryFlag&128)!=0)return "Conectado · sem bateria";
        return s.Percent<=100?$"{s.Percent}% · {(s.Ac==1?"conectado":s.Ac==0?"usando bateria":"energia desconhecida")}":"Bateria sem leitura";
    }
    private static string NetworkDescription()
    {
        try{return Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile()?.ProfileName??"Sem conexão";}catch{return "Rede indisponível";}
    }
}
internal static class MediaLabels
{
    public static string hasTitle(this MediaSnapshot m)=>m.HasSession&&!string.IsNullOrWhiteSpace(m.Title)?m.Title:"Estou de olho no seu fluxo";
    public static string hasSubtitle(this MediaSnapshot m)=>m.HasSession?m.Artist:"Clique para abrir · ou faça um gesto para cima";
}
