# Standalone regression tests: no Pester, network, Unity, or real installation needed.
# pwsh -NoProfile -File Tools/updater/test-update.ps1
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/../../Builds/Update_SS3D.ps1"
Add-Type -AssemblyName System.IO.Compression.FileSystem
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('ss3d-update-tests-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testRoot | Out-Null
$script:running = $false
$script:downloadFails = $false
$script:failMove = $false
$script:downloads = 0
$script:startDuringDownload = $false

function Assert($Condition, [string]$Message) {
    if (-not $Condition) { throw "ASSERTION FAILED: $Message" }
}
function Write-Fixture([string]$Root, [string]$Name, [string]$Value) {
    $path = Join-Path $Root $Name
    New-Item -ItemType Directory -Path (Split-Path $path -Parent) -Force | Out-Null
    [IO.File]::WriteAllText($path, $Value)
}
function Read-Fixture([string]$Root, [string]$Name) {
    return [IO.File]::ReadAllText((Join-Path $Root $Name))
}
function Expect-Failure([scriptblock]$Action, [string]$Pattern) {
    try { & $Action } catch {
        Assert ($_.Exception.Message -match $Pattern) "Expected '$Pattern', got $($_.Exception.Message)"
        return
    }
    throw "Expected a failure matching $Pattern"
}
function Get-Process {
    param($Name, $Id, $ErrorAction)
    if ($Id) { return $script:gameProcess }
    if ($script:running) { return 'fake SS3D process' }
}
$script:gameProcess = $null
# Like Windows PowerShell 5.1, emit the JSON array as a single pipeline object (unary comma).
function Invoke-RestMethod { param($Uri, $Headers, $TimeoutSec, [switch]$UseBasicParsing) return ,@($script:releases) }
function Invoke-WebRequest {
    param($Uri, $OutFile, $TimeoutSec, [switch]$UseBasicParsing)
    $script:downloads++
    if ($script:downloadFails) { throw 'Network unavailable' }
    Copy-Item -LiteralPath $script:zip -Destination $OutFile
    if ($script:startDuringDownload) { $script:running = $true }
}
function Move-Item {
    param($LiteralPath, $Destination)
    if ($script:failMove -and $LiteralPath -like '*.ss3d-update-*README.txt') {
        $script:failMove = $false
        throw 'Simulated disk failure'
    }
    Microsoft.PowerShell.Management\Move-Item -LiteralPath $LiteralPath -Destination $Destination
}

try {
    $payload = Join-Path $testRoot 'payload'
    $new = Join-Path $payload 'SS3D-Windows-v0.3.0'
    foreach ($name in @('Game/SS3D.exe', 'Game/UnityPlayer.dll', 'Game/SS3D_Data/globalgamemanagers',
        'Game/Config/permissions.txt', 'Game/Config/network.json', 'README.txt', 'TESTERS.md', 'Update_SS3D.ps1',
        'Game/Data/StationTemplates/ministation.json',
        'Game/Data/StationTemplates/edited.json')) { Write-Fixture $new $name 'new release' }
    $script:zip = Join-Path $testRoot 'release.zip'
    [IO.Compression.ZipFile]::CreateFromDirectory($payload, $script:zip)
    $digest = 'sha256:' + (Get-FileHash $script:zip -Algorithm SHA256).Hash
    $release = [pscustomobject]@{
        draft = $false; prerelease = $false; tag_name = 'v0.3.0'
        assets = @([pscustomobject]@{ name = 'SS3D-Windows-v0.3.0.zip'; digest = $digest
            browser_download_url = 'https://github.com/henkhooft/SS3D/releases/download/v0.3.0/SS3D-Windows-v0.3.0.zip' })
    }
    $script:releases = @(
        [pscustomobject]@{ draft = $false; prerelease = $false; tag_name = 'develop-nightly' },
        [pscustomobject]@{ draft = $false; prerelease = $false; tag_name = 'v0.2.9' },
        [pscustomobject]@{ draft = $true; prerelease = $false; tag_name = 'v9.0.0' },
        [pscustomobject]@{ draft = $false; prerelease = $true; tag_name = 'v8.0.0' },
        $release)
    Assert ((Get-SS3DRelease).tag_name -eq 'v0.3.0') 'Select stable numbered release, excluding prereleases, drafts and nightly'
    $allReleases = $script:releases
    $release.prerelease = $true
    $script:releases = @($release)
    Expect-Failure { Get-SS3DRelease } 'No stable numbered release'
    $release.prerelease = $false
    Assert ((Get-SS3DRelease).tag_name -eq 'v0.3.0') 'Promotion makes the same release available'
    $script:releases = $allReleases

    $install = Join-Path $testRoot 'installation with spaces'
    # The installed copy predates the main menu and still has the retired launch scripts.
    foreach ($name in @('Game/SS3D.exe', 'Start_SS3D_Host.bat', 'Start_SS3D_Client.bat', 'README.txt', 'TESTERS.md',
        'Update_SS3D.ps1', 'Update_SS3D.bat', 'Game/Data/StationTemplates/ministation.json', 'Game/Data/StationTemplates/edited.json',
        'Game/Data/StationTemplates/my station.json', 'Game/Data/Exports/map.zip', 'Game/Data/ServerMeta/permissions.json',
        'Game/Logs/player.log', 'Game/Config/network.json')) { Write-Fixture $install $name 'local data' }
    $hash = (Get-FileHash (Join-Path $install 'Game/Data/StationTemplates/ministation.json')).Hash
    @{ tag = 'v0.2.0'; defaults = @{ 'Game/Data/StationTemplates/ministation.json' = $hash
        'Game/Data/StationTemplates/edited.json' = 'original default hash' } } | ConvertTo-Json -Depth 4 |
        Set-Content (Join-Path $install '.ss3d-release.json')

    Update-SS3D $install -CheckOnly
    Assert ($script:downloads -eq 0) 'Check-only never downloads'
    $script:running = $true
    Expect-Failure { Update-SS3D $install } 'Close all SS3D'
    $script:running = $false
    $lock = [IO.File]::Open((Join-Path $install '.ss3d-update.lock'), 'OpenOrCreate', 'ReadWrite', 'None')
    try { Expect-Failure { Update-SS3D $install } 'used by another process' } finally { $lock.Dispose() }
    $script:downloadFails = $true
    Expect-Failure { Update-SS3D $install } 'Network unavailable'
    $script:downloadFails = $false
    Assert ((Read-Fixture $install 'Game/SS3D.exe') -eq 'local data') 'Network failure leaves original intact'
    $release.assets[0].digest = 'sha256:' + ('0' * 64)
    Expect-Failure { Update-SS3D $install } 'SHA-256 mismatch'
    Assert ((Read-Fixture $install 'Game/SS3D.exe') -eq 'local data') 'Bad digest leaves original intact'
    $release.assets[0].digest = $digest
    $script:startDuringDownload = $true
    Expect-Failure { Update-SS3D $install } 'Close all SS3D'
    $script:startDuringDownload = $false
    $script:running = $false
    Assert ((Read-Fixture $install 'Game/SS3D.exe') -eq 'local data') 'Game started during download prevents installation'
    $release.assets[0].digest = ''
    Expect-Failure { Update-SS3D $install } 'no valid SHA-256 digest'
    $release.assets[0].digest = $digest
    $assets = $release.assets
    $release.assets = @()
    Expect-Failure { Update-SS3D $install } 'exactly one Windows download'
    $release.assets = $assets
    $script:failMove = $true
    Expect-Failure { Update-SS3D $install } 'Simulated disk failure'
    Assert ((Read-Fixture $install 'Game/SS3D.exe') -eq 'local data') 'Swap failure restores game'
    Assert ((Read-Fixture $install 'README.txt') -eq 'local data') 'Swap failure restores launch files'
    Assert ((Read-Fixture $install 'Start_SS3D_Host.bat') -eq 'local data') 'Swap failure restores retired launch scripts'
    Assert ((Get-Content (Join-Path $install '.ss3d-release.json') -Raw | ConvertFrom-Json).tag -eq 'v0.2.0') 'Swap failure restores version'

    foreach ($old in @('20260101-000000-aaaaaaaa', '20260201-000000-bbbbbbbb')) {
        Write-Fixture (Join-Path $install ".ss3d-backups/$old") 'Game/SS3D.exe' 'older install'
    }
    Update-SS3D $install
    Assert ((Read-Fixture $install 'Game/SS3D.exe') -eq 'new release') 'Installs game'
    Assert ((Read-Fixture $install 'Update_SS3D.ps1') -eq 'new release') 'Updater script replaces itself'
    Assert ((Read-Fixture $install 'Update_SS3D.bat') -eq 'local data') 'Running batch launcher is left alone'
    foreach ($name in @('Start_SS3D_Host.bat', 'Start_SS3D_Client.bat')) {
        Assert (-not (Test-Path -LiteralPath (Join-Path $install $name))) "Moves retired $name out of the installation"
    }
    Assert (@(Get-ChildItem (Join-Path $install '.ss3d-backups') -Directory).Count -eq 1) 'Only the previous installation is kept'
    Assert ((Read-Fixture $install 'Game/Data/StationTemplates/ministation.json') -eq 'new release') 'Refreshes unchanged shipped map'
    foreach ($name in @('Game/Data/StationTemplates/edited.json', 'Game/Data/StationTemplates/my station.json',
        'Game/Data/Exports/map.zip', 'Game/Data/ServerMeta/permissions.json', 'Game/Logs/player.log', 'Game/Config/network.json')) {
        Assert ((Read-Fixture $install $name) -eq 'local data') "Preserves $name"
    }
    $backups = @(Get-ChildItem (Join-Path $install '.ss3d-backups') -Directory |
        Where-Object { Test-Path (Join-Path $_.FullName 'Game/SS3D.exe') })
    Assert ($backups.Count -eq 1) 'Keeps a complete runnable backup'
    Assert ((Read-Fixture $backups[0].FullName 'Game/SS3D.exe') -eq 'local data') 'Backup contains old build'
    Assert ((Read-Fixture $backups[0].FullName 'Start_SS3D_Host.bat') -eq 'local data') 'Backup keeps the retired launch script'
    Assert ((Read-Fixture $backups[0].FullName 'Update_SS3D.ps1') -eq 'local data') 'Backup contains matching updater'
    $count = $script:downloads
    Update-SS3D $install
    Assert ($script:downloads -eq $count) 'Same version does not download again'
    $metadata = Get-Content (Join-Path $install '.ss3d-release.json') -Raw | ConvertFrom-Json
    $metadata.tag = 'v9.0.0'
    $metadata | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $install '.ss3d-release.json')
    Update-SS3D $install
    Assert ($script:downloads -eq $count) 'Does not downgrade a newer installation'

    # Older downloads lack a manifest: conservatively keep every local map.
    $legacy = Join-Path $testRoot 'legacy'
    Write-Fixture $legacy 'Game/SS3D.exe' 'old'
    Write-Fixture $legacy 'Game/Data/StationTemplates/ministation.json' 'my edited ministation'
    Update-SS3D $legacy
    Assert ((Read-Fixture $legacy 'Game/Data/StationTemplates/ministation.json') -eq 'my edited ministation') 'Legacy map collision is preserved'

    # MAX_PATH (playtest report: "Could not find a part of the path" from a deep Downloads folder).
    # Boundaries: a file path may be 259 characters, a folder path 247.
    $limitRoot = Join-Path $testRoot 'limit'
    $base = $limitRoot.Length + 1
    Assert-SS3DPathLength @('f' * (259 - $base)) @($limitRoot)
    Expect-Failure { Assert-SS3DPathLength @('f' * (260 - $base)) @($limitRoot) } '1 character\(s\) over the limit.*C:\\SS3D'
    Assert-SS3DPathLength @(('d' * (247 - $base)) + '/f') @($limitRoot)
    Expect-Failure { Assert-SS3DPathLength @(('d' * (248 - $base)) + '/f') @($limitRoot) } 'too long'
    Assert-SS3DPathLength @(('d' * (247 - $base)) + '/') @($limitRoot)
    Expect-Failure { Assert-SS3DPathLength @(('d' * (248 - $base)) + '/') @($limitRoot) } 'too long'
    Expect-Failure { Assert-SS3DPathLength @('f' * (259 - $base)) @($limitRoot, "$limitRoot-longer") } 'too long'
    $long = Join-Path $testRoot 'long.zip'
    $archive = [IO.Compression.ZipFile]::Open($long, [IO.Compression.ZipArchiveMode]::Create)
    $null = $archive.CreateEntry('SS3D-Windows-v0.3.0/Game/SS3D_Data/' + ('a' * 240) + '.bundle')
    $archive.Dispose()
    $longExtract = Join-Path $testRoot 'long-extract'
    Expect-Failure { Expand-SS3DRelease $long $longExtract 'v0.3.0' } 'too long'
    Assert (-not (Test-Path -LiteralPath $longExtract)) 'Too-long archive is refused before anything is unpacked'
    # A real update from a deep folder: refused with advice, install untouched, staging cleaned up.
    $deep = Join-Path $testRoot ('deep' + ('p' * (195 - $testRoot.Length)))
    Assert ($deep.Length -eq 200) 'Deep installation fixture is 200 characters'
    Write-Fixture $deep 'Game/SS3D.exe' 'old'
    Expect-Failure { Update-SS3D $deep } 'too long.*Your installation has not been changed'
    Assert ((Read-Fixture $deep 'Game/SS3D.exe') -eq 'old') 'Too-long update leaves the installation intact'
    Assert (@(Get-ChildItem -LiteralPath $deep -Directory -Force -Filter '.ss3d-update-*').Count -eq 0) 'Too-long update removes its staging folder'

    $unsafe = Join-Path $testRoot 'unsafe.zip'
    $archive = [IO.Compression.ZipFile]::Open($unsafe, [IO.Compression.ZipArchiveMode]::Create)
    $null = $archive.CreateEntry('SS3D-Windows-v0.3.0/../../escape.txt')
    $archive.Dispose()
    Expect-Failure { Expand-SS3DRelease $unsafe (Join-Path $testRoot 'unsafe-extract') 'v0.3.0' } 'Unsafe'
    $incomplete = Join-Path $testRoot 'incomplete.zip'
    $archive = [IO.Compression.ZipFile]::Open($incomplete, [IO.Compression.ZipArchiveMode]::Create)
    $null = $archive.CreateEntry('SS3D-Windows-v0.3.0/Game/SS3D.exe')
    $archive.Dispose()
    Expect-Failure { Expand-SS3DRelease $incomplete (Join-Path $testRoot 'incomplete-extract') 'v0.3.0' } 'Incomplete'
    # In-game Update: wait for the quitting game; a process that never exits is refused.
    Wait-SS3DExit 4242
    $script:gameProcess = [pscustomobject]@{ Exited = $true } |
        Add-Member -MemberType ScriptMethod -Name WaitForExit -Value { param($ms) $this.Exited } -PassThru
    Wait-SS3DExit 4242
    $script:gameProcess.Exited = $false
    Expect-Failure { Wait-SS3DExit 4242 1 } 'did not close'
    $script:gameProcess = $null
    Write-Host 'PASS: stable release selection/promotion, check-only, process/lock refusal, download/hash/asset failures, swap rollback, data preservation, self-update, n-1 backup retention, repeat update, no downgrade, legacy update, MAX_PATH refusal, unsafe/incomplete archives, wait for the quitting game.'
} finally { Remove-Item -LiteralPath $testRoot -Recurse -Force }
