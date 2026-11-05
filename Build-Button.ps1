$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$buildDirectory = Join-Path $PSScriptRoot 'build'
New-Item -ItemType Directory -Path $buildDirectory -Force | Out-Null
& $compiler /nologo /target:winexe /optimize+ /reference:System.Drawing.dll /reference:System.Windows.Forms.dll "/out:$buildDirectory\IconBuilder.exe" "$PSScriptRoot\CodexScreenshot.cs"
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o gerador de icones.' }
$icons = Start-Process -FilePath "$buildDirectory\IconBuilder.exe" -ArgumentList @('--export-icons', ('"' + $PSScriptRoot + '\assets"')) -WindowStyle Hidden -PassThru -Wait
if ($icons.ExitCode -ne 0) { throw 'Falha ao gerar icones.' }
& $compiler /nologo /target:winexe /optimize+ /reference:System.Drawing.dll /reference:System.Windows.Forms.dll "/win32icon:$PSScriptRoot\assets\camera.ico" "/out:$buildDirectory\CodexScreenshot.exe" "$PSScriptRoot\CodexScreenshot.cs"
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar CodexScreenshot.exe.' }
$check = Start-Process -FilePath "$buildDirectory\CodexScreenshot.exe" -ArgumentList '--self-test' -WindowStyle Hidden -PassThru -Wait
if ($check.ExitCode -ne 0) { throw 'Falha nos testes de selecao e foco.' }
Write-Output 'Botao compilado; testes de coordenadas e identificacao do app passaram.'
if (Get-Process -Name CodexScreenshot -ErrorAction SilentlyContinue) {
    Write-Output 'Atualizacao pronta em build\CodexScreenshot.exe; feche o botao antes de substituir o executavel em uso.'
} else {
    try {
        Copy-Item -LiteralPath "$buildDirectory\CodexScreenshot.exe" -Destination "$PSScriptRoot\CodexScreenshot.exe" -Force
    } catch [System.IO.IOException] {
        Write-Output 'Atualizacao pronta em build\CodexScreenshot.exe; o executavel atual esta em uso e precisa ser fechado antes da substituicao.'
    }
}
