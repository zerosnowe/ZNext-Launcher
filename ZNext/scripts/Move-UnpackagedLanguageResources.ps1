param(
    [Parameter(Mandatory = $true)]
    [string]$PublishDir,

    [string]$LanguageRoot = "Languages"
)

$ErrorActionPreference = "Stop"

if ([System.IO.Path]::IsPathRooted($PublishDir)) {
    $publishPath = $PublishDir
} else {
    $publishPath = Join-Path (Get-Location) $PublishDir
}

if (-not (Test-Path -LiteralPath $publishPath)) {
    throw "Publish directory not found: $publishPath"
}

$resolvedPublishPath = (Resolve-Path -LiteralPath $publishPath).Path
$languageRootPath = Join-Path $resolvedPublishPath $LanguageRoot
$cultureDirectoryPattern = '^[a-z]{2,3}(-[a-z0-9]{2,8}){1,3}$'

$cultureDirectories = Get-ChildItem -LiteralPath $resolvedPublishPath -Directory |
    Where-Object {
        $_.Name -match $cultureDirectoryPattern -and
        -not [string]::Equals($_.Name, $LanguageRoot, [StringComparison]::OrdinalIgnoreCase)
    }

if ($cultureDirectories.Count -eq 0) {
    Write-Host "No unpackaged culture folders found in publish root."
    return
}

if (Test-Path -LiteralPath $languageRootPath) {
    $resolvedLanguageRootPath = (Resolve-Path -LiteralPath $languageRootPath).Path
    if (-not $resolvedLanguageRootPath.StartsWith($resolvedPublishPath, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Unsafe language resource directory: $resolvedLanguageRootPath"
    }

    Remove-Item -LiteralPath $languageRootPath -Recurse -Force
}

New-Item -Path $languageRootPath -ItemType Directory -Force | Out-Null

foreach ($cultureDirectory in $cultureDirectories) {
    Move-Item -LiteralPath $cultureDirectory.FullName -Destination (Join-Path $languageRootPath $cultureDirectory.Name)
}

Write-Host "Moved unpackaged culture folders to: $languageRootPath"
