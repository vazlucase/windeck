using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Media.Control;
using Windows.Storage.Streams;
namespace Windeck.Services;
internal sealed record MediaSnapshot(string Title,string Artist,bool Playing,bool HasSession,double Position,double Duration,ImageSource? Cover,bool CanPlay=false,bool CanPrevious=false,bool CanNext=false,bool CanSeek=false);
internal sealed class MediaService : IDisposable
{
    private readonly CancellationTokenSource stop=new();
    private GlobalSystemMediaTransportControlsSessionManager? manager;
    private GlobalSystemMediaTransportControlsSession? current;
    private string artworkKey="";
    private ImageSource? cover;
    private int busy;
    public MediaSnapshot Snapshot {get;private set;}=new("Sem reprodução","Abra seu player de música",false,false,0,0,null);
    public async Task RefreshAsync()
    {
        if(Interlocked.Exchange(ref busy,1)!=0)return;
        try{
            manager ??= await GlobalSystemMediaTransportControlsSessionManager.RequestAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5),stop.Token);
            current=manager.GetSessions().FirstOrDefault(s=>s.GetPlaybackInfo().PlaybackStatus==GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)??manager.GetCurrentSession();
            if(current==null){Snapshot=new("Sem reprodução","Abra seu player de música",false,false,0,0,null);return;}
            var properties=await current.TryGetMediaPropertiesAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(4),stop.Token);
            var timeline=current.GetTimelineProperties();
            string key=current.SourceAppUserModelId+properties.Title+properties.Artist;
            if(artworkKey!=key){
                artworkKey=key;cover=null;
                try { if(properties.Thumbnail!=null){
                    using var stream=await properties.Thumbnail.OpenReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(3),stop.Token);
                    uint size=(uint)Math.Min(stream.Size,2*1024*1024);
                    using var reader=new DataReader(stream.GetInputStreamAt(0));
                    await reader.LoadAsync(size).AsTask().WaitAsync(TimeSpan.FromSeconds(3),stop.Token);
                    var bytes=new byte[size];reader.ReadBytes(bytes);
                    using var memory=new MemoryStream(bytes);
                    var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.StreamSource=memory;image.DecodePixelWidth=180;image.EndInit();image.Freeze();cover=image;
                }} catch(Exception e){cover=null;App.Log(e);}
            }
            var playback=current.GetPlaybackInfo();bool playing=playback.PlaybackStatus==GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            double duration=Math.Max(0,(timeline.EndTime-timeline.StartTime).TotalSeconds);
            double position=Math.Max(0,(timeline.Position-timeline.StartTime).TotalSeconds);
            if(playing)position+=Math.Max(0,(DateTimeOffset.UtcNow-timeline.LastUpdatedTime).TotalSeconds);
            if(duration>0)position=Math.Min(position,duration);
            var controls=playback.Controls;
            Snapshot=new(string.IsNullOrWhiteSpace(properties.Title)?"Sem título":properties.Title,string.IsNullOrWhiteSpace(properties.Artist)?"Sessão de mídia do Windows":properties.Artist,
                playing,true,position,duration,cover,controls.IsPlayPauseToggleEnabled||controls.IsPlayEnabled,controls.IsPreviousEnabled,controls.IsNextEnabled,controls.IsPlaybackPositionEnabled&&duration>0);

        }catch(OperationCanceledException){}catch(Exception e){App.Log(e);}
        finally{Interlocked.Exchange(ref busy,0);}
    }
    public async Task CommandAsync(string command,double ratio=0)
    {
        var session=current;if(session==null)return;
        try{
            bool ok=command switch{
                "previous"=>await session.TrySkipPreviousAsync(),
                "next"=>await session.TrySkipNextAsync(),
                "seek"=>await session.TryChangePlaybackPositionAsync(session.GetTimelineProperties().StartTime.Ticks+(long)(Snapshot.Duration*Math.Clamp(ratio,0,1)*TimeSpan.TicksPerSecond)),
                _=>await session.TryTogglePlayPauseAsync()
            };
            if(!ok)throw new InvalidOperationException("O aplicativo não aceitou este comando de mídia.");
        }catch(Exception e){App.Log(e);}
        await RefreshAsync();
    }
    public void Dispose(){stop.Cancel();stop.Dispose();}
}
