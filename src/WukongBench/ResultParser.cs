using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WukongBench;

public sealed class BenchmarkResult
{
    public string TestName { get; set; } = "";
    public string SourceFile { get; set; } = "";
    public double? FpsAvg { get; set; }
    public double? FpsMin { get; set; }
    public double? FpsMax { get; set; }
    public double? Fps95 { get; set; }
    /// <summary>Посчитано нами из покадровых записей (если они есть).</summary>
    public double? Low1Percent { get; set; }
    public double? Low01Percent { get; set; }
    public double? FrameTimeAvgMs { get; set; }
    public int RecordsCount { get; set; }
    /// <summary>Прочие скалярные поля из JSON (загрузка CPU/GPU, настройки, версия и т.д.).</summary>
    public SortedDictionary<string, string> OtherMetrics { get; set; } = new();
    /// <summary>Настройки, которые утилита записала в ini.</summary>
    public Dictionary<string, string> AppliedSettings { get; set; } = new();
    public TestProfile? Profile { get; set; }
    public string Resolution { get; set; } = "";
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Парсер JSON из %TEMP%\b1\BenchMarkHistory\Tool. Имена полей ищутся без учёта регистра
/// и на любой глубине, чтобы пережить небольшие изменения формата в обновлениях бенчмарка.
/// </summary>
public static class ResultParser
{
    public static BenchmarkResult Parse(string path, string testName)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var doc = JsonDocument.Parse(fs);

        var scalars = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        JsonElement? records = null;
        Walk(doc.RootElement, "", scalars, ref records);

        var r = new BenchmarkResult { TestName = testName, SourceFile = path };
        r.FpsAvg = Find(scalars, "FPSAvg", "AvgFPS", "AverageFPS", "FPSAverage");
        r.FpsMin = Find(scalars, "FPSMin", "MinFPS");
        r.FpsMax = Find(scalars, "FPSMax", "MaxFPS");
        r.Fps95 = Find(scalars, "FPS95", "FPS95Percent", "FPS5Low");

        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "FPSAvg", "AvgFPS", "AverageFPS", "FPSAverage", "FPSMin", "MinFPS", "FPSMax", "MaxFPS", "FPS95", "FPS95Percent", "FPS5Low" };

        foreach (var (k, v) in scalars)
        {
            var leaf = k.Split('.').Last();
            if (used.Contains(leaf)) continue;
            r.OtherMetrics[k] = v.ValueKind switch
            {
                JsonValueKind.Number => v.GetDouble().ToString("0.##", CultureInfo.InvariantCulture),
                JsonValueKind.String => v.GetString() ?? "",
                _ => v.ToString()
            };
        }

        if (records is { ValueKind: JsonValueKind.Array } arr)
        {
            r.RecordsCount = arr.GetArrayLength();
            ComputeLows(arr, r);
        }
        return r;
    }

    private static void Walk(JsonElement e, string prefix, Dictionary<string, JsonElement> scalars, ref JsonElement? records)
    {
        switch (e.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var p in e.EnumerateObject())
                {
                    var name = prefix.Length == 0 ? p.Name : $"{prefix}.{p.Name}";
                    if (p.Value.ValueKind == JsonValueKind.Array)
                    {
                        if (p.Name.Equals("Records", StringComparison.OrdinalIgnoreCase) || p.Value.GetArrayLength() > 200)
                            records ??= p.Value.Clone();
                        else if (p.Value.GetArrayLength() <= 16 && p.Value.EnumerateArray().All(IsScalar))
                            scalars[name] = p.Value.Clone();
                    }
                    else Walk(p.Value, name, scalars, ref records);
                }
                break;
            case JsonValueKind.Number or JsonValueKind.String or JsonValueKind.True or JsonValueKind.False:
                scalars[prefix] = e.Clone();
                break;
        }
    }

    private static bool IsScalar(JsonElement e) =>
        e.ValueKind is JsonValueKind.Number or JsonValueKind.String or JsonValueKind.True or JsonValueKind.False;

    private static double? Find(Dictionary<string, JsonElement> scalars, params string[] names)
    {
        foreach (var n in names)
        {
            var hit = scalars.FirstOrDefault(kv => kv.Key.Split('.').Last().Equals(n, StringComparison.OrdinalIgnoreCase));
            if (hit.Key == null) continue;
            if (hit.Value.ValueKind == JsonValueKind.Number) return hit.Value.GetDouble();
            if (hit.Value.ValueKind == JsonValueKind.String &&
                double.TryParse(hit.Value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) return d;
        }
        return null;
    }

    /// <summary>
    /// Records — покадровые замеры. Ищем поле FPS или время кадра (мс) и считаем 1% / 0.1% low
    /// как средний FPS худших 1% / 0.1% кадров — метрики, которых нет в итоговом экране бенчмарка.
    /// </summary>
    private static void ComputeLows(JsonElement arr, BenchmarkResult r)
    {
        var frameTimes = new List<double>();
        foreach (var item in arr.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Number) { var fps = item.GetDouble(); if (fps > 0) frameTimes.Add(1000.0 / fps); continue; }
            if (item.ValueKind != JsonValueKind.Object) continue;

            double? ft = null, fps2 = null;
            foreach (var p in item.EnumerateObject())
            {
                if (p.Value.ValueKind != JsonValueKind.Number) continue;
                if (Regex.IsMatch(p.Name, "frame.?time|^ft$|delta", RegexOptions.IgnoreCase)) ft ??= p.Value.GetDouble();
                else if (Regex.IsMatch(p.Name, "^fps$|fps$", RegexOptions.IgnoreCase)) fps2 ??= p.Value.GetDouble();
            }
            if (ft is > 0) frameTimes.Add(ft.Value < 1 ? ft.Value * 1000 : ft.Value); // сек → мс
            else if (fps2 is > 0) frameTimes.Add(1000.0 / fps2.Value);
        }

        if (frameTimes.Count < 100) return;
        frameTimes.Sort();
        r.FrameTimeAvgMs = frameTimes.Average();
        r.Low1Percent = 1000.0 / frameTimes.Skip((int)(frameTimes.Count * 0.99)).Average();
        r.Low01Percent = 1000.0 / frameTimes.Skip((int)(frameTimes.Count * 0.999)).Average();
    }
}
