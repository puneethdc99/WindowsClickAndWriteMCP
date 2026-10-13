using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using WindowsClickAndWriteMCP.Services;

namespace WindowsClickAndWriteMCP.Tools;

/// <summary>
/// MCP tools for discovering windows and UI elements on the screen.
/// Essential for finding the correct element names, automation IDs, and 
/// control types to pass to click_element and type_text.
/// </summary>
[McpServerToolType]
public sealed class DiscoveryTools
{
    private readonly WindowsAutomationService _automation;

    public DiscoveryTools(WindowsAutomationService automation) => _automation = automation;

    [McpServerTool(Name = "list_windows"),
     Description("List all visible windows on the desktop with their titles, process names, " +
                 "and window handles. Use this to discover what applications are running " +
                 "and find the correct window title or process name for other tools.")]
    public string ListWindows()
    {
        try
        {
            var windows = _automation.ListWindows();
            return JsonSerializer.Serialize(windows, JsonOpts.Default);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, JsonOpts.Default);
        }
    }

    [McpServerTool(Name = "list_elements"),
     Description("List interactive UI elements within a window. Returns element names, " +
                 "automation IDs, control types, bounding rectangles, and current values. " +
                 "Use this to discover what elements are available for clicking or typing. " +
                 "Essential for finding the correct parameters for click_element and type_text.")]
    public string ListElements(
        [Description("Title (or partial title) of the target window")]
        string? windowTitle = null,

        [Description("Process name of the target application (without .exe)")]
        string? processName = null,

        [Description("Filter by control type: Button, Edit, CheckBox, RadioButton, ComboBox, MenuItem, TabItem, etc.")]
        string? controlType = null,

        [Description("Maximum depth to search in the UI tree (default: 5). Increase for deeply nested elements.")]
        int maxDepth = 5,

        [Description("Maximum number of elements to return (default: 50)")]
        int maxResults = 50)
    {
        try
        {
            var elements = _automation.ListElements(
                windowTitle, processName, controlType, maxDepth, maxResults);
            return JsonSerializer.Serialize(elements, JsonOpts.Default);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, JsonOpts.Default);
        }
    }
}
