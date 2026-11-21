param(
    [string]$Configuration = "Release",
    [ValidateSet("preview", "production")][string]$Channel = "preview",
    [string]$SignToolPath,
    [string]$CertificateThumbprint,
    [string]$TimestampUrl
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$version = "0.2.0-preview.1"
$staging = Join-Path $root "artifacts\installer"
$app = Join-Path $staging "app"
$runtimeStage = Join-Path $staging "runtime"
$runtimeSource = Join-Path $PSScriptRoot "runtime\MicrosoftEdgeWebView2RuntimeInstallerX64.exe"
$archive = Join-Path $root "artifacts\FunHabitTracker-Setup-win-x64.zip"
$website = Join-Path $root "artifacts\website\fun-habit-tracker\$version"

function Assert-UnderRoot([string]$Path) {
    $full = [IO.Path]::GetFullPath($Path)
    $rootPrefix = [IO.Path]::GetFullPath($root).TrimEnd('\') + '\'
    if (-not $full.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw "Unsafe output path: $full" }
}

Assert-UnderRoot $staging
Assert-UnderRoot $website
if (Test-Path -LiteralPath $staging) { Remove-Item -LiteralPath $staging -Recurse -Force }
if (Test-Path -LiteralPath $website) { Remove-Item -LiteralPath $website -Recurse -Force }
New-Item -ItemType Directory -Path $app -Force | Out-Null
New-Item -ItemType Directory -Path $runtimeStage -Force | Out-Null
New-Item -ItemType Directory -Path $website -Force | Out-Null

dotnet publish (Join-Path $root "src\FunHabitTracker.Desktop\FunHabitTracker.Desktop.csproj") `
    --configuration $Configuration --runtime win-x64 --self-contained true --output $app `
    /p:PublishReadyToRun=false /p:DebugType=none /p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

if (-not (Test-Path -LiteralPath $runtimeSource)) {
    Invoke-WebRequest -UseBasicParsing "https://go.microsoft.com/fwlink/p/?LinkId=2124701" -OutFile $runtimeSource
}
$runtimeSignature = Get-AuthenticodeSignature -LiteralPath $runtimeSource
if ($runtimeSignature.Status -ne "Valid" -or $runtimeSignature.SignerCertificate.Subject -notmatch "Microsoft Corporation") {
    throw "The offline WebView2 runtime does not have the expected valid Microsoft signature"
}
Copy-Item -LiteralPath $runtimeSource -Destination $runtimeStage

$desktopExe = Join-Path $app "FunHabitTracker.Desktop.exe"
if ($CertificateThumbprint) {
    if (-not $SignToolPath -or -not $TimestampUrl) { throw "Signing requires SignToolPath and TimestampUrl" }
    & $SignToolPath sign /sha1 $CertificateThumbprint /fd SHA256 /tr $TimestampUrl /td SHA256 $desktopExe
    if ($LASTEXITCODE -ne 0) { throw "Authenticode signing failed" }
}
$appSignature = Get-AuthenticodeSignature -LiteralPath $desktopExe
$isSigned = $appSignature.Status -eq "Valid"
if ($Channel -eq "production" -and -not $isSigned) { throw "Production packaging requires a verified Authenticode signature" }

Copy-Item -LiteralPath (Join-Path $PSScriptRoot "install.ps1") -Destination $staging
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "uninstall.ps1") -Destination $staging
Copy-Item -LiteralPath (Join-Path $root "docs\installation.md") -Destination (Join-Path $staging "README.md")
if (Test-Path -LiteralPath $archive) { Remove-Item -LiteralPath $archive -Force }
Compress-Archive -Path (Join-Path $staging "*") -DestinationPath $archive -CompressionLevel Optimal

$hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $archive).Hash.ToLowerInvariant()
Set-Content -LiteralPath "$archive.sha256" -Value "$hash  $(Split-Path -Leaf $archive)" -Encoding ascii
Copy-Item -LiteralPath $archive -Destination $website
Copy-Item -LiteralPath "$archive.sha256" -Destination $website
Copy-Item -LiteralPath (Join-Path $root "docs\installation.md") -Destination $website
Copy-Item -LiteralPath (Join-Path $root "docs\privacy.md") -Destination $website
Copy-Item -LiteralPath (Join-Path $root "docs\preview-trust.md") -Destination $website
Copy-Item -LiteralPath (Join-Path $root "docs\release-notes-0.2.0-preview.1.md") -Destination $website

$evidence = Join-Path $root "artifacts\evidence"
New-Item -ItemType Directory -Path $evidence -Force | Out-Null
$signatureReport = [ordered]@{ status = $(if ($isSigned) { "valid" } else { "unsigned-preview" }); signer = $(if ($isSigned) { $appSignature.SignerCertificate.Subject } else { $null }); runtimeStatus = $runtimeSignature.Status.ToString(); runtimeSigner = $runtimeSignature.SignerCertificate.Subject }
$signatureReport | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $evidence "release-signature-report.json") -Encoding utf8
$signatureReport | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $website "release-signature-report.json") -Encoding utf8

$publishedFiles = @(Get-ChildItem $app -Recurse -File | ForEach-Object { [ordered]@{ path=$_.FullName.Substring($app.Length).TrimStart('\').Replace('\','/'); sha256=(Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() } })
[ordered]@{ bomFormat="CycloneDX"; specVersion="1.5"; version=1; metadata=[ordered]@{ component=[ordered]@{ type="application"; name="Fun Habit Tracker Preview"; version=$version } }; components=@($publishedFiles | ForEach-Object { [ordered]@{ type="file"; name=$_.path; hashes=@([ordered]@{ alg="SHA-256"; content=$_.sha256 }) } }) } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $website "sbom.cdx.json") -Encoding utf8
[ordered]@{ generatedAtUtc=(Get-Date).ToUniversalTime().ToString("O"); product="Fun Habit Tracker Preview"; version=$version; channel=$Channel; subject=@([ordered]@{ name=(Split-Path -Leaf $archive); sha256=$hash; bytes=(Get-Item $archive).Length }); gates=@("openspec","build","tests","factory","desktop-browser","installer","signature") } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $website "release-provenance.json") -Encoding utf8
Copy-Item -LiteralPath (Join-Path $website "release-provenance.json") -Destination (Join-Path $evidence "release-provenance.json") -Force

$manifest = [ordered]@{
    product = "Fun Habit Tracker Preview"
    version = $version
    channel = $Channel
    architecture = "win-x64"
    minimumWindows = "10.0.17763.0"
    installer = [ordered]@{ file = (Split-Path -Leaf $archive); bytes = (Get-Item $archive).Length; sha256 = $hash }
    signature = [ordered]@{ status = $(if ($isSigned) { "valid" } else { "unsigned-preview" }); signer = $(if ($isSigned) { $appSignature.SignerCertificate.Subject } else { $null }) }
    runtime = [ordered]@{ name = "Microsoft Edge WebView2 Evergreen Standalone x64"; bundled = $true; sha256 = (Get-FileHash $runtimeSource -Algorithm SHA256).Hash.ToLowerInvariant(); signature = "valid-microsoft" }
    documents = @("installation.md", "privacy.md", "preview-trust.md", "release-notes-0.2.0-preview.1.md")
    evidence = @("sbom.cdx.json", "release-provenance.json", "release-signature-report.json")
}
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $website "release-manifest.json") -Encoding utf8

python (Join-Path $root "ci\release_bundle.py") (Join-Path $website "release-manifest.json") $website
if ($LASTEXITCODE -ne 0) { throw "Website release bundle validation failed" }
[ordered]@{ generatedAtUtc = (Get-Date).ToUniversalTime().ToString("O"); root = $website; files = @(Get-ChildItem $website -File | ForEach-Object { [ordered]@{ name=$_.Name; bytes=$_.Length; sha256=(Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() } }) } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $evidence "website-bundle-inventory.json") -Encoding utf8

Write-Host "Created $archive"
Write-Host "Website publishing bundle: $website"
