using System.Diagnostics;
using System.Windows;
using Windeck.Native;
using Windeck.Services;
namespace Windeck;
public sealed class App : Application
{
    private static Mutex? instance;
    internal static Runtime? ActiveRuntime;
    [STAThread] public static int Main(string[] args)
    {
        if(args.Length==3&&args[0]=="--guard"&&int.TryParse(args[1],out int pid))return Recovery.Guard(pid,args[2]);
        if(args.Contains("--restore")){Recovery.Restore(true);return 0;}
        instance=new Mutex(true,"Local\\Windeck.Native.01",out bool created);
        if(!created){MessageBox.Show("O Windeck já está aberto. Use Ctrl+Alt+Espaço para abrir a barra.","Windeck");return 0;}
        try{if(args.Contains("--verify-layouts"))return LayoutVerification.Run(args);
            var app=new App();app.Startup+=async(_,_)=>{
                var runtime=new Runtime(args.Contains("--preview"));ActiveRuntime=runtime;
                var monitors=Win32.Monitors();if(monitors.Count==0)throw new InvalidOperationException("Nenhum monitor disponível para o Windeck.");
                foreach(var monitor in monitors){var window=new DeckWindow(runtime,monitor.Info,monitor.Scale);runtime.Windows.Add(window);window.Show();}
                await runtime.StartAsync();
            };return app.Run();}
        catch(Exception e){Recovery.Restore();Log(e);MessageBox.Show("O Windeck não conseguiu iniciar. A barra original foi restaurada.\n\n"+e.Message,"Windeck");return 1;}
        finally{instance.ReleaseMutex();instance.Dispose();}
    }
    public App(bool verification=false)
    {
        ShutdownMode=ShutdownMode.OnExplicitShutdown;
        DispatcherUnhandledException+=(_,e)=>{Log(e.Exception);ActiveRuntime?.Dispose();Recovery.Restore(true);e.Handled=true;if(!verification)MessageBox.Show("A barra do Windows foi restaurada.\n"+e.Exception.Message,"Windeck");Shutdown(1);};
        AppDomain.CurrentDomain.UnhandledException+=(_,e)=>{Log(e.ExceptionObject as Exception);Recovery.Restore();};
        Ui.InstallStyles(Resources);
        SystemParameters.StaticPropertyChanged+=(_,_)=>Dispatcher.InvokeAsync(()=>Ui.Theme(Resources,ActiveRuntime?.Preferences.Theme??"Fluent"));
    }
    internal static void Log(Exception? exception)
    {try{Directory.CreateDirectory(Recovery.Folder);File.AppendAllText(Path.Combine(Recovery.Folder,"windeck.log"),$"{DateTime.UtcNow:O} {exception}\n");}catch{}}
}
