param(
    [switch]$RemoveData,
    [string]$InstallRoot = (Join-Path $env:LOCALAPPDATA "Programs\FunHabitTracker"),
    [string]$StartMenuRoot = (Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs"),
    [string]$DataRoot = (Join-Path $env:LOCALAPPDATA "FunHabitTracker")
)

$ErrorActionPreference = "Stop"
$desktopExe = Join-Path $InstallRoot "FunHabitTracker.Desktop.exe"
if (Test-Path -LiteralPath $desktopExe) {
    $owners = @(Get-Process FunHabitTracker.Desktop -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $desktopExe })
    $stop = Start-Process -FilePath $desktopExe -ArgumentList "--quit" -PassThru -WindowStyle Hidden
    $null = $stop.WaitForExit(5000)
    foreach ($owner in $owners) {
        if (-not $owner.WaitForExit(8000)) { throw "The running desktop application did not stop for removal" }
    }
}

Remove-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "FunHabitTracker" -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path $StartMenuRoot "Fun Habit Tracker.lnk") -Force -ErrorAction SilentlyContinue
if ($RemoveData) { Remove-Item -LiteralPath $DataRoot -Recurse -Force -ErrorAction SilentlyContinue }

if ((Get-Location).Path -ne $InstallRoot) {
    Remove-Item -LiteralPath $InstallRoot -Recurse -Force -ErrorAction SilentlyContinue
} else {
    Write-Host "Close this shell and remove $InstallRoot. User data was preserved."
}
