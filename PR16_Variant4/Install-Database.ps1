param([string]$Server = '.\SQLEXPRESS')
$ErrorActionPreference = 'Stop'
if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw 'sqlcmd не установлен. Откройте Database\01_CreateAndSeed.sql в SQL Server Management Studio.'
}
& sqlcmd -S $Server -E -C -b -f 65001 -i (Join-Path $PSScriptRoot 'Database\01_CreateAndSeed.sql')
if ($LASTEXITCODE -ne 0) { throw 'Установка не выполнена. Проверьте сообщение SQL Server выше.' }
Write-Host 'База PR16_AlisaClothes создана. Запустите dotnet run --project WPF_PR16_clothes_ZaytsevVI.'
