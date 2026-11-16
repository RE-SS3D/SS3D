param(
    [string]$Server = "127.0.0.1",
    [int]$Port = 1151,
    [string]$Ckey = "player",
    [string]$InstallDir = (Join-Path $env:LOCALAPPDATA "SS3D"),
    [string]$Tag = "",
    [switch]$NoLaunch
)

$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$Repo = "RE-SS3D/SS3D"
$Api = "https://api.github.com/repos/$Repo"
$Headers = @{
    "User-Agent" = "SS3D-Launcher"
    "Accept" = "application/vnd.github+json"
}
$Versions = Join-Path $InstallDir "versions"
$VersionFile = Join-Path $InstallDir ".version"
New-Item -ItemType Directory -Path $Versions -Force | Out-Null

$Local = if (Test-Path $VersionFile) { (Get-Content $VersionFile -Raw).Trim() } else { "0.0.0" }

function Find-Exe([string]$Root) {
    if (-not (Test-Path $Root)) { return $null }
    return Get-ChildItem -Path $Root -Recurse -File -Filter "SS3D.exe" -ErrorAction SilentlyContinue |
        Select-Object -First 1
}

function Start-SS3D([string]$Exe) {
    if ($NoLaunch) {
        Write-Host "installed: $($Exe.FullName)"
        return
    }
    $gameArgs = @("-ip=$Server", "-port=$Port", "-ckey=$Ckey", "-skipintro")
    Start-Process -FilePath $Exe.FullName -WorkingDirectory $Exe.DirectoryName -ArgumentList $gameArgs
}

$LocalExe = Find-Exe (Join-Path $Versions $Local)
if ($Tag) {
    $ReleaseTag = $Tag
} else {
    try {
        $Releases = Invoke-RestMethod -Uri "$Api/releases?per_page=50" -Headers $Headers -UseBasicParsing
    } catch {
        if ($LocalExe) {
            Write-Warning "Could not reach GitHub; launching local $Local"
            Start-SS3D $LocalExe
            exit 0
        }
        throw "Could not reach GitHub and no local build is installed: $_"
    }
    $Stable = $Releases | Where-Object { $_.tag_name -match '^v\d+\.\d+\.\d+$' }
    $Latest = $Stable | Sort-Object { [version]($_.tag_name.Substring(1)) } | Select-Object -Last 1
    $ReleaseTag = if ($Latest) { $Latest.tag_name } else { "" }
}

if (-not $ReleaseTag) {
    if ($LocalExe) {
        Write-Warning "No stable v* release found; launching local $Local"
        Start-SS3D $LocalExe
        exit 0
    }
    throw "No stable v* release found and no local build is installed."
}

if ($ReleaseTag -eq $Local -and $LocalExe) {
    Write-Host "already on $ReleaseTag"
    Start-SS3D $LocalExe
    exit 0
}

$Release = Invoke-RestMethod -Uri "$Api/releases/tags/$ReleaseTag" -Headers $Headers -UseBasicParsing
$Asset = $Release.assets | Where-Object { $_.name -like "SS3D-Windows-*.zip" } | Select-Object -First 1
if (-not $Asset) { throw "No Windows client asset on $ReleaseTag." }
if (-not $Asset.digest) { throw "$($Asset.name) has no SHA-256 digest; refusing to update." }

$Temp = Join-Path $env:TEMP ("ss3d-launcher-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $Temp | Out-Null
try {
    $Zip = Join-Path $Temp $Asset.name
    Write-Host "downloading $ReleaseTag ($($Asset.name))"
    Invoke-WebRequest -Uri $Asset.browser_download_url -OutFile $Zip -UseBasicParsing

    $Actual = (Get-FileHash -Path $Zip -Algorithm SHA256).Hash.ToLowerInvariant()
    $Expected = ($Asset.digest -replace '^sha256:', '').ToLowerInvariant()
    if ($Actual -ne $Expected) { throw "SHA-256 mismatch (expected $Expected, got $Actual)." }

    $Extract = Join-Path $Temp "extract"
    Expand-Archive -Path $Zip -DestinationPath $Extract -Force
    $NewExe = Find-Exe $Extract
    if (-not $NewExe) { throw "No SS3D.exe in $($Asset.name)." }

    $NewDir = Join-Path $Versions "$ReleaseTag.new"
    $DestDir = Join-Path $Versions $ReleaseTag
    if (Test-Path $NewDir) { Remove-Item $NewDir -Recurse -Force }
    Move-Item -Path $Extract -Destination $NewDir
    if (Test-Path $DestDir) { Remove-Item $DestDir -Recurse -Force }
    Move-Item -Path $NewDir -Destination $DestDir
    Set-Content -Path $VersionFile -Value $ReleaseTag

    Get-ChildItem -Path $Versions -Directory |
        Sort-Object LastWriteTime -Descending |
        Select-Object -Skip 2 |
        Remove-Item -Recurse -Force

    $Exe = Find-Exe $DestDir
    if (-not $Exe) { throw "Installed build has no SS3D.exe." }
    Start-SS3D $Exe
} finally {
    Remove-Item -Path $Temp -Recurse -Force -ErrorAction SilentlyContinue
}
