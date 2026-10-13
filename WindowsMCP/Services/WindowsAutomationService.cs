using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using WindowsMCP.Native;

namespace WindowsMCP.Services;

/// <summary>
/// Core automation service that provides two categories of interaction:
/// 
/// 1. **Element-based** — Finds UI elements via UI Automation properties 
///    (Name, AutomationId, ControlType, window title, process name) then
///    invokes their Invoke/Toggle/SelectionItem patterns or clicks their
///    center point.
///    
/// 2. **Coordinate-based** — Directly clicks or types at absolute screen 
///    coordinates using Win32 SendInput/mouse_event for scenarios where
///    UI Automation elements aren't available (custom-drawn UIs, games, etc.).
/// </summary>
public sealed class WindowsAutomationService : IDisposable
{
    private readonly UIA3Automation _automation;

    public WindowsAutomationService()
    {
        // Ensure DPI awareness so screen coordinates are accurate on high-DPI displays
        NativeMethods.SetProcessDPIAware();
        _automation = new UIA3Automation();
    }

    // ════════════════════════════════════════════════════════════════════
    //  LISTING / DISCOVERY
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Lists all visible top-level windows with their title, process name, and handle.
    /// </summary>
    public List<WindowInfo> ListWindows()
    {
        var windows = new List<WindowInfo>();
        NativeMethods.EnumWindows((hWnd, _) =>
        {
            if (!NativeMethods.IsWindowVisible(hWnd)) return true;

            int len = NativeMethods.GetWindowTextLength(hWnd);
            if (len == 0) return true;

            var sb = new StringBuilder(len + 1);
            NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
            string title = sb.ToString();

            NativeMethods.GetWindowThreadProcessId(hWnd, out uint pid);
            string processName = "";
            try { processName = Process.GetProcessById((int)pid).ProcessName; }
            catch { /* process may have exited */ }

            windows.Add(new WindowInfo(title, processName, (long)hWnd));
            return true;
        }, nint.Zero);
        return windows;
    }

    /// <summary>
    /// Lists interactive UI elements within a window, filtered by optional criteria.
    /// Returns elements that can be clicked or typed into.
    /// </summary>
    public List<ElementInfo> ListElements(
        string? windowTitle = null,
        string? processName = null,
        string? controlType = null,
        int maxDepth = 5,
        int maxResults = 50)
    {
        var root = FindTargetWindow(windowTitle, processName);
        if (root == null)
            throw new InvalidOperationException(
                $"Could not find window (title='{windowTitle}', process='{processName}').");

        var results = new List<ElementInfo>();
        CollectElements(root, controlType, maxDepth, 0, results, maxResults);
        return results;
    }

    private void CollectElements(
        AutomationElement parent,
        string? controlType,
        int maxDepth, int currentDepth,
        List<ElementInfo> results, int maxResults)
    {
        if (currentDepth > maxDepth || results.Count >= maxResults) return;

        AutomationElement[] children;
        try
        {
            children = parent.FindAllChildren();
        }
        catch { return; }

        foreach (var child in children)
        {
            if (results.Count >= maxResults) return;
            try
            {
                var ct = child.ControlType;
                string ctName = ct.ToString();

                // Filter by control type if specified
                if (controlType != null &&
                    !ctName.Equals(controlType, StringComparison.OrdinalIgnoreCase))
                {
                    // Still recurse into containers
                    CollectElements(child, controlType, maxDepth, currentDepth + 1, results, maxResults);
                    continue;
                }

                // Only include interactive elements
                bool isInteractive = ct == ControlType.Button ||
                                     ct == ControlType.Edit ||
                                     ct == ControlType.CheckBox ||
                                     ct == ControlType.RadioButton ||
                                     ct == ControlType.ComboBox ||
                                     ct == ControlType.ListItem ||
                                     ct == ControlType.MenuItem ||
                                     ct == ControlType.TabItem ||
                                     ct == ControlType.Hyperlink ||
                                     ct == ControlType.TreeItem ||
                                     ct == ControlType.Slider ||
                                     ct == ControlType.DataItem ||
                                     ct == ControlType.Custom ||
                                     ct == ControlType.SplitButton ||
                                     ct == ControlType.Text;

                if (isInteractive || controlType != null)
                {
                    var rect = child.BoundingRectangle;
                    results.Add(new ElementInfo(
                        Name: child.Name ?? "",
                        AutomationId: child.AutomationId ?? "",
                        ControlType: ctName,
                        ClassName: child.ClassName ?? "",
                        BoundingRectangle: rect.IsEmpty ? null :
                            $"{rect.X},{rect.Y},{rect.Width},{rect.Height}",
                        IsEnabled: child.IsEnabled,
                        Value: TryGetValue(child)
                    ));
                }

                CollectElements(child, controlType, maxDepth, currentDepth + 1, results, maxResults);
            }
            catch { /* skip inaccessible elements */ }
        }
    }

    private static string? TryGetValue(AutomationElement el)
    {
        try
        {
            if (el.Patterns.Value.IsSupported)
                return el.Patterns.Value.Pattern.Value.Value;
        }
        catch { }
        return null;
    }

    // ════════════════════════════════════════════════════════════════════
    //  CLICKING
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Click a UI element found by searching for it within a window.
    /// Falls back to coordinate-based click on the element's center if 
    /// Invoke/Toggle patterns aren't available.
    /// </summary>
    public ClickResult ClickElement(
        string? windowTitle = null,
        string? processName = null,
        string? elementName = null,
        string? automationId = null,
        string? controlType = null,
        string button = "left",
        bool doubleClick = false)
    {
        var window = FindTargetWindow(windowTitle, processName);
        if (window == null)
            return new ClickResult(false, "Window not found.");

        // Bring window to foreground
        BringToForeground(window);

        var element = FindElement(window, elementName, automationId, controlType);
        if (element == null)
            return new ClickResult(false,
                $"Element not found (name='{elementName}', id='{automationId}', type='{controlType}').");

        // Try pattern-based invocation first (most reliable)
        if (button == "left" && !doubleClick)
        {
            try
            {
                if (element.Patterns.Invoke.IsSupported)
                {
                    element.Patterns.Invoke.Pattern.Invoke();
                    return new ClickResult(true,
                        $"Invoked element '{element.Name}' via InvokePattern.");
                }
                if (element.Patterns.Toggle.IsSupported)
                {
                    element.Patterns.Toggle.Pattern.Toggle();
                    return new ClickResult(true,
                        $"Toggled element '{element.Name}' via TogglePattern.");
                }
                if (element.Patterns.SelectionItem.IsSupported)
                {
                    element.Patterns.SelectionItem.Pattern.Select();
                    return new ClickResult(true,
                        $"Selected element '{element.Name}' via SelectionItemPattern.");
                }
                if (element.Patterns.ExpandCollapse.IsSupported)
                {
                    var pattern = element.Patterns.ExpandCollapse.Pattern;
                    if (pattern.ExpandCollapseState == ExpandCollapseState.Collapsed)
                        pattern.Expand();
                    else
                        pattern.Collapse();
                    return new ClickResult(true,
                        $"Toggled expand/collapse on '{element.Name}'.");
                }
            }
            catch { /* fall through to coordinate click */ }
        }

        // Fallback: click the center of the element's bounding rectangle
        var rect = element.BoundingRectangle;
        if (rect.IsEmpty)
            return new ClickResult(false, "Element has no bounding rectangle.");

        int cx = rect.X + rect.Width / 2;
        int cy = rect.Y + rect.Height / 2;

        PerformClick(cx, cy, button, doubleClick);
        return new ClickResult(true,
            $"Clicked '{element.Name}' at ({cx},{cy}) via coordinate click.");
    }

    /// <summary>
    /// Click at absolute screen coordinates. Works on any part of the screen,
    /// any application, custom-drawn UIs, games, etc.
    /// </summary>
    public ClickResult ClickAtCoordinates(int x, int y, string button = "left", bool doubleClick = false)
    {
        PerformClick(x, y, button, doubleClick);
        return new ClickResult(true, $"Clicked at ({x},{y}) with {button} button.");
    }

    private static void PerformClick(int x, int y, string button, bool doubleClick)
    {
        NativeMethods.SetCursorPos(x, y);
        Thread.Sleep(50); // brief pause for cursor to settle

        uint down, up;
        switch (button.ToLowerInvariant())
        {
            case "right":
                down = NativeMethods.MOUSEEVENTF_RIGHTDOWN;
                up = NativeMethods.MOUSEEVENTF_RIGHTUP;
                break;
            case "middle":
                down = NativeMethods.MOUSEEVENTF_MIDDLEDOWN;
                up = NativeMethods.MOUSEEVENTF_MIDDLEUP;
                break;
            default:
                down = NativeMethods.MOUSEEVENTF_LEFTDOWN;
                up = NativeMethods.MOUSEEVENTF_LEFTUP;
                break;
        }

        NativeMethods.mouse_event(down, 0, 0, 0, nint.Zero);
        NativeMethods.mouse_event(up, 0, 0, 0, nint.Zero);

        if (doubleClick)
        {
            Thread.Sleep(50);
            NativeMethods.mouse_event(down, 0, 0, 0, nint.Zero);
            NativeMethods.mouse_event(up, 0, 0, 0, nint.Zero);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  TYPING / WRITING
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Types text into a specific UI element found by searching within a window.
    /// Uses the Value pattern when available, otherwise falls back to 
    /// focusing the element and sending keystrokes via SendInput.
    /// </summary>
    public TypeResult TypeIntoElement(
        string text,
        string? windowTitle = null,
        string? processName = null,
        string? elementName = null,
        string? automationId = null,
        string? controlType = null,
        bool clearFirst = true,
        bool pressEnterAfter = false)
    {
        var window = FindTargetWindow(windowTitle, processName);
        if (window == null)
            return new TypeResult(false, "Window not found.");

        BringToForeground(window);

        var element = FindElement(window, elementName, automationId, controlType);
        if (element == null)
            return new TypeResult(false,
                $"Element not found (name='{elementName}', id='{automationId}', type='{controlType}').");

        // Try ValuePattern first (most reliable for text fields)
        try
        {
            if (element.Patterns.Value.IsSupported)
            {
                var pattern = element.Patterns.Value.Pattern;
                if (!pattern.IsReadOnly)
                {
                    if (clearFirst)
                        pattern.SetValue(text);
                    else
                    {
                        string existing = pattern.Value.Value ?? "";
                        pattern.SetValue(existing + text);
                    }

                    if (pressEnterAfter)
                    {
                        element.Focus();
                        Thread.Sleep(100);
                        SendKeyPress(0x0D); // VK_RETURN
                    }

                    return new TypeResult(true,
                        $"Set value on '{element.Name}' via ValuePattern.");
                }
            }
        }
        catch { /* fall through */ }

        // Fallback: focus and use SendInput for keystrokes
        try { element.Focus(); }
        catch
        {
            // If focus fails, click the element center
            var rect = element.BoundingRectangle;
            if (!rect.IsEmpty)
            {
                PerformClick(rect.X + rect.Width / 2, rect.Y + rect.Height / 2, "left", false);
                Thread.Sleep(100);
            }
        }

        Thread.Sleep(150);

        if (clearFirst)
        {
            // Select all (Ctrl+A) then delete
            SendKeyCombo(0x11, 0x41); // Ctrl+A
            Thread.Sleep(50);
            SendKeyPress(0x2E); // VK_DELETE
            Thread.Sleep(50);
        }

        SendUnicodeString(text);

        if (pressEnterAfter)
        {
            Thread.Sleep(50);
            SendKeyPress(0x0D); // VK_RETURN
        }

        return new TypeResult(true,
            $"Typed into '{element.Name}' via keyboard input.");
    }

    /// <summary>
    /// Types text at the current cursor/focus position using SendInput.
    /// Useful when you've already clicked or focused a field.
    /// </summary>
    public TypeResult TypeAtFocus(string text, bool clearFirst = false, bool pressEnterAfter = false)
    {
        if (clearFirst)
        {
            SendKeyCombo(0x11, 0x41); // Ctrl+A
            Thread.Sleep(50);
            SendKeyPress(0x2E); // VK_DELETE
            Thread.Sleep(50);
        }

        SendUnicodeString(text);

        if (pressEnterAfter)
        {
            Thread.Sleep(50);
            SendKeyPress(0x0D);
        }

        return new TypeResult(true, $"Typed {text.Length} characters at current focus.");
    }

    /// <summary>
    /// Sends a keyboard shortcut (e.g., Ctrl+C, Alt+F4).
    /// </summary>
    public TypeResult SendKeyboardShortcut(string shortcut)
    {
        var parts = shortcut.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return new TypeResult(false, "Empty shortcut.");

        var vkCodes = new List<ushort>();
        foreach (var part in parts)
        {
            ushort vk = MapKeyName(part);
            if (vk == 0)
                return new TypeResult(false, $"Unknown key: '{part}'");
            vkCodes.Add(vk);
        }

        // Press all keys down in order
        foreach (var vk in vkCodes)
            SendKeyDown(vk);

        Thread.Sleep(50);

        // Release in reverse order
        for (int i = vkCodes.Count - 1; i >= 0; i--)
            SendKeyUp(vkCodes[i]);

        return new TypeResult(true, $"Sent shortcut: {shortcut}");
    }

    // ════════════════════════════════════════════════════════════════════
    //  PRIVATE HELPERS
    // ════════════════════════════════════════════════════════════════════

    private AutomationElement? FindTargetWindow(string? windowTitle, string? processName)
    {
        var desktop = _automation.GetDesktop();

        if (windowTitle != null)
        {
            // Try exact match first, then contains
            var win = desktop.FindFirstDescendant(cf =>
                cf.ByControlType(ControlType.Window).And(cf.ByName(windowTitle)));
            if (win != null) return win;

            // Partial match
            var allWindows = desktop.FindAllChildren(cf => cf.ByControlType(ControlType.Window));
            foreach (var w in allWindows)
            {
                try
                {
                    if (w.Name?.Contains(windowTitle, StringComparison.OrdinalIgnoreCase) == true)
                        return w;
                }
                catch { }
            }
        }

        if (processName != null)
        {
            var processes = Process.GetProcessesByName(processName);
            foreach (var proc in processes)
            {
                if (proc.MainWindowHandle != nint.Zero)
                {
                    try
                    {
                        var win = _automation.FromHandle(proc.MainWindowHandle);
                        if (win != null) return win;
                    }
                    catch { }
                }
            }
        }

        // If no criteria specified, use the foreground window
        if (windowTitle == null && processName == null)
        {
            var fgHandle = NativeMethods.GetForegroundWindow();
            if (fgHandle != nint.Zero)
            {
                try { return _automation.FromHandle(fgHandle); }
                catch { }
            }
        }

        return null;
    }

    private AutomationElement? FindElement(
        AutomationElement parent,
        string? name, string? automationId, string? controlType)
    {
        var cf = _automation.ConditionFactory;
        ConditionBase? condition = null;

        if (automationId != null)
        {
            condition = cf.ByAutomationId(automationId);
        }

        if (name != null)
        {
            var nameCond = cf.ByName(name);
            condition = condition != null
                ? new AndCondition(condition, nameCond)
                : nameCond;
        }

        if (controlType != null && Enum.TryParse<ControlType>(controlType, true, out var ct))
        {
            var typeCond = cf.ByControlType(ct);
            condition = condition != null
                ? new AndCondition(condition, typeCond)
                : typeCond;
        }

        if (condition == null)
            return null;

        // Search descendants (deeper search)
        var element = parent.FindFirstDescendant(condition);
        return element;
    }

    private static void BringToForeground(AutomationElement window)
    {
        try
        {
            var handle = window.Properties.NativeWindowHandle.Value;
            NativeMethods.ShowWindow(handle, NativeMethods.SW_RESTORE);
            NativeMethods.SetForegroundWindow(handle);
            Thread.Sleep(200);
        }
        catch { }
    }

    // --- Low-level input helpers ---

    private static void SendUnicodeString(string text)
    {
        foreach (char c in text)
        {
            var inputs = new NativeMethods.INPUT[2];

            inputs[0].type = NativeMethods.INPUT_KEYBOARD;
            inputs[0].U.ki.wVk = 0;
            inputs[0].U.ki.wScan = c;
            inputs[0].U.ki.dwFlags = NativeMethods.KEYEVENTF_UNICODE;

            inputs[1].type = NativeMethods.INPUT_KEYBOARD;
            inputs[1].U.ki.wVk = 0;
            inputs[1].U.ki.wScan = c;
            inputs[1].U.ki.dwFlags = NativeMethods.KEYEVENTF_UNICODE | NativeMethods.KEYEVENTF_KEYUP;

            NativeMethods.SendInput(2, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
            Thread.Sleep(5); // small delay between characters for reliability
        }
    }

    private static void SendKeyPress(ushort vkCode)
    {
        SendKeyDown(vkCode);
        Thread.Sleep(30);
        SendKeyUp(vkCode);
    }

    private static void SendKeyDown(ushort vkCode)
    {
        var input = new NativeMethods.INPUT[1];
        input[0].type = NativeMethods.INPUT_KEYBOARD;
        input[0].U.ki.wVk = vkCode;
        input[0].U.ki.dwFlags = 0;
        NativeMethods.SendInput(1, input, Marshal.SizeOf<NativeMethods.INPUT>());
    }

    private static void SendKeyUp(ushort vkCode)
    {
        var input = new NativeMethods.INPUT[1];
        input[0].type = NativeMethods.INPUT_KEYBOARD;
        input[0].U.ki.wVk = vkCode;
        input[0].U.ki.dwFlags = NativeMethods.KEYEVENTF_KEYUP;
        NativeMethods.SendInput(1, input, Marshal.SizeOf<NativeMethods.INPUT>());
    }

    private static void SendKeyCombo(ushort modifier, ushort key)
    {
        SendKeyDown(modifier);
        Thread.Sleep(20);
        SendKeyPress(key);
        Thread.Sleep(20);
        SendKeyUp(modifier);
    }

    private static ushort MapKeyName(string name) => name.ToUpperInvariant() switch
    {
        "CTRL" or "CONTROL" => 0x11,
        "ALT" or "MENU" => 0x12,
        "SHIFT" => 0x10,
        "WIN" or "WINDOWS" or "LWIN" => 0x5B,
        "ENTER" or "RETURN" => 0x0D,
        "TAB" => 0x09,
        "ESC" or "ESCAPE" => 0x1B,
        "SPACE" => 0x20,
        "BACKSPACE" or "BACK" => 0x08,
        "DELETE" or "DEL" => 0x2E,
        "HOME" => 0x24,
        "END" => 0x23,
        "PAGEUP" or "PGUP" => 0x21,
        "PAGEDOWN" or "PGDN" => 0x22,
        "UP" => 0x26,
        "DOWN" => 0x28,
        "LEFT" => 0x25,
        "RIGHT" => 0x27,
        "INSERT" or "INS" => 0x2D,
        "PRINTSCREEN" or "PRTSC" => 0x2C,
        "F1" => 0x70, "F2" => 0x71, "F3" => 0x72, "F4" => 0x73,
        "F5" => 0x74, "F6" => 0x75, "F7" => 0x76, "F8" => 0x77,
        "F9" => 0x78, "F10" => 0x79, "F11" => 0x7A, "F12" => 0x7B,
        "A" => 0x41, "B" => 0x42, "C" => 0x43, "D" => 0x44,
        "E" => 0x45, "F" => 0x46, "G" => 0x47, "H" => 0x48,
        "I" => 0x49, "J" => 0x4A, "K" => 0x4B, "L" => 0x4C,
        "M" => 0x4D, "N" => 0x4E, "O" => 0x4F, "P" => 0x50,
        "Q" => 0x51, "R" => 0x52, "S" => 0x53, "T" => 0x54,
        "U" => 0x55, "V" => 0x56, "W" => 0x57, "X" => 0x58,
        "Y" => 0x59, "Z" => 0x5A,
        "0" => 0x30, "1" => 0x31, "2" => 0x32, "3" => 0x33,
        "4" => 0x34, "5" => 0x35, "6" => 0x36, "7" => 0x37,
        "8" => 0x38, "9" => 0x39,
        _ => 0
    };

    public void Dispose() => _automation.Dispose();
}

// ════════════════════════════════════════════════════════════════════
//  DTOs
// ════════════════════════════════════════════════════════════════════

public record WindowInfo(string Title, string ProcessName, long Handle);
public record ElementInfo(string Name, string AutomationId, string ControlType,
    string ClassName, string? BoundingRectangle, bool IsEnabled, string? Value);
public record ClickResult(bool Success, string Message);
public record TypeResult(bool Success, string Message);
