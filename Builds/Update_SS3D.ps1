#requires -Version 5.1
param([switch]$CheckOnly, [int]$WaitPid = 0, [switch]$Restart)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2

function Get-SS3DRelease {
    # Only owner-promoted stable releases belong on the tester update channel.
    $headers = @{ 'User-Agent' = 'SS3D-Updater'; Accept = 'application/vnd.github+json' }
    $releases = @()
    for ($page = 1; ; $page++) {
        # Windows PowerShell 5.1 emits a JSON array as ONE pipeline object, so @(Invoke-RestMethod ...)
        # would nest it (one item: the whole page). Assigning first unrolls it in 5.1 and 7 alike.
        $response = Invoke-RestMethod -Uri "https://api.github.com/repos/henkhooft/SS3D/releases?per_page=100&page=$page" -Headers $headers -TimeoutSec 30 -UseBasicParsing
        $batch = @($response)
        $releases += $batch
        if ($batch.Count -lt 100) { break }
    }
    $latest = $releases | Where-Object {
        -not $_.draft -and -not $_.prerelease -and $_.tag_name -cmatch '^v[0-9]+\.[0-9]+\.[0-9]+$'
    } | Sort-Object { [version]$_.tag_name.Substring(1) } -Descending | Select-Object -First 1
    if (-not $latest) { throw 'No stable numbered release is available. Try again after a release is promoted.' }
    return $latest
}

function Assert-SS3DClosed {
    # Check again immediately before the swap, in case it started during download.
    if (Get-Process -Name SS3D -ErrorAction SilentlyContinue) {
        throw 'Close all SS3D game windows before updating. No game process was stopped.'
    }
}

function Wait-SS3DExit([int]$ProcessId, [int]$TimeoutSeconds = 60) {
    # The in-game Update button quits the game; wait for it before the closed check.
    $process = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
    if (-not $process) { return }
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        throw 'The game did not close within a minute. Close it and run the updater again.'
    }
}

function Start-SS3DGame([string]$Root) {
    # No arguments: the game opens its main menu.
    $game = Join-Path $Root 'Game'
    Start-Process -FilePath (Join-Path $game 'SS3D.exe') -WorkingDirectory $game
}

function Expand-SS3DRelease([string]$Zip, [string]$Destination, [string]$Tag) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($Zip)
    try {
        $prefix = "SS3D-Windows-$Tag/"
        # Validate ALL paths before extraction; Windows treats backslashes as separators too.
        foreach ($entry in $archive.Entries) {
            $name = $entry.FullName.Replace('\', '/')
            if (-not $name.StartsWith($prefix, [StringComparison]::Ordinal) -or
                $name.Contains(':') -or $name -match '(^|/)\.\.?(/|$)' -or
                $name -match '[\x00-\x1f]' -or $name -match '[. ](/|$)') {
                throw "Unsafe or unexpected archive path: $name"
            }
        }
    } finally { $archive.Dispose() }
    [IO.Compression.ZipFile]::ExtractToDirectory($Zip, $Destination)
    $root = Join-Path $Destination "SS3D-Windows-$Tag"
    foreach ($required in @('Game/SS3D.exe', 'Game/UnityPlayer.dll', 'Game/SS3D_Data/globalgamemanagers',
            'Game/Config/permissions.txt', 'Game/Config/network.json',
            'Game/Data/StationTemplates/ministation.json', 'README.txt', 'TESTERS.md', 'Update_SS3D.ps1')) {
        if (-not (Test-Path -LiteralPath (Join-Path $root $required) -PathType Leaf)) {
            throw "Incomplete Windows release: missing $required"
        }
    }
    return $root
}

function Copy-SS3DLocalData([string]$OldRoot, [string]$NewRoot) {
    $defaults = @{}
    $manifest = Join-Path $OldRoot '.ss3d-release.json'
    if (Test-Path -LiteralPath $manifest) {
        $metadata = Get-Content -LiteralPath $manifest -Raw | ConvertFrom-Json
        foreach ($property in $metadata.defaults.PSObject.Properties) {
            $defaults[$property.Name] = $property.Value
        }
    }
    foreach ($folder in @('Game/Config', 'Game/Data', 'Game/Logs')) {
        $source = Join-Path $OldRoot $folder
        if (-not (Test-Path -LiteralPath $source)) { continue }
        # Never follow junctions/symlinks out of the installation.
        $entries = @(Get-Item -LiteralPath $source) + @(Get-ChildItem -LiteralPath $source -Recurse -Force)
        if ($entries | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }) {
            throw "Remove links/junctions from $folder before updating."
        }
        foreach ($file in $entries | Where-Object { -not $_.PSIsContainer }) {
            $relative = $folder + '/' + $file.FullName.Substring($source.Length + 1).Replace('\', '/')
            # Refresh unmodified shipped maps. Preserve custom/modified maps, including name collisions.
            if ($defaults.ContainsKey($relative) -and
                (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash -eq $defaults[$relative]) {
                continue
            }
            $target = Join-Path $NewRoot $relative
            New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
            Copy-Item -LiteralPath $file.FullName -Destination $target -Force
        }
    }
}

function Install-SS3DRelease([string]$Root, [string]$NewRoot, [string]$Backup) {
    # Update_SS3D.ps1 replaces itself so updater fixes reach existing installs; PowerShell has already
    # read it. Update_SS3D.bat is NOT replaced: cmd reads a running batch file line by line.
    $managed = @('Game', 'README.txt', 'TESTERS.md', 'Update_SS3D.ps1', '.ss3d-release.json')
    # Launch scripts from releases before the main menu replaced them; moved into the backup, not reinstalled.
    $retired = @('Start_SS3D_Host.bat', 'Start_SS3D_Client.bat')
    $saved = @()
    $installed = @()
    New-Item -ItemType Directory -Path $Backup | Out-Null
    try {
        foreach ($name in $retired) {
            $old = Join-Path $Root $name
            if (Test-Path -LiteralPath $old) {
                Move-Item -LiteralPath $old -Destination (Join-Path $Backup $name)
                $saved += $name
            }
        }
        foreach ($name in $managed) {
            $old = Join-Path $Root $name
            if (Test-Path -LiteralPath $old) {
                Move-Item -LiteralPath $old -Destination (Join-Path $Backup $name)
                $saved += $name
            }
            Move-Item -LiteralPath (Join-Path $NewRoot $name) -Destination $old
            $installed += $name
        }
    } catch {
        $failure = $_
        # Keep failed payload in staging; restore the original files without deleting user data.
        foreach ($name in $installed) {
            Move-Item -LiteralPath (Join-Path $Root $name) -Destination (Join-Path $NewRoot $name)
        }
        foreach ($name in $saved) {
            Move-Item -LiteralPath (Join-Path $Backup $name) -Destination (Join-Path $Root $name)
        }
        throw $failure
    }
}

function Remove-SS3DOldBackups([string]$Backups, [string]$Keep) {
    # Keep only the backup just made (the previous version); older ones add up to GBs.
    $removed = 0
    # $Keep may mix / and \; GetFullPath normalizes both sides before comparing.
    $Keep = [IO.Path]::GetFullPath($Keep).TrimEnd('\', '/')
    foreach ($old in @(Get-ChildItem -LiteralPath $Backups -Directory -Force)) {
        if ([IO.Path]::GetFullPath($old.FullName).TrimEnd('\', '/') -eq $Keep -or
            ($old.Attributes -band [IO.FileAttributes]::ReparsePoint)) { continue }
        Remove-Item -LiteralPath $old.FullName -Recurse -Force -ErrorAction SilentlyContinue
        if (-not (Test-Path -LiteralPath $old.FullName)) { $removed++ }
    }
    return $removed
}

function Update-SS3D([string]$Root, [switch]$CheckOnly) {
    if (-not (Test-Path -LiteralPath (Join-Path $Root 'Game/SS3D.exe') -PathType Leaf)) {
        throw 'Keep the updater beside the Game folder in an extracted Windows release.'
    }
    Assert-SS3DClosed
    $lock = $null
    $stage = $null
    $swapStarted = $false
    $complete = $false
    try {
        # The file remains as an inert lock marker; FileShare.None is the actual lock.
        $lock = [IO.File]::Open((Join-Path $Root '.ss3d-update.lock'), [IO.FileMode]::OpenOrCreate,
            [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        $release = Get-SS3DRelease
        $tag = $release.tag_name
        $manifest = Join-Path $Root '.ss3d-release.json'
        $local = 'unknown (older download)'
        if (Test-Path -LiteralPath $manifest) {
            $local = (Get-Content -LiteralPath $manifest -Raw | ConvertFrom-Json).tag
        }
        Write-Host "Installed: $local   Latest stable release: $tag"
        if ($local -match '^v[0-9]+\.[0-9]+\.[0-9]+$' -and
            [version]$local.Substring(1) -ge [version]$tag.Substring(1)) {
            Write-Host 'Already up to date (or newer).'
            return
        }
        if ($CheckOnly) { return }
        $assets = @($release.assets | Where-Object { $_.name -ceq "SS3D-Windows-$tag.zip" })
        if ($assets.Count -ne 1) { throw "Release $tag does not have exactly one Windows download. Try again later." }
        $asset = $assets[0]
        if ($asset.digest -notmatch '^sha256:[a-fA-F0-9]{64}$') {
            throw 'The release has no valid SHA-256 digest. Refusing to update.'
        }
        $expectedUrl = "https://github.com/henkhooft/SS3D/releases/download/$tag/SS3D-Windows-$tag.zip"
        if ($asset.browser_download_url -cne $expectedUrl) { throw 'Unexpected release download URL.' }
        $stage = Join-Path $Root ('.ss3d-update-' + [guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $stage | Out-Null
        $zip = Join-Path $stage 'release.zip'
        Write-Host "Downloading $tag... Your current installation stays in place until verification finishes."
        Invoke-WebRequest -Uri $expectedUrl -OutFile $zip -UseBasicParsing -TimeoutSec 600
        if ((Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash -ne $asset.digest.Substring(7)) {
            throw 'Download SHA-256 mismatch. Your installation has not been changed.'
        }
        $newRoot = Expand-SS3DRelease $zip (Join-Path $stage 'extract') $tag
        # Capture defaults BEFORE copying local maps over them. Also supports older releases without metadata.
        $defaults = @{}
        foreach ($folder in @('Game/Data/StationTemplates', 'Game/Data/Tilemaps')) {
            $path = Join-Path $newRoot $folder
            if (Test-Path -LiteralPath $path) {
                foreach ($file in Get-ChildItem -LiteralPath $path -File -Recurse) {
                    $relative = $file.FullName.Substring($newRoot.Length + 1).Replace('\', '/')
                    $defaults[$relative] = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
                }
            }
        }
        @{ tag = $tag; defaults = $defaults } | ConvertTo-Json -Depth 4 |
            Set-Content -LiteralPath (Join-Path $newRoot '.ss3d-release.json') -Encoding UTF8
        Copy-SS3DLocalData $Root $newRoot
        Assert-SS3DClosed
        $backup = Join-Path $Root ('.ss3d-backups/' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 8))
        $swapStarted = $true
        Install-SS3DRelease $Root $newRoot $backup
        $complete = $true
        $removed = Remove-SS3DOldBackups (Split-Path $backup -Parent) $backup
        Write-Host "Updated to $tag. Start Game\SS3D.exe as usual."
        Write-Host "Previous installation: $backup"
        if ($removed) { Write-Host "Removed $removed older backup(s); only the previous installation is kept." }
        Write-Host 'To roll back, close SS3D and run Game\SS3D.exe inside that backup folder.'
    } finally {
        # On a swap/restore failure leave staging available for manual recovery.
        if ($stage -and (Test-Path -LiteralPath $stage) -and (-not $swapStarted -or $complete)) {
            Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue
        }
        if ($lock) { $lock.Dispose() }
    }
}

# Dot-sourcing exposes functions to isolated tests without touching an installation.
if ($MyInvocation.InvocationName -ne '.') {
    $result = 0
    try {
        if ($WaitPid) { Wait-SS3DExit $WaitPid }
        Update-SS3D -Root $PSScriptRoot -CheckOnly:$CheckOnly
    }
    catch {
        Write-Host "Update failed: $($_.Exception.Message)" -ForegroundColor Red
        $result = 1
        # Launched from the game: keep the reason readable before the old version restarts.
        if ($Restart) { Read-Host 'Press Enter to restart SS3D' | Out-Null }
    }
    if ($Restart) { Start-SS3DGame $PSScriptRoot }
    exit $result
}
