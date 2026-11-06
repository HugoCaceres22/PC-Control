param([string]$Dotnet='dotnet',[switch]$SkipBuild)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
if(-not $SkipBuild){& (Join-Path $PSScriptRoot 'build.ps1') -Dotnet $Dotnet -Test}
$dist=Join-Path $PSScriptRoot 'dist'
$app=Join-Path $dist 'PCControl'
if(-not(Test-Path -LiteralPath (Join-Path $app 'PCControl.exe'))){throw 'Compila la publicación antes de empaquetar.'}
$sourceDir=Join-Path $dist 'third-party-sources'
New-Item -ItemType Directory -Force -Path $sourceDir | Out-Null
$index=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'third-party-sources.json') -Raw | ConvertFrom-Json
foreach($entry in $index){
    $archive=Join-Path $sourceDir ($entry.name+'-'+$entry.version+'.zip')
    Write-Host ('Obteniendo fuentes: '+$entry.name+' '+$entry.version)
    Invoke-WebRequest -Uri $entry.archive -OutFile $archive
    $zip=[IO.Compression.ZipFile]::Open($archive,[IO.Compression.ZipArchiveMode]::Update)
    try{
        if($zip.Entries.Count -lt 1){throw ('Archivo fuente vacío: '+$entry.name)}
        if($entry.name -eq 'LibreHardwareMonitor'){
            # Do not redistribute the upstream GUI's unrelated driver installer.
            $remove=@($zip.Entries|Where-Object {$_.FullName.EndsWith('/LibreHardwareMonitor/Resources/PawnIO_setup.exe')})
            foreach($item in $remove){$item.Delete()}
        }
    }finally{$zip.Dispose()}
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'third-party-sources.json') -Destination $sourceDir -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'TERCEROS.md') -Destination $sourceDir -Force
$sourcesZip=Join-Path $dist 'PCControl-fuentes-terceros.zip'
$appZip=Join-Path $dist 'PCControl-win-x64.zip'
Compress-Archive -Path (Join-Path $sourceDir '*') -DestinationPath $sourcesZip -Force
Compress-Archive -Path (Join-Path $app '*') -DestinationPath $appZip -Force
Get-FileHash -LiteralPath $appZip,$sourcesZip -Algorithm SHA256 | ForEach-Object {$_.Hash+'  '+(Split-Path $_.Path -Leaf)} | Set-Content -LiteralPath (Join-Path $dist 'SHA256SUMS.txt') -Encoding utf8
Write-Host 'Publica juntos los dos ZIP y SHA256SUMS.txt en la misma GitHub Release.'
