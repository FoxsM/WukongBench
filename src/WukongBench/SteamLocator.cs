using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace WukongBench;

/// <summary>Поиск Steam и папки установки Benchmark Tool через libraryfolders.vdf / appmanifest.</summary>
public static class SteamLocator
{
    public static string? FindSteamDir()
    {
        foreach (var (hive, key, value) in new[]
                 {
                     (Registry.CurrentUser, @"Software\Valve\Steam", "SteamPath"),
                     (Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath"),
                     (Registry.LocalMachine, @"SOFTWARE\Valve\Steam", "InstallPath")
                 })
        {
            try
            {
                if (hive.OpenSubKey(key)?.GetValue(value) is string p && Directory.Exists(p))
                    return Path.GetFullPath(p.Replace('/', '\\'));
            }
            catch { /* нет доступа к реестру — пробуем дальше */ }
        }

        var def = @"C:\Program Files (x86)\Steam";
        return Directory.Exists(def) ? def : null;
    }

    public static string? FindSteamExe(string? steamDir)
    {
        if (steamDir == null) return null;
        var exe = Path.Combine(steamDir, "steam.exe");
        return File.Exists(exe) ? exe : null;
    }

    public static IEnumerable<string> GetLibraries(string steamDir)
    {
        yield return steamDir;
        var vdf = Path.Combine(steamDir, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(vdf)) yield break;

        foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\""))
        {
            var p = m.Groups[1].Value.Replace(@"\\", @"\");
            if (Directory.Exists(p)) yield return p;
        }
    }

    public static string? FindGameDir(int appId)
    {
        var steamDir = FindSteamDir();
        if (steamDir == null) return null;

        foreach (var lib in GetLibraries(steamDir).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var manifest = Path.Combine(lib, "steamapps", $"appmanifest_{appId}.acf");
            if (File.Exists(manifest))
            {
                var m = Regex.Match(File.ReadAllText(manifest), "\"installdir\"\\s+\"([^\"]+)\"");
                if (m.Success)
                {
                    var dir = Path.Combine(lib, "steamapps", "common", m.Groups[1].Value);
                    if (Directory.Exists(dir)) return dir;
                }
            }

            var guess = Path.Combine(lib, "steamapps", "common", "BlackMythWukongBenchmarkTool");
            if (Directory.Exists(guess)) return guess;
        }
        return null;
    }
}
