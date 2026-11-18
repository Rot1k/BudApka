param(
    [Parameter(Mandatory = $true)]
    [string] $IpAddress,

    [Parameter(Mandatory = $true)]
    [ValidateRange(1, 65535)]
    [int] $Port
)

$ErrorActionPreference = 'Stop'
$endpoint = "${IpAddress}:$Port"
$parsedAddress = $null
if (-not [System.Net.IPAddress]::TryParse($IpAddress, [ref] $parsedAddress)) {
    throw "Invalid IP address: $IpAddress"
}

$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectFile = Join-Path $projectDir 'BudApka.csproj'
if (-not (Test-Path -LiteralPath $projectFile)) {
    throw "MAUI project not found: $projectFile"
}

$adbCandidates = @(
    (Join-Path $env:LOCALAPPDATA 'Android\Sdk\platform-tools\adb.exe'),
    'C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe'
)
if ($env:ANDROID_SDK_ROOT) {
    $adbCandidates = @((Join-Path $env:ANDROID_SDK_ROOT 'platform-tools\adb.exe')) + $adbCandidates
}
if ($env:ANDROID_HOME) {
    $adbCandidates = @((Join-Path $env:ANDROID_HOME 'platform-tools\adb.exe')) + $adbCandidates
}
$adb = $adbCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $adb) {
    $adbCommand = Get-Command 'adb.exe' -ErrorAction SilentlyContinue
    if ($adbCommand) { $adb = $adbCommand.Source }
}
if (-not $adb) {
    throw 'ADB was not found. Install Android SDK Platform Tools or set ANDROID_SDK_ROOT.'
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw '.NET SDK was not found on PATH.'
}

Write-Host "Connecting to Android device at $endpoint ..."
$connectOutput = & $adb connect $endpoint 2>&1
$connectOutput | ForEach-Object { Write-Host $_ }
if ($LASTEXITCODE -ne 0) { throw "ADB could not connect to $endpoint." }
$deviceState = & $adb -s $endpoint get-state 2>&1
if ($LASTEXITCODE -ne 0 -or ($deviceState | Out-String).Trim() -ne 'device') {
    throw "ADB device $endpoint is not ready. Enable Wireless debugging and provide the current IP address and port. If pairing is requested, provide the pairing code and pairing port too."
}

Push-Location $projectDir
try {
    # Restart stale MSBuild workers that can block the Android AAPT2 daemon.
    & dotnet build-server shutdown
    if ($LASTEXITCODE -ne 0) { throw '.NET build-server shutdown failed.' }

    Write-Host 'Building and installing the MAUI Debug app through Fast Deployment ...'
    & dotnet build $projectFile -f net11.0-android -c Debug -t:Install "-p:AdbTarget=-s $endpoint" -nr:false
    if ($LASTEXITCODE -ne 0) { throw 'MAUI build/install failed.' }

    & $adb -s $endpoint logcat -c
    if ($LASTEXITCODE -ne 0) { throw 'Could not clear Android logcat.' }
    & $adb -s $endpoint shell monkey -p com.companyname.budapka -c android.intent.category.LAUNCHER 1
    if ($LASTEXITCODE -ne 0) { throw 'The app was installed, but Android could not launch it.' }
    Start-Sleep -Seconds 5

    $appPid = (& $adb -s $endpoint shell pidof com.companyname.budapka | Out-String).Trim()
    if (-not $appPid) {
        Write-Host 'Recent Android runtime errors:'
        & $adb -s $endpoint logcat -d -s monodroid:E AndroidRuntime:E Mono:E libc:F | Select-Object -Last 60
        throw 'The app did not stay running after launch; see the Android errors above.'
    }

    Write-Host "BudApka is installed and running on $endpoint (PID $appPid)."
}
finally {
    Pop-Location
}
