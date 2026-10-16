using System.Runtime.InteropServices;
using System.Text;

namespace Black_Jack;

static class Screen
{
    private const string Escape = "\u001b[";

    private const int StandardOutputHandle = -11;
    private const uint EnableVirtualTerminalProcessing = 0x0004;

    private static readonly object writeLock = new object();
    private static readonly Stream output = Console.OpenStandardOutput();

    private static string[] previousLines = [];
    private static int previousOffset = -1;
    private static int previousWidth = -1;
    private static int previousHeight = -1;

    private static bool isActive;

    // =========================================================
    // ENTER / LEAVE
    // =========================================================

    public static void Enter()
    {
        EnableVirtualTerminal();

        lock (writeLock)
        {
            Write(
                Escape + "?1049h" +
                Escape + "?25l" +
                Escape + "?7l" +
                Escape + "2J"
            );

            isActive = true;
            Invalidate();
        }
    }

    public static void Leave()
    {
        lock (writeLock)
        {
            if (!isActive)
            {
                return;
            }

            isActive = false;

            Write(
                Escape + "0m" +
                Escape + "?7h" +
                Escape + "?25h" +
                Escape + "?1049l"
            );
        }
    }

    // =========================================================
    // DRAWING
    // =========================================================

    public static void Present(
        IReadOnlyList<string> lines,
        int contentWidth
    )
    {
        lock (writeLock)
        {
            if (!isActive)
            {
                return;
            }

            int width = Console.WindowWidth;
            int height = Console.WindowHeight;

            int offset =
                Math.Max(0, (width - contentWidth) / 2);

            bool redrawAll =
                width != previousWidth ||
                height != previousHeight ||
                offset != previousOffset;

            StringBuilder text = new StringBuilder();

            text.Append(Escape + "?2026h");

            if (redrawAll)
            {
                text.Append(Escape + "0m" + Escape + "2J");
            }

            string padding = new string(' ', offset);
            int visibleRows = Math.Min(lines.Count, height);

            for (int row = 0; row < visibleRows; row++)
            {
                bool changed =
                    redrawAll ||
                    row >= previousLines.Length ||
                    previousLines[row] != lines[row];

                if (!changed)
                {
                    continue;
                }

                MoveTo(text, row);

                text.Append(padding);
                text.Append(lines[row]);
                text.Append(Escape + "0m");
                text.Append(Escape + "K");
            }

            if (!redrawAll)
            {
                int oldRows =
                    Math.Min(previousLines.Length, height);

                for (int row = lines.Count; row < oldRows; row++)
                {
                    MoveTo(text, row);
                    text.Append(Escape + "2K");
                }
            }

            text.Append(Escape + "?2026l");

            Write(text.ToString());

            previousLines = lines.ToArray();
            previousOffset = offset;
            previousWidth = width;
            previousHeight = height;
        }
    }

    public static bool HasWindowResized()
    {
        return
            Console.WindowWidth != previousWidth ||
            Console.WindowHeight != previousHeight;
    }

    public static void Invalidate()
    {
        previousWidth = -1;
        previousHeight = -1;
    }

    // =========================================================
    // CURSOR
    // =========================================================

    public static void ShowCursorAt(int row, int column)
    {
        lock (writeLock)
        {
            Write(
                Escape + row + ";" + (previousOffset + column) + "H" +
                Escape + "?25h"
            );
        }
    }

    public static void HideCursor()
    {
        lock (writeLock)
        {
            Write(Escape + "?25l");
        }
    }

    // =========================================================
    // LOW LEVEL
    // =========================================================

    private static void MoveTo(StringBuilder text, int row)
    {
        text.Append(Escape).Append(row + 1).Append(";1H");
    }

    private static void Write(string text)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);

        output.Write(bytes);
        output.Flush();
    }

    // =========================================================
    // WINDOWS TERMINAL MODE
    // =========================================================

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetStdHandle(int handle);

    [DllImport("kernel32.dll")]
    private static extern bool GetConsoleMode(IntPtr handle, out uint mode);

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleMode(IntPtr handle, uint mode);

    private static void EnableVirtualTerminal()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        IntPtr handle = GetStdHandle(StandardOutputHandle);

        if (GetConsoleMode(handle, out uint mode))
        {
            SetConsoleMode(
                handle,
                mode | EnableVirtualTerminalProcessing
            );
        }
    }
}
