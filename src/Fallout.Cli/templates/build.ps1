[CmdletBinding()]
Param(
    [Parameter(Position=0,Mandatory=$false,ValueFromRemainingArguments=$true)]
    [string[]]$BuildArguments
)

# Provisioning chatter goes to standard error, so standard output carries only what the build
# itself writes. That is what lets `./build.ps1 --describe | ConvertFrom-Json` work.
[Console]::Error.WriteLine("PowerShell $($PSVersionTable.PSEdition) version $($PSVersionTable.PSVersion)")

Set-StrictMode -Version 2.0; $ErrorActionPreference = "Stop"; $ConfirmPreference = "None"; trap { Write-Error $_ -ErrorAction Continue; exit 1 }
$PSScriptRoot = Split-Path $MyInvocation.MyCommand.Path -Parent

###########################################################################
# CONFIGURATION
###########################################################################

$TempDirectory = Join-Path $PSScriptRoot '_ROOT_DIRECTORY_/.fallout/temp'

$DotNetGlobalFile = Join-Path $PSScriptRoot '_ROOT_DIRECTORY_/global.json'
$DotNetInstallUrl = "https://dot.net/v1/dotnet-install.ps1"
$DotNetChannel = "STS"

$env:DOTNET_CLI_TELEMETRY_OPTOUT = 1
$env:DOTNET_NOLOGO = 1

###########################################################################
# EXECUTION
###########################################################################

function ExecSafe([scriptblock] $cmd) {
    & $cmd
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
}

# If dotnet CLI is installed globally, use it; otherwise provision a local copy under .fallout\temp.
if ($null -ne (Get-Command "dotnet" -ErrorAction SilentlyContinue) -and `
     $(dotnet --version) -and $LASTEXITCODE -eq 0) {
    $env:DOTNET_EXE = (Get-Command "dotnet").Path
}
else {
    $DotNetInstallFile = Join-Path $TempDirectory 'dotnet-install.ps1'
    New-Item -ItemType Directory -Path $TempDirectory -Force | Out-Null
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    (New-Object System.Net.WebClient).DownloadFile($DotNetInstallUrl, $DotNetInstallFile)

    if (Test-Path $DotNetGlobalFile) {
        $DotNetGlobal = $(Get-Content $DotNetGlobalFile | Out-String | ConvertFrom-Json)
        if ($DotNetGlobal.PSObject.Properties["sdk"] -and $DotNetGlobal.sdk.PSObject.Properties["version"]) {
            $DotNetVersion = $DotNetGlobal.sdk.version
        }
    }

    $DotNetDirectory = Join-Path $TempDirectory 'dotnet-win'
    if (!(Test-Path variable:DotNetVersion)) {
        ExecSafe { & powershell $DotNetInstallFile -InstallDir $DotNetDirectory -Channel $DotNetChannel -NoPath }
    } else {
        ExecSafe { & powershell $DotNetInstallFile -InstallDir $DotNetDirectory -Version $DotNetVersion -NoPath }
    }
    $env:DOTNET_EXE = Join-Path $DotNetDirectory 'dotnet.exe'
    $env:PATH = "$DotNetDirectory;$env:PATH"
}

[Console]::Error.WriteLine("Microsoft (R) .NET SDK version $(& $env:DOTNET_EXE --version)")

# PowerShell has no `1>&2`, so the restore's output is pumped to standard error explicitly.
ExecSafe { & $env:DOTNET_EXE tool restore 2>&1 | ForEach-Object { [Console]::Error.WriteLine($_) } }
ExecSafe { & $env:DOTNET_EXE fallout $BuildArguments }
