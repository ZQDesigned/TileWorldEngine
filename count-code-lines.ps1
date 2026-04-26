param(
    [string]$Root = ".",
    [switch]$IncludeBlankLines,
    [switch]$IncludeComments
)

$ErrorActionPreference = "Stop"

$rootPath = (Resolve-Path -LiteralPath $Root).Path

# Default to C# source files only.
$codeExtensions = @(
    ".cs"
)

# Common folders to skip.
$excludeDirs = @(
    ".git", ".idea", ".vs",
    "bin", "obj", "packages", "node_modules",
    "artifacts", "TestResults"
)

function Is-ExcludedPath {
    param(
        [string]$FullPath,
        [string[]]$ExcludedDirNames
    )

    $parts = $FullPath.Split([System.IO.Path]::DirectorySeparatorChar, [System.StringSplitOptions]::RemoveEmptyEntries)
    foreach ($part in $parts) {
        if ($ExcludedDirNames -contains $part) {
            return $true
        }
    }
    return $false
}

function Is-CommentLine {
    param(
        [string]$Line,
        [string]$Extension
    )

    $trim = $Line.Trim()
    if ([string]::IsNullOrWhiteSpace($trim)) {
        return $false
    }

    switch ($Extension.ToLowerInvariant()) {
        ".cs" { return $trim.StartsWith("//") }
        ".cpp" { return $trim.StartsWith("//") }
        ".c" { return $trim.StartsWith("//") }
        ".h" { return $trim.StartsWith("//") }
        ".hpp" { return $trim.StartsWith("//") }
        ".js" { return $trim.StartsWith("//") }
        ".ts" { return $trim.StartsWith("//") }
        ".jsx" { return $trim.StartsWith("//") }
        ".tsx" { return $trim.StartsWith("//") }
        ".java" { return $trim.StartsWith("//") }
        ".kt" { return $trim.StartsWith("//") }
        ".go" { return $trim.StartsWith("//") }
        ".py" { return $trim.StartsWith("#") }
        ".rb" { return $trim.StartsWith("#") }
        ".sh" { return $trim.StartsWith("#") }
        ".ps1" { return $trim.StartsWith("#") }
        ".psm1" { return $trim.StartsWith("#") }
        ".sql" { return $trim.StartsWith("--") }
        default { return $false }
    }
}

$files = Get-ChildItem -LiteralPath $rootPath -Recurse -File | Where-Object {
    ($codeExtensions -contains $_.Extension.ToLowerInvariant()) -and
    -not (Is-ExcludedPath -FullPath $_.FullName -ExcludedDirNames $excludeDirs)
}

if (-not $files) {
    Write-Host "No matching code files found under: $rootPath"
    exit 0
}

$totalLines = 0
$totalFiles = 0
$byExtension = @{}

foreach ($file in $files) {
    $lines = Get-Content -LiteralPath $file.FullName -Encoding UTF8
    $count = 0
    foreach ($line in $lines) {
        if (-not $IncludeBlankLines -and [string]::IsNullOrWhiteSpace($line)) {
            continue
        }
        if (-not $IncludeComments -and (Is-CommentLine -Line $line -Extension $file.Extension)) {
            continue
        }
        $count++
    }

    $totalFiles++
    $totalLines += $count

    $ext = $file.Extension.ToLowerInvariant()
    if (-not $byExtension.ContainsKey($ext)) {
        $byExtension[$ext] = [PSCustomObject]@{
            Extension = $ext
            Files = 0
            Lines = 0
        }
    }
    $byExtension[$ext].Files++
    $byExtension[$ext].Lines += $count
}

Write-Host ""
Write-Host "Code statistics for: $rootPath" -ForegroundColor Cyan
Write-Host ("Files: {0}" -f $totalFiles)
Write-Host ("Total lines: {0}" -f $totalLines)
Write-Host ""
Write-Host "By extension:" -ForegroundColor Yellow
$byExtension.Values |
    Sort-Object -Property Lines -Descending |
    Format-Table Extension, Files, Lines -AutoSize
