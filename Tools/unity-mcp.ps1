# Forward arguments to CoplayDev's CLI, targeting this project explicitly.
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$cliPath = Join-Path $projectRoot '.utmp/unity-mcp-venv/Scripts/unity-mcp.exe'
if (!(Test-Path -LiteralPath $cliPath)) { throw 'MCP environment missing. See Docs/UnityMcp.md.' }
$dataPath = (Join-Path $projectRoot 'Assets').Replace('\', '/')
$sha = [System.Security.Cryptography.SHA1]::Create()
try {
    $instanceHash = ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($dataPath)))).Replace('-', '').ToLowerInvariant().Substring(0, 16)
} finally { $sha.Dispose() }
$env:PYTHONUTF8 = '1'
& $cliPath --host 127.0.0.1 --port 8080 --instance $instanceHash @args
exit $LASTEXITCODE
