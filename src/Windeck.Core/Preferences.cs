using System.Text.Json;
namespace Windeck.Core;
public sealed record PinnedApp(string Name, string Target);
public sealed record Reminder(Guid Id, DateTime Due, string Text, bool Done = false);
public sealed class Preferences
{
    public string Theme { get; set; } = "Fluent";
    public bool ReplaceTaskbar { get; set; } = true;
    public bool ReduceMotion { get; set; }
    public bool Gestures { get; set; } = true;
    public bool HideOnFullscreen { get; set; } = true;
    public bool ReserveSpace { get; set; } = true;
    public double StrikeSpeed { get; set; } = 1100;
    public string WeatherCity { get; set; } = "Belém";
    public string WeatherCountry { get; set; } = "BR";
    public string Monitor { get; set; } = "";
    public List<PinnedApp> Pins { get; set; } = [];
    public List<string> PinnedTray { get; set; } = [];
    public List<Reminder> Reminders { get; set; } = [];
    public static Preferences Load(string path)
    {
        try { return Normalize(JsonSerializer.Deserialize<Preferences>(File.ReadAllText(path)) ?? new()); }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException) { return new(); }
    }
    public static Preferences Normalize(Preferences p)
    {
        p.Theme = p.Theme == "Dock" ? "Dock" : "Fluent";
        p.StrikeSpeed = double.IsFinite(p.StrikeSpeed) ? Math.Clamp(p.StrikeSpeed, 650, 2400) : 1100;
        p.Pins ??= []; p.Reminders ??= []; p.PinnedTray ??= [];
        p.WeatherCity = (p.WeatherCity ?? "Belém").Trim();
        p.WeatherCountry = (p.WeatherCountry ?? "BR").Trim().ToUpperInvariant();
        p.Monitor ??= "";
        return p;
    }
    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temp = path + ".new";
        File.WriteAllText(temp, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented=true }));
        File.Move(temp, path, true);
    }
}
