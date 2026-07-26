[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

function Write-Info([string]$Message) {
    Write-Host "[INFO] $Message" -ForegroundColor Blue
}

function Write-Success([string]$Message) {
    Write-Host "[OK] $Message" -ForegroundColor Green
}

function Write-WarningMessage([string]$Message) {
    Write-Host "[WARN] $Message" -ForegroundColor Yellow
}

function Write-ErrorMessage([string]$Message) {
    [Console]::Error.WriteLine("[ERROR] $Message")
}

function Test-Command([string]$Name) {
    return $null -ne (Get-Command $Name -ErrorAction SilentlyContinue)
}

function Assert-LastExitCode([string]$Message) {
    if ($LASTEXITCODE -ne 0) {
        throw $Message
    }
}

function ConvertTo-BashSingleQuoted([string]$Value) {
    return "'" + $Value.Replace("'", "'\''") + "'"
}

try {
    $ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
    Set-Location $ProjectRoot

    Write-Info "Checking required commands..."

    if (-not (Test-Command "git")) {
        throw "Git is not installed."
    }

    if (-not (Test-Command "dotnet")) {
        throw ".NET SDK is not installed. Install the .NET SDK and run this script again. https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.302/dotnet-sdk-10.0.302-win-x64.exe"
    }

    Write-Success "Required commands are available."
    Write-Info "Checking repository..."

    & git rev-parse --is-inside-work-tree *> $null
    Assert-LastExitCode "This directory is not a Git repository."

    if (-not (Test-Path "ProjectSettings/ProjectVersion.txt" -PathType Leaf)) {
        throw "ProjectSettings/ProjectVersion.txt was not found. Run this script from the Unity project repository."
    }

    if ((-not (Test-Path "unity1week-2026-07.slnx" -PathType Leaf)) -or
        (-not (Test-Path "Assembly-CSharp.csproj" -PathType Leaf))) {
        throw "Unity-generated .slnx / .csproj files were not found. Open the project in Unity Editor once, then run this script again."
    }

    Write-Success "Git repository detected."

    if (Test-Path ".config/dotnet-tools.json" -PathType Leaf) {
        Write-Info "Restoring repository-local .NET tools..."
        & dotnet tool restore
        Assert-LastExitCode "Failed to restore repository-local .NET tools."
        Write-Success ".NET tools restored."
    }
    else {
        Write-WarningMessage ".config/dotnet-tools.json was not found."
        Write-WarningMessage "CSharpier and other local .NET tools were not restored."
    }

    Write-Info "Configuring repository-local Git settings..."
    & git config --local commit.template .github/commit-message-template.txt
    Assert-LastExitCode "Failed to configure the commit template."
    & git config --local core.hooksPath .githooks
    Assert-LastExitCode "Failed to configure Git hooks."

    if (Test-Command "chmod") {
        & chmod +x .githooks/pre-commit .githooks/pre-push scripts/setup.sh scripts/setup.ps1
        Assert-LastExitCode "Failed to set executable permissions on Git hooks."
    }

    Write-Success "Commit template and Git hooks configured."

    $VersionLine = Get-Content "ProjectSettings/ProjectVersion.txt" |
        Where-Object { $_ -match '^m_EditorVersion:\s*(.+?)\s*$' } |
        Select-Object -First 1

    if ($null -eq $VersionLine) {
        throw "Could not read the Unity version from ProjectVersion.txt."
    }

    $UnityVersion = ([regex]::Match($VersionLine, '^m_EditorVersion:\s*(.+?)\s*$')).Groups[1].Value
    Write-Info "Unity version: $UnityVersion"

    $Candidates = [System.Collections.Generic.List[string]]::new()

    if (-not [string]::IsNullOrWhiteSpace($env:UNITY_YAML_MERGE_PATH)) {
        $Candidates.Add($env:UNITY_YAML_MERGE_PATH)
    }

    if (-not [string]::IsNullOrWhiteSpace($env:UNITY_EDITOR_PATH)) {
        $EditorDirectory = Split-Path -Parent $env:UNITY_EDITOR_PATH
        $Candidates.Add((Join-Path $EditorDirectory "Data/Tools/UnityYAMLMerge.exe"))
        $Candidates.Add((Join-Path $EditorDirectory "Data/Tools/UnityYAMLMerge"))
        $Candidates.Add((Join-Path $EditorDirectory "../Tools/UnityYAMLMerge"))
    }

    if ($IsMacOS -or $PSVersionTable.PSEdition -eq "Desktop" -and $env:OS -ne "Windows_NT") {
        $Candidates.Add("/Applications/Unity/Hub/Editor/$UnityVersion/Unity.app/Contents/Tools/UnityYAMLMerge")
        $Candidates.Add((Join-Path $HOME "Applications/Unity/Hub/Editor/$UnityVersion/Unity.app/Contents/Tools/UnityYAMLMerge"))
    }
    elseif ($IsLinux) {
        $Candidates.Add((Join-Path $HOME "Unity/Hub/Editor/$UnityVersion/Editor/Data/Tools/UnityYAMLMerge"))
        $Candidates.Add("/opt/unity/editors/$UnityVersion/Editor/Data/Tools/UnityYAMLMerge")
    }
    elseif ($IsWindows -or $env:OS -eq "Windows_NT") {
        if ($env:ProgramFiles) {
            $Candidates.Add((Join-Path $env:ProgramFiles "Unity/Hub/Editor/$UnityVersion/Editor/Data/Tools/UnityYAMLMerge.exe"))
        }
        if (${env:ProgramFiles(x86)}) {
            $Candidates.Add((Join-Path ${env:ProgramFiles(x86)} "Unity/Hub/Editor/$UnityVersion/Editor/Data/Tools/UnityYAMLMerge.exe"))
        }
    }
    else {
        Write-WarningMessage "Unknown operating system."
    }

    Write-Info "Searching for UnityYAMLMerge..."
    $UnityYamlMerge = $Candidates |
        Where-Object { Test-Path $_ -PathType Leaf } |
        Select-Object -First 1

    if ([string]::IsNullOrWhiteSpace($UnityYamlMerge)) {
        throw "UnityYAMLMerge was not found. Make sure Unity $UnityVersion is installed through Unity Hub, or set UNITY_YAML_MERGE_PATH before running scripts/setup.ps1."
    }

    $UnityYamlMerge = (Resolve-Path $UnityYamlMerge).Path
    Write-Success "UnityYAMLMerge found:"
    Write-Host "  $UnityYamlMerge"

    $GitDirectory = (& git rev-parse --absolute-git-dir).Trim()
    Assert-LastExitCode "Could not determine the Git directory."
    $MergeDriver = Join-Path $GitDirectory "unityyamlmerge-driver.sh"
    $BashUnityYamlMerge = ConvertTo-BashSingleQuoted ($UnityYamlMerge -replace '\\', '/')

    Write-Info "Creating UnityYAMLMerge wrapper..."
    $Wrapper = @"
#!/usr/bin/env bash
set -Eeuo pipefail

UNITY_YAML_MERGE=$BashUnityYamlMerge

exec "`$UNITY_YAML_MERGE" merge -p "`$1" "`$2" "`$3" "`$4"
"@
    [System.IO.File]::WriteAllText($MergeDriver, $Wrapper, [System.Text.UTF8Encoding]::new($false))

    if (Test-Command "chmod") {
        & chmod +x $MergeDriver
        Assert-LastExitCode "Failed to make the UnityYAMLMerge wrapper executable."
    }

    Write-Success "UnityYAMLMerge wrapper created."
    Write-Info "Registering UnityYAMLMerge with Git..."

    $MergeDriverForGit = $MergeDriver -replace '\\', '/'
    & git config --local merge.unityyamlmerge.name "Unity Smart Merge"
    Assert-LastExitCode "Failed to configure the Unity merge driver name."
    & git config --local merge.unityyamlmerge.driver "`"$MergeDriverForGit`" %O %B %A %A"
    Assert-LastExitCode "Failed to configure the Unity merge driver command."
    & git config --local merge.unityyamlmerge.recursive binary
    Assert-LastExitCode "Failed to configure the recursive Unity merge driver."

    Write-Success "UnityYAMLMerge registered in the local repository."

    if ((Test-Path ".config/dotnet-tools.json" -PathType Leaf) -and
        (Select-String -Path ".config/dotnet-tools.json" -Pattern "csharpier" -Quiet)) {
        Write-Info "Checking CSharpier installation..."
        $CSharpierVersion = & dotnet csharpier --version
        Assert-LastExitCode "CSharpier could not be executed after tool restore."
        Write-Success "CSharpier is available: $($CSharpierVersion.Trim())"
    }

    Write-Host ""
    Write-Success "Project setup completed."
    Write-Host ""
    Write-Host "Configured Unity version : $UnityVersion"
    Write-Host "UnityYAMLMerge           : $UnityYamlMerge"
    $ConfiguredDriver = (& git config --local --get merge.unityyamlmerge.driver).Trim()
    Write-Host "Git merge driver         : $ConfiguredDriver"
    Write-Host ""
    Write-Host "Open the project through Unity Hub using Unity $UnityVersion."
}
catch {
    Write-ErrorMessage $_.Exception.Message
    exit 1
}
