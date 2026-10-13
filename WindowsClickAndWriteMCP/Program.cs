using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using WindowsClickAndWriteMCP.Services;

// ════════════════════════════════════════════════════════════════════
//  WindowsClickAndWriteMCP — Windows Click & Write MCP Server
//  
//  A local MCP server that provides tools to click any UI element
//  and type text into any input field on any Windows application.
//  
//  Transport: stdio (standard input/output)
//  Protocol:  Model Context Protocol (MCP)
// ════════════════════════════════════════════════════════════════════

var builder = Host.CreateApplicationBuilder(args);

// Ensure stdout is kept clean strictly for MCP JSON-RPC communication
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Register our automation service as a singleton (holds the UIA3 COM automation object)
builder.Services.AddSingleton<WindowsAutomationService>();

// Register the MCP server with stdio transport
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

var app = builder.Build();
await app.RunAsync();
