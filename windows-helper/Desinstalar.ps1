$dest = Join-Path $env:LOCALAPPDATA 'TK5PowerPoint'
$exe = Join-Path $dest 'TK5PowerPoint.exe'
Get-Process TK5PowerPoint -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $exe } | Stop-Process
$shortcut = Join-Path ([Environment]::GetFolderPath('Startup')) 'TK5 PowerPoint.lnk'
if (Test-Path -LiteralPath $shortcut) { Remove-Item -LiteralPath $shortcut }
if (Test-Path -LiteralPath $exe) { Remove-Item -LiteralPath $exe }
$token = Join-Path $dest 'token.txt'
if (Test-Path -LiteralPath $token) { Remove-Item -LiteralPath $token }
Write-Host 'Helper detenido y arranque desactivado. Presentaciones conservadas.'
