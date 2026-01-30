$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$csprojPath = Join-Path $repoRoot "PulseAPK.csproj"
$aboutPath = Join-Path $repoRoot "ViewModels/AboutViewModel.cs"

$csprojContent = Get-Content -Raw -Path $csprojPath
$versionMatch = [regex]::Match($csprojContent, "<Version>([^<]+)</Version>")

if (-not $versionMatch.Success) {
    Write-Error "Unable to determine current version from $csprojPath."
    exit 1
}

$currentVersion = $versionMatch.Groups[1].Value

if ($currentVersion -notmatch "^1\.1\.(\d+)$") {
    Write-Error "Version '$currentVersion' is not in the expected 1.1.x format."
    exit 1
}

$patch = [int]$Matches[1] + 1
$nextVersion = "1.1.$patch"

$csprojContent = $csprojContent -replace "<Version>[^<]+</Version>", "<Version>$nextVersion</Version>"
Set-Content -Path $csprojPath -Value $csprojContent

$aboutContent = Get-Content -Raw -Path $aboutPath
$aboutContent = $aboutContent -replace "\?\? ""1\.1\.\d+""", "?? ""$nextVersion"""
Set-Content -Path $aboutPath -Value $aboutContent

Write-Host "Bumped version to $nextVersion."
