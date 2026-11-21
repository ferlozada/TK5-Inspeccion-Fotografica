$ErrorActionPreference = 'Stop'
$dest = Join-Path $env:LOCALAPPDATA 'TK5PowerPoint'
New-Item -ItemType Directory -Path $dest -Force | Out-Null
# Private per-user token and executable; never stored inside the repository.
$acl = Get-Acl $dest
$acl.SetAccessRuleProtection($true, $false)
$sid = [Security.Principal.WindowsIdentity]::GetCurrent().User
$acl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule($sid,'FullControl','ContainerInherit,ObjectInherit','None','Allow')))
$systemSid = New-Object Security.Principal.SecurityIdentifier('S-1-5-18')
$acl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule($systemSid,'FullControl','ContainerInherit,ObjectInherit','None','Allow')))
Set-Acl -LiteralPath $dest -AclObject $acl
$exe = Join-Path $dest 'TK5PowerPoint.exe'
$running = Get-Process TK5PowerPoint -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $exe }
$running | Stop-Process
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
& $compiler /nologo /target:winexe "/out:$exe" /r:System.IO.Compression.dll /r:Microsoft.CSharp.dll (Join-Path $PSScriptRoot 'Helper.cs')
if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar el helper. Se necesita .NET Framework 4.8.' }
$tokenPath = Join-Path $dest 'token.txt'
if (!(Test-Path $tokenPath)) {
 $bytes = New-Object byte[] 32
 $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
 $rng.GetBytes($bytes); $rng.Dispose()
 [IO.File]::WriteAllText($tokenPath, ([BitConverter]::ToString($bytes).Replace('-','').ToLowerInvariant()))
}
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath('Startup')) 'TK5 PowerPoint.lnk'))
$shortcut.TargetPath = $exe
$shortcut.WorkingDirectory = $dest
$shortcut.WindowStyle = 7
$shortcut.Save()
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Desinstalar.ps1') -Destination $dest -Force
Start-Process -FilePath $exe -WindowStyle Hidden
$token = [IO.File]::ReadAllText($tokenPath).Trim()
Start-Process ('https://ferlozada.github.io/TK5-Inspeccion-Fotografica/#tk5-helper=' + $token)
Write-Host 'Instalado. La web queda vinculada en este navegador. Permite el acceso local si el navegador lo solicita al exportar.'
Write-Host 'Las presentaciones se guardan en:' (Join-Path $dest 'Presentaciones')
