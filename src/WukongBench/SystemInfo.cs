using System.Management;
using Microsoft.Win32;

namespace WukongBench;

public sealed class SystemInfo
{
    public string Cpu { get; set; } = "";
    public string CpuDetails { get; set; } = "";
    public List<string> Gpus { get; set; } = new();
    public string Ram { get; set; } = "";
    public List<string> RamModules { get; set; } = new();
    public string Motherboard { get; set; } = "";
    public string Os { get; set; } = "";
    public string Display { get; set; } = "";
    public string GameDisk { get; set; } = "";
    public string PowerPlan { get; set; } = "";

    public static SystemInfo Collect(string gameDir)
    {
        var s = new SystemInfo();

        Try(() =>
        {
            foreach (var o in Query("SELECT Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed, L3CacheSize FROM Win32_Processor"))
            {
                s.Cpu = o["Name"]?.ToString()?.Trim() ?? "";
                s.CpuDetails = $"{o["NumberOfCores"]} ядер / {o["NumberOfLogicalProcessors"]} потоков, " +
                               $"базовая {o["MaxClockSpeed"]} МГц, L3 {Convert.ToInt64(o["L3CacheSize"] ?? 0) / 1024} МБ";
            }
        });

        Try(() =>
        {
            var vram = ReadVramFromRegistry();
            foreach (var o in Query("SELECT Name, DriverVersion, AdapterRAM FROM Win32_VideoController"))
            {
                var name = o["Name"]?.ToString() ?? "?";
                // AdapterRAM в WMI — uint32 и обрезается на 4 ГБ, поэтому берём точное значение из реестра драйвера
                long bytes = vram.TryGetValue(name, out var v) ? v : Convert.ToInt64(o["AdapterRAM"] ?? 0L);
                s.Gpus.Add($"{name} — {bytes / 1024d / 1024 / 1024:0.#} ГБ VRAM, драйвер {o["DriverVersion"]}");
            }
        });

        Try(() =>
        {
            long total = 0;
            foreach (var o in Query("SELECT Capacity, Speed, ConfiguredClockSpeed, Manufacturer, PartNumber FROM Win32_PhysicalMemory"))
            {
                long cap = Convert.ToInt64(o["Capacity"] ?? 0L);
                total += cap;
                var speed = o["ConfiguredClockSpeed"] ?? o["Speed"];
                s.RamModules.Add($"{cap / 1024 / 1024 / 1024} ГБ @ {speed} МТ/с, {o["Manufacturer"]?.ToString()?.Trim()} {o["PartNumber"]?.ToString()?.Trim()}");
            }
            s.Ram = $"{total / 1024d / 1024 / 1024:0} ГБ ({s.RamModules.Count} модул.)";
        });

        Try(() =>
        {
            foreach (var o in Query("SELECT Manufacturer, Product FROM Win32_BaseBoard"))
                s.Motherboard = $"{o["Manufacturer"]} {o["Product"]}".Trim();
        });

        Try(() =>
        {
            foreach (var o in Query("SELECT Caption, Version, OSArchitecture FROM Win32_OperatingSystem"))
                s.Os = $"{o["Caption"]} {o["OSArchitecture"]} (build {o["Version"]})";
        });

        Try(() =>
        {
            var (w, h, hz) = NativeMethods.GetPrimaryDisplayMode();
            s.Display = $"{w}x{h} @ {hz} Гц";
        });

        Try(() =>
        {
            var root = Path.GetPathRoot(gameDir);
            if (root == null) return;
            var d = new DriveInfo(root);
            s.GameDisk = $"{d.Name} {d.DriveFormat}, свободно {d.AvailableFreeSpace / 1024d / 1024 / 1024:0} ГБ из {d.TotalSize / 1024d / 1024 / 1024:0} ГБ";
            foreach (var o in Query("SELECT Model, MediaType FROM Win32_DiskDrive"))
                s.GameDisk += $"; диск: {o["Model"]}";
        });

        Try(() =>
        {
            var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes");
            var active = key?.GetValue("ActivePowerScheme")?.ToString();
            if (active != null)
            {
                var name = key!.OpenSubKey(active)?.GetValue("FriendlyName")?.ToString() ?? active;
                s.PowerPlan = name.Contains(',') ? name.Split(',').Last() : name;
            }
        });

        return s;
    }

    private static Dictionary<string, long> ReadVramFromRegistry()
    {
        var map = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        using var cls = Registry.LocalMachine.OpenSubKey(@"SYSTEM\ControlSet001\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}");
        if (cls == null) return map;
        foreach (var sub in cls.GetSubKeyNames().Where(n => n.All(char.IsDigit)))
        {
            using var k = cls.OpenSubKey(sub);
            var desc = k?.GetValue("DriverDesc")?.ToString();
            var mem = k?.GetValue("HardwareInformation.qwMemorySize");
            if (desc == null || mem == null) continue;
            long bytes = mem switch { long l => l, byte[] b when b.Length >= 8 => BitConverter.ToInt64(b, 0), _ => 0 };
            if (bytes > 0) map[desc] = bytes;
        }
        return map;
    }

    private static IEnumerable<ManagementBaseObject> Query(string wql)
    {
        using var searcher = new ManagementObjectSearcher(wql);
        foreach (var o in searcher.Get()) yield return o;
    }

    private static void Try(Action a)
    {
        try { a(); } catch (Exception e) { Log.Warn("Не удалось получить часть характеристик: " + e.Message); }
    }
}
