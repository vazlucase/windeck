using System.Globalization;
using System.Text.Json;
namespace Windeck.Services;
internal sealed record ForecastDay(DateTime Day,int Code,double Max,double Min);
internal sealed record WeatherSnapshot(string Place,double Temperature,int Code,bool Day,double Wind,double Humidity,IReadOnlyList<ForecastDay> Forecast,DateTime Updated,string? Error=null);
internal sealed class WeatherService : IDisposable
{
    private readonly HttpClient http=new(){Timeout=TimeSpan.FromSeconds(9)};
    private readonly CancellationTokenSource stop=new();
    private DateTime next;
    private string lastCity="";
    private int busy;
    public WeatherSnapshot? Snapshot {get;private set;}
    public string Status {get;private set;}="Consultando previsão…";
    public async Task RefreshAsync(string city,string country,bool force=false)
    {
        string key=city+country;
        if(!force&&key==lastCity&&DateTime.UtcNow<next)return;
        if(Interlocked.Exchange(ref busy,1)!=0)return;
        try{
            if(string.IsNullOrWhiteSpace(city)){Status="Configure sua cidade";Snapshot=null;return;}
            string geo=$"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&countryCode={Uri.EscapeDataString(country)}&count=1&language=pt&format=json";
            using var g=JsonDocument.Parse(await http.GetStringAsync(geo,stop.Token));
            if(!g.RootElement.TryGetProperty("results",out var locations)||locations.GetArrayLength()==0)throw new InvalidOperationException("Cidade não encontrada");
            var loc=locations[0];
            string lat=loc.GetProperty("latitude").GetDouble().ToString(CultureInfo.InvariantCulture),lon=loc.GetProperty("longitude").GetDouble().ToString(CultureInfo.InvariantCulture);
            string url=$"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current=temperature_2m,relative_humidity_2m,is_day,weather_code,wind_speed_10m&daily=weather_code,temperature_2m_max,temperature_2m_min&forecast_days=5&timezone=auto";
            using var d=JsonDocument.Parse(await http.GetStringAsync(url,stop.Token));
            var c=d.RootElement.GetProperty("current");var days=d.RootElement.GetProperty("daily");
            var forecast=new List<ForecastDay>();
            for(int i=0;i<5;i++)forecast.Add(new(DateTime.Parse(days.GetProperty("time")[i].GetString()!,CultureInfo.InvariantCulture),days.GetProperty("weather_code")[i].GetInt32(),days.GetProperty("temperature_2m_max")[i].GetDouble(),days.GetProperty("temperature_2m_min")[i].GetDouble()));
            Snapshot=new($"{loc.GetProperty("name").GetString()}, {(loc.TryGetProperty("admin1",out var region)?region.GetString():loc.GetProperty("country_code").GetString())}",c.GetProperty("temperature_2m").GetDouble(),c.GetProperty("weather_code").GetInt32(),c.GetProperty("is_day").GetInt32()!=0,c.GetProperty("wind_speed_10m").GetDouble(),c.GetProperty("relative_humidity_2m").GetDouble(),forecast,DateTime.Now);
            Status="Atualizado agora · Open-Meteo";next=DateTime.UtcNow.AddMinutes(15);lastCity=key;
        }catch(OperationCanceledException){}catch(Exception e){Status=Snapshot==null?"Clima indisponível · verifique a conexão":"Sem conexão · última previsão mantida";if(Snapshot!=null)Snapshot=Snapshot with {Error=Status};next=DateTime.UtcNow.AddMinutes(1);lastCity=key;App.Log(e);}
        finally{Interlocked.Exchange(ref busy,0);}
    }
    public static string Glyph(int code,bool day=true)=>code switch{0=>day?"☀":"☾",<=3=>"☁",45 or 48=>"≋",>=95=>"ϟ",71 or 73 or 75 or 77 or 85 or 86=>"❄",_=>"☂"};
    public static string Description(int code)=>code switch{0=>"Céu limpo",<=3=>"Parcialmente nublado",45 or 48=>"Névoa",>=95=>"Trovoadas",71 or 73 or 75 or 77 or 85 or 86=>"Neve",_=>"Chuva"};
    public void Dispose(){stop.Cancel();http.Dispose();stop.Dispose();}
}
