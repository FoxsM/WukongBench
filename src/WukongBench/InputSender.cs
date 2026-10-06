using System.Runtime.InteropServices;
using static WukongBench.NativeMethods;

namespace WukongBench;

/// <summary>
/// Эмуляция клавиатуры через SendInput со скан-кодами — UE-игры читают raw input,
/// поэтому PostMessage/WM_KEYDOWN они часто игнорируют.
/// </summary>
public static class InputSender
{
    private static readonly Dictionary<string, (ushort vk, bool extended)> Keys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Enter"] = (0x0D, false), ["Space"] = (0x20, false), ["Escape"] = (0x1B, false), ["Esc"] = (0x1B, false),
        ["Tab"] = (0x09, false), ["Up"] = (0x26, true), ["Down"] = (0x28, true),
        ["Left"] = (0x25, true), ["Right"] = (0x27, true), ["F1"] = (0x70, false), ["F2"] = (0x71, false)
    };

    public static void FocusWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return;
        ShowWindow(hwnd, SW_RESTORE);

        // Windows запрещает «красть» фокус — трюк с Alt + AttachThreadInput снимает ограничение.
        keybd_event(0x12, 0, 0, UIntPtr.Zero);
        keybd_event(0x12, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

        var fg = GetForegroundWindow();
        uint fgThread = GetWindowThreadProcessId(fg, out _);
        uint myThread = GetCurrentThreadId();
        if (fgThread != myThread) AttachThreadInput(myThread, fgThread, true);
        BringWindowToTop(hwnd);
        SetForegroundWindow(hwnd);
        if (fgThread != myThread) AttachThreadInput(myThread, fgThread, false);
    }

    public static void PressKey(string name)
    {
        (ushort vk, bool ext) key;
        if (Keys.TryGetValue(name, out var k)) key = k;
        else if (name.Length == 1 && char.IsLetterOrDigit(name[0])) key = ((ushort)char.ToUpperInvariant(name[0]), false);
        else throw new ArgumentException($"Неизвестная клавиша: {name}");

        ushort scan = (ushort)MapVirtualKey(key.vk, 0);
        uint flags = KEYEVENTF_SCANCODE | (key.ext ? KEYEVENTF_EXTENDEDKEY : 0);

        var inputs = new[]
        {
            new INPUT { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wVk = key.vk, wScan = scan, dwFlags = flags } } },
            new INPUT { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wVk = key.vk, wScan = scan, dwFlags = flags | KEYEVENTF_KEYUP } } }
        };
        SendInput(1, new[] { inputs[0] }, Marshal.SizeOf<INPUT>());
        Thread.Sleep(60);
        SendInput(1, new[] { inputs[1] }, Marshal.SizeOf<INPUT>());
    }
}
