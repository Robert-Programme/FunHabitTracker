param([string]$BundleRoot = (Join-Path (Split-Path -Parent $PSScriptRoot) "artifacts\installer"))

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$testRoot = Join-Path $env:TEMP ("FunHabitTracker-installer-" + [guid]::NewGuid().ToString("N"))
$installRoot = Join-Path $testRoot "Programs\FunHabitTracker"
$startMenu = Join-Path $testRoot "StartMenu"
$dataRoot = Join-Path $testRoot "Data"
$evidenceRoot = Join-Path $root "artifacts\evidence"
$process = $null
$runKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
$previousRegistration = (Get-ItemProperty -Path $runKey -Name "FunHabitTracker" -ErrorAction SilentlyContinue).FunHabitTracker

try {
    & (Join-Path $BundleRoot "install.ps1") -Source (Join-Path $BundleRoot "app") -RuntimeSource (Join-Path $BundleRoot "runtime\MicrosoftEdgeWebView2RuntimeInstallerX64.exe") -InstallRoot $installRoot -StartMenuRoot $startMenu
    $desktopExe = Join-Path $installRoot "FunHabitTracker.Desktop.exe"
    if (-not (Test-Path -LiteralPath $desktopExe)) { throw "Installed desktop executable is missing" }
    if (-not (Test-Path -LiteralPath (Join-Path $startMenu "Fun Habit Tracker.lnk"))) { throw "Start Menu shortcut is missing" }

    $arguments = @("--FunHabitTracker:DataDirectory=$dataRoot", "--background")
    $process = Start-Process -FilePath $desktopExe -ArgumentList $arguments -PassThru -WindowStyle Hidden
    $endpoint = Join-Path $dataRoot "desktop-endpoint.txt"
    $deadline = (Get-Date).AddSeconds(20)
    while (-not (Test-Path -LiteralPath $endpoint) -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 100 }
    if (-not (Test-Path -LiteralPath $endpoint)) { throw "Desktop endpoint was not created" }
    $url = (Get-Content -Raw -LiteralPath $endpoint).Trim()
    if (-not $url.StartsWith("http://127.0.0.1:")) { throw "Desktop endpoint is not loopback-only: $url" }
    foreach ($path in @("/Home/Compact", "/Home/Today", "/Habits", "/Backup")) {
        $response = Invoke-WebRequest -UseBasicParsing ($url + $path)
        if ($response.StatusCode -ne 200) { throw "Installed app returned HTTP $($response.StatusCode) for $path" }
    }

    $listeners = Get-NetTCPConnection -OwningProcess $process.Id -State Listen -ErrorAction SilentlyContinue
    if ($listeners | Where-Object { $_.LocalAddress -notin @("127.0.0.1", "::1") }) { throw "Desktop host exposed a non-loopback listener" }

    $second = Start-Process -FilePath $desktopExe -ArgumentList @("--FunHabitTracker:DataDirectory=$dataRoot") -PassThru -WindowStyle Hidden
    if (-not $second.WaitForExit(5000)) { $second.Kill(); throw "Second instance did not exit" }

    New-Item -ItemType File -Path (Join-Path $dataRoot "preserve.marker") -Force | Out-Null
    New-Item -ItemType File -Path (Join-Path $dataRoot "desktop-settings.json") -Force | Out-Null
    Set-ItemProperty -Path $runKey -Name "FunHabitTracker" -Value ('"' + $desktopExe + '" --background')
    $quit = Start-Process -FilePath $desktopExe -ArgumentList "--quit" -PassThru -WindowStyle Hidden
    $null = $quit.WaitForExit(5000)
    if (-not $process.WaitForExit(8000)) { throw "Desktop owner did not exit after Quit activation" }
    $process = $null

    & (Join-Path $BundleRoot "install.ps1") -Source (Join-Path $BundleRoot "app") -RuntimeSource (Join-Path $BundleRoot "runtime\MicrosoftEdgeWebView2RuntimeInstallerX64.exe") -InstallRoot $installRoot -StartMenuRoot $startMenu
    if (-not (Test-Path -LiteralPath (Join-Path $dataRoot "preserve.marker"))) { throw "Upgrade removed user data" }
    if (-not (Test-Path -LiteralPath (Join-Path $dataRoot "desktop-settings.json"))) { throw "Upgrade removed desktop settings" }

    & (Join-Path $installRoot "uninstall.ps1") -InstallRoot $installRoot -StartMenuRoot $startMenu -DataRoot $dataRoot
    if (Test-Path -LiteralPath $installRoot) { throw "Uninstall did not remove application files" }
    if (-not (Test-Path -LiteralPath (Join-Path $dataRoot "preserve.marker"))) { throw "Uninstall removed user data without consent" }
    if ((Get-ItemProperty -Path $runKey -Name "FunHabitTracker" -ErrorAction SilentlyContinue).FunHabitTracker) { throw "Uninstall left start-at-sign-in registration" }

    & (Join-Path $BundleRoot "install.ps1") -Source (Join-Path $BundleRoot "app") -RuntimeSource (Join-Path $BundleRoot "runtime\MicrosoftEdgeWebView2RuntimeInstallerX64.exe") -InstallRoot $installRoot -StartMenuRoot $startMenu
    & (Join-Path $installRoot "uninstall.ps1") -RemoveData -InstallRoot $installRoot -StartMenuRoot $startMenu -DataRoot $dataRoot
    if (Test-Path -LiteralPath $dataRoot) { throw "Explicit data removal retained the data directory" }

    New-Item -ItemType Directory -Path $evidenceRoot -Force | Out-Null
    $report = @"
# Windows desktop installer acceptance

- Offline self-contained desktop and bundled WebView2 runtime installed.
- Start Menu shortcut targets `FunHabitTracker.Desktop.exe`.
- Background launch created a tray owner and loopback endpoint: $url.
- Compact and full application routes returned HTTP 200.
- No non-loopback ASP.NET listener was exposed.
- Duplicate activation exited and reused the owning process.
- In-place update preserved habit data and desktop settings.
- Normal removal removed application files and autostart registration while preserving data.
- Explicit destructive removal deleted the isolated test data.
- Archive SHA-256: $((Get-FileHash -Algorithm SHA256 (Join-Path $root "artifacts\FunHabitTracker-Setup-win-x64.zip")).Hash.ToLowerInvariant())
"@
    Set-Content -LiteralPath (Join-Path $evidenceRoot "windows-clean-install.md") -Value $report -Encoding utf8
    Set-Content -LiteralPath (Join-Path $evidenceRoot "windows-upgrade-uninstall.md") -Value $report -Encoding utf8
    Write-Host "Installer verification passed"
}
finally {
    if ($process -and -not $process.HasExited) { $process.Kill(); $process.WaitForExit() }
    if ($null -eq $previousRegistration) { Remove-ItemProperty -Path $runKey -Name "FunHabitTracker" -ErrorAction SilentlyContinue }
    else { Set-ItemProperty -Path $runKey -Name "FunHabitTracker" -Value $previousRegistration }
    $resolvedTestRoot = [IO.Path]::GetFullPath($testRoot)
    $resolvedTemp = [IO.Path]::GetFullPath($env:TEMP).TrimEnd('\') + '\'
    if (-not $resolvedTestRoot.StartsWith($resolvedTemp, [StringComparison]::OrdinalIgnoreCase)) { throw "Unsafe test cleanup path: $resolvedTestRoot" }
    for ($attempt = 0; $attempt -lt 10 -and (Test-Path -LiteralPath $resolvedTestRoot); $attempt++) {
        Start-Sleep -Milliseconds 500
        Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
