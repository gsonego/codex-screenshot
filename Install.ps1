$ErrorActionPreference = 'Stop'
$marketplaceDirectory = Join-Path $PSScriptRoot '.agents\plugins'
New-Item -ItemType Directory -Force -Path $marketplaceDirectory | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'marketplace.json') -Destination (Join-Path $marketplaceDirectory 'marketplace.json')
& codex plugin marketplace add $PSScriptRoot --json
if ($LASTEXITCODE -ne 0) { throw 'Nao foi possivel registrar o marketplace.' }
& codex plugin add codex-screenshot@codex-screenshot-local --json
if ($LASTEXITCODE -ne 0) { throw 'Nao foi possivel instalar o plugin.' }
Write-Output 'Codex Screenshot instalado. Abra uma nova conversa ou reinicie o app para carregar a ferramenta.'
