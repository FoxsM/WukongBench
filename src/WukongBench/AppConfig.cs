using System.Text.Json;

namespace WukongBench;

/// <summary>Профиль настроек одного прохода бенчмарка.</summary>
public sealed class TestProfile
{
    public string Name { get; set; } = "";
    /// <summary>Ширина рендера. 0 = нативное разрешение основного монитора.</summary>
    public int Width { get; set; }
    /// <summary>Высота рендера. 0 = нативное разрешение основного монитора.</summary>
    public int Height { get; set; }
    /// <summary>Масштаб разрешения рендера (sg.ResolutionQuality), 25..100.</summary>
    public int ResolutionScale { get; set; } = 100;
    /// <summary>Общее качество: 0=Low, 1=Medium, 2=High, 3=Very High, 4=Cinematic.</summary>
    public int Quality { get; set; }
    /// <summary>Дальность прорисовки (0..4). -1 = как Quality.</summary>
    public int ViewDistance { get; set; } = -1;
    /// <summary>Full Ray Tracing.</summary>
    public bool RayTracing { get; set; }
    /// <summary>Значения, которые дополнительно патчатся внутри строки UISettingData.</summary>
    public Dictionary<string, int> UiSettingOverrides { get; set; } = new();

    public static readonly string[] QualityNames = { "Low", "Medium", "High", "Very High", "Cinematic" };
    public string QualityName(int q) => q is >= 0 and <= 4 ? QualityNames[q] : q.ToString();
}

public sealed class AppConfig
{
    public string GameDir { get; set; } = "";
    public int SteamAppId { get; set; } = 3132990;
    /// <summary>"Steam" (steam.exe -applaunch) или "Direct" (прямой запуск exe).</summary>
    public string LaunchMode { get; set; } = "Steam";
    public string LaunchArgs { get; set; } = "-dx12";

    public int MenuLoadDelaySeconds { get; set; } = 30;
    public List<string> StartKeys { get; set; } = new() { "Enter" };
    public int KeyDelayMs { get; set; } = 800;
    public int BenchmarkTimeoutMinutes { get; set; } = 10;
    public int CooldownSeconds { get; set; } = 10;

    public List<string> ResultDirs { get; set; } = new() { @"%TEMP%\b1\BenchMarkHistory\Tool" };

    public TestProfile Cpu { get; set; } = new()
    {
        Name = "CPU", Width = 1280, Height = 720, ResolutionScale = 50, Quality = 0, ViewDistance = 0,
        RayTracing = false, UiSettingOverrides = new() { ["Rtx"] = 0 }
    };

    public TestProfile Gpu { get; set; } = new()
    {
        Name = "GPU", Width = 0, Height = 0, ResolutionScale = 100, Quality = 4, ViewDistance = 4,
        RayTracing = true, UiSettingOverrides = new() { ["Rtx"] = 1 }
    };

    public static AppConfig Load(string path)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"[i] {path} не найден — используются настройки по умолчанию.");
            return new AppConfig();
        }

        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        return JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(path), opts) ?? new AppConfig();
    }
}
