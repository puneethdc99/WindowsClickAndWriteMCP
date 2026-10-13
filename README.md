# 🖱️ WindowsClickAndWriteMCP — Windows Click & Write MCP Server

[![Build & Publish](https://github.com/puneethdc99/WindowsClickAndWriteMCP/actions/workflows/build.yml/badge.svg)](https://github.com/puneethdc99/WindowsClickAndWriteMCP/actions/workflows/build.yml)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![MCP](https://img.shields.io/badge/Protocol-MCP-green)](https://modelcontextprotocol.io/)
[![License](https://img.shields.io/badge/License-MIT-yellow)](LICENSE)

**WindowsClickAndWriteMCP** is a local MCP (Model Context Protocol) server built with .NET 10 that gives any AI agent or LLM the ability to **click on anything** and **type into any input field** on any Windows application.

It works with **any app** — Notepad, Chrome, VS Code, File Explorer, system dialogs, taskbar, custom-drawn UIs, games — anything that runs on your Windows desktop.

---

## 🎯 What Is This?

MCP (Model Context Protocol) is an open standard that lets AI assistants call **tools** on your computer. WindowsClickAndWriteMCP is an MCP server that exposes Windows desktop automation as tools — so any LLM or AI agent that speaks MCP can:

- **Click** any button, menu item, checkbox, link, tab, or any spot on the screen
- **Type** text into any text field, search box, address bar, or editor
- **Send keyboard shortcuts** like Ctrl+C, Alt+F4, Ctrl+Shift+N, Win+D
- **Discover** what windows are open and what UI elements are available to interact with

### Why Use This?

| Without WindowsClickAndWriteMCP | With WindowsClickAndWriteMCP |
|---|---|
| AI can only read/write files and run terminal commands | AI can interact with **any GUI application** on your desktop |
| Can't click buttons in apps | Clicks buttons, menus, links, checkboxes — anything |
| Can't fill in forms or dialogs | Types into any text field in any app |
| Blind to what's on screen | Lists all windows and UI elements it can interact with |

---

## 🛠️ MCP Tools Provided

WindowsClickAndWriteMCP exposes **7 tools** via the MCP protocol:

### 🖱️ Click Tools

| Tool | Description |
|------|-------------|
| **`click_element`** | Click a UI element by its name, automation ID, or control type. Finds the element inside a window and clicks it using UI Automation patterns (Invoke, Toggle, SelectionItem) or coordinate-based fallback. |
| **`click_screen`** | Click at absolute screen coordinates `(x, y)`. Works on **any** part of the screen — custom-drawn UIs, games, desktop icons, taskbar, system tray, anything. |

### ⌨️ Type Tools

| Tool | Description |
|------|-------------|
| **`type_text`** | Type text into a specific UI element (text field, search box, address bar, etc.). Finds the field by name/ID inside a window and types into it. |
| **`type_at_cursor`** | Type text at whatever is currently focused. Use this after you've already clicked a text field. |
| **`send_keys`** | Send keyboard shortcuts like `Ctrl+C`, `Alt+F4`, `Ctrl+Shift+S`, `Win+D`, `Enter`, `Tab`, `F5`, etc. |

### 🔍 Discovery Tools

| Tool | Description |
|------|-------------|
| **`list_windows`** | List all visible windows on the desktop — returns window title, process name, and window handle for each. |
| **`list_elements`** | List all interactive UI elements inside a window — returns element name, automation ID, control type, bounding rectangle, and current value. Essential for discovering what to click or type into. |

---

## 📦 Installation

### Prerequisites

- **Windows 10 or 11**
- **One** of the following:
  - [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) — for the framework-dependent exe (~4 MB)
  - No runtime needed — for the self-contained exe (~70 MB, includes .NET runtime)

### Option 1: Download the Pre-Built Exe (Easiest)

1. Go to the [**Releases**](https://github.com/puneethdc99/WindowsClickAndWriteMCP/releases) page
2. Download **`WindowsClickAndWriteMCP.exe`** (framework-dependent) or **`WindowsClickAndWriteMCP-self-contained.exe`** (no runtime needed)
3. Save it somewhere on your machine (e.g., `C:\Tools\WindowsClickAndWriteMCP.exe`)

### Option 2: Build From Source

```bash
# Clone the repository
git clone https://github.com/puneethdc99/WindowsClickAndWriteMCP.git
cd WindowsClickAndWriteMCP

# Build
dotnet build WindowsClickAndWriteMCP

# Or publish as a standalone single-file exe
dotnet publish WindowsClickAndWriteMCP -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true -o ./publish
```

The built exe will be at `./publish/WindowsClickAndWriteMCP.exe`.

---

## 🔌 Setup Guide — Connect to Your AI Agent / LLM

WindowsClickAndWriteMCP uses **stdio transport** — it reads MCP JSON-RPC messages from stdin and writes responses to stdout. This is the standard way local MCP servers work.

Below are setup instructions for every major AI platform that supports MCP:

---

### Claude Desktop

Edit your Claude Desktop config file:

- **Windows:** `%APPDATA%\Claude\claude_desktop_config.json`

Add WindowsClickAndWriteMCP to the `mcpServers` section:

```json
{
  "mcpServers": {
    "windows-click-and-write": {
      "command": "C:\\Tools\\WindowsClickAndWriteMCP.exe",
      "args": []
    }
  }
}
```

Or if you want to run from source instead of the exe:

```json
{
  "mcpServers": {
    "windows-click-and-write": {
      "command": "dotnet",
      "args": ["run", "--project", "C:\\path\\to\\WindowsClickAndWriteMCP\\WindowsClickAndWriteMCP"]
    }
  }
}
```

Restart Claude Desktop. You'll see the WindowsClickAndWriteMCP tools appear in the tool list (🔨 icon).

---

### Google Antigravity (AGY)

Create or edit your MCP config at the workspace or global level:

**Workspace-level** (`.gemini/settings.json` in your project root):
```json
{
  "mcpServers": {
    "windows-click-and-write": {
      "command": "C:\\Tools\\WindowsClickAndWriteMCP.exe",
      "args": []
    }
  }
}
```

**Global-level** (`~/.gemini/settings.json`):
```json
{
  "mcpServers": {
    "windows-click-and-write": {
      "command": "C:\\Tools\\WindowsClickAndWriteMCP.exe",
      "args": []
    }
  }
}
```

The tools will be available in your next Antigravity session.

---

### Cursor

Edit Cursor's MCP configuration:

1. Open Cursor Settings → **MCP**
2. Click **"Add new MCP server"**
3. Fill in:
   - **Name:** `windows-click-and-write`
   - **Type:** `stdio`
   - **Command:** `C:\Tools\WindowsClickAndWriteMCP.exe`

Or manually edit `~/.cursor/mcp.json`:

```json
{
  "mcpServers": {
    "windows-click-and-write": {
      "command": "C:\\Tools\\WindowsClickAndWriteMCP.exe",
      "args": []
    }
  }
}
```

---

### VS Code (GitHub Copilot with MCP)

Add to your VS Code `settings.json` or `.vscode/mcp.json`:

```json
{
  "mcp": {
    "servers": {
      "windows-click-and-write": {
        "command": "C:\\Tools\\WindowsClickAndWriteMCP.exe",
        "args": []
      }
    }
  }
}
```

---

### Windsurf

Edit `~/.windsurf/mcp.json`:

```json
{
  "mcpServers": {
    "windows-click-and-write": {
      "command": "C:\\Tools\\WindowsClickAndWriteMCP.exe",
      "args": []
    }
  }
}
```

---

### Cline (VS Code Extension)

Edit Cline's MCP settings file (found via Cline Settings → MCP Servers):

```json
{
  "mcpServers": {
    "windows-click-and-write": {
      "command": "C:\\Tools\\WindowsClickAndWriteMCP.exe",
      "args": []
    }
  }
}
```

---

### OpenAI Agents SDK / Custom Python Agents

If you're building your own agent with the OpenAI Agents SDK or any Python MCP client:

```python
from agents import Agent
from agents.mcp import MCPServerStdio

async with MCPServerStdio(
    command="C:/Tools/WindowsClickAndWriteMCP.exe",
    args=[]
) as windows_mcp:
    agent = Agent(
        name="Desktop Assistant",
        instructions="You can interact with Windows desktop apps using the MCP tools.",
        mcp_servers=[windows_mcp]
    )
```

---

### Any MCP-Compatible Client

WindowsClickAndWriteMCP works with **any** MCP client. The connection details are:

| Property | Value |
|----------|-------|
| **Transport** | `stdio` |
| **Command** | Path to `WindowsClickAndWriteMCP.exe` |
| **Arguments** | None |
| **Protocol** | MCP (Model Context Protocol) JSON-RPC over stdin/stdout |

---

## 📖 Usage Examples

Once connected, the AI agent can use the tools like this:

### 1. See What's on the Desktop

```
Agent calls: list_windows()
Response:
[
  { "title": "Untitled - Notepad", "processName": "notepad", "handle": 12345 },
  { "title": "Google Chrome", "processName": "chrome", "handle": 67890 },
  ...
]
```

### 2. Explore UI Elements in an App

```
Agent calls: list_elements(windowTitle: "Calculator")
Response:
[
  { "name": "Seven", "automationId": "num7Button", "controlType": "Button", ... },
  { "name": "Display is 0", "automationId": "CalculatorResults", "controlType": "Text", ... },
  ...
]
```

### 3. Click a Button

```
Agent calls: click_element(windowTitle: "Calculator", elementName: "Seven")
Response: { "success": true, "message": "Invoked element 'Seven' via InvokePattern." }
```

### 4. Type Text into Notepad

```
Agent calls: type_text(
    text: "Hello from AI!",
    windowTitle: "Notepad",
    controlType: "Edit"
)
Response: { "success": true, "message": "Set value on 'Text Editor' via ValuePattern." }
```

### 5. Save a File (Keyboard Shortcut)

```
Agent calls: send_keys(shortcut: "Ctrl+S")
Response: { "success": true, "message": "Sent shortcut: Ctrl+S" }
```

### 6. Click at Exact Screen Coordinates

```
Agent calls: click_screen(x: 500, y: 300, button: "right")
Response: { "success": true, "message": "Clicked at (500,300) with right button." }
```

### 7. Type into a Search Box and Hit Enter

```
Agent calls: type_text(
    text: "weather today",
    windowTitle: "Chrome",
    elementName: "Address and search bar",
    clearFirst: true,
    pressEnterAfter: true
)
Response: { "success": true, "message": "Typed into 'Address and search bar' via keyboard input." }
```

---

## 🏗️ Architecture

```
WindowsClickAndWriteMCP/
├── Program.cs                           ← MCP server entry point (stdio transport)
├── Tools/
│   ├── ClickTools.cs                    ← click_element, click_screen
│   ├── TypeTools.cs                     ← type_text, type_at_cursor, send_keys
│   ├── DiscoveryTools.cs                ← list_windows, list_elements
│   └── JsonOpts.cs                      ← Shared JSON serialization config
├── Services/
│   └── WindowsAutomationService.cs      ← Core automation engine (FlaUI + Win32)
├── Native/
│   └── NativeMethods.cs                 ← Win32 P/Invoke declarations
└── WindowsClickAndWriteMCP.csproj       ← Project file (.NET 10)
```

### How It Works Under the Hood

```
AI Agent ──MCP JSON-RPC──▶ stdin ──▶ WindowsClickAndWriteMCP.exe ──▶ MCP Tool Handler
                                                                        │
                                             ┌──────────────────────────┤
                                             ▼                          ▼
                                      FlaUI (UIA3)              Win32 P/Invoke
                                   UI Automation API           SendInput / mouse_event
                                             │                          │
                                             ▼                          ▼
                                    Find elements by            Raw mouse clicks &
                                   Name / AutomationId /        keyboard input at
                                   ControlType, then use        any screen coordinates
                                   Invoke/Toggle/Value
                                   patterns to interact
```

1. **Element-based interactions** use **[FlaUI](https://github.com/FlaUI/FlaUI)** with the **UIA3** (UI Automation v3) backend to find and interact with standard Windows UI elements. This works with Win32 apps, WPF, WinForms, UWP, and most desktop applications.

2. **Coordinate-based interactions** use **Win32 P/Invoke** (`SendInput`, `mouse_event`, `SetCursorPos`) for raw mouse and keyboard input. This works even with custom-drawn UIs, games, and anything else on screen.

3. The MCP server uses **stdio transport** — it reads JSON-RPC from stdin and writes responses to stdout, following the [Model Context Protocol](https://modelcontextprotocol.io/) specification.

---

## 🔧 Tool Parameter Reference

### `click_element`

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `windowTitle` | string | No | Window title (or partial match). e.g., `"Notepad"`, `"Chrome"` |
| `processName` | string | No | Process name without .exe. e.g., `"notepad"`, `"chrome"` |
| `elementName` | string | No | Display name of the element. e.g., `"Save"`, `"OK"`, `"File"` |
| `automationId` | string | No | Automation ID (most precise). Use `list_elements` to find these. |
| `controlType` | string | No | Filter by type: `Button`, `Edit`, `CheckBox`, `MenuItem`, `TabItem`, etc. |
| `button` | string | No | Mouse button: `"left"` (default), `"right"`, `"middle"` |
| `doubleClick` | bool | No | Double-click instead of single-click. Default: `false` |

### `click_screen`

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `x` | int | **Yes** | X coordinate (pixels from left edge) |
| `y` | int | **Yes** | Y coordinate (pixels from top edge) |
| `button` | string | No | `"left"` (default), `"right"`, `"middle"` |
| `doubleClick` | bool | No | Default: `false` |

### `type_text`

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `text` | string | **Yes** | The text to type |
| `windowTitle` | string | No | Target window title (partial match) |
| `processName` | string | No | Target process name |
| `elementName` | string | No | Name of the input element |
| `automationId` | string | No | Automation ID of the input element |
| `controlType` | string | No | Typically `"Edit"` for text fields |
| `clearFirst` | bool | No | Clear existing text before typing. Default: `true` |
| `pressEnterAfter` | bool | No | Press Enter after typing. Default: `false` |

### `type_at_cursor`

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `text` | string | **Yes** | The text to type at current focus |
| `clearFirst` | bool | No | Select all + delete first. Default: `false` |
| `pressEnterAfter` | bool | No | Press Enter after. Default: `false` |

### `send_keys`

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `shortcut` | string | **Yes** | Key combination, `+` separated. e.g., `"Ctrl+C"`, `"Alt+F4"`, `"Ctrl+Shift+S"` |

**Supported key names:** `Ctrl`, `Alt`, `Shift`, `Win`, `Enter`, `Tab`, `Escape`, `Space`, `Backspace`, `Delete`, `Home`, `End`, `PageUp`, `PageDown`, `Up`, `Down`, `Left`, `Right`, `Insert`, `PrintScreen`, `F1`–`F12`, `A`–`Z`, `0`–`9`

### `list_windows`

No parameters. Returns all visible windows.

### `list_elements`

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `windowTitle` | string | No | Target window title |
| `processName` | string | No | Target process name |
| `controlType` | string | No | Filter: `Button`, `Edit`, `CheckBox`, `ComboBox`, `MenuItem`, etc. |
| `maxDepth` | int | No | UI tree search depth. Default: `5` |
| `maxResults` | int | No | Max elements to return. Default: `50` |

---

## 🔄 CI/CD Pipeline

Every push to `main` triggers a GitHub Actions workflow that:

1. **Builds** the project on `windows-latest`
2. **Publishes** a single-file exe
3. **Creates a GitHub Release** with:
   - Framework-dependent exe (~4 MB, needs .NET 10 runtime)
   - Self-contained exe (~70 MB, no runtime needed)

Build artifacts are also attached to every workflow run (including PRs).

---

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/my-feature`
3. Commit your changes: `git commit -m "Add my feature"`
4. Push to the branch: `git push origin feature/my-feature`
5. Open a Pull Request

---

## 📄 License

This project is open source under the MIT License.

---

## ⭐ Star This Repo

If you find WindowsClickAndWriteMCP useful, please give it a ⭐ on GitHub — it helps others discover the project!
