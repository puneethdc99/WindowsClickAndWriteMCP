# WindowsMCP — Windows Click & Write MCP Server

A .NET 10 MCP (Model Context Protocol) server that enables AI agents to **click** any UI element and **type** text into any input field on **any Windows application**.

## Features

### 🖱️ Click Tools
| Tool | Description |
|------|-------------|
| `click_element` | Click a UI element by name, automation ID, or control type. Uses UI Automation patterns (Invoke, Toggle, etc.) with coordinate-click fallback. |
| `click_screen` | Click at absolute screen coordinates (x, y). Works on any part of the screen — custom UIs, games, desktop, taskbar. |

### ⌨️ Type Tools
| Tool | Description |
|------|-------------|
| `type_text` | Type text into a specific input field found by name/automation ID. Uses Value pattern or SendInput keystrokes. |
| `type_at_cursor` | Type text at the current focus/cursor position. |
| `send_keys` | Send keyboard shortcuts (Ctrl+C, Alt+F4, Win+D, etc.). |

### 🔍 Discovery Tools
| Tool | Description |
|------|-------------|
| `list_windows` | List all visible windows with titles, process names, and handles. |
| `list_elements` | List interactive UI elements within a window (names, IDs, types, bounds). |

## Architecture

```
Program.cs                    — MCP server entry point (stdio transport)
├── Tools/
│   ├── ClickTools.cs         — click_element, click_screen
│   ├── TypeTools.cs          — type_text, type_at_cursor, send_keys
│   ├── DiscoveryTools.cs     — list_windows, list_elements
│   └── JsonOpts.cs           — Shared serialization config
├── Services/
│   └── WindowsAutomationService.cs  — Core automation (FlaUI + Win32)
└── Native/
    └── NativeMethods.cs      — Win32 P/Invoke declarations
```

**Technology Stack:**
- **FlaUI (UIA3)** — Windows UI Automation for element discovery and pattern-based interaction
- **Win32 P/Invoke** — `SendInput`, `mouse_event`, `SetCursorPos` for raw input
- **ModelContextProtocol SDK** — Official .NET MCP server implementation
- **stdio transport** — Communicates via standard input/output

## Build & Run

```bash
# Build
dotnet build

# Run directly
dotnet run

# Publish standalone exe
dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true -o ./publish
```

The published exe is at `publish/WindowsMCP.exe`.

## MCP Configuration

Add this to your MCP client configuration (e.g., Claude Desktop, Antigravity, etc.):

```json
{
  "mcpServers": {
    "windows-mcp": {
      "command": "C:\\Users\\punee\\Documents\\WindowsMCP\\WindowsMCP\\publish\\WindowsMCP.exe",
      "args": []
    }
  }
}
```

Or to run from source:

```json
{
  "mcpServers": {
    "windows-mcp": {
      "command": "dotnet",
      "args": ["run", "--project", "C:\\Users\\punee\\Documents\\WindowsMCP\\WindowsMCP"]
    }
  }
}
```

## Usage Examples

### 1. Discover what's on screen
```
→ list_windows
← [{ "title": "Untitled - Notepad", "processName": "notepad", "handle": 12345 }, ...]

→ list_elements(windowTitle: "Notepad")
← [{ "name": "Text Editor", "automationId": "15", "controlType": "Edit", ... }, ...]
```

### 2. Click a button
```
→ click_element(windowTitle: "Calculator", elementName: "Seven")
← { "success": true, "message": "Invoked element 'Seven' via InvokePattern." }
```

### 3. Type into a text field
```
→ type_text(text: "Hello World!", windowTitle: "Notepad", controlType: "Edit")
← { "success": true, "message": "Set value on 'Text Editor' via ValuePattern." }
```

### 4. Send a keyboard shortcut
```
→ send_keys(shortcut: "Ctrl+S")
← { "success": true, "message": "Sent shortcut: Ctrl+S" }
```

### 5. Click at exact coordinates
```
→ click_screen(x: 500, y: 300)
← { "success": true, "message": "Clicked at (500,300) with left button." }
```

## Requirements

- Windows 10/11
- .NET 10 Runtime (or use self-contained publish)
