using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace WukongBench;

public static class ReportWriter
{
    private static string F(double? v) => v.HasValue ? v.Value.ToString("0.0", CultureInfo.InvariantCulture) : "—";

    public static string BuildText(SystemInfo sys, IReadOnlyList<BenchmarkResult> results, string gameDir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Black Myth: Wukong Benchmark — отчёт");
        sb.AppendLine();
        sb.AppendLine($"Дата: {DateTime.Now:yyyy-MM-dd HH:mm}  ");
        sb.AppendLine($"Папка бенчмарка: `{gameDir}`");
        sb.AppendLine();

        sb.AppendLine("## Характеристики ПК");
        sb.AppendLine();
        sb.AppendLine("| Компонент | Значение |");
        sb.AppendLine("|---|---|");
        sb.AppendLine($"| CPU | {sys.Cpu} ({sys.CpuDetails}) |");
        for (int i = 0; i < sys.Gpus.Count; i++) sb.AppendLine($"| GPU{(sys.Gpus.Count > 1 ? " " + (i + 1) : "")} | {sys.Gpus[i]} |");
        sb.AppendLine($"| RAM | {sys.Ram} |");
        foreach (var m in sys.RamModules) sb.AppendLine($"| — модуль | {m} |");
        sb.AppendLine($"| Мат. плата | {sys.Motherboard} |");
        sb.AppendLine($"| ОС | {sys.Os} |");
        sb.AppendLine($"| Монитор | {sys.Display} |");
        sb.AppendLine($"| Диск с игрой | {sys.GameDisk} |");
        sb.AppendLine($"| План питания | {sys.PowerPlan} |");
        sb.AppendLine();

        sb.AppendLine("## Результаты");
        sb.AppendLine();
        sb.AppendLine("| Тест | Разрешение | Avg FPS | Min FPS | Max FPS | FPS95 | 1% low* | 0.1% low* | Frametime, мс* | Длительность |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
        foreach (var r in results)
            sb.AppendLine($"| **{r.TestName}** | {r.Resolution} | **{F(r.FpsAvg)}** | {F(r.FpsMin)} | {F(r.FpsMax)} | {F(r.Fps95)} | " +
                          $"{F(r.Low1Percent)} | {F(r.Low01Percent)} | {F(r.FrameTimeAvgMs)} | {r.Duration:mm\\:ss} |");
        sb.AppendLine();
        sb.AppendLine("\\* посчитано утилитой из покадровых записей `Records` (если бенчмарк их сохранил).");
        sb.AppendLine();

        foreach (var r in results)
        {
            sb.AppendLine($"## Настройки {r.TestName}-теста");
            sb.AppendLine();
            if (r.Profile != null)
            {
                var p = r.Profile;
                sb.AppendLine($"- Разрешение: **{r.Resolution}**, полноэкранный режим");
                sb.AppendLine($"- Масштаб рендера (Super Resolution / sg.ResolutionQuality): **{p.ResolutionScale}%**");
                sb.AppendLine($"- Общее качество: **{p.QualityName(p.Quality)}**, дальность прорисовки: **{p.QualityName(p.ViewDistance >= 0 ? p.ViewDistance : p.Quality)}**");
                sb.AppendLine($"- Full Ray Tracing: **{(p.RayTracing ? "Вкл" : "Выкл")}**");
                sb.AppendLine("- VSync: выкл, лимит FPS: нет, динамическое разрешение: выкл");
                sb.AppendLine();
            }
            sb.AppendLine("<details><summary>Записанные в GameUserSettings.ini значения</summary>");
            sb.AppendLine();
            sb.AppendLine("| Ключ | Значение |");
            sb.AppendLine("|---|---|");
            foreach (var (k, v) in r.AppliedSettings) sb.AppendLine($"| `{k}` | {v} |");
            sb.AppendLine();
            sb.AppendLine("</details>");
            sb.AppendLine();

            if (r.OtherMetrics.Count > 0)
            {
                sb.AppendLine("<details><summary>Прочие поля из результата бенчмарка (загрузка CPU/GPU, фактические настройки и т.д.)</summary>");
                sb.AppendLine();
                sb.AppendLine("| Поле | Значение |");
                sb.AppendLine("|---|---|");
                foreach (var (k, v) in r.OtherMetrics) sb.AppendLine($"| `{k}` | {v} |");
                sb.AppendLine();
                sb.AppendLine("</details>");
                sb.AppendLine();
            }
            sb.AppendLine($"Исходный файл: `{r.SourceFile}` (записей кадров: {r.RecordsCount})");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public static void PrintConsole(SystemInfo sys, IReadOnlyList<BenchmarkResult> results)
    {
        Console.WriteLine();
        Console.WriteLine(new string('═', 78));
        Console.WriteLine("  ХАРАКТЕРИСТИКИ ПК");
        Console.WriteLine(new string('─', 78));
        Console.WriteLine($"  CPU   : {sys.Cpu}");
        Console.WriteLine($"          {sys.CpuDetails}");
        foreach (var g in sys.Gpus) Console.WriteLine($"  GPU   : {g}");
        Console.WriteLine($"  RAM   : {sys.Ram}");
        Console.WriteLine($"  Плата : {sys.Motherboard}");
        Console.WriteLine($"  ОС    : {sys.Os}");
        Console.WriteLine($"  Экран : {sys.Display}");
        Console.WriteLine(new string('═', 78));
        Console.WriteLine($"  {"Тест",-5} {"Разрешение",-12} {"Avg",8} {"Min",8} {"Max",8} {"FPS95",8} {"1% low",8} {"Время",7}");
        Console.WriteLine(new string('─', 78));
        foreach (var r in results)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  {r.TestName,-5} {r.Resolution,-12} {F(r.FpsAvg),8} {F(r.FpsMin),8} {F(r.FpsMax),8} {F(r.Fps95),8} {F(r.Low1Percent),8} {r.Duration,7:mm\\:ss}");
            Console.ResetColor();
            if (r.Profile is { } p)
                Console.WriteLine($"        {p.QualityName(p.Quality)}, scale {p.ResolutionScale}%, RT {(p.RayTracing ? "on" : "off")}, VSync off, no FPS cap");
        }
        Console.WriteLine(new string('═', 78));
    }

    public static void SaveJson(string path, SystemInfo sys, IReadOnlyList<BenchmarkResult> results)
    {
        var opts = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        File.WriteAllText(path, JsonSerializer.Serialize(new { GeneratedAt = DateTime.Now, System = sys, Results = results }, opts));
    }
}
