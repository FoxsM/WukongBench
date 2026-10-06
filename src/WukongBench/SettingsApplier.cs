using System.Globalization;
using System.Text.RegularExpressions;

namespace WukongBench;

/// <summary>
/// Применяет профиль теста к GameUserSettings.ini Benchmark Tool.
///
/// Настройки хранятся в двух местах, и игра читает оба:
///  1) секция [/Script/GSGameSettings.GSGameUserSettings] — разрешение, режим окна, VSync,
///     а также строка UISettingData = (("Key", "Value"),...) — значения меню;
///  2) [ScalabilityGroups] — группы качества Unreal Engine (sg.* = значение меню − 1).
/// Если обновить только одно из мест, меню может «откатить» значения, поэтому пишем оба согласованно.
/// </summary>
public static class SettingsApplier
{
    private const string WukongSection = "/Script/GSGameSettings.GSGameUserSettings";
    private const string EngineSection = "/Script/Engine.GameUserSettings";
    private const string SgSection = "ScalabilityGroups";

    /// <summary>Пары (ключ UISettingData, группа sg.*) — у каждой настройки качества.</summary>
    private static readonly (string ui, string? sg)[] QualityKeys =
    {
        ("AntiAliasing", "sg.AntiAliasingQuality"),
        ("PostProcessing", "sg.PostProcessQuality"),
        ("ShadowQuality", "sg.ShadowQuality"),
        ("TextureQuality", "sg.TextureQuality"),
        ("FxQuality", "sg.EffectsQuality"),
        ("MaterialQuality", "sg.ShadingQuality"),
        ("VegetationQuality", "sg.FoliageQuality"),
        ("GlobalIllumination", "sg.GlobalIlluminationQuality"),
        ("ReflectionQuality", "sg.ReflectionQuality"),
    };

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

    /// <summary>Записывает профиль в ini. Возвращает словарь фактически записанных значений — он попадает в отчёт.</summary>
    public static Dictionary<string, string> Apply(string iniPath, TestProfile p, (int w, int h) native)
    {
        int w = p.Width > 0 ? p.Width : native.w;
        int h = p.Height > 0 ? p.Height : native.h;
        int q = Math.Clamp(p.Quality, 0, 4);
        int vd = p.ViewDistance >= 0 ? Math.Clamp(p.ViewDistance, 0, 4) : q;
        int scale = Math.Clamp(p.ResolutionScale, 25, 100);
        int rtLevel = Math.Clamp(p.RayTracingLevel, 0, 3);

        var ini = IniFile.Load(iniPath);
        var applied = new Dictionary<string, string>();

        // Секция настроек игры: у Wukong своя (GSGameUserSettings); стандартная UE — запасной вариант.
        var gus = ini.Sections().FirstOrDefault(s => s.Equals(WukongSection, StringComparison.OrdinalIgnoreCase))
                  ?? ini.Sections().FirstOrDefault(s => s.EndsWith("GameUserSettings", StringComparison.OrdinalIgnoreCase))
                  ?? EngineSection;

        void Set(string section, string key, string value) { ini.Set(section, key, value); applied[key] = value; }

        // --- Разрешение и режим окна (0 = эксклюзивный полноэкранный) ---
        foreach (var k in new[] { "ResolutionSizeX", "LastUserConfirmedResolutionSizeX", "DesiredScreenWidth", "LastUserConfirmedDesiredScreenWidth" })
            Set(gus, k, w.ToString());
        foreach (var k in new[] { "ResolutionSizeY", "LastUserConfirmedResolutionSizeY", "DesiredScreenHeight", "LastUserConfirmedDesiredScreenHeight" })
            Set(gus, k, h.ToString());
        Set(gus, "FullscreenMode", "0");
        Set(gus, "LastConfirmedFullscreenMode", "0");
        Set(gus, "PreferredFullscreenMode", "0");

        // --- Всё, что искажает замер ---
        Set(gus, "bUseVSync", "False");
        Set(gus, "FrameRateLimit", "0.000000");
        Set(gus, "bUseDynamicResolution", "False");

        // --- Группы качества UE ---
        Set(SgSection, "sg.ResolutionQuality", scale.ToString(CultureInfo.InvariantCulture));
        Set(SgSection, "sg.ViewDistanceQuality", vd.ToString());
        foreach (var (_, sg) in QualityKeys) if (sg != null) Set(SgSection, sg, q.ToString());
        Set(SgSection, "sg.RayTracingQuality", (p.RayTracing ? rtLevel : 0).ToString());

        // --- Значения меню (UISettingData): в меню нумерация с 1, т.е. значение = sg + 1 ---
        var ui = new Dictionary<string, string>
        {
            ["ScreenMode"] = "0",                                    // полноэкранный
            ["Vsync"] = "0",
            ["LockFrameRate"] = "0",
            ["InsertFrame"] = "0",                                   // генерация кадров выкл.
            ["ImageQuality"] = ((int)Math.Round(h * scale / 100.0)).ToString(), // высота рендера = высота × масштаб
            ["QualityLevel"] = (q + 1).ToString(),                   // 1 Low … 5 Cinematic
            ["ViewDistance"] = (vd + 1).ToString(),
            ["Rtx"] = p.RayTracing ? "1" : "0",
            ["RtxLevel"] = (rtLevel + 1).ToString(),
        };
        foreach (var (uiKey, _) in QualityKeys) ui[uiKey] = (q + 1).ToString();
        foreach (var (k, v) in p.UiSettingOverrides) ui[k] = v.ToString();  // ручные переопределения из конфига

        PatchUiSettingData(ini, gus, ui, applied);
        ini.SetEverywhere("r.RayTracing.EnableInGame", p.RayTracing ? "True" : "False", "SystemSettings");

        ini.Save();
        return applied;
    }

    /// <summary>UISettingData=(("Key", "Value"),("Key2", "Value2"),...) — заменяем значения или дописываем пары.</summary>
    private static void PatchUiSettingData(IniFile ini, string gus, Dictionary<string, string> values, Dictionary<string, string> applied)
    {
        var found = ini.FindAny("UISettingData");
        var section = found?.section ?? gus;
        var data = found?.value.Trim() ?? "()";
        if (data.Length < 2 || data[0] != '(' || data[^1] != ')') data = "()";

        foreach (var (key, value) in values)
        {
            var re = new Regex($@"\(\s*""{Regex.Escape(key)}""\s*,\s*""[^""]*""\s*\)");
            var pair = $"(\"{key}\", \"{value}\")";
            if (re.IsMatch(data))
                data = re.Replace(data, pair.Replace("$", "$$"), 1);
            else
                data = data.Length > 2 ? data[..^1] + "," + pair + ")" : "(" + pair + ")";
            applied[$"UISettingData.{key}"] = value;
        }
        ini.Set(section, "UISettingData", data);
    }

    /// <summary>
    /// Сверяет фактические настройки из JSON результата с ожидаемыми.
    /// В JSON значения меню (1-based), разрешение — строкой "2560 × 1440".
    /// </summary>
    public static List<string> Verify(BenchmarkResult r, TestProfile p, string expectedResolution)
    {
        var issues = new List<string>();
        string? Get(string k) => r.OtherMetrics.FirstOrDefault(kv => kv.Key.Equals(k, StringComparison.OrdinalIgnoreCase)).Value;

        void Check(string key, string expected, string label)
        {
            var actual = Get(key);
            if (actual == null) return;
            var norm = Regex.Replace(actual, @"\s*[×xX]\s*", "x").Trim();
            if (!norm.Equals(expected, StringComparison.OrdinalIgnoreCase))
                issues.Add($"{label}: ожидалось {expected}, бенчмарк использовал {actual}");
        }

        Check("ScreenResolution", expectedResolution, "Разрешение");
        Check("QualityLevel", (Math.Clamp(p.Quality, 0, 4) + 1).ToString(), "Пресет качества");
        Check("ImageQuality", Math.Clamp(p.ResolutionScale, 25, 100).ToString(), "Масштаб рендера");
        // В JSON поле Rtx = уровень Full RT в нумерации меню (0 = выкл, 1 Low … 4 Very High)
        Check("Rtx", p.RayTracing ? (Math.Clamp(p.RayTracingLevel, 0, 3) + 1).ToString() : "0", "Ray Tracing (уровень)");
        Check("InsertFrame", "0", "Генерация кадров");
        return issues;
    }
}
