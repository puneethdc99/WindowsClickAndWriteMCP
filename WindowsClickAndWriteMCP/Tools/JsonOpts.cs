using System.Text.Json;

namespace WindowsClickAndWriteMCP.Tools;

/// <summary>
/// Shared JSON serialization options for consistent MCP tool output formatting.
/// </summary>
internal static class JsonOpts
{
    internal static readonly JsonSerializerOptions Default = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
