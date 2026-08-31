param(
    [string]$SdkRoot,
    [switch]$RequireInstalled
)

$ErrorActionPreference = "Stop"
$modRoot = (Get-Item $PSScriptRoot).Parent.FullName
$SdkRoot = if ([string]::IsNullOrWhiteSpace($SdkRoot)) {
    [System.IO.Path]::GetFullPath((Join-Path $modRoot "..\..\.."))
} else {
    [System.IO.Path]::GetFullPath($SdkRoot)
}

$modId = "AutoShopping"
$version = (Get-Content -LiteralPath (Join-Path $modRoot "VERSION") -Raw).Trim()
$manifest = Get-Content -LiteralPath (Join-Path $modRoot "ModManifest.asset") -Raw
$versionSource = Get-Content -LiteralPath (Join-Path $modRoot "Scripts\AutoShoppingConfig.cs") -Raw
$shortcutSource = Get-Content -LiteralPath (Join-Path $modRoot "Scripts\AutoShoppingShortcuts.cs") -Raw
$driverSource = Get-Content -LiteralPath (Join-Path $modRoot "Scripts\AutoShoppingDriver.cs") -Raw
$toggleHudSource = Get-Content -LiteralPath (Join-Path $modRoot "Scripts\AutoShoppingToggleHud.cs") -Raw
$panelSource = Get-Content -LiteralPath (Join-Path $modRoot "Scripts\AutoShoppingPanel.cs") -Raw
$configSource = Get-Content -LiteralPath (Join-Path $modRoot "Scripts\AutoShoppingConfig.cs") -Raw
$routeSource = Get-Content -LiteralPath (Join-Path $modRoot "Scripts\StoreItemRouteService.cs") -Raw
$required = Get-Content -LiteralPath (Join-Path $modRoot "REQUIRED_MODS.txt") -Raw
$asmdef = Get-Content -LiteralPath (Join-Path $modRoot "AutoShopping.asmdef") -Raw

if ($version -ne "1.0.1" -or $manifest -notmatch '(?m)^\s*Version:\s*1\.0\.1\s*$' -or
    $versionSource -notmatch 'Version\s*=\s*"1\.0\.1"') {
    throw "AutoShopping version surfaces are not aligned to 1.0.1."
}
if ($required -notmatch 'LIB_BaUnifiedUI\s+1\.0\.0\+' -or
    $asmdef -notmatch 'f1e2d3c4b5a6478890ab1c2d3e4f5a6b' -or
    $asmdef -match '"LIB_BaUnifiedUI[^"/]*\.dll"') {
    throw "AutoShopping does not declare the standalone LIB_BaUnifiedUI 1.0.0+ contract."
}
$keybindOptionCount = ([regex]::Matches($shortcutSource, 'AddKeybind\s*\(')).Count
if ($asmdef -notmatch '75469ad4d38634e559750d17036d5f7c' -or
    $keybindOptionCount -ne 4 -or
    $shortcutSource -notmatch 'new\s+BaKeybind\s*\(\s*Key\.F8\s*\)' -or
    $driverSource -notmatch 'AutoShoppingShortcuts\.Tick\s*\(' -or
    $driverSource -match 'Input\.GetKeyDown\s*\(\s*KeyCode\.F8\s*\)' -or
    $toggleHudSource -notmatch 'AddToggleButtonHint\s*\(' -or
    $toggleHudSource -notmatch 'TryInvokeToggleShortcut\s*\(') {
    throw "AutoShopping configurable shortcut contract is incomplete or still uses hard-coded F8 polling."
}
if ($configSource -notmatch 'AddColorPicker\s*\(' -or
    $configSource -notmatch 'StoreItemRouteService\.SetLineColor\s*\(' -or
    $routeSource -notmatch 'internal\s+static\s+void\s+SetLineColor\s*\(' -or
    $panelSource -notmatch 'BaPanelRecipe\.MainPanel' -or
    $toggleHudSource -notmatch 'BaPanelRecipe\.ActionPanel' -or
    $panelSource -notmatch 'TryInvokeClearShortcut\s*\(' -or
    $panelSource -notmatch 'TryInvokePayShortcut\s*\(' -or
    $panelSource -notmatch 'TryInvokeCancelActionsShortcut\s*\(') {
    throw "AutoShopping color, fluent panel, or action shortcut contract is incomplete."
}

$releaseDir = Join-Path $modRoot "releases\$version"
foreach ($name in @("short-description.txt", "short-description.fr.txt", "full-description.md", "full-description.fr.md", "Steam_ChangeLog.md", "Steam_ChangeLog.fr.md")) {
    if (-not (Test-Path -LiteralPath (Join-Path $releaseDir $name))) {
        throw "Missing release text: $name"
    }
}
foreach ($name in @('short-description.txt', 'short-description.fr.txt')) {
    $short = (Get-Content -LiteralPath (Join-Path $releaseDir $name) -Raw -Encoding UTF8).Trim()
    if ($short.Length -lt 1 -or $short.Length -gt 300) {
        throw "Steam $name must contain 1-300 characters; found $($short.Length)."
    }
}
foreach ($name in @('full-description.md', 'full-description.fr.md', 'Steam_ChangeLog.md', 'Steam_ChangeLog.fr.md')) {
    $text = Get-Content -LiteralPath (Join-Path $releaseDir $name) -Raw -Encoding UTF8
    if ([Text.Encoding]::UTF8.GetByteCount($text) -gt 8000 -or $text -notmatch '1\.0\.1') {
        throw "Steam $name exceeds 8000 UTF-8 bytes or omits version 1.0.1."
    }
}

$localeFiles = @(Get-ChildItem -LiteralPath (Join-Path $modRoot "Locales") -Filter *.json -File)
$expectedLocales = @('cs', 'da', 'de', 'el', 'en', 'es', 'fi', 'fr', 'hu', 'it', 'ja', 'ko', 'lt', 'nl', 'pl', 'pt', 'ro', 'ru', 'tr', 'uk', 'zh-cn', 'zh-tw')
if (Compare-Object $expectedLocales @($localeFiles.BaseName | Sort-Object) -CaseSensitive) {
    throw 'AutoShopping must cover all 22 native game locale codes.'
}
$english = Get-Content -LiteralPath (Join-Path $modRoot 'Locales\en.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$baseline = @($english.PSObject.Properties.Name | Sort-Object)
foreach ($file in $localeFiles) {
    $locale = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
    $keys = @($locale.PSObject.Properties.Name | Sort-Object)
    if (Compare-Object $baseline $keys) {
        throw "Locale keys differ from English: $($file.Name)"
    }
    foreach ($key in $baseline) {
        $value = $locale.$key
        $expectedTokens = @([regex]::Matches($english.$key, '\{[^{}]+\}') | ForEach-Object Value | Sort-Object) -join '|'
        $actualTokens = @([regex]::Matches([string]$value, '\{[^{}]+\}') | ForEach-Object Value | Sort-Object) -join '|'
        if ($value -isnot [string] -or [string]::IsNullOrWhiteSpace($value) -or $actualTokens -cne $expectedTokens) {
            throw "Empty/invalid translation or placeholder mismatch: $($file.Name)/$key"
        }
    }
}

$outputDir = Join-Path $SdkRoot "Output\$modId"
$outputDll = Join-Path $outputDir "$modId.dll"
if (-not (Test-Path -LiteralPath $outputDll)) {
    throw "Missing official output: $outputDll"
}
. (Join-Path $SdkRoot "scripts\_project.ps1")
Assert-PlayerRuntimeAssembly -DllPath $outputDll
if ([System.Reflection.AssemblyName]::GetAssemblyName($outputDll).Name -ne $modId) {
    throw "Unexpected AutoShopping assembly identity."
}
$compiled = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($outputDll)
try {
    $configType = $compiled.MainModule.Types | Where-Object FullName -eq 'AutoShopping.AutoShoppingConfig'
    $compiledVersion = $configType.Fields | Where-Object Name -eq 'Version'
    if ($null -eq $compiledVersion -or $compiledVersion.Constant -ne $version) {
        throw 'Compiled AutoShopping version does not match VERSION.'
    }
    $references = @($compiled.MainModule.AssemblyReferences | Select-Object -ExpandProperty Name)
    $shortcutType = $compiled.MainModule.Types | Where-Object FullName -eq 'AutoShopping.AutoShoppingShortcuts'
    $driverType = $compiled.MainModule.Types | Where-Object FullName -eq 'AutoShopping.AutoShoppingDriver'
    $driverUpdate = $driverType.Methods | Where-Object Name -eq 'Update'
    $tickCalls = @($driverUpdate.Body.Instructions | Where-Object {
        $_.Operand -and $_.Operand.ToString() -match 'AutoShoppingShortcuts::Tick\(\)'
    })
    if ('LIB_BaUnifiedUI' -notin $references -or
        'Unity.InputSystem' -notin $references -or
        $null -eq $shortcutType -or
        $null -eq $driverUpdate -or
        $tickCalls.Count -ne 1) {
        throw "Compiled AutoShopping shortcut contract is missing BAUI/Input System references or its Update hook."
    }
}
finally {
    $compiled.Dispose()
}
$bundled = @(Get-ChildItem -LiteralPath $outputDir -Filter "LIB_BaUnifiedUI*.dll" -File -Recurse -ErrorAction SilentlyContinue)
if ($bundled.Count -ne 0) {
    throw "AutoShopping output embeds LIB_BaUnifiedUI: $($bundled.FullName -join ', ')"
}
$packagedLocales = @(Get-ChildItem -LiteralPath (Join-Path $outputDir 'Locales') -Filter '*.json' -File)
if (Compare-Object $expectedLocales @($packagedLocales.BaseName | Sort-Object) -CaseSensitive) {
    throw 'Official output does not contain all 22 locales.'
}
foreach ($file in $localeFiles) {
    $packagedFile = Join-Path $outputDir ('Locales\' + $file.Name)
    if ((Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath $packagedFile).Hash) {
        throw "Packaged locale differs from source: $($file.Name)"
    }
}
foreach ($name in @('ModManifest.asset', 'VERSION', 'REQUIRED_MODS.txt', 'LICENSE', 'README.md')) {
    $packagedFile = Join-Path $outputDir $name
    if (-not (Test-Path -LiteralPath $packagedFile) -or
        (Get-FileHash -LiteralPath (Join-Path $modRoot $name)).Hash -ne (Get-FileHash -LiteralPath $packagedFile).Hash) {
        throw "Missing or stale public package metadata: $name"
    }
}

$hash = (Get-FileHash -LiteralPath $outputDll -Algorithm SHA256).Hash
if ($RequireInstalled) {
    $installedDll = Join-Path $ModsLocalRoot "$modId\$modId.dll"
    if (-not (Test-Path -LiteralPath $installedDll)) {
        throw "Installed AutoShopping DLL is missing: $installedDll"
    }
    $installedHash = (Get-FileHash -LiteralPath $installedDll -Algorithm SHA256).Hash
    if ($hash -ne $installedHash) {
        throw "AutoShopping output and installed DLL hashes differ."
    }
    $installedDir = Join-Path $ModsLocalRoot $modId
    foreach ($file in Get-ChildItem -LiteralPath $outputDir -File -Recurse) {
        $relative = $file.FullName.Substring($outputDir.Length + 1)
        $installedFile = Join-Path $installedDir $relative
        if (-not (Test-Path -LiteralPath $installedFile) -or
            (Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath $installedFile).Hash) {
            throw "Installed file differs from official output: $relative"
        }
    }
}

Write-Host "[verify] mod=$modId version=$version locales=$($localeFiles.Count) sha256=$hash installed=$RequireInstalled embedded_baui=0"
