param([switch]$NoBuild)
$ErrorActionPreference='Stop'
Push-Location $PSScriptRoot
try {
    if(-not $NoBuild){ & ./Build.ps1 }
    $package=Join-Path $PSScriptRoot 'artifacts/windows-x64'
    if(-not (Test-Path "$package/Windeck.exe")){throw 'Windeck.exe nao foi compilado.'}
    Get-ChildItem $package -Filter '*.pdb' | Remove-Item
    Copy-Item README.md,RELEASE-NOTES.md,LICENSE $package -Force
    foreach($directory in @('docs','THIRD-PARTY','design')){
        $target=Join-Path $package $directory
        New-Item $target -ItemType Directory -Force | Out-Null
        Get-ChildItem $directory -File | Where-Object { $_.Extension -notin @('.log') -and $_.Name -notin @('package-lock.json','package.json','check-preview.cjs') } | Copy-Item -Destination $target -Force
    }
    Set-Content -Path "$package/Previa.cmd" -Value '@start "" "%~dp0Windeck.exe" --preview' -Encoding ascii
    Set-Content -Path "$package/Restaurar-barra.cmd" -Value '@start "" "%~dp0Windeck.exe" --restore' -Encoding ascii
    $zip=Join-Path $PSScriptRoot 'artifacts/Windeck-windows-x64.zip'
    Compress-Archive -Path "$package/*" -DestinationPath $zip -Force
    $hash=(Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    Set-Content -Path "$zip.sha256" -Value "$hash  Windeck-windows-x64.zip" -Encoding ascii
    Write-Host "Pacote criado: $zip"
} finally {Pop-Location}
