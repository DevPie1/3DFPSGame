# Start the installed Codex CLI in this Unity project with its existing MCP config.
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
& (Join-Path $PSScriptRoot 'Start-UnityMcp.ps1')
$codexCommand = Get-Command codex.exe -ErrorAction SilentlyContinue
if ($codexCommand) {
    $codexPath = $codexCommand.Source
} else {
    $codexBin = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'OpenAI/Codex/bin'
    $codexPath = Get-ChildItem -Path (Join-Path $codexBin '*/codex.exe') -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1 -ExpandProperty FullName
}
if (!$codexPath) { throw 'Codex CLI was not found. Open/update the Codex desktop app first.' }
& $codexPath -C $projectRoot @args
exit $LASTEXITCODE
