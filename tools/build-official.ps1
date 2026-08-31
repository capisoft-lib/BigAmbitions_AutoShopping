param([string]$SdkRoot)

$ErrorActionPreference = "Stop"
$modRoot = (Get-Item $PSScriptRoot).Parent.FullName
$SdkRoot = if ([string]::IsNullOrWhiteSpace($SdkRoot)) {
    [System.IO.Path]::GetFullPath((Join-Path $modRoot "..\..\.."))
} else {
    [System.IO.Path]::GetFullPath($SdkRoot)
}

. (Join-Path $SdkRoot "scripts\_project.ps1")

$lockFile = Join-Path $SdkRoot "Temp\UnityLockfile"
if (Test-Path -LiteralPath $lockFile) {
    try {
        $lockProbe = [System.IO.File]::Open($lockFile, 'Open', 'ReadWrite', 'None')
        $lockProbe.Dispose()
    }
    catch {
        throw "The SDK project is already locked. Close Unity before the official headless build: $lockFile"
    }
}

Assert-UnityProductVersion -BinaryPath $UnityEditor -Component "SDK Unity Editor"
$uiOutput = Join-Path $SdkRoot "Output\LIB_BaUnifiedUI\LIB_BaUnifiedUI.dll"
if (-not (Test-Path -LiteralPath $uiOutput)) {
    throw "Build LIB_BaUnifiedUI 1.0.0 first: $uiOutput"
}
Assert-PlayerRuntimeAssembly -DllPath $uiOutput

$logDir = Join-Path $SdkRoot ("Logs\AutoShopping\" + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $logDir -Force | Out-Null
$logPath = Join-Path $logDir "autoshopping-official-modbuilder.log"

$outputDir = [System.IO.Path]::GetFullPath((Join-Path $SdkRoot 'Output\AutoShopping'))
$installDir = [System.IO.Path]::GetFullPath((Join-Path $ModsLocalRoot 'AutoShopping'))
foreach ($entry in @(@($outputDir, 'previous-output'), @($installDir, 'previous-install'))) {
    if (Test-Path -LiteralPath $entry[0]) {
        Copy-Item -LiteralPath $entry[0] -Destination (Join-Path $logDir $entry[1]) -Recurse
    }
}

$previousMod = $env:BA_MOD_BUILD_CLI
$previousTimeout = $env:BA_MOD_BUILD_TIMEOUT_SECONDS
$timeout = Get-ModBuildTimeoutSeconds
$started = [DateTime]::UtcNow
try {
    $env:BA_MOD_BUILD_CLI = 'AutoShopping'
    $env:BA_MOD_BUILD_TIMEOUT_SECONDS = [string]$timeout
    Set-UnityBatchBuildEnvironment
    # The official bootstrap owns the async build and Editor shutdown. CLI run's
    # injected -quit exits too early, so invoke the exact Editor without -quit.
    $process = Start-Process -FilePath $UnityEditor -ArgumentList @(
        '-batchmode', '-nographics', '-ignoreCompilerErrors', '-disable-assembly-updater',
        '-projectPath', ('"' + $SdkRoot + '"'), '-logFile', ('"' + $logPath + '"')
    ) -WindowStyle Hidden -PassThru
    Write-Host "[build] Official Unity PID=$($process.Id), log=$logPath"
    while (-not $process.WaitForExit(1000)) {
        if (([DateTime]::UtcNow - $started).TotalSeconds -gt ($timeout + 120)) {
            $process.Kill() # Only this script's own batch child.
            throw "Official AutoShopping build timed out. See $logPath"
        }
    }
    if ($process.ExitCode -ne 0) {
        throw "Official Big Ambitions Mod Builder failed with exit code $($process.ExitCode). See $logPath"
    }
}
finally {
    $env:BA_MOD_BUILD_CLI = $previousMod
    $env:BA_MOD_BUILD_TIMEOUT_SECONDS = $previousTimeout
}

$marker = "[ModBuildCli] Build succeeded: " + (Join-Path $SdkRoot "Output\AutoShopping")
if (-not (Select-String -LiteralPath $logPath -SimpleMatch $marker -Quiet)) {
    throw "Unity exited successfully but the AutoShopping success marker is missing. See $logPath"
}
if ((Get-Item -LiteralPath (Join-Path $outputDir 'AutoShopping.dll')).LastWriteTimeUtc -lt $started) {
    throw "Official AutoShopping output is stale. See $logPath"
}

# The official packager copies runtime assets only. Include the public version,
# manifest, dependency notice, license and documentation in the publishable folder.
foreach ($name in @('ModManifest.asset', 'VERSION', 'REQUIRED_MODS.txt', 'LICENSE', 'README.md')) {
    Copy-Item -LiteralPath (Join-Path $modRoot $name) -Destination (Join-Path $outputDir $name) -Force
    Copy-Item -LiteralPath (Join-Path $modRoot $name) -Destination (Join-Path $installDir $name) -Force
}

& (Join-Path $modRoot "tools\validate-release.ps1") -SdkRoot $SdkRoot -RequireInstalled
Write-Host "[build] Official AutoShopping release is ready at $(Join-Path $SdkRoot 'Output\AutoShopping')."
