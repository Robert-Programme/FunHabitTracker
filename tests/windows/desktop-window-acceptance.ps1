param(
    [Parameter(Mandatory)][string]$ExePath,
    [string]$EvidenceRoot = (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) "artifacts\evidence")
)

$ErrorActionPreference = "Stop"
$ExePath = (Resolve-Path -LiteralPath $ExePath).Path
$dataRoot = Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) (".tmp\window-acceptance-" + [guid]::NewGuid().ToString("N"))
$endpoint = Join-Path $dataRoot "desktop-endpoint.txt"
$owner = $null

Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
using System.Text;
public static class WindowAcceptanceNative {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int command);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool IsZoomed(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll", EntryPoint="GetWindowLongPtrW")] public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int index);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
}
"@

function Wait-Until([scriptblock]$Condition, [string]$Failure, [int]$Seconds = 20) {
    $deadline = (Get-Date).AddSeconds($Seconds)
    while ((Get-Date) -lt $deadline) {
        if (& $Condition) { return }
        Start-Sleep -Milliseconds 100
    }
    throw $Failure
}

function Get-MainHandle([Diagnostics.Process]$Process) {
    $Process.Refresh()
    return $Process.MainWindowHandle
}

function Get-WindowTitle([IntPtr]$Handle) {
    $value = [Text.StringBuilder]::new(256)
    $null = [WindowAcceptanceNative]::GetWindowText($Handle, $value, $value.Capacity)
    return $value.ToString()
}

try {
    $owner = Start-Process -FilePath $ExePath -ArgumentList @("--FunHabitTracker:DataDirectory=$dataRoot") -PassThru
    Wait-Until { (Test-Path -LiteralPath $endpoint) -and (Get-MainHandle $owner) -ne [IntPtr]::Zero } "Compact window did not become ready"
    $url = (Get-Content -Raw -LiteralPath $endpoint).Trim()
    Wait-Until { try { (Invoke-WebRequest -UseBasicParsing ($url + "/Home/Compact") -TimeoutSec 2).StatusCode -eq 200 } catch { $false } } "Compact route did not become ready"
    Start-Sleep -Seconds 2
    $compact = Get-MainHandle $owner
    if ((Get-WindowTitle $compact) -ne "Heute | Fun Habit Tracker") { throw "Unexpected compact title" }
    $compactStyle = [WindowAcceptanceNative]::GetWindowLongPtr($compact, -16).ToInt64()
    if (($compactStyle -band 0x00010000) -ne 0) { throw "Compact window still exposes Maximize" }
    if (($compactStyle -band 0x00020000) -eq 0 -or ($compactStyle -band 0x00040000) -eq 0) { throw "Compact window lost Minimize or Resize" }
    $icon = [Drawing.Icon]::ExtractAssociatedIcon($ExePath)
    if ($null -eq $icon) { throw "Desktop executable has no associated icon" }
    $icon.Dispose()

    New-Item -ItemType Directory -Path $EvidenceRoot -Force | Out-Null
    $rect = [WindowAcceptanceNative+RECT]::new()
    if (-not [WindowAcceptanceNative]::GetWindowRect($compact, [ref]$rect)) { throw "Could not read compact window bounds" }
    $bitmap = [Drawing.Bitmap]::new($rect.Right - $rect.Left, $rect.Bottom - $rect.Top)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    $graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bitmap.Size)
    $graphics.Dispose()
    $bitmap.Save((Join-Path $EvidenceRoot "desktop-window-chrome.png"), [Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()

    $null = [WindowAcceptanceNative]::ShowWindow($compact, 6)
    Wait-Until { [WindowAcceptanceNative]::IsIconic($compact) } "Compact window did not minimize"
    $activation = Start-Process -FilePath $ExePath -PassThru
    if (-not $activation.WaitForExit(5000)) { throw "Duplicate activation did not exit" }
    Wait-Until { -not [WindowAcceptanceNative]::IsIconic($compact) -and [WindowAcceptanceNative]::IsWindowVisible($compact) } "Repeated launch did not restore compact window"

    $null = [WindowAcceptanceNative]::SendMessage($compact, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero)
    Wait-Until { -not [WindowAcceptanceNative]::IsWindowVisible($compact) -and -not $owner.HasExited } "Close did not hide compact window"
    if (-not (Test-Path -LiteralPath $endpoint)) { throw "Close stopped the local host" }

    $restore = Start-Process -FilePath $ExePath -PassThru
    if (-not $restore.WaitForExit(5000)) { throw "Tray-equivalent activation did not exit" }
    Wait-Until { [WindowAcceptanceNative]::IsWindowVisible($compact) } "Activation did not restore hidden compact window"
    $null = [WindowAcceptanceNative]::SendMessage($compact, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero)
    Wait-Until { -not [WindowAcceptanceNative]::IsWindowVisible($compact) } "Compact window did not hide before full-window test"

    $fullActivation = Start-Process -FilePath $ExePath -ArgumentList "--full" -PassThru
    if (-not $fullActivation.WaitForExit(5000)) { throw "Full activation did not exit" }
    Wait-Until { (Get-MainHandle $owner) -ne [IntPtr]::Zero -and (Get-MainHandle $owner) -ne $compact } "Full window did not become ready"
    $full = Get-MainHandle $owner
    if ((Get-WindowTitle $full) -ne "Fun Habit Tracker") { throw "Unexpected full title" }
    if ([WindowAcceptanceNative]::IsWindowVisible($compact)) { throw "Compact and full windows are visible at the same time" }
    $fullStyle = [WindowAcceptanceNative]::GetWindowLongPtr($full, -16).ToInt64()
    if (($fullStyle -band 0x00010000) -eq 0) { throw "Full window lost Maximize" }

    $compactActivation = Start-Process -FilePath $ExePath -PassThru
    if (-not $compactActivation.WaitForExit(5000)) { throw "Compact surface activation did not exit" }
    Wait-Until { [WindowAcceptanceNative]::IsWindowVisible($compact) -and -not [WindowAcceptanceNative]::IsWindowVisible($full) } "Full-to-compact switching did not preserve exclusive visibility"

    $fullActivation = Start-Process -FilePath $ExePath -ArgumentList "--full" -PassThru
    if (-not $fullActivation.WaitForExit(5000)) { throw "Repeated full surface activation did not exit" }
    Wait-Until { [WindowAcceptanceNative]::IsWindowVisible($full) -and -not [WindowAcceptanceNative]::IsWindowVisible($compact) } "Compact-to-full switching did not preserve exclusive visibility"
    $null = [WindowAcceptanceNative]::ShowWindow($full, 3)
    Wait-Until { [WindowAcceptanceNative]::IsZoomed($full) } "Full window did not maximize"

    $quit = Start-Process -FilePath $ExePath -ArgumentList "--quit" -PassThru -WindowStyle Hidden
    $null = $quit.WaitForExit(5000)
    if (-not $owner.WaitForExit(8000)) { throw "Explicit Quit did not stop the owner" }
    $owner = $null
    if (Test-Path -LiteralPath $endpoint) { throw "Endpoint remained after explicit Quit" }

    $report = @'
# Desktop window acceptance

- Compact title and executable icon verified.
- Compact native Maximize command absent; Minimize and Resize retained.
- Minimized compact window remained owned and repeated launch restored it.
- Close hid compact to tray ownership without stopping the loopback endpoint.
- Repeated activation restored the hidden compact window without a second owner.
- Compact-to-full and full-to-compact transitions left exactly one application window visible.
- Full window retained and successfully executed native Maximize.
- Explicit Quit stopped the owner and removed the endpoint file.
- Screenshot: `artifacts/evidence/desktop-window-chrome.png`.
'@
    Set-Content -LiteralPath (Join-Path $EvidenceRoot "desktop-window-acceptance.md") -Value $report -Encoding utf8
    Write-Host "Desktop window acceptance passed"
}
finally {
    if ($owner -and -not $owner.HasExited) {
        $owner.Kill()
        $owner.WaitForExit()
    }
}
