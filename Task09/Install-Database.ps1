param([string]$Server = '.\SQLEXPRESS')
$ErrorActionPreference = 'Stop'
& sqlcmd -S $Server -E -C -b -i (Join-Path $PSScriptRoot 'Database\01_CreateAndImport.sql')
if ($LASTEXITCODE -ne 0) { throw 'Не удалось создать базу. Проверьте вывод sqlcmd.' }
Write-Host 'База создана и заполнена. Запустите dotnet run --project SchoolApp.'
