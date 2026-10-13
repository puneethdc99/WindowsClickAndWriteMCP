param(
    [Parameter(Mandatory=$true)]
    [string]$Tool,

    [Parameter(Mandatory=$false)]
    [string]$ArgsJson = "{}"
)

if (Test-Path $ArgsJson -PathType Leaf -ErrorAction SilentlyContinue) {
    $ArgsJson = Get-Content $ArgsJson -Raw
}

$argsParsed = $ArgsJson | ConvertFrom-Json

$exe = "C:\Users\punee\Documents\WindowsMCP\WindowsClickAndWriteMCP\publish\WindowsClickAndWriteMCP.exe"

$p = New-Object System.Diagnostics.Process
$p.StartInfo.FileName = $exe
$p.StartInfo.UseShellExecute = $false
$p.StartInfo.RedirectStandardInput = $true
$p.StartInfo.RedirectStandardOutput = $true
$p.StartInfo.RedirectStandardError = $true
$p.Start() | Out-Null

$writer = $p.StandardInput
$reader = $p.StandardOutput

# 1. Initialize
$writer.WriteLine('{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"runner","version":"1.0"}}}')
$writer.Flush()
$reader.ReadLine() | Out-Null

# 2. Initialized notification
$writer.WriteLine('{"jsonrpc":"2.0","method":"notifications/initialized"}')
$writer.Flush()

# 3. Call tool
$callObj = @{
    jsonrpc = "2.0"
    id = 2
    method = "tools/call"
    params = @{
        name = $Tool
        arguments = $argsParsed
    }
}
$callJson = $callObj | ConvertTo-Json -Compress -Depth 10
$writer.WriteLine($callJson)
$writer.Flush()

$res = $reader.ReadLine()
$p.Kill()

if ($res) {
    $parsed = $res | ConvertFrom-Json
    if ($parsed.result.content) {
        $parsed.result.content[0].text
    } else {
        $res
    }
}
