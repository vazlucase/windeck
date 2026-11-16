param([switch]$FrameworkDependent)
$ErrorActionPreference='Stop'
Push-Location $PSScriptRoot
try {
    dotnet run --project tests/Windeck.Checks -c Release
    if($LASTEXITCODE -ne 0){throw 'Os testes falharam.'}
    $selfContained=if($FrameworkDependent){'false'}else{'true'}
    dotnet publish src/Windeck.App -c Release -r win-x64 --self-contained $selfContained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o artifacts/windows-x64
    if($LASTEXITCODE -ne 0){throw 'A compilacao falhou.'}
    Write-Host 'Executavel criado em artifacts/windows-x64/Windeck.exe'
} finally {Pop-Location}
