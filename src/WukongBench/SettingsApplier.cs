using System.Globalization;
using System.Text.RegularExpressions;

namespace WukongBench;

/// <summary>Применяет профиль теста к GameUserSettings.ini Benchmark Tool.</summary>
public static class SettingsApplier
{
    private const string GusSection = "/Script/Engine.GameUserSettings";
    private const string SgSection = "ScalabilityGroups";

    private static readonly string[] QualityGroups =
    {
        "sg.AntiAliasingQuality", "sg.ShadowQuality", "sg.GlobalIlluminationQuality",
        "sg.ReflectionQuality", "sg.PostProcessQuality", "sg.TextureQuality",
        "sg.EffectsQuality", "sg.FoliageQuality", "sg.ShadingQuality"
    };

    /// <summary>Конфиг бенчмарка: сначала папка игры, затем %LOCALAPPDATA%.</summary>
    public static string? FindConfig(string gameDir)
    {
        var candidates = new[]
        {
            Path.Combine(gameDir, "b1", "Saved", "Config", "Windows", "GameUserSettings.ini"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "b1", "Saved", "Config", "Windows", "GameUserSettings.ini")
        };
        return candidates.FirstOrDefault(File.Exists);
    }

    /// <summary>Возвращает словарь фактически записанных значений — он попадает в отчёт.</summary>
    public static Dictionary<string, string> Apply(string iniPath, TestProfile p, (int w, int h) native)
    {
        int w = p.Width > 0 ? p.Width : native.w;
        int h = p.Height > 0 ? p.Height : native.h;
        int q = Math.Clamp(p.Quality, 0, 4);
        int vd = p.ViewDistance >= 0 ? Math.Clamp(p.ViewDistance, 0, 4) : q;
        string scale = Math.Clamp(p.ResolutionScale, 25, 100).ToString("F6", CultureInfo.InvariantCulture);

        var ini = IniFile.Load(iniPath);
        var applied = new Dictionary<string, string>();
        void Set(string section, string key, string value) { ini.Set(section, key, value); applied[key] = value; }

        // --- Разрешение и режим окна ---
        foreach (var k in new[] { "ResolutionSizeX", "LastUserConfirmedResolutionSizeX", "DesiredScreenWidth", "LastUserConfirmedDesiredScreenWidth" })
            Set(GusSection, k, w.ToString());
        foreach (var k in new[] { "ResolutionSizeY", "LastUserConfirmedResolutionSizeY", "DesiredScreenHeight", "LastUserConfirmedDesiredScreenHeight" })
            Set(GusSection, k, h.ToString());
        Set(GusSection, "FullscreenMode", "0");          // эксклюзивный полноэкранный
        Set(GusSection, "LastConfirmedFullscreenMode", "0");
        Set(GusSection, "PreferredFullscreenMode", "0");

        // --- Всё, что искажает замер ---
        Set(GusSection, "bUseVSync", "False");
        Set(GusSection, "FrameRateLimit", "0.000000");
        Set(GusSection, "bUseDynamicResolution", "False");

        // --- Масштаб рендера и группы качества ---
        Set(SgSection, "sg.ResolutionQuality", scale);
        Set(SgSection, "sg.ViewDistanceQuality", vd.ToString());
        foreach (var g in QualityGroups) Set(SgSection, g, q.ToString());

        // --- Трассировка лучей ---
        ini.SetEverywhere("r.RayTracing.EnableInGame", p.RayTracing ? "True" : "False", "SystemSettings");
        applied["r.RayTracing.EnableInGame"] = p.RayTracing ? "True" : "False";

        // --- UISettingData: меню бенчмарка хранит свою копию значений и может перезаписать sg.* ---
        PatchUiSettingData(ini, p.UiSettingOverrides, applied);

        ini.Save();
        return applied;
    }

    /// <summary>
    /// UISettingData — сериализованная map из UE. Формат точно не документирован, поэтому патчим
    /// значения по имени ключа, поддерживая варианты Key=1, ("Key",1), (Key, 1).
    /// </summary>
    private static void PatchUiSettingData(IniFile ini, Dictionary<string, int> overrides, Dictionary<string, string> applied)
    {
        if (overrides.Count == 0) return;
        var found = ini.FindAny("UISettingData");
        if (found == null) return;

        var value = found.Value.value;
        foreach (var (key, v) in overrides)
        {
            var re = new Regex($@"(\b{Regex.Escape(key)}\b""?\s*[=,:]\s*)(-?\d+)", RegexOptions.IgnoreCase);
            if (re.IsMatch(value))
            {
                value = re.Replace(value, m => m.Groups[1].Value + v.ToString(), 1);
                applied[$"UISettingData.{key}"] = v.ToString();
            }
        }
        ini.Set(found.Value.section, "UISettingData", value);
    }
}
