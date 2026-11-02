# End-to-end check of the SHIPPED Windows updater against real GitHub releases, on real Windows.
# Installs the previous stable release (or -FromTag), seeds local data, runs its Update_SS3D.bat,
# then checks the result, a repeat update, and that both the updated game and the rollback backup
# start (headless host + Tools/updater/e2e-host.txt). Needs gh (GH_TOKEN) and network.
# -UseRepoUpdater swaps in this checkout's Builds/Update_SS3D.ps1 first, to test an updater
# change against real releases before it ships.
# Run by .github/workflows/updater-e2e.yml; locally: powershell -File Tools/updater/e2e-windows.ps1
param([string]$FromTag = '', [string]$WorkDir = '', [switch]$UseRepoUpdater)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2
$repo = 'henkhooft/SS3D'
if (-not $WorkDir) { $WorkDir = Join-Path ([IO.Path]::GetTempPath()) 'ss3d-updater-e2e' }
$hostScript = Join-Path $PSScriptRoot 'e2e-host.txt'

function Assert($Condition, [string]$Message) {
    if (-not $Condition) { throw "ASSERTION FAILED: $Message" }
    Write-Host "ok: $Message"
}
function Get-Hash([string]$Path) { (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash }
function Get-Release([string]$Tag, [string]$Destination) {
    gh release download $Tag -R $repo -p "SS3D-Windows-$Tag.zip" -D $WorkDir --clobber
    if ($LASTEXITCODE) { throw "Could not download the Windows zip of $Tag" }
    Expand-Archive -LiteralPath (Join-Path $WorkDir "SS3D-Windows-$Tag.zip") -DestinationPath $Destination
    return Join-Path $Destination "SS3D-Windows-$Tag"
}
function Invoke-Updater([string]$Root) {
    # A wrapper avoids cmd quoting rules for paths with spaces; NUL answers the final `pause`.
    $wrapper = Join-Path $WorkDir 'run-updater.cmd'
    [IO.File]::WriteAllText($wrapper, "@call `"$Root\Update_SS3D.bat`" < NUL`r`n@exit /b %ERRORLEVEL%`r`n")
    $ErrorActionPreference = 'Continue'  # Windows PowerShell turns redirected native stderr into errors.
    $output = & cmd.exe /c $wrapper 2>&1 | Out-String
    Write-Host $output
    Assert ($LASTEXITCODE -eq 0) "Update_SS3D.bat exits 0 (got $LASTEXITCODE)"
    return $output
}
function Test-Launch([string]$Root, [string]$Label) {
    # The client keeps running after ScriptComplete in batchmode, so stop it once the signal lands.
    # Start inside Game\ like a double-click from Explorer would.
    $log = Join-Path $Root 'Game\Logs\LogHost.json'
    Remove-Item -LiteralPath $log -ErrorAction SilentlyContinue
    $unityLog = Join-Path $WorkDir "$Label-unity.log"
    $game = Start-Process -FilePath (Join-Path $Root 'Game\SS3D.exe') -WorkingDirectory (Join-Path $Root 'Game') -PassThru -ArgumentList @(
        '-batchmode', '-host', '-port=1151', '-ckey=e2e', '-skipintro', "-testscript=$hostScript", '-logFile', $unityLog)
    $result = 'timeout'
    $deadline = (Get-Date).AddMinutes(5)
    try {
        while ((Get-Date) -lt $deadline) {
            if (Test-Path -LiteralPath $log) {
                $text = Get-Content -LiteralPath $log -Raw
                if ($text -match 'ScriptFailed') { $result = 'ScriptFailed'; break }
                if ($text -match 'ScriptComplete') { $result = 'ScriptComplete'; break }
            }
            if ($game.HasExited) { $result = "exited with code $($game.ExitCode)"; break }
            Start-Sleep -Seconds 2
        }
    } finally {
        if (-not $game.HasExited) { Stop-Process -Id $game.Id -Force }
        Copy-Item -LiteralPath $log -Destination (Join-Path $WorkDir "$Label-LogHost.json") -ErrorAction SilentlyContinue
    }
    Assert ($result -eq 'ScriptComplete') "$Label game hosts headless and finishes the script ($result)"
}

Remove-Item -LiteralPath $WorkDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $WorkDir | Out-Null
$releases = gh api "repos/$repo/releases?per_page=100" | Out-String | ConvertFrom-Json
$stable = @($releases | Where-Object { -not $_.draft -and -not $_.prerelease -and $_.tag_name -cmatch '^v[0-9]+\.[0-9]+\.[0-9]+$' } |
    Sort-Object { [version]$_.tag_name.Substring(1) } -Descending)
$toTag = $stable[0].tag_name
if (-not $FromTag) {
    if ($stable.Count -lt 2) { throw 'Need two stable releases (or pass -FromTag).' }
    $FromTag = $stable[1].tag_name
}
Write-Host "Updating $FromTag -> $toTag (latest stable)"
Assert ([version]$FromTag.Substring(1) -lt [version]$toTag.Substring(1)) 'Start version is older than latest stable'
# Updaters from v0.3.1 on replace their own .ps1 and keep only the previous installation.
$modernUpdater = $UseRepoUpdater -or [version]$FromTag.Substring(1) -ge [version]'0.3.1'

$expected = Get-Release $toTag (Join-Path $WorkDir 'expected')
$root = Get-Release $FromTag (Join-Path $WorkDir 'install with spaces')
if ($UseRepoUpdater) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot '..\..\Builds\Update_SS3D.ps1') -Destination (Join-Path $root 'Update_SS3D.ps1') -Force
    Write-Host 'Using this checkout''s Builds/Update_SS3D.ps1 instead of the shipped one'
}
$originalPs1 = Get-Hash (Join-Path $root 'Update_SS3D.ps1')
$originalBat = Get-Hash (Join-Path $root 'Update_SS3D.bat')

# Seed local data the update must carry over.
$network = Join-Path $root 'Game\Config\network.json'
[IO.File]::WriteAllText($network, ([IO.File]::ReadAllText($network) -replace '^\s*\{', "{`r`n  `"e2eMarker`": 1,"))
Copy-Item (Join-Path $root 'Game\Data\StationTemplates\ministation.json') (Join-Path $root 'Game\Data\StationTemplates\my custom map.json')
$edited = Join-Path $root 'Game\Data\Tilemaps\teststation.json'
[IO.File]::AppendAllText($edited, ' ')
New-Item -ItemType Directory -Force -Path (Join-Path $root 'Game\Data\ServerMeta'), (Join-Path $root 'Game\Logs') | Out-Null
[IO.File]::WriteAllText((Join-Path $root 'Game\Data\ServerMeta\bugs.jsonl'), "{`"id`":`"e2e`"}`n")
[IO.File]::WriteAllText((Join-Path $root 'Game\Logs\bug-e2e.zip'), 'bundle')
[IO.File]::WriteAllText((Join-Path $root 'my notes.txt'), 'not managed by the updater')
foreach ($old in @('20200101-000000-aaaaaaaa', '20200201-000000-bbbbbbbb')) {
    $dir = Join-Path $root ".ss3d-backups\$old\Game"
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    [IO.File]::WriteAllText((Join-Path $dir 'SS3D.exe'), 'stale backup')
}
$seeded = @('Game\Config\network.json', 'Game\Config\permissions.txt', 'Game\Data\StationTemplates\my custom map.json',
    'Game\Data\Tilemaps\teststation.json', 'Game\Data\ServerMeta\bugs.jsonl', 'Game\Logs\bug-e2e.zip', 'my notes.txt')
$before = @{}
foreach ($name in $seeded) { $before[$name] = Get-Hash (Join-Path $root $name) }
$oldGame = Get-Hash (Join-Path $root 'Game\SS3D_Data\globalgamemanagers')

$output = Invoke-Updater $root
Assert ($output -match "Updated to $toTag") "Updater reports the update to $toTag"
$metadata = Get-Content -LiteralPath (Join-Path $root '.ss3d-release.json') -Raw | ConvertFrom-Json
Assert ($metadata.tag -eq $toTag) ".ss3d-release.json says $toTag"
foreach ($name in $seeded) { Assert ((Get-Hash (Join-Path $root $name)) -eq $before[$name]) "Preserves $name" }

# Every shipped file outside the local-data folders must match the new release byte for byte.
$mismatches = @()
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $expected 'Game') -Recurse -File) {
    $relative = $file.FullName.Substring($expected.Length + 1)
    if ($relative -match '^Game\\(Config|Data|Logs)\\') { continue }
    $installed = Join-Path $root $relative
    if (-not (Test-Path -LiteralPath $installed) -or (Get-Hash $installed) -ne (Get-Hash $file.FullName)) { $mismatches += $relative }
}
Assert ($mismatches.Count -eq 0) "Installed Game files match $toTag ($($mismatches -join ', '))"
Assert ((Get-Hash (Join-Path $root 'Game\Data\StationTemplates\ministation.json')) -eq
    (Get-Hash (Join-Path $expected 'Game\Data\StationTemplates\ministation.json'))) 'Unmodified bundled map is the new release copy'
Assert ((Get-Hash (Join-Path $root 'Update_SS3D.bat')) -eq $originalBat) 'Update_SS3D.bat is left alone'

$backups = @(Get-ChildItem -LiteralPath (Join-Path $root '.ss3d-backups') -Directory)
$previous = @($backups | Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'Game\SS3D_Data\globalgamemanagers') })
Assert ($previous.Count -eq 1) 'One backup holds the previous installation'
Assert ((Get-Hash (Join-Path $previous[0].FullName 'Game\SS3D_Data\globalgamemanagers')) -eq $oldGame) "Backup is the $FromTag game"
if ($modernUpdater) {
    Assert ($backups.Count -eq 1) 'Older backups are pruned (n-1)'
    Assert ((Get-Hash (Join-Path $root 'Update_SS3D.ps1')) -eq (Get-Hash (Join-Path $expected 'Update_SS3D.ps1'))) 'Update_SS3D.ps1 updated itself'
} else {
    Write-Host "note: $FromTag updater predates self-update and pruning; $($backups.Count) backups remain"
    Assert ((Get-Hash (Join-Path $root 'Update_SS3D.ps1')) -eq $originalPs1) "$FromTag updater leaves its script alone"
}

if ($UseRepoUpdater) {
    # The update installed the release's own script; test the repeat with the candidate again.
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot '..\..\Builds\Update_SS3D.ps1') -Destination (Join-Path $root 'Update_SS3D.ps1') -Force
}
$output = Invoke-Updater $root
Assert ($output -match 'Already up to date') 'Repeat update is a no-op'
Assert (@(Get-ChildItem -LiteralPath (Join-Path $root '.ss3d-backups') -Directory).Count -eq $backups.Count) 'Repeat update adds no backup'

Test-Launch $root 'updated'
Test-Launch $previous[0].FullName 'rollback'
Write-Host "PASS: $FromTag -> $toTag Windows update, data preservation, repeat no-op, updated and rollback launches."
