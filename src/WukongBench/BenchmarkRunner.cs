using System.Diagnostics;
using System.Text.Json;

namespace WukongBench;

/// <summary>Запуск Benchmark Tool, старт прогона, ожидание файла результатов, закрытие процесса.</summary>
public sealed class BenchmarkRunner
{
    public const string ProcessName = "b1-Win64-Shipping";

    private readonly AppConfig _cfg;
    private readonly string _gameDir;

    private IntPtr _hwnd;

    public BenchmarkRunner(AppConfig cfg, string gameDir) { _cfg = cfg; _gameDir = gameDir; }

    /// <summary>Выполняет сценарий StartActions: Key:..., Click:x,y, Wait:мс.</summary>
    private void RunStartActions()
    {
        Log.Info($"Запускаю тест: {string.Join(" → ", _cfg.StartActions)}");
        foreach (var raw in _cfg.StartActions)
        {
            var parts = raw.Split(':', 2, StringSplitOptions.TrimEntries);
            var cmd = parts[0].ToLowerInvariant();
            var arg = parts.Length > 1 ? parts[1] : "";
            try
            {
                switch (cmd)
                {
                    case "wait":
                        Thread.Sleep(int.Parse(arg));
                        break;
                    case "key":
                        InputSender.FocusWindow(_hwnd);
                        Thread.Sleep(200);
                        InputSender.PressKey(arg);
                        break;
                    case "click":
                        var xy = arg.Split(',', StringSplitOptions.TrimEntries);
                        InputSender.FocusWindow(_hwnd);
                        Thread.Sleep(200);
                        InputSender.ClickRelative(_hwnd,
                            double.Parse(xy[0], System.Globalization.CultureInfo.InvariantCulture),
                            double.Parse(xy[1], System.Globalization.CultureInfo.InvariantCulture));
                        break;
                    default:
                        Log.Warn($"Неизвестная команда сценария: {raw}");
                        break;
                }
            }
            catch (Exception e) { Log.Warn($"Команда «{raw}» не выполнена: {e.Message}"); }
        }
    }

    public IReadOnlyList<string> ResultDirs =>
        _cfg.ResultDirs.Select(Environment.ExpandEnvironmentVariables).ToList();

    public static void KillRunning()
    {
        foreach (var p in Process.GetProcessesByName(ProcessName))
        {
            try { Log.Warn($"Закрываю уже запущенный {ProcessName} (PID {p.Id})"); p.Kill(true); p.WaitForExit(15000); }
            catch { /* игнор */ }
        }
    }

    /// <summary>Полный цикл одного прохода. Возвращает путь к JSON-файлу результата.</summary>
    public string Run(string testName)
    {
        var before = SnapshotResults();
        var started = DateTime.UtcNow;

        Launch();
        var proc = WaitForProcess(TimeSpan.FromMinutes(3))
                   ?? throw new Exception("Процесс бенчмарка не появился за 3 минуты.");
        Log.Info($"Процесс найден (PID {proc.Id}). Жду загрузку меню {_cfg.MenuLoadDelaySeconds} с…");

        var hwnd = WaitForWindow(proc, TimeSpan.FromMinutes(2));
        Thread.Sleep(TimeSpan.FromSeconds(_cfg.MenuLoadDelaySeconds));

        _hwnd = hwnd != IntPtr.Zero ? hwnd : proc.MainWindowHandle;
        RunStartActions();

        Log.Info($"[{testName}] Бенчмарк идёт. Жду файл результатов (таймаут {_cfg.BenchmarkTimeoutMinutes} мин)…");
        var result = WaitForResult(before, started, proc, TimeSpan.FromMinutes(_cfg.BenchmarkTimeoutMinutes));

        Log.Ok($"Результат: {result}");
        Close(proc);
        return result;
    }

    private void Launch()
    {
        if (_cfg.LaunchMode.Equals("Direct", StringComparison.OrdinalIgnoreCase))
        {
            var exe = new[]
            {
                Path.Combine(_gameDir, "b1_benchmark.exe"),
                Path.Combine(_gameDir, "b1", "Binaries", "Win64", ProcessName + ".exe")
            }.FirstOrDefault(File.Exists) ?? throw new FileNotFoundException("Не найден exe бенчмарка в " + _gameDir);

            Log.Info($"Запуск напрямую: {exe} {_cfg.LaunchArgs}");
            Process.Start(new ProcessStartInfo(exe, _cfg.LaunchArgs) { WorkingDirectory = Path.GetDirectoryName(exe)!, UseShellExecute = true });
            return;
        }

        var steamExe = SteamLocator.FindSteamExe(SteamLocator.FindSteamDir());
        if (steamExe != null)
        {
            var args = $"-applaunch {_cfg.SteamAppId} {_cfg.LaunchArgs}".Trim();
            Log.Info($"Запуск через Steam: steam.exe {args}");
            Process.Start(new ProcessStartInfo(steamExe, args) { UseShellExecute = true });
        }
        else
        {
            Log.Info($"steam.exe не найден, запуск через протокол steam://rungameid/{_cfg.SteamAppId}");
            Process.Start(new ProcessStartInfo($"steam://rungameid/{_cfg.SteamAppId}") { UseShellExecute = true });
        }
    }

    private static Process? WaitForProcess(TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            var p = Process.GetProcessesByName(ProcessName).FirstOrDefault();
            if (p != null) return p;
            Thread.Sleep(1000);
        }
        return null;
    }

    private static IntPtr WaitForWindow(Process p, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout && !p.HasExited)
        {
            p.Refresh();
            if (p.MainWindowHandle != IntPtr.Zero) return p.MainWindowHandle;
            Thread.Sleep(1000);
        }
        return IntPtr.Zero;
    }

    private Dictionary<string, DateTime> SnapshotResults()
    {
        var map = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        foreach (var dir in ResultDirs.Where(Directory.Exists))
            foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                map[f] = File.GetLastWriteTimeUtc(f);
        return map;
    }

    private string WaitForResult(Dictionary<string, DateTime> before, DateTime startedUtc, Process proc, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        var lastProgress = TimeSpan.Zero;
        bool retried = false;

        while (sw.Elapsed < timeout)
        {
            foreach (var dir in ResultDirs.Where(Directory.Exists))
            {
                var candidate = Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
                    .Select(f => new FileInfo(f))
                    .Where(f => f.LastWriteTimeUtc >= startedUtc.AddSeconds(-2) &&
                                (!before.TryGetValue(f.FullName, out var t) || f.LastWriteTimeUtc > t))
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .FirstOrDefault();

                if (candidate != null && IsCompleteJson(candidate.FullName))
                    return candidate.FullName;
            }

            proc.Refresh();
            if (proc.HasExited)
                throw new Exception("Бенчмарк закрылся раньше, чем появился файл результатов.");

            if (!retried && _cfg.StartRetryMinutes > 0 && sw.Elapsed >= TimeSpan.FromMinutes(_cfg.StartRetryMinutes))
            {
                // Прогон длится ~2.5 мин + загрузка; если результата всё нет — скорее всего, старт не сработал.
                retried = true;
                Log.Warn($"Результата нет {_cfg.StartRetryMinutes} мин — похоже, тест не стартовал. Повторяю сценарий запуска.");
                proc.Refresh();
                if (proc.MainWindowHandle != IntPtr.Zero) _hwnd = proc.MainWindowHandle;
                RunStartActions();
            }

            if (sw.Elapsed - lastProgress >= TimeSpan.FromSeconds(30))
            {
                lastProgress = sw.Elapsed;
                Log.Info($"  …прошло {sw.Elapsed:mm\\:ss}");
            }
            Thread.Sleep(2000);
        }
        throw new TimeoutException("Файл результатов не появился за отведённое время.");
    }

    /// <summary>Файл считается готовым, когда размер стабилен и он парсится как JSON.</summary>
    private static bool IsCompleteJson(string path)
    {
        try
        {
            long s1 = new FileInfo(path).Length;
            Thread.Sleep(1500);
            long s2 = new FileInfo(path).Length;
            if (s1 != s2 || s2 == 0) return false;

            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var _ = JsonDocument.Parse(fs);
            return true;
        }
        catch { return false; }
    }

    private void Close(Process proc)
    {
        Thread.Sleep(3000);
        try
        {
            proc.Refresh();
            if (!proc.HasExited)
            {
                proc.CloseMainWindow();
                if (!proc.WaitForExit(8000)) { proc.Kill(true); proc.WaitForExit(15000); }
            }
        }
        catch { /* процесс уже завершился */ }

        KillRunning();
        Log.Info($"Пауза {_cfg.CooldownSeconds} с (остывание + Steam освобождает приложение)…");
        Thread.Sleep(TimeSpan.FromSeconds(_cfg.CooldownSeconds));
    }
}
