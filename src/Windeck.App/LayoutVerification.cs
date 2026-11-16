using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windeck.Core;
using Windeck.Native;

namespace Windeck;

// Used by the Windows build to exercise the actual WPF views without enabling replacement.
internal static class LayoutVerification
{
    public static int Run(string[] args)
    {
        int index=Array.IndexOf(args,"--verify-layouts");
        string output=Path.GetFullPath(index+1<args.Length?args[index+1]:"artifacts/native-layouts");
        Directory.CreateDirectory(output);
        var app=new App(verification:true);
        app.Startup+=async(_,_)=>{
            Runtime? runtime=null;
            int result=1;
            try{
                runtime=new Runtime(preview:true);App.ActiveRuntime=runtime;
                var monitor=Win32.Monitors().FirstOrDefault();
                if(monitor.Info.Monitor.Width<=0)throw new InvalidOperationException("A sessão Windows não possui monitor.");
                var window=new DeckWindow(runtime,monitor.Info,monitor.Scale);
                runtime.Windows.Add(window);window.Show();
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                var files=new List<string>();
                foreach(string theme in new[]{"Fluent","Dock"}){
                    runtime.Preferences.Theme=theme;Ui.Theme(app.Resources,theme);
                    foreach(var page in Enum.GetValues<DeckPage>()){
                        string name=$"{theme.ToLowerInvariant()}-{page.ToString().ToLowerInvariant()}.png";
                        window.CaptureLayout(page,Path.Combine(output,name));files.Add(name);
                        await Dispatcher.Yield(DispatcherPriority.Background);
                    }
                }
                File.WriteAllText(Path.Combine(output,"verification.json"),JsonSerializer.Serialize(new{
                    status="passed",kind="native-wpf-layout-smoke-test",os=Environment.OSVersion.ToString(),
                    replacementEnabled=runtime.Desktop.Replacing,layouts=files,
                    trayInitializationError=runtime.Desktop.Error,
                    note="Construction and rendering only; interactive taskbar parity is not certified."
                },new JsonSerializerOptions{WriteIndented=true}));
                if(runtime.Desktop.Replacing)throw new InvalidOperationException("A verificação não deve substituir a barra.");
                result=0;
            }catch(Exception exception){App.Log(exception);File.WriteAllText(Path.Combine(output,"failure.txt"),exception.ToString());}
            finally{runtime?.Dispose();app.Shutdown(result);}
        };
        return app.Run();
    }
}

internal sealed partial class DeckWindow
{
    internal void CaptureLayout(DeckPage page,string path)
    {
        interaction.Expand(page);ShowPage(page);RefreshTasks();UpdateLabels();
        width.Snap(DeckWidth);height.Snap(DeckHeight);
        surface.Width=DeckWidth;surface.Height=DeckHeight;
        surface.CornerRadius=new CornerRadius(runtime.Preferences.Theme=="Dock"?32:25);
        UpdateLayout();
        var visual=new DrawingVisual();
        using(var drawing=visual.RenderOpen())
            drawing.DrawRectangle(new VisualBrush(surface){Stretch=Stretch.Fill},null,new Rect(0,0,DeckWidth,DeckHeight));
        var bitmap=new RenderTargetBitmap((int)Math.Ceiling(DeckWidth),(int)Math.Ceiling(DeckHeight),96,96,PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream=File.Create(path);encoder.Save(stream);
    }
}
