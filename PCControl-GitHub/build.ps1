param([string]$Dotnet='dotnet',[switch]$Test,[switch]$HardwareTests,[switch]$UiTests)
$ErrorActionPreference='Stop'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:NUGET_PACKAGES=Join-Path $PSScriptRoot '.packages'
Push-Location $PSScriptRoot
try {
    $expected='364E5D98D4D134BD54DD25C22ED2CA2F4883F8BC3ED6502BEE0C151E3436D30C'
    if((Get-FileHash 'tools/PresentMon.exe' -Algorithm SHA256).Hash -ne $expected){throw 'El hash del PresentMon incluido no coincide con la versión oficial fijada.'}
    & $Dotnet restore PCControl.csproj --configfile NuGet.Config
    if($LASTEXITCODE){throw 'Error de restauración'}
    & $Dotnet build PCControl.csproj -c Release --no-restore
    if($LASTEXITCODE){throw 'Error de compilación'}
    if($Test -or $HardwareTests -or $UiTests){
        & $Dotnet restore tests/PCControl.Tests.csproj --configfile NuGet.Config
        if($LASTEXITCODE){throw 'Error al restaurar las pruebas'}
        & $Dotnet build tests/PCControl.Tests.csproj -c Release --no-restore
        if($LASTEXITCODE){throw 'Error al compilar las pruebas'}
        $testArgs=@()
        if($HardwareTests){$testArgs+='--hardware'}
        if($UiTests){$testArgs+='--ui'}
        & $Dotnet run --project tests/PCControl.Tests.csproj -c Release --no-build --no-restore -- @testArgs
        if($LASTEXITCODE){throw 'Error en pruebas'}
    }
    $config=Join-Path $PSScriptRoot 'NuGet.Config'
    & $Dotnet publish PCControl.csproj -c Release -r win-x64 --self-contained true "-p:RestoreConfigFile=$config" -o dist/PCControl
    if($LASTEXITCODE){throw 'Error de publicación'}
    Write-Host 'Publicación preparada en dist/PCControl/PCControl.exe'
} finally { Pop-Location }
