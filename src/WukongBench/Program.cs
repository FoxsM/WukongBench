using System.Diagnostics;
using System.Text;
using WukongBench;

Console.OutputEncoding = Encoding.UTF8;
NativeMethods.EnableDpiAwareness();
Console.WriteLine("=== WukongBench — автоматический CPU/GPU прогон Black Myth: Wukong Benchmark Tool ===\n");

// ---------- Аргументы ----------
string configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
string? gameDirArg = null;
bool runCpu = true, runGpu = true;
for (int i = 0; i < args.Length; i++)
{
    switch (args[i].ToLowerInvariant())
    {
        case "--config" when i + 1 < args.Length: configPath = args[++i]; break;
        case "--game-dir" when i + 1 < args.Length: gameDirArg = args[++i]; break;
        case "--cpu-only": runGpu = false; break;
        case "--gpu-only": runCpu = false; break;
        case "-h" or "--help":
            Console.WriteLine("WukongBench.exe [--game-dir <папка бенчмарка>] [--config <appsettings.json>] [--cpu-only | --gpu-only]");
            return 0;
    }
}

var cfg = AppConfig.Load(configPath);

// ---------- Поиск бенчмарка ----------
var gameDir = gameDirArg ?? (string.IsNullOrWhiteSpace(cfg.GameDir) ? SteamLocator.FindGameDir(cfg.SteamAppId) : cfg.GameDir);
if (gameDir == null || !Directory.Exists(gameDir))
{
    Log.Error("Black Myth: Wukong Benchmark Tool не найден. Укажите папку: --game-dir \"D:\\Steam\\steamapps\\common\\BlackMythWukongBenchmarkTool\"");
    return 1;
}
Log.Ok($"Бенчмарк: {gameDir}");

var iniPath = SettingsApplier.FindConfig(gameDir);
if (iniPath == null)
{
    Log.Error("GameUserSettings.ini не найден. Запустите бенчмарк один раз вручную и закройте его — файл создастся.");
    return 1;
}
Log.Ok($"Конфиг: {iniPath}");

// ---------- Подготовка ----------
var sys = SystemInfo.Collect(gameDir);
var (nw, nh, _) = NativeMethods.GetPrimaryDisplayMode();

var outDir = Path.Combine(AppContext.BaseDirectory, "results", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
Directory.CreateDirectory(outDir);

var backup = Path.Combine(outDir, "GameUserSettings.original.ini");
File.Copy(iniPath, backup, true);
Log.Info($"Резервная копия настроек: {backup}");

void Restore()
{
    try { BenchmarkRunner.KillRunning(); File.Copy(backup, iniPath, true); Log.Ok("Исходные настройки бенчмарка восстановлены."); }
    catch (Exception e) { Log.Warn("Не удалось восстановить настройки: " + e.Message); }
}
Console.CancelKeyPress += (_, e) => { e.Cancel = false; Restore(); };

BenchmarkRunner.KillRunning();
var runner = new BenchmarkRunner(cfg, gameDir);
var results = new List<BenchmarkResult>();
int exitCode = 0;

try
{
    var tests = new List<TestProfile>();
    if (runCpu) tests.Add(cfg.Cpu);
    if (runGpu) tests.Add(cfg.Gpu);

    foreach (var profile in tests)
    {
        Console.WriteLine();
        Log.Info($"========== {profile.Name}-тест ==========");
        var applied = SettingsApplier.Apply(iniPath, profile, (nw, nh));
        var res = $"{applied["ResolutionSizeX"]}x{applied["ResolutionSizeY"]}";
        Log.Ok($"Настройки применены: {res}, {profile.QualityName(profile.Quality)}, scale {profile.ResolutionScale}%, RT {(profile.RayTracing ? "on" : "off")}");

        var testDir = Path.Combine(outDir, profile.Name.ToLowerInvariant());
        Directory.CreateDirectory(testDir);
        File.Copy(iniPath, Path.Combine(testDir, "GameUserSettings.applied.ini"), true);

        var sw = Stopwatch.StartNew();
        var resultFile = runner.Run(profile.Name);
        sw.Stop();

        File.Copy(resultFile, Path.Combine(testDir, "benchmark_raw.json"), true);
        var parsed = ResultParser.Parse(resultFile, profile.Name);
        parsed.Profile = profile;
        parsed.AppliedSettings = applied;
        parsed.Resolution = res;
        parsed.Duration = sw.Elapsed;
        parsed.Verification = SettingsApplier.Verify(parsed, profile, res);
        results.Add(parsed);

        if (parsed.Verification.Count == 0) Log.Ok("Проверка: бенчмарк работал с заданными настройками.");
        foreach (var issue in parsed.Verification) Log.Warn("Проверка: " + issue);

        Log.Ok($"{profile.Name}: Avg {parsed.FpsAvg:0.0} FPS, Min {parsed.FpsMin:0.0}, FPS95 {parsed.Fps95:0.0}");
    }
}
catch (Exception e)
{
    Log.Error(e.Message);
    exitCode = 2;
}
finally
{
    Restore();
}

// ---------- Отчёт ----------
ReportWriter.PrintConsole(sys, results);
var md = Path.Combine(outDir, "report.md");
File.WriteAllText(md, ReportWriter.BuildText(sys, results, gameDir), new UTF8Encoding(false));
ReportWriter.SaveJson(Path.Combine(outDir, "report.json"), sys, results);
Log.Ok($"Отчёт сохранён: {md}");

if (!Console.IsInputRedirected)
{
    Console.WriteLine("\nНажмите любую клавишу для выхода…");
    Console.ReadKey(true);
}
return exitCode;
