param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [string]$ArchiveName = "ZNext_Launcher_Green_x64.zip",
    [switch]$SkipPublish
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptRoot
$projectPath = Join-Path $projectRoot "ZNext.csproj"

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Project file not found: $projectPath"
}

if (-not $SkipPublish) {
    dotnet publish $projectPath `
        -c $Configuration `
        -p:Platform=$Platform `
        -p:GenerateAppxPackageOnBuild=false

    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

$targetFramework = dotnet msbuild $projectPath `
    -p:Configuration=$Configuration `
    -p:Platform=$Platform `
    -getProperty:TargetFramework

$runtimeIdentifier = dotnet msbuild $projectPath `
    -p:Configuration=$Configuration `
    -p:Platform=$Platform `
    -getProperty:RuntimeIdentifier

$publishDir = Join-Path $projectRoot "bin\$Configuration\$targetFramework\$runtimeIdentifier\publish"
if (-not (Test-Path -LiteralPath $publishDir)) {
    throw "Publish directory not found: $publishDir"
}

$resolvedPublishDir = (Resolve-Path -LiteralPath $publishDir).Path
$resolvedProjectRoot = (Resolve-Path -LiteralPath $projectRoot).Path
if (-not $resolvedPublishDir.StartsWith($resolvedProjectRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Unsafe publish directory: $resolvedPublishDir"
}

$languageRootName = dotnet msbuild $projectPath `
    -p:Configuration=$Configuration `
    -p:Platform=$Platform `
    -getProperty:ZNextLanguageResourceDirectory

if ([string]::IsNullOrWhiteSpace($languageRootName)) {
    $languageRootName = "Languages"
}

$moveLanguageResourcesScript = Join-Path $scriptRoot "Move-UnpackagedLanguageResources.ps1"
& $moveLanguageResourcesScript -PublishDir $resolvedPublishDir -LanguageRoot $languageRootName

$languageRootDir = Join-Path $resolvedPublishDir $languageRootName
$cultureDirectoryPattern = '^[a-z]{2,3}(-[a-z0-9]{2,8}){1,3}$'
$keptCultures = New-Object System.Collections.Generic.List[string]
$removedCultures = New-Object System.Collections.Generic.List[string]

if (Test-Path -LiteralPath $languageRootDir) {
    Get-ChildItem -LiteralPath $languageRootDir -Directory |
        Where-Object { $_.Name -match $cultureDirectoryPattern } |
        ForEach-Object {
            $cultureName = $_.Name
            $shouldKeep = $cultureName.StartsWith("en", [StringComparison]::OrdinalIgnoreCase) -or
                [string]::Equals($cultureName, "zh-CN", [StringComparison]::OrdinalIgnoreCase)

            if ($shouldKeep) {
                $keptCultures.Add($cultureName)
                return
            }

            $removedCultures.Add($cultureName)
            Remove-Item -LiteralPath $_.FullName -Recurse -Force
        }
}

$archiveDir = Join-Path $projectRoot "AppPackages"
New-Item -Path $archiveDir -ItemType Directory -Force | Out-Null
$archivePath = Join-Path $archiveDir $ArchiveName
Compress-Archive -Path (Join-Path $resolvedPublishDir "*") -DestinationPath $archivePath -Force

Write-Host "Green package created: $archivePath"
Write-Host "Publish directory: $resolvedPublishDir"
Write-Host "Language resource directory: $languageRootDir"
Write-Host "Kept culture folders: $([string]::Join(', ', ($keptCultures | Sort-Object)))"
Write-Host "Removed culture folder count: $($removedCultures.Count)"
