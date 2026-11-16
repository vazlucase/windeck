using System.Collections.Specialized;
using System.Windows;
using System.Windows.Threading;
using ManagedShell.WindowsTasks;
using Windeck.Core;
using Windeck.Native;
using Windeck.Services;
namespace Windeck;
internal sealed record Notice(DateTime Time,string Title,string Body,Action? Activate=null);
internal sealed class Runtime : IDisposable
{
    public static readonly string SettingsPath=Path.Combine(Recovery.Folder,"settings.json");
    public Preferences Preferences {get;}=Preferences.Load(SettingsPath);
    public DesktopIntegration Desktop {get;}
    public MediaService Media {get;}=new();
    public WeatherService Weather {get;}=new();
    public AudioService Audio {get;}=new();
    public List<CatalogApp> Catalog {get;private set;}=[];
    public List<Notice> Notices {get;}=[];
    public List<DeckWindow> Windows {get;}=[];
    public bool Preview {get;}
    public bool Closing {get;private set;}
    public event Action<string>? Changed;
    private readonly DispatcherTimer timer=new(){Interval=TimeSpan.FromSeconds(1)};
    private bool refreshing;
    private bool disposed;
    private readonly SemaphoreSlim replacementGate=new(1,1);
    private bool monitorRefresh;
    private NoticeWindow? toast;
    private DateTime? lastWeather;
    public Runtime(bool preview)
    {
        Preview=preview;
        if(File.Exists(Recovery.RecoveredPath)){Preferences.ReplaceTaskbar=false;Notices.Add(new(DateTime.Now,"Recuperação automática","A barra original foi restaurada. A substituição permanece desativada até você ativá-la novamente."));try{File.Delete(Recovery.RecoveredPath);}catch{}}
        Desktop=new DesktopIntegration(Preferences.PinnedTray.ToArray());
        if(Preferences.Pins.Count==0){Preferences.Pins=AppCatalog.ImportPins();if(Preferences.Pins.Count==0)Preferences.Pins=[new("Explorador","explorer.exe"),new("Configurações","ms-settings:"),new("Navegador","https://www.bing.com")];}
        Ui.Theme(Application.Current.Resources,Preferences.Theme);
        if(Preferences.PinnedTray.Count>0)Desktop.Shell.NotificationArea.SetPinnedIcons(Preferences.PinnedTray.ToArray());
        Desktop.Shell.NotificationArea.NotificationBalloonShown+=(_,e)=>{AddNotice(e.Balloon.Title,e.Balloon.Info,e.Balloon.Click);e.Handled=true;};
        Desktop.Shell.Tasks.GroupedWindows.CollectionChanged+=(_,_)=>Changed?.Invoke("tasks");
        Desktop.Shell.NotificationArea.PinnedIcons.CollectionChanged+=(_,_)=>Changed?.Invoke("tray");
        Desktop.Shell.NotificationArea.UnpinnedIcons.CollectionChanged+=(_,_)=>Changed?.Invoke("tray");
        timer.Tick+=async(_,_)=>await TickAsync();
    }
    public async Task StartAsync()
    {
        Catalog=await AppCatalog.LoadAsync();Changed?.Invoke("catalog");
        if(Preferences.ReplaceTaskbar&&!Preview)await SetReplacementAsync(true);
        timer.Start();await TickAsync();
    }
    public void RequestMonitorRefresh()=>monitorRefresh=true;
    private void ReconcileMonitors()
    {
        monitorRefresh=false;var monitors=Win32.Monitors();if(monitors.Count==0)return;
        foreach(var old in Windows.Where(w=>!monitors.Any(m=>m.Info.Device==w.MonitorDevice)).ToArray()){
            Desktop.Release(old.Handle);old.Retiring=true;Windows.Remove(old);old.Close();
        }
        foreach(var monitor in monitors){
            var existing=Windows.FirstOrDefault(w=>w.MonitorDevice==monitor.Info.Device);
            if(existing!=null)existing.SyncMonitor(monitor.Info,monitor.Scale);
            else{var window=new DeckWindow(this,monitor.Info,monitor.Scale);Windows.Add(window);window.Show();}
        }
        if(Desktop.Replacing&&Preferences.ReserveSpace)foreach(var window in Windows)Desktop.Reserve(window.Handle,window.MonitorRect);
    }
    public IEnumerable<ApplicationWindow> Tasks=>Desktop.Shell.Tasks.GroupedWindows.Cast<ApplicationWindow>().Where(w=>w.ShowInTaskbar);
    public void Save(){Preferences.Save(SettingsPath);Changed?.Invoke("preferences");}
    public void AddNotice(string title,string body,Action? activate=null)
    {
        var notice=new Notice(DateTime.Now,title,body,activate);Notices.Insert(0,notice);if(Notices.Count>60)Notices.RemoveAt(Notices.Count-1);
        Changed?.Invoke("notices");
        if(!Closing&&Windows.Count>0&&NoticeWindow.CanDisplay()){
            toast?.Close();toast=new NoticeWindow(notice,Windows[0].MonitorRect);toast.Show();
        }
    }
    public async Task SetReplacementAsync(bool value)
    {
        await replacementGate.WaitAsync();
        try{
            if(Closing)return;
            if(value){
                var first=Windows.FirstOrDefault();if(first==null)return;
                if(!await Desktop.ReplaceAsync(first.Handle,first.MonitorRect,Preferences.ReserveSpace)){AddNotice("Barra original mantida",Desktop.Error??"Não foi possível ativar a substituição.");return;}
                if(Closing){Desktop.Restore();return;}
                foreach(var window in Windows)if(Preferences.ReserveSpace)Desktop.Reserve(window.Handle,window.MonitorRect);
            }else Desktop.Restore();
            Preferences.ReplaceTaskbar=value;Save();
        }finally{replacementGate.Release();}
    }
    private async Task TickAsync()
    {
        if(Closing)return;
        if(File.Exists(Recovery.RecoveredPath)){
            Desktop.Restore();Preferences.ReplaceTaskbar=false;Save();File.Delete(Recovery.RecoveredPath);AddNotice("Recuperação automática","A barra original foi restaurada após uma interrupção.");
        }
        Recovery.Beat();if(monitorRefresh)ReconcileMonitors();Audio.Refresh();
        foreach(var reminder in Preferences.Reminders.Where(r=>!r.Done&&r.Due<=DateTime.Now).ToArray()){
            AddNotice("Lembrete",reminder.Text);
            Preferences.Reminders[Preferences.Reminders.IndexOf(reminder)]=reminder with{Done=true};Save();
        }
        Changed?.Invoke("tick");
        if(refreshing)return;
        refreshing=true;
        try{
            // Start the weather request independently so a slow network never delays the media UI.
            await Task.WhenAll(Media.RefreshAsync(),Weather.RefreshAsync(Preferences.WeatherCity,Preferences.WeatherCountry));
            if(Weather.Snapshot?.Updated!=lastWeather){lastWeather=Weather.Snapshot?.Updated;Changed?.Invoke("weather");}
            Changed?.Invoke("media");
        }catch(Exception e){App.Log(e);}finally{refreshing=false;}
    }
    public void Quit(){if(Closing)return;Closing=true;Dispose();Application.Current.Shutdown();}
    public void Dispose()
    {
        if(disposed)return;disposed=true;Closing=true;timer.Stop();toast?.Close();
        foreach(IDisposable service in new IDisposable[]{Media,Weather,Audio,Desktop})try{service.Dispose();}catch(Exception e){App.Log(e);}
        Recovery.Restore();
    }
}
