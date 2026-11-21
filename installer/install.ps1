param(
    [string]$Source = (Join-Path $PSScriptRoot "app"),
    [string]$RuntimeSource = (Join-Path $PSScriptRoot "runtime\MicrosoftEdgeWebView2RuntimeInstallerX64.exe"),
    [string]$InstallRoot = (Join-Path $env:LOCALAPPDATA "Programs\FunHabitTracker"),
    [string]$StartMenuRoot = (Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs")
)

$ErrorActionPreference = "Stop"
$desktopExe = Join-Path $Source "FunHabitTracker.Desktop.exe"
if (-not (Test-Path -LiteralPath $desktopExe)) { throw "FunHabitTracker.Desktop.exe was not found under $Source" }

$installedExe = Join-Path $InstallRoot "FunHabitTracker.Desktop.exe"
if (Test-Path -LiteralPath $installedExe) {
    $owners = @(Get-Process FunHabitTracker.Desktop -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $installedExe })
    $stop = Start-Process -FilePath $installedExe -ArgumentList "--quit" -PassThru -WindowStyle Hidden
    $null = $stop.WaitForExit(5000)
    foreach ($owner in $owners) {
        if (-not $owner.WaitForExit(8000)) { throw "The running desktop application did not stop for update" }
    }
}

$runtimeRoots = @(
    (Join-Path ${env:ProgramFiles(x86)} "Microsoft\EdgeWebView\Application"),
    (Join-Path $env:LOCALAPPDATA "Microsoft\EdgeWebView\Application")
)
$hasRuntime = $runtimeRoots | Where-Object { Test-Path -LiteralPath $_ } | ForEach-Object {
    Get-ChildItem -LiteralPath $_ -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -match '^\d+\.' }
} | Select-Object -First 1
if (-not $hasRuntime) {
    if (-not (Test-Path -LiteralPath $RuntimeSource)) { throw "The offline WebView2 runtime installer is missing" }
    $runtimeSignature = Get-AuthenticodeSignature -LiteralPath $RuntimeSource
    if ($runtimeSignature.Status -ne "Valid") { throw "The WebView2 runtime signature is not valid" }
    $runtimeInstall = Start-Process -FilePath $RuntimeSource -ArgumentList "/silent /install" -PassThru -Wait -WindowStyle Hidden
    if ($runtimeInstall.ExitCode -notin @(0, 3010)) { throw "WebView2 runtime installation failed with exit code $($runtimeInstall.ExitCode)" }
}

New-Item -ItemType Directory -Path $InstallRoot -Force | Out-Null
New-Item -ItemType Directory -Path $StartMenuRoot -Force | Out-Null
Copy-Item -Path (Join-Path $Source "*") -Destination $InstallRoot -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "uninstall.ps1") -Destination $InstallRoot -Force

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut((Join-Path $StartMenuRoot "Fun Habit Tracker.lnk"))
$shortcut.TargetPath = Join-Path $InstallRoot "FunHabitTracker.Desktop.exe"
$shortcut.WorkingDirectory = $InstallRoot
$shortcut.Description = "Local-first habit tracking"
$shortcut.Save()

Write-Host "Installed Fun Habit Tracker Preview to $InstallRoot"
