$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location $projectRoot
try {
    New-Item -ItemType Directory -Force -Path 'artifacts\tests' | Out-Null
    dotnet build WinFormsApp1.slnx --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    dotnet run --project Chat.Tests --no-build 2>&1 | Tee-Object -FilePath 'artifacts\tests\integration.txt'
    if ($LASTEXITCODE -ne 0) { throw 'Integration tests failed.' }
    $uiProcess = Start-Process -FilePath '.\WinFormsApp1\bin\Debug\net10.0-windows\WinFormsApp1.exe' -ArgumentList '--ui-smoke' -WindowStyle Hidden -PassThru
    if (-not $uiProcess.WaitForExit(60000)) { throw 'UI test timeout; inspect the process and artifacts/ui.' }
    if ($uiProcess.ExitCode -ne 0) { throw 'UI smoke failed. See artifacts/ui/result.txt.' }
    Get-Content -LiteralPath 'artifacts\ui\result.txt'
}
finally { Pop-Location }
