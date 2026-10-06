namespace WukongBench;

public static class Log
{
    private static readonly object Sync = new();

    private static void Write(ConsoleColor color, string tag, string msg)
    {
        lock (Sync)
        {
            var old = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write($"[{DateTime.Now:HH:mm:ss}] {tag} ");
            Console.ForegroundColor = old;
            Console.WriteLine(msg);
        }
    }

    public static void Info(string m) => Write(ConsoleColor.Cyan, "[i]", m);
    public static void Ok(string m) => Write(ConsoleColor.Green, "[✓]", m);
    public static void Warn(string m) => Write(ConsoleColor.Yellow, "[!]", m);
    public static void Error(string m) => Write(ConsoleColor.Red, "[x]", m);
}
