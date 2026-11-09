param([string]$Server = '.\SQLEXPRESS')

$ErrorActionPreference = 'Stop'
if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw 'sqlcmd не установлен. Откройте Database\01_CreateAndSeed.sql в SQL Server Management Studio.'
}
& sqlcmd -S $Server -E -C -b -f 65001 -i (Join-Path $PSScriptRoot 'Database\01_CreateAndSeed.sql')
if ($LASTEXITCODE -ne 0) { throw 'Установка базы не выполнена. Проверьте сообщение SQL Server.' }
Write-Host 'База PR19_Fitness готова.'
