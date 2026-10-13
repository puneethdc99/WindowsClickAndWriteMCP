using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using WindowsMCP.Services;

namespace WindowsMCP.Tools;

/// <summary>
/// MCP tools for clicking UI elements and screen coordinates on Windows.
/// Supports element-based clicking (by name, automation ID, control type) 
/// and raw coordinate clicking for any part of the screen.
/// </summary>
[McpServerToolType]
public sealed class ClickTools
{
    private readonly WindowsAutomationService _automation;

    public ClickTools(WindowsAutomationService automation) => _automation = automation;

    [McpServerTool(Name = "click_element"),
     Description("Click a UI element on any Windows application. Finds the element using " +
                 "window title, process name, element name, automation ID, or control type. " +
                 "Supports left/right/middle click and double-click. " +
                 "Uses UI Automation patterns (Invoke, Toggle, etc.) when available, " +
                 "otherwise falls back to clicking the element's center coordinates.")]
    public string ClickElement(
        [Description("Title (or partial title) of the target window. Example: 'Notepad', 'Calculator'")]
        string? windowTitle = null,

        [Description("Process name of the target application (without .exe). Example: 'notepad', 'chrome'")]
        string? processName = null,

        [Description("Display name of the UI element to click. Example: 'Save', 'OK', 'File'")]
        string? elementName = null,

        [Description("Automation ID of the UI element. More precise than name. Use list_elements to discover IDs.")]
        string? automationId = null,

        [Description("Control type filter. Values: Button, Edit, CheckBox, RadioButton, ComboBox, MenuItem, TabItem, Hyperlink, ListItem, TreeItem")]
        string? controlType = null,

        [Description("Mouse button to use: 'left' (default), 'right', or 'middle'")]
        string button = "left",

        [Description("Whether to double-click instead of single-click")]
        bool doubleClick = false)
    {
        try
        {
            var result = _automation.ClickElement(
                windowTitle, processName, elementName,
                automationId, controlType, button, doubleClick);

            return JsonSerializer.Serialize(result, JsonOpts.Default);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(
                new ClickResult(false, $"Error: {ex.Message}"), JsonOpts.Default);
        }
    }

    [McpServerTool(Name = "click_screen"),
     Description("Click at absolute screen coordinates (x, y). Works on ANY part of the screen — " +
                 "any application, custom-drawn UI, game, desktop, taskbar, system tray, etc. " +
                 "Use this when you know the exact pixel coordinates to click.")]
    public string ClickScreen(
        [Description("X coordinate (pixels from left edge of primary screen)")]
        int x,

        [Description("Y coordinate (pixels from top edge of primary screen)")]
        int y,

        [Description("Mouse button: 'left' (default), 'right', or 'middle'")]
        string button = "left",

        [Description("Whether to double-click")]
        bool doubleClick = false)
    {
        try
        {
            var result = _automation.ClickAtCoordinates(x, y, button, doubleClick);
            return JsonSerializer.Serialize(result, JsonOpts.Default);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(
                new ClickResult(false, $"Error: {ex.Message}"), JsonOpts.Default);
        }
    }
}
