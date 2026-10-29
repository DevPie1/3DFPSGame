$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$serverPath = Join-Path $projectRoot '.utmp/unity-mcp-venv/Scripts/mcp-for-unity.exe'
if (!(Test-Path -LiteralPath $serverPath)) { throw 'MCP environment missing. See Docs/UnityMcp.md.' }
try {
    $health = Invoke-RestMethod 'http://127.0.0.1:8080/health' -TimeoutSec 2
    if ($health) { Write-Output 'MCP server is already responding on 127.0.0.1:8080.'; return }
} catch { }
$logDirectory = Join-Path $projectRoot 'Logs'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$env:PYTHONUTF8 = '1'
$server = Start-Process -FilePath $serverPath -ArgumentList @('--transport', 'http', '--http-url', 'http://127.0.0.1:8080') -WorkingDirectory $projectRoot -WindowStyle Hidden -RedirectStandardOutput (Join-Path $logDirectory 'unity-mcp-server.log') -RedirectStandardError (Join-Path $logDirectory 'unity-mcp-server-error.log') -PassThru
$server.Id | Set-Content -LiteralPath (Join-Path $projectRoot '.utmp/unity-mcp-server.pid')
Write-Output "MCP server starting (PID $($server.Id)). Run Tools/unity-mcp.ps1 status to check."
