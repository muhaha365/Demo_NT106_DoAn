$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$clientOutput = Join-Path $projectRoot 'artifacts\publish\UITChat'
$serverOutput = Join-Path $projectRoot 'artifacts\publish\Server'
Push-Location $projectRoot
try {
    dotnet build WinFormsApp1.slnx -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    dotnet publish WinFormsApp1\WinFormsApp1.csproj -c Release -r win-x64 --self-contained false -o $clientOutput --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Client publish failed.' }
    dotnet publish Chat.Host\Chat.Host.csproj -c Release -r win-x64 --self-contained false -o $serverOutput --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Server publish failed.' }
    Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination $clientOutput
    Write-Host "Client: $clientOutput\WinFormsApp1.exe"
    Write-Host "Server: $serverOutput\Chat.Host.exe"
    Write-Host 'Keep all files in each folder. Target PC needs .NET 10 Desktop Runtime x64.'
}
finally { Pop-Location }
