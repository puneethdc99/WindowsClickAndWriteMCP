using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using WindowsMCP.Services;

namespace WindowsMCP.Tools;

/// <summary>
/// MCP tools for typing/writing text into any input field on any Windows application.
/// Supports element-based typing (finds the field via UI Automation) and 
/// focus-based typing (types at whatever is currently focused).
/// Also supports sending keyboard shortcuts (Ctrl+C, Alt+F4, etc.).
/// </summary>
[McpServerToolType]
public sealed class TypeTools
{
    private readonly WindowsAutomationService _automation;

    public TypeTools(WindowsAutomationService automation) => _automation = automation;

    [McpServerTool(Name = "type_text"),
     Description("Type text into a specific UI element (text field, search box, input, etc.) on any Windows " +
                 "application. Finds the element using window title, process name, element name, " +
                 "automation ID, or control type. Uses the Value pattern when available for reliability, " +
                 "otherwise focuses the element and sends keystrokes.")]
    public string TypeText(
        [Description("The text to type into the element")]
        string text,

        [Description("Title (or partial title) of the target window. Example: 'Notepad', 'Chrome'")]
        string? windowTitle = null,

        [Description("Process name of the target application (without .exe). Example: 'notepad', 'chrome'")]
        string? processName = null,

        [Description("Display name of the target input element. Example: 'Search', 'URL bar'")]
        string? elementName = null,

        [Description("Automation ID of the target input element. More precise than name.")]
        string? automationId = null,

        [Description("Control type filter. Typically 'Edit' for text fields, 'ComboBox' for dropdowns.")]
        string? controlType = null,

        [Description("Whether to clear existing text before typing (default: true)")]
        bool clearFirst = true,

        [Description("Whether to press Enter after typing the text")]
        bool pressEnterAfter = false)
    {
        try
        {
            var result = _automation.TypeIntoElement(
                text, windowTitle, processName, elementName,
                automationId, controlType, clearFirst, pressEnterAfter);

            return JsonSerializer.Serialize(result, JsonOpts.Default);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(
                new TypeResult(false, $"Error: {ex.Message}"), JsonOpts.Default);
        }
    }

    [McpServerTool(Name = "type_at_cursor"),
     Description("Type text at the current keyboard focus / cursor position. " +
                 "Use this after you've already clicked or focused a text field. " +
                 "Works with any application — just types the characters as keyboard input.")]
    public string TypeAtCursor(
        [Description("The text to type")]
        string text,

        [Description("Whether to select all and delete existing text first (Ctrl+A, Delete)")]
        bool clearFirst = false,

        [Description("Whether to press Enter after typing")]
        bool pressEnterAfter = false)
    {
        try
        {
            var result = _automation.TypeAtFocus(text, clearFirst, pressEnterAfter);
            return JsonSerializer.Serialize(result, JsonOpts.Default);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(
                new TypeResult(false, $"Error: {ex.Message}"), JsonOpts.Default);
        }
    }

    [McpServerTool(Name = "send_keys"),
     Description("Send a keyboard shortcut or key combination. " +
                 "Examples: 'Ctrl+C' (copy), 'Ctrl+V' (paste), 'Alt+F4' (close), " +
                 "'Ctrl+Shift+S' (save as), 'Win+D' (show desktop), 'Enter', 'Tab', 'Escape'. " +
                 "Keys are specified as '+' separated names.")]
    public string SendKeys(
        [Description("Keyboard shortcut string. Examples: 'Ctrl+C', 'Alt+Tab', 'Ctrl+Shift+N', 'F5', 'Enter'")]
        string shortcut)
    {
        try
        {
            var result = _automation.SendKeyboardShortcut(shortcut);
            return JsonSerializer.Serialize(result, JsonOpts.Default);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(
                new TypeResult(false, $"Error: {ex.Message}"), JsonOpts.Default);
        }
    }
}
