# Refresh this project's Development connection without changing or recreating databases.
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectPath = Join-Path $PSScriptRoot '..\Intravision\Intravision.csproj'
$logPath = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Microsoft\Microsoft SQL Server Local DB\Instances\MSSQLLocalDB\error.log'
foreach ($command in @('dotnet', 'sqlcmd', 'sqllocaldb')) {
    if (-not (Get-Command $command -ErrorAction SilentlyContinue)) {
        throw "Required command is missing: $command"
    }
}

function Test-DatabaseConnection([string] $serverName) {
    # Windows PowerShell treats redirected native stderr as PowerShell errors.
    $ErrorActionPreference = 'Continue'
    $null = & sqlcmd -S $serverName -E -d Intravision -l 3 -b -Q 'SET NOCOUNT ON; SELECT 1;' 2>&1
    return $LASTEXITCODE -eq 0
}

function Get-LivePipe {
    if (-not (Test-Path -LiteralPath $logPath)) { return $null }
    $logText = Get-Content -LiteralPath $logPath -Raw
    $pipeMatches = [regex]::Matches($logText, '\\\\\.\\pipe\\LOCALDB#[A-Fa-f0-9]+\\tsql\\query')
    if ($pipeMatches.Count -eq 0) { return $null }
    $candidate = 'np:' + $pipeMatches[$pipeMatches.Count - 1].Value
    if (Test-DatabaseConnection $candidate) { return $candidate }
    return $null
}

$serverName = '(localdb)\MSSQLLocalDB'
if (-not (Test-DatabaseConnection $serverName)) {
    $serverName = Get-LivePipe
    if (-not $serverName) {
        # Some installations report failure even though the engine starts.
        # Trust a successful SQL connection, not the utility's status alone.
        & sqllocaldb start MSSQLLocalDB
        $serverName = Get-LivePipe
    }
}
if (-not $serverName) {
    throw "Cannot reach the existing Intravision database. Inspect $logPath. No connection settings were changed."
}

$connection = "Server=$serverName;Database=Intravision;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
& dotnet user-secrets set 'ConnectionStrings:DefaultConnection' $connection --project $projectPath
if ($LASTEXITCODE -ne 0) { throw 'Could not update the Development connection.' }
Write-Host 'Database connection verified. Stop debugging and start the project again.'
if ($serverName.StartsWith('np:')) {
    Write-Host 'LocalDB instance discovery failed; using the verified current pipe. Run this helper again after LocalDB restarts.'
}
