using System.Text;

namespace WukongBench;

/// <summary>
/// Минималистичный редактор INI Unreal Engine, сохраняющий порядок строк, комментарии и неизвестные ключи.
/// (Стандартные парсеры ломают UE-специфичный синтаксис вроде +Key=, поэтому работаем построчно.)
/// </summary>
public sealed class IniFile
{
    private readonly List<string> _lines;
    public string Path { get; }

    private IniFile(string path, List<string> lines) { Path = path; _lines = lines; }

    public static IniFile Load(string path) =>
        new(path, File.Exists(path) ? File.ReadAllLines(path).ToList() : new List<string>());

    public void Save()
    {
        if (File.Exists(Path))
            File.SetAttributes(Path, File.GetAttributes(Path) & ~FileAttributes.ReadOnly);
        File.WriteAllLines(Path, _lines, new UTF8Encoding(false));
    }

    private static bool IsSection(string line, out string name)
    {
        var t = line.Trim();
        if (t.StartsWith('[') && t.EndsWith(']')) { name = t[1..^1]; return true; }
        name = ""; return false;
    }

    private static bool IsKey(string line, string key) =>
        line.TrimStart().StartsWith(key + "=", StringComparison.OrdinalIgnoreCase);

    /// <summary>Возвращает (начало, конец-исключительно) секции или null.</summary>
    private (int start, int end)? FindSection(string section)
    {
        for (int i = 0; i < _lines.Count; i++)
        {
            if (IsSection(_lines[i], out var n) && n.Equals(section, StringComparison.OrdinalIgnoreCase))
            {
                int j = i + 1;
                while (j < _lines.Count && !IsSection(_lines[j], out _)) j++;
                return (i, j);
            }
        }
        return null;
    }

    public IEnumerable<string> Sections()
    {
        foreach (var l in _lines)
            if (IsSection(l, out var n)) yield return n;
    }

    public string? Get(string section, string key)
    {
        var s = FindSection(section);
        if (s == null) return null;
        for (int i = s.Value.start + 1; i < s.Value.end; i++)
            if (IsKey(_lines[i], key))
                return _lines[i][(_lines[i].IndexOf('=') + 1)..];
        return null;
    }

    public void Set(string section, string key, string value)
    {
        var s = FindSection(section);
        if (s == null)
        {
            if (_lines.Count > 0 && _lines[^1].Trim().Length > 0) _lines.Add("");
            _lines.Add($"[{section}]");
            _lines.Add($"{key}={value}");
            return;
        }

        for (int i = s.Value.start + 1; i < s.Value.end; i++)
        {
            if (IsKey(_lines[i], key)) { _lines[i] = $"{key}={value}"; return; }
        }

        // Вставляем перед хвостовыми пустыми строками секции
        int insertAt = s.Value.end;
        while (insertAt - 1 > s.Value.start && _lines[insertAt - 1].Trim().Length == 0) insertAt--;
        _lines.Insert(insertAt, $"{key}={value}");
    }

    /// <summary>Устанавливает ключ во всех секциях, где он уже есть; если нигде нет — в секции по умолчанию.</summary>
    public void SetEverywhere(string key, string value, string defaultSection)
    {
        bool found = false;
        string? current = null;
        for (int i = 0; i < _lines.Count; i++)
        {
            if (IsSection(_lines[i], out var n)) { current = n; continue; }
            if (current != null && IsKey(_lines[i], key)) { _lines[i] = $"{key}={value}"; found = true; }
        }
        if (!found) Set(defaultSection, key, value);
    }

    /// <summary>Находит строку вида Key=... в любой секции (для UISettingData).</summary>
    public (string section, string value)? FindAny(string key)
    {
        string? current = null;
        foreach (var l in _lines)
        {
            if (IsSection(l, out var n)) { current = n; continue; }
            if (current != null && IsKey(l, key)) return (current, l[(l.IndexOf('=') + 1)..]);
        }
        return null;
    }
}
